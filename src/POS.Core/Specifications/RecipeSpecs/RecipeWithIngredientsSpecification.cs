
namespace POS.Core.Specifications.RecipeSpecs
{
    public class RecipeWithIngredientsSpecification : BaseSpecifications<Recipe>
    {
        public RecipeWithIngredientsSpecification() : base()
        {
            Includes.Add(r => r.Ingredients);
            Includes.Add(r => r.MenuSalesItem!);
        }

        public RecipeWithIngredientsSpecification(int itemId) : base(r => r.MenuSalesItemId == itemId && r.IsActive)
        {
            Includes.Add(r => r.Ingredients);
            IncludeStrings.Add("Ingredients.MenuSalesIngredient");
            Includes.Add(r => r.MenuSalesItem!);
        }
    }

}
