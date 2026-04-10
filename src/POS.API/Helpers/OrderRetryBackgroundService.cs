using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.OrderServices;
using POS.Core.Services.Contract.CompanyService;
using POS.Contract.Dtos.OrderDtos;
using System.Text.Json;
using System.Text;

namespace POS.API.Helpers;

public class OrderRetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderRetryBackgroundService> _logger;
    private readonly CallCenterSettings _centerSettings;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _retryInterval;

    public OrderRetryBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<OrderRetryBackgroundService> logger,
        CallCenterSettings centerSettings,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _centerSettings = centerSettings;
        _configuration = configuration;
        _retryInterval = TimeSpan.FromMinutes(_centerSettings!.SendRetryIntervalMessagesAfterByMinutes > 0 
            ? _centerSettings.SendRetryIntervalMessagesAfterByMinutes 
            : 1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Retry Service] Starting retry cycle.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    var branchService = scope.ServiceProvider.GetRequiredService<IBranchService>();

                    var failedOrders = await orderService.GetFailedDeliveryOrdersAsync();

                    if (failedOrders != null && failedOrders.Any())
                    {
                        foreach (Orders order in failedOrders)
                        {
                            var branch = await branchService.GetBranchByIdAsync(order.BranchID);

                            if (string.IsNullOrEmpty(branch?.ApiUrl))
                            {
                                _logger.LogWarning("[Retry Service] Branch {BranchID} has no API URL. Skipping order {OrderID}.", order.BranchID, order.OrderID);
                                continue;
                            }

                            try
                            {
                                using var httpClient = new HttpClient();
                                httpClient.Timeout = TimeSpan.FromSeconds(30);

                                var orderDto = await orderService.GetOrderDtoByIdAsync(order.Id);
                                if (orderDto == null) 
                                {
                                    _logger.LogWarning("[Retry Service] Could not fetch DTO for Order ID {OrderId}. Skipping.", order.Id);
                                    continue;
                                }

                                // Get Call Center API URL from configuration (Kestrel URLs)
                                var urls = _configuration["Kestrel:Endpoints:Https:Url"] ?? _configuration["Kestrel:Endpoints:Http:Url"];
                                if (string.IsNullOrEmpty(urls))
                                {
                                    // Fallback: try to construct from server addresses
                                    urls = _configuration.GetValue<string>("ASPNETCORE_URLS") ?? "https://localhost:7142";
                                }
                                orderDto.CallCenterApiUrl = urls.Split(';')[0]; // Take first URL if multiple
                                
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var json = JsonSerializer.Serialize(orderDto, options);
                                var content = new StringContent(json, Encoding.UTF8, "application/json");

                                var cleanBranchUrl = branch.ApiUrl.EndsWith("/") ? branch.ApiUrl.TrimEnd('/') : branch.ApiUrl;
                                var targetEndpoint = $"{cleanBranchUrl}/api/order/receiveDispatchedOrder";

                                _logger.LogInformation("[Retry Service] Retrying Order {OrderID} for Branch {BranchName} at {Endpoint}", order.OrderID, branch.Name, targetEndpoint);

                                var response = await httpClient.PostAsync(targetEndpoint, content);
                                _logger.LogInformation("[Retry Service] Branch responded with StatusCode: {StatusCode} for Order {OrderID}", response.StatusCode, order.OrderID);

                                if (response.IsSuccessStatusCode)
                                {
                                    var branchResponseJson = await response.Content.ReadAsStringAsync();
                                    var branchOrderDto = JsonSerializer.Deserialize<OrderDto>(branchResponseJson, options);

                                    order.OrderState = OrderStates.SentToBranch;
                                    if (branchOrderDto != null)
                                    {
                                        order.CallCenterOrderId = branchOrderDto.Id;
                                    }
                                    await orderService.UpdateOrderAsync(order);
                                    _logger.LogInformation("[Retry Service] SUCCESS: Order {OrderID} sent to branch {BranchName}.", order.OrderID, branch.Name);
                                }
                                else
                                {
                                    var errorContent = await response.Content.ReadAsStringAsync();
                                    _logger.LogWarning("[Retry Service] FAILED: Order {OrderID}. StatusCode: {StatusCode}. Error: {Error}", order.OrderID, response.StatusCode, errorContent);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[Retry Service] EXCEPTION: Order {OrderID} to URL: {Url}", order.OrderID, branch.ApiUrl);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in retry background service cycle.");
                }

                await Task.Delay(_retryInterval, stoppingToken);
        }
    }
}
