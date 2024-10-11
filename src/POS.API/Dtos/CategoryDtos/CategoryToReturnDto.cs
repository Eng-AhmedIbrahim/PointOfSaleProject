namespace POS.API.Dtos.CategoryDtos;

public class CategoryToReturnDto
{
    public int Id { get; set; }
    public string ArabicName { get; set; }
    public string EnglishName { get; set; }
    public bool Invisible { get; set; } = false;
}