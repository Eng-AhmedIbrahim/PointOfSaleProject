namespace POS.Desktop.Components.DineInComponents;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using BlazorBase;
using BlazorBase.Models;
using BlazorBase.ERPFrontServices.DineInOrderServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class VoidOrderDialog : ComponentBase
{
    [CascadingParameter] MudBlazor.IMudDialogInstance MudDialog { get; set; }
    [Parameter] public DineInOrderDetails? OrderToVoid { get; set; }
    [Parameter] public List<TableItem>? Items { get; set; }
    [Parameter] public bool IsDineIn { get; set; } = true;

    private string VoidReason = string.Empty;
    private Dictionary<int, int> VoidQuantities = new();

    protected override void OnInitialized()
    {
        var targetItems = IsDineIn ? OrderToVoid?.BasicOrderDetails?.Items : Items;

        if (targetItems != null)
        {
            foreach (var item in targetItems)
            {
                int key = IsDineIn ? item.DatabaseId : item.Id;
                VoidQuantities[key] = 0;
            }
        }
    }

    public int GetVoidKey(TableItem item) => IsDineIn ? item.DatabaseId : item.Id;

    private int GetVoidQuantity(int key) => VoidQuantities.ContainsKey(key) ? VoidQuantities[key] : 0;

    private void SetVoidQuantity(int key, int quantity)
    {
        VoidQuantities[key] = quantity;
    }

    private bool CanVoid => !string.IsNullOrWhiteSpace(VoidReason) && VoidQuantities.Values.Any(v => v > 0);

    public List<TableItem>? TargetItems => IsDineIn ? OrderToVoid?.BasicOrderDetails?.Items : Items;

    private async Task PerformVoid()
    {
        if (!CanVoid) return;

        if (IsDineIn && OrderToVoid != null)
        {
            var itemsToVoid = VoidQuantities
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new OrderItemVoidDto(kvp.Key, kvp.Value))
                .ToList();

            var success = await _dineInOrderService.VoidDineInItemsAsync(
                OrderToVoid.DatabaseId, 
                itemsToVoid, 
                VoidReason, 
                _commonProperties.CurrentUser ?? "System");

            if (success)
            {
                _snackbar.Add("Items voided successfully!", MudBlazor.Severity.Success);
                _commonProperties.NotifyStateChanged();
                MudDialog.Close(MudBlazor.DialogResult.Ok(true));
            }
            else
            {
                _snackbar.Add("Failed to void items.", MudBlazor.Severity.Error);
            }
        }
        else if (!IsDineIn && Items != null)
        {
             var itemsToRemove = VoidQuantities.Where(k => k.Value > 0).ToList();
             foreach (var kvp in itemsToRemove)
             {
                 var item = Items.FirstOrDefault(i => i.Id == kvp.Key);
                 if (item != null)
                 {
                     if (kvp.Value >= item.Quantity)
                     {
                         Items.Remove(item);
                     }
                     else
                     {
                         item.Quantity -= kvp.Value;
                         item.Total = item.Quantity * item.Price; 
                     }
                 }
             }
             
             _snackbar.Add("Items voided locally!", MudBlazor.Severity.Success);
             _commonProperties.NotifyStateChanged();
             MudDialog.Close(MudBlazor.DialogResult.Ok(true));
        }
    }

    private void CloseDialog() => MudDialog.Cancel();
}
