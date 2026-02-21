namespace POS.Authorization.Models;

public static class Permissions
{
    public static readonly Dictionary<string, string> RolePermissions = new()
    {
        // ── Screens / Nav ────────────────────────────────────────────────────
        { "CanAccessTables",       "CanAccessTables" },
        { "CanAccessDelivery",     "CanAccessDelivery" },
        { "CanAccessTakeAway",     "CanAccessTakeAway" },
        { "CanAccessAccounts",     "CanAccessAccounts" },
        { "CanAccessSummary",      "CanAccessSummary" },
        { "CanAccessOrders",       "CanAccessOrders" },
        { "CanAccessDistribution", "CanAccessDistribution" },
        { "CanAccessDiscount",     "CanAccessDiscount" },
        { "CanAccessMeals",        "CanAccessMeals" },
        { "CanAccessWaiting",      "CanAccessWaiting" },
        { "CanAccessSettings",     "CanAccessSettings" },
        { "CanAccessKitchen",      "CanAccessKitchen" },
        { "CanAccessReport",       "CanAccessReport" },

        // ── POS Actions ───────────────────────────────────────────────────────
        { "CanAccessVoidOrder",       "CanAccessVoidOrder" },
        { "CanAccessTransferTable",   "CanAccessTransferTable" },
        { "CanAccessMergeTable",      "CanAccessMergeTable" },
        { "CanAccessSplitOrder",      "CanAccessSplitOrder" },
        { "CanAccessPrintReceipt",    "CanAccessPrintReceipt" },
        { "CanAccessCloseOrder",      "CanAccessCloseOrder" },
        { "CanAccessVoidItem",        "CanAccessVoidItem" },

        // ── Settings ──────────────────────────────────────────────────────────
        { "CanAccessUsers",            "CanAccessUsers" },
        { "CanAccessRoles",            "CanAccessRoles" },
        { "CanAccessPosSettings",      "CanAccessPosSettings" },
        { "CanAccessPrintingSettings", "CanAccessPrintingSettings" },

        // ── Footer Buttons ────────────────────────────────────────────────────
        { "CanAccessFooterDiscountBtn",      "CanAccessFooterDiscountBtn" },
        { "CanAccessFooterCustomerDataBtn",  "CanAccessFooterCustomerDataBtn" },
        { "CanAccessFooterPaymentMethodBtn", "CanAccessFooterPaymentMethodBtn" },
        { "CanAccessFooterQuickPaymentBtn",  "CanAccessFooterQuickPaymentBtn" },
        { "CanAccessFooterMealsBtn",         "CanAccessFooterMealsBtn" },
        { "CanAccessFooterWaitingBtn",       "CanAccessFooterWaitingBtn" },
        { "CanAccessFooterSettingsBtn",      "CanAccessFooterSettingsBtn" },

        // ── DineIn Buttons ────────────────────────────────────────────────────
        { "CanAccessDineInOrderBtn",       "CanAccessDineInOrderBtn" },
        { "CanAccessDineInReceiptBtn",     "CanAccessDineInReceiptBtn" },
        { "CanAccessDineInCloseTableBtn",  "CanAccessDineInCloseTableBtn" },
        { "CanAccessDineInSplitOrderBtn",  "CanAccessDineInSplitOrderBtn" },
        { "CanAccessDineInMergeTableBtn",  "CanAccessDineInMergeTableBtn" },
        { "CanAccessDineInTransferBtn",    "CanAccessDineInTransferBtn" },
        { "CanAccessDineInVoidBtn",        "CanAccessDineInVoidBtn" },
        { "CanAccessDineInGuestCountBtn",  "CanAccessDineInGuestCountBtn" },

        // ── Delivery Buttons ──────────────────────────────────────────────────
        { "CanAccessDeliveryOrderBtn",              "CanAccessDeliveryOrderBtn" },
        { "CanAccessDeliveryAddNewBtn",             "CanAccessDeliveryAddNewBtn" },
        { "CanAccessDeliveryClearBtn",              "CanAccessDeliveryClearBtn" },
        { "CanAccessDeliveryComplaintsBtn",         "CanAccessDeliveryComplaintsBtn" },
        { "CanAccessDeliverySearchBtn",             "CanAccessDeliverySearchBtn" },
        { "CanAccessDeliveryBranchManagementBtn",   "CanAccessDeliveryBranchManagementBtn" },
        { "CanAccessDeliveryDistributionBtn",       "CanAccessDeliveryDistributionBtn" },
        { "CanAccessDeliveryToggleDirectionBtn",    "CanAccessDeliveryToggleDirectionBtn" },

        // ── All Orders Buttons ────────────────────────────────────────────────
        { "CanAccessAllOrdersViewBtn",  "CanAccessAllOrdersViewBtn" },
        { "CanAccessAllOrdersPrintBtn", "CanAccessAllOrdersPrintBtn" },
        { "CanAccessAllOrdersVoidBtn",  "CanAccessAllOrdersVoidBtn" },

        // ── Distribution Buttons ──────────────────────────────────────────────
        { "CanAccessDistributionAssignBtn",           "CanAccessDistributionAssignBtn" },
        { "CanAccessDistributionViewBtn",             "CanAccessDistributionViewBtn" },
        { "CanAccessDistributionVoidBtn",             "CanAccessDistributionVoidBtn" },
        { "CanAccessDistributionPrintBtn",            "CanAccessDistributionPrintBtn" },
        { "CanAccessDistributionUnDispatchBtn",       "CanAccessDistributionUnDispatchBtn" },
        { "CanAccessDistributionCollectBtn",          "CanAccessDistributionCollectBtn" },
        { "CanAccessDistributionVoidHistoryBtn",      "CanAccessDistributionVoidHistoryBtn" },
        { "CanAccessDistributionDriverSettlementBtn", "CanAccessDistributionDriverSettlementBtn" },
        { "CanAccessDistributionViewDriversBtn",      "CanAccessDistributionViewDriversBtn" },

        // ── Accounts Buttons ──────────────────────────────────────────────────
        { "CanAccessAccountsViewBtn",  "CanAccessAccountsViewBtn" },
        { "CanAccessAccountsPrintBtn", "CanAccessAccountsPrintBtn" },

        // ── Summary Buttons ───────────────────────────────────────────────────
        { "CanAccessSummaryViewDetailsBtn", "CanAccessSummaryViewDetailsBtn" },
        { "CanAccessSummaryPrintBtn",       "CanAccessSummaryPrintBtn" },

        // ── Global Features ───────────────────────────────────────────────────
        { "CanAccessPosSettingsFeature", "CanAccessPosSettingsFeature" },
    };
}
