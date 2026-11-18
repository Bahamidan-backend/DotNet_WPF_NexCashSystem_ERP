using System;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Helpers;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class ShiftManagementViewModel : BaseViewModel
    {
        private readonly bool _hasPendingOrders;

        public ShiftManagementViewModel(int userId, bool hasPendingOrders = false)
        {
            _hasPendingOrders = hasPendingOrders;
            _userId = userId;
            var sp = POSSystem.WPF.App.ServiceProvider;
            var unitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _shiftService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IShiftService>(sp);
            _cashBoxTransactionService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICashBoxTransactionService>(sp);
            _userService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IUserService>(sp);
            _reportServiceForShift = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IReportService>(sp);
            _employeeService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IEmployeeService>(sp);
            _bankDepositService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<POSSystem.WPF.Business.Services.interfase.IBankDepositService>(sp);
            
            shiftManagementProberty = new ShiftManagementProberty();
            shiftManagementProberty.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(shiftManagementProberty.EndShiftDepositAmount) ||
                    e.PropertyName == nameof(shiftManagementProberty.IsDepositOnEndShift))
                {
                    RecalculateEndingCash();
                    OnPropertyChanged(nameof(ActionButtonText));
                }
            };

            LoadCurrentShiftAsync().SafeFireAndForget();
            LoadEmployeesAsync(unitOfWork);
            Commands = new ShiftManagementCommands(this);
        }

        private async void LoadEmployeesAsync(IUnitOfWork unitOfWork)
        {
            try { foreach (var user in await unitOfWork.Users.GetActiveUsersAsync()) shiftManagementProberty.Employees.Add(user); }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل بيانات الموظفين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void SetShiftType(string type) => shiftManagementProberty.SelectedShiftType = type switch { "Morning" => ShiftType.Morning, "Evening" => ShiftType.Evening, _ => ShiftType.Morning };

        public async Task LoadCurrentShiftAsync()
        {
            try
            {
                CurrentShift = await _shiftService.GetActiveShiftByUserIdAsync(_userId);
                if (CurrentShift == null)
                {
                    var lastShift = await _shiftService.GetLastClosedShiftAsync();
                    if (lastShift != null)
                    {
                        shiftManagementProberty.StartingCash = lastShift.EndingCash;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل المناوبة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task StartShiftAsync()
        {
            try
            {
                if (shiftManagementProberty.StartingCash < 0) { MessageBox.Show("المبلغ الابتدائي يجب أن يكون أكبر من أو يساوي الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (new POSSystem.WPF.Views.Popups.ShiftPasswordPopup().ShowDialog() != true) return;
                CurrentShift = await _shiftService.StartShiftAsync(_userId, shiftManagementProberty.SelectedShiftType, shiftManagementProberty.StartingCash, shiftManagementProberty.ShiftNotes);
                try { var settings = SettingsService.LoadSettings(); if (!string.IsNullOrEmpty(settings.PrinterName)) RawPrinterHelper.PurgePrinterQueue(settings.PrinterName); } catch { }
                OnPropertyChanged(nameof(ShowStartShiftSection)); OnPropertyChanged(nameof(ShowActiveShiftSection)); OnPropertyChanged(nameof(ActionButtonText)); OnPropertyChanged(nameof(ShiftStatusText)); LoadCashBoxBalance();
                MessageBox.Show($"تم بدء المناوبة بنجاح\nرقم المناوبة: {CurrentShift.ShiftNumber}", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في بدء المناوبة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void RecalculateEndingCash()
        {
            if (shiftManagementProberty.IsDepositOnEndShift)
            {
                shiftManagementProberty.EndingCash = Math.Max(0, CurrentCashInBox - shiftManagementProberty.EndShiftDepositAmount);
            }
            else
            {
                if (shiftManagementProberty.EndingCash == 0)
                {
                    shiftManagementProberty.EndingCash = CurrentCashInBox;
                }
            }
        }

        public async Task EndShiftAsync()
        {
            try
            {
                if (CurrentShift == null) return;
                if (_hasPendingOrders)
                {
                    MessageBox.Show("لا يمكن إنهاء المناوبة وهناك طلبات جارية في قائمة الطلبات. الرجاء إتمامها أو إلغائها أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من عدم وجود طلبات معلقة (HeldOrders) محفوظة في النظام
                var heldOrders = await ServiceLocator.HeldOrderService.GetAllHeldOrdersAsync();
                if (heldOrders != null && heldOrders.Any())
                {
                    MessageBox.Show(
                        $"لا يمكن إنهاء المناوبة لوجود ({heldOrders.Count()}) طلبات معلقة محفوظة في النظام!\n\nيرجى استرجاع هذه الطلبات وإتمامها أو إلغاؤها أولاً قبل إغلاق المناوبة لضمان دقة الحسابات والمخزون.",
                        "تنبيه - توجد طلبات معلقة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // سيناريو التوريد البنكي عند إقفال المناوبة (الترحيل لحساب البنك)
                if (shiftManagementProberty.IsDepositOnEndShift)
                {
                    decimal depositAmount = shiftManagementProberty.EndShiftDepositAmount;
                    if (depositAmount <= 0)
                    {
                        MessageBox.Show("يرجى إدخال مبلغ صحيح للتوريد البنكي", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (depositAmount > CurrentCashInBox)
                    {
                        MessageBox.Show($"مبلغ التوريد ({depositAmount:N0} ريال) أكبر من المبلغ المتوفر في الصندوق ({CurrentCashInBox:N0} ريال)", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var bank = string.IsNullOrWhiteSpace(shiftManagementProberty.EndShiftBankName) ? "حاسب كريمي" : shiftManagementProberty.EndShiftBankName.Trim();
                    POSSystem.WPF.Helpers.FinancialChannelHelper.AddCustomWallet(bank);
                    var refNum = string.IsNullOrWhiteSpace(shiftManagementProberty.EndShiftDepositReference)
                        ? $"SH-{CurrentShift.ShiftNumber ?? CurrentShift.Id.ToString()}"
                        : shiftManagementProberty.EndShiftDepositReference.Trim();
                    var depositor = string.IsNullOrWhiteSpace(shiftManagementProberty.EndShiftDepositorName) 
                        ? (CurrentShift.User?.FullName ?? "الكاشير") 
                        : shiftManagementProberty.EndShiftDepositorName.Trim();
                    decimal remainingInBox = Math.Max(0, CurrentCashInBox - depositAmount);

                    if (new POSSystem.WPF.Views.Popups.ShiftPasswordPopup().ShowDialog() != true) return;

                    var confirmMsg = $"تأكيد إنهاء المناوبة رقم ({CurrentShift.ShiftNumber}) وتوريد الكاش:\n\n" +
                                     $"• الحساب المودع فيه: {bank}\n" +
                                     $"• رقم إشعار الإيداع الورقي: {refNum}\n" +
                                     $"• اسم المودع: {depositor}\n" +
                                     $"• مبلغ التوريد للبنك: {depositAmount:N0} ريال\n" +
                                     $"• المبلغ المتبقي في الدرج: {remainingInBox:N0} ريال\n\n" +
                                     $"هل أنت متأكد من ترحيل التوريد وإنهاء المناوبة؟";

                    if (MessageBox.Show(confirmMsg, "تأكيد التوريد وإنهاء المناوبة", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;

                    // 1. تسجيل حركة سند صرف وتوريد بنكي في حركات الصندوق
                    await _cashBoxTransactionService.CreateTransactionAsync(new CashBoxTransaction
                    {
                        Name = $"توريد بنكي عند الإغلاق: {bank}",
                        ShiftId = CurrentShift.Id,
                        UserId = _userId,
                        Type = CashBoxTransactionType.BankDeposit,
                        Amount = depositAmount,
                        Description = $"توريد كاش المناوبة إلى {bank} | إشعار: {refNum}",
                        IsPersonal = false,
                        Notes = $"[إيداع بنكي: {bank} | رقم الإشعار: {refNum} | المودع: {depositor}]",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        TransactionDate = DateTime.Now
                    });

                    // 2. تسجيل سند إيداع بنكي في جدول الإيداعات البنكية للنظام
                    try
                    {
                        if (_bankDepositService != null)
                        {
                            await _bankDepositService.CreateDepositAsync(new BankDeposit
                            {
                                DepositDate = DateTime.Now,
                                Amount = depositAmount,
                                DepositorName = depositor,
                                Source = $"صندوق الكاشير - مناوبة #{CurrentShift.ShiftNumber}",
                                ReferenceNumber = refNum,
                                Status = "Approved",
                                CreatedBy = depositor,
                                Notes = $"توريد كاش عند إقفال مناوبة #{CurrentShift.ShiftNumber} إلى {bank}"
                            });
                        }
                    }
                    catch { }

                    // 3. طباعة إيصال إغلاق المناوبة شاملاً التوريد
                    try
                    {
                        var user = await _userService.GetUserByIdAsync(_userId);
                        if (user != null)
                        {
                            POSSystem.WPF.StaticFunction.GloublStatic.PrintShiftEndReceipt(await _reportServiceForShift.GetUserShiftReportAsync(user), user);
                        }
                    }
                    catch { }

                    // 4. إغلاق المناوبة مع تثبيت المبلغ المتبقي الفعلي
                    await _shiftService.EndShiftAsync(CurrentShift.Id, remainingInBox);
                    POSSystem.WPF.Business.ServiceLocator.NotifyCashBoxChanged();
                    await LoadCurrentShiftAsync();
                    OnPropertyChanged(nameof(ShowStartShiftSection));
                    OnPropertyChanged(nameof(ShowActiveShiftSection));
                    OnPropertyChanged(nameof(ActionButtonText));
                    OnPropertyChanged(nameof(ShiftStatusText));
                    LoadCashBoxBalance();

                    MessageBox.Show($"تم إنهاء المناوبة بنجاح وتوريد مبلغ {depositAmount:N0} ريال إلى {bank} (إشعار: {refNum}).", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // الإغلاق الاعتيادي بدون توريد بنكي
                if (shiftManagementProberty.EndingCash < 0) { MessageBox.Show("المبلغ النهائي يجب أن يكون أكبر من أو يساوي الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                if (shiftManagementProberty.EndingCash == 0)
                {
                    shiftManagementProberty.EndingCash = CurrentCashInBox;
                }
                else if (shiftManagementProberty.EndingCash > CurrentCashInBox)
                {
                    decimal surplus = shiftManagementProberty.EndingCash - CurrentCashInBox;
                    var confirmSurplus = MessageBox.Show(
                        $"المبلغ المدخل ({shiftManagementProberty.EndingCash:N0} ريال) أكبر من الرصيد الدفتري المسجل في الصندوق ({CurrentCashInBox:N0} ريال) بزيادة قدرها ({surplus:N0} ريال).\n\n" +
                        "هل تريد المتابعة واعتماد تسجيل هذا الفائض كـ (زيادة نقدية) في تقرير إغلاق المناوبة؟",
                        "تأكيد وجود زيادة نقدية في الصندوق",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirmSurplus != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                if (new POSSystem.WPF.Views.Popups.ShiftPasswordPopup().ShowDialog() != true) return;
                if (MessageBox.Show($"هل أنت متأكد من إنهاء المناوبة {CurrentShift.ShiftNumber}؟\nالمبلغ النهائي في الصندوق: {shiftManagementProberty.EndingCash:N0} ريال", "تأكيد إنهاء المناوبة", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try { var user = await _userService.GetUserByIdAsync(_userId); if (user != null) POSSystem.WPF.StaticFunction.GloublStatic.PrintShiftEndReceipt(await _reportServiceForShift.GetUserShiftReportAsync(user), user); } catch { }
                    await _shiftService.EndShiftAsync(CurrentShift.Id, shiftManagementProberty.EndingCash);
                    POSSystem.WPF.Business.ServiceLocator.NotifyCashBoxChanged();
                    await LoadCurrentShiftAsync();
                    OnPropertyChanged(nameof(ShowStartShiftSection)); OnPropertyChanged(nameof(ShowActiveShiftSection)); OnPropertyChanged(nameof(ActionButtonText)); OnPropertyChanged(nameof(ShiftStatusText)); LoadCashBoxBalance();
                    MessageBox.Show("تم إنهاء المناوبة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في إنهاء المناوبة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async void LoadCashBoxBalance() => await LoadCashBoxBalanceAsync();
        public async Task LoadCurrentShiftAfterAction() { await LoadCurrentShiftAsync(); LoadCashBoxBalance(); }
        public event EventHandler? CloseRequested;

        public async Task WithdrawCashAsync()
        {
            try
            {
                if (CurrentShift == null || !CurrentShift.IsActive) { MessageBox.Show("يجب أن تكون هناك مناوبة نشطة لسحب المبلغ من الصندوق", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!decimal.TryParse(shiftManagementProberty.WithdrawAmount.ToString(), out decimal amount) || amount <= 0) { MessageBox.Show("يجب إدخال مبلغ صحيح أكبر من الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                var balance = await _cashBoxTransactionService.GetCashBoxBalanceAsync(CurrentShift.Id);
                if (amount > balance) { MessageBox.Show($"المبلغ المتوفر ({balance:N0} ريال) أقل من المطلوب ({amount:N0} ريال)", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (string.IsNullOrWhiteSpace(shiftManagementProberty.WithdrawDescription)) { MessageBox.Show("يجب إدخال وصف أو سبب السحب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                if (shiftManagementProberty.IsPersonalWithdrawal && (shiftManagementProberty.SelectedEmployee == null || new POSSystem.WPF.Views.Popups.ShiftPasswordPopup().ShowDialog() != true)) return;

                if (MessageBox.Show($"هل أنت متأكد من سحب {amount:N0} ريال ك{(shiftManagementProberty.IsPersonalWithdrawal ? "دين شخصي" : "مصروف مطعم")}؟", "تأكيد السحب", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (shiftManagementProberty.IsPersonalWithdrawal)
                    {
                        var adv = new EmployeeAdvance
                        {
                            EmployeeId = shiftManagementProberty.SelectedEmployee!.Id,
                            Amount = amount,
                            AdvanceDate = DateTime.Now,
                            RepaymentMethod = AdvanceRepaymentMethod.MonthlyDeduction,
                            PaymentMethod = PaymentMethod.Cash,
                            TargetShiftId = CurrentShift.Id,
                            Notes = shiftManagementProberty.WithdrawDescription,
                            IsRepaid = false,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        await _employeeService.CreateAdvanceAsync(adv);
                    }
                    else
                    {
                        await _cashBoxTransactionService.CreateTransactionAsync(new CashBoxTransaction
                        {
                            Name = "سحب مطعم",
                            ShiftId = CurrentShift.Id,
                            UserId = _userId,
                            Type = CashBoxTransactionType.Expense,
                            Amount = amount,
                            Description = shiftManagementProberty.WithdrawDescription,
                            IsPersonal = false,
                            Notes = shiftManagementProberty.WithdrawDescription,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            TransactionDate = DateTime.Now
                        });
                    }
                    shiftManagementProberty.WithdrawAmount = 0; shiftManagementProberty.WithdrawDescription = ""; await LoadCashBoxBalanceAsync();
                    MessageBox.Show($"تم السحب بنجاح\nالمبلغ الحالي: {await _cashBoxTransactionService.GetCashBoxBalanceAsync(CurrentShift.Id):N0} ريال", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في سحب المبلغ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task LoadCashBoxBalanceAsync()
        {
            try 
            { 
                CurrentCashInBox = CurrentShift != null ? await _cashBoxTransactionService.GetCashBoxBalanceAsync(CurrentShift.Id) : 0; 
            }
            catch 
            { 
                CurrentCashInBox = CurrentShift?.StartingCash ?? 0; 
            }
            OnPropertyChanged(nameof(CurrentCashInBox));

            // ضبط القيم الافتراضية للتوريد البنكي عند الإغلاق
            if (shiftManagementProberty.EndShiftDepositAmount == 0 || shiftManagementProberty.EndShiftDepositAmount > CurrentCashInBox)
            {
                shiftManagementProberty.EndShiftDepositAmount = Math.Max(0, CurrentCashInBox);
            }

            if (string.IsNullOrWhiteSpace(shiftManagementProberty.EndShiftDepositorName))
            {
                try
                {
                    var user = await _userService.GetUserByIdAsync(_userId);
                    if (user != null)
                    {
                        shiftManagementProberty.EndShiftDepositorName = user.FullName ?? user.Name;
                    }
                }
                catch { }
            }

            RecalculateEndingCash();
        }

        public void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
