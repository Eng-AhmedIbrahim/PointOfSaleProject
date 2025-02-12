namespace POS.Contract.Dtos.CategoryDtos;

public class UpdatedCategoryDto
{
    [Required]
    public int Id { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string? ItemsFont { get; set; }
    public bool Invisible { get; set; } = false;
    public DateTime UpdatedDate { get; set; }

    public UpdatedCategoryDto()
    {
        UpdatedDate = DateTime.Now;
        UpdatedDate.ToString("yyyy-MM-dd hh:mm:ss.fff tt zzz");
    }
}