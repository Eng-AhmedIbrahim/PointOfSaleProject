namespace POS.Contract.Dtos.ItemDto;

public class MenuSalesItemsDto
{
    public string? Barcode { get; set; }
    [Required]
    public string? ArabicName { get; set; }
    [Required]
    public string? EnglishName { get; set; }
    [Required]
    public decimal? Price { get; set; }
    public decimal? SecondPrice { get; set; }
    public decimal? ThirdPrice { get; set; }
    public decimal? FourthPrice { get; set; }
    public decimal? FifthPrice { get; set; }
    public decimal? AttributePrice { get; set; } = 0;
    public decimal? Tax { get; set; }
    public string? Description { get; set; }
    public IFormFile? Image { get; set; }
    //[Required]
    //public MainCategories? MainCategoryId { get; set; }
    public string? BackColor { get; set; }
    public string? TextColor { get; set; }
    public int? TextSize { get; set; } // px or pt
    public bool Invisible { get; set; } = false;
    public DateTime CreationDate { get; set; }

    public int? BranchId { get; set; } = 1;
    [Required]
    public int? CategoryId { get; set; }
    public bool HasAttribute { get; set; } = false;
    public int? AttributeId { get; set; }

    public MenuSalesItemsDto()
    {
        CreationDate = DateTime.Now;
        CreationDate.ToString("yyyy-MM-dd hh:mm:ss.fff tt zzz");
    }
}