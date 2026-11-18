using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Products;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class ProductsView : UserControl, POSSystem.WPF.Services.ISearchable
    {
        private ProductsViewModel _viewModel;
        private string? _pendingSearchQuery;

        public void ApplySearch(string query)
        {
            if (_viewModel != null)
            {
                _viewModel.ApplySearch(query);
            }
            else
            {
                _pendingSearchQuery = query;
            }
        }

        public ProductsView()
        {
            InitializeComponent();
            Loaded += ProductsView_Loaded;

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            DataContext = new object();

            Task.Run(() =>
            {
                try
                {
                    var vm = new ProductsViewModel();
                    // Assign to UI thread when ready
                    Dispatcher.Invoke(() =>
                    {
                        _viewModel = vm;
                        DataContext = _viewModel;
                        if (!string.IsNullOrEmpty(_pendingSearchQuery))
                        {
                            _viewModel.ApplySearch(_pendingSearchQuery);
                            _pendingSearchQuery = null;
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Report the error on the UI thread but do not rethrow
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"تعذر تحميل واجهة المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        // leave DataContext as the placeholder so UI remains usable
                    });
                }
            });
        }


        private void ProductsView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyPermissions();
        }

        public void ApplyPermissions()
        {
            var user = AdminDashboardWindow._currentUser;
            bool canManageCarcasses = POSSystem.WPF.Services.PermissionGuard.HasPermission(user, POSSystem.WPF.Services.SystemPermissions.AnimalBatchesManage);
            if (AnimalBatchesTabItem != null)
            {
                AnimalBatchesTabItem.Visibility = canManageCarcasses ? Visibility.Visible : Visibility.Collapsed;
                if (!canManageCarcasses && MainTabControl != null && MainTabControl.SelectedItem == AnimalBatchesTabItem)
                {
                    MainTabControl.SelectedIndex = 0;
                }
            }

            bool canAddProduct = POSSystem.WPF.Services.PermissionGuard.HasPermission(user, POSSystem.WPF.Services.SystemPermissions.ProductsAdd);
            if (AddProductButton != null)
                AddProductButton.Visibility = canAddProduct ? Visibility.Visible : Visibility.Collapsed;
            if (ManageCategoriesButton != null)
                ManageCategoriesButton.Visibility = (canAddProduct || POSSystem.WPF.Services.PermissionGuard.HasPermission(user, POSSystem.WPF.Services.SystemPermissions.ProductsEdit)) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (!POSSystem.WPF.Services.PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, POSSystem.WPF.Services.SystemPermissions.ProductsAdd, "إضافة منتج جديد")) return;
            try
            {
                if (_viewModel == null) { MessageBox.Show("جاري تحميل البيانات، يرجى الانتظار...", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                var addProductControl = new POSSystem.WPF.Views.Popups.AddProductControl(_viewModel);
                if (Window.GetWindow(this) is AdminDashboardWindow adminWindow)
                {
                    adminWindow.ShowOverlay(addProductControl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إضافة المنتج: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (!POSSystem.WPF.Services.PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, POSSystem.WPF.Services.SystemPermissions.ProductsEdit, "تعديل بيانات منتج")) return;
            if (sender is Button button && button.Tag is Product product)
            {
                try
                {
                    if (product.TotalValue == 0)
                    {
                        product.StockQuantity = 0;

                    }
                    var editProductControl = new POSSystem.WPF.Controls.EditProductControl();
                    await editProductControl.InitializeAsync(product, _viewModel);
                    editProductControl.CloseRequested += (s, args) => HideOverlay();
                    ShowOverlay(editProductControl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في فتح نافذة تعديل المنتج: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (!POSSystem.WPF.Services.PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, POSSystem.WPF.Services.SystemPermissions.ProductsDelete, "حذف منتج")) return;
            if (sender is Button button && button.Tag is Product product)
            {
                var result = MessageBox.Show($"هل أنت متأكد من حذف المنتج '{product.Name}'؟", 
                    "تأكيد الحذف", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        button.IsEnabled = false; // Prevent double clicks
                        bool deleted = await _viewModel.DeleteProduct(product);
                        if (deleted)
                        {
                            MessageBox.Show("تم حذف المنتج بنجاح", "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        button.IsEnabled = true;
                    }
                }
            }
        }

        private System.Windows.Threading.DispatcherTimer _searchTimer;

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is TextBox textBox)
            {
                _viewModel.productsProberty.SearchText = textBox.Text;

                if (_searchTimer == null)
                {
                    _searchTimer = new System.Windows.Threading.DispatcherTimer();
                    _searchTimer.Interval = TimeSpan.FromMilliseconds(300);
                    _searchTimer.Tick += (s, args) =>
                    {
                        _searchTimer.Stop();
                        _viewModel?.FilterProducts();
                    };
                }
                
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }

        private void AllCategories_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel?.SelectAllCategories();
        }

        private void CategoryFilter_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is CheckBox checkBox && checkBox.Tag is string categoryName)
            {
                _viewModel.ToggleCategorySelection(categoryName, true);
            }
        }

        private void CategoryFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is CheckBox checkBox && checkBox.Tag is string categoryName)
            {
                _viewModel.ToggleCategorySelection(categoryName, false);
            }
        }

        private void AllStock_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel?.SelectAllStock();
        }

        private void AllStock_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && !_viewModel.productsProberty.LowStockSelected && !_viewModel.productsProberty.OutOfStockSelected)
            {
                _viewModel.productsProberty.AllStockSelected = true;
            }
        }

        private void StockFilter_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel?.FilterProducts();
        }

        private void StockFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            _viewModel?.FilterProducts();
        }

        private async void RefreshProductsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                var button = sender as Button;
                if (button != null) button.IsEnabled = false;

                try
                {
                    await _viewModel.InitializeAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ أثناء التحديث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (button != null) button.IsEnabled = true;
                }
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ClearAllFilters();
        }

        private void ManageCategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!POSSystem.WPF.Services.PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, POSSystem.WPF.Services.SystemPermissions.ProductsEdit, "إدارة الفئات")) return;
            try
            {
                var manageCategoriesControl = new POSSystem.WPF.Views.Popups.ManageCategoriesControl();
                manageCategoriesControl.Initialize(_viewModel);
                if (Window.GetWindow(this) is AdminDashboardWindow adminWindow)
                {
                    adminWindow.ShowOverlay(manageCategoriesControl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إدارة الفئات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MangerRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (!POSSystem.WPF.Services.PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, POSSystem.WPF.Services.SystemPermissions.RecipesManage, "إدارة الوصفات ومكونات المنتجات")) return;
            try
            {
                var button = sender as System.Windows.Controls.Button;
                if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int SelectId))
                {
                    var recipeManagementControl = new POSSystem.WPF.Controls.RecipeManagementControl();
                    recipeManagementControl.Initialize(_viewModel, SelectId);
                    recipeManagementControl.CloseRequested += (s, args) => HideOverlay();
                    ShowOverlay(recipeManagementControl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إدارة وصفات: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowOverlay(UserControl content)
        {
            var parentWindow = Window.GetWindow(this) as AdminDashboardWindow;
            if (parentWindow != null)
            {
                parentWindow.ShowOverlay(content);
            }
        }

        private void HideOverlay()
        {
            var parentWindow = Window.GetWindow(this) as AdminDashboardWindow;
            if (parentWindow != null)
            {
                parentWindow.CloseOverlay();
            }
        }



        private async void RefillAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("هل أنت متأكد من تعبئة جميع المنتجات المنتهية؟\n\nسيتم إضافة كمية (طبخة واحدة) لكل منتج وخصم المكونات من المستودع.", 
                    "تأكيد التعبئة الشاملة", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _viewModel.RefillAllOutOfStock();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عملية التعبئة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
