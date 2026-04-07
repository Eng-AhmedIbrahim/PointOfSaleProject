namespace POS.API.Hubs;

public class DeliveryHub : Hub
{
    public async Task SendNewDeliveryOrder(OrderDto orderDto)
        => await Clients.All.SendAsync("ReceiveNewDeliveryOrder", orderDto);

    public async Task SendOrderDispatched(OrderDto orderDto)
        => await Clients.All.SendAsync("ReceiveOrderDispatched", orderDto);

    public async Task SendOrderCollected(OrderDto orderDto)
        => await Clients.All.SendAsync("ReceiveOrderCollected", orderDto);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            Console.WriteLine($"Client disconnected with error: {exception.Message}");
        else
            Console.WriteLine("Client disconnected gracefully.");

        await base.OnDisconnectedAsync(exception);
    }
}
