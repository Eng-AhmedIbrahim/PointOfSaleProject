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
        _httpClient = httpClientFactory.CreateClient(_apiSettings!.ApiName!);
    }

    public async Task<ICollection<OrderSettingToReturnDto>> GetOrderSettingsAsync()
    {
        return await GetApiResponseAsync<OrderSettingToReturnDto>(
              GetOrderSettingsRequest,
              "Failed to retrieve Order Settings from the API."
          );
    }


    private async Task<ICollection<T>> GetApiResponseAsync<T>(
       Func<Task<HttpResponseMessage>> apiRequest,
       string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return [];
        }

        var content = await response.Content.ReadAsStringAsync();
        var items = ApiRequestHelpers.DeserializeResponseContent<List<T>>(content);

        return items ?? [];
    }

    private Task<HttpResponseMessage> GetOrderSettingsRequest()
        => _httpClient.GetAsync(_apiSettings!.Endpoints!.GetOrderSettings);


}