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
        LocalizationService.OnLanguageChanged += StateHasChanged;

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

    private async Task OpenOrderDiscountDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        commonProperties.OrderDiscountDialogReference = await DialogService.ShowAsync<OrderDiscountDialog>(LocalizationService["OrderDiscount"], options);
    }

    private void GotoWaitingPage()
    {
        if (commonProperties!.TableItems!.Any())
        {
            Snackbar.Add(LocalizationService["FinishOrderFirst"], Severity.Warning);
        }
        else
        {
            navigationManager.NavigateTo("/waitingPage");
        }
    }

    private async Task OpenCustomerInfoDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        commonProperties.CustomerInfoDialogReference = await DialogService.ShowAsync<CustomerInfoDialog>(LocalizationService["CustomerData"], options);
    }

    private async Task OpenPaymentMethodDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        commonProperties.PaymentMethodDialogReference = await DialogService.ShowAsync<PaymentModeDialog>(LocalizationService["PaymentMethod"], options);
    }

    private async Task OpenQuickPaymentDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        commonProperties.QuickPaymentDialogReference = await DialogService.ShowAsync<QuickPaymentDialog>(LocalizationService["QuickPayment"], options);
    }

    private void ToggleDrawer()
        => _drawerOpen = !_drawerOpen;

    public void Dispose()
    {
        commonProperties.OnChange -= StateHasChanged;
        LocalizationService.OnLanguageChanged -= StateHasChanged;
    }
}