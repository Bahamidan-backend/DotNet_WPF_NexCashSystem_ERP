using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using System.Threading.Tasks;
using static POSSystem.WPF.Models.Recipe;
using System.Globalization;
using System.Windows;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace POSSystem.WPF.Views.Popups
{
    public partial class MangerRecipePopup : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        private object? _viewModel;
        private CategoryService _categoryService;
        private RecipesServices _recipesServices;
        private ProductService _productService;
        private WarehouseService _warehouseService;
        private readonly UnitOfWork _unitOfWork;
        private Recipe? _editingRecipe;
        private bool _isEditing = false;

        public ObservableCollection<Recipe> Recipes { get; set; }
        public ObservableCollection<WarehouseFromProduct> warehouseFromProducts;
        public string QuantityRecipe { get; set; } = "0";
        public int? PersonsNumberFood { get; set; }
        public PortionUnit NewRecipesportionUnit { get; set; }
        public string NewRecipesNameProduct { get; set; } = string.Empty;
        public bool IsMainIngredient { get; set; } = false;
        public int Id;
        private readonly IServiceScope _scope;

        public MangerRecipePopup(object viewModel, int id)
        {
            InitializeComponent();
            Id = id;
            _viewModel = viewModel;

            _scope = App.ServiceProvider.CreateScope();
            var context = _scope.ServiceProvider.GetRequiredService<POSDbContext>();
            _unitOfWork = new UnitOfWork(context);
            _categoryService = new CategoryService(_unitOfWork);
            _productService = new ProductService(_unitOfWork);
            Recipes = new ObservableCollection<Recipe>();
            warehouseFromProducts = new ObservableCollection<WarehouseFromProduct>();
            _recipesServices = new RecipesServices(_unitOfWork);
            _warehouseService = new WarehouseService(_unitOfWork);
            Closed += (_, _) =>
            {
                _unitOfWork.Dispose();
                _scope.Dispose();
            };
            DataContext = this;
            LoadwarehouseFromProductsAsync();
            _ = LoadAvailableUnit();
            LoadRecipes();
            LoadAvailablNameProductWarehose();
        }

        public async void LoadAvailablNameProductWarehose()
        {
            try
            {
                var productWarehose = _warehouseService.GetWarehouseFromProductAll();
                RecipeNameCobobox.ItemsSource = productWarehose;
                RecipeNameCobobox.DisplayMemberPath = "Name";
                RecipeNameCobobox.SelectedValuePath = "Name";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المواد: {ex.Message}", "خطأ",
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
                RecipeUnitComboBox.DisplayMemberPath = "Value";
                RecipeUnitComboBox.SelectedValuePath = "Key";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الوحدات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RecipeNameCobobox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (RecipeNameCobobox.SelectedItem is WarehouseFromProduct whItem)
                {
                    NewRecipesNameProduct = whItem.Name ?? string.Empty;
                    var unitToUse = whItem.PortionUnit ?? PortionUnit.Kg;
                    string unitDisplay = PortionUnitHelper.GetDisplayName(unitToUse);
                    if (SelectedStockInfoText != null)
                    {
                        SelectedStockInfoText.Text = $"📦 المتوفر حالياً بالمخزون: {whItem.StockQuantity:0.##} {unitDisplay}";
                    }
                    if (!_isEditing)
                    {
                        NewRecipesportionUnit = unitToUse;
                        RecipeUnitComboBox.SelectedValue = unitToUse;
                        RecipeUnitComboBox.Text = unitDisplay;
                    }
                }
                else if (SelectedStockInfoText != null)
                {
                    SelectedStockInfoText.Text = string.Empty;
                }
            }
            catch { }
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
                if (FindProducts == null)
                {
                    MessageBox.Show("المنتج غير موجود في قاعدة البيانات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

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
                MessageBox.Show($"خطأ في تحميل الوصفات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearForm()
        {
            QuantityRecipe = "0";
            RecipeNameCobobox.Text = "";
            RecipeQuntityTextBox.Text = "";
            RecipeUnitComboBox.Text = "";
            if (SelectedStockInfoText != null)
                SelectedStockInfoText.Text = "";
            IsMainIngredient = false;
            OnPropertyChanged(nameof(IsMainIngredient));
        }

        private void CancelEdit()
        {
            _isEditing = false;
            _editingRecipe = null;
            ClearForm();
            AddUpdateButton.Content = "➕ إضافة وصفة";
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private async void AddUpdateCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string rawQty = !string.IsNullOrWhiteSpace(RecipeQuntityTextBox.Text) ? RecipeQuntityTextBox.Text : QuantityRecipe;
                string normalizedQuantity = (rawQty ?? "0").Replace(',', '.');
                if (!decimal.TryParse(normalizedQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedQuantity) || parsedQuantity <= 0)
                {
                    MessageBox.Show("يرجى إدخال كمية صحيحة أكبر من الصفر (مثال: 4 أو 2 أو 0.5)", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string? selectedProduct = RecipeNameCobobox.SelectedValue as string ?? NewRecipesNameProduct;
                if (string.IsNullOrWhiteSpace(selectedProduct))
                {
                    MessageBox.Show("يرجى اختيار المادة من المخزون", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                PortionUnit selectedUnit = RecipeUnitComboBox.SelectedValue is PortionUnit pu
                    ? pu
                    : (NewRecipesportionUnit != 0 ? NewRecipesportionUnit : PortionUnit.Kg);

                var allWhProducts = _warehouseService.GetWarehouseFromProductAll();
                var matchedWhProduct = allWhProducts.FirstOrDefault(x => x.Name != null && x.Name.Trim().Equals(selectedProduct.Trim(), StringComparison.OrdinalIgnoreCase));

                if (_isEditing && _editingRecipe != null)
                {
                    if (matchedWhProduct == null)
                    {
                        MessageBox.Show($"لم يتم العثور على المادة '{selectedProduct}' في المخزن.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    NewRecipesNameProduct = selectedProduct;
                    decimal requiredInWarehouseUnit = UnitConversionHelper.ConvertQuantity(parsedQuantity, selectedUnit, matchedWhProduct.PortionUnit);
                    decimal oldInWarehouseUnit = UnitConversionHelper.ConvertQuantity(_editingRecipe.Quantity, _editingRecipe.portionUnit, matchedWhProduct.PortionUnit);
                    decimal availableStockBeforeUpdate = matchedWhProduct.StockQuantity + oldInWarehouseUnit;

                    if (availableStockBeforeUpdate < requiredInWarehouseUnit)
                    {
                        string whUnitStr = matchedWhProduct.PortionUnit.HasValue ? PortionUnitHelper.GetDisplayName(matchedWhProduct.PortionUnit.Value) : "";
                        MessageBox.Show($"عذراً، المخزون غير كافٍ. المتوفر حالياً بالمستودع: {matchedWhProduct.StockQuantity:0.##} {whUnitStr}",
                            "المخزون غير كافٍ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _editingRecipe.Name = NewRecipesNameProduct.Trim();
                    _editingRecipe.Quantity = parsedQuantity;
                    _editingRecipe.portionUnit = selectedUnit;
                    _editingRecipe.IsMainIngredient = IsMainIngredient;
                    _editingRecipe.WarehouseFromProductId = matchedWhProduct.Id;

                    await _recipesServices.UpdateRecipeAsync(_editingRecipe);
                    MessageBox.Show("تم تحديث الوصفة بنجاح", "تم التحديث",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    CancelEdit();
                }
                else
                {
                    var newRecipe = new Recipe
                    {
                        Name = selectedProduct.Trim(),
                        Quantity = parsedQuantity,
                        portionUnit = selectedUnit,
                        IsMainIngredient = IsMainIngredient,
                        ProductId = Id,
                        WarehouseFromProductId = matchedWhProduct?.Id,
                        Product = await _productService.GetProductByIdAsync(Id)
                    };

                    var CreatedRecipes = await _recipesServices.CreateRecipeAsync(newRecipe);
                    Recipes.Add(CreatedRecipes);

                    MessageBox.Show("تم إضافة الوصفة بنجاح", "تم الإضافة",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
                await LoadRecipesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الوصفة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Recipe Recipe)
            {
                _editingRecipe = Recipe;
                _isEditing = true;

                NewRecipesNameProduct = Recipe.Name;
                QuantityRecipe = Recipe.Quantity.ToString(CultureInfo.InvariantCulture);
                NewRecipesportionUnit = Recipe.portionUnit;
                IsMainIngredient = Recipe.IsMainIngredient;
                OnPropertyChanged(nameof(IsMainIngredient));

                var matchedItem = (RecipeNameCobobox.ItemsSource as IEnumerable<WarehouseFromProduct>)?
                    .FirstOrDefault(p => p.Name != null && p.Name.Trim().Equals(Recipe.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (matchedItem != null)
                {
                    RecipeNameCobobox.SelectedItem = matchedItem;
                    RecipeNameCobobox.SelectedValue = matchedItem.Name;
                }
                else
                {
                    RecipeNameCobobox.SelectedValue = Recipe.Name;
                    RecipeNameCobobox.Text = NewRecipesNameProduct;
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
                var result = MessageBox.Show($"هل أنت متأكد من حذف وصفة '{Recipe.Name}'؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _recipesServices.DeleteRecipeAsync(Recipe.Id);
                        Recipes.Remove(Recipe);

                        MessageBox.Show("تم حذف الوصفة بنجاح", "تم الحذف",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في حذف الوصفة: {ex.Message}", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
