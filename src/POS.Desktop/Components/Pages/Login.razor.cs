using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace POS.Desktop.Components.Pages;

public partial class Login
{
    private string _pin = string.Empty;
    private string _userName = string.Empty;
    private ICollection<UserDto> Users = new List<UserDto>();
    private HttpClient? client;
    private ElementReference pinInput;

    [Inject] private AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;


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
        try
        {
            client = _clientFactory.CreateClient(ConstantStrings.ApiUrlName);
            var response = await client.GetAsync(ConstantStrings.GetAllUsersUrl);
            if (response != null && response.IsSuccessStatusCode)
            {
                var usersString = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserDto>>(usersString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                Users = users.OrderByDescending(u => u.UserName == "Administrator").ToList();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to load users. Please check connection.", Severity.Error);
        }
        await base.OnInitializedAsync();
    }

    private async Task LoginAction()
    {
        if (string.IsNullOrEmpty(_userName) || _userName == "Select User")
        {
            Snackbar.Add("Please select a user first", Severity.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_pin))
        {
            Snackbar.Add("Please enter your PIN", Severity.Warning);
            return;
        }

        try
        {
            var response = await client!.PostAsJsonAsync(ConstantStrings.LoginUserUrl, new { UserName = _userName, Password = _pin });

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadFromJsonAsync<UserDto>();
                string token = responseContent?.Token ?? string.Empty;

                if (string.IsNullOrEmpty(token))
                {
                    Snackbar.Add("Login Failed: No token received", Severity.Error);
                    return;
                }

                _commonProperties.CurrentUser = _userName;
                _commonProperties.CurrentUserId = responseContent!.UserId;

                //Store token in local storage &update authentication state
                if (_authenticationStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
                {
                    await customAuthStateProvider.NotifyUserAuthentication(token);
                }

                _navigationManager.NavigateTo("/pos", true);
            }
            else
            {
                Snackbar.Add("Login Failed: Invalid Username or PIN", Severity.Error);
                _pin = string.Empty;
                StateHasChanged(); 
                await pinInput.FocusAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("An error occurred during login", Severity.Error);
            _pin = string.Empty;
            StateHasChanged();
            await pinInput.FocusAsync();
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await LoginAction();
        }
    }

    private async Task<List<string>> GetUserPermissions()
    {
        var url = ConstantStrings.GetUserPermissionsUrl; // No userId
        var permissionsResponse = await client!.GetAsync(url);

        if (permissionsResponse.IsSuccessStatusCode)
        {
            var permissionsString = await permissionsResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(permissionsString) ?? new List<string>();
        }

        return new List<string>();
    }


    private void HandelOnChange(ChangeEventArgs e)
       => _userName = e.Value?.ToString() ?? string.Empty;

    private void ExitApp()
    {
        Application.Current.Shutdown();
    }
}

