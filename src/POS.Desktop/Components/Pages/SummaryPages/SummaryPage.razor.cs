namespace POS.Desktop.Components.Pages.SummaryPages;

public partial class SummaryPage
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IPrintOrderService PrintOrderService { get; set; } = default!;

    private bool _canViewDetails;
    private bool _canPrint;
    private bool _canPosSettingsFeature;
    private SalesSummaryDto _summaryData = new();
    private bool _isLoading = true;
    private DateTime SelectedDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            _canViewDetails = (await AuthorizationService.AuthorizeAsync(user, "CanAccessSummaryViewDetailsBtn")).Succeeded;
            _canPrint = (await AuthorizationService.AuthorizeAsync(user, "CanAccessSummaryPrintBtn")).Succeeded;
            _canPosSettingsFeature = (await AuthorizationService.AuthorizeAsync(user, "CanAccessPosSettingsFeature")).Succeeded;
        }

        try
        {
            var appDate = await AppDateService.GetAppDate();
            SelectedDate = appDate.PosDate;
            _summaryData = await ReportingService.GetSalesSummary(SelectedDate);
            if (string.IsNullOrEmpty(_summaryData.Overall.Currency))
                _summaryData.Overall.Currency = "L.E";
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error loading summary: " + ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void GoBack()
    {
        _commonProperties.CurrentPosMode = "TakeAway";
        _navigationManager.NavigateTo("/pos");
    }

    private async Task ShowSalesItems()
    {
        try
        {
            var items = await ReportingService.GetSalesItemsSummary(SelectedDate);
            var parameters = new DialogParameters { ["Items"] = items };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = false, NoHeader = true };
            await DialogService.ShowAsync<SalesItemsDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error loading sales items: " + ex.Message, Severity.Error);
        }
    }

    private async Task PrintSummary()
    {
        try
        {
            var items = await ReportingService.GetSalesItemsSummary(SelectedDate);
            await PrintOrderService.PrintSalesSummaryAsync(_summaryData, items);
            Snackbar.Add("Printing Summary...", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error printing summary: " + ex.Message, Severity.Error);
        }
    }
}