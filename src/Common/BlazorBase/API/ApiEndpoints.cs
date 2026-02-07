namespace BlazorBase.API;

public record ApiEndpoints
{
    public string? GetAllCategories { get; set; }
    public string? GetItemsByCategoryId { get; set; }
    public string? GetAllRoles { get; set; }
    public string? GetUserPermissions { get; set; }
    public string? GetOrderSettings { get; set; }
    public string? GetTableGroups { get; set; }
    public string? GetTablesByGroupId { get; set; }
    public string? GetUsersByRole { get; set; }
    public string? GetAppDate { get; set; }
    public string? UpdateAppDate { get; set; }
    public string? UpdateOrderNumber { get; set; }
    public string? UpdateTables { get; set; }
    public string? GetAllDeliveryCustomerTitles { get; set; }
    public string? GetBranches { get; set; }
    public string? GetZoneByBranchId { get; set; }
    public string? GetCustomerByPhone { get; set; }
    public string? CreateNewCustomer { get; set; }
    public string? AddNewCustomerAddress { get; set; }
    public string? CreateOrder { get; set; }
    
    // DineIn Order endpoints
    public string? CreateDineInOrder { get; set; }
    public string? UpdateDineInOrder { get; set; }
    public string? GetDineInOrderByTableId { get; set; }
    public string? GetOpenOrdersByTableId { get; set; }
    public string? GetAllOpenDineInOrders { get; set; }
    public string? CloseDineInOrder { get; set; }
    public string? VoidDineInOrder { get; set; }
    public string? AddItemsToDineInOrder { get; set; }
    public string? UpdateDineInOrderDiscount { get; set; }
    public string? TransferDineInOrder { get; set; }
    public string? MergeDineInOrders { get; set; }
    public string? SplitDineInOrder { get; set; }
    public string? VoidDineInItems { get; set; }
    
    // Order Track endpoints
    public string? TrackOrderAction { get; set; }
    public string? GetOrderTrackingHistory { get; set; }
    public string? GetOrderTrackingByDateRange { get; set; }
}