namespace POS.Desktop.Components.Pages;

public partial class WaitingPage
{
    public int CurrentOrderId { get; set; }
    public List<TableItem>? Items { get; set; }
    private void ShowWaitingOrder(int orderId)
    {
        CurrentOrderId = orderId;
        Items = _commonProperties!.WaitingQueue!.WaitingOrders
        .Where(o => o.Id == orderId)
        .Select(o => o.Items.Select(item =>
        {
            var newItem = item.Clone();
            newItem.IsReadOnly = true;
            return newItem;
        }).ToList())
        .FirstOrDefault() ?? new();
    }

    private Task OnWaitingItemsChanged()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void RemoveWaitingOrder()
    {
        var orderToRemove = _commonProperties!.WaitingQueue!.WaitingOrders.FirstOrDefault(o => o.Id == CurrentOrderId);
        if (orderToRemove != null)
        {
            _commonProperties.WaitingQueue.WaitingOrders.Remove(orderToRemove);
        }

        Items = new List<TableItem>();

        StateHasChanged();
    }

    private async Task CompleteWaitingOrder()
    {
        WaitingOrder? waitingOrder = _commonProperties!.WaitingQueue!.WaitingOrders.FirstOrDefault(o => o.Id == CurrentOrderId);
        _commonProperties.TableItems = waitingOrder!.Items;
        _commonProperties!._financeSettingsList![0].Value = _commonProperties.TableItems.Sum(i => i.Total ?? 0);

        RemoveWaitingOrder();
        await SafeNavigateAsync("/pos");
    }

    private async Task BackToPos()
    {
        await SafeNavigateAsync("/pos");
    }

    private async Task SafeNavigateAsync(string uri)
    {
        int maxRetries = 5;
        int currentRetry = 0;
        int delayMs = 50;

        while (currentRetry < maxRetries)
        {
            try
            {
                await Task.Delay(delayMs);
                if (_navigationManager != null && !string.IsNullOrEmpty(_navigationManager.Uri))
                {
                    await InvokeAsync(() => _navigationManager.NavigateTo(uri, forceLoad: false));
                    return;
                }
                else
                {
                    throw new InvalidOperationException("NavigationManager not yet initialized");
                }
            }
            catch (InvalidOperationException)
            {
                currentRetry++;
                if (currentRetry >= maxRetries) return;
                delayMs *= 2;
            }
        }
    }
}