namespace POS.API.Controllers;

public class OrderController : BaseApiController
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(Orders), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost("createTakeawayOrder")]
    public async Task<IActionResult> CreateTakeawayOrder([FromBody]TakeawayOrderDto takeawayOrderDto)
    {
        if (takeawayOrderDto == null || takeawayOrderDto.OrderDetails == null || !takeawayOrderDto.OrderDetails.Any())
        {
            return BadRequest("Invalid order data.");
        }

        var order = new Orders
        {
            OrderID = takeawayOrderDto.OrderId,
            BranchID = takeawayOrderDto.BranchId,
            BranchName = takeawayOrderDto.BranchName,
            CashierID = takeawayOrderDto.CashierId,
            CashierName = takeawayOrderDto.CashierName,
            TakeawayCustomerName = takeawayOrderDto.CustomerName,
            TakeawayCustomerPhone = takeawayOrderDto.CustomerPhone,
            OrderDate = DateTime.UtcNow,
            OrderType = OrderTypes.TakeAway,
            PaymentMethod = takeawayOrderDto.PaymentMethod,
            OrderState = OrderStates.Completed,
            OrderDetails = takeawayOrderDto.OrderDetails?.Select(detail => new OrderItemsDetails
            {
                MenuSalesItemId = detail.TableItem?.Id,
                Quantity = detail.TableItem?.Quantity,
                TotalAmount = detail.TotalAmount,
                Discount = detail.HasDiscount,
                TotalDiscountPercentage = detail.DiscountPercentage ?? 0M,
                TotalDiscountAmount = detail.DiscountAmount ?? 0M,
                OrderItemAttributes = detail!.TableItem!.Attributes.Select(a => new OrderItemAttributes
                {
                    OrderItemId = detail.TableItem.Id,
                    AttributeItemId = a.Id,
                    AttributeName = a.Name??string.Empty

                }).ToList()
            }).ToList()
        };

        //foreach (var detail in takeawayOrderDto!.OrderDetails ?? [])
        //{
        //    var orderItem = new OrderItemsDetails
        //    {
        //        MenuSalesItemId = detail!.TableItem!.Id,
        //        Quantity = detail.TableItem.Quantity,
        //        TotalAmount = detail.TableItem.Total,
        //        Discount = detail.HasDiscount,
        //        TotalDiscountPercentage = detail.DiscountPercentage ?? 0M,
        //        TotalDiscountAmount = detail.DiscountAmount ?? 0M
        //    };

        //    //todo: add attributes
        //    if (detail.TableItem.Attributes.Any())
        //    {
        //        orderItem.MenuSalesItem = new MenuSalesItems
        //        {
        //            HasAttribute = true,


        //            Attribute = new Attributes
        //            {
        //                EnglishName = string.Join(", ", detail.TableItem.Attributes.Select(a => a.Name)),  // Extract names
        //                ArabicName = string.Join(", ", detail.TableItem.Attributes.Select(a => a.Name ?? "N/A")) // Ensure ArabicName is not NULL
        //            }
        //        };
        //    }

            //order!.OrderDetails!.Add(orderItem);
        //}
        var createdOrder = await _orderService.CreateOrderAsync(order);
        if (createdOrder is null)
            return BadRequest(new ApiResponse(404, "Order Not Created"));
        return Ok(createdOrder);
    }

}
