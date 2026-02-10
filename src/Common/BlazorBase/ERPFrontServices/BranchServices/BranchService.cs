using BlazorBase.ERPFrontServices.AppDateServices;

namespace BlazorBase.ERPFrontServices.BranchServices;

public class BranchService : IBranchService
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<AppDateService> _logger;
    private readonly HttpClient _httpClient;

    public BranchService(ApiSettings apiSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<AppDateService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings.ApiName!);
    }

    public async Task<IReadOnlyList<BranchToReturnDto>> GetBranches()
    {
        return await GetApiResponseAsync<BranchToReturnDto>(
            GetBranchesRequest,
           "Failed to retrieve AppDate from the API.");
    }

    private async Task<HttpResponseMessage> GetBranchesRequest()
    => await _httpClient.GetAsync(_apiSettings!.Endpoints!.GetBranches);

    private async Task<IReadOnlyList<T>> GetApiResponseAsync<T>(
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
}
