using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using System.Windows.Controls;

namespace POSSystem.WPF.Business.Services
{
    public class CashBoxTransactionService : ICashBoxTransactionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CashBoxTransactionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CashBoxTransaction> CreateTransactionAsync(CashBoxTransaction transaction)
        {
            if (transaction.Amount <= 0)
                throw new ArgumentException("المبلغ يجب أن يكون أكبر من الصفر");

            transaction.CreatedAt = DateTime.Now;
            transaction.UpdatedAt = DateTime.Now;
            transaction.TransactionDate = DateTime.Now;

            await _unitOfWork.CashBoxTransactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            return transaction;
        }

        public async Task<IEnumerable<CashBoxTransaction>> GetTransactionsByShiftIdAsync(int shiftId)
        {
            return await _unitOfWork.CashBoxTransactions.GetByShiftIdAsync(shiftId);
        }

        public async Task<decimal> GetCashBoxBalanceAsync(int shiftId)
        {
            var context = _unitOfWork.GetContext();
            var shift = await context.Shifts
                .AsNoTracking()
                .Where(s => s.Id == shiftId)
                .Select(s => new { s.StartingCash, s.IsActive })
                .FirstOrDefaultAsync();

            if (shift == null)
                return 0m;

            // FIX ISSUE-FIN-001: GetCashBoxBalanceAsync must be a pure, side-effect-free read query.
            // Never delete transactions or clear ChangeTracker during a balance read, and exclude post-close settlement entries when computing expected cash.
            var netTransactions = await context.CashBoxTransactions
                .AsNoTracking()
                .Where(t => t.ShiftId == shiftId &&
                            (t.Notes == null || (!t.Notes.Contains("[تسوية عجز مناوبة]") && !t.Notes.Contains("[تسوية زيادة مناوبة]"))))
                .SumAsync(t => t.Type == CashBoxTransactionType.Income ? (double)t.Amount : -(double)t.Amount);

            return shift.StartingCash + (decimal)netTransactions;
        }

