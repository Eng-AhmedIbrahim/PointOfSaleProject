namespace ERPFront.ERPFrontServices.CategoryServices;

public interface ICategoryServices
{
    Task<ICollection<CategoryToReturnDto>> GetAllCategoriesAsync();

    Task<ICollection<MenuSalesItemsToReturnDto>> GetItemsByCategoryIdAsync(int categoryId);

}