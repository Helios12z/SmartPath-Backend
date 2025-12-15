using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Services
{
    public class StudyMaterialLibraryService : IStudyMaterialLibraryService
    {
        private readonly SmartPathDbContext _context;
        private readonly ILogger<StudyMaterialLibraryService> _logger;
        private readonly IStudyMaterialAiReviewer _aiReviewer;

        public StudyMaterialLibraryService(
            SmartPathDbContext context,
            ILogger<StudyMaterialLibraryService> logger,
            IStudyMaterialAiReviewer aiReviewer)
        {
            _context = context;
            _logger = logger;
            _aiReviewer = aiReviewer;
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
                CreatedAt = DateTime.UtcNow
            };

            if (meta.SourceType == StudyMaterialSourceType.File && file != null)
            {
                material.FileUrl = $"/uploads/materials/{material.Id}/{file.FileName}";
                material.MimeType = file.ContentType;
                material.FileSize = file.Length;
            }
            else if (meta.SourceType == StudyMaterialSourceType.Url && !string.IsNullOrEmpty(meta.SourceUrl))
            {
                material.SourceUrl = meta.SourceUrl;
            }

            _context.StudyMaterials.Add(material);
            await _context.SaveChangesAsync(ct);

            _ = Task.Run(async () =>
            {
                try
                {
                    var aiReview = await _aiReviewer.ReviewAsync(
                        category?.Path ?? "",
                        material.Title,
                        material.Description,
                        null, 
                        new List<string>(), 
                        ct);
                    if (aiReview != null)
                    {
                        material.AiConfidence = aiReview.confidence;
                        material.AiCategoryMatch = aiReview.categoryMatch;
                        material.AiReason = aiReview.reason;

                        await _context.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI review failed for material {MaterialId}", material.Id);
                }
            }, ct);

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

        public async Task<(List<StudyMaterialResponse> Items, int Total)> SearchAsync(Guid? categoryId, Status? status, string? q, int page, int pageSize)
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

            var total = await query.CountAsync();

            var materials = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responses = new List<StudyMaterialResponse>();
            foreach (var material in materials)
            {
                responses.Add(await MapToResponseAsync(material));
            }

            return (responses, total);
        }

        public async Task<(List<StudyMaterialResponse> Items, int Total)> GetMineAsync(Guid uploaderId, Status? status, int page, int pageSize)
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

            var total = await query.CountAsync();

            var materials = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responses = new List<StudyMaterialResponse>();
            foreach (var material in materials)
            {
                responses.Add(await MapToResponseAsync(material));
            }

            return (responses, total);
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

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<StudyMaterialResponse> MapToResponseAsync(StudyMaterial material)
        {
            var reviews = await _context.StudyMaterialReviews
                .Where(r => r.MaterialId == material.Id)
                .ToListAsync();

            var averageRating = 0.0; 

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
                material.CreatedAt
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
    }
}