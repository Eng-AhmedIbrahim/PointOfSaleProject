namespace ERPFront.Components.Pages;

public partial class POS
{
    private Dictionary<int, int> _itemClickCount = new Dictionary<int, int>();
    private MenuSalesItemsToReturnDto? _currentBaseItem;
    private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();
    private ICollection<MenuSalesItemsToReturnDto> _itemByCatId = new List<MenuSalesItemsToReturnDto>();
    private int currentCatId;
    private List<string>? currentSelectedAttribute;

    protected override async Task OnInitializedAsync()
    {
         _categories = await _categoryServices.GetAllCategoriesAsync();
        _commonProperties.OnChange += StateHasChanged;
    }

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

    private async Task AddItemToSection4(MenuSalesItemsToReturnDto selectedMenuItem)
    {
        TableItem? selectedTableItem = GetItemFromTableById(selectedMenuItem);

        if (!selectedMenuItem.Attributes.Any() && selectedTableItem != null)
        {
            selectedTableItem.Quantity++;
            selectedTableItem.Total = selectedTableItem.Total + selectedTableItem.Price;
        }
        else
        {
            if (_itemClickCount.Count == 0)
                InitializeBaseItem(selectedMenuItem);

            var currentClickCount = GetCurrentClickCount();

            if (currentClickCount < _currentBaseItem?.Attributes.Count)
            {
                if (currentClickCount > 0)
                    AddAttributeNameToSection4Item(selectedMenuItem, currentClickCount);

                UpdateAttributeGroup(currentClickCount);
                IncrementClickCount();
            }
            else
            {
                if (_currentBaseItem!.Attributes.Any())
                    AddAttributeNameToSection4Item(selectedMenuItem, currentClickCount);

                await AddItemToTable(_currentBaseItem ?? new());
                ResetClickCountAndBaseItem();
            }
        }
        UpdateTableItemCount();
        StateHasChanged();
    }

    private void AddAttributeNameToSection4Item(MenuSalesItemsToReturnDto selectedMenuItem, int currentClickCount)
    {
        currentSelectedAttribute?.Add(selectedMenuItem.ArabicName ?? "");
        _currentBaseItem!.Price += selectedMenuItem.Price ?? 0;
    }

    private void InitializeBaseItem(MenuSalesItemsToReturnDto menuItem)
    {
        _currentBaseItem = menuItem;
        _itemClickCount[menuItem.Id] = 0;
        currentSelectedAttribute = [];
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
         => _itemClickCount[_currentBaseItem?.Id ?? 0]++;

    private async Task AddItemToTable(MenuSalesItemsToReturnDto menuItem)
    {
        var newTableItem = new TableItem
        {
            Id = menuItem.Id,
            Name = menuItem.ArabicName,
            Price = menuItem.Price ?? 0,
            Quantity = 1,
            Total = menuItem.Price ?? 0,
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

    public void ClearTableItems()
    {
        _commonProperties?.TableItems?.Clear();
        StateHasChanged();
    }
    private void RemoveItemFromSection4(TableItem item)
    {
        _commonProperties?.TableItems?.Remove(item);
        UpdateTableItemCount();
        StateHasChanged();
    }

    private TableItem? GetItemFromTableById(MenuSalesItemsToReturnDto selectedMenuItem)
        => _commonProperties?.TableItems?.Where(c=>c.Attributes.Count == 0).FirstOrDefault(s => s.Id == selectedMenuItem.Id);

    public void Dispose()
    {
        _commonProperties.OnChange -= StateHasChanged;
    }
}