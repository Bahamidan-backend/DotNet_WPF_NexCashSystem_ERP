using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Services;
using POSSystem.WPF.Services.Logging;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;
using System.Windows.Controls;

namespace POSSystem.WPF.Business.Services
{
    public class ShiftService : IShiftService
    {
        private readonly IUnitOfWork _unitOfWork;
        

        public event EventHandler ShiftStarted;
        public event EventHandler ShiftEnded;

        public ShiftService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
          
        }







        public async Task<Shift?> GetActiveShiftByUserIdAsync(int userId)
        {
            return await _unitOfWork.Shifts.GetActiveShiftByUserIdAsync(userId);
        }

        public async Task<Shift> StartShiftAsync(int userId, ShiftType type, decimal startingCash, string? notes = null)
        {
            Shift shift = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var context = _unitOfWork.GetContext();

                // التحقق الذري داخل المعاملة لمنع إنشاء أكثر من مناوبة نشطة لنفس المستخدم
                var activeShift = await context.Shifts
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

                if (activeShift != null)
                {
                    throw new InvalidOperationException("لديك مناوبة نشطة بالفعل. يجب إنهاء المناوبة الحالية قبل بدء مناوبة جديدة.");
                }

                var shiftNumber = await _unitOfWork.Shifts.GenerateShiftNumberAsync();
                var now = DateTime.Now;
                shift = new Shift
                {
                    ShiftNumber = shiftNumber,
                    Name = shiftNumber,
                    Description = $"مناوبة {type switch { ShiftType.Morning => "صباحية", ShiftType.Evening => "مسائية", _ => "غير محدد" }}",
                    UserId = userId,
                    Type = type,
                    StartingCash = startingCash,
                    Notes = notes,
                    StartTime = now,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _unitOfWork.Shifts.AddAsync(shift);
                await _unitOfWork.SaveChangesAsync();
            });

            ShiftStarted?.Invoke(this, EventArgs.Empty);

