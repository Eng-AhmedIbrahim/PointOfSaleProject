namespace POS.API.Tests;

public class Attribute
{
    public int Id { get; set; }
    public string Name { get; set; }

    // Removed RelatedItemId/RelatedItem since Attribute belongs to an Item
    public ICollection<AttributeItem> AttributeItems { get; set; } = new HashSet<AttributeItem>();
}
