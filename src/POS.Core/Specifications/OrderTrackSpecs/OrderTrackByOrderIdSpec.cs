using System;
using System.Linq.Expressions;
using POS.Core.Entities.OrderEntity;

namespace POS.Core.Specifications.OrderTrackSpecs;

public class OrderTrackByOrderIdSpec : BaseSpecifications<OrderTrack>
{
    public OrderTrackByOrderIdSpec(int orderId)
        : base(ot => ot.OrderId == orderId)
    {
        AddOrderByDesc(ot => ot.ActionDateTime);
    }
}
