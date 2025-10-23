using System.ComponentModel.DataAnnotations;

namespace SmartPathBackend.Models.DTOs
{
    public class MaterialResponse
    {
        public Guid Id { get; set; }
        public Guid UploaderId { get; set; }
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public Guid? MessageId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string FileUrl { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
    }

    public class MaterialCreateRequest
    {
        public Guid? PostId { get; set; }
        public Guid? CommentId { get; set; }
        public Guid? MessageId { get; set; }

        [Required] public string Title { get; set; } = null!;
        public string? Description { get; set; }
    }
}
