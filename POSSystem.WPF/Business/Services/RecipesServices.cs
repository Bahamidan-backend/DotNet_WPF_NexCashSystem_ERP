

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services
{
    public class RecipesServices : IRecipesServices
    {
        private readonly IUnitOfWork _unitOfWork;
        public RecipesServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Recipe>> GetAllRecipes()
        {
            return await _unitOfWork.Recipes.GetRecipesAllProducts();
        }

        public async Task<Recipe> CreateRecipeAsync(Recipe recipe)
        {
            var tempProduct = recipe.Product;
            recipe.Product = null; // Sever navigation property to prevent graph conflict
            
            await _unitOfWork.Recipes.AddAsync(recipe);
            await _unitOfWork.SaveChangesAsync();
            
            return recipe;
        }

        public async Task<Recipe> UpdateRecipeAsync(Recipe recipe)
        {
            var tempProduct = recipe.Product;
            recipe.Product = null; // Sever navigation property to prevent graph conflict
            
            var context = _unitOfWork.GetContext();
            var trackedEntity = context.ChangeTracker.Entries<Recipe>().FirstOrDefault(e => e.Entity.Id == recipe.Id);
            if (trackedEntity != null)
            {
                trackedEntity.CurrentValues.SetValues(recipe);
            }
            else
            {
                _unitOfWork.Recipes.Update(recipe);
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            return recipe;
        }

        public async Task DeleteRecipeAsync(int recipeId)
        {
            int deleted = await _unitOfWork.GetContext().Recipes
                .Where(r => r.Id == recipeId)
                .ExecuteDeleteAsync();

            if (deleted == 0)
                throw new ArgumentException("Recipe not found");

            _unitOfWork.ClearEntityTracking();
        }
    }
}
