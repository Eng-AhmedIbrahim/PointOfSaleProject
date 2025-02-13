namespace ERPFront.Components.Pages;

public partial class Login
{
    private string _pin = string.Empty;
    private ICollection<UserDto> Users = new List<UserDto>();

    private void AddDigit(string digit)
        => _pin += digit;

    private void ClearInput()
     => _pin = string.Empty;

    private void DeleteLastDigit()
    {
        if (_pin.Length > 0)
            _pin = _pin.Substring(0, _pin.Length - 1);
    }

    protected async override Task OnInitializedAsync()
    {
        var client = _clientFactory.CreateClient(ConstantStrings.ApiUrlName);
        var response = await client.GetAsync(ConstantStrings.GetAllUsersUrl);
        if(response.IsSuccessStatusCode)
        {
            var usersString = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserDto>>(usersString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true})??new();
            
            Users = users.OrderByDescending(u => u.UserName == "Administrator").ToList();
        }
        await base.OnInitializedAsync();
    }
}