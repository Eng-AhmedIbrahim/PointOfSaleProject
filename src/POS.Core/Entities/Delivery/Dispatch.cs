namespace POS.Core.Entities.Delivery;

public class Dispatch:BaseEntity
{
    public int BranchID { get; set; } = 1;
    public DateTime? OrderDate { get; set; }
    public int? UserID { get; set; } 
    public string? UserName { get; set; } 
    public int? DriverID { get; set; }
    public string? DriverName { get; set; } 
    public int? OrderID { get; set; }
    public Orders? Order { get; set; }
}