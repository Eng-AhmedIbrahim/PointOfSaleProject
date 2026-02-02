namespace POS.Desktop.Components.DineInComponents;

public partial class MenuButtons
{
    private void CreateDineInOrder()
    {
        if (_commonProperties!.DineInOrdersDetails!.TryGetValue(_commonProperties.TableId, out DineInOrderDetails? OrderDetails))
        {

            UpdateCurrentDineInOrder(OrderDetails);
            _navigationManager.NavigateTo("/pos");
        }
        else
        {
            BackUpDineInOrder();

            if (_commonProperties!.CurrentDineInOrder!.CaptainName is null || _commonProperties.CurrentDineInOrder.RelatedTableName is null)
            {
                _snackbar.Add("Please Select Table and Captain", Severity.Error);
                return;
            }

            _navigationManager.NavigateTo("/pos");
        }
    }
    private void BackUpDineInOrder()
    {
        _commonProperties!.CurrentDineInOrder = new DineInOrderDetails
        {
            CaptainName = _commonProperties!.CurrentDineInOrder!.CaptainName,
            CaptainId = _commonProperties.CurrentDineInOrder.CaptainId,
            RelatedTableId = _commonProperties.CurrentDineInOrder.RelatedTableId,
            RelatedTableName = _commonProperties.CurrentDineInOrder.RelatedTableName,
            BasicOrderDetails = new BlazorBase.Models.OrderDetails
            {
                CashierName = _commonProperties.CurrentUser,
                Tax = _commonProperties!.DineInSettings!.Tax,
                Service = _commonProperties.DineInSettings.Service,
                Items = new List<TableItem>(),
                OrderDiscount = new()
            }
        };
    }
    private void BackToPos()
    {
        _navigationManager.NavigateTo("/pos");
        _commonProperties.AppendedTableItems!.Clear();
        _commonProperties.CurrentPosMode = PosModes.TakeAway.ToString();
    }
    private void UpdateCurrentDineInOrder(DineInOrderDetails dineInOrderDetails)
    {
        _commonProperties.UpdateDineInOrder = true;

        _commonProperties.TableItems = new List<TableItem>(
            dineInOrderDetails!.BasicOrderDetails!.Items.Select(item =>
            {
                var newItem = item.Clone();
                newItem.IsReadOnly = true;
                return newItem;
            })
        );
        _commonProperties.OrderDiscount = dineInOrderDetails.BasicOrderDetails.OrderDiscount;
        if (dineInOrderDetails.BasicOrderDetails.OrderDiscount.DiscountType == "percentage")
            _commonProperties!._financeSettingsList![1].Value = dineInOrderDetails.BasicOrderDetails.OrderDiscount.Percentage;
        else
            _commonProperties!._financeSettingsList![1].Value = dineInOrderDetails.BasicOrderDetails.OrderDiscount.Value;

        _commonProperties._financeSettingsList[4].Value = dineInOrderDetails.BasicOrderDetails.Total;
    }
 
    public void MergeTables(int primaryTableId, List<int> mergingTableIds)
    {
        if (!_commonProperties.DineInOrdersDetails!.ContainsKey(primaryTableId) || mergingTableIds.Count == 0)
            return;

        var primaryOrder = _commonProperties.DineInOrdersDetails[primaryTableId];

        foreach (var tableId in mergingTableIds)
        {
            if (_commonProperties.DineInOrdersDetails.TryGetValue(tableId, out var mergingOrder))
            {
                if (mergingOrder.BasicOrderDetails != null)
                {
                    primaryOrder.BasicOrderDetails ??= new BlazorBase.Models.OrderDetails();
                    primaryOrder.BasicOrderDetails.Merge(mergingOrder.BasicOrderDetails);
                }

                primaryOrder.RelatedTableName += $"{mergingOrder.RelatedTableName}";

                _commonProperties.DineInOrdersDetails.Remove(tableId);
            }
        }

        // Update the dictionary with the merged order
        _commonProperties.DineInOrdersDetails[primaryTableId] = primaryOrder;
    }

    private async Task OpenMergeTablesDialog()
        => _commonProperties.DialogReference = await _dialogService.ShowAsync<MergeTables>("Merge Tables");

    private async Task TransferTable()
        => _commonProperties.DialogReference = await _dialogService.ShowAsync<TransferTable>("Transfer Table");
}