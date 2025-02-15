using BlazorBase;
using Microsoft.JSInterop;
namespace ERPFront.Components.Pages;

public partial class POS
{
    private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();
    private ICollection<MenuSalesItemsToReturnDto> _itemByCatId = new List<MenuSalesItemsToReturnDto>();

    protected override async Task OnInitializedAsync()
        => _categories = await CategoryServices.GetAllCategoriesAsync();

    private async Task InvokeItems(int catId)
        => _itemByCatId = await CategoryServices.GetItemsByCategoryIdAsync(catId);

    private Task OnSection4ItemsChanged()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void AddItemToSection4(MenuSalesItemsToReturnDto selectedItem)
    {
        var newItem = new TableItem
        {
            Id = selectedItem.Id,
            Name = selectedItem.ArabicName,
            Price = (double)(selectedItem.Price ?? 0),
            Quantity = 1,
            Total = (double)(selectedItem.Price ?? 0)
        };

        CommonProperties?.TableItems?.Add(newItem);
        UpdateTableItemCount();
        StateHasChanged();
    }
    private void UpdateTableItemCount()
    {
        int count = CommonProperties?.TableItems?.Count ?? 0;
        JsRuntime.InvokeVoidAsync("setTableItemCount", count);
    }

    private void RemoveItemFromSection4(TableItem item)
    {
        CommonProperties?.TableItems?.Remove(item);
        UpdateTableItemCount(); // Update count after removal
        StateHasChanged();
    }

    public void ClearTableItems()
    {
        CommonProperties?.TableItems?.Clear();
        StateHasChanged();
    }
}