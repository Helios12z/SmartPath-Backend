namespace SmartPathBackend.Models.Entities
{
    public class SystemLog: BaseEntity
    {
        public Guid? UserId { get; set; }
        public string Action { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public string? Url { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
