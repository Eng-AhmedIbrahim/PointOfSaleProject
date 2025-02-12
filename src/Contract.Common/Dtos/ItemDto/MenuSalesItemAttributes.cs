namespace POS.Contract.Dtos.ItemDto;

public class MenuSalesItemAttributes
{
    public int AppearanceIndex { get; set; }
    public List<MenuSalesItemsGroupDto> GroupItems { get; set; } = [];
}