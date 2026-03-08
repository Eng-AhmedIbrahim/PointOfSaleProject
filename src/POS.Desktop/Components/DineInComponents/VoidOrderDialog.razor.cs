namespace POS.Desktop.Components.DineInComponents;

public partial class VoidOrderDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int OrderId { get; set; }
    [Parameter] public List<TableItem>? TableItems { get; set; }
    [Parameter] public bool IsDineIn { get; set; } = true;
    [Parameter] public bool IsDistribution { get; set; } = false;
    [Parameter] public OrderDto? InitialDistributionOrder { get; set; }
    [Parameter] public DineInOrderDto? InitialDineInOrder { get; set; }
    [Inject] public IDineInOrderFrontService _dineInOrderFrontService { get; set; } = default!;
    [Inject] public IDistributionErpService _distributionErpService { get; set; } = default!;
    [Inject] public IVoidErpService _voidErpService { get; set; } = default!;
    [Inject] public ISnackbar _snackbar { get; set; } = default!;
    [Inject] public LocalizationService _localizer { get; set; } = default!;
    [Inject] public CommonProperties _commonProperties { get; set; } = default!;
    [Inject] public IPrintOrderService _printOrderService { get; set; } = default!;
    public OrderDto? DistributionOrder { get; set; }
    public DineInOrderDto? Order { get; set; }
    public List<VoidItemModel> Items { get; set; } = new();
    public VoidItemModel? SelectedItem { get; set; }
    public string NumpadInput { get; set; } = "0";
    public string CustomReason { get; set; } = string.Empty;
    public bool ReturnToStock { get; set; } = true;
    private string Reason => CustomReason;
    private decimal OriginalOrderTotal 
    {
        get
        {
            var headerTotal = (DistributionOrder?.GrandTotal ?? 0) + (Order?.GrandTotal ?? 0) + (TableItems?.Sum(i => i.Total ?? 0) ?? 0);
            if (headerTotal == 0 && Items.Any())
                return Items.Sum(i => i.OriginalQuantity * i.UnitPrice);
            return headerTotal;
        }
    }
    private decimal TotalVoidAmount => Items.Sum(i => i.TotalAmount);
    private decimal NewOrderTotal => Math.Max(0, OriginalOrderTotal - TotalVoidAmount);

    protected override async Task OnInitializedAsync()
    {
        // 1. Priority: Use pre-loaded order data (avoids extra API calls)
        if (InitialDistributionOrder != null)
        {
            DistributionOrder = InitialDistributionOrder;
            PopulateFromDistribution();
        }
        else if (InitialDineInOrder != null)
        {
            Order = InitialDineInOrder;
            PopulateFromDineIn();
        }
        // 2. Secondary: Fetch from API if only ID is provided (standard flow)
        else if (OrderId > 0)
        {
            if (IsDineIn)
            {
                Order = await _dineInOrderFrontService.GetDineInOrderByIdAsync(OrderId);
                PopulateFromDineIn();
            }
            else
            {
                var orders = await _distributionErpService.GetUnCompletedDeliveryOrders();
                DistributionOrder = orders.FirstOrDefault(o => o.Id == OrderId);
                PopulateFromDistribution();
            }
        }
        // 3. Fallback: Local table items (unsaved orders)
        else if (TableItems != null)
        {
            Items = TableItems.Select(x => new VoidItemModel
            {
                TableItem = x,
                OriginalQuantity = x.Quantity,
                QuantityToVoid = 0,
                UnitPrice = x.Price ?? 0,
                ItemName = _commonProperties?.Language == "ar"
                ? x.NameAr ?? x.Name ?? ""
                : x.Name ?? x.NameAr ?? ""
            }).ToList();
        }

        // Check for dispatched state to warn the user or block
        var orderState = DistributionOrder?.OrderState ?? Order?.OrderState;
        if (orderState == "Dispatched" || orderState == "Delivering")
        {
            _snackbar.Add(_localizer["CannotVoidDispatchedOrder"], Severity.Error);
            MudDialog.Cancel();
        }
    }

    private void PopulateFromDistribution()
    {
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
                    ItemName = _commonProperties?.Language == "ar" 
                    ? x.NameAr ?? x.Name ?? "" 
                    : x.Name ?? x.NameAr ?? ""
                }).ToList();
        }
    }

    private void PopulateFromDineIn()
    {
        if (Order?.OrderDetails != null)
        {
            Items = Order.OrderDetails
                .Where(x => !(x.IsVoided ?? false))
                .Select(x => new VoidItemModel
                {
                    OriginalItem = x,
                    OriginalQuantity = x.Quantity ?? 0,
                    QuantityToVoid = 0,
                    UnitPrice = x.Price ?? 0,
                    ItemName = _commonProperties?.Language == "ar" 
                    ? x.ItemNameAr ?? x.ItemName ?? ""
                    : x.ItemName ?? x.ItemNameAr ?? ""
                }).ToList();
        }
    }

    public async Task OnRowClick(TableRowClickEventArgs<VoidItemModel> args)
    {
        SelectedItem = args.Item;
        NumpadInput = SelectedItem!.QuantityToVoid.ToString();
        await Task.CompletedTask;
    }

    private void OnNumpadClick(string key)
    {
        if (SelectedItem == null)
        {
            _snackbar.Add(_localizer["PleaseSelectItemFirst"], Severity.Warning);
            return;
        }

        if (NumpadInput == "0" || NumpadInput == "")
            NumpadInput = key;
        else
            NumpadInput += key;

        UpdateVoidQuantity();
    }

    private void Backspace()
    {
        if (SelectedItem == null) return;

        if (NumpadInput.Length > 1)
            NumpadInput = NumpadInput.Substring(0, NumpadInput.Length - 1);
        else
            NumpadInput = "0";

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
            if (qty > SelectedItem!.OriginalQuantity)
            {
                _snackbar.Add($"{_localizer["CannotVoidMoreThan"]} ({SelectedItem.OriginalQuantity})", Severity.Warning);
                NumpadInput = SelectedItem.OriginalQuantity.ToString();
                SelectedItem.QuantityToVoid = SelectedItem.OriginalQuantity;
            }
            else
                SelectedItem.QuantityToVoid = qty;
        }
        else
            SelectedItem!.QuantityToVoid = 0;
    }

    private void VoidAll()
    {
        foreach (var item in Items)
            item.QuantityToVoid = item.OriginalQuantity;

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

        string voidBy = _commonProperties.CurrentUserId ?? "System";
        string voidByName = _commonProperties.CurrentUser ?? "System";
        bool isFullVoid = itemsToVoid.Count == Items.Count && itemsToVoid.All(i => i.QuantityToVoid == i.OriginalQuantity);

        if (OrderId > 0)
        {
            bool result;
            if (isFullVoid)
            {
                result = await _voidErpService.VoidOrder(OrderId, Reason, voidBy, voidByName, ReturnToStock);
            }
            else
            {
                var voidDtos = itemsToVoid.Select(i =>
                {
                    int detailId = DistributionOrder != null ? i.DistributionItem!.Id : i.OriginalItem!.Id;
                    return new OrderItemVoidDto(detailId, i.QuantityToVoid);
                }).ToList();

                result = await _voidErpService.VoidItems(OrderId, voidDtos, Reason, voidBy, voidByName, ReturnToStock);
            }

            if (result)
            {
                try
                { 
                    if (IsDineIn && Order != null)
                    {
                        var voidedTableItems = itemsToVoid.Select(i => new TableItem
                        {
                            Id = i.OriginalItem!.MenuSalesItemId ?? 0,
                            DatabaseId = i.OriginalItem.Id,
                            Name = i.OriginalItem.ItemName,
                            NameAr = i.OriginalItem.ItemNameAr,
                            Price = i.OriginalItem.Price,
                            Quantity = i.QuantityToVoid,
                            Total = i.QuantityToVoid * i.OriginalItem.Price,
                            ItemKitchenTypeId = i.OriginalItem.ItemKitchenTypeId,
                            CategoryKitchenTypeId = i.OriginalItem.CategoryKitchenTypeId,
                            CategoryId = i.OriginalItem.CategoryId,
                            CategoryName = i.OriginalItem.CategoryName,
                        }).ToList();

                        await _printOrderService.PrintDineInVoidReceiptAsync(Order, voidedTableItems);
                    }
                    else if (DistributionOrder != null)
                    {
                        var voidedTableItems = itemsToVoid.Select(i =>
                        {
                            var item = i.DistributionItem?.Clone() ?? new TableItem();
                            item.Quantity = i.QuantityToVoid;
                            item.Total = item.Quantity * item.Price;
                            return item;
                        }).ToList();

                        await _printOrderService.PrintVoidReceiptAsync(DistributionOrder, voidedTableItems);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error printing void receipt: {ex.Message}");
                    Log.Error(ex, "Error printing void receipt for OrderId {OrderId}", OrderId);
                }

                _snackbar.Add(_localizer["VoidOperationSuccessful"], Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                _snackbar.Add(_localizer["FailedToVoid"], Severity.Error);
            }
        }
        else
        {
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
