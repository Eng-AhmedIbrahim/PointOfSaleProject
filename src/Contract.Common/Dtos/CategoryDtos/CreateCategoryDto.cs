namespace POS.Contract.Dtos.CategoryDtos;

public class CreateCategoryDto
{
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string ItemsFont { get; set; } = "regular";
    public bool Invisible { get; set; } = false;

    public DateTime CreationDate { get; set; }
}
