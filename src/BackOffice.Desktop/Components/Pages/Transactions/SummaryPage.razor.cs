using BackOffice.Desktop.Components.Pages.Reporting;

namespace BackOffice.Desktop.Components.Pages.Transactions;

public partial class SummaryPage
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IPrintOrderService PrintOrderService { get; set; } = default!;
    [Inject] private IBranchService BranchService { get; set; } = default!;


    [SupplyParameterFromQuery]
    [Parameter]
    public string? Start { get; set; }

    [SupplyParameterFromQuery]
    [Parameter]
    public string? End { get; set; }

    private bool _canViewDetails;
    private bool _canPrint;
    private SalesSummaryDto _summaryData = new();
    private bool _isLoading = true;
    private MudBlazor.DateRange _dateRange = new MudBlazor.DateRange(null, null);

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            _canViewDetails = (await AuthorizationService.AuthorizeAsync(user, "CanViewSummaryDetailsAtBackOffice")).Succeeded;
            _canPrint = (await AuthorizationService.AuthorizeAsync(user, "CanPrintSummaryReportAtBackOffice")).Succeeded;
        }

        try
        {
            if (_commonProperties.BranchDetails?.Id == null || _commonProperties.BranchDetails.Id == 0)
            {
                var branches = await BranchService.GetBranches();
                if (branches != null && branches.Any())
                {
                    _commonProperties.BranchDetails = branches.FirstOrDefault();
                }
            }

            var appDate = await AppDateService.GetAppDate();
            var dtStart = appDate.PosDate;
            var dtEnd = appDate.PosDate;

            if (DateTime.TryParse(Start, out var pStart) && DateTime.TryParse(End, out var pEnd))
            {
                dtStart = pStart;
                dtEnd = pEnd;
            }

            _dateRange = new MudBlazor.DateRange(dtStart, dtEnd);
            await LoadSummaryData();
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error loading summary: " + ex.Message, Severity.Error);
            _isLoading = false;
        }
    }

    private async Task OnDateRangeChanged(MudBlazor.DateRange range)
    {
        _dateRange = range;
        if (_dateRange.Start.HasValue)
        {
            await LoadSummaryData();
        }
    }

    private async Task LoadSummaryData()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            DateTime startDate;
            DateTime endDate;

            if (_dateRange.Start.HasValue)
            {
                startDate = _dateRange.Start.Value;
                endDate = _dateRange.End ?? startDate;
            }
            else
            {
                var appDate = await AppDateService.GetAppDate();
                startDate = appDate.PosDate;
                endDate = appDate.PosDate;
                _dateRange = new DateRange(startDate, endDate);
            }

            _summaryData = await ReportingService.GetSalesSummary(startDate, endDate);
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
            StateHasChanged();
        }
    }

    private void GoBack()
    {
        _navigationManager.NavigateTo("/");
    }

    private async Task ShowSalesItems()
    {
        try
        {
            var startDate = _dateRange.Start ?? DateTime.Today;
            var endDate = _dateRange.End ?? startDate;
            var items = await ReportingService.GetSalesItemsSummary(startDate, endDate);
            var parameters = new DialogParameters { ["Items"] = items };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = false, NoHeader = true };
            await DialogService.ShowAsync<SalesItemsDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error loading sales items: " + ex.Message, Severity.Error);
        }
    }

    private async Task PreviewFastReport(string reportId, string reportName)
    {
        var startDate = _dateRange.Start ?? DateTime.Today;
        var endDate = _dateRange.End ?? startDate;
        
        var parameters = new DialogParameters<ReportViewerDialog>
        {
            { x => x.ReportId, reportId },
            { x => x.ReportName, reportName },
            { x => x.FromDate, startDate },
            { x => x.ToDate, endDate }
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Large, FullWidth = true, FullScreen = true };
        await DialogService.ShowAsync<ReportViewerDialog>(reportName, parameters, options);
    }
}
