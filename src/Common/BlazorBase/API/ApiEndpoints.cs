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
}