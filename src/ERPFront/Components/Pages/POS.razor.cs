namespace ERPFront.Components.Pages;

public partial class POS
{
    private ICollection<CategoryToReturnDto> categories = new List<CategoryToReturnDto>();

    protected async override Task OnInitializedAsync()
    {
        try
        {
            var httpClient = _clientFactory.CreateClient(ConstantStrings.ApiUrlName);
            var response = await httpClient.GetAsync("/api/Category/GetAllCategories");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                categories = JsonSerializer.Deserialize<List<CategoryToReturnDto>>(result,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            //_logger.LogError(ex, ex.Message);
        }
    }

}