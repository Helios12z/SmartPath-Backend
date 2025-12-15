namespace SmartPathBackend.Models.DTOs
{
    public record StudyMaterialRatingRequest(
        int Rating, // 1-5
        string? Comment
    );

    public record StudyMaterialRatingResponse(
        Guid Id,
        Guid MaterialId,
        Guid UserId,
        string UserName,
        int Rating,
        string? Comment,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    public record StudyMaterialRatingStats(
        double AverageRating,
        int TotalRatings,
        int RatingDistribution1,
        int RatingDistribution2,
        int RatingDistribution3,
        int RatingDistribution4,
        int RatingDistribution5
    );
}