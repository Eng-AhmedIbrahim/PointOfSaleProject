namespace BlazorBase.API;

public record ApiEndpoints
{
    public string? GetAllCategories { get; set; }
    public string? GetItemsByCategoryId { get; set; }
}