namespace BlazorBase.ERPFrontServices.DeliveryServices;

public class DeliveryServices : IDeliveryServices
{

    private readonly ApiSettings _apiSettings;
    private readonly ILogger<DeliveryServices> _logger;
    private readonly HttpClient _httpClient;

    public DeliveryServices(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<DeliveryServices> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<IReadOnlyList<DeliveryTitleToReturnDto>> GetAllDeliveryCustomerTitlesAsync()
    {
        return await GetApiResponseAsync<List<DeliveryTitleToReturnDto>>(
          GetAllDeliveryCustomerTitlesRequest,
          "Failed to retrieve Title from the API."
      )??new();
    }

    public async Task<IReadOnlyList<DeliveryZonesToReturnDto>> GetDeliveryZoneByBranchAsync(int branchId)
    {
        return await GetApiResponseAsync<List<DeliveryZonesToReturnDto>>(
        () => GetDeliveryZoneByBranchRequest(branchId),
        "Failed to retrieve Zones from the API."
    ) ?? new List<DeliveryZonesToReturnDto>();
    }


    public async Task<DeliveryCustomerToReturnDto> GetClientByPhoneNumberAsync(string phoneNumber)
    {
            return await GetApiResponseAsync<DeliveryCustomerToReturnDto>(
            () => GetClientByPhoneNumberRequest(phoneNumber),
            "Failed to retrieve Zones from the API."
        )??new();
    }


    public async Task<DeliveryCustomerDto> CreateClientAsync(DeliveryCustomerDto deliveryCustomer)
    {
        return await GetApiResponseAsync<DeliveryCustomerDto>(
        () => CreateClientAsyncRequest(deliveryCustomer),
        "Failed to Create Client from the API."
        ) ?? new();
    }

    public async Task<CustomerNewAddressDto> AddNewCustomerAddressAsync(CustomerNewAddressDto newDeliveryCustomerAddress)
    {
        return await GetApiResponseAsync<CustomerNewAddressDto>(
         () => AddNewCustomerAddressRequest(newDeliveryCustomerAddress),
         "Failed to Add New Customer Address from the API."
        ) ?? new();
    }

    private async Task<HttpResponseMessage> AddNewCustomerAddressRequest(CustomerNewAddressDto newDeliveryCustomerAddress)
    => await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.AddNewCustomerAddress, newDeliveryCustomerAddress);

    private async Task<HttpResponseMessage> GetClientByPhoneNumberRequest(string phoneNumber)
     => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetCustomerByPhone}/{phoneNumber}");

    private async Task<HttpResponseMessage> GetDeliveryZoneByBranchRequest(int branchId)
     => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetZoneByBranchId}/{branchId}");
    private async Task<HttpResponseMessage> GetAllDeliveryCustomerTitlesRequest()
        => await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllDeliveryCustomerTitles);

    private async Task<HttpResponseMessage> CreateClientAsyncRequest(DeliveryCustomerDto deliveryCustomer)
       => await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateNewCustomer,deliveryCustomer);

    private async Task<T?> GetApiResponseAsync<T>(
    Func<Task<HttpResponseMessage>> apiRequest,
    string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return default;
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = ApiRequestHelpers.DeserializeResponseContent<T>(content);

        return result;
    }
}