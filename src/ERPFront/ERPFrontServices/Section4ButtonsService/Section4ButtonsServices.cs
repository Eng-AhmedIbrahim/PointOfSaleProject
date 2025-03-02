using POS.Contract.Models;

namespace ERPFront.ERPFrontServices.Section4ButtonsService;

public class Section4ButtonsServices : ISection4ButtonsServices
{
    private readonly CartService? _cartService;
    private readonly CommonProperties _commonProperties;

    private int _nextOrderId = 1;

    public Section4ButtonsServices(CartService cartService, CommonProperties commonProperties)
    {
        _cartService = cartService;
        _commonProperties = commonProperties;
    }
    public event Action? OnChanged;


    public void RemoveAllItems(List<TableItem> tableItems)
    {
        tableItems.Clear();
        _cartService!.CalculateTotalAmountFromTableItems(new List<TableItem>());
        NotifyStateChanged();
    }
    public void AddOrderToWaitingQueue(List<TableItem> tableItems)
    {
        if (tableItems.Any())
        {
            if (_commonProperties!.WaitingQueue!.WaitingOrders.Count == 0)
            {
                _nextOrderId = 1;
            }

            _commonProperties.WaitingQueue.WaitingOrders.Add(new()
            {
                Id = _nextOrderId++,
                Items = new List<TableItem>(tableItems)
            });

            RemoveAllItems(tableItems);
            Console.WriteLine(_commonProperties.WaitingQueue.WaitingOrders.Count);
        }
    }

    private void NotifyStateChanged() => OnChanged?.Invoke();
}