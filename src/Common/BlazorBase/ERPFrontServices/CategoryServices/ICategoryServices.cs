using POS.Contract;

namespace BlazorBase.ERPFrontServices.CategoryServices;

public interface ICategoryServices
{
    public Task<ICollection<CategoryToReturnDto>> GetAllCategoriesAsync();

    public Task<ICollection<MenuSalesItemsToReturnDto>> GetItemsByCategoryIdAsync(int categoryId);
    Task<ServiceResponse<IReadOnlyList<CategoryToReturnDto>>> GetAllCategories();
    Task<ServiceResponse<CategoryToReturnDto>> GetCategoryById(int categoryId);
    public Task<ServiceResponse<CategoryToReturnDto>> CreateCategory(CreateCategoryDto newCategory);
    Task<ServiceResponse<CategoryToReturnDto>> UpdateCategory(UpdatedCategoryDto updatedCategory);
    Task<ServiceResponse<bool>> DeleteCategory(int categoryId);
}