namespace POS.Contract.Dtos.ItemDto;

public class MenuSalesItemsGroupDto
{
    public int Id { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public decimal? Price { get; set; }

    public MenuSalesItemsGroupDto Clone()
    {
        return new MenuSalesItemsGroupDto
        {
            Id = this.Id,
            ArabicName = this.ArabicName,
            EnglishName = this.EnglishName,
            Price = this.Price
        };
    }
}