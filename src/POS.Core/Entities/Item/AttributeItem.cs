namespace POS.Core.Entities.Item;

public class AttributeItem : BaseEntity
{
    public int AppearanceIndex { get; set; }  // Determines the order in which items appear

    public int AttributeId { get; set; }
    public Attribute? Attribute { get; set; }

    public int RelatedMenuItemId { get; set; }  // This links to the related item // 1 , 2 , 20 , 23
    public MenuSalesItems? RelatedMenuItem { get; set; }
}