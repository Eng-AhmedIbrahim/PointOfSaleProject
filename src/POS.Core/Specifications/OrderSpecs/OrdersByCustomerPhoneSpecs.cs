using POS.Core.Entities.OrderEntity;

namespace POS.Core.Specifications.OrderSpecs;

public class OrdersByCustomerPhoneSpecs : BaseSpecifications<Orders>
{
    public OrdersByCustomerPhoneSpecs(string phoneNumber)
        : base(o => o.Phone1 == phoneNumber || o.Phone2 == phoneNumber || o.TakeawayCustomerPhone == phoneNumber)
    {
        // Order by date DESC - most recent first for efficient stats calculation
        AddOrderByDesc(o => o.OrderDate);
        
        // Note: We intentionally don't include OrderDetails here to improve performance
        // If you need OrderDetails, create a separate specification
    }
}
