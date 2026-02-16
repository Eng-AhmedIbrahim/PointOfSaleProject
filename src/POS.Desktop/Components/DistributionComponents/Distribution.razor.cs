using Microsoft.AspNetCore.SignalR.Client;
using POS.Contract.Dtos.OrderDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using BlazorBase.Models;
using System.Threading;
using BlazorBase.ERPFrontServices.DistributionServices;
using BlazorBase;
using ERPFront.HubSettings;
using MudBlazor;
using BlazorBase.ERPFrontServices.CartServices;
using ERPFront.Models;
using POS.Desktop.Components.DineInComponents;
using BlazorBase.ERPFrontServices.AppDateServices;

namespace POS.Desktop.Components.DistributionComponents;

public partial class Distribution : IDisposable, IAsyncDisposable
{
    [Inject] public IDistributionErpService _distributionService { get; set; } = default!;
    [Inject] public CallCenterHubSettings _hubSettings { get; set; } = default!;
    [Inject] public IOptions<DispatcherSettings> _dispatcherSettings { get; set; } = default!;
    [Inject] public CommonProperties _commonProperties { get; set; } = default!;
    [Inject] public NavigationManager navigationManager { get; set; } = default!;
    [Inject] public CartService _cartService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService _dialogService { get; set; } = default!;
    [Inject] public BlazorBase.ERPFrontServices.PrintOrderServices.IPrintOrderService _printOrderService { get; set; } = default!;
    [Inject] public IAppDateService _appDateService { get; set; } = default!;

    private async Task ShowDriverSettlement()
    {
        var appDate = await _appDateService.GetAppDate();
        var settlements = await _distributionService.GetDriverSettlement(appDate.PosDate);
        
        var parameters = new DialogParameters<DriversSettlementDialog>();
        parameters.Add("Settlements", settlements);
        parameters.Add("PosDate", appDate.PosDate);
        
        await _dialogService.ShowAsync<DriversSettlementDialog>(Localizer["Distribution_DriverSettlement"], parameters);
    }

    private async Task PrintOrder(OrderDto order)
    {
        await _printOrderService.PrintDispatchOrderAsync(order);
    }

    private List<HubConnection> _externalHubConnections = new();
    private List<OrderDto> Orders = new();
    private Timer? _timer;

    private IEnumerable<OrderDto> FilteredOrders => showAssigned
        ? Orders.Where(o => !String.IsNullOrEmpty(o.DriverName))
        : Orders.Where(o => String.IsNullOrEmpty(o.DriverName));

    private HashSet<int> SelectedOrderIds = new();
    private bool SelectAllOrders = false;
    private bool showAssigned = false;

    private IEnumerable<OrderDto> SelectedOrders => FilteredOrders.Where(o => SelectedOrderIds.Contains(o.OrderId));

    private string? _selectedDriverId;
    private List<OrderDto> BusyDrivers => Orders.Where(o => !string.IsNullOrEmpty(o.DriverID))
                                               .Select(o => new OrderDto { DriverID = o.DriverID, DriverName = o.DriverName })
                                               .DistinctBy(d => d.DriverID)
                                               .ToList();

    // Dialog state variables
    private bool showDriversDialog = false;
    private string driversDialogTitle = "";

    private bool showAssignmentDialog = false;
    private OrderDto? selectedOrder;
    private string? selectedDriver;


