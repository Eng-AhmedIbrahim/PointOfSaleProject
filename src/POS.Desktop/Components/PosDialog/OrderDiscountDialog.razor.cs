using Microsoft.AspNetCore.Components;
using MudBlazor;
using POS.Contract.Models;
using BlazorBase.Models;

namespace POS.Desktop.Components.PosDialog;

public partial class OrderDiscountDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;

    private string DiscountType { get; set; } = "Percentage";
    private decimal DiscountPercentage { get; set; } = 0;
    private decimal DiscountValue { get; set; } = 0;
    private DiscountReason SelectedReason { get; set; } = DiscountReason.LoyaltyReward;

    protected override void OnInitialized()
    {
        if (_commonProperties.OrderDiscount != null && !string.IsNullOrEmpty(_commonProperties.OrderDiscount.DiscountType))
        {
            DiscountType = _commonProperties.OrderDiscount.DiscountType.Equals("value", StringComparison.OrdinalIgnoreCase) ? "Value" : "Percentage";
            DiscountPercentage = _commonProperties.OrderDiscount.Percentage;
            DiscountValue = _commonProperties.OrderDiscount.Value;
            if (Enum.TryParse<DiscountReason>(_commonProperties.OrderDiscount.DiscountReason, out var reason))
            {
                SelectedReason = reason;
            }
        }
    }

    private void OnModeChanged(string mode)
    {
        DiscountType = mode;
        StateHasChanged();
    }

    private void ApplyDiscount()
    {
        _commonProperties.OrderDiscount = new OrderDiscount()
        {
            DiscountType = DiscountType,
            Percentage = DiscountType == "Percentage" ? DiscountPercentage : 0M,
            Value = DiscountType == "Value" ? DiscountValue : 0M,
            DiscountReason = SelectedReason.ToString()
        };

        _cartService.CalculateSection4Table();
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void RemoveDiscount()
    {
        _cartService.ResetDiscount();
        MudDialog.Close(DialogResult.Ok(true));
    }
}
