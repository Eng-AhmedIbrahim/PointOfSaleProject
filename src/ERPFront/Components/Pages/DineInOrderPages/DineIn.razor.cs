namespace ERPFront.Components.Pages.DineInOrderPages;

public partial class DineIn
{

    private string OpenTime { get; set; } = "12:30 PM";

    public List<TableItem> Items { get; set; } = new();

    public List<TableGroupToReturnDto>? _tableGroups { get; set; }
    public List<TableToReturnDto>? _tables { get; set; }

    public List<CaptainOrderUserToReturnDto>? _captainOrders { get; set; } = new();

    protected async override Task OnInitializedAsync()
    {
         var TableGroups = await _DineInService.GetTableGroupsAsync();
        _tableGroups = TableGroups.ToList();

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

    private Dictionary<string, bool> buttonStates = new();
    private Dictionary<int, bool> tableStates = new();

    private async Task ToggleState<T>(Dictionary<T, bool> stateDict, T key ,string keyName, Action<Dictionary<T, bool>, T> stateUpdater)
    {
        if (key == null) return;

        if(key is int)
        {
            _commonProperties!.CurrentDineInOrder!.RelatedTableId = Int32.Parse(key.ToString()!);
            _commonProperties!.CurrentDineInOrder!.RelatedTableName = keyName;
        }
        if(key is string)
        {
            _commonProperties!.CurrentDineInOrder!.CaptainId = key.ToString()!;
            _commonProperties!.CurrentDineInOrder!.CaptainName = keyName;
        }

        stateUpdater(stateDict, key);
        await InvokeAsync(StateHasChanged);
    }

    private void UpdateState<T>(Dictionary<T, bool> stateDict, T key)
    {
        var newDict = new Dictionary<T, bool>(stateDict)
        {
            [key] = !stateDict.GetValueOrDefault(key, false)
        };
        stateDict.Clear();
        foreach (var kvp in newDict)
        {
            stateDict[kvp.Key] = kvp.Value;
        }
    }

    private string GetButtonClass(CaptainOrderUserToReturnDto captainOrder)
    {
        if (captainOrder == null || string.IsNullOrEmpty(captainOrder.Id))
            return "order-button";

        return buttonStates.TryGetValue(captainOrder.Id, out bool isActive) && isActive
            ? "order-button red-button"
            : "order-button";
    }

    private string GetTableClass(TableToReturnDto table)
    {
        if (table == null || table.Id == null)
            return "order-button";

        return tableStates.TryGetValue(table.Id.Value, out bool isActive) && isActive
            ? "order-button red-button"
            : "order-button";
    }
}