namespace ERPFront.Components.POSLayoutComponents;

public partial class POSFooterCommponent
{
    private bool canAccessDiscount;
    private bool canAccessMeals;
    private bool canAccessWaiting;
    private bool canAccessSettings;


    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;


        if (user.Identity is not { IsAuthenticated: true })
        {
            canAccessDiscount = false;
            canAccessMeals = false;
            canAccessWaiting = false;
            canAccessSettings = false;
            return;
        }
        canAccessDiscount = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDiscount")).Succeeded;
        canAccessMeals = (await AuthorizationService.AuthorizeAsync(user, "CanAccessMeals")).Succeeded;
        canAccessWaiting = (await AuthorizationService.AuthorizeAsync(user, "CanAccessWaiting")).Succeeded;
        canAccessSettings = (await AuthorizationService.AuthorizeAsync(user, "CanAccessSettings")).Succeeded;
    }
}
