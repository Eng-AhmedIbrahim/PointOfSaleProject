namespace POS.API.Controllers;

public class CategoryController : BaseApiController
{
    private readonly ICategoryService _categoryService;
    private readonly IMapper _mapper;

    public CategoryController(ICategoryService categoryService , IMapper mapper)
    {
        _categoryService = categoryService;
        _mapper = mapper;
    }

    [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> CreateCategoryAsync([FromQuery]CategoryDto categoryDto)
    {
        if (categoryDto is null)
            return BadRequest(new ApiResponse(400));

        var mappedCategory = _mapper.Map<CategoryDto, Category>(categoryDto);

        if (mappedCategory is null)
            return BadRequest(new ApiResponse(400));

        var category = await _categoryService.CreateCategoryAsync(mappedCategory);
        if (category is null)
            return BadRequest(new ApiResponse(400));

        return Ok(category);
    }

    [ProducesResponseType(typeof(IReadOnlyList<Category>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("GetAllCategories")]
    public async Task<IActionResult?> GetAllCategories()
    {
        var categories = await _categoryService.GetCategoriesAsync();
        if (categories is null)
            return NotFound(new ApiResponse(404));
        return Ok(categories);
    }

    [ProducesResponseType(typeof(IReadOnlyList<Category>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("{categoryId}")]
    public async Task<IActionResult?> GetAllCategoryById(int categoryId)
    {
        var category = await _categoryService.GetCategoryByIdAsync(categoryId);
        if (category is null)
            return NotFound(new ApiResponse(404));
        return Ok(category);
    }

    [ProducesResponseType(typeof(IReadOnlyList<Category>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPut]
    public async Task<IActionResult?> UpdateCategory(UpdatedCategoryDto newCategory)
    {
        var oldCategory = await _categoryService.GetCategoryByIdAsync(newCategory.Id);
        if (oldCategory is null)
            return NotFound(new ApiResponse(404));

        var mappedNewCategory = _mapper.Map<UpdatedCategoryDto, Category>(newCategory);

        var category = await _categoryService.UpdateCategory(oldCategory, mappedNewCategory);

        return Ok(category);
    }

    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpDelete]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var category = await _categoryService.GetCategoryByIdAsync(categoryId);
        if (category is null)
            return NotFound(new ApiResponse(404));

        var result = await _categoryService.DeleteCategory(category);
        if (result is true)
            return Ok("Deleted Successfully");

        return BadRequest(new ApiResponse(400));
    }
}