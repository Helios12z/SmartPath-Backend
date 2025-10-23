namespace SmartPathBackend.Models.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = null!;
        public ICollection<CategoryPost>? CategoryPosts { get; set; }
    }
}