namespace BlazorBase.ERPFrontServices.PrintOrderServices;

public interface IPrintOrderService
{
    public Task PrintDineInOrder(DineInOrderDetails orderId);
    public Task<bool> PrintTakeAwayOrder(decimal paid = 0.00m, string customerName = "", string customerPhone = "");
}