namespace BlazorBase.ERPFrontServices.PrintOrderServices;

public interface IPrintOrderService
{
    public Task PrintInitialDineInOrder(DineInOrderDetails orderId);
    public Task<bool> PrintTakeAwayOrder(decimal paid = 0, string customerName = "", string customerPhone = "", PaymentMethod paymentMethod = PaymentMethod.Cash);
    public Task<bool> ReprintOrderAsync(int orderId);
}