            return shift;
        }

        public async Task<Shift> EndShiftAsync(int shiftId, decimal endingCash)
        {
            Shift shift = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var context = _unitOfWork.GetContext();
                shift = await context.Shifts.FirstOrDefaultAsync(s => s.Id == shiftId);
                if (shift == null)
                    throw new ArgumentException("المناوبة غير موجودة");

                if (!shift.IsActive)
                    throw new InvalidOperationException("المناوبة منتهية بالفعل");

                bool hasHeldOrders = await context.HeldOrders
                    .AsNoTracking()
                    .AnyAsync(h => h.ShiftId == shiftId);
                if (hasHeldOrders)
                    throw new InvalidOperationException("لا يمكن إغلاق المناوبة لوجود طلبات معلقة. يرجى إكمالها أو حذفها أولاً.");

                var now = DateTime.Now;

                // 1. حساب النقدية الدفترية المتوقعة من حركات الصندوق للمناوبة
                var shiftTransactions = await context.CashBoxTransactions
                    .Where(t => t.ShiftId == shiftId)
                    .Select(t => new { t.Type, t.Amount })
                    .ToListAsync();
                var netTransactions = shiftTransactions.Sum(t => t.Type == CashBoxTransactionType.Income ? t.Amount : -t.Amount);
                decimal expectedCash = shift.StartingCash + netTransactions;

                var variance = endingCash - expectedCash;
                var varianceText = variance < 0 ? $"عجز ({Math.Abs(variance):N0} ريال)" : (variance > 0 ? $"زيادة ({variance:N0} ريال)" : "مطابق تماماً");
                var auditNote = $"[إغلاق المناوبة: النقدية الفعلية={endingCash:N0} | النقدية الدفترية={expectedCash:N0} | الفارق={variance:N0} ({varianceText})]";
                shift.Notes = string.IsNullOrWhiteSpace(shift.Notes) ? auditNote : $"{shift.Notes}\n{auditNote}";

                // 2. تسجيل الفارق كحركة تسوية صريحة في الصندوق لتوثيق العجز أو الزيادة محاسبياً
                if (Math.Abs(variance) >= 0.01m)
                {
                    if (variance < 0)
                    {
                        // تسوية عجز نقدي
                        var shortageTx = new CashBoxTransaction
                        {
                            Name = $"تسوية عجز مناوبة #{shift.ShiftNumber ?? shift.Id.ToString()}",
                            ShiftId = shift.Id,
                            UserId = shift.UserId,
                            Type = CashBoxTransactionType.Expense,
                            Amount = Math.Abs(variance),
                            Description = $"تسوية عجز نقدي عند مطابقة إغلاق المناوبة #{shift.ShiftNumber} (الفعلي: {endingCash:N0}، الدفتري: {expectedCash:N0})",
                            TransactionDate = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Notes = $"[تسوية عجز مناوبة] فارق عجز: {Math.Abs(variance):N0} ريال"
                        };
                        await context.CashBoxTransactions.AddAsync(shortageTx);
                    }
                    else
                    {
                        // تسوية زيادة نقدية
                        var surplusTx = new CashBoxTransaction
                        {
                            Name = $"تسوية زيادة مناوبة #{shift.ShiftNumber ?? shift.Id.ToString()}",
                            ShiftId = shift.Id,
                            UserId = shift.UserId,
                            Type = CashBoxTransactionType.Income,
                            Amount = variance,
                            Description = $"تسوية فائض نقدي عند مطابقة إغلاق المناوبة #{shift.ShiftNumber} (الفعلي: {endingCash:N0}، الدفتري: {expectedCash:N0})",
                            TransactionDate = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Notes = $"[تسوية زيادة مناوبة] فارق زيادة: {variance:N0} ريال"
                        };
                        await context.CashBoxTransactions.AddAsync(surplusTx);
                    }
                }

                shift.EndingCash = endingCash;
                shift.EndTime = now;
                shift.IsActive = false;
                shift.UpdatedAt = now;

                _unitOfWork.Shifts.Update(shift);
                await _unitOfWork.SaveChangesAsync();
            });

            // Clear entity tracking to ensure fresh data on next query
            _unitOfWork.ClearEntityTracking();

            ShiftEnded?.Invoke(this, EventArgs.Empty);

            // Automated Compressed Database Backup on Shift Close
            TriggerAutomatedShiftBackup(shift);

            return shift;
        }

        private void TriggerAutomatedShiftBackup(Shift shift)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var settings = SettingsService.LoadSettings();
                    if (!settings.AutoBackup)
                    {
                        AppLogger.LogInfo($"Automated shift backup skipped for shift #{shift.ShiftNumber ?? shift.Id.ToString()} (AutoBackup is disabled).");
                        return;
                    }

                    var backupService = App.ServiceProvider?.GetService(typeof(IBackupService)) as IBackupService;
                    if (backupService == null)
                    {
                        AppLogger.LogWarning("BackupService is not registered in service provider.");
                        return;
                    }

                    var result = await backupService.CreateBackupAsync(
                        reason: $"إغلاق وردية #{shift.ShiftNumber ?? shift.Id.ToString()}",
                        shiftId: shift.Id,
                        shiftNumber: shift.ShiftNumber);

                    if (result.IsSuccess)
                    {
                        AppLogger.LogInfo($"Automated shift backup created successfully: {result.FilePath} ({result.FileSizeText})");
                    }
                    else
                    {
                        AppLogger.LogWarning($"Automated shift backup failed: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Error during automated shift backup for shift #{shift.Id}", ex);
                }
            });
        }

        public async Task<bool> CanUserWorkAsync(int userId)
        {
            var activeShift = await GetActiveShiftByUserIdAsync(userId);
            return activeShift != null;
        }

        public async Task<Shift?> GetCurrentShiftAsync(int userId)
        {
            return await GetActiveShiftByUserIdAsync(userId);
        }



        public async Task<Shift?> LoadCurrentOrLastShiftAsync(User _currentUser)
        {
            var currentShift = await GetActiveShiftByUserIdAsync(_currentUser.Id);
            if (currentShift != null) return currentShift;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _unitOfWork.GetContext().Shifts
                .AsNoTracking()
                .Where(s => s.UserId == _currentUser.Id && s.StartTime >= today && s.StartTime < tomorrow)
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<Shift?> GetLastClosedShiftAsync()
        {
            return await _unitOfWork.GetContext().Shifts
                .AsNoTracking()
                .Where(s => !s.IsActive)
                .OrderByDescending(s => s.EndTime ?? s.StartTime)
                .FirstOrDefaultAsync();
        }

        public async Task LoadCashBoxTransactionsByShiftAsync(
            IEnumerable<CashBoxTransaction> transactions, 
            ObservableCollection<CashBoxTransaction> _cashBoxTransactions, 
            ObservableCollection<CashBoxTransactionDetail> _transactionDetails)
        {
            var usersDict = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.Id, u => u.Name);

            foreach (var transaction in transactions)
            {
                _cashBoxTransactions.Add(transaction);

                var cashierName = "غير معروف";
                if (transaction.User != null && !string.IsNullOrWhiteSpace(transaction.User.Name))
                {
                    cashierName = transaction.User.Name;
                }
                else if (transaction.Shift != null && transaction.Shift.User != null && !string.IsNullOrWhiteSpace(transaction.Shift.User.Name))
                {
                    cashierName = transaction.Shift.User.Name;
                }
                else if (transaction.UserId.HasValue && usersDict.TryGetValue(transaction.UserId.Value, out var name))
                {
                    cashierName = name;
                }

                _transactionDetails.Add(new CashBoxTransactionDetail
                {
                    TransactionId = transaction.Id,
                    Date = transaction.TransactionDate,
                    Time = transaction.TransactionDate.ToString("HH:mm:ss"),
                    CashierName = cashierName,
                    Type = transaction.TypeText ?? "غير محدد",
                    Amount = transaction.Amount,
                    Description = !string.IsNullOrWhiteSpace(transaction.Description) ? transaction.Description : transaction.Name ?? "بدون وصف",
                    IsPersonal = transaction.IsPersonal,
                    ExpenseType = transaction.IsPersonal ? "شخصي (ينقص من الراتب)" : (transaction.Type == CashBoxTransactionType.BankDeposit ? "إيداع في البنك" : "للمطعم"),
                    Notes = transaction.Notes ?? ""
                });
            }
        }














           public async Task<IEnumerable<Order>> GetShiftTransactionsAsync(Shift _currentShift, User _currentUser, IOrderService _orderService)
           {
            var shiftStart = _currentShift.StartTime.Date;
            var shiftEnd = _currentShift.EndTime?.Date ?? DateTime.Today.AddDays(1);

            var allOrders = await _orderService.GetOrdersByDateRangeAsync(shiftStart, shiftEnd.AddDays(1));
            var shiftOrders = allOrders
                .Where(o => o.UserId == _currentUser.Id &&
                            o.CreatedAt >= _currentShift.StartTime &&
                            (_currentShift.EndTime == null || o.CreatedAt <= _currentShift.EndTime))
                .ToList();



            var order= shiftOrders
                .AsEnumerable();
            return order;

           

        }













        }


    }





