namespace POS.Services.ItemServices;

public class MenuSalesItemService : IMenuSalesItemService
{
    private readonly IUnitOfWork _unitOfWork;

    public MenuSalesItemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MenuSalesItems?> CreateItemAsync(MenuSalesItems item)
    {
        try
        {
            if (item is null)
                return null;

            await _unitOfWork.Repository<MenuSalesItems>().AddAsync(item);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            return item;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating Item.");
            return null;
        }
    }
    public async Task<bool> DeleteItem(MenuSalesItems item)
    {
        try
        {
            if (item is null)
                return false;

            _unitOfWork.Repository<MenuSalesItems>().Delete(item);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return false;

            return true;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cant Delete Company With Id {companyId}", item.Id);
            return false;
        }
    }
    public async Task<IReadOnlyList<MenuSalesItems>?> GetAllItemsAsync()
    {
        try
        {
            var itemSpecs = new MenuSalesItemsWithIncludeSpec();
            var items = await _unitOfWork.Repository<MenuSalesItems>().GetAllWithSpecificationAsync(itemSpecs);

            if (items is null)
                return null;

            return items;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Items");
            return null;
        }
    }
    public async Task<MenuSalesItems?> GetItemByIdAsync(int itemId)
    {
        try
        {
            var itemSpecs = new MenuSalesItemsWithIncludeSpec(itemId);
            var item = await _unitOfWork.Repository<MenuSalesItems>().GetByIdWithSpecificationAsync(itemSpecs);

            if (item is null)
                return default;

            return item;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Item With This Id {itemId}", itemId);
            return default;
        }
    }
    public async Task<MenuSalesItems?> UpdateItemAsync(MenuSalesItems oldItem, MenuSalesItems newItem)
    {
        try
        {

            oldItem.Id = newItem.Id;
            if (!string.IsNullOrEmpty(newItem.ArabicName))
                oldItem.ArabicName = newItem.ArabicName;
            if (!string.IsNullOrEmpty(newItem.EnglishName))
                oldItem.EnglishName = newItem.EnglishName;

            if (newItem.Price != oldItem.Price || newItem.Price != null)
                oldItem.Price = newItem.Price;

            if (newItem.SecondPrice != oldItem.SecondPrice || newItem.SecondPrice != null)
                oldItem.SecondPrice = newItem.SecondPrice;

            if (newItem.ThirdPrice != oldItem.ThirdPrice || newItem.ThirdPrice != null)
                oldItem.ThirdPrice = newItem.ThirdPrice;

            if (newItem.FourthPrice != oldItem.FourthPrice || newItem.FourthPrice != null)
                oldItem.FourthPrice = newItem.FourthPrice;

            if (newItem.FifthPrice != oldItem.FifthPrice || newItem.FifthPrice != null)
                oldItem.FifthPrice = newItem.FifthPrice;

            if (!string.IsNullOrEmpty(newItem.Description))
                oldItem.Description = newItem.Description;

            if (newItem.CategoryId != oldItem.CategoryId || newItem.CategoryId != null)
                oldItem.CategoryId = newItem.CategoryId;

            if (newItem.Tax != oldItem.Tax || newItem.Tax != null)
                oldItem.Tax = newItem.Tax;

            if (!string.IsNullOrEmpty(newItem.BackColor))
                oldItem.BackColor = newItem.BackColor;

            if (!string.IsNullOrEmpty(newItem.TextColor))
                oldItem.TextColor = newItem.TextColor;

            if (!string.IsNullOrEmpty(newItem.MainCategoryId.ToString()))
                oldItem.MainCategoryId = newItem.MainCategoryId;


            if (!string.IsNullOrEmpty(newItem.Barcode))
                oldItem.Barcode = newItem.Barcode;

            if (newItem.TextSize != oldItem.TextSize || newItem.TextSize != null)
                oldItem.TextSize = newItem.TextSize;

            if (!string.IsNullOrEmpty(newItem.ImagePath))
                oldItem.ImagePath = newItem.ImagePath;

            if (!string.IsNullOrEmpty(newItem.ImagePath))
                oldItem.ImagePath = newItem.ImagePath;

            if (newItem.Invisible != oldItem.Invisible)
                oldItem.Invisible = newItem.Invisible;

            _unitOfWork.Repository<MenuSalesItems>().Update(oldItem);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            return oldItem ?? null;
        }
        catch (Exception ex)

        {
            Log.Error(ex, "Error Occur During Update Item That Have Id {itemId}", oldItem.Id);
            return null;

        }
    }
    public async Task<MenuSalesItems?> AddAttributeToItem(int attributeId, int itemId)
    {
        try
        {
           var item =  await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(itemId);
            if (item is null)
                return null;
            
            item.AttributeId = attributeId;

            var result  = await _unitOfWork.CompleteAsync();
            if(result <= 0) return null;

            var itemSpecs = new MenuSalesItemsWithIncludeSpec(itemId);
            var itemWithAttribute = await _unitOfWork.Repository<MenuSalesItems>().GetByIdWithSpecificationAsync(itemSpecs);

            //if(itemWithAttribute is null)
            //    return null;

            return itemWithAttribute;

        }catch(Exception ex)
        {
            Log.Error(ex, "Error Ocurr During Add Attribute To Item That Has Id {itemId}",itemId);
            return null;
        }
    }

    public async Task<IReadOnlyList<MenuSalesItems>?> GetItemByCategoryIdAsync(int catId)
    {
        try
        {
            var itemSpecs = new ItemsSpecs(c=>c.CategoryId == catId);
            var items = await _unitOfWork.Repository<MenuSalesItems>().GetAllWithSpecificationAsync(itemSpecs);


            if (items == null) return null;

            return items;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return null;
        }
    }
}