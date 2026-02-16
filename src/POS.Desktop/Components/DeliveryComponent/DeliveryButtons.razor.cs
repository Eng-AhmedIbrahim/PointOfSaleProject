using POS.Contract.Dtos;
using POS.Desktop.Components.PosDialog;

namespace POS.Desktop.Components.DeliveryComponent;

public partial class DeliveryButtons
{
    private async Task ShowComplaints()
    {
        if (string.IsNullOrEmpty(_commonProperties?.CustomerDetails?.CustomerName))
        {
            _snackbar.Add(Localizer["SearchCustomerFirst"], Severity.Warning);
            return;
        }

        var parameters = new DialogParameters();
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };

        var dialog = await _dialogService.ShowAsync<ComplaintActionDialog>("Complaints", parameters, options);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is string action)
        {
            if (action == "New")
            {
                await CreateNewComplaint();
            }
            else if (action == "View")
            {
                await SafeNavigateAsync("/delivery-complaints");
            }
        }
    }

    private async Task CreateNewComplaint()
    {
        if (string.IsNullOrEmpty(_commonProperties?.CustomerDetails?.CustomerName))
        {
            _snackbar.Add(Localizer["SearchCustomerFirst"], Severity.Warning);
            return;
        }

        var targetOrder = _commonProperties.CustomerDetails.ActiveOrder ?? _commonProperties.CustomerDetails.Last10Orders?.FirstOrDefault();
        
        var complaintDto = new ComplaintDto
        {
            CustomerId = _commonProperties.CustomerDetails.Id,
            CustomerName = _commonProperties.CustomerDetails.CustomerName,
            CustomerPhone = _commonProperties.CustomerDetails.FirstPhoneNumber,
            OrderId = targetOrder?.OrderId,
            OrderDatabaseId = targetOrder?.Id
        };

        var parameters = new DialogParameters { ["ComplaintDto"] = complaintDto };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

        var dialog = await _dialogService.ShowAsync<ComplaintDialog>(Localizer["NewComplaint"], parameters, options);
        var result = await dialog.Result;
        
        if (!result!.Canceled && result.Data is ComplaintDto savedComplaint)
        {
            // Optional: You could reload complaints if there's a local list being displayed
        }
    }

    private async Task AddCustomer()
    {
        var client = await _deliveryServices.GetClientByPhoneNumberAsync(_commonProperties!.CustomerDetails!.FirstPhoneNumber!);
        if (client.Id == 0)
        {
            var customerDetails = BackupCustomer() ?? new();
            var createdCustomer = await _deliveryServices.CreateClientAsync(customerDetails);
            if (createdCustomer is not null)
            {
                await SetPosModeToDeliveryMode();
                return;
            }
            else
            {
                _snackbar.Add("Customer Not Created", Severity.Error);
                return;
            }
        }

        await SetPosModeToDeliveryMode();
    }

    private async Task BackToPos()
    {
        await SafeNavigateAsync("/pos");
        _commonProperties.AppendedTableItems!.Clear();
        _commonProperties.CustomerDetails = new();
        _commonProperties.CurrentPosMode = PosModes.TakeAway.ToString();
    }


    private async Task SetPosModeToDeliveryMode()
    {
        _commonProperties.CurrentPosMode = PosModes.Delivery.ToString();

        _handelDeliveryInvocation.DeliveryDetails = string.Empty; // Keep header clean as per user request
        await SafeNavigateAsync("/pos");
    }

    private DeliveryCustomerDto BackupCustomer()
        => new DeliveryCustomerDto()
        {
            FirstPhoneNumber = _commonProperties.CustomerDetails!.FirstPhoneNumber,
            SecondPhoneNumber = _commonProperties.CustomerDetails!.SecondPhoneNumber,
            ClientTitle = _commonProperties.CustomerDetails!.ClientTitle,
            CustomerName = _commonProperties.CustomerDetails!.CustomerName,
            BranchName = _commonProperties.CustomerDetails!.BranchName,
            ZoneName = _commonProperties.CustomerDetails!.ZoneName,
            HomeNumber = _commonProperties.CustomerDetails!.HomeNumber,
            FloorNumber = _commonProperties.CustomerDetails!.FloorNumber,
            FlatNumber = _commonProperties.CustomerDetails!.FlatNumber,
            ClientAddress = _commonProperties.CustomerDetails!.ClientAddress,
            AddressNote = _commonProperties.CustomerDetails!.AddressNote
        };

    private void ClearDeliveryOrder()
    {
        _commonProperties.CustomerDetails = new();
        _handelDeliveryInvocation.CustomerDetails = new();
        _handelDeliveryInvocation.TriggerAddressSelected();
    }


    private async Task AddNewAddress()
    {
        if (string.IsNullOrEmpty(_commonProperties.CustomerDetails!.FirstPhoneNumber))
        {
            _snackbar.Add("Please Enter Customer Phone Number", Severity.Error);
            return;
        }

        ClearAddressDateToAddNewData();
        _commonProperties.DialogReference = await _dialogService.ShowAsync<AddNewAddressDialog>("Add New Address");
    }

    private void ClearAddressDateToAddNewData()
    {
        _handelDeliveryInvocation.CustomerDetails = CustomerDetails.Clone(_commonProperties.CustomerDetails!);

        _commonProperties.CustomerDetails!.ClientAddress = string.Empty;
        _commonProperties.CustomerDetails.FloorNumber = string.Empty;
        _commonProperties.CustomerDetails.FlatNumber = string.Empty;
        _commonProperties.CustomerDetails.HomeNumber = string.Empty;
        _commonProperties.CustomerDetails.AddressNote = string.Empty;
        _commonProperties.CustomerDetails.BranchName = string.Empty;
        _commonProperties.CustomerDetails.ZoneName = string.Empty;
    }

    private async Task ToggleDirection()
    {
        _handelDeliveryInvocation.TriggerToggleDirection();
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
                if (_navigationManager != null)
                {
                    try
                    {
                        // Try to access Uri - if it throws, NavigationManager isn't ready yet
                        var currentUri = _navigationManager.Uri;
                        if (!string.IsNullOrEmpty(currentUri))
                        {
                            // Use InvokeAsync to ensure we're on the correct synchronization context
                            await InvokeAsync(() => _navigationManager.NavigateTo(uri, forceLoad: false));
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
                    _snackbar.Add($"Navigation failed: {ex.Message}", Severity.Error);
                    return;
                }
                
                // Exponential backoff
                delayMs *= 2;
            }
        }
    }
}