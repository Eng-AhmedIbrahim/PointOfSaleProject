namespace ERPFront.Components.Pages;

public partial class POS
{
    private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();
    private ICollection<MenuSalesItemsToReturnDto> _itemByCatId = new List<MenuSalesItemsToReturnDto>();
    private static readonly JsonSerializerOptions option = new() { PropertyNameCaseInsensitive = true };
    HttpClient? client;

    protected async override Task OnInitializedAsync()
    {
        client = _clientFactory?.CreateClient(ConstantStrings.ApiUrlName);
        var response = await client!.GetFromJsonAsync<List<CategoryToReturnDto>>(ConstantStrings.GetAllCategoriesUrl, option) ?? new();

        if (!response.Any())
            _categories = new List<CategoryToReturnDto>();

        _categories = response;
    }

    private async Task<ICollection<MenuSalesItemsToReturnDto>> GetItemsByCatId(int catId)
    {

        var items = new HashSet<MenuSalesItemsToReturnDto>();

        string url = $"{ConstantStrings.GetItemsByCategoryId}?catId={catId}";
        var response = await client!.GetAsync(url) ?? new();
        if (response.IsSuccessStatusCode)
        {
            var dataAsStringStream = await response.Content.ReadAsStringAsync();

            items = JsonSerializer.Deserialize<HashSet<MenuSalesItemsToReturnDto>>(dataAsStringStream, option);
        }

        return items ?? [];
    }

    private async Task InvokeItems(int catId)
    {
        _itemByCatId = await GetItemsByCatId(catId);
    }
}