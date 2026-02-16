using BlazorBase.ERPFrontServices.DistributionServices;

namespace POS.Desktop.Components.DineInComponents;

public partial class VoidOrderDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    
    // For DineIn mode (order already saved in database)
    [Parameter] public int OrderId { get; set; }
    
    // For TakeAway/Delivery mode (order not yet saved)
    [Parameter] public List<TableItem>? TableItems { get; set; }
    [Parameter] public bool IsDineIn { get; set; } = true;
    [Parameter] public bool IsDistribution { get; set; } = false;

    [Inject] public IDineInOrderFrontService _dineInOrderFrontService { get; set; } = default!;
    [Inject] public IDistributionErpService _distributionErpService { get; set; } = default!;
    [Inject] public ISnackbar _snackbar { get; set; } = default!;
    [Inject] public LocalizationService _localizer { get; set; } = default!;
    [Inject] public CommonProperties _commonProperties { get; set; } = default!;

    public OrderDto? DistributionOrder { get; set; }
    public DineInOrderDto? Order { get; set; } 
    public List<VoidItemModel> Items { get; set; } = new();
    public VoidItemModel? SelectedItem { get; set; }
    public string NumpadInput { get; set; } = "0";
    public string SelectedReason { get; set; } = "MISTAKE";
    public string CustomReason { get; set; } = string.Empty;
    private string Reason => SelectedReason == "OTHER" ? CustomReason : SelectedReason;

    private decimal TotalVoidAmount => Items.Sum(i => i.QuantityToVoid * i.UnitPrice);

    protected override async Task OnInitializedAsync()
    {
        // Distribution mode: Load from provided OrderId using Distribution service
        if (IsDistribution && OrderId > 0)
        {
            var orders = await _distributionErpService.GetUnCompletedDeliveryOrders();
            DistributionOrder = orders.FirstOrDefault(o => o.OrderId == OrderId);
            
            if (DistributionOrder?.OrderDetails != null)
            {
                Items = DistributionOrder.OrderDetails
                    .Where(x => !x.IsVoided)
                    .Select(x => new VoidItemModel
                    {
                        DistributionItem = x,
                        OriginalQuantity = x.Quantity,
                        QuantityToVoid = 0,
                        UnitPrice = x.Price ?? 0,
                        ItemName = _commonProperties!.Language == "ar" ? x.NameAr : x.Name
                    }).ToList();
            }
        }
        // DineIn mode: Load from database
        else if (IsDineIn && OrderId > 0)
        {
            Order = await _dineInOrderFrontService.GetDineInOrderByIdAsync(OrderId);
            
            if (Order?.OrderDetails != null)
            {
                Items = Order.OrderDetails
                    .Where(x => !(x.IsVoided ?? false)) // Exclude already voided items
                    .Select(x => new VoidItemModel
                    {
                        OriginalItem = x,
                        OriginalQuantity = x.Quantity ?? 0,
                        QuantityToVoid = 0,
                        UnitPrice = x.Price ?? 0,
                        ItemName = _commonProperties!.Language == "ar" ? x.ItemNameAr : x.ItemName 
                    }).ToList();
            }
        }
        // TakeAway/Delivery mode: Use provided items
        else if (!IsDineIn && TableItems != null)
        {
            Items = TableItems.Select(x => new VoidItemModel
            {
                TableItem = x,
                OriginalQuantity = x.Quantity,
                QuantityToVoid = 0,
                UnitPrice = x.Price ?? 0,
                ItemName = _commonProperties!.Language == "ar" ? x.NameAr : x.Name
            }).ToList();
        }
    }

    public async Task OnRowClick(TableRowClickEventArgs<VoidItemModel> args)
    {
        SelectedItem = args.Item;
        NumpadInput = SelectedItem.QuantityToVoid.ToString();
        await Task.CompletedTask;
    }

    private void OnNumpadClick(string key)
    {
        if (SelectedItem == null)
        {
            _snackbar.Add(_localizer["PleaseSelectItemFirst"], Severity.Warning);
            return;
        }

        // The instruction removes the '.' check. Assuming it's no longer needed or handled elsewhere.

        if (NumpadInput == "0" || NumpadInput == "")
        {
            NumpadInput = key;
        }
        else
        {
            NumpadInput += key;
        }

        UpdateVoidQuantity();
    }

    private void Backspace()
    {
        if (SelectedItem == null) return;

        if (NumpadInput.Length > 1)
        {
            NumpadInput = NumpadInput.Substring(0, NumpadInput.Length - 1);
        }
        else
        {
            NumpadInput = "0";
        }

        UpdateVoidQuantity();
    }

    private void ClearQuantity()
    {
        if (SelectedItem == null) return;
        NumpadInput = "0";
        UpdateVoidQuantity();
    }

    private void UpdateVoidQuantity()
    {
        if (int.TryParse(NumpadInput, out int qty))
        {
            if (qty > SelectedItem.OriginalQuantity)
            {
                _snackbar.Add($"{_localizer["CannotVoidMoreThan"]} ({SelectedItem.OriginalQuantity})", Severity.Warning);
                NumpadInput = SelectedItem.OriginalQuantity.ToString();
                SelectedItem.QuantityToVoid = SelectedItem.OriginalQuantity;
            }
            else
            {
                SelectedItem.QuantityToVoid = qty;
            }
        }
        else
        {
            // If NumpadInput is not a valid integer (e.g., empty string after backspace),
            // set quantity to 0 to avoid invalid state.
            SelectedItem.QuantityToVoid = 0;
        }
    }

    private void VoidAll()
    {
        foreach (var item in Items)
        {
            item.QuantityToVoid = item.OriginalQuantity;
        }
        _snackbar.Add(_localizer["AllItemsSelectedForVoid"], Severity.Info);
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            _snackbar.Add(_localizer["VoidReasonRequired"], Severity.Error);
            return;
        }

        var itemsToVoid = Items.Where(i => i.QuantityToVoid > 0).ToList();
        if (!itemsToVoid.Any())
        {
            _snackbar.Add(_localizer["NoItemsSelectedToVoid"], Severity.Warning);
            return;
        }

        // Distribution mode: Save to database using Distribution service
        if (IsDistribution && DistributionOrder != null)
        {
            bool result;
            string voidBy = _commonProperties.CurrentUserId ?? "System";
            string voidByName = _commonProperties.CurrentUser ?? "System";

            bool isFullVoid = itemsToVoid.Count == Items.Count && itemsToVoid.All(i => i.QuantityToVoid == i.OriginalQuantity);

            if (isFullVoid)
            {
                result = await _distributionErpService.VoidOrder(DistributionOrder.OrderId, Reason, voidBy, voidByName);
            }
            else
            {
                var voidDtos = itemsToVoid.Select(i => new OrderItemVoidDto(i.DistributionItem!.Id, i.QuantityToVoid)).ToList();
                result = await _distributionErpService.VoidItems(DistributionOrder.OrderId, voidDtos, Reason, voidBy, voidByName);
            }

            if (result)
            {
                _snackbar.Add(_localizer["VoidOperationSuccessful"], Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                _snackbar.Add(_localizer["FailedToVoid"], Severity.Error);
            }
        }
        // DineIn mode: Save to database
        else if (IsDineIn && Order != null)
        {
            bool result;
            string voidBy = _commonProperties.CurrentUserId ?? "System";
            string voidByName = _commonProperties.CurrentUser ?? "System";

            bool isFullVoid = itemsToVoid.Count == Items.Count && itemsToVoid.All(i => i.QuantityToVoid == i.OriginalQuantity);

            if (isFullVoid)
            {
                result = await _dineInOrderFrontService.VoidDineInOrderAsync(Order.Id, Reason, voidBy, voidByName);
            }
            else
            {
                // Use simple class name now that ambiguity is resolved
                var voidDtos = itemsToVoid.Select(i => new OrderItemVoidDto(i.OriginalItem!.Id, i.QuantityToVoid)).ToList();
                result = await _dineInOrderFrontService.VoidDineInItemsAsync(Order.Id, voidDtos, Reason, voidBy, voidByName);
            }

            if (result)
            {
                _snackbar.Add(_localizer["VoidOperationSuccessful"], Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                _snackbar.Add(_localizer["FailedToVoid"], Severity.Error);
            }
        }
        // TakeAway/Delivery mode: Return voided items to caller
        else if (!IsDineIn)
        {
            // Return the list of items to void so the caller can handle them
            var voidedItems = itemsToVoid.Select(i => new
            {
                Item = i.TableItem,
                QuantityToVoid = i.QuantityToVoid,
                Reason = Reason
            }).ToList();
            
            _snackbar.Add(_localizer["VoidOperationSuccessful"], Severity.Success);
            MudDialog.Close(DialogResult.Ok(voidedItems));
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

public class VoidItemModel
{
    // For DineIn mode
    public OrderItemsDetailsDto? OriginalItem { get; set; }
    
    // For Distribution mode
    public TableItem? DistributionItem { get; set; }
    
    // For TakeAway/Delivery mode (unsaved)
    public TableItem? TableItem { get; set; }
    
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int OriginalQuantity { get; set; }
    public int QuantityToVoid { get; set; }
    public decimal TotalAmount => QuantityToVoid * UnitPrice;
}

