namespace ERPFront.Components.POSLayoutComponents;

public partial class POSNavbarCommponent
{

    private bool canAccessTables;
    private bool canAccessDelivery;
    private bool canAccessTakeAway;
    private bool canAccessAccounts;
    private bool canAccessSummary;
    private bool canAccessDistribution;
    private bool canAccessOrders;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            canAccessTables = false;
            canAccessDelivery = false;
            canAccessTakeAway = false;
            canAccessAccounts = false;
            canAccessSummary = false;
            canAccessDistribution = false;
            canAccessOrders = false;
            return;
        }

        canAccessTables = (await AuthorizationService.AuthorizeAsync(user, "CanAccessTables")).Succeeded;
        canAccessDelivery = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDelivery")).Succeeded;
        canAccessTakeAway = (await AuthorizationService.AuthorizeAsync(user, "CanAccessTakeAway")).Succeeded;
        canAccessAccounts = (await AuthorizationService.AuthorizeAsync(user, "CanAccessAccounts")).Succeeded;
        canAccessSummary = (await AuthorizationService.AuthorizeAsync(user, "CanAccessSummary")).Succeeded;
        canAccessDistribution = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDistribution")).Succeeded;
        canAccessOrders = (await AuthorizationService.AuthorizeAsync(user, "CanAccessOrders")).Succeeded;
    }

    private void SetMode(string mode)
    {
        if (_commonProperties.CurrentPosMode != mode)
        {
            _commonProperties.CurrentPosMode = mode;

            InvokeAsync(StateHasChanged);
        }
        switch (mode)
        {
            case "TakeAway":
                {
                    _navigationManager.NavigateTo("/pos");
                    break;
                }
            case "Delivery":
                {
                    _navigationManager.NavigateTo("/pos/delivery");
                    break;
                }
            case "DineIn":
                {
                    _navigationManager.NavigateTo("/dinein");
                    break;
                }
        }
    }
}
