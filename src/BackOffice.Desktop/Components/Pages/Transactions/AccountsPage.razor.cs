namespace BackOffice.Desktop.Components.Pages.Transactions;

public partial class AccountsPage
{
    [Inject] private IAuthorizationService _authorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider _authStateProvider { get; set; } = default!;
    [Inject] private IReportingErpService _reportingService { get; set; } = default!;
    [Inject] private IAppDateService _appDateService { get; set; } = default!;
    [Inject] private LocalizationService _localizer { get; set; } = default!;
    [Inject] private ISnackbar _snackbar { get; set; } = default!;
    [Inject] private IDialogService _dialogService { get; set; } = default!;

    private bool _canViewOrderDetails;
    private bool _canPrintOrderReport;
    private List<AccountSummaryDto> _summaries = new();
    private bool _isLoading = false;
    private DateTime? _selectedDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        var appDate = await _appDateService.GetAppDate();
        _selectedDate = appDate.PosDate;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            _canViewOrderDetails = (await _authorizationService.AuthorizeAsync(user, "CanViewOrderAtBackOffice")).Succeeded;
            _canPrintOrderReport = (await _authorizationService.AuthorizeAsync(user, "CanPrintStaffAccountsAtBackOffice")).Succeeded;
        }

        await LoadData();
    }

    private async Task LoadData()
    {
        if (_selectedDate == null) return;
        _isLoading = true;
        try
        {
            _summaries = await _reportingService.GetAccountsSummary(_selectedDate.Value, "Cashier");
        }
        catch (Exception ex)
        {
            _snackbar.Add("Error: " + ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OpenStaffDetails(AccountSummaryDto summary)
    {
        if (_selectedDate == null) return;
        
        try
        {
            var orders = await _reportingService.GetStaffOrders(_selectedDate.Value, summary.Id, summary.Type);
            
            var parameters = new DialogParameters
            {
                ["StaffId"] = summary.Id,
                ["StaffName"] = summary.Name,
                ["StaffType"] = summary.Type,
                ["SelectedDate"] = _selectedDate.Value,
                ["Orders"] = orders
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            _dialogService.Show<StaffTransactionsDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            _snackbar.Add("Error loading details: " + ex.Message, Severity.Error);
        }
    }

    private async Task PrintAccountsReport()
    {
        try
        {
            var parameters = new DialogParameters
            {
                ["ReportTitle"] = Localizer.GetCurrentLanguage() == "ar" ? "تقرير حسابات الموظفين" : "Staff Accounts Report",
                ["BranchName"] = _commonProperties.StoreName,
                ["ReportDate"] = _selectedDate ?? DateTime.Today,
                ["Items"] = _summaries.Select(s => new SalesItemSummaryDto 
                { 
                    ItemName = s.Name, 
                    Quantity = s.OrderCount, 
                    TotalAmount = s.TotalAmount 
                }).ToList()
            };
            
            var options = new DialogOptions { FullScreen = true, CloseButton = false, NoHeader = true };
            await _dialogService.ShowAsync<ReportPreviewDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            _snackbar.Add("Error preparing preview: " + ex.Message, Severity.Error);
        }
    }

    private void GoBack()
    {
        _navigationManager.NavigateTo("/");
    }
}
