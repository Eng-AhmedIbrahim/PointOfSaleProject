using BlazorBase.Models.DeliveryModels;

namespace BlazorBase;

public class HandelDeliveryInvocation
{
    public event Action? OnAddressSelected;

    public void TriggerAddressSelected()
    {
        OnAddressSelected?.Invoke();
    }

    public CustomerDetails? CustomerDetails { get; set; } = new();
    public string? DeliveryDetails { get; set; } = string.Empty;

}