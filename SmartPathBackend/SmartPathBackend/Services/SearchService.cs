using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;
using SmartPathBackend.Services;
using System.Text.Json;
using Pgvector;

namespace SmartPathBackend.Services
{
    public class SearchService : ISearchService
    {
        private readonly SmartPathDbContext _context;
        private readonly IEmbedderService _embedderService;
        private readonly ILogger<SearchService> _logger;

        public SearchService(
            SmartPathDbContext context,
            IEmbedderService embedderService,
            ILogger<SearchService> logger)
        {
            _context = context;
            _embedderService = embedderService;
            _logger = logger;
        }

        public async Task<SearchResultDTO> SearchAsync(SearchRequestDTO request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var result = new SearchResultDTO();

            try
            {
                // Sequential search to avoid DbContext concurrency issues
                result.Posts = await SearchPostsAsync(request, cancellationToken);
                result.StudyMaterials = await SearchStudyMaterialsAsync(request, cancellationToken);
                result.TotalPosts = result.Posts.Count;
                result.TotalStudyMaterials = result.StudyMaterials.Count;

                // Get facets
                result.Facets = await GetFacetsAsync(request, cancellationToken);

                // Generate suggestions if no results
                if (result.TotalResults == 0 && !string.IsNullOrEmpty(request.Query))
                {
                    result.Suggestions = await GenerateSuggestionsAsync(request.Query, cancellationToken);
                }

                result.QueryTime = DateTime.UtcNow - startTime;

                // Log the search query
                await LogSearchQueryAsync(request, result, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search: {Query}", request.Query);
                throw;
            }

            return result;
        }

        private async Task<List<PostSearchResultDTO>> SearchPostsAsync(SearchRequestDTO request, CancellationToken cancellationToken)
        {
            if (request.SearchType == SearchType.StudyMaterials)
                return new List<PostSearchResultDTO>();

            var query = _context.PostSearchIndices.AsQueryable();

            // Debug: Log total PostSearchIndex count
            var totalCount = await _context.PostSearchIndices.CountAsync(cancellationToken);
            _logger.LogInformation("Total PostSearchIndex records: {Count}", totalCount);

            // Apply filters - temporarily disabled category filter due to EF Core translation issues
            // TODO: Fix category filtering later by avoiding JSON deserialization in queries
            // if (request.CategoryIds.Any())
            // {
            //     query = query.Where(p => p.CategoryIdList.Any(id => request.CategoryIds.Contains(id)));
            // }

            if (request.IsQuestion.HasValue)
            {
                query = query.Where(p => p.IsQuestion == request.IsQuestion.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= request.ToDate.Value);
            }

            // Simplified - removed tag filtering to avoid EF Core translation issues

            var results = new List<PostSearchResultDTO>();

            // Keyword search
            if (request.IncludeKeywordSearch && !string.IsNullOrEmpty(request.Query))
            {
                var keywordResults = await KeywordSearchPostsAsync(query, request.Query, cancellationToken);
                results.AddRange(keywordResults);
            }
            else
            {
                // Get all matching posts without text search
                var matchingPosts = await query
                    .Select(p => new PostSearchResultDTO
                    {
                        Id = p.PostId,
                        Title = p.Title,
                        Content = p.Content,
                        Summary = p.Summary,
                        IsQuestion = p.IsQuestion,
                        IsSolved = p.IsSolved,
                        ViewCount = p.ViewCount,
                        LikeCount = p.LikeCount,
                        CommentCount = p.CommentCount,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        Author = new AuthorDTO
                        {
                            Id = p.AuthorId,
                            Username = p.AuthorUsername,
                            DisplayName = p.AuthorName,
                            Avatar = p.AuthorAvatar
                        },
                        RelevanceScore = 1.0f,
                        MatchType = SearchMatchType.Keyword
                    })
                    .ToListAsync(cancellationToken);

                results.AddRange(matchingPosts);
            }

            // Semantic search
            if (request.IncludeSemanticSearch && !string.IsNullOrEmpty(request.Query))
            {
                var semanticResults = await SemanticSearchPostsAsync(query, request.Query, cancellationToken);

                // Merge semantic results with keyword results, avoiding duplicates
                var existingIds = results.Select(r => r.Id).ToHashSet();
                var newSemanticResults = semanticResults.Where(r => !existingIds.Contains(r.Id)).ToList();
                results.AddRange(newSemanticResults);
            }

            // Sort results
            results = ApplySorting(results, request.SortBy, request.SortOrder);

            // Pagination
            var skip = (request.Page - 1) * request.PageSize;
            return results.Skip(skip).Take(request.PageSize).ToList();
        }

        private async Task<List<StudyMaterialSearchResultDTO>> SearchStudyMaterialsAsync(SearchRequestDTO request, CancellationToken cancellationToken)
        {
            if (request.SearchType == SearchType.Posts)
                return new List<StudyMaterialSearchResultDTO>();

            var query = _context.StudyMaterialSearchIndices.AsQueryable();

            // Only show approved materials
            query = query.Where(m => m.IsApproved);

            // Apply filters
            if (request.MaterialCategoryIds.Any())
            {
                query = query.Where(m => request.MaterialCategoryIds.Contains(m.CategoryId));
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= request.ToDate.Value);
            }

            // Simplified - removed tag filtering to avoid EF Core translation issues

            var results = new List<StudyMaterialSearchResultDTO>();

            // Keyword search
            if (request.IncludeKeywordSearch && !string.IsNullOrEmpty(request.Query))
            {
                var keywordResults = await KeywordSearchMaterialsAsync(query, request.Query, cancellationToken);
                results.AddRange(keywordResults);
            }
            else
            {
                // Get all matching materials without text search
                var matchingMaterials = await query
                    .Select(m => new StudyMaterialSearchResultDTO
                    {
                        Id = m.StudyMaterialId,
                        Title = m.Title,
                        Description = m.Description,
                        Summary = m.Summary,
                        Type = m.ResourceType.ToString(),
                        Url = m.Url,
                        DownloadUrl = m.DownloadUrl,
                        ViewCount = m.ViewCount,
                        DownloadCount = m.DownloadCount,
                        AverageRating = m.AverageRating,
                        ReviewCount = m.ReviewCount,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt,
                        Uploader = new AuthorDTO
                        {
                            Id = m.UploaderId,
                            Username = m.UploaderUsername,
                            DisplayName = m.UploaderName,
                            Avatar = m.UploaderAvatar
                        },
                        Category = new MaterialCategoryDTO
                        {
                            Id = m.CategoryId,
                            Name = m.CategoryName,
                            Path = m.CategoryPath
                        },
                        Tags = m.TagList,
                        RelevanceScore = 1.0f,
                        MatchType = SearchMatchType.Keyword,
                        IsApproved = m.IsApproved,
                        AiConfidence = m.AiConfidence
                    })
                    .ToListAsync(cancellationToken);

                results.AddRange(matchingMaterials);
            }

            // Semantic search
            if (request.IncludeSemanticSearch && !string.IsNullOrEmpty(request.Query))
            {
                var semanticResults = await SemanticSearchMaterialsAsync(query, request.Query, cancellationToken);

                // Merge semantic results with keyword results, avoiding duplicates
                var existingIds = results.Select(r => r.Id).ToHashSet();
                var newSemanticResults = semanticResults.Where(r => !existingIds.Contains(r.Id)).ToList();
                results.AddRange(newSemanticResults);
            }

            // Sort results
            results = ApplyMaterialSorting(results, request.SortBy, request.SortOrder);

            // Pagination
            var skip = (request.Page - 1) * request.PageSize;
            return results.Skip(skip).Take(request.PageSize).ToList();
        }

