using Microsoft.AspNetCore.Components;
using MudBlazor;
using POS.Contract.Dtos.OrderDtos;

namespace POS.Desktop.Components.DeliveryComponent;

public partial class ActiveOrderDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public OrderDto ActiveOrder { get; set; } = new();

    void CreateNew() => MudDialog.Close(DialogResult.Ok(false));
    void EditExisting() => MudDialog.Close(DialogResult.Ok(true));
}