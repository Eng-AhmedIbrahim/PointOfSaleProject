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
    };
}

