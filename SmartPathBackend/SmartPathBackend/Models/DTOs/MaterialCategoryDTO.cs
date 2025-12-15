namespace SmartPathBackend.Models.DTOs
{
    public record MaterialCategoryNodeDto(
        Guid Id, string Name, string Slug, string Path, int Level, int SortOrder, bool IsActive,
        List<MaterialCategoryNodeDto> Children
    );

    public record MaterialCategoryCreateRequest(string Name, string Slug, Guid? ParentId, int SortOrder);
    public record MaterialCategoryUpdateRequest(string Name, string Slug, Guid? ParentId, int SortOrder, bool IsActive);
    public record MoveCategoryRequest(Guid? NewParentId, int NewSortOrder);
}
