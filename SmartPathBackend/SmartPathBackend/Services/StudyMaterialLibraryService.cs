using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;
using SmartPathBackend.Utils;
using Amazon.S3;
using Amazon.S3.Model;

namespace SmartPathBackend.Services
{
    public class StudyMaterialLibraryService : IStudyMaterialLibraryService
    {
        private readonly SmartPathDbContext _context;
        private readonly ILogger<StudyMaterialLibraryService> _logger;
        private readonly IStudyMaterialAiReviewer _aiReviewer;
        private readonly IIntelligentFileSummarizer _fileSummarizer;
        private readonly IAmazonS3 _s3;
        private readonly R2Options _r2;

        public StudyMaterialLibraryService(
            SmartPathDbContext context,
            ILogger<StudyMaterialLibraryService> logger,
            IStudyMaterialAiReviewer aiReviewer,
            IIntelligentFileSummarizer fileSummarizer,
            IAmazonS3 s3,
            IOptions<R2Options> r2)
        {
            _context = context;
            _logger = logger;
            _aiReviewer = aiReviewer;
            _fileSummarizer = fileSummarizer;
            _s3 = s3;
            _r2 = r2.Value;
        }

        public async Task<StudyMaterialResponse> CreateAsync(Guid uploaderId, StudyMaterialCreateMeta meta, IFormFile? file, CancellationToken ct)
        {
            var category = await _context.MaterialCategories.FindAsync(meta.CategoryId);
            if (category == null)
            {
                throw new ArgumentException($"Category with ID {meta.CategoryId} not found");
            }

            var material = new StudyMaterial
            {
                Id = Guid.NewGuid(),
                UploaderId = uploaderId,
                CategoryId = meta.CategoryId,
                Title = meta.Title,
                Description = meta.Description,
                SourceType = meta.SourceType,
                Status = Status.Pending,
                CreatedAt = DateTime.UtcNow,
                // Explicitly initialize to null to prevent any default values
                FileUrl = null,
                SourceUrl = null,
                MimeType = null,
                FileSize = null
            };

            // Auto-determine SourceType based on input
            StudyMaterialSourceType actualSourceType;
            if (file != null)
            {
                actualSourceType = StudyMaterialSourceType.File;
            }
            else if (!string.IsNullOrEmpty(meta.SourceUrl))
            {
                actualSourceType = StudyMaterialSourceType.Url;
            }
            else
            {
                // If both are provided, prioritize file, otherwise use the provided SourceType
                actualSourceType = meta.SourceType;
            }

            if (actualSourceType == StudyMaterialSourceType.File)
            {
                if (file == null)
                    throw new ArgumentException("File is required when SourceType is File");

                // Save file to temporary location for later R2 upload on acceptance
                var tempDirectory = Path.Combine(Path.GetTempPath(), "StudyMaterials", material.Id.ToString());
                Directory.CreateDirectory(tempDirectory);
                var tempFilePath = Path.Combine(tempDirectory, file.FileName);

                using (var fileStream = file.OpenReadStream())
                using (var tempStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(tempStream, ct);
                }

                // Store the temporary file path for now (will be updated with R2 URL on acceptance)
                material.FileUrl = tempFilePath;
                material.MimeType = file.ContentType;
                material.FileSize = file.Length;
                material.SourceUrl = null; // Ensure SourceUrl is null when SourceType is File
            }
            else if (meta.SourceType == StudyMaterialSourceType.Url)
            {
                if (string.IsNullOrEmpty(meta.SourceUrl))
                    throw new ArgumentException("SourceUrl is required when SourceType is Url");

                material.SourceUrl = meta.SourceUrl;
                material.FileUrl = null; // Ensure FileUrl is null when SourceType is Url
            }
            else
            {
                throw new ArgumentException($"Invalid SourceType: {meta.SourceType}");
            }

            // Debug logging
            _logger.LogInformation("Creating StudyMaterial: SourceType={SourceType} (value: {SourceTypeValue}), FileUrl={FileUrl}, SourceUrl={SourceUrl}",
                meta.SourceType, (int)meta.SourceType, material.FileUrl, material.SourceUrl);

            _context.StudyMaterials.Add(material);
            await _context.SaveChangesAsync(ct);

            // Process AI review synchronously to avoid DbContext disposal issues
            FileSummaryResult? fileSummary = null;
            try
            {
                // Summarize file if available
                if (file != null)
                {
                    // Create a copy of the file to avoid stream disposal issues
                    using var fileStream = file.OpenReadStream();
                    using var memoryStream = new MemoryStream();
                    await fileStream.CopyToAsync(memoryStream, ct);
                    memoryStream.Position = 0;

                    fileSummary = await _fileSummarizer.SummarizeFileAsync(
                        material.Title,
                        material.Description,
                        file.FileName,
                        file.Length,
                        file.ContentType,
                        memoryStream,
                        ct
                    );
                }

                // Generate optimized prompt for LLM
                var prompt = await _fileSummarizer.GenerateSummarizationPromptAsync(
                    category?.Path ?? "",
                    fileSummary ?? new FileSummaryResult(),
                    ct
                );

                // Get candidate category paths
                var candidateCategoryPaths = await GetCandidateCategoryPathsAsync(ct);

                var aiReview = await _aiReviewer.ReviewAsync(
                    prompt,
                    candidateCategoryPaths,
                    ct
                );

                if (aiReview != null)
                {
                    material.AiConfidence = aiReview.confidence;
                    material.AiCategoryMatch = aiReview.categoryMatch;
                    material.AiReason = aiReview.reason;

                    // Store file summary metadata if available
                    if (fileSummary != null)
                    {
                        material.AiReason = $"File Type: {fileSummary.ContentType}, Quality: {fileSummary.QualityScore:F2}, Topics: {string.Join(", ", fileSummary.KeyTopics.Take(3))}. {aiReview.reason}";
                    }

                    await _context.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI review failed for material {MaterialId}", material.Id);
                // Don't rethrow - the material should still be created even if AI processing fails
            }

            return await MapToResponseAsync(material);
        }

        public async Task<StudyMaterialResponse?> GetByIdAsync(Guid? requesterId, Guid id)
        {
            var material = await _context.StudyMaterials
                .Include(m => m.Uploader)
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null)
                return null;

            if (material.Status != Status.Accepted &&
                (requesterId == null || (requesterId != material.UploaderId && !await IsAdminAsync(requesterId.Value))))
            {
                return null;
            }

            return await MapToResponseAsync(material);
        }

