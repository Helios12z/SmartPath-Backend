using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.DTOs
{
    public class SearchRequestDTO
    {
        public string Query { get; set; } = string.Empty;
        public SearchType SearchType { get; set; } = SearchType.All;
        public List<Guid> CategoryIds { get; set; } = new();
        public List<Guid> MaterialCategoryIds { get; set; } = new();
        public bool? IsQuestion { get; set; }
        public bool IncludeSemanticSearch { get; set; } = true;
        public bool IncludeKeywordSearch { get; set; } = true;
        public string SortBy { get; set; } = "relevance"; // relevance, created, updated, views
        public string SortOrder { get; set; } = "desc"; // asc, desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class SearchResultDTO
    {
        public List<PostSearchResultDTO> Posts { get; set; } = new();
        public List<StudyMaterialSearchResultDTO> StudyMaterials { get; set; } = new();
        public int TotalPosts { get; set; }
        public int TotalStudyMaterials { get; set; }
        public int TotalResults => TotalPosts + TotalStudyMaterials;
        public SearchFacetsDTO Facets { get; set; } = new();
        public SearchSuggestionDTO Suggestions { get; set; } = new();
        public TimeSpan QueryTime { get; set; }
    }

    public class PostSearchResultDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public bool IsQuestion { get; set; }
        public bool IsSolved { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public AuthorDTO Author { get; set; } = new();
        public List<CategoryDTO> Categories { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public float RelevanceScore { get; set; }
        public SearchMatchType MatchType { get; set; }
        public List<string> HighlightedTitle { get; set; } = new();
        public List<string> HighlightedContent { get; set; } = new();
    }

    public class StudyMaterialSearchResultDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int DownloadCount { get; set; }
        public float AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public AuthorDTO Uploader { get; set; } = new();
        public MaterialCategoryDTO Category { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public float RelevanceScore { get; set; }
        public SearchMatchType MatchType { get; set; }
        public List<string> HighlightedTitle { get; set; } = new();
        public List<string> HighlightedDescription { get; set; } = new();
        public bool IsApproved { get; set; }
        public float AiConfidence { get; set; }
    }

    public class SearchFacetsDTO
    {
        public List<CategoryFacetDTO> Categories { get; set; } = new();
        public List<MaterialCategoryFacetDTO> MaterialCategories { get; set; } = new();
        public List<FacetCountDTO> Types { get; set; } = new();
        public List<FacetCountDTO> Tags { get; set; } = new();
        public List<FacetCountDTO> Years { get; set; } = new();
    }

    public class CategoryFacetDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MaterialCategoryFacetDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Count { get; set; }
        public List<MaterialCategoryFacetDTO> Children { get; set; } = new();
    }

    public class FacetCountDTO
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class SearchSuggestionDTO
    {
        public string CorrectedQuery { get; set; } = string.Empty;
        public List<string> RelatedQueries { get; set; } = new();
        public List<string> DidYouMean { get; set; } = new();
    }

    public class PostSuggestionDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsQuestion { get; set; }
        public List<string> Categories { get; set; } = new();
    }

    public class StudyMaterialSuggestionDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class SearchAnalyticsDTO
    {
        public int TotalQueries { get; set; }
        public int UniqueQueries { get; set; }
        public List<string> TopQueries { get; set; } = new();
        public double AverageQueryLength { get; set; }
        public List<int> QueryLengthDistribution { get; set; } = new();
        public List<SearchResultTypeDTO> TopResultTypes { get; set; } = new();
        public double AverageResultsPerPage { get; set; }
    }

    public class SearchResultTypeDTO
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class AuthorDTO
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }

    public class CategoryDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    public class MaterialCategoryDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}

namespace SmartPathBackend.Models.Enums
{
    public enum SearchType
    {
        All,
        Posts,
        StudyMaterials
    }

    public enum SearchMatchType
    {
        Exact,
        Semantic,
        Keyword,
        Fuzzy
    }
}