        private static string EscapeLike(string input)
        {
            // Escape ký tự đặc biệt của LIKE: %, _, \
            // Dùng "\" làm escape char
            return (input ?? "")
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_");
        }

        private async Task<List<PostSearchResultDTO>> KeywordSearchPostsAsync(
            IQueryable<PostSearchIndex> query,
            string searchQuery,
            CancellationToken ct)
        {
            var terms = (searchQuery ?? "")
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _logger.LogInformation("Keyword search - Query: '{Query}', Terms: [{Terms}]", searchQuery, string.Join(", ", terms));

            if (terms.Length == 0) return new();

            // IMPORTANT:
            // Đảm bảo query ở đây CHƯA Skip/Take từ bên ngoài.
            // Paginate phải làm sau bước keyword search + sort.

            // Simple keyword search - find posts that contain ANY of the search terms
            IQueryable<PostSearchIndex> filtered = query.Where(p => false);

            foreach (var term in terms)
            {
                var escapedTerm = EscapeLike(term);
                var termMatches = query.Where(p =>
                    EF.Functions.ILike(p.Title, $"%{escapedTerm}%") ||
                    EF.Functions.ILike(p.Content, $"%{escapedTerm}%")
                );
                filtered = filtered.Concat(termMatches);
            }

            // Load và loại trùng theo PostId
            var rows = await filtered
                .AsNoTracking()
                .ToListAsync(ct);

            var uniqueRows = rows
                .DistinctBy(x => x.PostId)
                .ToList();

            _logger.LogInformation("Keyword search - Found {TotalRows} rows, {UniqueRows} unique results", rows.Count, uniqueRows.Count);

            var searchTermsLower = terms.Select(x => x.ToLowerInvariant()).ToArray();

            // Score + map
            var results = uniqueRows
                .Select(p => new
                {
                    p,
                    TitleScore = CalculateTextScore((p.Title ?? "").ToLowerInvariant(), searchTermsLower),
                    ContentScore = CalculateTextScore((p.Content ?? "").ToLowerInvariant(), searchTermsLower)
                })
                .ToList();

            return results.Select(r => new PostSearchResultDTO
            {
                Id = r.p.PostId,
                Title = r.p.Title,
                Content = r.p.Content,
                Summary = r.p.Summary,
                IsQuestion = r.p.IsQuestion,
                IsSolved = r.p.IsSolved,
                ViewCount = r.p.ViewCount,
                LikeCount = r.p.LikeCount,
                CommentCount = r.p.CommentCount,
                CreatedAt = r.p.CreatedAt,
                UpdatedAt = r.p.UpdatedAt,
                Author = new AuthorDTO
                {
                    Id = r.p.AuthorId,
                    Username = r.p.AuthorUsername,
                    DisplayName = r.p.AuthorName,
                    Avatar = r.p.AuthorAvatar
                },
                RelevanceScore = Math.Max(r.TitleScore, r.ContentScore),
                MatchType = SearchMatchType.Keyword,
                HighlightedTitle = HighlightText(r.p.Title, searchTermsLower),
                HighlightedContent = HighlightText(r.p.Content, searchTermsLower.Take(5).ToArray())
            }).ToList();
        }

