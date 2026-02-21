using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.ReportingDtos;
using POS.Contract.Models;

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
    public Task<bool> ReprintOrderAsync(int orderId, bool isCopy = false, bool printCustomer = true, bool printKitchen = true);
    public Task PrintReceivedOrderAsync(OrderDto order);
    public Task PrintDispatchOrderAsync(OrderDto order);
    public Task PrintVoidReceiptAsync(OrderDto order, List<TableItem> voidedItems);
    public Task PrintDineInVoidReceiptAsync(DineInOrderDto order, List<TableItem> voidedItems);
    public Task PrintSalesSummaryAsync(SalesSummaryDto summary, List<SalesItemSummaryDto> items);
}