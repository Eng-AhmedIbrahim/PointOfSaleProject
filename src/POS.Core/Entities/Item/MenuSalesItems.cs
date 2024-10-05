namespace POS.Core.Entities.Item;

public class MenuSalesItems : BaseEntity
{
    public string? ItemCode { get; set; } 
    public string? Barcode { get; set; }
    public string? MenuItemName { get; set; }
    public string? NormalizedMenuItemName { get; set; }
    public string? EnglishItemName { get; set; }
    public string? NormalizedEnglishItemName { get; set; }
    public decimal Price { get; set; }
    public decimal SecondPrice { get; set; }
    public decimal ThirdPrice { get; set; }
    public decimal FourthPrice { get; set; }
    public decimal FifthPrice { get; set; }
    public decimal Tax { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public MainCategories MainCategoryId { get; set; }
    public string? BackColor { get; set; }
    public string? TextColor { get; set; }
    public bool Invisible { get; set; } = false;
    public DateTime CreationDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? ModifierId { get; set; }
    public Modifiers? Modifier { get; set; }
}