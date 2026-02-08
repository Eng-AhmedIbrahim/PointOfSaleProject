using Microsoft.AspNetCore.Components;
using MudBlazor;
using POS.Contract.Dtos.DineIn; // Assuming namespace based on previous checks
using BlazorBase.ERPFrontServices.DineInOrderServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace POS.Desktop.Components.DineInComponents;

public partial class VoidOrderDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int OrderId { get; set; }

    private DineInOrderDto Order { get; set; }
    private List<VoidItemModel> Items { get; set; } = new();
    private VoidItemModel SelectedItem { get; set; }
    private string NumpadInput { get; set; } = "0";
    private string Reason { get; set; }

    private decimal TotalVoidAmount => Items.Sum(i => i.QuantityToVoid * i.UnitPrice);

    protected override async Task OnInitializedAsync()
    {
        if (OrderId > 0)
        {
            Order = await DineInOrderFrontService.GetDineInOrderByIdAsync(OrderId);
            
            if (Order?.OrderDetails != null)
            {
                Items = Order.OrderDetails.Select(x => new VoidItemModel
                {
                    OriginalItem = x,
                    OriginalQuantity = x.Quantity ?? 0,
                    QuantityToVoid = 0,
                    UnitPrice = x.Price ?? 0,
                    ItemName = x.ItemName
                }).ToList();
            }
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
            Snackbar.Add("Please select an item first.", Severity.Warning);
            return;
        }

        if (key == ".")
        {
            // Project uses int quantities, decimal points not needed
            return;
        }

        if (NumpadInput == "0")
        {
            NumpadInput = key;
        }
        else
        {
            NumpadInput += key;
        }

        if (int.TryParse(NumpadInput, out int qty))
        {
            if (qty > SelectedItem.OriginalQuantity)
            {
                Snackbar.Add($"Cannot void more than order quantity ({SelectedItem.OriginalQuantity})", Severity.Warning);
                NumpadInput = SelectedItem.OriginalQuantity.ToString();
                SelectedItem.QuantityToVoid = SelectedItem.OriginalQuantity;
            }
            else
            {
                SelectedItem.QuantityToVoid = qty;
            }
        }
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

        if (int.TryParse(NumpadInput, out int qty))
        {
            SelectedItem.QuantityToVoid = qty;
        }
    }

    private void VoidAll()
    {
        foreach (var item in Items)
        {
            item.QuantityToVoid = item.OriginalQuantity;
        }
        Snackbar.Add("All items selected for voiding.", Severity.Info);
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            Snackbar.Add("Void Reason is required.", Severity.Error);
            return;
        }

        var itemsToVoid = Items.Where(i => i.QuantityToVoid > 0).ToList();
        if (!itemsToVoid.Any())
        {
            Snackbar.Add("No items selected to void.", Severity.Warning);
            return;
        }

        bool result;

        bool isFullVoid = itemsToVoid.Count == Items.Count && itemsToVoid.All(i => i.QuantityToVoid == i.OriginalQuantity);

        if (isFullVoid)
        {
            result = await DineInOrderFrontService.VoidDineInOrderAsync(Order.Id);
        }
        else
        {
            var voidDtos = itemsToVoid.Select(i => new OrderItemVoidDto(i.OriginalItem.Id, i.QuantityToVoid)).ToList();
            
            string voidBy = Order.CashierId ?? "Unknown"; 

            result = await DineInOrderFrontService.VoidDineInItemsAsync(Order.Id, voidDtos, Reason, voidBy);
        }

        if (result)
        {
            Snackbar.Add("Void operation successful.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add("Failed to void order/items.", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

public class VoidItemModel
{
    public OrderItemsDetailsDto OriginalItem { get; set; }
    public string ItemName { get; set; }
    public decimal UnitPrice { get; set; }
    public int OriginalQuantity { get; set; }
    public int QuantityToVoid { get; set; }
    public decimal TotalAmount => QuantityToVoid * UnitPrice;
}

