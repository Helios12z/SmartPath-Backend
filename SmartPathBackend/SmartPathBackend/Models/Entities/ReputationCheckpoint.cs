using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Models.Entities
{
    public class ReputationCheckpoint
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public ContentType ContentType { get; set; }
        public Guid ContentId { get; set; }

        public int LikeBandsApplied { get; set; }   
        public int DislikeBandsApplied { get; set; } 

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
