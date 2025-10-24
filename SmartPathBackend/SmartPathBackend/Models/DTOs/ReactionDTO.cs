namespace SmartPathBackend.Models.DTOs
{
    public class ReactionRequestDto
    {
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public bool IsPositive { get; set; }
    }

    public class ReactionResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool IsPositive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