    private void NavigateBack()
    {
        _commonProperties.CurrentPosMode = "TakeAway";
        navigationManager.NavigateTo("/pos");
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await ConnectToExternalHubs();

        var orders = await _distributionService.GetUnCompletedDeliveryOrders();

        foreach (var order in orders)
            AddNewDeliveryOrder(order);

        if (!_commonProperties.Drivers.Any())
        {
            var drivers = await _distributionService.GetDeliveryUsers();
            foreach (var driver in drivers)
                _commonProperties.Drivers.Add(driver, "Available");
        }

        UpdateDriverStatus();

        _timer = new Timer(_ =>
        {
            InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(_dispatcherSettings.Value.RefreshTimeForDeliveryOrderColorsPerSecond));
    }

    private async Task ConnectToExternalHubs()
    {
        var urls = _hubSettings.Urls ?? new();

        foreach (var hubUrl in urls)
        {
            if (string.IsNullOrWhiteSpace(hubUrl))
                continue;

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            connection.On<OrderDto>("ReceiveNewDeliveryOrder", orderDto =>
            {
                Console.WriteLine($"New external order from {hubUrl}: {orderDto.OrderId}");
                InvokeAsync(() => {
                    AddNewDeliveryOrder(orderDto);
                    UpdateDriverStatus();
                });
            });

            connection.On<OrderDto>("ReceiveOrderDispatched", orderDto =>
            {
                Console.WriteLine($"Order dispatched: {orderDto.OrderId} to driver {orderDto.DriverName}");
                InvokeAsync(() => {
                    UpdateOrderStatus(orderDto);
                    UpdateDriverStatus();
                });
            });

            connection.On<int>("ReceiveOrderUnDispatched", orderId =>
            {
                Console.WriteLine($"Order un-dispatched: {orderId}");
                InvokeAsync(() => {
                    var order = Orders.FirstOrDefault(o => o.OrderId == orderId);
                    if (order != null)
                    {
                        order.DriverID = null;
                        order.DriverName = null;
                        order.AssignTime = null;
                        order.DispatchID = null;
                        UpdateDriverStatus();
                        StateHasChanged();
                    }
                });
            });

            connection.On<OrderDto>("ReceiveOrderCollected", orderDto =>
            {
                Console.WriteLine($"Order collected: {orderDto.OrderId}");
                InvokeAsync(() => {
                    RemoveOrder(orderDto.OrderId);
                    UpdateDriverStatus();
                });
            });

            try
            {
                await connection.StartAsync();
                Console.WriteLine($"Connected to external hub: {hubUrl}");
                _externalHubConnections.Add(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to external hub {hubUrl}: {ex.Message}");
            }
        }
    }

    private void AddNewDeliveryOrder(OrderDto newOrder)
    {
        if (!Orders.Any(o => o.OrderId == newOrder.OrderId))
        {
            if (string.IsNullOrEmpty(newOrder.DriverName))
                newOrder.DriverName = null;

            Orders.Insert(0, newOrder);
            UpdateDriverStatus();
            InvokeAsync(StateHasChanged);
        }
    }

    private void UpdateOrderStatus(OrderDto updatedOrder)
    {
        var existingOrder = Orders.FirstOrDefault(o => o.OrderId == updatedOrder.OrderId);
        if (existingOrder != null)
        {
            existingOrder.DriverName = updatedOrder.DriverName;
            existingOrder.DriverID = updatedOrder.DriverID;
            existingOrder.OrderState = updatedOrder.OrderState;
            existingOrder.AssignTime = updatedOrder.AssignTime;
            existingOrder.DispatchID = updatedOrder.DispatchID;
            UpdateDriverStatus();
            InvokeAsync(StateHasChanged);
        }
    }

    private void RemoveOrder(int orderId)
    {
        Orders.RemoveAll(o => o.OrderId == orderId);
        UpdateDriverStatus();
        InvokeAsync(StateHasChanged);
    }

    private void UpdateDriverStatus()
    {
        try 
        {
            var busyDriverIds = Orders
                .Where(o => !string.IsNullOrEmpty(o.DriverID))
                .Select(o => o.DriverID)
                .ToHashSet();

            foreach (var driverKey in _commonProperties.Drivers.Keys.ToList())
            {
                if (busyDriverIds.Contains(driverKey.Id))
                {
                    _commonProperties.Drivers[driverKey] = "Unavailable";
                }
                else
                {
                    _commonProperties.Drivers[driverKey] = "Available";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating driver status: {ex.Message}");
        }
    }

    private void OnOrderCollectedHandler(OrderDto collectedOrder)
    {
        Orders.RemoveAll(o => o.OrderId == collectedOrder.OrderId);
        StateHasChanged();
    }

    private void ToggleSelectAll(bool value)
    {
        SelectAllOrders = value;
        if (value)
            SelectedOrderIds = new HashSet<int>(FilteredOrders.Select(o => o.OrderId));
        else
            SelectedOrderIds.Clear();
    }

    private void ToggleOrderSelection(OrderDto order, bool value)
    {
        if (value)
            SelectedOrderIds.Add(order.OrderId);
        else
            SelectedOrderIds.Remove(order.OrderId);

        SelectAllOrders = SelectedOrderIds.Count == FilteredOrders.Count() && FilteredOrders.Any();
    }

    private void PrepareCollection(OrderDto order)
    {
        order.BackTime = DateTime.Now;
        order.CollectorID = _commonProperties.CurrentUserId;
        order.CollectorName = _commonProperties.CurrentUser;
    }

    private async Task CollectSelected()
    {
        var selected = SelectedOrders.ToList();
        if (!selected.Any())
        {
            Snackbar.Add(Localizer["Distribution_NoOrdersSelected"], Severity.Warning);
            return;
        }

        foreach (var order in selected)
        {
            PrepareCollection(order);
            await _distributionService.CollectDeliveryOrder(order);
        }

        Snackbar.Add(Localizer["Distribution_CollectedSelectedSuccess"], Severity.Success);
        await RefreshData();
    }

    private async Task CollectByDriver(string driverId)
    {
        if (string.IsNullOrEmpty(driverId)) return;

        var ordersToCollect = Orders.Where(o => o.DriverID == driverId && o.OrderState == "Dispatched" || o.OrderState == "Delivering").ToList();
        if(!ordersToCollect.Any())
        {
             ordersToCollect = Orders.Where(o => o.DriverID == driverId).ToList();
        }

        foreach (var order in ordersToCollect)
        {
            PrepareCollection(order);
            await _distributionService.CollectDeliveryOrder(order);
        }

        Snackbar.Add(Localizer["Distribution_CollectedDriverSuccess"], Severity.Success);
        await RefreshData();
    }

    private void ToggleAssigned(bool assigned)
    {
        showAssigned = assigned;
        SelectedOrderIds.Clear();
        SelectAllOrders = false;
        StateHasChanged();
    }

    private async Task RefreshData()
    {
        Snackbar.Add(Localizer["Distribution_Updating"], Severity.Info);
        var orders = await _distributionService.GetUnCompletedDeliveryOrders();
        Orders.Clear();
        foreach (var order in orders)
            AddNewDeliveryOrder(order);
        
        SelectedOrderIds.Clear();
        SelectAllOrders = false;
        showDriversDialog = false;
        showAssignmentDialog = false;
        Snackbar.Add(Localizer["Distribution_Updated"], Severity.Success);
        StateHasChanged();
    }

    private async Task UnDispatchOrder(OrderDto order)
    {
        var result = await _distributionService.UnDispatchOrder(order.OrderId);
        if (result)
        {
            order.DriverID = null;
            order.DriverName = null;
            order.AssignTime = null;
            order.DispatchID = null;
            UpdateDriverStatus();
            Snackbar.Add(Localizer["Distribution_UnDispatchedSuccess"], Severity.Success);
            StateHasChanged();
        }
    }

    private async Task CollectAll()
    {
        var dispatchedOrders = FilteredOrders.Where(o => !string.IsNullOrEmpty(o.DriverID)).ToList();
        if (!dispatchedOrders.Any())
        {
            Snackbar.Add(Localizer["Distribution_NoOrdersSelected"], Severity.Warning);
            return;
        }

        foreach (var order in dispatchedOrders)
        {
            PrepareCollection(order);
            await _distributionService.CollectDeliveryOrder(order);
        }

        Snackbar.Add(Localizer["Distribution_CollectedAllSuccess"], Severity.Success);
        await RefreshData();
    }

    private void ExitApp()
    {
        navigationManager.NavigateTo("/");
    }

    private async Task OpenAssignmentDialog(OrderDto order)
    {
        selectedOrder = order;
        selectedDriver = null;
        showAssignmentDialog = true;

        var parameters = new DialogParameters<ChoiceDriverDialog>();
        parameters.Add(x => x.OrderClientName, order.CustomerName);
        parameters.Add(x => x.CurrentDeliveryOrder, order);
        parameters.Add(x => x.OrderAddress, $"{order.AddressNotice}-{order.StreetName}");
        parameters.Add(x => x.OrderTime, order.OrderDate.HasValue ? order.OrderDate.Value.ToString("hh:mm tt") : "");

        _commonProperties.ChoiceDriverDialogReference = await _dialogService.ShowAsync<ChoiceDriverDialog>(Localizer["Distribution_ChoiceDriver"], parameters);
    }

    private void AutoDistribute()
    {
    }

    private void ClearAssignments()
    {
        showAssigned = false;
        Snackbar.Add("تم تفريغ جميع التعيينات", Severity.Warning);
        StateHasChanged();
    }

    private string GetButtonClass(bool isActive)
        => isActive ? "toggle-button-active" : "toggle-button-inactive";

    private bool IsVoidDisabled(OrderDto order)
    {
        if (!_dispatcherSettings.Value.AllowDeliveryVoidAtBranch)
            return true;

        if (order.OrderState == "Dispatched" || order.OrderState == "Delivering")
            return true;

        if (_dispatcherSettings.Value.AllowVoidLimitMinutesForDeliveryOrder && order.OrderDate.HasValue)
        {
            var timeDiff = DateTime.Now.TimeOfDay - order.OrderDate.Value.TimeOfDay;
            if (timeDiff.TotalMinutes > _dispatcherSettings.Value.VoidLimitMinutesForDeliveryOrder)
                return true;
        }

        return false;
    }

    private async Task VoidOrder(OrderDto order)
    {
        var parameters = new DialogParameters<VoidOrderDialog>();
        parameters.Add(x => x.OrderId, order.OrderId);
        parameters.Add(x => x.IsDistribution, true);
        parameters.Add(x => x.IsDineIn, false);

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = await _dialogService.ShowAsync<VoidOrderDialog>(Localizer["Distribution_VoidOrderTitle"], parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await RefreshData();
        }
    }

    private async Task ShowVoidedOrders()
    {
        var appDate = await _appDateService.GetAppDate();
        var voidedOrders = await _distributionService.GetVoidedOrders(appDate.PosDate);

        var parameters = new DialogParameters<VoidedOrdersDialog>();
        parameters.Add(x => x.VoidedOrders, voidedOrders);
        parameters.Add(x => x.PosDate, appDate.PosDate);

        var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };
        await _dialogService.ShowAsync<VoidedOrdersDialog>(Localizer["Distribution_VoidHistory"], parameters, options);
    }

    private async Task ViewOrder(OrderDto context)
    {
        var parameters = new DialogParameters<DeliveryOrderViewDialog>();
        parameters.Add("Order", context);
        parameters.Add(x => x.OnOrderCollected, EventCallback.Factory.Create<OrderDto>(this, OnOrderCollectedHandler));

        _commonProperties.DeliveryOrderViewDialogReference = await _dialogService.ShowAsync<DeliveryOrderViewDialog>(Localizer["Distribution_ViewOrder"], parameters);
    }

    private async Task ShowAvailableDrivers()
    {
        var parameters = new DialogParameters<DriversDialog>();
        parameters.Add(x => x.ShowAvailableDrivers, true);
        _commonProperties.DriversDialogReference = await _dialogService.ShowAsync<DriversDialog>(Localizer["Distribution_AvailableDrivers"], parameters);
    }

    private async Task ShowUnavailableDrivers()
    {
        var parameters = new DialogParameters<DriversDialog>();
        parameters.Add(x => x.ShowAvailableDrivers, false);
        _commonProperties.DriversDialogReference = await _dialogService.ShowAsync<DriversDialog>(Localizer["Distribution_UnavailableDrivers"], parameters);
    }

    private string GetRowClass(OrderDto order, int rowNumber)
    {
        if (order.OrderDate == null)
            return string.Empty;

        var orderTime = order.OrderDate.Value.TimeOfDay;
        var nowTime = DateTime.Now.TimeOfDay;

        var timeDiff = nowTime - orderTime;

        if (timeDiff.TotalMinutes < 0)
            timeDiff = timeDiff.Add(TimeSpan.FromDays(1));

        if (timeDiff.TotalMinutes >= _dispatcherSettings.Value.CriticalTimeForDeliveryOrderPerMinute)
            return "bg-red-100";
        else if (timeDiff.TotalMinutes >= _dispatcherSettings.Value.WarningTimeForDeliveryOrderPerMinute)
            return "bg-yellow-100";

        return string.Empty;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var conn in _externalHubConnections)
        {
            try
            {
                if (conn.State == HubConnectionState.Connected || conn.State == HubConnectionState.Connecting)
                    await conn.StopAsync();

                await conn.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing external hub connection: {ex.Message}");
            }
        }

    }
    public void Dispose()
        => _timer?.Dispose();
}
