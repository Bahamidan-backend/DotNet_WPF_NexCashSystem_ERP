

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IRecipesRepostries : IRepository<Recipe>
    {

        Task<IEnumerable<Recipe>> GetRecipesAllProducts();


    }
}
