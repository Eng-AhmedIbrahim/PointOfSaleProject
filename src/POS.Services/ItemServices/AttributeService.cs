namespace POS.Services.ItemServices;

public class AttributeService : IAttributeService
{
    private readonly IUnitOfWork _unitOfWork;

    public AttributeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Attributes?> CreateAttributeAsync(Attributes attribute)
    {
        try
        {
            if (attribute is null)
                return null;

            var attributeItems = await GetAllAttributeAsync();
            int newId;

            if (attributeItems is not null && attributeItems.Any())
            {
                newId = attributeItems.Max(attr => attr.Id) + 1;
            }
            else
            {
                newId = 1;
            }

            attribute.Id = newId;

            await _unitOfWork.Repository<Attributes>().AddAsync(attribute);
            await AddAttributeItems(attribute.AttributeItems);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            return attribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating Attribute.");
            return null;
        }
    }
    public async Task<bool> DeleteAttribute(Attributes attribute)
    {
        try
        {
            if (attribute is null)
                return false;

            _unitOfWork.Repository<Attributes>().Delete(attribute);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return false;

            return true;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cant Delete Attribute With Id {attributeId}", attribute.Id);
            return false;
        }
    }
    public async Task<IReadOnlyList<Attributes>?> GetAllAttributeAsync()
    {
        try
        {
            var attributes = await _unitOfWork.Repository<Attributes>().GetAllAsync();

            if (attributes is null)
                return null;

            return attributes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Attributes");
            return null;
        }
    }
    public async Task<Attributes?> GetAttributeByIdAsync(int attributeId)
    {
        try
        {
            var attribute = await _unitOfWork.Repository<Attributes>().GetByIdAsync(attributeId);

            if (attribute is null)
                return null;

            return attribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Attribute With This Id {attributeId}", attributeId);
            return null;
        }
    }
    public async Task<Attributes?> UpdateAttributeAsync(Attributes oldAttribute, Attributes newAttribute)
    {
        try
        {
            oldAttribute.Id = newAttribute.Id;
            if (!string.IsNullOrEmpty(newAttribute.ArabicName))
                oldAttribute.ArabicName = newAttribute.ArabicName;
            if (!string.IsNullOrEmpty(newAttribute.EnglishName))
                oldAttribute.EnglishName = newAttribute.EnglishName;

            var itemsIds = oldAttribute.AttributeItems.Select(x => x.RelatedMenuItemId).ToHashSet(); // 2 3 
            //int deletedItemId; 
            foreach (var newAtt in newAttribute.AttributeItems)
            {
                if (itemsIds.Contains(newAtt.RelatedMenuItemId))  // 2 , 3  5
                    continue;
                else
                {
                    //var attributeItem = await _unitOfWork.Repository<AttributeItem>().GetByIdAsync()
                    //_unitOfWork.Repository<AttributeItem>().Delete()
                    oldAttribute.AttributeItems.Add(newAtt);
                }
            }

            _unitOfWork.Repository<Attributes>().Update(oldAttribute);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            return oldAttribute ?? null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Occur During Update Attribute That Have Id {attributeId}", oldAttribute.Id);
            return null;
        }
    }
    public async Task<ICollection<AttributeItem>?> AddAttributeItems(ICollection<AttributeItem> attributeItems)
    {
        try
        {
            if (attributeItems is null)
                return null;

            await _unitOfWork.Repository<AttributeItem>().AddRangeAsync(attributeItems);
            return attributeItems;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Occur During Update AttributeItems");
            return null;
        }
    }
    public async Task<Attributes?> GetAttributeByIdWithSpecAsync(ISpecifications<Attributes> attributeSpecifications)
    {
        try
        {
            var attribute = await _unitOfWork.Repository<Attributes>().GetByIdWithSpecificationAsync(attributeSpecifications);

            if (attribute is null)
                return null;

            return attribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are No Attribute Has This Id");
            return null;
        }
    }
    public async Task<IReadOnlyList<Attributes>?> GetAllAttributeWithSpecsAsync(ISpecifications<Attributes> attributeSpecifications)
    {
        try
        {
            var attributes = await _unitOfWork.Repository<Attributes>().GetAllWithSpecificationAsync(attributeSpecifications);
            if (attributes is null)
                return null;

            return attributes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are No Attributes");
            return null;
        }
    }
    public async Task<bool> DeleteAttributeItem(AttributeItem attributeItem)
    {
        try
        {
            if (attributeItem is null)
                return false;

            _unitOfWork.Repository<AttributeItem>().Delete(attributeItem);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return false;

            return true;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cant Delete AttributeItem With Id {attributeId}", attributeItem.Id);
            return false;
        }
    }

    public async Task<AttributeItem?> GetAttributeItemByIdAsync(int attributeItemId)
    {
        try
        {
            var attribute = await _unitOfWork.Repository<AttributeItem>().GetByIdAsync(attributeItemId);

            if (attribute is null)
                return null;

            return attribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not AttributeItem With This Id {attributeItemId}", attributeItemId);
            return null;
        }
    }
}