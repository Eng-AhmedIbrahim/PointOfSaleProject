namespace POS.Desktop.Components.Pages.DeliveryPages;

public partial class Delivery : IDisposable
{
    public IReadOnlyList<DeliveryTitleToReturnDto>? CustomerTitles { get; set; } = new List<DeliveryTitleToReturnDto>();
    public IReadOnlyList<BranchToReturnDto>? Branches { get; set; } = new List<BranchToReturnDto>();

    private IReadOnlyList<DeliveryZonesToReturnDto> Zones = [];
    protected async override Task OnInitializedAsync()
    {
        _handelDeliveryInvocation.OnAddressSelected += HandleAddressSelected;
        _commonProperties.CustomerDetails = new ();

        await GetAllTitles();
        await GetAllBranches();
    }

    private async Task GetAllBranches()
         => Branches = await _branchService.GetBranches();

    private async Task GetAllTitles()
       => CustomerTitles = await _deliveryServices.GetAllDeliveryCustomerTitlesAsync();

    private void HandleAddressSelected()
        => StateHasChanged();

    public void Dispose()
        => _handelDeliveryInvocation.OnAddressSelected -= HandleAddressSelected;
}
