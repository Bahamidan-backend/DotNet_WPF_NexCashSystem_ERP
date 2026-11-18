using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Diagnostics;
using static POSSystem.WPF.Models.Recipe;
using POSSystem.WPF.Helpers;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace POSSystem.WPF.Controls
{
    public partial class RecipeManagementControl : UserControl
    {
        private object? _viewModel;
        private CategoryService _categoryService;
        private RecipesServices _recipesServices;
        private ProductService  _productService;
        private WarehouseService  _warehouseService;
        private UnitOfWork? _ownedUnitOfWork;
        private Recipe? _editingRecipe;
        private bool _isEditing = false;
        private IServiceScope _scope;

        public ObservableCollection<Recipe> Recipes { get; set; }
        public ObservableCollection<WarehouseFromProduct> warehouseFromProducts;
        public ObservableCollection<Warehouse> Warehouses { get; set; }
        public string QuantityRecipe { get; set; } = "0";
        public int? PersonsNumberFood { get; set; }
        public PortionUnit NewRecipesportionUnit { get; set; }
        public string NewRecipesNameProduct{ get; set; }
        public bool IsMainIngredientRecipe { get; set; } = false;
        public string YieldRateRecipe { get; set; } = "0";
        public int Id;
        
        // Event to signal close/cancel to the parent
        public event EventHandler CloseRequested;


        public RecipeManagementControl()
        {
            InitializeComponent();
            Unloaded += (_, _) => 
            {
                _ownedUnitOfWork?.Dispose();
                _scope?.Dispose();
            };
        }

        public void Initialize(object viewModel, int id)
        {
             Id = id;
            _viewModel = viewModel;

            // Initialize CategoryService with UnitOfWork
             // Assuming ServiceLocator.UnitOfWork is available globally as in previous refactors
            if (ServiceLocator.UnitOfWork != null)
            {
                var unitOfWork = (POSSystem.WPF.Data.UnitOfWork.UnitOfWork)ServiceLocator.UnitOfWork; 
                _categoryService = new CategoryService(unitOfWork);
                _productService=new ProductService(unitOfWork);
                _recipesServices = new RecipesServices(unitOfWork);
                _warehouseService=new WarehouseService(unitOfWork);
            }
            else
            {
                 // Create scope for dependencies
                 _scope = App.ServiceProvider.CreateScope();
                 var unitOfWork = _scope.ServiceProvider.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>();
                 _ownedUnitOfWork = (UnitOfWork)unitOfWork;
                 _categoryService = new CategoryService(_ownedUnitOfWork);
                 _productService=new ProductService(_ownedUnitOfWork);
                 _recipesServices = new RecipesServices(_ownedUnitOfWork);
                 _warehouseService=new WarehouseService(_ownedUnitOfWork);
            }
            
            Recipes = new ObservableCollection<Recipe>();
            warehouseFromProducts= new ObservableCollection<WarehouseFromProduct>();
            Warehouses = new ObservableCollection<Warehouse>();

            DataContext = this;
            LoadwarehouseFromProductsAsync();
            _ = LoadAvailableUnit();
            LoadRecipes();
            LoadWarehousesAsync();
            
            // By default, no warehouse is selected, so products list should be empty
            RecipeNameCobobox.ItemsSource = null;
        }

        private async void LoadWarehousesAsync()
        {
            try
            {
                Warehouses.Clear();
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                foreach (var warehouse in warehouses)
                {
                    Warehouses.Add(warehouse);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المستودعات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WarehouseComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (WarehouseComboBox.SelectedValue is int warehouseId)
            {
                try
                {
                    var productWarehose = _warehouseService.GetWarehouseFromProductAll()
                                          .Where(p => p.WarehouseId == warehouseId)
                                          .ToList();
                    RecipeNameCobobox.ItemsSource = productWarehose;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تصفية المنتجات: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                RecipeNameCobobox.ItemsSource = null; // Clear products if no warehouse is selected
            }
        }

        public async void LoadAvailablNameProductWarehose()
        {
            try
            {
                var productWarehose = _warehouseService.GetWarehouseFromProductAll();
                RecipeNameCobobox.ItemsSource = productWarehose;
                RecipeNameCobobox.SelectedValuePath = "Name";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفئات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadwarehouseFromProductsAsync()
        {
            try
            {
                warehouseFromProducts.Clear();
                var productWarehose = _warehouseService.GetWarehouseFromProductAll();
                foreach (var warehouseFromProduct in productWarehose)
                {
                     warehouseFromProducts.Add(warehouseFromProduct);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل وصفات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadAvailableUnit()
        {
            try
            {
                RecipeUnitComboBox.ItemsSource = PortionUnitHelper.DisplayNames;
                RecipeUnitComboBox.DisplayMemberPath = "Value"; // النص
                RecipeUnitComboBox.SelectedValuePath = "Key";   // القيمة (enum)
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفئات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadRecipes()
        {
            await LoadRecipesAsync();
        }
        private async Task LoadRecipesAsync()
        {
            try
            {
                Recipes.Clear();
                var FindProducts = await _productService.GetProductByIdAsync(Id);

                var Recipeses = await _recipesServices.GetAllRecipes();
                foreach (var Recipe in Recipeses)
                {
                    if (FindProducts.Id == Recipe.ProductId)
                    {
                        Recipes.Add(Recipe);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفئات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
             CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        
        private void ClearForm()
        {
            QuantityRecipe = "0";
            YieldRateRecipe = "0";
            NewRecipesNameProduct = string.Empty;
            RecipeNameCobobox.SelectedIndex = -1;
            RecipeNameCobobox.SelectedItem = null;
            RecipeNameCobobox.SelectedValue = null;
            RecipeNameCobobox.Text = "";
            RecipeQuntityTextBox.Text = "";
            if (YieldRateTextBox != null) YieldRateTextBox.Text = "";
            WarehouseComboBox.SelectedIndex = -1;
            IsMainIngredientRecipe = false;
            if (IsMainCheckBox != null) IsMainCheckBox.IsChecked = false;
            if (FormTitleTextBlock != null) FormTitleTextBlock.Text = "✨ إضافة وصفة جديدة";
            
             // Force UI Update - simple way for now since properties aren't full INotifyPropertyChanged in View 
             RecipeQuntityTextBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
        }

        private void CancelEdit()
        {
            _isEditing = false;
            _editingRecipe = null;
            ClearForm();
            AddUpdateButton.Content = "➕ إضافة وصفة";
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private void RecipeNameCobobox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (RecipeNameCobobox.SelectedItem is WarehouseFromProduct selectedProduct)
            {
                NewRecipesNameProduct = selectedProduct.Name;
                if (!_isEditing && selectedProduct.PortionUnit.HasValue)
                {
                    RecipeUnitComboBox.SelectedValue = selectedProduct.PortionUnit.Value;
                    RecipeUnitComboBox.Text = PortionUnitHelper.GetDisplayName(selectedProduct.PortionUnit.Value);
                }
            }
        }

        private async void AddUpdateCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string normalizedQuantity = (QuantityRecipe ?? "0")
                    .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                    .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
                    .Replace('٫', '.').Replace(',', '.').Trim();
                if (!decimal.TryParse(normalizedQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedQuantity))
                {
                    MessageBox.Show("يرجى إدخال كمية صحيحة (مثال: 0.5)", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewRecipesNameProduct))
                {
                    MessageBox.Show("يرجى إدخال اسم وصفة", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int parsedYieldRate = 0;
                if (IsMainCheckBox.IsChecked == true)
                {
                    string normYield = (YieldRateRecipe ?? "0")
                        .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                        .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9').Trim();
                    if (!int.TryParse(normYield, out parsedYieldRate) || parsedYieldRate <= 0)
                    {
                        MessageBox.Show("يرجى إدخال معدل أنفار (إنتاجية) صحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                if (_isEditing && _editingRecipe != null)
                {
                    var selectedProduct = (RecipeNameCobobox.SelectedItem as WarehouseFromProduct)?.Name 
                        ?? RecipeNameCobobox.SelectedValue as string 
                        ?? NewRecipesNameProduct;
                    if (string.IsNullOrWhiteSpace(selectedProduct))
                    {
                        MessageBox.Show("يرجى اختيار المنتج من القائمة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Prefer the exact WarehouseFromProduct selected by the user in the ComboBox
                    WarehouseFromProduct? callFromCategry = RecipeNameCobobox.SelectedItem as WarehouseFromProduct;

                    if (callFromCategry == null)
                    {
                        int? selectedWarehouseId = WarehouseComboBox.SelectedValue as int?;
                        var allWhProducts = _warehouseService.GetWarehouseFromProductAll().ToList();

                        if (selectedWarehouseId.HasValue && selectedWarehouseId.Value > 0)
                        {
                            callFromCategry = allWhProducts.FirstOrDefault(x => 
                                x.WarehouseId == selectedWarehouseId.Value && 
                                x.Name != null && x.Name.Trim().Equals(selectedProduct.Trim(), StringComparison.OrdinalIgnoreCase));
                        }

                        if (callFromCategry == null)
                        {
                            callFromCategry = allWhProducts.FirstOrDefault(x => 
                                x.Name != null && x.Name.Trim().Equals(selectedProduct.Trim(), StringComparison.OrdinalIgnoreCase));
                        }
                    }
                    
                    if (callFromCategry == null)
                    {
                        MessageBox.Show($"لم يتم العثور على المنتج المختار ({selectedProduct}) في المستودع.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    NewRecipesNameProduct = callFromCategry.Name;

                    // Update existing recipe BOM standard definition (No warehouse stock deduction here)
                    _editingRecipe.Name            = NewRecipesNameProduct.Trim();
                    _editingRecipe.WarehouseFromProductId = callFromCategry.Id;
                    _editingRecipe.Quantity        = parsedQuantity;
                    _editingRecipe.portionUnit     = NewRecipesportionUnit;
                    _editingRecipe.IsMainIngredient = IsMainCheckBox.IsChecked ?? false;

                    await _recipesServices.UpdateRecipeAsync(_editingRecipe);
                    
                    if (_editingRecipe.IsMainIngredient && parsedYieldRate > 0)
                    {
                        await Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync(
                            ServiceLocator.UnitOfWork.GetContext().Products.Where(p => p.Id == Id),
                            s => s.SetProperty(p => p.YieldRate, parsedYieldRate).SetProperty(p => p.UpdatedAt, DateTime.Now));
                    }

                    MessageBox.Show("تم تحديث وصفة بنجاح", "تم التحديث",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    CancelEdit();
                }
                else
                {
                    var selectedUnit = (PortionUnit)RecipeUnitComboBox.SelectedValue;
                    var selectedProduct = (RecipeNameCobobox.SelectedItem as WarehouseFromProduct)?.Name 
                        ?? RecipeNameCobobox.SelectedValue as string 
                        ?? NewRecipesNameProduct;
                    
                    if (string.IsNullOrWhiteSpace(selectedProduct))
                    {
                        MessageBox.Show("يرجى اختيار المنتج من القائمة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Prefer the exact WarehouseFromProduct selected by the user in the ComboBox
                    WarehouseFromProduct? callFromCategry = RecipeNameCobobox.SelectedItem as WarehouseFromProduct;

                    if (callFromCategry == null)
                    {
                        int? selectedWarehouseId = WarehouseComboBox.SelectedValue as int?;
                        var allWhProducts = _warehouseService.GetWarehouseFromProductAll().ToList();

                        if (selectedWarehouseId.HasValue && selectedWarehouseId.Value > 0)
                        {
                            callFromCategry = allWhProducts.FirstOrDefault(x => 
                                x.WarehouseId == selectedWarehouseId.Value && 
                                x.Name != null && x.Name.Trim().Equals(selectedProduct.Trim(), StringComparison.OrdinalIgnoreCase));
                        }

                        if (callFromCategry == null)
                        {
                            callFromCategry = allWhProducts.FirstOrDefault(x => 
                                x.Name != null && x.Name.Trim().Equals(selectedProduct.Trim(), StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    if (callFromCategry == null)
                    {
                        MessageBox.Show($"لم يتم العثور على المنتج المختار ({selectedProduct}) في المستودع.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newRecipe = new Recipe
                    {
                        Name = callFromCategry.Name,
                        WarehouseFromProductId = callFromCategry.Id,
                        Quantity = parsedQuantity,
                        portionUnit = selectedUnit,
                        ProductId = Id,
                        IsMainIngredient = IsMainCheckBox.IsChecked ?? false,
                        Product = await _productService.GetProductByIdAsync(Id)
                    };

                    var CreatedRecipes = await _recipesServices.CreateRecipeAsync(newRecipe);
                    Recipes.Add(CreatedRecipes);

                    if (newRecipe.IsMainIngredient && parsedYieldRate > 0)
                    {
                        await Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync(
                            ServiceLocator.UnitOfWork.GetContext().Products.Where(p => p.Id == Id),
                            s => s.SetProperty(p => p.YieldRate, parsedYieldRate).SetProperty(p => p.UpdatedAt, DateTime.Now));
                    }

                    MessageBox.Show("تم إضافة وصفة بنجاح", "تم الإضافة",
                     MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
               await LoadRecipesAsync();
               NotifyProductsUpdated();
             
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ وصفة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Recipe Recipe)
            {
                _editingRecipe = Recipe;
                _isEditing = true;

                if (FormTitleTextBlock != null) FormTitleTextBlock.Text = "✏️ تعديل وصفة";

                NewRecipesNameProduct = Recipe.Name;
                QuantityRecipe = Recipe.Quantity.ToString("0.##", CultureInfo.InvariantCulture);
                NewRecipesportionUnit = Recipe.portionUnit;
                IsMainIngredientRecipe = Recipe.IsMainIngredient;
                if (IsMainCheckBox != null) IsMainCheckBox.IsChecked = IsMainIngredientRecipe;

                // Select corresponding warehouse if available (by Id first, then Name)
                var allWarehouseProducts = _warehouseService.GetWarehouseFromProductAll().ToList();
                var warehouseProduct = (Recipe.WarehouseFromProductId.HasValue
                    ? allWarehouseProducts.FirstOrDefault(p => p.Id == Recipe.WarehouseFromProductId.Value)
                    : null)
                    ?? allWarehouseProducts.FirstOrDefault(p => p.Name != null && p.Name.Trim().Equals(Recipe.Name.Trim(), StringComparison.OrdinalIgnoreCase));

                if (warehouseProduct != null && warehouseProduct.WarehouseId != null)
                {
                    WarehouseComboBox.SelectedValue = warehouseProduct.WarehouseId;
                }

                // Ensure RecipeNameCobobox items are populated
                if (RecipeNameCobobox.ItemsSource == null)
                {
                    if (warehouseProduct != null && warehouseProduct.WarehouseId != null)
                    {
                        RecipeNameCobobox.ItemsSource = allWarehouseProducts.Where(p => p.WarehouseId == warehouseProduct.WarehouseId).ToList();
                    }
                    else
                    {
                        RecipeNameCobobox.ItemsSource = allWarehouseProducts;
                    }
                }

                // Explicitly select the product item directly so SelectionBoxItem renders correctly
                var currentItems = RecipeNameCobobox.ItemsSource as IEnumerable<WarehouseFromProduct>;
                var matchedItem = currentItems?.FirstOrDefault(p =>
                    (warehouseProduct != null && p.Id == warehouseProduct.Id) ||
                    (p.Name != null && p.Name.Trim().Equals(Recipe.Name.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (matchedItem != null)
                {
                    RecipeNameCobobox.SelectedItem = matchedItem;
                    RecipeNameCobobox.SelectedValue = matchedItem.Name;
                    NewRecipesNameProduct = matchedItem.Name;
                }
                else
                {
                    RecipeNameCobobox.SelectedValue = Recipe.Name;
                }

                if (IsMainIngredientRecipe)
                {
                    var product = await _productService.GetProductByIdAsync(Id);
                    if (product != null)
                    {
                        YieldRateRecipe = product.YieldRate.ToString();
                        if (YieldRateTextBox != null) YieldRateTextBox.Text = YieldRateRecipe;
                    }
                }

                RecipeQuntityTextBox.Text = QuantityRecipe;
                RecipeUnitComboBox.SelectedValue = NewRecipesportionUnit;
                RecipeUnitComboBox.Text = PortionUnitHelper.GetDisplayName(NewRecipesportionUnit);
                AddUpdateButton.Content = "💾 تحديث وصفة";
                CancelButton.Visibility = Visibility.Visible;
            }
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            CancelEdit();
        }
        private async void DeleteRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Recipe Recipe)
            {
                var result = MessageBox.Show($"هل أنت متأكد من حذف وصفة '{Recipe.Name}'؟\n\nسيتم حذف جميع المنتجات المرتبطة بهذه وصفة أيضاً.",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _recipesServices.DeleteRecipeAsync(Recipe.Id);
                        Recipes.Remove(Recipe);

                        MessageBox.Show("تم حذف وصفة بنجاح", "تم الحذف",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        NotifyProductsUpdated();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في حذف وصفة: {ex.Message}", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private static void NotifyProductsUpdated()
        {
            ServiceLocator.NotifyInventoryChanged();
            ServiceLocator.NotifyProductsChanged();
        }

    }
}