        public async Task<List<StudyMaterialResponse>> SearchAsync(Guid? categoryId, Status? status, string? q)
        {
            var query = _context.StudyMaterials
                .Include(m => m.Uploader)
                .Include(m => m.Category)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }
            else
            {
                query = query.Where(m => m.Status == Status.Accepted);
            }

            if (categoryId.HasValue)
            {
                var childCategoryIds = await GetChildCategoryIdsAsync(categoryId.Value);
                childCategoryIds.Add(categoryId.Value);
                query = query.Where(m => childCategoryIds.Contains(m.CategoryId));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var searchQuery = q.ToLowerInvariant();
                query = query.Where(m =>
                    EF.Functions.ILike(m.Title.ToLower(), $"%{searchQuery}%") ||
                    EF.Functions.ILike(m.Description.ToLower(), $"%{searchQuery}%"));
            }

            var materials = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var responses = new List<StudyMaterialResponse>();
            foreach (var material in materials)
            {
                responses.Add(await MapToResponseAsync(material));
            }

            return responses;
        }

        public async Task<List<StudyMaterialResponse>> GetMineAsync(Guid uploaderId, Status? status)
        {
            var query = _context.StudyMaterials
                .Include(m => m.Uploader)
                .Include(m => m.Category)
                .Where(m => m.UploaderId == uploaderId)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }

            var materials = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var responses = new List<StudyMaterialResponse>();
            foreach (var material in materials)
            {
                responses.Add(await MapToResponseAsync(material));
            }

