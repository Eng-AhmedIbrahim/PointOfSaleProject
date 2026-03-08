using POS.Core.Services.Contract.InventoryServices;
using POS.Core.Specifications;
using POS.Core.Specifications.InventorySpecs;
using POS.Core.Services.Contract.PosFeatureServices;

namespace POS.Services.InventoryServices;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecipeService _recipeService;
    private readonly IPosFeatureSettingsService _featureSettings;

    public InventoryService(IUnitOfWork unitOfWork, 
    IRecipeService recipeService, 
    IPosFeatureSettingsService featureSettings)
    {
        _unitOfWork = unitOfWork;
        _recipeService = recipeService;
        _featureSettings = featureSettings;
    }

    public async Task<InventoryItem?> GetInventoryByItemIdAsync(int menuSalesItemId)
    {
        try
        {
            var spec = new InventoryItemByMenuSalesItemIdSpecification(menuSalesItemId);
            return await _unitOfWork.Repository<InventoryItem>().GetByIdWithSpecificationTrackedAsync(spec);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting inventory for item {itemId}", menuSalesItemId);
            return null;
        }
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllInventoryItemsAsync()
    {
        try
        {
            return await _unitOfWork.Repository<InventoryItem>().GetAllAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all inventory items");
            return new List<InventoryItem>();
        }
    }

    public async Task ConsumeItemStockAsync(int menuSalesItemId, decimal quantity, TransactionType type = TransactionType.Sale, string? referenceId = null)
    {
        if (!await _featureSettings.IsFeatureEnabledAsync("EnableInventoryTracking")) return;

        try 
        {
            var recipe = await _recipeService.GetRecipeByItemIdAsync(menuSalesItemId);
            
            string actionWord = type == TransactionType.Void ? "Returned" : "Consumed";
            decimal factor = type == TransactionType.Void ? 1 : -1;

            if (recipe != null && recipe.Ingredients.Any())
            {
                foreach (var ingredient in recipe.Ingredients)
                {
                    decimal totalRequiredAction = ingredient.Quantity * quantity;
                    await UpdateStockAsync(
                        ingredient.MenuSalesIngredientId, 
                        totalRequiredAction * factor, 
                        type, 
                        referenceId, 
                        $"{actionWord} for {recipe.RecipeName} x {quantity}");
                }
            }
            else 
            {
                var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
                if (inventory != null && inventory.TrackInventory)
                {
                    await UpdateStockAsync(
                        menuSalesItemId, 
                        quantity * factor, 
                        type, 
                        referenceId, 
                        $"Item {actionWord.ToLower()} x {quantity}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error consuming stock for item {itemId}", menuSalesItemId);
        }
    }

    public async Task UpdateStockAsync(int menuSalesItemId, decimal quantityChange, TransactionType type, string? referenceId = null, string? notes = null)
    {
        try
        {
            var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
            if (inventory == null || !inventory.TrackInventory) return;

            inventory.CurrentQuantity += quantityChange;
            inventory.UpdatedAt = DateTime.UtcNow;

            var transaction = new InventoryTransaction
            {
                InventoryItemId = inventory.Id,
                QuantityChange = quantityChange,
                ResultingQuantity = inventory.CurrentQuantity,
                Type = type,
                ReferenceId = referenceId,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<InventoryItem>().Update(inventory);
            await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);
            
            Console.WriteLine($"[INVENTORY DEBUG] Calling CompleteAsync for ItemId: {menuSalesItemId}, Change: {quantityChange}");
            var result = await _unitOfWork.CompleteAsync();
            Console.WriteLine($"[INVENTORY DEBUG] CompleteAsync result: {result}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating stock for item {itemId}", menuSalesItemId);
        }
    }

    public async Task<InventoryItem> InitializeInventoryAsync(int menuSalesItemId, decimal initialQuantity, decimal minQuantity, int? unitId)
    {
        var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
        
        var branch = (await _unitOfWork.Repository<Branch>().GetAllAsync()).FirstOrDefault();
        var branchId = branch?.Id;

        // Fetch metadata
        var item = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(menuSalesItemId);
        Unit? unit = unitId.HasValue ? await _unitOfWork.Repository<Unit>().GetByIdAsync(unitId.Value) : null;
        Category? category = item?.CategoryId.HasValue == true ? await _unitOfWork.Repository<Category>().GetByIdAsync(item.CategoryId.Value) : null;

        if (inventory == null)
        {
            inventory = new InventoryItem
            {
                MenuSalesItemId = menuSalesItemId,
                CurrentQuantity = initialQuantity,
                MinimumQuantity = minQuantity,
                UnitId = unitId,
                TrackInventory = true,
                CreatedAt = DateTime.UtcNow,
                BranchId = branchId
            };
            await _unitOfWork.Repository<InventoryItem>().AddAsync(inventory);
            
            var transaction = new InventoryTransaction
            {
                InventoryItem = inventory,
                QuantityChange = initialQuantity,
                ResultingQuantity = initialQuantity,
                Type = TransactionType.StockIn,
                Notes = "Initial setup",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);
        }
        else 
        {
            inventory.UnitId = unitId;
            inventory.MinimumQuantity = minQuantity;
        }

        // Keep metadata updated
        if (item != null)
        {
            inventory.ItemNameAr = item.ArabicName;
            inventory.ItemNameEn = item.EnglishName;
            inventory.CategoryId = item.CategoryId;
            
            if (category != null)
            {
                inventory.CategoryNameAr = category.ArabicName;
                inventory.CategoryNameEn = category.EnglishName;
            }
        }

        if (unit != null)
        {
            inventory.UnitNameAr = unit.ArabicName;
            inventory.UnitNameEn = unit.EnglishName;
        }

        if (inventory.Id > 0)
        {
            _unitOfWork.Repository<InventoryItem>().Update(inventory);
        }

        await _unitOfWork.CompleteAsync();
        return inventory;
    }

    public async Task InitializeAllItemsAsync()
    {
        try
        {
            var branch = (await _unitOfWork.Repository<Branch>().GetAllAsync()).FirstOrDefault();
            var branchId = branch?.Id;

            // Load all items with categories and units to populate denormalized fields
            var allItems = await _unitOfWork.Repository<MenuSalesItems>().GetAllAsync();
            var allUnits = await _unitOfWork.Repository<Unit>().GetAllAsync();
            var allCategories = await _unitOfWork.Repository<Category>().GetAllAsync();

            var unitMap = allUnits.ToDictionary(u => u.Id);
            var categoryMap = allCategories.ToDictionary(c => c.Id);

            var existingInventory = await _unitOfWork.Repository<InventoryItem>().GetAllAsync();
            var inventoryMap = existingInventory.ToDictionary(x => x.MenuSalesItemId);

            foreach (var item in allItems)
            {
                if (!inventoryMap.TryGetValue(item.Id, out var inventory))
                {
                    inventory = new InventoryItem
                    {
                        MenuSalesItemId = item.Id,
                        CurrentQuantity = 0,
                        MinimumQuantity = 0,
                        TrackInventory = true,
                        CreatedAt = DateTime.UtcNow,
                        BranchId = branchId
                    };
                    await _unitOfWork.Repository<InventoryItem>().AddAsync(inventory);
                }

                // Always update metadata to keep it in sync
                inventory.ItemNameAr = item.ArabicName;
                inventory.ItemNameEn = item.EnglishName;
                inventory.CategoryId = item.CategoryId;
                
                if (item.CategoryId.HasValue && categoryMap.TryGetValue(item.CategoryId.Value, out var category))
                {
                    inventory.CategoryNameAr = category.ArabicName;
                    inventory.CategoryNameEn = category.EnglishName;
                }

                if (inventory.UnitId.HasValue && unitMap.TryGetValue(inventory.UnitId.Value, out var unit))
                {
                    inventory.UnitNameAr = unit.ArabicName;
                    inventory.UnitNameEn = unit.EnglishName;
                }
                
                if (inventory.Id > 0)
                {
                    _unitOfWork.Repository<InventoryItem>().Update(inventory);
                }
            }
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing all inventory items with metadata");
        }
    }

    public async Task SetOpeningStockAsync(int menuSalesItemId, decimal openingQuantity, string? notes = null)
    {
        try
        {
            var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
            if (inventory == null) return;

            decimal difference = openingQuantity - inventory.CurrentQuantity;
            inventory.CurrentQuantity = openingQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            var transaction = new InventoryTransaction
            {
                InventoryItemId = inventory.Id,
                QuantityChange = difference,
                ResultingQuantity = openingQuantity,
                Type = TransactionType.OpeningStock,
                Notes = notes ?? "Opening stock entry",
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<InventoryItem>().Update(inventory);
            await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting opening stock for item {itemId}", menuSalesItemId);
        }
    }

    public async Task SetPhysicalStockAsync(int menuSalesItemId, decimal actualQuantity, string? notes = null)
    {
        try
        {
            var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
            if (inventory == null) return;

            decimal difference = actualQuantity - inventory.CurrentQuantity;
            inventory.CurrentQuantity = actualQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            var transaction = new InventoryTransaction
            {
                InventoryItemId = inventory.Id,
                QuantityChange = difference,
                ResultingQuantity = actualQuantity,
                Type = TransactionType.Adjustment,
                Notes = notes ?? "Physical stock adjustment",
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<InventoryItem>().Update(inventory);
            await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting physical stock for item {itemId}", menuSalesItemId);
        }
    }

    public async Task<IReadOnlyList<InventoryTransaction>> GetTransactionsByItemIdAsync(int menuSalesItemId)
    {
        var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
        if (inventory == null) return new List<InventoryTransaction>();

        var spec = new InventoryTransactionsByInventoryIdSpecification(inventory.Id);
        return await _unitOfWork.Repository<InventoryTransaction>().GetAllWithSpecificationAsync(spec);
    }
}
