namespace POS.Desktop.Components.Pages.AccountingPages;

public partial class AccountsPage
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private bool _canViewAccountDetails;
    private bool _canPrintAccountReport;
    private bool _canPosSettingsFeature;
    private List<AccountSummaryDto> _accounts = new();
    private bool _isLoading = false;
    private string _staffType = "Cashier";
    private DateTime? _selectedDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        var appDate = await AppDateService.GetAppDate();
        _selectedDate = appDate.PosDate;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            _canViewAccountDetails = (await AuthorizationService.AuthorizeAsync(user, "CanAccessAccountsViewBtn")).Succeeded;
            _canPrintAccountReport = (await AuthorizationService.AuthorizeAsync(user, "CanAccessAccountsPrintBtn")).Succeeded;
            _canPosSettingsFeature = (await AuthorizationService.AuthorizeAsync(user, "CanAccessPosSettingsFeature")).Succeeded;
        }

        await LoadData();
    }

    private async Task LoadData()
    {
        if (_selectedDate == null) return;
        _isLoading = true;
        try
        {
            _accounts = await ReportingService.GetAccountsSummary(_selectedDate.Value, _staffType);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error: " + ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Trigger reload when staff type changes
    protected override async Task OnParametersSetAsync()
    {
        // This doesn't catch the bind-value change automatically in all cases, better to use a property with setter or OnAfterRender if needed, 
        // but MudToggleGroup bind-Value usually works fine for UI. I'll add a Task to watch it if needed.
    }

    // Since bind-Value doesn't trigger a refresh automatically unless we use @bind-Value:after (but that's .NET 7+)
    // In current version let's use a Property for StaffType
    private string StaffType
    {
        get => _staffType;
        set
        {
            if (_staffType != value)
            {
                _staffType = value;
                _ = LoadData();
            }
        }
    }

    private void GoBack()
    {
        _commonProperties.CurrentPosMode = "TakeAway";
        _navigationManager.NavigateTo("/pos");
    }
}