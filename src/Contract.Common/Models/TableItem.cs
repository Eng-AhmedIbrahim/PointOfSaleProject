namespace POS.Contract.Models;

public class TableItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public decimal? Total { get; set; }
    public List<AttributeDto> Attributes { get; set; } = new List<AttributeDto>();
}


public class AttributeDto
{
    public int? Id { get; set; }
    public string? Name { get; set; } = string.Empty;
}