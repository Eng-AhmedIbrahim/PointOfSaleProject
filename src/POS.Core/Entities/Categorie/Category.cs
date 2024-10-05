using POS.Core.Entities.Item;

namespace POS.Core.Entities.Categorie;

public class Category : BaseEntity
{
    public string? Name { get; set; } 
    public string? NomalizedName { get; set; }
    public string? EnglishName { get; set; } 
    public string? NomalizedEnglishName { get; set; } 
    public string? ItemsFont { get; set; }
    public bool Invisible { get; set; } = false;
    
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    public DateTime CreationDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    public ICollection<MenuSalesItems> MenuSalesItems { get; set; } = [];
}