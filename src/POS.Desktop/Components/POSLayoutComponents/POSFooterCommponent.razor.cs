using POS.Desktop.Components.PosDialog;
using BlazorBase.Services;
using Microsoft.Extensions.Localization;

namespace POS.Desktop.Components.POSLayoutComponents;

public partial class POSFooterCommponent : IDisposable
{
    private bool canAccessDiscount;
    private bool canAccessMeals;
    private bool canAccessWaiting;
    private bool canAccessSettings;

    [Inject] public required LocalizationService LocalizationService { get; set; }

    private bool _drawerOpen;

    protected override async Task OnInitializedAsync()
    {
        commonProperties.OnChange += StateHasChanged;
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;


        if (user.Identity is not { IsAuthenticated: true })
        {
            canAccessDiscount = false;
            canAccessMeals = false;
            canAccessWaiting = false;
            canAccessSettings = false;
            LocalizationService.OnLanguageChanged += StateHasChanged;
            return;
        }
        canAccessDiscount = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDiscount")).Succeeded;
        canAccessMeals = (await AuthorizationService.AuthorizeAsync(user, "CanAccessMeals")).Succeeded;
        canAccessWaiting = (await AuthorizationService.AuthorizeAsync(user, "CanAccessWaiting")).Succeeded;
        canAccessSettings = (await AuthorizationService.AuthorizeAsync(user, "CanAccessSettings")).Succeeded;
        LocalizationService.OnLanguageChanged += StateHasChanged;
    }



    private async Task OpenOrderDiscountDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        await DialogService.ShowAsync<OrderDiscountDialog>("Order Discount", options);
    }

    private void GotoWaitingPage()
    {
        if (commonProperties!.TableItems!.Count() > 0)
        {
            Snackbar.Add("Please Finish The Order First", Severity.Warning);
        }
        else
        {
            navigationManager.NavigateTo("/waitingPage");
        }
    }

  


    private async Task OpenCustomerInfoDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        await DialogService.ShowAsync<CustomerInfoDialog>("Customer Info", options);
    }

    private async Task OpenPaymentMethodDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        await DialogService.ShowAsync<PaymentModeDialog>("Payment Method", options);
    }

    private void ToggleDrawer()
        => _drawerOpen = !_drawerOpen;


    public void Dispose()
    {
        commonProperties.OnChange -= StateHasChanged;
        LocalizationService.OnLanguageChanged -= StateHasChanged;
    }
}