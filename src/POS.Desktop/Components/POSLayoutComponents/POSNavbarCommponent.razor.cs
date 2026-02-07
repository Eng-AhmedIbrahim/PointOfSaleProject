using System.Windows;

namespace POS.Desktop.Components.POSLayoutComponents;

public partial class POSNavbarCommponent : IDisposable
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
        _section4ButtonsServices.OnChanged += () => InvokeAsync(StateHasChanged);
        Localizer.OnLanguageChanged += StateHasChanged;

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
        if (_commonProperties!.TableItems!.Any())
        {
            if (_commonProperties.CurrentPosMode == "DineIn" && 
                _commonProperties.UpdateDineInOrder == true && 
                (_commonProperties.AppendedTableItems == null || !_commonProperties.AppendedTableItems.Any()) &&
                _commonProperties.OrderDiscount == null)
            {
                 _cartService.ClearDineInOrderAttributes();
            }
            else 
            {
                _snackbar.Add(Localizer["PleaseCompleteOrder"], Severity.Warning);
                return;
            }
        }

        switch (mode)
        {
            case "TakeAway":
                {
                    _navigationManager.NavigateTo("/pos");
                    _commonProperties.CurrentPosMode = "TakeAway";
                    _commonProperties.TableItems!.Clear();
                    _commonProperties.AppendedTableItems!.Clear();
                    _commonProperties.CurrentDineInOrder = null;
                    _commonProperties.DineInOrderValues = new();
                    _commonProperties.UpdateDineInOrder = false;
                    _commonProperties.OrderDiscount = new();
                    break;
                }
            case "Delivery":
                {
                    _navigationManager.NavigateTo("/delivery");
                    _commonProperties.CurrentPosMode = "Delivery";
                    break;
                }
            case "DineIn":
                {
                    _navigationManager.NavigateTo("/dinein");
                    _commonProperties.CurrentPosMode = "DineIn";
                    break;
                }
        }
        _cartService.UpdateFinanceSettingsByMode(_commonProperties.CurrentPosMode);
        InvokeAsync(StateHasChanged);
    }

    private void ExitApp()
    {
        if (_commonProperties!.TableItems!.Any())
        {
            if (_commonProperties.CurrentPosMode == "DineIn" && 
                _commonProperties.UpdateDineInOrder == true && 
                (_commonProperties.AppendedTableItems == null || !_commonProperties.AppendedTableItems.Any()) &&
                _commonProperties.OrderDiscount == null)
            {
                 // Allowed to exit
            }
            else 
            {
                _snackbar.Add(Localizer["PleaseCompleteOrder"], Severity.Warning);
                return;
            }
        }
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        Localizer.OnLanguageChanged -= StateHasChanged;
    }
}
