namespace POS.Core.Specifications.AttributeSpecs;

public class AttributeWithIncludeSpecs :BaseSpecifications<Attributes>
{
    
    public AttributeWithIncludeSpecs()
    {
        AddIncludes();
    }

    public AttributeWithIncludeSpecs(AttributeSpecs specs) :base
        (a => specs.attId == null || a.Id == specs.attId )
    {
        AddIncludes();
    }
    private void AddIncludes()
    {
        Includes.Add(a=>a.AttributeItems);
    }


    
}