        private async Task<List<StudyMaterialSearchResultDTO>> KeywordSearchMaterialsAsync(
            IQueryable<StudyMaterialSearchIndex> query,
            string searchQuery,
            CancellationToken ct)
        {
            var terms = (searchQuery ?? "")
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (terms.Length == 0) return new();

            // OR semantics: union theo từng term
            IQueryable<StudyMaterialSearchIndex> filtered = query.Where(_ => false);

            foreach (var t in terms)
            {
                var pat = $"%{t}%";

                filtered = filtered.Union(query.Where(m =>
                    EF.Functions.ILike(m.Title, pat) ||
                    EF.Functions.ILike(m.Description ?? "", pat) ||
                    EF.Functions.ILike(m.Summary ?? "", pat)
                ));
            }

            // lấy data về
            var rows = await filtered
                .AsNoTracking()
                .ToListAsync(ct);

            // tránh trùng do UNION nhiều term (nếu index table có 1 row/material thì GroupBy vẫn ok)
            var unique = rows
                .GroupBy(x => x.StudyMaterialId)
                .Select(g => g.First())
                .ToList();

            // scoring + highlight (làm in-memory)
            var lowerTerms = terms.Select(x => x.ToLowerInvariant()).ToArray();

            var scored = unique.Select(m => new
            {
                m,
                TitleScore = CalculateTextScore((m.Title ?? "").ToLowerInvariant(), lowerTerms),
                DescriptionScore = CalculateTextScore((m.Description ?? "").ToLowerInvariant(), lowerTerms),
                SummaryScore = CalculateTextScore((m.Summary ?? "").ToLowerInvariant(), lowerTerms)
            }).ToList();

            return scored.Select(r => new StudyMaterialSearchResultDTO
            {
                Id = r.m.StudyMaterialId,
                Title = r.m.Title,
                Description = r.m.Description,
                Summary = r.m.Summary,
                Type = r.m.ResourceType.ToString(),
                Url = r.m.Url,
                DownloadUrl = r.m.DownloadUrl,
                ViewCount = r.m.ViewCount,
                DownloadCount = r.m.DownloadCount,
                AverageRating = r.m.AverageRating,
                ReviewCount = r.m.ReviewCount,
                CreatedAt = r.m.CreatedAt,
                UpdatedAt = r.m.UpdatedAt,
                Uploader = new AuthorDTO
                {
                    Id = r.m.UploaderId,
                    Username = r.m.UploaderUsername,
                    DisplayName = r.m.UploaderName,
                    Avatar = r.m.UploaderAvatar
                },
                Category = new MaterialCategoryDTO
                {
                    Id = r.m.CategoryId,
                    Name = r.m.CategoryName,
                    Path = r.m.CategoryPath
                },
                Tags = r.m.TagList,
                RelevanceScore = Math.Max(Math.Max(r.TitleScore, r.DescriptionScore), r.SummaryScore),
                MatchType = SearchMatchType.Keyword,
                HighlightedTitle = HighlightText(r.m.Title ?? "", lowerTerms),
                HighlightedDescription = HighlightText(r.m.Description ?? "", lowerTerms.Take(5).ToArray()),
                IsApproved = r.m.IsApproved,
                AiConfidence = r.m.AiConfidence
            }).ToList();
        }

