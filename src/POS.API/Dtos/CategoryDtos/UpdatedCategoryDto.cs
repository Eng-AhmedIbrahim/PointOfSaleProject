namespace POS.API.Dtos.CategoryDtos;

public class UpdatedCategoryDto
{
    public int Id { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string? NormalizedEnglishName { get; set; }
    public string? ItemsFont { get; set; }
    public bool Invisible { get; set; } = false;
}
