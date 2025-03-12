using BlazorBase.Models;

namespace BlazorBase.ERPFrontServices.PrintOrderServices;

public interface IPrintOrderService
{
    public Task PrintDineInOrder(DineInOrderDetails orderId);
}