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
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.ViewModels.Expenses;
using POSSystem.WPF.Views;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddEditExpenseControl : UserControl
    {
        private ExpenseViewModel _viewModel = null!;
        private ExpenseManagementViewModel _parentViewModel = null!;
        private Expense? _editingExpense;
        private decimal _currentAvailableBalance = 0m;

        public AddEditExpenseControl(ExpenseManagementViewModel parentViewModel, Expense? expense = null, string? defaultCategory = null)
        {
            try
            {
                InitializeComponent();
                _parentViewModel = parentViewModel;
                _editingExpense = expense;

                string initialCategory = !string.IsNullOrWhiteSpace(expense?.Category)
                    ? expense!.Category
                    : (!string.IsNullOrWhiteSpace(defaultCategory) && defaultCategory != "الكل (جميع الحسابات)"
                        ? defaultCategory!
                        : "حساب مصاريف المطعم والتشغيل");

                _viewModel = new ExpenseViewModel
                {
                    Name = expense?.Name ?? "",
                    Amount = expense?.Amount ?? 0,
                    SelectedCategory = initialCategory,
                    ExpenseDate = expense?.ExpenseDate ?? DateTime.Now,
                    InvoiceNumber = expense?.InvoiceNumber ?? "",
                    SupplierName = expense?.SupplierName ?? "",
                    Notes = expense?.Notes ?? "",
                    IsEditing = expense != null
                };

                // Sync any custom accounts from parentViewModel into _viewModel.Categories
                if (_parentViewModel?.expenseManagementProberty?.ExpenseCategories != null)
                {
                    foreach (var cat in _parentViewModel.expenseManagementProberty.ExpenseCategories)
                    {
                        if (!string.IsNullOrWhiteSpace(cat) && cat != "الكل (جميع الحسابات)" && !_viewModel.Categories.Contains(cat))
                        {
                            _viewModel.Categories.Add(cat);
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(initialCategory) && !_viewModel.Categories.Contains(initialCategory))
                {
                    _viewModel.Categories.Add(initialCategory);
                }

                var typeValue = expense?.Type ?? ExpenseType.OneTime;
                _viewModel.SelectedExpenseTypeItem = _viewModel.ExpenseTypes.FirstOrDefault(t => t.Value == typeValue) ?? _viewModel.ExpenseTypes.FirstOrDefault();

                if (expense != null)
                {
                    var oldChannel = FinancialChannelHelper.ExtractChannel(expense.PaymentMethod, expense.Notes);
                    var match = _viewModel.PaymentMethods.FirstOrDefault(p =>
                        FinancialChannelHelper.NormalizeChannelKey(p.ChannelTag) == oldChannel ||
                        (!string.IsNullOrWhiteSpace(p.WalletName) && expense.Notes != null && expense.Notes.Contains(p.WalletName)));
                    _viewModel.SelectedPaymentMethodItem = match ?? _viewModel.PaymentMethods[0];
                }
                else
                {
                    _viewModel.SelectedPaymentMethodItem = _viewModel.PaymentMethods[0];
                }

                DataContext = _viewModel;
                ExpenseDatePicker.SelectedDate = _viewModel.ExpenseDate;

                _ = LoadBalancesAndRefreshUIAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة المصروف: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CloseOverlay();
            }
        }

        private async Task LoadBalancesAndRefreshUIAsync()
        {
            try
            {
                foreach (var item in _viewModel.PaymentMethods)
                {
                    decimal bal = await ServiceLocator.FinancialChannelService.GetAvailableBalanceAsync(item.ChannelTag);
                    if (_editingExpense != null)
                    {
                        var oldCh = FinancialChannelHelper.ExtractChannel(_editingExpense.PaymentMethod, _editingExpense.Notes);
                        if (FinancialChannelHelper.NormalizeChannelKey(item.ChannelTag) == oldCh)
                        {
                            bal += _editingExpense.Amount;
                        }
                    }
                    item.AvailableBalance = bal;
                    item.BalanceDisplay = $"({bal:N0} ر.ي)";
                }

                if (WalletBalancesChipsControl != null)
                {
                    WalletBalancesChipsControl.ItemsSource = null;
                    WalletBalancesChipsControl.ItemsSource = _viewModel.PaymentMethods;
                }

                await UpdateSelectedChannelBalanceAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading expense channel balances: {ex.Message}");
            }
        }

        private async Task UpdateSelectedChannelBalanceAsync()
        {
            try
            {
                if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null || AvailableBalanceLabel == null) return;

                var selected = _viewModel?.SelectedPaymentMethodItem ?? _viewModel?.PaymentMethods.FirstOrDefault();
                string tag = selected?.ChannelTag ?? FinancialChannelHelper.ChannelCash;
                string displayName = selected?.Display ?? "صندوق الكاشير (نقداً)";

                decimal balance = await ServiceLocator.FinancialChannelService.GetAvailableBalanceAsync(tag);
                if (_editingExpense != null)
                {
                    var oldCh = FinancialChannelHelper.ExtractChannel(_editingExpense.PaymentMethod, _editingExpense.Notes);
                    if (FinancialChannelHelper.NormalizeChannelKey(tag) == oldCh)
                    {
                        balance += _editingExpense.Amount;
                    }
                }

                _currentAvailableBalance = balance;
                if (selected != null)
                {
                    selected.AvailableBalance = balance;
                    selected.BalanceDisplay = $"({balance:N0} ر.ي)";
                }

                AvailableBalanceTextBlock.Text = $"{balance:N0} ريال";
                ValidateAmountAgainstBalance();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating selected expense channel balance: {ex.Message}");
            }
        }

        private void ValidateAmountAgainstBalance()
        {
            if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null || AvailableBalanceLabel == null) return;

            var selected = _viewModel?.SelectedPaymentMethodItem ?? _viewModel?.PaymentMethods.FirstOrDefault();
            string displayName = selected?.Display ?? "صندوق الكاشير";
            decimal enteredAmount = _viewModel?.Amount ?? 0m;

            if (enteredAmount > _currentAvailableBalance)
            {
                AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fef2f2"));
                AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
                AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b91c1c"));
                AvailableBalanceLabel.Text = $"⚠️ الرصيد غير كافٍ في ({displayName})! المتاح حالياً:";
                AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
            }
            else
            {
                AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0fdf4"));
                AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86efac"));
                AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
                AvailableBalanceLabel.Text = $"💰 الرصيد المتاح للصرف في ({displayName}):";
                AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803d"));
            }
        }

        private async void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateSelectedChannelBalanceAsync();
        }

        private async void WalletChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PaymentMethodItem clickedItem && _viewModel != null)
            {
                _viewModel.SelectedPaymentMethodItem = clickedItem;
                await UpdateSelectedChannelBalanceAsync();
            }
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAmountAgainstBalance();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Name))
            {
                MessageBox.Show("يرجى إدخال اسم أو بيان المصروف.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_viewModel.Amount <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_viewModel.SelectedCategory))
            {
                MessageBox.Show("يرجى اختيار أو كتابة حساب الصرف (مثال: حساب قرطاسية، حساب كهرباء، حساب مصاريف المطعم...).", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ExpenseDatePicker.SelectedDate == null)
            {
                MessageBox.Show("يرجى اختيار تاريخ المصروف.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedMethod = _viewModel.SelectedPaymentMethodItem ?? _viewModel.PaymentMethods[0];
            string channelTag = selectedMethod.ChannelTag;

            await UpdateSelectedChannelBalanceAsync();
            if (_viewModel.Amount > _currentAvailableBalance)
            {
                MessageBox.Show(
                    $"لا يمكن إتمام عملية الصرف!\n\n" +
                    $"الحساب المختار: {selectedMethod.Display}\n" +
                    $"الرصيد المتوفر حالياً: {_currentAvailableBalance:N0} ريال\n" +
                    $"المبلغ المطلوب صرفه: {_viewModel.Amount:N0} ريال\n\n" +
                    $"يرجى اختيار حساب آخر يحتوي على رصيد كافٍ أو تغذية الحساب أولاً.",
                    "الرصيد غير كافٍ للصرف",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _viewModel.ExpenseDate = ExpenseDatePicker.SelectedDate.Value;

            string cleanedNotes = _viewModel.Notes ?? string.Empty;
            cleanedNotes = System.Text.RegularExpressions.Regex.Replace(cleanedNotes, @"\[محفظة:[^\]]+\]", "").Trim();
            if (!string.IsNullOrWhiteSpace(selectedMethod.WalletName))
            {
                string walletTag = $"[محفظة: {selectedMethod.WalletName}]";
                cleanedNotes = string.IsNullOrWhiteSpace(cleanedNotes)
                    ? walletTag
                    : $"{cleanedNotes} {walletTag}";
            }

            try
            {
                if (_editingExpense != null)
                {
                    _editingExpense.Name = _viewModel.Name.Trim();
                    _editingExpense.Amount = _viewModel.Amount;
                    _editingExpense.Category = _viewModel.SelectedCategory.Trim();
                    _editingExpense.Type = _viewModel.SelectedExpenseTypeItem?.Value ?? ExpenseType.OneTime;
                    _editingExpense.PaymentMethod = selectedMethod.Value;
                    _editingExpense.ExpenseDate = _viewModel.ExpenseDate;
                    _editingExpense.InvoiceNumber = _viewModel.InvoiceNumber;
                    _editingExpense.SupplierName = _viewModel.SupplierName;
                    _editingExpense.Notes = cleanedNotes;

                    await _parentViewModel.UpdateExpenseAsync(_editingExpense);
                    MessageBox.Show("تم تحديث المصروف وسند الصرف المرتبط به بنجاح!", "تم الحفظ",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var expense = new Expense
                    {
                        Name = _viewModel.Name.Trim(),
                        Amount = _viewModel.Amount,
                        Category = _viewModel.SelectedCategory.Trim(),
                        Type = _viewModel.SelectedExpenseTypeItem?.Value ?? ExpenseType.OneTime,
                        PaymentMethod = selectedMethod.Value,
                        ExpenseDate = _viewModel.ExpenseDate,
                        InvoiceNumber = _viewModel.InvoiceNumber,
                        SupplierName = _viewModel.SupplierName,
                        Notes = cleanedNotes,
                        UserId = AdminDashboardWindow._currentUser?.Id,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    await _parentViewModel.AddExpenseAsync(expense);
                    MessageBox.Show("تم تسجيل المصروف وإصدار سند الصرف وخصم المبلغ من الحساب بنجاح!", "تم الحفظ",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CloseOverlay();
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? $"{ex.Message}\n{ex.InnerException.Message}" : ex.Message;
                MessageBox.Show($"حدث خطأ أثناء حفظ المصروف: {errorMsg}", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => CloseOverlay();
        private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseOverlay();

        private void CloseOverlay()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.CloseOverlay();
        }
    }

    public class PaymentMethodItem : BaseViewModel
    {
        public PaymentMethod Value { get; set; }
        public string Display { get; set; } = "";
        public string ChannelTag { get; set; } = FinancialChannelHelper.ChannelCash;
        public string WalletName { get; set; } = "";

        private decimal _availableBalance;
        public decimal AvailableBalance
        {
            get => _availableBalance;
            set => SetProperty(ref _availableBalance, value);
        }

        private string _balanceDisplay = "";
        public string BalanceDisplay
        {
            get => _balanceDisplay;
            set => SetProperty(ref _balanceDisplay, value);
        }

        public override string ToString() => string.IsNullOrWhiteSpace(BalanceDisplay) ? Display : $"{Display} {BalanceDisplay}";
    }

    public class ExpenseTypeItem
    {
        public ExpenseType Value { get; set; }
        public string Display { get; set; } = "";
        public override string ToString() => Display;
    }

    public class ExpenseViewModel : BaseViewModel
    {
        private string _name = "";
        private string _amountString = "0";
        private string _selectedCategory = "حساب مصاريف المطعم والتشغيل";
        private ExpenseTypeItem? _selectedExpenseTypeItem;
        private DateTime _expenseDate = DateTime.Now;
        private string _invoiceNumber = "";
        private string _supplierName = "";
        private string _notes = "";
        private bool _isEditing = false;

        public ExpenseViewModel()
        {
            ExpenseTypes = new List<ExpenseTypeItem>
            {
                new() { Value = ExpenseType.OneTime, Display = "مرة واحدة" },
                new() { Value = ExpenseType.Daily, Display = "يومي" },
                new() { Value = ExpenseType.Weekly, Display = "أسبوعي" },
                new() { Value = ExpenseType.Monthly, Display = "شهري" }
            };

            PaymentMethods = new List<PaymentMethodItem>
            {
                new() { Value = PaymentMethod.Cash, Display = "💵 نقدي (من صندوق الكاشير)", ChannelTag = FinancialChannelHelper.ChannelCash, WalletName = "" }
            };

            foreach (var w in FinancialChannelHelper.GetActiveWalletsList())
            {
                string tag = FinancialChannelHelper.NormalizeChannelKey(w);
                PaymentMethods.Add(new PaymentMethodItem
                {
                    Value = PaymentMethod.MobilePayment,
                    Display = $"📱 {w}",
                    ChannelTag = tag,
                    WalletName = w
                });
            }
        }

        public string WindowTitle => _isEditing ? "✏️ تعديل مصروف" : "➕ إضافة مصروف جديد";

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public List<string> Categories { get; } = new()
        {
            "حساب مصاريف المطعم والتشغيل",
            "حساب أدوات وقرطاسية",
            "حساب مصاريف الكهرباء",
            "حساب مصاريف الزكاة",
            "حساب مصاريف الضرائب والواجبات",
            "حساب إيجار",
            "حساب مياه",
            "حساب غاز ووقود",
            "حساب صيانة ومعدات",
            "حساب مواد نظافة",
            "حساب رواتب وموظفين",
            "حساب إنترنت واتصالات",
            "حساب نثريات وأخرى"
        };

        public List<ExpenseTypeItem> ExpenseTypes { get; }

        private PaymentMethodItem? _selectedPaymentMethodItem;
        public List<PaymentMethodItem> PaymentMethods { get; }

        public PaymentMethodItem? SelectedPaymentMethodItem
        {
            get => _selectedPaymentMethodItem;
            set => SetProperty(ref _selectedPaymentMethodItem, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string AmountString
        {
            get => _amountString;
            set => SetProperty(ref _amountString, value);
        }

        private static string NormalizeDigits(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "0";
            var chars = input.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= (char)0x0660 && chars[i] <= (char)0x0669)
                    chars[i] = (char)(48 + (chars[i] - (char)0x0660));
                else if (chars[i] >= (char)0x06F0 && chars[i] <= (char)0x06F9)
                    chars[i] = (char)(48 + (chars[i] - (char)0x06F0));
                else if (chars[i] == (char)0x066B)
                    chars[i] = (char)46;
                else if (chars[i] == (char)0x066C)
                    chars[i] = (char)44;
            }
            return new string(chars).Replace(",", "");
        }

        public decimal Amount
        {
            get
            {
                var norm = NormalizeDigits(_amountString);
                if (decimal.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                    return val;
                if (decimal.TryParse(norm, out val))
                    return val;
                return 0;
            }
            set => AmountString = value.ToString(CultureInfo.InvariantCulture);
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public ExpenseTypeItem? SelectedExpenseTypeItem
        {
            get => _selectedExpenseTypeItem;
            set => SetProperty(ref _selectedExpenseTypeItem, value);
        }

        public DateTime ExpenseDate
        {
            get => _expenseDate;
            set => SetProperty(ref _expenseDate, value);
        }

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        public string SupplierName
        {
            get => _supplierName;
            set => SetProperty(ref _supplierName, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }
    }
}
