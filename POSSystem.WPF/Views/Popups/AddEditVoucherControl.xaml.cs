using System;
using System.Collections.Generic;
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
    public partial class AddEditVoucherControl : UserControl
    {
        private VoucherViewModel _viewModel = null!;
        private ExpenseManagementViewModel _parentViewModel = null!;
        private ReceiptVoucher? _editingVoucher;
        private decimal _currentAvailableBalance = 0m;

        public AddEditVoucherControl(ExpenseManagementViewModel parentViewModel, ReceiptVoucher? voucher = null)
        {
            try
            {
                InitializeComponent();
                _parentViewModel = parentViewModel;
                _editingVoucher = voucher;
                
                _viewModel = new VoucherViewModel
                {
                    VoucherNumber = voucher?.VoucherNumber ?? "سيتم التوليد تلقائياً",
                    Amount = voucher?.Amount ?? 0,
                    VoucherType = voucher?.Type ?? VoucherType.Receipt,
                    SelectedVoucherType = voucher?.Type ?? VoucherType.Receipt,
                    CustomerName = voucher?.CustomerName ?? "",
                    VoucherDate = voucher?.VoucherDate ?? DateTime.Now,
                    Notes = voucher?.Notes ?? "",
                    IsEditing = voucher != null
                };
                
                DataContext = _viewModel;
                VoucherDatePicker.SelectedDate = _viewModel.VoucherDate;

                if (voucher != null)
                {
                    string targetTag = "Cash";
                    if (voucher.PaymentMethod == PaymentMethod.BankTransfer)
                    {
                        targetTag = "BankTransfer";
                    }
                    else if (voucher.PaymentMethod == PaymentMethod.DebitCard || voucher.PaymentMethod == PaymentMethod.CreditCard)
                    {
                        targetTag = "DebitCard";
                    }
                    else if (voucher.PaymentMethod == PaymentMethod.MobilePayment)
                    {
                        var notes = voucher.Notes ?? string.Empty;
                        if (notes.Contains("كريمي")) targetTag = "Wallet_Kuraimi";
                        else if (notes.Contains("جيب")) targetTag = "Wallet_Jeeb";
                        else if (notes.Contains("يمن والت")) targetTag = "Wallet_YemenWallet";
                        else if (notes.Contains("ون كاش")) targetTag = "Wallet_OneCash";
                        else if (notes.Contains("فلوسك")) targetTag = "Wallet_Floosak";
                        else if (notes.Contains("جوالي")) targetTag = "Wallet_Jawali";
                        else if (notes.Contains("محفظتي")) targetTag = "Wallet_Mahfathati";
                        else if (notes.Contains("كاك") || notes.Contains("كاش")) targetTag = "Wallet_CacCash";
                        else if (notes.Contains("موبايل موني")) targetTag = "Wallet_MobileMoney";
                        else targetTag = "MobilePayment";
                    }

                    foreach (ComboBoxItem item in PaymentMethodComboBox.Items)
                    {
                        if (item.Tag?.ToString() == targetTag)
                        {
                            PaymentMethodComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                _ = UpdateAvailableBalanceAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة السند: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CloseOverlay();
            }
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionRefPanel == null) return;
            var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString() ?? "Cash";

            if (tag != "Cash")
            {
                TransactionRefPanel.Visibility = Visibility.Visible;
                if (tag.StartsWith("Wallet_") || tag == "MobilePayment")
                {
                    TransactionRefLabel.Text = "رقم العملية / الحوالة في المحفظة (اختياري)";
                }
                else if (tag == "BankTransfer")
                {
                    TransactionRefLabel.Text = "رقم الحوالة البنكية / الإشعار (اختياري)";
                }
                else
                {
                    TransactionRefLabel.Text = "رقم الإيصال / العملية (اختياري)";
                }
            }
            else
            {
                TransactionRefPanel.Visibility = Visibility.Collapsed;
            }

            _ = UpdateAvailableBalanceAsync();
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = UpdateAvailableBalanceAsync();
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAmountAgainstBalance();
        }

        public async Task UpdateAvailableBalanceAsync()
        {
            try
            {
                if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null) return;

                var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
                var tag = selectedItem?.Tag?.ToString() ?? "Cash";
                var channelName = FinancialChannelHelper.GetChannelDisplayName(tag);

                AvailableBalanceTextBlock.Text = "جاري التحميل...";

                decimal balance = await ServiceLocator.FinancialChannelService.GetAvailableBalanceAsync(tag);

                // إذا كنا نقوم بتعديل سند صرف سابق، نعيد احتساب مبلغه ضمن المتاح لنفس السند
                if (_editingVoucher != null && _editingVoucher.Type == VoucherType.Payment)
                {
                    var oldChannel = FinancialChannelHelper.ExtractChannel(_editingVoucher.PaymentMethod, _editingVoucher.Notes);
                    if (FinancialChannelHelper.NormalizeChannelKey(tag) == oldChannel)
                    {
                        balance += _editingVoucher.Amount;
                    }
                }

                _currentAvailableBalance = balance;
                AvailableBalanceTextBlock.Text = $"{balance:N0} ريال";

                ValidateAmountAgainstBalance();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating available balance: {ex.Message}");
            }
        }

        private void ValidateAmountAgainstBalance()
        {
            if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null || AvailableBalanceLabel == null) return;

            var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString() ?? "Cash";
            var channelName = FinancialChannelHelper.GetChannelDisplayName(tag);

            decimal enteredAmount = 0m;
            if (decimal.TryParse(AmountTextBox?.Text?.Trim(), out var a))
            {
                enteredAmount = a;
            }
            else
            {
                enteredAmount = _viewModel?.Amount ?? 0m;
            }

            bool isPayment = _viewModel?.SelectedVoucherType == VoucherType.Payment;

            if (isPayment)
            {
                if (enteredAmount > _currentAvailableBalance)
                {
                    AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fef2f2"));
                    AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fca5a5"));
                    AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
                    AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991b1b"));
                    AvailableBalanceLabel.Text = $"⚠️ رصيد {channelName} غير كافٍ! المتاح:";
                    if (AvailableBalanceIcon != null) AvailableBalanceIcon.Text = "⚠️";
                }
                else
                {
                    AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0fdf4"));
                    AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86efac"));
                    AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803d"));
                    AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
                    AvailableBalanceLabel.Text = $"الرصيد المتاح في {channelName}:";
                    if (AvailableBalanceIcon != null) AvailableBalanceIcon.Text = "💳";
                }
            }
            else
            {
                // سند قبض: الرصيد سيزداد
                AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#eff6ff"));
                AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bfdbfe"));
                AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1d4ed8"));
                AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e40af"));
                AvailableBalanceLabel.Text = $"الرصيد الحالي في {channelName} (سيزداد):";
                if (AvailableBalanceIcon != null) AvailableBalanceIcon.Text = "📥";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_viewModel.CustomerName))
            {
                MessageBox.Show("يرجى إدخال اسم العميل", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerNameTextBox.Focus();
                return;
            }

            if (_viewModel.Amount <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            if (VoucherDatePicker.SelectedDate == null)
            {
                MessageBox.Show("يرجى اختيار تاريخ السند", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                VoucherDatePicker.Focus();
                return;
            }

            _viewModel.VoucherDate = VoucherDatePicker.SelectedDate.Value;

            var selectedMethodItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var methodTag = selectedMethodItem?.Tag?.ToString() ?? "Cash";
            var channelDisplayName = FinancialChannelHelper.GetChannelDisplayName(methodTag);

            // التحقق من كفاية الرصيد قبل اعتماد وحفظ سند الصرف
            if (_viewModel.SelectedVoucherType == VoucherType.Payment)
            {
                if (_viewModel.Amount > _currentAvailableBalance)
                {
                    var shortage = _viewModel.Amount - _currentAvailableBalance;
                    MessageBox.Show(
                        $"عفواً، لا يمكن إتمام عملية الصرف لعدم كفاية الرصيد في جهة الصرف المحددة!\n\n" +
                        $"• جهة الصرف: {channelDisplayName}\n" +
                        $"• الرصيد المتاح حالياً: {_currentAvailableBalance:N0} ريال\n" +
                        $"• المبلغ المطلوب صرفه: {_viewModel.Amount:N0} ريال\n" +
                        $"• مقدار العجز: {shortage:N0} ريال\n\n" +
                        "يرجى تخفيض المبلغ أو اختيار جهة صرف أخرى بها سيولة كافية.",
                        "الرصيد المتاح غير كافٍ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    AmountTextBox.Focus();
                    return;
                }
            }

            try
            {
                var paymentMethod = (methodTag.StartsWith("Wallet_") || methodTag == "MobilePayment")
                    ? PaymentMethod.MobilePayment
                    : methodTag switch
                    {
                        "Cash" => PaymentMethod.Cash,
                        "BankTransfer" => PaymentMethod.BankTransfer,
                        "DebitCard" => PaymentMethod.DebitCard,
                        _ => PaymentMethod.Cash
                    };

                // Prepare wallet / reference notes
                string finalNotes = _viewModel.Notes ?? string.Empty;
                string walletName = methodTag switch
                {
                    "Wallet_Kuraimi" => "حاسب كريمي",
                    "Wallet_Jeeb" => "محفظة جيب",
                    "Wallet_Floosak" => "محفظة فلوسك",
                    "Wallet_OneCash" => "محفظة ون كاش",
                    "Wallet_Jawali" => "محفظة جوالي",
                    "Wallet_MobileMoney" => "محفظة موبايل موني",
                    "Wallet_YemenWallet" => "محفظة يمن والت",
                    _ => methodTag.StartsWith("Wallet_") ? methodTag.Substring("Wallet_".Length) : string.Empty
                };

                var refNo = TransactionRefTextBox?.Text?.Trim();
                var noteTags = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(walletName) && !finalNotes.Contains(walletName))
                {
                    noteTags.Add($"[محفظة: {walletName}]");
                }
                if (!string.IsNullOrEmpty(refNo) && !finalNotes.Contains(refNo))
                {
                    noteTags.Add($"[رقم العملية: {refNo}]");
                }
                if (noteTags.Count > 0)
                {
                    finalNotes = string.IsNullOrWhiteSpace(finalNotes)
                        ? string.Join(" ", noteTags)
                        : $"{finalNotes} {string.Join(" ", noteTags)}";
                }

                ReceiptVoucher voucher;
                
                if (_editingVoucher != null)
                {
                    // Update existing voucher (modify tracked entity)
                    _editingVoucher.CustomerName = _viewModel.CustomerName;
                    _editingVoucher.Name = string.IsNullOrWhiteSpace(_viewModel.CustomerName) ? _editingVoucher.Name : _viewModel.CustomerName;
                    _editingVoucher.Amount = _viewModel.Amount;
                    _editingVoucher.Type = _viewModel.SelectedVoucherType;
                    _editingVoucher.PaymentMethod = paymentMethod;
                    _editingVoucher.VoucherDate = _viewModel.VoucherDate;
                    _editingVoucher.Notes = finalNotes;
                    
                    await _parentViewModel.UpdateVoucherAsync(_editingVoucher);
                    MessageBox.Show("تم تحديث السند بنجاح!", "تم الحفظ", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Create new voucher
                    voucher = new ReceiptVoucher
                    {
                        VoucherNumber = "",
                        Amount = _viewModel.Amount,
                        Type = _viewModel.SelectedVoucherType,
                        PaymentMethod = paymentMethod,
                        CustomerName = _viewModel.CustomerName,
                        Name = string.IsNullOrWhiteSpace(_viewModel.CustomerName) ? string.Empty : _viewModel.CustomerName,
                        VoucherDate = _viewModel.VoucherDate,
                        Notes = finalNotes,
                        UserId = AdminDashboardWindow._currentUser?.Id,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    
                    await _parentViewModel.AddVoucherAsync(voucher);
                    MessageBox.Show("تم إضافة السند بنجاح!", "تم الحفظ", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CloseOverlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ السند: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.CloseOverlay();
        }
    }

    public class VoucherViewModel : BaseViewModel
    {
        private string _voucherNumber = "";
        private decimal _amount;
        private VoucherType _voucherType = VoucherType.Receipt;
        private VoucherType _selectedVoucherType = VoucherType.Receipt;
        private string _customerName = "";
        private DateTime _voucherDate = DateTime.Now;
        private string _notes = "";
        private bool _isEditing = false;

        public string WindowTitle => _isEditing ? "✏️ تعديل سند" : "➕ إضافة سند جديد";

        public List<VoucherType> VoucherTypes { get; } = new() 
        { 
            VoucherType.Receipt, 
            VoucherType.Payment 
        };

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public string VoucherNumber
        {
            get => _voucherNumber;
            set => SetProperty(ref _voucherNumber, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public VoucherType VoucherType
        {
            get => _voucherType;
            set => SetProperty(ref _voucherType, value);
        }

        public VoucherType SelectedVoucherType
        {
            get => _selectedVoucherType;
            set => SetProperty(ref _selectedVoucherType, value);
        }

        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        public DateTime VoucherDate
        {
            get => _voucherDate;
            set => SetProperty(ref _voucherDate, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }
    }
}
