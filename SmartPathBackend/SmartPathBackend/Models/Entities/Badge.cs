namespace SmartPathBackend.Models.Entities
{
    public class Badge: BaseEntity
    {
        public int Point { get; set; }
        public string Name { get; set; } = null!;
    }
}