        private async Task<List<PostSearchResultDTO>> SemanticSearchPostsAsync(IQueryable<PostSearchIndex> query, string searchQuery, CancellationToken cancellationToken)
        {
            try
            {
                // Generate embedding for the search query
                var queryEmbeddingArray = await _embedderService.EmbedOneAsync(searchQuery);
                if (queryEmbeddingArray == null || queryEmbeddingArray.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for query: {Query}", searchQuery);
                    return new List<PostSearchResultDTO>();
                }

                var queryEmbedding = new Vector(queryEmbeddingArray);

                // Enhanced semantic search using embedding-based approach
                var searchLower = searchQuery.ToLowerInvariant();

                // Get all posts with embeddings first, then perform semantic matching in memory
                var candidates = await query
                    .Where(p => p.Embedding != null)
                    .Take(100) // Get top 100 candidates to avoid memory issues
                    .Select(p => new
                    {
                        p.PostId,
                        p.Title,
                        p.Content,
                        p.Summary,
                        p.IsQuestion,
                        p.IsSolved,
                        p.ViewCount,
                        p.LikeCount,
                        p.CommentCount,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.AuthorId,
                        p.AuthorName,
                        p.AuthorUsername,
                        p.AuthorAvatar,
                        p.CategoryIds,
                        p.CategoryNames,
                        p.CategorySlugs,
                        p.Tags,
                        Embedding = p.Embedding!
                    })
                    .ToListAsync(cancellationToken);

                // Calculate semantic similarity scores using actual embeddings
                var results = candidates
                    .Select(p => new
                    {
                        Post = p,
                        SimilarityScore = CalculateVectorSimilarity(queryEmbeddingArray, p.Embedding),
                        TextSimilarity = CalculateTextSemanticSimilarity(searchLower,
                            p.Title.ToLowerInvariant() + " " +
                            p.Content.ToLowerInvariant() + " " +
                            (p.Summary ?? "").ToLowerInvariant())
                    })
                    .Select(x => new
                    {
                        x.Post,
                        // Combine vector similarity and text similarity
                        CombinedScore = (x.SimilarityScore * 0.7) + (x.TextSimilarity * 0.3)
                    })
                    .Where(x => x.CombinedScore > 0.1) // Filter out very low similarity
                    .OrderByDescending(x => x.CombinedScore)
                    .ThenByDescending(x => x.Post.LikeCount)
                    .Take(20)
                    .Select(x => new PostSearchResultDTO
                    {
                        Id = x.Post.PostId,
                        Title = x.Post.Title,
                        Content = x.Post.Content,
                        Summary = x.Post.Summary,
                        IsQuestion = x.Post.IsQuestion,
                        IsSolved = x.Post.IsSolved,
                        ViewCount = x.Post.ViewCount,
                        LikeCount = x.Post.LikeCount,
                        CommentCount = x.Post.CommentCount,
                        CreatedAt = x.Post.CreatedAt,
                        UpdatedAt = x.Post.UpdatedAt,
                        Author = new AuthorDTO
                        {
                            Id = x.Post.AuthorId,
                            Username = x.Post.AuthorUsername,
                            DisplayName = x.Post.AuthorName,
                            Avatar = x.Post.AuthorAvatar
                        },
                        // Simplified - removing Categories and Tags to avoid EF Core translation issues
                        RelevanceScore = (float)x.CombinedScore,
                        MatchType = SearchMatchType.Semantic
                    })
                    .ToList();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during semantic search for posts: {Query}", searchQuery);
                return new List<PostSearchResultDTO>();
            }
        }

        private async Task<List<StudyMaterialSearchResultDTO>> SemanticSearchMaterialsAsync(IQueryable<StudyMaterialSearchIndex> query, string searchQuery, CancellationToken cancellationToken)
        {
            try
            {
                // Generate embedding for the search query
                var queryEmbeddingArray = await _embedderService.EmbedOneAsync(searchQuery);
                if (queryEmbeddingArray == null || queryEmbeddingArray.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for query: {Query}", searchQuery);
                    return new List<StudyMaterialSearchResultDTO>();
                }

                var queryEmbedding = new Vector(queryEmbeddingArray);

                // Enhanced semantic search using embedding-based approach
                var searchLower = searchQuery.ToLowerInvariant();

                // Get all materials with embeddings first, then perform semantic matching in memory
                var candidates = await query
                    .Where(m => m.Embedding != null)
                    .Take(100) // Get top 100 candidates to avoid memory issues
                    .Select(m => new
                    {
                        m.StudyMaterialId,
                        m.Title,
                        m.Description,
                        m.Summary,
                        m.ResourceType,
                        m.Url,
                        m.DownloadUrl,
                        m.ViewCount,
                        m.DownloadCount,
                        m.AverageRating,
                        m.ReviewCount,
                        m.CreatedAt,
                        m.UpdatedAt,
                        m.UploaderId,
                        m.UploaderName,
                        m.UploaderUsername,
                        m.UploaderAvatar,
                        m.CategoryId,
                        m.CategoryName,
                        m.CategoryPath,
                        m.Tags,
                        m.IsApproved,
                        m.AiConfidence,
                        Embedding = m.Embedding!
                    })
                    .ToListAsync(cancellationToken);

                // Calculate semantic similarity scores using actual embeddings
                var results = candidates
                    .Select(m => new
                    {
                        Material = m,
                        SimilarityScore = CalculateVectorSimilarity(queryEmbeddingArray, m.Embedding),
                        TextSimilarity = CalculateTextSemanticSimilarity(searchLower,
                            m.Title.ToLowerInvariant() + " " +
                            m.Description.ToLowerInvariant() + " " +
                            m.Summary.ToLowerInvariant())
                    })
                    .Select(x => new
                    {
                        x.Material,
                        // Combine vector similarity and text similarity
                        CombinedScore = (x.SimilarityScore * 0.7) + (x.TextSimilarity * 0.3)
                    })
                    .Where(x => x.CombinedScore > 0.1) // Filter out very low similarity
                    .OrderByDescending(x => x.CombinedScore)
                    .ThenByDescending(x => x.Material.AverageRating)
                    .Take(20)
                    .Select(m => new StudyMaterialSearchResultDTO
                    {
                        Id = m.Material.StudyMaterialId,
                        Title = m.Material.Title,
                        Description = m.Material.Description,
                        Summary = m.Material.Summary,
                        Type = m.Material.ResourceType.ToString(),
                        Url = m.Material.Url,
                        DownloadUrl = m.Material.DownloadUrl,
                        ViewCount = m.Material.ViewCount,
                        DownloadCount = m.Material.DownloadCount,
                        AverageRating = m.Material.AverageRating,
                        ReviewCount = m.Material.ReviewCount,
                        CreatedAt = m.Material.CreatedAt,
                        UpdatedAt = m.Material.UpdatedAt,
                        Uploader = new AuthorDTO
                        {
                            Id = m.Material.UploaderId,
                            Username = m.Material.UploaderUsername,
                            DisplayName = m.Material.UploaderName,
                            Avatar = m.Material.UploaderAvatar
                        },
                        Category = new MaterialCategoryDTO
                        {
                            Id = m.Material.CategoryId,
                            Name = m.Material.CategoryName,
                            Path = m.Material.CategoryPath
                        },
                        Tags = ParseJsonList<string>(m.Material.Tags),
                        RelevanceScore = (float)m.CombinedScore,
                        MatchType = SearchMatchType.Semantic,
                        IsApproved = m.Material.IsApproved,
                        AiConfidence = m.Material.AiConfidence
                    })
                    .ToList();

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during semantic search for materials: {Query}", searchQuery);
                return new List<StudyMaterialSearchResultDTO>();
            }
        }

