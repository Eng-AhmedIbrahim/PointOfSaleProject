namespace POS.Authorization.Models;



public static class Permissions
{
    public static readonly Dictionary<string, string> RolePermissions = new()
    {
        { "CanAccessTables", "CanAccessTables" },
        { "CanAccessDelivery", "CanAccessDelivery" },
        { "CanAccessTakeAway", "CanAccessTakeAway" },
        { "CanAccessAccounts", "CanAccessAccounts" },
        { "CanAccessSummary", "CanAccessSummary" },
        { "CanAccessOrders", "CanAccessOrders" },
        { "CanAccessDistribution", "CanAccessDistribution" },
        { "CanAccessDiscount", "CanAccessDiscount" },
        { "CanAccessMeals", "CanAccessMeals" },
        { "CanAccessWaiting", "CanAccessWaiting" },
        { "CanAccessSettings", "CanAccessSettings" },
        { "CanAccessKitchen", "CanAccessKitchen" },
        { "CanAccessReport", "CanAccessReport" },
        { "CanAccessVoidOrder", "CanAccessVoidOrder" },
        { "CanAccessTransferTable", "CanAccessTransferTable" },
        { "CanAccessMergeTable", "CanAccessMergeTable" },
        { "CanAccessSplitOrder", "CanAccessSplitOrder" },
        { "CanAccessPrintReceipt", "CanAccessPrintReceipt" },
        { "CanAccessCloseOrder", "CanAccessCloseOrder" },
        { "CanAccessVoidItem", "CanAccessVoidItem" },
        { "CanAccessUsers", "CanAccessUsers" },
        { "CanAccessRoles", "CanAccessRoles" },
        { "CanAccessPosSettings", "CanAccessPosSettings" },
        { "CanAccessPrintingSettings", "CanAccessPrintingSettings" },
        { "CanAccessDeliveryOrderBtn", "CanAccessDeliveryOrderBtn" },
        { "CanAccessDeliveryAddNewBtn", "CanAccessDeliveryAddNewBtn" },
        { "CanAccessDeliveryClearBtn", "CanAccessDeliveryClearBtn" },
        { "CanAccessDeliveryComplaintsBtn", "CanAccessDeliveryComplaintsBtn" },
        { "CanAccessDeliverySearchBtn", "CanAccessDeliverySearchBtn" },
        { "CanAccessDeliveryBranchManagementBtn", "CanAccessDeliveryBranchManagementBtn" },
        { "CanAccessDeliveryDistributionBtn", "CanAccessDeliveryDistributionBtn" },
        { "CanAccessDeliveryToggleDirectionBtn", "CanAccessDeliveryToggleDirectionBtn" },
    };
}

