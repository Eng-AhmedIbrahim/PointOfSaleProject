namespace POS.API.Dtos.AttributeDtos;

public class AttributeDto
{
    [Required]
    public string EnglishName { get; set; }
    [Required]
    public string ArabicName { get; set; }

    public ICollection<AttributeItemDto> AttributeItems { get; set; } = [];
}