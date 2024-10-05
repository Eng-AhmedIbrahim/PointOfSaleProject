namespace POS.Core.Entities.Item;

public class Modifiers : BaseEntity
{
    public string? Name { get; set; }
    public string? ArabicName { get; set; }

    public ICollection<ModifierDetails> ModifierDetails { get; set; } = [];
}