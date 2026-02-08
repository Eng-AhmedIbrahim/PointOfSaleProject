namespace BlazorBase.ERPFrontServices.PrintOrderServices;

public interface IPrintOrderService
{
    public Task PrintInitialDineInOrder(DineInOrderDetails orderId, bool printCustomer = true, bool printKitchen = true, bool isClosing = false);
    public Task PrintDineInClosingReceipt(DineInOrderDetails orderId);
    public Task<bool> PrintTakeAwayOrder(decimal paid = 0, string customerName = "", string customerPhone = "", PaymentMethod paymentMethod = PaymentMethod.Cash);
    public Task<bool> ReprintOrderAsync(int orderId);
}