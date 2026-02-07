namespace POS.Desktop.Components.DineInComponents;

public partial class TransferTable
{
    private List<DineInOrderDetails> OccupiedTables = new();
    private List<TableToReturnDto> AvailableTables = new();
    private int? SelectedCurrentTable;
    private int? SelectedAvailableTable;

    protected override async Task OnInitializedAsync()
    {
        if (_commonProperties.DineInOrdersDetails != null)
        {
            var allTables = await _dineInService.GetTables();

            OccupiedTables = _commonProperties.DineInOrdersDetails.Values
                .SelectMany(x => x)
                .Where(x => !string.IsNullOrEmpty(x.RelatedTableName))
                .ToList();

            AvailableTables = allTables
                .Where(t => !OccupiedTables.Any(o => o.RelatedTableId == t.Id))
                .ToList();
        }
    }

    private void OnCurrentTableChanged(int? tableId)
    {
        SelectedCurrentTable = tableId;
        StateHasChanged();
    }

    private void OnAvailableTableChanged(int? tableId)
    {
        SelectedAvailableTable = tableId;
        StateHasChanged();
    }

    [Inject] private IDineInOrderFrontService _dineInOrderServiceFront { get; set; }
    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    private async Task TransferTables()
    {
        if (SelectedCurrentTable == null || SelectedAvailableTable == null)
        {
            _snackbar.Add("Please select both a current and an available table.", Severity.Warning);
            return;
        }

        var success = await ChangeTableDetails(SelectedCurrentTable, SelectedAvailableTable, AvailableTables.First(t => t.Id == SelectedAvailableTable).TableName ?? string.Empty);

        if (success)
        {
            _snackbar.Add("Table transferred successfully!", Severity.Success);
            
            _commonProperties.CurrentDineInOrder = null;
            _commonProperties.TableId = 0;
            
            CloseDialog();
            _navigationManager.NavigateTo("/dineIn", true);
        }
        else
        {
            _snackbar.Add("Failed to transfer table.", Severity.Error);
        }
    }

    private async Task<bool> ChangeTableDetails(int? oldTableId, int? newTableId, string newTableName)
    {
        if (oldTableId == null || newTableId == null) return false;

        // Since we are transferring a WHOLE table in this dialog (usually), 
        // we might move ALL orders for that table or just one?
        // The original logic seemed to assume one order per table.
        // Let's move all of them.
        
        if (_commonProperties!.DineInOrdersDetails!.TryGetValue(oldTableId ?? 0, out var orderDetails) && orderDetails != null && orderDetails.Any())
        {
            bool allSuccess = true;
            foreach (var orderDetail in orderDetails.ToList())
            {
                var result = await _dineInOrderServiceFront.TransferDineInOrderAsync(orderDetail.DatabaseId, newTableId.Value, newTableName);
                if (result)
                {
                    orderDetail.RelatedTableId = newTableId;
                    orderDetail.RelatedTableName = newTableName;
                    
                    if (!_commonProperties.DineInOrdersDetails.ContainsKey(newTableId.Value))
                        _commonProperties.DineInOrdersDetails[newTableId.Value] = new List<DineInOrderDetails>();
                    
                    _commonProperties.DineInOrdersDetails[newTableId.Value].Add(orderDetail);
                    orderDetails.Remove(orderDetail);
                }
                else allSuccess = false;
            }

            if (orderDetails.Count == 0)
                _commonProperties.DineInOrdersDetails.Remove(oldTableId ?? 0);
            
            return allSuccess;
        }
        return false;
    }

    private bool IsTransferDisabled => SelectedCurrentTable == null || SelectedAvailableTable == null;

    private void CloseDialog() => _commonProperties.DialogReference?.Close();
}