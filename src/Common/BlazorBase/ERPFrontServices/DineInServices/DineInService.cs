namespace BlazorBase.ERPFrontServices.DineInServices;

public class DineInService : IDineInService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<DineInService> _logger;

    public DineInService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<DineInService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings!.ApiName!);
    }
    public async Task<ICollection<TableGroupToReturnDto>> GetTableGroupsAsync()
    {
        return await GetApiResponseAsync<TableGroupToReturnDto>(
              GetAllTableGroupsRequest,
              "Failed to retrieve Table Groups from the API."
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

    private Task<HttpResponseMessage> GetAllTableGroupsRequest()
    => _httpClient.GetAsync(_apiSettings.Endpoints!.GetTableGroups!);

    public async Task<ICollection<TableToReturnDto>> GetTablesByGroupId(int tableGroupId)
    {
        return await GetApiResponseAsync<TableToReturnDto>(
            () => GetTablesByGroupIdRequest(tableGroupId),
             "Failed to retrieve Table Groups from the API."
         );
    }

    public async Task<ICollection<CaptainOrderUserToReturnDto>> GetCaptainOrders()
        => await GetApiResponseAsync<CaptainOrderUserToReturnDto>(GetCaptainOrdersRequest,
            "Failed to retrieve Captain Orders from the API.");

    private async Task<HttpResponseMessage> GetTablesByGroupIdRequest(int groupId)
    => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetTablesByGroupId}/{groupId}");

    private async Task<HttpResponseMessage> GetCaptainOrdersRequest()
        => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetUsersByRole}/كابتن صاله");

    public async Task<ICollection<TableToReturnDto>> GetTables()
    {
        return await GetApiResponseAsync<TableToReturnDto>(
             GetTablesRequest,
             "Failed to retrieve Table Groups from the API."
         );
    }

    private async Task<HttpResponseMessage> GetTablesRequest()
     =>   await _httpClient.GetAsync($"{_apiSettings.Endpoints!.UpdateTables}");
}