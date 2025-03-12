using POS.Contract.Dtos.OrderDto;
using POS.Contract.Enums;
using POS.Core.Entities.OrderEntity;

namespace ERPFront.Components.Pages;

public partial class POS
{
    private Dictionary<int, int> _itemClickCount = new Dictionary<int, int>();
    private MenuSalesItemsToReturnDto? _currentBaseItem;
    private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();
    private ICollection<MenuSalesItemsToReturnDto> _itemByCatId = new List<MenuSalesItemsToReturnDto>();
    private int currentCatId;
    private List<AttributeDto>? currentSelectedAttribute;
    private delegate void FinanceSettingsDelegate(OrderSettingToReturnDto? orderSettings);

    public string? NoteValue { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _categories = await _categoryServices.GetAllCategoriesAsync();
        _categories = _categories.Where(c => c.Invisible == false).ToList();

        _commonProperties.OnChange += StateHasChanged;
        CustomizationSettingsService.OnChanged += StateHasChanged;
        _section4ButtonsServices.OnChanged += () => InvokeAsync(StateHasChanged);





        await GetCurrentDayAndTime();
        await GetOrdersSetting();

        _cartService.UpdateFinanceSettingsByMode(_commonProperties.CurrentPosMode);

    }
    private async Task InvokeItems(int catId)
    {
        _itemByCatId = await _categoryServices.GetItemsByCategoryIdAsync(catId);
        _itemByCatId = _itemByCatId.Where(i => i.Invisible == false).ToList();

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

        if (!selectedMenuItem.Attributes.Any() && _itemClickCount.Count() == 0 && selectedTableItem != null)
        {
            selectedTableItem.Quantity++;
            selectedTableItem.Total = selectedTableItem.Total + selectedTableItem.Price;
            _cartService.CalculateTotalAmountFromTableItems(_commonProperties!.TableItems!);
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
        if (selectedMenuItem == null) return;

        var newAttribute = new AttributeDto
        {
            Id = selectedMenuItem.Id,
            Name = selectedMenuItem.ArabicName ?? string.Empty
        };

        currentSelectedAttribute?.Add(newAttribute);
        _currentBaseItem!.Price += selectedMenuItem.AttributePrice ?? 0;
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

        CalculateTotalAmount();

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
        CalculateTotalAmount();
        StateHasChanged();
    }

    private TableItem? GetItemFromTableById(MenuSalesItemsToReturnDto selectedMenuItem)
        => _commonProperties?.TableItems?.Where(c => c!.Attributes!.Count == 0).FirstOrDefault(s => s.Id == selectedMenuItem.Id);
    public void CalculateTotalAmount()
    {
        if (_commonProperties?.TableItems != null)
        {
            _commonProperties._financeSettingsList![0].Value = _commonProperties.TableItems.Sum(i => i.Total ?? 0);
            StateHasChanged();
        }
    }


    private DateTime _lastEnterPress = DateTime.MinValue;
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            var now = DateTime.UtcNow;
            if ((now - _lastEnterPress).TotalMilliseconds < 500)
            {
                await ExecuteDoubleEnterFunction();
            }
            _lastEnterPress = now;
        }
    }

    private Task ExecuteDoubleEnterFunction()
    {
        Console.WriteLine("Double Enter Pressed!");
        // Call your desired function here
        return Task.CompletedTask;
    }

    private async Task GetCurrentDayAndTime()
    {
        var appDate = await _appDate.GetAppDate();
        _commonProperties.PosDate = DateOnly.FromDateTime(appDate.PosDate);
    }

    private async Task GetOrdersSetting()
        => _commonProperties.OrderSettings = await _orderSettingsService.GetOrderSettingsAsync();


    private void SetFinanceSettings(OrderSettingToReturnDto? orderSettings)
    {
        _commonProperties._financeSettingsList = new()
    {
        new FinanceSettings { Label = "Account", Value = 0M },
        new FinanceSettings { Label = "Discount", Value = 0M },
        new FinanceSettings { Label = "Tax", Value = orderSettings?.Tax ?? 0M },
        new FinanceSettings { Label = "Service", Value = orderSettings?.Service ?? 0M },
        new FinanceSettings { Label = "Total", Value = 0M }
    };
    }


    public void Dispose()
    {
        _commonProperties.OnChange -= StateHasChanged;
        CustomizationSettingsService.OnChanged -= StateHasChanged;
        _section4ButtonsServices.OnChanged -= StateHasChanged;
    }
}