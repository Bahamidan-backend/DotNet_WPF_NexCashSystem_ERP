using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Addons;

namespace POSSystem.WPF.ViewModels.Categories
{
    public partial class ManageCategoriesViewModel : BaseViewModel
    {
        public ManageCategoriesViewModel()
        {
            Categories = new ObservableCollection<Category>();
            _Orderitem = new OrderItem();
            var sp = POSSystem.WPF.App.ServiceProvider;
            _categoryService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICategoryService>(sp);
            _kitchenService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IKitchenService>(sp);

            productsViewModel = null;
            addonManagementViewModel = null;

            Task.Run(async () =>
            {
                try
                {
                    await LoadCategoriesAsync();
                    await LoadKitchensAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
                }
            });
        }

        public async Task LoadKitchensAsync()
        {
            try
            {
                var kitchens = await _kitchenService.GetAllKitchensAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableKitchens.Clear();
                    foreach (var kitchen in kitchens)
                    {
                        AvailableKitchens.Add(kitchen);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading kitchens: {ex.Message}");
            }
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var category in categories)
                    {
                        Categories.Add(category);
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطأ في تحميل الفئات: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public async Task SaveCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                MessageBox.Show("يرجى إدخال اسم الفئة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsEditing)
                {
                    var category = Categories.FirstOrDefault(c => c.Id == _editingCategoryId);
                    if (category != null)
                    {
                        category.Name = NewCategoryName;
                        category.Description = NewCategoryDescription;
                        category.portionSize = NewCategoryportionSize;
                        category.IsSubCategory = NewCategoryIsSub;
                        category.KitchenId = SelectedKitchen?.Id;
                        category.Color = NewCategoryColor;

                        await _categoryService.UpdateCategoryAsync(category);
                        MessageBox.Show("تم تعديل الفئة بنجاح", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    var newCategory = new Category
                    {
                        Name = NewCategoryName,
                        Description = NewCategoryDescription,
                        portionSize = (int)NewCategoryportionSize == 0 ? Category.PortionSize.Single : NewCategoryportionSize,
                        IsSubCategory = NewCategoryIsSub,
                        KitchenId = SelectedKitchen?.Id,
                        Color = NewCategoryColor,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _categoryService.CreateCategoryAsync(newCategory);
                    MessageBox.Show("تم إضافة الفئة بنجاح", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
                await LoadCategoriesAsync();
                productsViewModel?.RefreshCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EditCategory(Category category)
        {
            if (category == null) return;

            NewCategoryName = category.Name;
            NewCategoryDescription = category.Description ?? "";
            NewCategoryportionSize = category.portionSize;
            NewCategoryIsSub = category.IsSubCategory;
            SelectedKitchen = AvailableKitchens.FirstOrDefault(k => k.Id == category.KitchenId);
            NewCategoryColor = category.Color ?? "#3498DB";
            _editingCategoryId = category.Id;
            IsEditing = true;
        }

        public void ClearForm()
        {
            NewCategoryName = "";
            NewCategoryDescription = "";
            NewCategoryIsSub = false;
            NewCategoryportionSize = Category.PortionSize.Single;
            NewCategoryColor = "#3498DB";
            SelectedKitchen = null;
            _editingCategoryId = 0;
            IsEditing = false;
        }

        public async Task DeleteCategoryAsync(Category category)
        {
            try
            {
                await _categoryService.DeleteCategoryAsync(category);
                Categories.Remove(category);
                productsViewModel?.RefreshCategoriesAsync();
                MessageBox.Show("تم حذف الفئة بنجاح", "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف الفئة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
