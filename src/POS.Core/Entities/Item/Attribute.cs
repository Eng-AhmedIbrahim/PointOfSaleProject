namespace POS.Core.Entities.Item;

public class Attribute : BaseEntity
{
    public string? Name { get; set; }
    public string? ArabicName { get; set; }

    public ICollection<AttributeItem> AttributeItems { get; set; } = new HashSet<AttributeItem>();
}