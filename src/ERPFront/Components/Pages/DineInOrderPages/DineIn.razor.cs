namespace ERPFront.Components.Pages.DineInOrderPages;

public partial class DineIn
{
    public List<TableItem> Items { get; set; } = new();
    public List<TableGroupToReturnDto>? _tableGroups { get; set; }
    public List<TableToReturnDto>? _tables { get; set; }
    public List<CaptainOrderUserToReturnDto>? _captainOrders { get; set; } = new();
    private Dictionary<string, bool> buttonStates = new();
    private Dictionary<int, bool> tableStates = new();
    protected async override Task OnInitializedAsync()
    {
        var TableGroups = await _DineInService.GetTableGroupsAsync();
        _tableGroups = TableGroups.ToList();


        _commonProperties.AppendedTableItems!.Clear();

        await GetCaptainOrders();
    }

    private async Task GetTablesFromGroup(int tableGroupId)
    {
        var TableItems = await _DineInService.GetTablesByGroupId(tableGroupId);
        _tables = TableItems.ToList();
    }

    private async Task GetCaptainOrders()
    {
        var CaptainOrders = await _DineInService.GetCaptainOrders();
        _captainOrders = CaptainOrders.ToList();
    }

    private async Task ToggleState<T>(Dictionary<T, bool> stateDict, T key, string keyName, Action<Dictionary<T, bool>, T> stateUpdater)
    {
        if (key == null) return;

        if (key is int)
        {
            _commonProperties.TableId = Int32.Parse(key.ToString()!);
            _commonProperties!.CurrentDineInOrder!.RelatedTableId = Int32.Parse(key.ToString()!);
            _commonProperties!.CurrentDineInOrder!.RelatedTableName = keyName;
            if (int.TryParse(key.ToString(), out int keyInt) && _commonProperties.DineInOrdersDetails?.TryGetValue(keyInt, out DineInOrderDetails? orderDetails) == true)
            {
                _commonProperties!.DineInOrderValues = new()
                {
                    OrderID = orderDetails!.BasicOrderDetails!.OrderId,
                    TableOpenTime = orderDetails?.BasicOrderDetails?.OrderDataTime?.ToString("hh:mm tt") ?? "N/A",
                    TableName = orderDetails!.RelatedTableName,
                    CaptainName = orderDetails.CaptainName,
                    Total = orderDetails.BasicOrderDetails.Total
                };
                Items = new List<TableItem>(
                    orderDetails!.BasicOrderDetails!.Items.Select(item =>
                    {
                        var newItem = item.Clone();
                        newItem.IsReadOnly = true;
                        return newItem;
                    })
                );

            }
            else
            {
                Items.Clear();
                _commonProperties!.DineInOrderValues = new();
            }

        }
        if (key is string)
        {
            _commonProperties!.CurrentDineInOrder!.CaptainId = key.ToString()!;
            _commonProperties!.CurrentDineInOrder!.CaptainName = keyName;
        }

        stateUpdater(stateDict, key);
        await InvokeAsync(StateHasChanged);
    }

    private void UpdateState<T>(Dictionary<T, bool> stateDict, T key)
    {
        foreach (var existingKey in stateDict.Keys.ToList())
        {
            stateDict[existingKey] = false;
        }

        stateDict[key] = true;
    }

    private string GetTableClass(TableToReturnDto table)
    {
        if (table == null || table.Id == null)
            return "order-button";

        _commonProperties!.DineInOrdersDetails!.TryGetValue(table.Id.Value, out var found);
        if (found is null)
        {
            return tableStates.TryGetValue(table.Id.Value, out bool isActive) && isActive
                ? "order-button red-button"
                : "order-button";
        }

        return "order-button";
    }

    private string GetButtonClass(CaptainOrderUserToReturnDto captainOrder)
    {
        if (captainOrder == null || string.IsNullOrEmpty(captainOrder.Id))
            return "order-button";

        return buttonStates.TryGetValue(captainOrder.Id, out bool isActive) && isActive
            ? "order-button red-button"
            : "order-button";
    }
}