        private async Task<SearchFacetsDTO> GetFacetsAsync(SearchRequestDTO request, CancellationToken cancellationToken)
        {
            var facets = new SearchFacetsDTO();

            // Get category facets for posts - temporarily disabled due to EF Core translation issues
            if (request.SearchType != SearchType.StudyMaterials)
            {
                // TODO: Fix category facets later by avoiding Zip operations on JSON properties
                facets.Categories = new List<CategoryFacetDTO>();
            }

            // Get material category facets
            if (request.SearchType != SearchType.Posts)
            {
                var materialCategoryFacets = await _context.StudyMaterialSearchIndices
                    .Where(m => m.IsApproved &&
                               (string.IsNullOrEmpty(request.Query) ||
                                EF.Functions.ILike(m.Title, $"%{request.Query}%") ||
                                EF.Functions.ILike(m.Description, $"%{request.Query}%")))
                    .GroupBy(m => new { m.CategoryId, m.CategoryName, m.CategoryPath, m.CategoryLevel })
                    .Select(g => new MaterialCategoryFacetDTO
                    {
                        Id = g.Key.CategoryId,
                        Name = g.Key.CategoryName,
                        Path = g.Key.CategoryPath,
                        Level = g.Key.CategoryLevel,
                        Count = g.Count()
                    })
                    .OrderByDescending(c => c.Count)
                    .Take(10)
                    .ToListAsync(cancellationToken);

                facets.MaterialCategories = materialCategoryFacets;
            }

            return facets;
        }

        private async Task<SearchSuggestionDTO> GenerateSuggestionsAsync(string query, CancellationToken cancellationToken)
        {
            var suggestions = new SearchSuggestionDTO();

            // Simple spelling correction (you can integrate with a proper spell checker)
            var commonMisspellings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["javascript"] = "JavaScript",
                ["typescript"] = "TypeScript",
                ["reactjs"] = "React",
                ["nodejs"] = "Node.js",
                ["python"] = "Python",
                ["mysql"] = "MySQL",
                ["postgresql"] = "PostgreSQL"
            };

            var correctedQuery = query.ToLowerInvariant();
            foreach (var (wrong, correct) in commonMisspellings)
            {
                if (correctedQuery.Contains(wrong))
                {
                    suggestions.CorrectedQuery = correctedQuery.Replace(wrong, correct);
                    break;
                }
            }

            // Get related queries from search logs
            var relatedQueries = await _context.SearchQueryLogs
                .Where(l => l.Query.ToLower().Contains(query.ToLower()) &&
                           l.ResultCount > 0)
                .GroupBy(l => l.Query)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            suggestions.RelatedQueries = relatedQueries.Where(q => q != query).ToList();

            return suggestions;
        }

