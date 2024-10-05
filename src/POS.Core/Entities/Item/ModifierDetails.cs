namespace POS.Core.Entities.Item;

public class ModifierDetails : BaseEntity
{
    public int? Index { get; set; } = 1;

    public ICollection<int> SalesItemId { get; set; } = [];
    public ModifierDetails(int? index)
    {
        Index++;
        Index = index;
    }

    public int ModifierId { get; set; }
    public Modifiers? Modifier { get; set; }
}