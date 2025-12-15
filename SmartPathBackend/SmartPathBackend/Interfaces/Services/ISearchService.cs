using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Interfaces.Services
{
    public interface ISearchService
    {
        Task<SearchResultDTO> SearchAsync(SearchRequestDTO request, CancellationToken cancellationToken = default);
        Task<List<PostSuggestionDTO>> GetPostSuggestionsAsync(string query, int limit = 5, CancellationToken cancellationToken = default);
        Task<List<StudyMaterialSuggestionDTO>> GetStudyMaterialSuggestionsAsync(string query, int limit = 5, CancellationToken cancellationToken = default);
        Task<bool> ReindexPostAsync(Guid postId, CancellationToken cancellationToken = default);
        Task<bool> ReindexStudyMaterialAsync(Guid materialId, CancellationToken cancellationToken = default);
        Task<SearchAnalyticsDTO> GetSearchAnalyticsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    }
}