        private async Task LogSearchQueryAsync(SearchRequestDTO request, SearchResultDTO result, CancellationToken cancellationToken)
        {
            try
            {
                var log = new SearchQueryLog
                {
                    Query = request.Query,
                    NormalizedQuery = request.Query.ToLowerInvariant().Trim(),
                    SearchType = request.SearchType.ToString(),
                    Filters = JsonSerializer.Serialize(new
                    {
                        request.CategoryIds,
                        request.MaterialCategoryIds,
                        request.IsQuestion,
                        request.FromDate,
                        request.ToDate,
                        request.Tags
                    }),
                    ResultCount = result.TotalResults,
                    PostResults = result.TotalPosts,
                    StudyMaterialResults = result.TotalStudyMaterials,
                    QueryTime = result.QueryTime,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    SortOrder = request.SortOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SearchQueryLogs.Add(log);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging search query: {Query}", request.Query);
            }
        }

        private static float CalculateTextScore(string text, string[] searchTerms)
        {
            if (string.IsNullOrEmpty(text) || searchTerms.Length == 0)
                return 0f;

            var score = 0f;
            var textLower = text.ToLowerInvariant();
            var words = textLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var term in searchTerms)
            {
                // Exact phrase match gets highest score
                if (textLower.Contains(term))
                {
                    score += 2f;
                }

                // Individual word matches
                foreach (var word in words)
                {
                    if (word.Equals(term, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 1f;
                    }
                    else if (word.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.5f;
                    }
                }
            }

            // Normalize by text length to prefer concise matches
            return score / (words.Length * 0.1f + 1f);
        }

        private static List<string> HighlightText(string text, string[] searchTerms)
        {
            var highlights = new List<string>();
            var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries);

            foreach (var sentence in sentences.Take(3)) // Take first 3 sentences
            {
                var trimmedSentence = sentence.Trim();
                var sentenceLower = trimmedSentence.ToLowerInvariant();

                if (searchTerms.Any(term => sentenceLower.Contains(term)))
                {
                    highlights.Add(trimmedSentence.Trim() + ".");
                }
            }

            return highlights;
        }

        private static List<PostSearchResultDTO> ApplySorting(List<PostSearchResultDTO> results, string sortBy, string sortOrder)
        {
            var ascending = sortOrder.ToLowerInvariant() == "asc";

            return sortBy.ToLowerInvariant() switch
            {
                "created" => ascending
                    ? results.OrderBy(r => r.CreatedAt).ToList()
                    : results.OrderByDescending(r => r.CreatedAt).ToList(),
                "updated" => ascending
                    ? results.OrderBy(r => r.UpdatedAt).ToList()
                    : results.OrderByDescending(r => r.UpdatedAt).ToList(),
                "views" => ascending
                    ? results.OrderBy(r => r.ViewCount).ToList()
                    : results.OrderByDescending(r => r.ViewCount).ToList(),
                "likes" => ascending
                    ? results.OrderBy(r => r.LikeCount).ToList()
                    : results.OrderByDescending(r => r.LikeCount).ToList(),
                _ => results.OrderByDescending(r => r.RelevanceScore).ThenByDescending(r => r.CreatedAt).ToList()
            };
        }

        private static List<StudyMaterialSearchResultDTO> ApplyMaterialSorting(List<StudyMaterialSearchResultDTO> results, string sortBy, string sortOrder)
        {
            var ascending = sortOrder.ToLowerInvariant() == "asc";

            return sortBy.ToLowerInvariant() switch
            {
                "created" => ascending
                    ? results.OrderBy(r => r.CreatedAt).ToList()
                    : results.OrderByDescending(r => r.CreatedAt).ToList(),
                "updated" => ascending
                    ? results.OrderBy(r => r.UpdatedAt).ToList()
                    : results.OrderByDescending(r => r.UpdatedAt).ToList(),
                "views" => ascending
                    ? results.OrderBy(r => r.ViewCount).ToList()
                    : results.OrderByDescending(r => r.ViewCount).ToList(),
                "downloads" => ascending
                    ? results.OrderBy(r => r.DownloadCount).ToList()
                    : results.OrderByDescending(r => r.DownloadCount).ToList(),
                "rating" => ascending
                    ? results.OrderBy(r => r.AverageRating).ToList()
                    : results.OrderByDescending(r => r.AverageRating).ToList(),
                _ => results.OrderByDescending(r => r.RelevanceScore).ThenByDescending(r => r.CreatedAt).ToList()
            };
        }

        public async Task<List<PostSuggestionDTO>> GetPostSuggestionsAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
                return new List<PostSuggestionDTO>();

            var normalizedQuery = query.ToLowerInvariant();

