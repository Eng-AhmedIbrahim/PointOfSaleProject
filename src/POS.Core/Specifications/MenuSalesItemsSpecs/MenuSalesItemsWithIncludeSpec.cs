namespace POS.Core.Specifications.MenuSalesItemsSpecs;

public class MenuSalesItemsWithIncludeSpec : BaseSpecifications<MenuSalesItems>
{
    public MenuSalesItemsWithIncludeSpec()
    {
        AddInclude();
        //AddThenInclude();
    }

    public MenuSalesItemsWithIncludeSpec(ItemsSpecs specs)
        : base(S => specs.ItemId == null || specs.ItemId == S.Id)
    {
        AddInclude();
        //AddThenInclude();
    }
    private void AddInclude()
    {
        Includes.Add(s => s.Attribute);
    }

    private void AddThenInclude()
    {
        ThenIncludes.Add("Attribute.AttributeItems");
    }

    
}