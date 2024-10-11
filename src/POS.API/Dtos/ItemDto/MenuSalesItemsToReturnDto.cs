namespace POS.API.Dtos.ItemDto;

public class MenuSalesItemsToReturnDto
{
    public int Id { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public decimal? Price { get; set; }
    //public decimal? Tax { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? BackColor { get; set; }
    public string? TextColor { get; set; }
    public int? TextSize { get; set; }
    public bool Invisible { get; set; } = false;
    public List<MenuSalesItemAttributes> Attributes { get; set; } = [];
}