            return await _context.PostSearchIndices
                .Where(p => EF.Functions.ILike(p.Title, $"%{normalizedQuery}%"))
                .OrderBy(p => p.Title.ToLower().StartsWith(normalizedQuery) ? 0 : 1)
                .ThenByDescending(p => p.ViewCount)
                .Take(limit)
                .Select(p => new PostSuggestionDTO
                {
                    Id = p.PostId,
                    Title = p.Title,
                    IsQuestion = p.IsQuestion
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<StudyMaterialSuggestionDTO>> GetStudyMaterialSuggestionsAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
                return new List<StudyMaterialSuggestionDTO>();

            var normalizedQuery = query.ToLowerInvariant();

            return await _context.StudyMaterialSearchIndices
                .Where(m => m.IsApproved && EF.Functions.ILike(m.Title, $"%{normalizedQuery}%"))
                .OrderBy(m => m.Title.ToLower().StartsWith(normalizedQuery) ? 0 : 1)
                .ThenByDescending(m => m.ViewCount)
                .Take(limit)
                .Select(m => new StudyMaterialSuggestionDTO
                {
                    Id = m.StudyMaterialId,
                    Title = m.Title,
                    Type = m.ResourceType.ToString(),
                    Category = m.CategoryName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ReindexPostAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.Author)
                    .Include(p => p.CategoryPosts)
                        .ThenInclude(cp => cp.Category)
                    .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

                if (post == null)
                    return false;

                // Generate embedding
                var textToEmbed = $"{post.Title}\n{post.Content}";
                var embedding = await _embedderService.EmbedOneAsync(textToEmbed);

                var searchIndex = await _context.PostSearchIndices
                    .FirstOrDefaultAsync(si => si.PostId == postId, cancellationToken);

                if (searchIndex == null)
                {
                    searchIndex = new PostSearchIndex { PostId = postId };
                    _context.PostSearchIndices.Add(searchIndex);
                }

                // Update search index
                searchIndex.Title = post.Title;
                searchIndex.Content = post.Content;
                searchIndex.Summary = post.Content.Length > 500 ? post.Content[..500] + "..." : post.Content;
                searchIndex.IsQuestion = post.IsQuestion;
                searchIndex.IsSolved = false; // Post entity doesn't have IsSolved property
                searchIndex.ViewCount = 0; // Post entity doesn't have ViewCount property
                searchIndex.LikeCount = post.Reactions?.Count(r => r.IsPositive) ?? 0; // Calculate from reactions
                searchIndex.CommentCount = post.Comments?.Count ?? 0; // Calculate from comments
                searchIndex.CreatedAt = post.CreatedAt;
                searchIndex.UpdatedAt = post.UpdatedAt ?? DateTime.UtcNow;
                searchIndex.AuthorId = post.AuthorId;
                searchIndex.AuthorName = post.Author?.FullName ?? post.Author?.Username ?? "";
                searchIndex.AuthorUsername = post.Author?.Username ?? "";
                searchIndex.AuthorAvatar = post.Author?.AvatarUrl ?? "";

                var categoryIds = post.CategoryPosts.Select(cp => cp.CategoryId).ToList();
                var categoryNames = post.CategoryPosts.Select(cp => cp.Category?.Name ?? "").ToList();
                var categorySlugs = post.CategoryPosts.Select(cp => cp.Category?.Name?.ToLower().Replace(" ", "-") ?? "").ToList();

                searchIndex.CategoryIds = JsonSerializer.Serialize(categoryIds);
                searchIndex.CategoryNames = JsonSerializer.Serialize(categoryNames);
                searchIndex.CategorySlugs = JsonSerializer.Serialize(categorySlugs);

                // Extract tags from content (simple implementation)
                var tags = ExtractTagsFromContent(post.Content);
                searchIndex.Tags = JsonSerializer.Serialize(tags);

                searchIndex.Embedding = embedding != null ? new Vector(embedding) : null;
                searchIndex.LastIndexedAt = DateTime.UtcNow;
                searchIndex.Version++;

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reindexing post {PostId}", postId);
                return false;
            }
        }

        public async Task<bool> ReindexStudyMaterialAsync(Guid materialId, CancellationToken cancellationToken = default)
        {
            try
            {
                var material = await _context.StudyMaterials
                    .Include(m => m.Uploader)
                    .Include(m => m.Category)
                    .FirstOrDefaultAsync(m => m.Id == materialId, cancellationToken);

                if (material == null)
                    return false;

                // Generate embedding
                var textToEmbed = $"{material.Title}\n{material.Description ?? ""}";
                var embedding = await _embedderService.EmbedOneAsync(textToEmbed);

                var searchIndex = await _context.StudyMaterialSearchIndices
                    .FirstOrDefaultAsync(si => si.StudyMaterialId == materialId, cancellationToken);

                if (searchIndex == null)
                {
                    searchIndex = new StudyMaterialSearchIndex { StudyMaterialId = materialId };
                    _context.StudyMaterialSearchIndices.Add(searchIndex);
                }

                // Update search index
                searchIndex.Title = material.Title;
                searchIndex.Description = material.Description ?? "";
                searchIndex.Summary = material.Description?.Length > 500 ? material.Description[..500] + "..." : (material.Description ?? "");
                searchIndex.ResourceType = StudyMaterialResourceType.Other; // Default resource type
                searchIndex.Url = material.FileUrl ?? material.SourceUrl ?? "";
                searchIndex.DownloadUrl = material.FileUrl ?? "";
                searchIndex.ViewCount = 0; // Would need to be tracked separately
                searchIndex.DownloadCount = 0; // Would need to be tracked separately
                searchIndex.AverageRating = 0; // Would need to be calculated from reviews
                searchIndex.ReviewCount = 0; // Would need to be counted from reviews
                searchIndex.CreatedAt = material.CreatedAt;
                searchIndex.UpdatedAt = material.ReviewedAt ?? DateTime.UtcNow; // Use ReviewedAt as fallback
                searchIndex.UploaderId = material.UploaderId;
                searchIndex.UploaderName = material.Uploader?.FullName ?? material.Uploader?.Username ?? "";
                searchIndex.UploaderUsername = material.Uploader?.Username ?? "";
                searchIndex.UploaderAvatar = material.Uploader?.AvatarUrl ?? "";
                searchIndex.CategoryId = material.CategoryId;
                searchIndex.CategoryName = material.Category?.Name ?? "";
                searchIndex.CategoryPath = material.Category?.Path ?? "";
                searchIndex.CategoryLevel = material.Category?.Level ?? 0;

                // Extract tags from content (simple implementation)
                var tags = ExtractTagsFromContent(material.Description ?? "");
                searchIndex.Tags = JsonSerializer.Serialize(tags);

                searchIndex.IsApproved = material.Status == Status.Accepted;
                searchIndex.AiConfidence = (float)(material.AiConfidence ?? 0);
                searchIndex.AiReason = material.AiReason;

                searchIndex.Embedding = embedding != null ? new Vector(embedding) : null;
                searchIndex.LastIndexedAt = DateTime.UtcNow;
                searchIndex.Version++;

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reindexing study material {MaterialId}", materialId);
                return false;
            }
        }

        public async Task<SearchAnalyticsDTO> GetSearchAnalyticsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        {
            var fromDate = from.ToDateTime(TimeOnly.MinValue);
            var toDate = to.ToDateTime(TimeOnly.MaxValue);

            var logs = await _context.SearchQueryLogs
                .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate)
                .ToListAsync(cancellationToken);

            return new SearchAnalyticsDTO
            {
                TotalQueries = logs.Count,
                UniqueQueries = logs.Select(l => l.Query.ToLowerInvariant()).Distinct().Count(),
                TopQueries = logs.GroupBy(l => l.Query.ToLowerInvariant())
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => g.Key)
                    .ToList(),
                AverageQueryLength = logs.Any() ? logs.Average(l => l.Query.Length) : 0,
                QueryLengthDistribution = new List<int>(), // Would need more complex calculation
                TopResultTypes = new List<SearchResultTypeDTO>(), // Would need more complex calculation
                AverageResultsPerPage = logs.Any() ? logs.Average(l => l.ResultCount) : 0
            };
        }

        private static List<string> ExtractTagsFromContent(string content)
        {
            var tags = new List<string>();

            // Simple tag extraction - look for common programming languages and technologies
            var commonTags = new[]
            {
                "javascript", "typescript", "react", "vue", "angular", "nodejs", "python", "java", "c#",
                "dotnet", "mysql", "postgresql", "mongodb", "docker", "kubernetes", "aws", "azure",
                "git", "github", "html", "css", "sass", "webpack", "rest", "api", "graphql",
                "sql", "nosql", "database", "frontend", "backend", "fullstack", "devops",
                "testing", "unit-test", "integration-test", "e2e", "ci", "cd"
            };

            var contentLower = content.ToLowerInvariant();

            foreach (var tag in commonTags)
            {
                if (contentLower.Contains(tag))
                {
                    tags.Add(tag);
                }
            }

            return tags.Distinct().ToList();
        }

        private List<T> ParseJsonList<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<T>();

            try
            {
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        private double CalculateTextSemanticSimilarity(string query, string content)
        {
            // Simple text similarity based on Jaccard similarity
            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var contentWords = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            if (queryWords.Count == 0 || contentWords.Count == 0)
                return 0.0;

            var intersection = queryWords.Intersect(contentWords).Count();
            var union = queryWords.Union(contentWords).Count();

            return intersection / (double)union;
        }

        private double CalculateVectorSimilarity(float[] queryEmbedding, Vector documentEmbedding)
        {
            if (queryEmbedding == null || documentEmbedding == null)
                return 0.0;

            // Convert Vector to float array
            var docEmbedding = documentEmbedding.ToArray();

            if (queryEmbedding.Length != docEmbedding.Length)
                return 0.0;

            // Calculate cosine similarity
            double dotProduct = 0.0;
            double queryMagnitude = 0.0;
            double docMagnitude = 0.0;

            for (int i = 0; i < queryEmbedding.Length; i++)
            {
                dotProduct += queryEmbedding[i] * docEmbedding[i];
                queryMagnitude += queryEmbedding[i] * queryEmbedding[i];
                docMagnitude += docEmbedding[i] * docEmbedding[i];
            }

            if (queryMagnitude == 0 || docMagnitude == 0)
                return 0.0;

            return dotProduct / (Math.Sqrt(queryMagnitude) * Math.Sqrt(docMagnitude));
        }
    }
}