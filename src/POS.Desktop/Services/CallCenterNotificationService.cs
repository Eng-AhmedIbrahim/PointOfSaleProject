using BlazorBase.ERPFrontServices.SettingsServices;

namespace POS.Desktop.Services;

public class CallCenterNotificationService : IDisposable, IAsyncDisposable
{
    private readonly CallCenterHubSettings _hubSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HandelDeliveryInvocation _deliveryInvocation;
    private readonly DispatcherSettings _dispatcherSettings;
    private readonly List<HubConnection> _connections = new();

    public CallCenterNotificationService(
        CallCenterHubSettings hubSettings, 
        DispatcherSettings dispatcherSettings,
        IServiceScopeFactory scopeFactory,
        HandelDeliveryInvocation deliveryInvocation)
    {
        _hubSettings = hubSettings;
        _dispatcherSettings = dispatcherSettings;
        _scopeFactory = scopeFactory;
        _deliveryInvocation = deliveryInvocation;
    }

    public async Task InitializeAsync()
    {
        if (_hubSettings.Urls == null || !_hubSettings.Urls.Any())
            return;

        foreach (var url in _hubSettings.Urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;

            var connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            connection.On<OrderDto>("ReceiveNewDeliveryOrder", (order) =>
            {
                // Prevent duplicate notifications and self-printing at the sending Call Center.
                if (order.MachineName == Environment.MachineName) 
                    return;

                if (_dispatcherSettings.SoundEnableCallCenter)
                    System.Media.SystemSounds.Asterisk.Play();

                _deliveryInvocation.TriggerShowNotification($"جديد: طلب توصيل برقم {order.OrderId} من {order.CustomerName}", Severity.Info);
                
                _deliveryInvocation.TriggerNewOrderReceived();

                // Get latest settings from DB/Service
                Task.Run(async () =>
                {
                    try
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var settingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsServices>();
                            var settings = await settingsService.GetDispatcherSettingsAsync();

                            if (settings.IsDispatcher)
                            {
                                var printService = scope.ServiceProvider.GetRequiredService<IPrintOrderService>();
                                await printService.PrintReceivedOrderAsync(order);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to print order {order.OrderId} automatically: {ex.Message}");
                    }
                });
            });

            connection.On<OrderDto>("ReceiveOrderDispatched", (order) =>
            {
                _deliveryInvocation.TriggerShowNotification($"تم خروج الطلب رقم {order.OrderId} مع السائق {order.DriverName}", Severity.Success);
                _deliveryInvocation.TriggerNewOrderReceived(); // Refresh UI if needed
            });

            connection.On<OrderDto>("ReceiveOrderCollected", (order) =>
            {
                _deliveryInvocation.TriggerShowNotification($"تم تسليم الطلب رقم {order.OrderId} بنجاح", Severity.Info);
                _deliveryInvocation.TriggerNewOrderReceived(); // Refresh UI if needed
            });

            connection.On<OrderDto>("OrderDispatchedCentralNotification", (order) =>
            {
                if (_dispatcherSettings.SoundEnableCallCenter)
                    System.Media.SystemSounds.Asterisk.Play();

                _deliveryInvocation.TriggerShowNotification($"تم إرسال الطلب رقم {order.OrderId} للفرع بنجاح", Severity.Success);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            connection.On<OrderDto, string>("OrderDispatchFailedCentralNotification", (order, error) =>
            {
                if (_dispatcherSettings.SoundEnableCallCenter)
                    System.Media.SystemSounds.Exclamation.Play();

                _deliveryInvocation.TriggerShowNotification($"فشل إرسال الطلب رقم {order.OrderId} للفرع: {error}", Severity.Error);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            try
            {
                await connection.StartAsync();
                _connections.Add(connection);
            }
            catch (Exception ex)
            {
                // Log and ignore
                Console.WriteLine($"Failed to connect to {url}: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            // Sync disposal for HubConnection is complex, but we can fire and forget or block
            // Here we use GetAwaiter().GetResult() as we are in a shutdown context
            try { connection.DisposeAsync().GetAwaiter().GetResult(); } catch { }
        }
        _connections.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            await connection.DisposeAsync();
        }
        _connections.Clear();
    }
}
