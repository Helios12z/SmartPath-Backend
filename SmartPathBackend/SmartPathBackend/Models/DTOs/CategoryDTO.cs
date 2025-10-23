namespace SmartPathBackend.Models.DTOs
{
    public class CategoryRequestDto
    {
        public string Name { get; set; } = null!;
    }

    public class CategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int PostCount { get; set; } 
    }
}
