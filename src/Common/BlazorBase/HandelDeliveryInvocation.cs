using BlazorBase.Models.DeliveryModels;
using MudBlazor;

namespace BlazorBase;

public class HandelDeliveryInvocation
{

    public event Action? OnAddressSelected;
    public event Action? OnToggleDirection;
    public event Action? OnNewOrderReceived;
    public event Action<string, Severity>?OnShowNotification;

    public void TriggerShowNotification(string message, Severity severity)
    {
        OnShowNotification?.Invoke(message, severity);
    }

    public void TriggerAddressSelected()
    {
        OnAddressSelected?.Invoke();
    }
    public void TriggerToggleDirection()
    {
        OnToggleDirection?.Invoke();
    }

    public void TriggerNewOrderReceived()
    {
        OnNewOrderReceived?.Invoke();
    }

    public CustomerDetails? CustomerDetails { get; set; } = new();
    public string? DeliveryDetails { get; set; } = string.Empty;

}