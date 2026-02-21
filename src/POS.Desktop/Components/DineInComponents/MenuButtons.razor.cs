namespace POS.Desktop.Components.DineInComponents;

public partial class MenuButtons
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private bool _canDineInOrder;
    private bool _canDineInReceipt;
    private bool _canDineInCloseTable;
    private bool _canDineInSplitOrder;
    private bool _canDineInMergeTable;
    private bool _canDineInTransfer;
    private bool _canDineInVoid;
    private bool _canDineInGuestCount;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is { IsAuthenticated: true })
        {
            _canDineInOrder      = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDineInOrderBtn")).Succeeded;
            _canDineInReceipt    = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDineInReceiptBtn")).Succeeded;
            _canDineInCloseTable = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDineInCloseTableBtn")).Succeeded;
            _canDineInSplitOrder = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDineInSplitOrderBtn")).Succeeded;
            _canDineInMergeTable = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDineInMergeTableBtn")).Succeeded;
            _canDineInTransfer   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDineInTransferBtn")).Succeeded;
            _canDineInVoid       = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDineInVoidBtn")).Succeeded;
            _canDineInGuestCount = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDineInGuestCountBtn")).Succeeded;
        }
    }

    private async Task CreateDineInOrder()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            UpdateCurrentDineInOrder(orderDetails);
            await SafeNavigateAsync("/pos");
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

            await SafeNavigateAsync("/pos");
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
    private async Task BackToPos()
    {
        await SafeNavigateAsync("/pos");
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
                await SafeNavigateAsync("/dineIn");
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
            var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Medium, FullWidth = true }; // Increased size slightly
            var dialog = _dialogService.Show<VoidOrderDialog>(Localizer["VoidItems"], parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                _commonProperties.CurrentDineInOrder = null;
                _commonProperties.DineInOrderValues = new();
                _commonProperties.TableItems = new List<TableItem>();
                
                _commonProperties.NotifyStateChanged();
                _section4ButtonsServices.NotifyStateChanged();
                await SafeNavigateAsync("/dineIn");
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

            // Get payment information from BasicOrderDetails
            var paid = orderDetails.BasicOrderDetails?.Paid;
            var remain = orderDetails.BasicOrderDetails?.Remain;

            var result = await _dineInOrderService.CloseDineInOrderAsync(orderDetails.DatabaseId, paid, remain);
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
                await SafeNavigateAsync("/dineIn");
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

    private async Task SafeNavigateAsync(string uri)
    {
        int maxRetries = 5;
        int currentRetry = 0;
        int delayMs = 5;

        while (currentRetry < maxRetries)
        {
            try
            {
                await Task.Delay(delayMs);
                
                // Check if NavigationManager is initialized by safely checking the Uri property
                if (_navigationManager != null)
                {
                    try
                    {
                        // Try to access Uri - if it throws, NavigationManager isn't ready yet
                        var currentUri = _navigationManager.Uri;
                        if (!string.IsNullOrEmpty(currentUri))
                        {
                            // Use InvokeAsync to ensure we're on the correct synchronization context
                            await InvokeAsync(() => _navigationManager.NavigateTo(uri, forceLoad: false));
                            return;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // NavigationManager not yet initialized, will retry
                        throw new InvalidOperationException("NavigationManager not yet initialized");
                    }
                }
                else
                {
                    throw new InvalidOperationException("NavigationManager is null");
                }
            }
            catch (Exception ex)
            {
                currentRetry++;
                
                if (currentRetry >= maxRetries)
                {
                    _snackbar.Add($"Navigation failed: {ex.Message}", Severity.Error);
                    return;
                }
                
                // Exponential backoff
                delayMs *= 2;
            }
        }
    }
}