namespace POS.Core.Services.Contract.InventoryServices;
using POS.Core.Entities.Item;

public interface IInventoryService
{
    Task<InventoryItem?> GetInventoryByItemIdAsync(int menuSalesItemId);
    Task<IReadOnlyList<InventoryItem>> GetAllInventoryItemsAsync();
    
    Task UpdateStockAsync(int menuSalesItemId, decimal quantityChange, TransactionType type, string? referenceId = null, string? notes = null);
    
    Task SetOpeningStockAsync(int menuSalesItemId, decimal openingQuantity, string? notes = null);
    Task SetPhysicalStockAsync(int menuSalesItemId, decimal actualQuantity, string? notes = null);
    
    Task ConsumeItemStockAsync(int menuSalesItemId, decimal quantity, TransactionType type = TransactionType.Sale, string? referenceId = null);
    
    Task<InventoryItem> InitializeInventoryAsync(int menuSalesItemId, decimal initialQuantity, decimal minQuantity, int? unitId);
    Task InitializeAllItemsAsync();
    
    Task<IReadOnlyList<InventoryTransaction>> GetTransactionsByItemIdAsync(int menuSalesItemId);
}