        public async Task<CashBoxTransaction> CreateAdvanceAsync(int shiftId, int userId, decimal amount, string description)
        {
            if (amount <= 0)
                throw new ArgumentException("مبلغ السلفة يجب أن يكون أكبر من الصفر.", nameof(amount));

            decimal currentBalance = await GetCashBoxBalanceAsync(shiftId);
            if (currentBalance < amount)
                throw new InvalidOperationException($"رصيد الصندوق الحالي ({currentBalance:N0}) غير كافٍ لصرف السلفة ({amount:N0}).");

            var now = DateTime.Now;
            var voucherCode = $"PV-ADV-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
            var voucher = new ReceiptVoucher
            {
                VoucherNumber = voucherCode,
                Name = $"سند صرف سلفة موظف - {voucherCode}",
                Amount = amount,
                Type = VoucherType.Payment,
                PaymentMethod = PaymentMethod.Cash,
                CustomerName = $"موظف #{userId}",
                Notes = description ?? "سلفة موظف",
                UserId = userId,
                VoucherDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

            var advance = new EmployeeAdvance
            {
                EmployeeId = userId,
                Amount = amount,
                AdvanceDate = now,
                PaymentMethod = PaymentMethod.Cash,
                RepaymentMethod = AdvanceRepaymentMethod.MonthlyDeduction,
                IsRepaid = false,
                Notes = description,
                CreatedAt = now,
                UpdatedAt = now
            };

            var transaction = new CashBoxTransaction
            {
                ShiftId = shiftId,
                UserId = userId,
                Type = CashBoxTransactionType.Advance,
                Amount = amount,
                Name = $"سلفة موظف #{userId}",
                Description = description,
                EmployeeAdvance = advance,
                CreatedAt = now,
                UpdatedAt = now,
                TransactionDate = now
            };

            await _unitOfWork.CashBoxTransactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            return transaction;
        }

        public async Task<CashBoxTransaction> CreateExpenseAsync(int shiftId, decimal amount, string description)
        {
            var transaction = new CashBoxTransaction
            {
                ShiftId = shiftId,
                Type = CashBoxTransactionType.Expense,
                Amount = amount,
                Description = description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                TransactionDate = DateTime.Now
            };

            await _unitOfWork.CashBoxTransactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            return transaction;
        }

        public async Task<IEnumerable<CashBoxTransaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.CashBoxTransactions.GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<int> CleanupOrphanedCashBoxTransactionsAsync(int? shiftId = null)
        {
            var context = _unitOfWork.GetContext();

            // استعلام الحركات الخاصة بالمناوبات النشطة (أو المناوبة المحددة)
            var shiftsQuery = context.Shifts.Where(s => s.IsActive);
            if (shiftId.HasValue && shiftId.Value > 0)
            {
                shiftsQuery = context.Shifts.Where(s => s.Id == shiftId.Value);
            }
            var activeShiftIds = await shiftsQuery.Select(s => s.Id).ToListAsync();
            if (!activeShiftIds.Any()) return 0;

            var candidates = await context.CashBoxTransactions
                .Where(t => activeShiftIds.Contains(t.ShiftId))
                .ToListAsync();

            var orphaned = new List<CashBoxTransaction>();

            foreach (var t in candidates)
            {
                // 1. حركات صرف السلف اليتيمة فقط (لا تحذف إيرادات تسديد السلف إطلاقاً)
                if (t.Type == CashBoxTransactionType.Advance)
                {
                    if (t.EmployeeAdvanceId.HasValue)
                    {
                        bool advanceExists = await context.EmployeeAdvances.AnyAsync(a => a.Id == t.EmployeeAdvanceId.Value);
                        if (!advanceExists)
                        {
                            orphaned.Add(t);
                            continue;
                        }
                    }
                    else
                    {
                        // سلفة بدون معرف صريح: تحقق من وجود سند صرف أو سجل سلفة مطابق
                        bool voucherExists = !string.IsNullOrWhiteSpace(t.Notes) &&
                            await context.ReceiptVouchers.AnyAsync(v => v.Type == VoucherType.Payment &&
                                (v.Notes.Contains(t.Notes) || (v.VoucherNumber != null && t.Notes.Contains(v.VoucherNumber))));

                        bool advanceMatches = t.UserId.HasValue &&
                            await context.EmployeeAdvances.AnyAsync(a => a.EmployeeId == t.UserId.Value && a.Amount == t.Amount);

                        if (!voucherExists && !advanceMatches)
                        {
                            orphaned.Add(t);
                            continue;
                        }
                    }
                }

                // 2. حركات السحب المكررة الناتجة عن الخلل السابق في شاشة المناوبات (سحب شخصي مكرر)
                if (t.Type == CashBoxTransactionType.Debt && t.Name != null && t.Name.Contains("سحب شخصي للموظف"))
                {
                    bool hasMatchingAdvanceTx = candidates.Any(other =>
                        other.Id != t.Id &&
                        other.ShiftId == t.ShiftId &&
                        other.Amount == t.Amount &&
                        other.UserId == t.UserId &&
                        other.Type == CashBoxTransactionType.Advance &&
                        Math.Abs((other.CreatedAt - t.CreatedAt).TotalMinutes) < 5);

                    if (hasMatchingAdvanceTx)
                    {
                        orphaned.Add(t);
                        continue;
                    }
                }

                // 3. حركات المصروفات التي حُذفت سنداتها وسجلاتها
                if (t.Type == CashBoxTransactionType.Expense && !string.IsNullOrWhiteSpace(t.Notes) && t.Notes.Contains("[مصروف #"))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(t.Notes, @"\[مصروف #(\d+)\]");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int expId))
                    {
                        bool expExists = await context.Expenses.AnyAsync(e => e.Id == expId);
                        if (!expExists)
                        {
                            orphaned.Add(t);
                            continue;
                        }
                    }
                }
            }

            if (orphaned.Count > 0)
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    context.CashBoxTransactions.RemoveRange(orphaned);
                    await _unitOfWork.SaveChangesAsync();
                });
                _unitOfWork.ClearEntityTracking();
            }

            return orphaned.Count;
        }






      
    }




}



