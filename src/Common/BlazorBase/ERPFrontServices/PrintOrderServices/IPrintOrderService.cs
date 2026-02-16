namespace BlazorBase.ERPFrontServices.PrintOrderServices;

public interface IPrintOrderService
{
    public Task PrintInitialDineInOrder(DineInOrderDetails orderId, 
        bool printCustomer = true, 
        bool printKitchen = true, 
        bool isClosing = false,
        bool isUpdate = false);

    public Task PrintDineInClosingReceipt(DineInOrderDetails orderId);

    public Task<bool> PrintTakeAwayOrder(decimal paid = 0, 
        string customerName = "", 
        string customerPhone = "",
        PaymentMethod paymentMethod = PaymentMethod.Cash);

    public Task<bool> PrintDeliveryOrder(decimal paid = 0);
    public Task<bool> ReprintOrderAsync(int orderId);
    public Task PrintReceivedOrderAsync(OrderDto order);
    public Task PrintDispatchOrderAsync(OrderDto order);
}