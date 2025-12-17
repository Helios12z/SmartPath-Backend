using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<SearchResultDTO>> Search([FromBody] SearchRequestDTO request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { error = "Query is required" });
                }

                // Set defaults if not provided
                request.Page = request.Page <= 0 ? 1 : request.Page;
                request.PageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);
                request.SortBy = string.IsNullOrEmpty(request.SortBy) ? "relevance" : request.SortBy;
                request.SortOrder = string.IsNullOrEmpty(request.SortOrder) ? "desc" : request.SortOrder;

                var result = await _searchService.SearchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Search failed", message = ex.Message });
            }
        }

        
        [HttpGet("posts/suggestions")]
        [AllowAnonymous]
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
        [AllowAnonymous]
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
    }
}