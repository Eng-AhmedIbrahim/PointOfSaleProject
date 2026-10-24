using Microsoft.AspNetCore.Components;
using MudBlazor;
using POS.Contract.Dtos.OrderDtos;
using BlazorBase.Services;

namespace POS.Desktop.Components.DeliveryComponent;

public partial class ActiveOrderDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;
    [Inject] LocalizationService Localizer { get; set; } = default!;

    [Parameter] public List<OrderDto> ActiveOrders { get; set; } = new();

    void CreateNew() => MudDialog.Close(DialogResult.Ok(false));
    void EditExisting(OrderDto order) => MudDialog.Close(DialogResult.Ok(order));

    async Task VoidExisting(OrderDto order)
    {
        var parameters = new DialogParameters
        {
            ["OrderId"]                  = order.Id,
            ["IsDineIn"]                 = false,
            ["IsDistribution"]           = true,
            ["InitialDistributionOrder"] = order
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = false };
        var dialog = await DialogService.ShowAsync<BlazorBase.Components.Shared.VoidOrderDialog>(Localizer["VoidOrder"], parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            MudDialog.Close(DialogResult.Ok("Voided"));
        }
    }
}