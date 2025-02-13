namespace ERPFront.Components.Pages
{
    public partial class Sliders
    {
        private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();

        protected async override Task OnInitializedAsync()
        {
            var client = _clientFactory.CreateClient(ConstantStrings.ApiUrlName);
            var response = await client.GetAsync(ConstantStrings.GetAllCategoriesUrl);
            if (response.IsSuccessStatusCode)
            {
                var dataAsStringStream = await response.Content.ReadAsStringAsync();
                _categories = JsonSerializer.Deserialize<List<CategoryToReturnDto>>(dataAsStringStream
                    , new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); ;
            }
            await base.OnInitializedAsync();
        }

        private async Task SaveChanges()
        {
            
        }
    }
}
