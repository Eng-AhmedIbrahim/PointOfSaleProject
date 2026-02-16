namespace POS.Contract.Dtos.OrderDtos;

public class DriverSettlementDto
{
    public string DriverId { get; set; }
    public string DriverName { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalBonus { get; set; }
}
