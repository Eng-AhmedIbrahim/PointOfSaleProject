namespace ERPFront.ERPFrontServices.CategoryServices;

public class CategoryService : ICategoryServices
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<CategoryService> _logger;
    private readonly HttpClient _httpClient;

    public CategoryService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<CategoryService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<ICollection<CategoryToReturnDto>> GetAllCategoriesAsync()
    {
        return await GetApiResponseAsync<CategoryToReturnDto>(
            GetAllCategoriesRequest,
            "Failed to retrieve categories from the API."
        );
    }

    public async Task<ICollection<MenuSalesItemsToReturnDto>> GetItemsByCategoryIdAsync(
        int categoryId)
    {
        return await GetApiResponseAsync<MenuSalesItemsToReturnDto>(
            () => GetItemsByCategoryIdRequest(categoryId),
            "Failed to retrieve items from the API.");
    }

    private async Task<ICollection<T>> GetApiResponseAsync<T>(
        Func<Task<HttpResponseMessage>> apiRequest,
        string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest, _logger);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return [];
        }

        var content = await response.Content.ReadAsStringAsync();
        var items = ApiRequestHelpers.DeserializeResponseContent<List<T>>(content);

        return items ?? [];
    }


    private Task<HttpResponseMessage> GetAllCategoriesRequest()
        => _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllCategories);

    private Task<HttpResponseMessage> GetItemsByCategoryIdRequest(int categoryId)
        => _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetItemsByCategoryId}?catId={categoryId}");
}