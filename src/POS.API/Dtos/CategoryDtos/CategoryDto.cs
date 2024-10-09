namespace POS.API.Dtos.CategoryDtos;

public class CategoryDto
{
    [Required]
    public string? ArabicName { get; set; }
    [Required]
    public string? EnglishName { get; set; }
    public string? ItemsFont { get; set; }
    public bool Invisible { get; set; } = false;
    public bool HasAttribute { get; set; } = false;
}