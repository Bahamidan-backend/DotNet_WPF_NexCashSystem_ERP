

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IRecipesServices
    {
        Task<IEnumerable<Recipe>> GetAllRecipes();

        Task<Recipe> CreateRecipeAsync(Recipe Recipes);
        Task<Recipe> UpdateRecipeAsync(Recipe Recipes);
        Task DeleteRecipeAsync(int RecipeId);

    }
}
