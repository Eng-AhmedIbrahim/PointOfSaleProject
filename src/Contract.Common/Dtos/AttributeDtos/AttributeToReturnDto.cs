namespace POS.Contract.Dtos.AttributeDtos;

public class AttributeToReturnDto
{
    public int Id { get; set; }
    public string? EnglishName { get; set; }
    public string? ArabicName { get; set; }
    public ICollection<AttributeItemToReturnDto> AttributeItems { get; set; } = new HashSet<AttributeItemToReturnDto>();
}