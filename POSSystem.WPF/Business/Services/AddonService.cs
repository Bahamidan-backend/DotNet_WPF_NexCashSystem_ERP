
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services
{
    public class AddonService : IAddonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICategoryService _categoryService;

        public AddonService(IUnitOfWork unitOfWork, ICategoryService categoryService)
        {
            _unitOfWork = unitOfWork;
            _categoryService = categoryService;

        }

        public async Task<IEnumerable<Addon>> GetAllAddonsAsync()
        {
            return await _unitOfWork.Addons.GetAllAsync();
        }

        public async Task<IEnumerable<Addon>> GetActiveAddonsAsync()
        {
            return await _unitOfWork.Addons.GetActiveAddonsAsync();
        }

        public async Task<IEnumerable<Addon>> GetAddonsByCategoryAsync(int categoryId)
        {
            return await _unitOfWork.Addons.GetAddonsByCategoryAsync(categoryId);
        }

        public async Task<IEnumerable<Addon>> GetLowStockAddonsAsync()
        {
            return await _unitOfWork.Addons.GetLowStockAddonsAsync();
        }

        public async Task<Addon?> GetAddonByIdAsync(int id)
        {
            return await _unitOfWork.Addons.GetByIdAsync(id);
        }
        public async Task<List<Addon>?> GetAddonByIdsAsync(List<int> id)
        {
            return await _unitOfWork.Addons.GetByIdsAsync(id);
        }
        public async Task<Addon?> GetAddonByNameAsync(string name)
        {
            return await _unitOfWork.Addons.GetAddonByNameAsync(name);
        }

        public async Task<Addon> CreateAddonAsync(Addon addon)
        {
            if (!await ValidateAddonDataAsync(addon))
                throw new ArgumentException("Invalid addon data");

            addon.Name = addon.Name.Trim();

            // Check if addon name already exists
            if (await _unitOfWork.Addons.AddonNameExistsAsync(addon.Name))
                throw new InvalidOperationException("Addon with this name already exists");

            // Handle Manual Stock Propagation for NEW addon
            if (addon.BaseAddonId.HasValue && addon.BaseAddonQuantity > 0)
            {
                var baseAddon = await _unitOfWork.Addons.GetByIdAsync(addon.BaseAddonId.Value);
                if (baseAddon != null)
                {
                    decimal deduction = addon.DeductOnSale 
                        ? 0 
                        : addon.BaseAddonQuantity;

                    if (deduction > 0)
                    {
                        if (baseAddon.Quantity < deduction)
                        {
                            throw new InvalidOperationException($"كمية غير كافية من الإضافة الأساسية ({baseAddon.Name}) لإنشاء {addon.Name}");
                        }
                        baseAddon.Quantity -= deduction;
                        _unitOfWork.Addons.Update(baseAddon);
                    }
                }
            }

            addon.CreatedAt = DateTime.Now;
            addon.UpdatedAt = DateTime.Now;

            await _unitOfWork.Addons.AddAsync(addon);
            await _unitOfWork.SaveChangesAsync();
            ServiceLocator.NotifyAddonsChanged();

            return addon;
        }

        public async Task<Addon> UpdateAddonAsync(int id, Addon addon)
        {
            if (!await ValidateAddonDataAsync(addon))
                throw new ArgumentException("Invalid addon data");

            // Clear any existing tracking to avoid conflicts with entities loaded in the UI
            _unitOfWork.ClearEntityTracking();

            var existingAddon = await _unitOfWork.Addons.GetByIdAsync(id);
            if (existingAddon == null)
                throw new ArgumentException("Addon not found");

            addon.Name = addon.Name.Trim();

            // Check if addon name already exists (excluding current addon)
            if (await _unitOfWork.Addons.AddonNameExistsAsync(addon.Name, id))
                throw new InvalidOperationException("Addon with this name already exists");

            // Handle Manual Stock Propagation (if quantity increased and it has a base addon)
            if (addon.BaseAddonId.HasValue && addon.BaseAddonQuantity > 0)
            {
                decimal quantityDifference = addon.Quantity - existingAddon.Quantity;
                if (quantityDifference > 0)
                {
                    var baseAddon = await _unitOfWork.Addons.GetByIdAsync(addon.BaseAddonId.Value);
                    if (baseAddon != null)
                    {
                        decimal deduction = addon.DeductOnSale 
                            ? 0 
                            : addon.BaseAddonQuantity * quantityDifference;

                        if (deduction > 0)
                        {
                            if (baseAddon.Quantity < deduction)
                            {
                                throw new InvalidOperationException($"كمية غير كافية من الإضافة الأساسية ({baseAddon.Name}) لزيادة كمية {addon.Name}");
                            }
                            baseAddon.Quantity -= deduction;
                            _unitOfWork.Addons.Update(baseAddon);
                        }
                    }
                }
            }

            // Update core properties
            existingAddon.Name = addon.Name;
            existingAddon.Description = addon.Description;
            existingAddon.Price = addon.Price;
            existingAddon.Quantity = addon.Quantity;
            existingAddon.CategoryId = addon.CategoryId;
            existingAddon.IsActive = addon.IsActive;
            existingAddon.IsInHouse = addon.IsInHouse;
            existingAddon.BaseAddonId = addon.BaseAddonId;
            existingAddon.BaseAddonQuantity = addon.BaseAddonQuantity;
            existingAddon.DeductOnSale = addon.DeductOnSale;
            existingAddon.BuyingPrice = addon.BuyingPrice;
            existingAddon.UpdatedAt = DateTime.Now;
            
            _unitOfWork.Addons.Update(existingAddon);
            await _unitOfWork.SaveChangesAsync();
            ServiceLocator.NotifyAddonsChanged();

            return existingAddon;
        }

        public async Task DeleteAddonAsync(int addonId)
        {
            var context = _unitOfWork.GetContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await context.PurchaseItems
                    .Where(pi => pi.AddonId == addonId)
                    .ExecuteUpdateAsync(s => s.SetProperty(pi => pi.AddonId, (int?)null));

                int deleted = await context.Addons
                    .Where(a => a.Id == addonId)
                    .ExecuteDeleteAsync();

                if (deleted == 0)
                    throw new ArgumentException("الإضافة غير موجودة");

                await transaction.CommitAsync();
                _unitOfWork.ClearEntityTracking();
                ServiceLocator.NotifyAddonsChanged();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAddonQuantityAsync(int addonId, decimal newQuantity)
        {
            await _unitOfWork.Addons.UpdateAddonQuantityAsync(addonId, newQuantity);
            ServiceLocator.NotifyAddonsChanged();
        }

        public async Task<bool> AddonNameExistsAsync(string name, int? excludeId = null)
        {
            return await _unitOfWork.Addons.AddonNameExistsAsync(name, excludeId);
        }


        public async Task FilterAddons(ObservableCollection<Addon> Addons, Category? SelectedCategory, bool ShowLowStockOnly)
        {
            var addonsAll = await _unitOfWork.Addons.GetAllAsync();
            var filteredAddons = addonsAll.AsEnumerable();

            // Filter by category
            if (SelectedCategory != null)
            {
                filteredAddons = filteredAddons.Where(a => a.CategoryId == SelectedCategory.Id);
            }

            // Filter by low stock
            if (ShowLowStockOnly)
            {
                filteredAddons = filteredAddons.Where(a => a.IsLowStock);
            }

            // Update the collection
            Addons.Clear();
            foreach (var addon in filteredAddons.OrderBy(a => a.Category?.Name ?? "").ThenBy(a => a.Name))
            {
                Addons.Add(addon);
            }
        }









        public Task<bool> ValidateAddonDataAsync(Addon addon)
        {
            if (addon == null) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(addon.Name)) return Task.FromResult(false);
            if (addon.Price < 0) return Task.FromResult(false);
            if (addon.Quantity < 0) return Task.FromResult(false);
            // CategoryId is now optional for some addons
            // if (addon.CategoryId <= 0) return Task.FromResult(false);

            return Task.FromResult(true);
        }








        public async Task RefreshCategories(ObservableCollection<Category> Categories)
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();

            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }
     




        public async Task<IEnumerable<Addon>> GetAddonsForOrderAsync(OrderItem orderItem)
        {
            if (orderItem == null || orderItem.Product == null)
                return Enumerable.Empty<Addon>();

            var categoryId = orderItem.Product.CategoryId;

            // ����� ���
            if (orderItem.Addons != null && orderItem.Addons.Any())
            {
                return orderItem.Addons
                    .Where(a => a.CategoryId == categoryId)
                    .Select(a => CloneAddon(a, a.IsActive));
            }

            // ��� ����
            var activeAddons = await GetActiveAddonsAsync();

            return activeAddons
                .Where(a => a.CategoryId == categoryId)
                .Select(a => CloneAddon(a, false));
        }

        private static Addon CloneAddon(Addon source, bool isActive)
        {
            return new Addon
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                CategoryId = source.CategoryId,
                Price = source.Price,
                Quantity = source.Quantity,
                TotalAmount = source.TotalAmount,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt,
                IsActive = isActive,
                IsInHouse = source.IsInHouse,
                BaseAddonId = source.BaseAddonId,
                BaseAddonQuantity = source.BaseAddonQuantity,
                BuyingPrice = source.BuyingPrice
            };
        }

        public async Task<POSSystem.WPF.Models.AddonStatistics> GetAddonStatisticsAsync()
        {
            var context = _unitOfWork.GetContext();

            var addonAgg = await context.Addons
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    TotalStock = g.Sum(a => (double)a.Quantity)
                })
                .FirstOrDefaultAsync();

            var totalPurchase = await context.PurchaseItems
                .AsNoTracking()
                .Where(p => p.AddonId != null)
                .SumAsync(p => (double?)p.TotalPrice) ?? 0.0;

            var salesAgg = await context.OrderItems
                .AsNoTracking()
                .Where(o => o.AddonId != null && o.Order.Status != OrderStatus.Cancelled)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    SoldCount = g.Sum(o => (int?)o.Quantity) ?? 0,
                    SoldValue = g.Sum(o => (double?)o.TotalPrice) ?? 0.0
                })
                .FirstOrDefaultAsync();

            return new POSSystem.WPF.Models.AddonStatistics
            {
                TotalAddonTypes = addonAgg?.Count ?? 0,
                TotalAddonStock = (decimal)(addonAgg?.TotalStock ?? 0.0),
                TotalPurchaseValue = (decimal)totalPurchase,
                SoldAddonsCount = salesAgg?.SoldCount ?? 0,
                TotalSoldValue = (decimal)(salesAgg?.SoldValue ?? 0.0)
            };
        }

        public async Task<int> RefillAllOutOfStockAsync()
        {
            var addonsList = (await _unitOfWork.Addons.GetActiveAddonsAsync()).ToList();
            var outOfStockAddons = addonsList.Where(a => a.Quantity <= 0 && a.BaseAddonId.HasValue && a.BaseAddonQuantity > 0).ToList();

            if (outOfStockAddons.Count == 0) return 0;

            var addonsById = addonsList.ToDictionary(a => a.Id);
            int refilledCount = 0;
            var now = DateTime.Now;

            foreach (var addon in outOfStockAddons)
            {
                if (addonsById.TryGetValue(addon.BaseAddonId!.Value, out var baseAddon) && baseAddon.Quantity >= addon.BaseAddonQuantity)
                {
                    decimal possibleUnits = Math.Floor(baseAddon.Quantity / addon.BaseAddonQuantity);
                    if (possibleUnits > 0)
                    {
                        baseAddon.Quantity -= (addon.BaseAddonQuantity * possibleUnits);
                        _unitOfWork.Addons.Update(baseAddon);

                        addon.Quantity += possibleUnits;
                        addon.UpdatedAt = now;
                        _unitOfWork.Addons.Update(addon);

                        refilledCount++;
                    }
                }
            }

            if (refilledCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return refilledCount;
        }
    }
}
