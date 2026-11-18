using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using POSSystem.WPF.Business;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.DTOs;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Views.Popups
{
    public partial class KitchenCookingBatchPopup : UserControl
    {
        public event EventHandler? CloseRequested;

        private List<Product> _cookableProducts = new();
        private List<WarehouseFromProduct> _warehouseStock = new();
        private List<KitchenCookingBatch> _recentBatches = new();
        private Product? _selectedProduct;
        private Recipe? _mainRecipe;
        private decimal _standardMainQty = 1.0m;
        private decimal _standardYield = 100m;
        private decimal _expectedPortions = 100m;
        private bool _isDeficitApproved = false;
        private decimal _approvedDeficitPortions = 0m;
        private string? _deficitAuditNote = null;
        private User? _currentUser;

        public class IngredientPreviewViewModel
        {
            public string Name { get; set; } = string.Empty;
            public string RequiredDisplay { get; set; } = string.Empty;
            public string StockStatusDisplay { get; set; } = string.Empty;
            public Brush StatusBrush { get; set; } = Brushes.Green;
            public bool IsSufficient { get; set; } = true;
        }

        public class SettleBatchOption
        {
            public int BatchId { get; set; }
            public KitchenCookingBatch Batch { get; set; } = null!;
            public string DisplayText { get; set; } = string.Empty;
            public override string ToString() => DisplayText;
        }

        public KitchenCookingBatchPopup()
        {
            InitializeComponent();
            _ = LoadInitialDataAsync();
        }

        public KitchenCookingBatchPopup(User? currentUser) : this()
        {
            _currentUser = currentUser;
        }

        private int _initialProductId = 0;

        public KitchenCookingBatchPopup(int defaultProductId, User? currentUser = null) : this(currentUser)
        {
            _initialProductId = defaultProductId;
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                var products = await ServiceLocator.KitchenCookingBatchService.GetBatchCookableProductsAsync();
                _cookableProducts = products.ToList();
                ProductComboBox.ItemsSource = _cookableProducts;

                var whStock = await ServiceLocator.UnitOfWork.warehouseFromProduct.FindAsync(w => w.IsActive);
                _warehouseStock = whStock.ToList();

                var recent = await ServiceLocator.KitchenCookingBatchService.GetRecentSettleableBatchesAsync(3);
                _recentBatches = recent.ToList();

                var options = _recentBatches.Select(b =>
                {
                    bool alreadySettled = !string.IsNullOrWhiteSpace(b.Notes) && b.Notes.Contains("[تسوية وفر معتمدة:");
                    string statusTag = alreadySettled ? " (تمت تسويته)" : "";
                    return new SettleBatchOption
                    {
                        BatchId = b.Id,
                        Batch = b,
                        DisplayText = $"{b.Product?.Name ?? "وجبة"} | رقم الطبخة: {b.BatchNumber} | المسجل: {b.ActualPortions:N0} نفر | {b.CookedAt:yyyy/MM/dd HH:mm}{statusTag}"
                    };
                }).ToList();

                SettleBatchComboBox.ItemsSource = options;
                if (options.Any())
                    SettleBatchComboBox.SelectedIndex = 0;

                if (_cookableProducts.Any())
                {
                    if (_initialProductId > 0 && _cookableProducts.Any(p => p.Id == _initialProductId))
                        ProductComboBox.SelectedValue = _initialProductId;
                    else
                        ProductComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الطبخ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TabNewCookingBtn_Click(object sender, RoutedEventArgs e)
        {
            TabNewCookingBtn.Background = new SolidColorBrush(Color.FromRgb(0, 53, 140));
            TabNewCookingBtn.Foreground = Brushes.White;
            TabSurplusSettlementBtn.Background = Brushes.Transparent;
            TabSurplusSettlementBtn.Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85));

            NewCookingPanel.Visibility = Visibility.Visible;
            SurplusSettlementPanel.Visibility = Visibility.Collapsed;
            SubmitBatchButton.Visibility = Visibility.Visible;
            SubmitSurplusButton.Visibility = Visibility.Collapsed;
        }

        private void TabSurplusSettlementBtn_Click(object sender, RoutedEventArgs e)
        {
            TabSurplusSettlementBtn.Background = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            TabSurplusSettlementBtn.Foreground = Brushes.White;
            TabNewCookingBtn.Background = Brushes.Transparent;
            TabNewCookingBtn.Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85));

            NewCookingPanel.Visibility = Visibility.Collapsed;
            SurplusSettlementPanel.Visibility = Visibility.Visible;
            SubmitBatchButton.Visibility = Visibility.Collapsed;
            SubmitSurplusButton.Visibility = Visibility.Visible;
            UpdateSurplusPreview();
        }

        private void SettleBatchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSurplusPreview();
        }

        private void SurplusPortionsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSurplusPreview();
        }

        private void UpdateSurplusPreview()
        {
            if (SettleBatchComboBox?.SelectedItem is not SettleBatchOption opt)
            {
                if (SettleBatchDetailsBorder != null)
                    SettleBatchDetailsBorder.Visibility = Visibility.Collapsed;
                return;
            }

            SettleBatchDetailsBorder.Visibility = Visibility.Visible;
            var batch = opt.Batch;
            var prod = _cookableProducts.FirstOrDefault(p => p.Id == batch.ProductId) ?? batch.Product;

            SettleRecordedPortionsText.Text = $"{batch.ActualPortions:N0} نفر";
            SettleCurrentPosQtyText.Text = $"{(prod?.AvailableQtyInPOS ?? 0m):N0} نفر";

            decimal surplus = 0m;
            if (SurplusPortionsTextBox != null)
            {
                decimal.TryParse(SurplusPortionsTextBox.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out surplus);
            }

            if (SurplusNewTotalPreviewText != null)
            {
                decimal newTotal = batch.ActualPortions + Math.Max(0, surplus);
                decimal newPos = (prod?.AvailableQtyInPOS ?? 0m) + Math.Max(0, surplus);
                SurplusNewTotalPreviewText.Text = $"{newTotal:N0} نفر (سيصبح بالكاشير: {newPos:N0} نفر)";
            }
        }

        private async void SubmitSurplusButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettleBatchComboBox.SelectedItem is not SettleBatchOption opt)
            {
                MessageBox.Show("يرجى اختيار دفعة الطهي / القدر المراد تسوية الزيادة له.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string surplusText = SurplusPortionsTextBox.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(surplusText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal surplusPortions) || surplusPortions <= 0)
            {
                MessageBox.Show("يرجى إدخال عدد أنفار الزيادة بشكل صحيح (أكبر من صفر).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string reason = SurplusReasonTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("يرجى كتابة سبب الزيادة أو ملاحظة التسوية.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SubmitSurplusButton.IsEnabled = false;
                var updated = await ServiceLocator.KitchenCookingBatchService.SettleCookingSurplusAsync(
                    opt.BatchId,
                    surplusPortions,
                    reason,
                    _currentUser?.Id,
                    _currentUser?.Name);

                MessageBox.Show(
                    $"📈 تم اعتماد تسوية زيادة القدر بنجاح!\n\n" +
                    $"رقم الطبخة: {updated.BatchNumber}\n" +
                    $"الأنفار المضافة للكاشير: +{surplusPortions:N0} نفر\n" +
                    $"إجمالي إنتاج القدر بعد التسوية: {updated.ActualPortions:N0} نفر\n" +
                    $"سحب مواد خام من المستودع: 0.00 (بدون أي خصم إضافي من المخزن)",
                    "نجاح تسوية زيادة القدر",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر تسوية زيادة القدر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitSurplusButton.IsEnabled = true;
            }
        }

        private bool _suppressTextChanged = false;

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (e.AddedItems != null && e.AddedItems.Count > 0 ? e.AddedItems[0] as Product : null)
                           ?? ProductComboBox.SelectedItem as Product;
            if (selected == null) return;

            _selectedProduct = selected;

            try
            {
                var context = ServiceLocator.UnitOfWork.GetContext();
                var freshProduct = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(context.Products)
                    .FirstOrDefault(p => p.Id == selected.Id);
                if (freshProduct != null)
                {
                    _selectedProduct.YieldRate = freshProduct.YieldRate;
                    _selectedProduct.portionUnit = freshProduct.portionUnit;
                    _selectedProduct.TypeWarehoseFromProduct = freshProduct.TypeWarehoseFromProduct;
                    _selectedProduct.AvailableQtyInPOS = freshProduct.AvailableQtyInPOS;
                }

                var freshRecipes = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(context.Recipes)
                    .Where(r => r.ProductId == selected.Id)
                    .ToList();

                if (freshRecipes.Count == 0 && !string.IsNullOrWhiteSpace(_selectedProduct.TypeWarehoseFromProduct))
                {
                    var fallbackWh = _warehouseStock.FirstOrDefault(w =>
                        w.Name == _selectedProduct.TypeWarehoseFromProduct ||
                        w.Id.ToString() == _selectedProduct.TypeWarehoseFromProduct);
                    if (fallbackWh != null)
                    {
                        freshRecipes.Add(new Recipe
                        {
                            ProductId = _selectedProduct.Id,
                            WarehouseFromProductId = fallbackWh.Id,
                            Name = fallbackWh.Name ?? _selectedProduct.Name,
                            Quantity = 1m,
                            portionUnit = _selectedProduct.portionUnit != 0 ? _selectedProduct.portionUnit : (fallbackWh.PortionUnit ?? PortionUnit.Bag),
                            IsMainIngredient = true
                        });
                    }
                }

                _selectedProduct.Recipe = freshRecipes;
            }
            catch { }

            _mainRecipe = _selectedProduct.Recipe?.FirstOrDefault(r => r.IsMainIngredient)
                       ?? _selectedProduct.Recipe?.FirstOrDefault();

            _standardMainQty = _mainRecipe != null && _mainRecipe.Quantity > 0 ? _mainRecipe.Quantity : 1.0m;
            _standardYield = _selectedProduct.YieldRate > 0 ? (decimal)_selectedProduct.YieldRate : 100m;
            _expectedPortions = _standardYield;

            _isDeficitApproved = false;
            _approvedDeficitPortions = 0m;
            _deficitAuditNote = null;

            var resolvedUnit = _mainRecipe != null
                ? _mainRecipe.portionUnit
                : (_selectedProduct.portionUnit != 0 ? _selectedProduct.portionUnit : PortionUnit.Bag);
            string mainUnitName = PortionUnitHelper.GetDisplayName(resolvedUnit);

            StandardInfoBorder.Visibility = Visibility.Visible;
            if (_mainRecipe != null)
            {
                StandardRecipeText.Text = $"{_standardMainQty:G29} {mainUnitName} = {_standardYield:G29} نفر (وجبة: {_selectedProduct.Name})";
                MainIngredientText.Text = $"المكون الأساسي لـ ({_selectedProduct.Name}): {_mainRecipe.Name} ({_standardMainQty:G29} {mainUnitName})";
            }
            else
            {
                StandardRecipeText.Text = $"⚠️ لم يتم تحديد وصفة لوجبة ({_selectedProduct.Name}) بعد";
                MainIngredientText.Text = $"يرجى الدخول إلى إدارة المنتجات -> زر (وصفة) بجوار ({_selectedProduct.Name}) لتحديد المكون الأساسي ومعدل الأنفار";
            }
            UnitLabelText.Text = mainUnitName;

            _suppressTextChanged = true;
            MainQtyTextBox.Text = _standardMainQty.ToString("G29", CultureInfo.InvariantCulture);
            ActualPortionsTextBox.Text = _standardYield.ToString("G29", CultureInfo.InvariantCulture);
            _suppressTextChanged = false;

            UpdateVarianceFeedback();
            UpdateIngredientsPreview();
        }

        private void MultiplierButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (decimal.TryParse(btn.Tag.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal mult))
                {
                    decimal scaledQty = _standardMainQty * mult;
                    decimal scaledYield = Math.Round(_standardYield * mult);
                    _expectedPortions = scaledYield;

                    _isDeficitApproved = false;
                    _approvedDeficitPortions = 0m;
                    _deficitAuditNote = null;

                    MainQtyTextBox.Text = scaledQty.ToString("G29", CultureInfo.InvariantCulture);
                    ActualPortionsTextBox.Text = scaledYield.ToString("G29", CultureInfo.InvariantCulture);

                    UpdateVarianceFeedback();
                    UpdateIngredientsPreview();
                }
            }
        }

        private void MainQtyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextChanged || _selectedProduct == null || _standardMainQty <= 0) return;

            string text = MainQtyTextBox.Text.Trim().Replace(',', '.');
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal customQty) && customQty > 0)
            {
                decimal ratio = customQty / _standardMainQty;
                _expectedPortions = Math.Round(_standardYield * ratio);

                _isDeficitApproved = false;
                _approvedDeficitPortions = 0m;
                _deficitAuditNote = null;

                ActualPortionsTextBox.Text = _expectedPortions.ToString("G29", CultureInfo.InvariantCulture);
                UpdateVarianceFeedback();
                UpdateIngredientsPreview();
            }
        }

        private void ActualPortionsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateVarianceFeedback();
        }

        private void UpdateVarianceFeedback()
        {
            if (VarianceFeedbackText == null || ActualPortionsTextBox == null) return;

            string portionsText = ActualPortionsTextBox.Text.Trim().Replace(',', '.');
            if (decimal.TryParse(portionsText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal actual))
            {
                decimal diff = actual - _expectedPortions;
                if (diff == 0)
                {
                    VarianceFeedbackText.Text = $"مطابق للمعيار القياسي للقدر ({_expectedPortions:G29} نفر)";
                    VarianceFeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(2, 132, 199));
                }
                else if (diff > 0)
                {
                    VarianceFeedbackText.Text = $"🟢 وفر إنتاج إضافي: +{diff:G29} نفر (زيادة قدر)";
                    VarianceFeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(1, 146, 42));
                }
                else
                {
                    decimal deficit = Math.Abs(diff);
                    VarianceFeedbackText.Text = $"⚠️ عجز وهدر: -{deficit:G29} نفر (يتطلب اعتماد المشرف)";
                    VarianceFeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                }
            }
        }

        private void UpdateIngredientsPreview()
        {
            if (_selectedProduct == null || _selectedProduct.Recipe == null)
            {
                IngredientsPreviewItems.ItemsSource = null;
                return;
            }

            string text = MainQtyTextBox.Text.Trim().Replace(',', '.');
            decimal currentQty = decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal q) ? q : _standardMainQty;
            decimal ratio = _standardMainQty > 0 ? (currentQty / _standardMainQty) : 1.0m;

            var previewList = new List<IngredientPreviewViewModel>();
            bool allSufficient = true;

            foreach (var recipe in _selectedProduct.Recipe)
            {
                if (recipe.Quantity <= 0) continue;

                var whItem = _warehouseStock.FirstOrDefault(w => 
                    (recipe.WarehouseFromProductId.HasValue && w.Id == recipe.WarehouseFromProductId.Value)
                    || (w.Name != null && !string.IsNullOrWhiteSpace(recipe.Name) && w.Name.Trim().Equals(recipe.Name.Trim(), StringComparison.OrdinalIgnoreCase)));

                decimal neededQty = recipe.Quantity * ratio;
                decimal convertedQty = whItem != null 
                    ? UnitConversionHelper.ConvertQuantity(neededQty, recipe.portionUnit, whItem.PortionUnit)
                    : neededQty;

                decimal available = whItem?.StockQuantity ?? 0m;
                bool isSuff = whItem != null && available >= convertedQty;
                if (!isSuff) allSufficient = false;

                var unitToDisplay = whItem?.PortionUnit ?? recipe.portionUnit;
                string unitName = PortionUnitHelper.GetDisplayName(unitToDisplay);

                previewList.Add(new IngredientPreviewViewModel
                {
                    Name = whItem?.Name ?? recipe.Name,
                    RequiredDisplay = $"{convertedQty:N0} {unitName}",
                    StockStatusDisplay = whItem != null 
                        ? (isSuff ? $"متوفر: {available:N0} {unitName}" : $"عجز! المتوفر: {available:N0} {unitName}")
                        : "غير موجود بالمستودع!",
                    StatusBrush = isSuff ? new SolidColorBrush(Color.FromRgb(1, 146, 42)) : new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    IsSufficient = isSuff
                });
            }

            IngredientsPreviewItems.ItemsSource = previewList;
            SubmitBatchButton.IsEnabled = allSufficient;
            SubmitBatchButton.ToolTip = !allSufficient ? "لا يمكن اعتماد الطبخة؛ هناك مواد خام غير كافية في المستودع." : null;
        }

        private async void SubmitBatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                MessageBox.Show("يرجى اختيار الوجبة المراد طبخها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string qtyText = MainQtyTextBox.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(qtyText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal mainQty) || mainQty <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية صحيحة للأرز/المكون الأساسي.", "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string portionsText = ActualPortionsTextBox.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(portionsText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal actualPortions) || actualPortions <= 0)
            {
                MessageBox.Show("يرجى إدخال عدد أنفار صحيح.", "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (actualPortions < _expectedPortions)
            {
                if (!_isDeficitApproved || _approvedDeficitPortions != actualPortions)
                {
                    decimal deficit = _expectedPortions - actualPortions;
                    DeficitWarningText.Text = $"⚠️ الأنفار المدخلة ({actualPortions:G29} نفر) أقل من المعيار القياسي ({_expectedPortions:G29} نفر).";
                    DeficitAmountText.Text = $"قيمة العجز والهدر: -{deficit:G29} أنفار";
                    SupervisorPasswordBox.Password = string.Empty;
                    DeficitCustomNotesTextBox.Text = string.Empty;
                    DeficitApprovalOverlay.Visibility = Visibility.Visible;
                    SupervisorPasswordBox.Focus();
                    return;
                }
            }

            await ExecuteBatchSubmissionAsync(mainQty, actualPortions);
        }

        private async void ConfirmDeficitButton_Click(object sender, RoutedEventArgs e)
        {
            string portionsText = ActualPortionsTextBox.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(portionsText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal actualPortions))
            {
                DeficitApprovalOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            string password = SupervisorPasswordBox.Password?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("يرجى إدخال كلمة مرور المشرف أو المدير للاعتماد.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                SupervisorPasswordBox.Focus();
                return;
            }

            var (isValid, supervisorName) = await VerifySupervisorPasswordAsync(password);
            if (!isValid)
            {
                MessageBox.Show("كلمة المرور غير صحيحة أو ليس لدى الحساب صلاحية مدير/مشرف.", "خطأ في الاعتماد", MessageBoxButton.OK, MessageBoxImage.Error);
                SupervisorPasswordBox.Focus();
                return;
            }

            string reason = (DeficitReasonComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "عجز وهدر في الطبخ";
            string customNotes = DeficitCustomNotesTextBox.Text?.Trim() ?? string.Empty;
            decimal deficit = _expectedPortions - actualPortions;

            _deficitAuditNote = string.IsNullOrWhiteSpace(customNotes)
                ? $"[عجز معتمد: -{deficit:G29} نفر | السبب: {reason} | المشرف: {supervisorName}]"
                : $"[عجز معتمد: -{deficit:G29} نفر | السبب: {reason} - {customNotes} | المشرف: {supervisorName}]";

            _isDeficitApproved = true;
            _approvedDeficitPortions = actualPortions;
            DeficitApprovalOverlay.Visibility = Visibility.Collapsed;

            string qtyText = MainQtyTextBox.Text.Trim().Replace(',', '.');
            decimal mainQty = decimal.TryParse(qtyText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal q) ? q : 1.0m;
            await ExecuteBatchSubmissionAsync(mainQty, actualPortions);
        }

        private void CancelDeficitButton_Click(object sender, RoutedEventArgs e)
        {
            DeficitApprovalOverlay.Visibility = Visibility.Collapsed;
            ActualPortionsTextBox.Text = _expectedPortions.ToString("G29", CultureInfo.InvariantCulture);
            _isDeficitApproved = false;
            _approvedDeficitPortions = 0m;
            _deficitAuditNote = null;
            UpdateVarianceFeedback();
        }

        private async Task<(bool IsValid, string SupervisorName)> VerifySupervisorPasswordAsync(string password)
        {
            try
            {
                var admins = await ServiceLocator.UnitOfWork.Users.FindAsync(u => u.Role == UserRole.Admin && u.IsActive);
                foreach (var admin in admins)
                {
                    if (POSSystem.WPF.Business.Services.PasswordHasher.VerifyPassword(password, admin.Password))
                    {
                        return (true, admin.Name);
                    }
                }
                return (false, string.Empty);
            }
            catch
            {
                return (false, string.Empty);
            }
        }

        private async Task ExecuteBatchSubmissionAsync(decimal mainQty, decimal actualPortions)
        {
            int warehouseId = _mainRecipe?.WarehouseFromProductId.HasValue == true
                ? (_warehouseStock.FirstOrDefault(w => w.Id == _mainRecipe.WarehouseFromProductId.Value)?.WarehouseId ?? 1)
                : 1;

            SubmitBatchButton.IsEnabled = false;
            SubmitBatchButton.Content = "⏳ جارٍ الطبخ وخصم المخزون...";

            try
            {
                string notes = NotesTextBox.Text?.Trim() ?? string.Empty;
                if (actualPortions > _expectedPortions)
                {
                    decimal surplus = actualPortions - _expectedPortions;
                    notes = $"[وفر إنتاج إضافي: +{surplus:G29} نفر] " + notes;
                }
                else if (!string.IsNullOrEmpty(_deficitAuditNote))
                {
                    notes = $"{_deficitAuditNote} " + notes;
                }

                var dto = new CreateCookingBatchDto
                {
                    ProductId = _selectedProduct!.Id,
                    WarehouseId = warehouseId,
                    MainIngredientQuantity = mainQty,
                    PortionUnit = _mainRecipe?.portionUnit ?? PortionUnit.Bag,
                    ActualPortions = actualPortions,
                    CookedByUserId = _currentUser?.Id,
                    Notes = notes.Trim()
                };

                var createdBatch = await ServiceLocator.KitchenCookingBatchService.CreateBatchAsync(dto);

                string cookedUnitName = PortionUnitHelper.GetDisplayName(createdBatch.PortionUnit);

                MessageBox.Show($"🍲 تم اعتماد طبخ الوجبة بنجاح!\n\n" +
                                $"رقم الطبخة: {createdBatch.BatchNumber}\n" +
                                $"الوجبة: {_selectedProduct.Name}\n" +
                                $"الكمية المستهلكة: {createdBatch.MainIngredientQuantity:G29} {cookedUnitName}\n" +
                                $"الأنفار المضافة للكاشير: {createdBatch.ActualPortions:N0} نفر\n" +
                                $"المعيار القياسي للقدر: {createdBatch.ExpectedPortions:N0} نفر\n" +
                                $"إجمالي تكلفة المواد: {createdBatch.TotalRawCost:N0} ريال",
                                "نجاح أمر الطبخ", MessageBoxButton.OK, MessageBoxImage.Information);

                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر اعتماد أمر الطبخ: {ex.Message}", "خطأ في الطبخ", MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitBatchButton.IsEnabled = true;
                SubmitBatchButton.Content = "🍲 اعتماد وبدء طبخ القدر";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
