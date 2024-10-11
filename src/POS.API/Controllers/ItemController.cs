using POS.API.Tests;

namespace POS.API.Controllers;

public class ItemController : BaseApiController
{
    private readonly IAttributeService _attributeService;
    private readonly IMenuSalesItemService _itemService;
    private readonly IMapper _mapper;

    public ItemController(IAttributeService attributeService,IMenuSalesItemService menuSalesItemService, IMapper mapper)
    {
        _attributeService = attributeService;
        _itemService = menuSalesItemService;
        _mapper = mapper;
    }

    [ProducesResponseType(typeof(MenuSalesItemsToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> CreateMenuItemAsync([FromQuery] MenuSalesItemsDto itemDto)
    {
        string logoPath = "";
        if (itemDto is null)
            return BadRequest(new ApiResponse(400));

        var mappedItem = _mapper.Map<MenuSalesItemsDto, MenuSalesItems>(itemDto);

        if (mappedItem is null)
            return BadRequest(new ApiResponse(400));

        if (itemDto.Image is not null)
        {
            logoPath = DocumentSetting.UploadFile(itemDto.Image, "Imgs");
            mappedItem.ImagePath = logoPath;
        }

        var item = await _itemService.CreateItemAsync(mappedItem);

        if (item is null)
            return BadRequest(new ApiResponse(400));

        var itemToReturn = _mapper.Map<MenuSalesItems, MenuSalesItemsToReturnDto>(item);

        return Ok(itemToReturn);
    }

    [ProducesResponseType(typeof(IReadOnlyList<MenuSalesItemsToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("GetAllItems")]
    public async Task<IActionResult?> GetAllItems()
    {
        var items = await _itemService.GetAllItemsAsync();
        if (items is null)
            return NotFound(new ApiResponse(404));

        var mappedItems = _mapper.Map<IReadOnlyList<MenuSalesItems>, IReadOnlyList<MenuSalesItemsToReturnDto>>(items);

        return Ok(mappedItems);
    }

    [ProducesResponseType(typeof(IReadOnlyList<MenuSalesItemsToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("{itemId}")]
    public async Task<IActionResult?> GetItemById([FromRoute] int itemId)
    {
        var item = await _itemService.GetItemByIdAsync(itemId);

        if (item is null)
            return NotFound(new ApiResponse(404));

        var mappedItem = _mapper.Map<MenuSalesItems, MenuSalesItemsToReturnDto>(item);

        return Ok(mappedItem);
    }

    [ProducesResponseType(typeof(MenuSalesItemsToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPut]
    public async Task<IActionResult?> UpdateItem(UpdatedItemDto newItem)
    {
        string logoPath = "";
        var oldItem = await _itemService.GetItemByIdAsync(newItem.ItemId);
        if (oldItem is null)
            return NotFound(new ApiResponse(404));

        var mappedNewItem = _mapper.Map<UpdatedItemDto, MenuSalesItems>(newItem);
        if (newItem.Image is not null)
        {
            DocumentSetting.DeleteFile(oldItem?.ImagePath);
            logoPath = DocumentSetting.UploadFile(newItem.Image, "Imgs");
            mappedNewItem.ImagePath = logoPath;
        }


        var item = await _itemService.UpdateItemAsync(oldItem, mappedNewItem);
        if (item is null)
            return null;

        var itemToReturn = _mapper.Map<MenuSalesItems, MenuSalesItemsToReturnDto>(item);

        return Ok(itemToReturn);
    }

    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpDelete("{itemId}")]
    public async Task<IActionResult> DeleteItem([Required]int itemId)
    {
        var item = await _itemService.GetItemByIdAsync(itemId);
        if (item is null)
            return NotFound(new ApiResponse(404));

        var result = await _itemService.DeleteItem(item);
        if (result is true)
        {
            DocumentSetting.DeleteFile(item?.ImagePath??string.Empty);
            return Ok("Deleted Successfully");
        }

        return BadRequest(new ApiResponse(400));
    }


    [ProducesResponseType(typeof(MenuSalesItemsToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPost("AddAttributeToItem")]
    public async Task<IActionResult?> AddAttributeToItem([Required]int attributeId, [Required] int ItemId)
    {
        var item = await _itemService.AddAttributeToItem(attributeId,ItemId);

        if (item is null)
            return null;

        var itemToReturn = _mapper.Map<MenuSalesItems, MenuSalesItemsToReturnDto>(item);
        return Ok(itemToReturn);
    }

}