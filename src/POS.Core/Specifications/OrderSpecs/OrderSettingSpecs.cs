namespace POS.Core.Specifications.OrderSpecs;

public class OrderSettingSpecs:BaseSpecifications<OrderSetting>
{
    public OrderSettingSpecs(OrderTypes orderTypes):base(x=>x.OrderType==orderTypes.ToString())
    {
    }
}