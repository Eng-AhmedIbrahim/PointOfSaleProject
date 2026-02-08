namespace POS.Desktop.Components.DineInComponents;

public partial class MenuButtons
{
    private void CreateDineInOrder()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            UpdateCurrentDineInOrder(orderDetails);
            _navigationManager.NavigateTo("/pos");
        }
        else
        {
            _commonProperties.UpdateDineInOrder = false;
            _commonProperties.AppendedTableItems?.Clear();
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
        _commonProperties.TableItems!.Clear();
        _commonProperties.CurrentDineInOrder = null;
        _commonProperties.DineInOrderValues = new();
        _commonProperties.UpdateDineInOrder = false;
        _commonProperties.OrderDiscount = new();
        _commonProperties.CurrentPosMode = PosModes.TakeAway.ToString();
        _cartService.UpdateFinanceSettingsByMode("TakeAway");
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
 
    private async Task OpenMergeTablesDialog()
        => _commonProperties.DialogReference = await _dialogService.ShowAsync<MergeTables>("Merge Tables");

    private async Task TransferTable()
        => _commonProperties.DialogReference = await _dialogService.ShowAsync<TransferTable>("Transfer Table");

    [Inject] private Section4ButtonsServices _section4ButtonsServices { get; set; } = default!;
    [Inject] private IPrintOrderService _printOrderService { get; set; } = default!;

    private async Task PrintReceipt()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            await _printOrderService.PrintInitialDineInOrder(orderDetails, true, false);
            
            var newCount = await _dineInOrderService.IncrementPrintCountAsync(orderDetails.DatabaseId);
            // Update the in-memory PrintCount to reflect the database change (newCount is the updated value)
            orderDetails.PrintCount = newCount; 
            StateHasChanged();

            _snackbar.Add(Localizer["PrintingReceipt"], Severity.Info);
        }
        else
        {
            _snackbar.Add(Localizer["NoOrderForTable"], Severity.Warning);
        }
    }

    private async Task OpenSplitOrderDialog()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            var parameters = new DialogParameters { ["OrderToSplit"] = orderDetails };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<SplitOrderDialog>("Split Order", parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                // Clear active table state to force a clean refresh
                _commonProperties.CurrentDineInOrder = null;
                _commonProperties.DineInOrderValues = new();
                _commonProperties.TableItems = new List<TableItem>(); // Reset to empty list
                _commonProperties.AppendedTableItems?.Clear();
                
                _commonProperties.NotifyStateChanged(); // Explicitly notify MainLayout
                _section4ButtonsServices.NotifyStateChanged();
                // Navigate without forceLoad to avoid "Leave site" browser prompt
                _navigationManager.NavigateTo("/dineIn");
            }
        }
        else
        {
            _snackbar.Add(Localizer["NoOrderForTable"], Severity.Warning);
        }
    }

    private async Task OpenVoidDialog()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            var parameters = new DialogParameters { ["OrderId"] = orderDetails.DatabaseId };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true }; // Increased size slightly
            var dialog = _dialogService.Show<VoidOrderDialog>(Localizer["VoidItems"], parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                _commonProperties.CurrentDineInOrder = null;
                _commonProperties.DineInOrderValues = new();
                _commonProperties.TableItems = new List<TableItem>();
                
                _commonProperties.NotifyStateChanged();
                _section4ButtonsServices.NotifyStateChanged();
                _navigationManager.NavigateTo("/dineIn");
            }
        }
        else
        {
            _snackbar.Add(Localizer["NoOrderForTable"], Severity.Warning);
        }
    }

    private async Task OpenGuestCountDialog()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            var parameters = new DialogParameters { ["OrderId"] = orderDetails.DatabaseId };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = false };
            var dialog = _dialogService.Show<GuestCountDialog>("Guest Count", parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                // Optional: refresh order details if needed, but not strictly required for guest count unless UI shows it
                _snackbar.Add("Guests updated", Severity.Success);
            }
        }
        else
        {
             _snackbar.Add(Localizer["NoOrderForTable"], Severity.Warning);
        }
    }

    private async Task CloseTable()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            // Check setting: Must print before close?
            var mustPrint = _commonProperties.DineInSettings?.CanCloseWithoutPrint == false;
            
            // Fetch latest order from DB to check PrintCount
            var dbOrder = await _dineInOrderService.GetDineInOrderByIdAsync(orderDetails.DatabaseId);
            
            if (mustPrint && (dbOrder?.PrintCount ?? 0) == 0)
            {
                _snackbar.Add(Localizer["MustPrintBeforeClose"], Severity.Warning);
                return;
            }

            var result = await _dineInOrderService.CloseDineInOrderAsync(orderDetails.DatabaseId);
            if (result)
            {
                await _printOrderService.PrintDineInClosingReceipt(orderDetails);

                var tableOrders = _commonProperties.DineInOrdersDetails![_commonProperties.TableId];
                tableOrders.Remove(orderDetails);
                if (!tableOrders.Any())
                {
                    _commonProperties.DineInOrdersDetails.Remove(_commonProperties.TableId);
                }
                
                _commonProperties.CurrentDineInOrder = null;
                _commonProperties.DineInOrderValues = new();
                _commonProperties.TableItems = new List<TableItem>(); // Reset to avoid NaveLock warning
                
                _commonProperties.NotifyStateChanged(); // Explicitly notify MainLayout
                _section4ButtonsServices.NotifyStateChanged();

                _snackbar.Add(Localizer["TableClosed"], Severity.Success);
                StateHasChanged();
                _navigationManager.NavigateTo("/dineIn");
            }
            else
            {
                _snackbar.Add(Localizer["FailedToCloseTable"], Severity.Error);
            }
        }
    }

    private bool IsCloseDisabled()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails == null) return true;
        
        var mustPrint = _commonProperties.DineInSettings?.CanCloseWithoutPrint == false;
        if (!mustPrint) return false;
        
        // Use the in-memory PrintCount which should be updated after printing
        return (orderDetails?.PrintCount ?? 0) == 0;
    }
}