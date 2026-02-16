using System.Linq.Expressions;
using POS.Core.Entities.OrderEntity;

namespace POS.Core.Specifications.OrderSpecs;

public class OrdersByOrderIdSpecs : BaseSpecifications<Orders>
{
    public OrdersByOrderIdSpecs(int orderId)
        : base(o => o.OrderID == orderId)
    {
        Includes.Add(o => o.OrderDetails!);
        AddThenInclude("OrderDetails.OrderItemAttributes");
        AddThenInclude("OrderDetails.OrderItemComments");
        AddThenInclude("OrderDetails.MenuSalesItem");
        AddThenInclude("OrderDetails.MenuSalesItem.Category");
    }
}
