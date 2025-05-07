namespace BlazorBase.ERPFrontServices.OrderServices;
public class OrderSettingsService : IOrderSettingsService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<OrderSettingsService> _logger;

    public OrderSettingsService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<OrderSettingsService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<OrderDto?> CreateOrderAsync(OrderDto orderDto)
    {
        return await GetApiResponseAsync<OrderDto>(
            () => CreateOrderRequest(orderDto),
            "Failed to create order via the API."
        );
    }

    public async Task<ICollection<OrderSettingToReturnDto>?> GetOrderSettingsAsync()
    {
        return await GetApiResponseAsync<ICollection<OrderSettingToReturnDto>>(
            GetOrderSettingsRequest,
            "Failed to retrieve Order Settings from the API."
        );
    }

    private async Task<T> GetApiResponseAsync<T>(
        Func<Task<HttpResponseMessage>> apiRequest,
        string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return default!;
        }

        var content = await response.Content.ReadAsStringAsync();
        T? items = ApiRequestHelpers.DeserializeResponseContent<T>(content);

        return items!;
    }

    private Task<HttpResponseMessage> CreateOrderRequest(OrderDto orderDto)
      => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateOrder!, orderDto);

    private Task<HttpResponseMessage> GetOrderSettingsRequest()
     => _httpClient.GetAsync(_apiSettings.Endpoints!.GetOrderSettings!);

}