using POS.Desktop.Components.PosDialog;
using BlazorBase.Services;
using Microsoft.Extensions.Localization;

namespace POS.Desktop.Components.POSLayoutComponents;

public partial class POSFooterCommponent : IDisposable
{
    private bool _canFooterDiscount;
    private bool _canFooterCustomerData;
    private bool _canFooterPaymentMethod;
    private bool _canFooterQuickPayment;
    private bool _canFooterMeals;
    private bool _canFooterWaiting;
    private bool _canFooterSettings;
    private bool _canPosSettingsFeature;

    [Inject] public required LocalizationService LocalizationService { get; set; }
    private bool _drawerOpen;
    private Action? _stateChangedHandler;

    protected override async Task OnInitializedAsync()
    {
        _stateChangedHandler = async () => 
        {
            try { await InvokeAsync(StateHasChanged); } catch { }
        };

        _commonProperties.OnChange += _stateChangedHandler;
        LocalizationService.OnLanguageChanged += _stateChangedHandler;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        _canFooterDiscount       = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterDiscountBtn")).Succeeded;
        _canFooterCustomerData   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterCustomerDataBtn")).Succeeded;
        _canFooterPaymentMethod  = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterPaymentMethodBtn")).Succeeded;
        _canFooterQuickPayment   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterQuickPaymentBtn")).Succeeded;
        _canFooterMeals          = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterMealsBtn")).Succeeded;
        _canFooterWaiting        = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterWaitingBtn")).Succeeded;
        _canFooterSettings       = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterSettingsBtn")).Succeeded;
        _canPosSettingsFeature   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessPosSettingsFeature")).Succeeded;
    }

    private async Task OpenOrderDiscountDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        _commonProperties.OrderDiscountDialogReference = await DialogService.ShowAsync<OrderDiscountDialog>(LocalizationService["OrderDiscount"], options);
    }

    private async Task GotoWaitingPage()
    {
        if (_commonProperties!.TableItems!.Any())
        {
            Snackbar.Add(LocalizationService["FinishOrderFirst"], Severity.Warning);
        }
        else
        {
            await SafeNavigateAsync("/waitingPage");
        }
    }

    private async Task SafeNavigateAsync(string uri)
    {
        int maxRetries = 5;
        int currentRetry = 0;
        int delayMs = 5;

        while (currentRetry < maxRetries)
        {
            try
            {
                await Task.Delay(delayMs);
                
                // Check if NavigationManager is initialized by safely checking the Uri property
                if (navigationManager != null)
                {
                    try
                    {
                        // Try to access Uri - if it throws, NavigationManager isn't ready yet
                        var currentUri = navigationManager.Uri;
                        if (!string.IsNullOrEmpty(currentUri))
                        {
                            // Use InvokeAsync to ensure we're on the correct synchronization context
                            await InvokeAsync(() => navigationManager.NavigateTo(uri, forceLoad: false));
                            return;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // NavigationManager not yet initialized, will retry
                        throw new InvalidOperationException("NavigationManager not yet initialized");
                    }
                }
                else
                {
                    throw new InvalidOperationException("NavigationManager is null");
                }
            }
            catch (Exception ex)
            {
                currentRetry++;
                
                if (currentRetry >= maxRetries)
                {
                    Snackbar.Add($"Navigation failed: {ex.Message}", Severity.Error);
                    return;
                }
                
                // Exponential backoff
                delayMs *= 2;
            }
        }
    }


    private async Task OpenCustomerInfoDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        _commonProperties.CustomerInfoDialogReference = await DialogService.ShowAsync<CustomerInfoDialog>(LocalizationService["CustomerData"], options);
    }

    private async Task OpenPaymentMethodDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        _commonProperties.PaymentMethodDialogReference = await DialogService.ShowAsync<PaymentModeDialog>(LocalizationService["PaymentMethod"], options);
    }

    private async Task OpenQuickPaymentDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        _commonProperties.QuickPaymentDialogReference = await DialogService.ShowAsync<QuickPaymentDialog>(LocalizationService["QuickPayment"], options);
    }

    private void ToggleDrawer()
        => _drawerOpen = !_drawerOpen;

    public void Dispose()
    {
        if (_stateChangedHandler != null)
        {
            _commonProperties.OnChange -= _stateChangedHandler;
            LocalizationService.OnLanguageChanged -= _stateChangedHandler;
        }
    }
}