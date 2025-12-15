using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost("search")]
        public async Task<ActionResult<SearchResultDTO>> Search([FromBody] SearchRequestDTO request)
        {
            try
            {
                var result = await _searchService.SearchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Search failed", message = ex.Message });
            }
        }

        [HttpGet("posts/suggestions")]
        public async Task<ActionResult<List<PostSuggestionDTO>>> GetPostSuggestions([FromQuery] string q, [FromQuery] int limit = 5)
        {
            try
            {
                var suggestions = await _searchService.GetPostSuggestionsAsync(q, limit);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get post suggestions", message = ex.Message });
            }
        }

        [HttpGet("materials/suggestions")]
        public async Task<ActionResult<List<StudyMaterialSuggestionDTO>>> GetStudyMaterialSuggestions([FromQuery] string q, [FromQuery] int limit = 5)
        {
            try
            {
                var suggestions = await _searchService.GetStudyMaterialSuggestionsAsync(q, limit);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get study material suggestions", message = ex.Message });
            }
        }

        [HttpPost("posts/{postId}/reindex")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<bool>> ReindexPost(Guid postId)
        {
            try
            {
                var result = await _searchService.ReindexPostAsync(postId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to reindex post", message = ex.Message });
            }
        }

        [HttpPost("materials/{materialId}/reindex")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<bool>> ReindexStudyMaterial(Guid materialId)
        {
            try
            {
                var result = await _searchService.ReindexStudyMaterialAsync(materialId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to reindex study material", message = ex.Message });
            }
        }

        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SearchAnalyticsDTO>> GetSearchAnalytics([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            try
            {
                var analytics = await _searchService.GetSearchAnalyticsAsync(from, to);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get search analytics", message = ex.Message });
            }
        }

        [HttpGet("quick")]
        [AllowAnonymous]
        public async Task<ActionResult<SearchResultDTO>> QuickSearch([FromQuery] string q, [FromQuery] SearchType type = SearchType.All, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var request = new SearchRequestDTO
                {
                    Query = q,
                    SearchType = type,
                    IncludeKeywordSearch = true,
                    IncludeSemanticSearch = false, // Quick search is keyword only for performance
                    Page = page,
                    PageSize = pageSize,
                    SortBy = "relevance",
                    SortOrder = "desc"
                };

                var result = await _searchService.SearchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Quick search failed", message = ex.Message });
            }
        }

        [HttpGet("semantic")]
        [AllowAnonymous]
        public async Task<ActionResult<SearchResultDTO>> SemanticSearch([FromQuery] string q, [FromQuery] SearchType type = SearchType.All, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { error = "Query is required for semantic search" });
                }

                var request = new SearchRequestDTO
                {
                    Query = q,
                    SearchType = type,
                    IncludeKeywordSearch = false, // Semantic search only
                    IncludeSemanticSearch = true,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = "relevance",
                    SortOrder = "desc"
                };

                var result = await _searchService.SearchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Semantic search failed", message = ex.Message });
            }
        }

        [HttpGet("advanced")]
        [AllowAnonymous]
        public async Task<ActionResult<SearchResultDTO>> AdvancedSearch(
            [FromQuery] string q = "",
            [FromQuery] SearchType type = SearchType.All,
            [FromQuery] List<Guid> categoryIds = null,
            [FromQuery] List<Guid> materialCategoryIds = null,
            [FromQuery] bool? isQuestion = null,
            [FromQuery] bool includeSemantic = true,
            [FromQuery] bool includeKeyword = true,
            [FromQuery] string sortBy = "relevance",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] List<string> tags = null)
        {
            try
            {
                var request = new SearchRequestDTO
                {
                    Query = q,
                    SearchType = type,
                    CategoryIds = categoryIds ?? new List<Guid>(),
                    MaterialCategoryIds = materialCategoryIds ?? new List<Guid>(),
                    IsQuestion = isQuestion,
                    IncludeSemanticSearch = includeSemantic,
                    IncludeKeywordSearch = includeKeyword,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    Page = page,
                    PageSize = pageSize,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Tags = tags ?? new List<string>()
                };

                var result = await _searchService.SearchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Advanced search failed", message = ex.Message });
            }
        }
    }
}