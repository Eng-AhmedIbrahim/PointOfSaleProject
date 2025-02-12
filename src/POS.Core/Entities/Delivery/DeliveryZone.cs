namespace POS.Core.Entities.Delivery;

public class DeliveryZone : BaseEntity
{
    public string? ZoneName { get; set; }
    public decimal? DeliveryFee { get; set; }
}