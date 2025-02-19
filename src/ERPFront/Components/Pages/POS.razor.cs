namespace ERPFront.Components.Pages;

public partial class POS
{
    private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();
    private ICollection<MenuSalesItemsToReturnDto> _itemByCatId = new List<MenuSalesItemsToReturnDto>();
    private int currentCatId;
    private List<string>? currentSelectedAttribute;

    protected override async Task OnInitializedAsync()
        => _categories = await _categoryServices.GetAllCategoriesAsync();

    private async Task InvokeItems(int catId)
    {
        _itemByCatId = await _categoryServices.GetItemsByCategoryIdAsync(catId);
        currentCatId = catId;
    }

    private Task OnSection4ItemsChanged()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Dictionary<int, int> _itemClickCount = new Dictionary<int, int>();
    private MenuSalesItemsToReturnDto? _currentBaseItem;

    private async Task AddItemToSection4(MenuSalesItemsToReturnDto selectedMenuItem)
    {
        if (_itemClickCount.Count == 0)
        {
            InitializeBaseItem(selectedMenuItem);
        }

        var currentClickCount = GetCurrentClickCount();

        if (currentClickCount < _currentBaseItem?.Attributes.Count)
        {
            if(currentClickCount > 0)
                currentSelectedAttribute?.Add(selectedMenuItem.ArabicName??"");
            
            UpdateAttributeGroup(currentClickCount);
            IncrementClickCount();
            
        }
        else
        {
            if(_currentBaseItem!.Attributes.Any())
                currentSelectedAttribute?.Add(selectedMenuItem.ArabicName ?? "");

            await AddItemToTable(_currentBaseItem??new());
            ResetClickCountAndBaseItem();
        }

        UpdateTableItemCount();
        StateHasChanged();
    }

    private void InitializeBaseItem(MenuSalesItemsToReturnDto menuItem)
    {
        _currentBaseItem = menuItem;
        _itemClickCount[menuItem.Id] = 0;
        currentSelectedAttribute = new List<string>();
    }

    private int GetCurrentClickCount()
     => _itemClickCount[_currentBaseItem?.Id ?? 0];

    private void UpdateAttributeGroup(int clickCount)
    {
        var attributeGroup = _currentBaseItem?.Attributes
            .FirstOrDefault(a => a.AppearanceIndex == clickCount + 1);

        if (attributeGroup != null)
        {
            _itemByCatId.Clear();
            foreach (var item in attributeGroup.GroupItems)
            {
                var newMenuItem = new MenuSalesItemsToReturnDto
                {
                    Id = item.Id,
                    ArabicName = item.ArabicName,
                    EnglishName = item.EnglishName,
                    Price = item.Price
                };
                _itemByCatId.Add(newMenuItem);
            }
        }
    }

    private void IncrementClickCount()
         =>
        _itemClickCount[_currentBaseItem?.Id ?? 0]++;

    private async Task AddItemToTable(MenuSalesItemsToReturnDto menuItem)
    {
        var newTableItem = new TableItem
        {
            Id = menuItem.Id,
            Name = menuItem.ArabicName,
            Price = (double)(menuItem.Price ?? 0),
            Quantity = 1,
            Total = (double)(menuItem.Price ?? 0),
            Attributes = currentSelectedAttribute ?? []
        };
        _commonProperties?.TableItems?.Add(newTableItem);
        await InvokeItems(currentCatId);
    }

    private void ResetClickCountAndBaseItem()
    {
        _itemClickCount.Clear();
        _currentBaseItem = null;
    }
    private void UpdateTableItemCount()
    {
        int count = _commonProperties?.TableItems?.Count ?? 0;
        JsRuntime.InvokeVoidAsync("setTableItemCount", count);
    }

    private void RemoveItemFromSection4(TableItem item)
    {
        _commonProperties?.TableItems?.Remove(item);
        UpdateTableItemCount(); 
        StateHasChanged();
    }

    public void ClearTableItems()
    {
        _commonProperties?.TableItems?.Clear();
        StateHasChanged();
    }
}