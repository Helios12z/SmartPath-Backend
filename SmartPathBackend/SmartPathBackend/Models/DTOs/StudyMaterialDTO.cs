using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.DTOs
{
    public record StudyMaterialCreateMeta(
        Guid CategoryId,
        string Title,
        string? Description,
        StudyMaterialSourceType SourceType,
        string? SourceUrl
    );

    public record StudyMaterialResponse(
        Guid Id,
        Guid CategoryId,
        string CategoryPath,
        string Title,
        string? Description,
        StudyMaterialSourceType SourceType,
        string? FileUrl,
        string? SourceUrl,
        Status Status,
        string? RejectReason,
        bool? AiCategoryMatch,
        double? AiConfidence,
        Guid? AiSuggestedCategoryId,
        string? AiReason,
        DateTime CreatedAt,
        double AverageRating = 0.0,
        int TotalRatings = 0
    );

    public record ReviewDecisionRequest(Status Decision, string? Reason);
}
