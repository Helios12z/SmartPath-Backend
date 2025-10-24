namespace SmartPathBackend.Models.DTOs
{
    public class BadgeRequestDTO
    {
        public int Point { get; set; }
        public string Name { get; set; } = null!;
    }

    public class BadgeResponseDTO
    {
        public Guid Id { get; set; }
        public int Point { get; set; }
        public string Name { get; set; } = null!;
    }
}
