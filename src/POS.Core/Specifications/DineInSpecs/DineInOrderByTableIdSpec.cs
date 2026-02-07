using System;
using System.Linq.Expressions;
using POS.Core.Entities.DineIn;

namespace POS.Core.Specifications.DineInSpecs;

public class DineInOrderByTableIdSpec : BaseSpecifications<Orders>
{
    public DineInOrderByTableIdSpec(int tableId, string state = "Open")
        : base(o => o.TableID == tableId && 
                   o.OrderType == OrderTypes.DineIn && 
                   (state == "Open" ? o.OrderState == OrderStates.Pending : o.OrderState == OrderStates.Completed))
    {
        Includes.Add(o => o.OrderDetails!);
        AddThenInclude("OrderDetails.OrderItemAttributes");
        AddThenInclude("OrderDetails.MenuSalesItem");
        AddOrderByDesc(o => o.OrderDate);
    }
}
