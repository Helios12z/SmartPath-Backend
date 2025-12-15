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
        DateTime CreatedAt
    );

    public record ReviewDecisionRequest(Status Decision, string? Reason);
}