            return responses;
        }

        public async Task<bool> AdminReviewAsync(Guid adminId, Guid materialId, ReviewDecisionRequest req)
        {
            var material = await _context.StudyMaterials.FindAsync(materialId);
            if (material == null)
                return false;

            material.Status = req.Decision;
            material.RejectReason = req.Decision == Status.Rejected ? req.Reason : null;
            material.ReviewedAt = DateTime.UtcNow;
            material.ReviewedByAdminId = adminId;

            // Handle file based on review decision
            if (material.SourceType == StudyMaterialSourceType.File)
            {
                if (req.Decision == Status.Accepted)
                {
                    // Upload to Cloudflare R2 on acceptance
                    try
                    {
                        var r2Url = await UploadToR2Async(material);
                        if (!string.IsNullOrEmpty(r2Url))
                        {
                            material.FileUrl = r2Url;
                            _logger.LogInformation("Successfully uploaded study material {MaterialId} to R2", materialId);
                        }
                        else
                        {
                            _logger.LogError("Failed to upload study material {MaterialId} to R2 - null URL returned", materialId);
                            material.FileUrl = "Failed to upload to R2";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload study material {MaterialId} to R2", materialId);
                        // Continue with acceptance even if R2 upload fails
                        material.FileUrl = $"Failed to upload to R2: {ex.Message}";
                    }
                }
                else if (req.Decision == Status.Rejected)
                {
                    // Clean up temporary file on rejection
                    CleanupTemporaryFile(material);
                    material.FileUrl = "File deleted due to rejection";
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<StudyMaterialResponse> MapToResponseAsync(StudyMaterial material)
        {
            // Calculate rating statistics
            var ratingStats = await GetRatingStatsAsync(material.Id); 

            return new StudyMaterialResponse(
                material.Id,
                material.CategoryId,
                material.Category?.Path ?? "",
                material.Title,
                material.Description,
                material.SourceType,
                material.FileUrl,
                material.SourceUrl,
                material.Status,
                material.RejectReason,
                material.AiCategoryMatch,
                material.AiConfidence,
                material.AiSuggestedCategoryId,
                material.AiReason,
                material.CreatedAt,
                ratingStats.AverageRating,
                ratingStats.TotalRatings
            );
        }

        private async Task<bool> IsAdminAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role == Role.Admin;
        }

        private async Task<List<Guid>> GetChildCategoryIdsAsync(Guid categoryId)
        {
            var childIds = new List<Guid>();
            var children = await _context.MaterialCategories
                .Where(c => c.ParentId == categoryId)
                .ToListAsync();

            foreach (var child in children)
            {
                childIds.Add(child.Id);
                var grandChildren = await GetChildCategoryIdsAsync(child.Id);
                childIds.AddRange(grandChildren);
            }

            return childIds.Distinct().ToList();
        }

        private async Task<List<string>> GetCandidateCategoryPathsAsync(CancellationToken ct)
        {
            // Get all material category paths as candidates
            var categories = await _context.MaterialCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Level)
                .ThenBy(c => c.SortOrder)
                .Select(c => c.Path)
                .ToListAsync(ct);

            return categories.Distinct().ToList();
        }

        #region Rating System

        public async Task<StudyMaterialRatingStats> GetRatingStatsAsync(Guid materialId)
        {
            var ratings = await _context.StudyMaterialRatings
                .Where(r => r.MaterialId == materialId)
                .ToListAsync();

            if (!ratings.Any())
            {
                return new StudyMaterialRatingStats(0, 0, 0, 0, 0, 0, 0);
            }

            var averageRating = ratings.Average(r => r.Rating);
            var totalRatings = ratings.Count;

            var distribution = ratings.GroupBy(r => r.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            return new StudyMaterialRatingStats(
                Math.Round(averageRating, 2),
                totalRatings,
                distribution.GetValueOrDefault(1, 0),
                distribution.GetValueOrDefault(2, 0),
                distribution.GetValueOrDefault(3, 0),
                distribution.GetValueOrDefault(4, 0),
                distribution.GetValueOrDefault(5, 0)
            );
        }

        public async Task<StudyMaterialRatingResponse?> RateMaterialAsync(Guid userId, Guid materialId, StudyMaterialRatingRequest request)
        {
            // Validate rating
            if (request.Rating < 1 || request.Rating > 5)
            {
                throw new ArgumentException("Rating must be between 1 and 5");
            }

            // Check if material exists and is accepted
            var material = await _context.StudyMaterials.FindAsync(materialId);
            if (material == null || material.Status != Status.Accepted)
            {
                throw new ArgumentException("Material not found or not available for rating");
            }

            // Check if user already rated
            var existingRating = await _context.StudyMaterialRatings
                .FirstOrDefaultAsync(r => r.MaterialId == materialId && r.UserId == userId);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Rating = request.Rating;
                existingRating.Comment = request.Comment;
                existingRating.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new StudyMaterialRatingResponse(
                    existingRating.Id,
                    existingRating.MaterialId,
                    existingRating.UserId,
                    existingRating.User?.Username ?? "",
                    existingRating.Rating,
                    existingRating.Comment,
                    existingRating.CreatedAt,
                    existingRating.UpdatedAt
                );
            }
            else
            {
                // Create new rating
                var rating = new StudyMaterialRating
                {
                    Id = Guid.NewGuid(),
                    MaterialId = materialId,
                    UserId = userId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StudyMaterialRatings.Add(rating);
                await _context.SaveChangesAsync();

                return new StudyMaterialRatingResponse(
                    rating.Id,
                    rating.MaterialId,
                    rating.UserId,
                    rating.User?.Username ?? "",
                    rating.Rating,
                    rating.Comment,
                    rating.CreatedAt,
                    rating.UpdatedAt
                );
            }
        }

        public async Task<List<StudyMaterialRatingResponse>> GetMaterialRatingsAsync(Guid materialId)
        {
            var ratings = await _context.StudyMaterialRatings
                .Include(r => r.User)
                .Where(r => r.MaterialId == materialId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return ratings.Select(r => new StudyMaterialRatingResponse(
                r.Id,
                r.MaterialId,
                r.UserId,
                r.User?.Username ?? "",
                r.Rating,
                r.Comment,
                r.CreatedAt,
                r.UpdatedAt
            )).ToList();
        }

        public async Task<StudyMaterialRatingResponse?> GetUserRatingAsync(Guid userId, Guid materialId)
        {
            var rating = await _context.StudyMaterialRatings
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.MaterialId == materialId && r.UserId == userId);

            if (rating == null) return null;

            return new StudyMaterialRatingResponse(
                rating.Id,
                rating.MaterialId,
                rating.UserId,
                rating.User?.Username ?? "",
                rating.Rating,
                rating.Comment,
                rating.CreatedAt,
                rating.UpdatedAt
            );
        }

        public async Task<bool> DeleteRatingAsync(Guid userId, Guid materialId)
        {
            var rating = await _context.StudyMaterialRatings
                .FirstOrDefaultAsync(r => r.MaterialId == materialId && r.UserId == userId);

            if (rating == null) return false;

            _context.StudyMaterialRatings.Remove(rating);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Cloudflare R2 Integration

        private async Task<string?> UploadToR2Async(StudyMaterial material)
        {
            if (material.SourceType != StudyMaterialSourceType.File || string.IsNullOrEmpty(material.FileUrl))
                return null;

            // Check if FileUrl is a temporary file path
            if (!File.Exists(material.FileUrl))
            {
                _logger.LogError("Temporary file not found: {FilePath}", material.FileUrl);
                return null;
            }

            try
            {
                var fileName = Path.GetFileName(material.FileUrl);
                var key = $"study-materials/{material.Id}/{fileName}";

                using var fileStream = new FileStream(material.FileUrl, FileMode.Open, FileAccess.Read);

                var request = new PutObjectRequest
                {
                    BucketName = _r2.Bucket,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = material.MimeType ?? "application/octet-stream",
                    CannedACL = S3CannedACL.PublicRead
                };

                var response = await _s3.PutObjectAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Return the public URL
                    var publicUrl = $"{_r2.PublicBaseUrl.TrimEnd('/')}/{key}";

                    // Delete temporary file after successful upload
                    try
                    {
                        File.Delete(material.FileUrl);
                        var directory = Path.GetDirectoryName(material.FileUrl);
                        if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                        {
                            Directory.Delete(directory, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file: {FilePath}", material.FileUrl);
                    }

                    return publicUrl;
                }
                else
                {
                    _logger.LogError("Failed to upload to R2. Status code: {StatusCode}", response.HttpStatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to R2");
                return null;
            }
        }

        private void CleanupTemporaryFile(StudyMaterial material)
        {
            if (material.SourceType == StudyMaterialSourceType.File && !string.IsNullOrEmpty(material.FileUrl))
            {
                try
                {
                    if (File.Exists(material.FileUrl))
                    {
                        File.Delete(material.FileUrl);
                        _logger.LogInformation("Deleted temporary file: {FilePath}", material.FileUrl);
                    }

                    var directory = Path.GetDirectoryName(material.FileUrl);
                    if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temporary file: {FilePath}", material.FileUrl);
                }
            }
        }

        #endregion
    }
}