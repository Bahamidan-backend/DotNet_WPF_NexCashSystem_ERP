using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.WPF.Business.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialChannelService _financialChannelService;

        public ExpenseService(
            IUnitOfWork unitOfWork,
            IFinancialChannelService? financialChannelService = null)
        {
            _unitOfWork = unitOfWork;
            _financialChannelService = financialChannelService ?? ServiceLocator.FinancialChannelService;
        }

        public async Task<IEnumerable<Expense>> GetAllExpensesAsync()
        {
            return await _unitOfWork.Expenses.GetAllAsync();
        }

        public async Task<IEnumerable<Expense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Expenses.GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<Expense>> GetByCategoryAsync(string category)
        {
            return await _unitOfWork.Expenses.GetByCategoryAsync(category);
        }

        public async Task<IEnumerable<Expense>> GetByTypeAsync(ExpenseType type)
        {
            return await _unitOfWork.Expenses.GetByTypeAsync(type);
        }

        public async Task<Expense?> GetExpenseByIdAsync(int id)
        {
            return await _unitOfWork.Expenses.GetByIdAsync(id);
        }

        public async Task<Expense> CreateExpenseAsync(Expense expense)
        {
            if (string.IsNullOrWhiteSpace(expense.Name))
                throw new ArgumentException("اسم المصروف مطلوب");

            if (expense.Amount <= 0)
                throw new ArgumentException("المبلغ يجب أن يكون أكبر من الصفر");

            if (string.IsNullOrWhiteSpace(expense.Category))
                throw new ArgumentException("فئة المصروف مطلوبة");

            var now = DateTime.Now;
            expense.CreatedAt = now;
            expense.UpdatedAt = now;
            expense.ExpenseDate = expense.ExpenseDate == default ? now : expense.ExpenseDate;

            // التحقق الصارم من كفاية الرصيد لجميع جهات الصرف (صندوق الكاشير، المحافظ الإلكترونية، الحسابات البنكية)
            var expenseChannel = FinancialChannelHelper.ExtractChannel(expense.PaymentMethod, expense.Notes);
            await _financialChannelService.ValidateSufficientBalanceAsync(expenseChannel, expense.Amount);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // التحقق المزدوج داخل المعاملة قبل الخصم
                await _financialChannelService.ValidateSufficientBalanceAsync(expenseChannel, expense.Amount);

                await _unitOfWork.Expenses.AddAsync(expense);
                await _unitOfWork.SaveChangesAsync();

                // 1. Create Official Payment Voucher (سند صرف رسمي)
                var voucherNumber = $"PV-EXP-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}";
                var voucherNotes = $"[مصروف #{expense.Id}] مصروف: {expense.Name} | الفئة: {expense.Category} | النوع: {expense.TypeText} | طريقة الدفع: {expense.PaymentMethodText}" +
                                   (!string.IsNullOrWhiteSpace(expense.Notes) ? $" | {expense.Notes}" : "");
                if (voucherNotes.Length > 2000) voucherNotes = voucherNotes.Substring(0, 2000);

                var voucher = new ReceiptVoucher
                {
                    VoucherNumber = voucherNumber,
                    Name = $"سند صرف مصروف #{expense.Id} - {expense.Name}",
                    Type = VoucherType.Payment,
                    PaymentMethod = expense.PaymentMethod,
                    Amount = expense.Amount,
                    CustomerName = !string.IsNullOrWhiteSpace(expense.SupplierName)
                        ? expense.SupplierName
                        : (!string.IsNullOrWhiteSpace(expense.Category) ? $"مصروفات: {expense.Category}" : "مصروفات عامة"),
                    VoucherDate = expense.ExpenseDate,
                    Notes = voucherNotes,
                    UserId = expense.UserId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

                string voucherTag = $"[سند صرف: {voucherNumber}]";
                expense.InvoiceNumber = voucherNumber;
                expense.Notes = string.IsNullOrWhiteSpace(expense.Notes)
                    ? voucherTag
                    : (!expense.Notes.Contains("[سند صرف:") ? $"{expense.Notes} {voucherTag}" : expense.Notes);

                // 2. Link to CashBoxTransaction ONLY if payment was made in cash from the drawer
                if (expense.PaymentMethod == PaymentMethod.Cash)
                {
                    var activeShift = await _unitOfWork.GetContext().Shifts
                        .AsNoTracking()
                        .Where(s => s.IsActive)
                        .OrderByDescending(s => expense.UserId.HasValue && s.UserId == expense.UserId.Value)
                        .FirstOrDefaultAsync();

                    if (activeShift != null)
                    {
                        var cashTransaction = new CashBoxTransaction
                        {
                            Name = $"مصروف #{expense.Id} - {expense.Name}",
                            ShiftId = activeShift.Id,
                            UserId = expense.UserId ?? activeShift.UserId,
                            Type = CashBoxTransactionType.Expense,
                            Amount = expense.Amount,
                            Description = $"[مصروف #{expense.Id}] مصروف {expense.Category}: {expense.Name}" + (!string.IsNullOrWhiteSpace(expense.Notes) ? $" ({expense.Notes})" : ""),
                            TransactionDate = expense.ExpenseDate,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Notes = $"[مصروف #{expense.Id}] {expense.Notes}"
                        };
                        await _unitOfWork.CashBoxTransactions.AddAsync(cashTransaction);

                        string shiftTag = $"[وردية #{activeShift.Id}]";
                        if (!expense.Notes.Contains("[وردية #"))
                        {
                            expense.Notes = $"{expense.Notes} {shiftTag}";
                        }
                    }
                }

                _unitOfWork.Expenses.Update(expense);
                await _unitOfWork.SaveChangesAsync();
            });

            return expense;
        }

        public async Task<Expense> UpdateExpenseAsync(Expense expense)
        {
            if (string.IsNullOrWhiteSpace(expense.Name))
                throw new ArgumentException("اسم المصروف مطلوب");

            if (expense.Amount <= 0)
                throw new ArgumentException("المبلغ يجب أن يكون أكبر من الصفر");

            var context = _unitOfWork.GetContext();
            var existing = await context.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expense.Id);
            if (existing == null)
                throw new ArgumentException("المصروف غير موجود");

            string expenseTag = $"[مصروف #{expense.Id}]";
            string expenseNameTag = $"مصروف #{expense.Id}";

            // البحث عن حركات الصندوق المرتبطة بالمصروف
            var cashTransactions = await context.CashBoxTransactions
                .Include(t => t.Shift)
                .Where(t => t.Type == CashBoxTransactionType.Expense &&
                    (t.Description.Contains(expenseTag) ||
                     t.Name.Contains(expenseNameTag) ||
                     (!string.IsNullOrWhiteSpace(t.Notes) && t.Notes.Contains(expenseTag))))
                .ToListAsync();

            // فحص حالة المناوبة/الوردية: منع التعديل إذا كانت المناوبة مغلقة
            foreach (var tx in cashTransactions)
            {
                var shift = tx.Shift ?? await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == tx.ShiftId);
                if (shift != null && (!shift.IsActive || shift.EndTime.HasValue))
                {
                    throw new InvalidOperationException($"لا يمكن تعديل المصروف لأن الوردية المرتبطة به (مناوبة #{shift.Id}) مغلقة ومرحلة بالفعل.");
                }
            }

            var oldMethod = existing.PaymentMethod;
            var newMethod = expense.PaymentMethod;

            if (newMethod != PaymentMethod.Cash)
            {
                var newChannel = FinancialChannelHelper.ExtractChannel(expense.PaymentMethod, expense.Notes);
                var existingVoucher = await context.ReceiptVouchers.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Type == VoucherType.Payment &&
                        ((v.Notes != null && v.Notes.Contains(expenseTag)) ||
                         (!string.IsNullOrWhiteSpace(existing.InvoiceNumber) && v.VoucherNumber == existing.InvoiceNumber)));
                await _financialChannelService.ValidateSufficientBalanceAsync(newChannel, expense.Amount, null, existingVoucher?.Id);
            }

            // إذا أصبحت طريقة الدفع نقداً، نتحقق من وجود مناوبة نشطة وكفاية رصيد درج الكاشير
            if (newMethod == PaymentMethod.Cash)
            {
                var activeShift = await context.Shifts
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => expense.UserId.HasValue && s.UserId == expense.UserId.Value)
                    .FirstOrDefaultAsync();

                if (activeShift == null)
                {
                    throw new InvalidOperationException("لا يمكن اختيار الدفع نقداً لعدم وجود مناوبة نشطة حالياً في درج الكاشير.");
                }

                decimal startingCash = activeShift.StartingCash;
                var existingTxIds = cashTransactions.Select(t => t.Id).ToList();
                var netTransactions = (decimal)(await context.CashBoxTransactions
                    .AsNoTracking()
                    .Where(t => t.ShiftId == activeShift.Id && !existingTxIds.Contains(t.Id))
                    .SumAsync(t => t.Type == CashBoxTransactionType.Income ? (double)t.Amount : -(double)t.Amount));
                decimal currentDrawerBalance = startingCash + netTransactions;

                if (currentDrawerBalance < expense.Amount)
                {
                    throw new InvalidOperationException(
                        $"الرصيد النقدي في الصندوق ({currentDrawerBalance:N0} ريال) غير كافٍ لصرف المصروف بعد التعديل ({expense.Amount:N0} ريال).");
                }
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var now = DateTime.Now;
                expense.UpdatedAt = now;

                // 1. معالجة حركة الصندوق وفقاً لتغير طريقة الدفع
                if (oldMethod == PaymentMethod.Cash && newMethod != PaymentMethod.Cash)
                {
                    // الحالة أ: كان نقداً وتغير إلى غير نقدي (بنك / حوالة / شبكة)
                    // نقوم بحذف حركة الصندوق لأن المبلغ لم يعد خارجاً من درج الكاشير
                    if (cashTransactions.Count > 0)
                    {
                        context.CashBoxTransactions.RemoveRange(cashTransactions);
                    }
                }
                else if (oldMethod != PaymentMethod.Cash && newMethod == PaymentMethod.Cash)
                {
                    // الحالة ب: كان غير نقدي وتغير إلى نقدي
                    // نقوم بإنشاء حركة خروج نقدي في الصندوق للمناوبة النشطة
                    var activeShift = await context.Shifts
                        .AsNoTracking()
                        .Where(s => s.IsActive)
                        .OrderByDescending(s => expense.UserId.HasValue && s.UserId == expense.UserId.Value)
                        .FirstOrDefaultAsync();

                    if (activeShift != null)
                    {
                        var newCashTx = new CashBoxTransaction
                        {
                            Name = $"مصروف #{expense.Id} - {expense.Name}",
                            ShiftId = activeShift.Id,
                            UserId = expense.UserId ?? activeShift.UserId,
                            Type = CashBoxTransactionType.Expense,
                            Amount = expense.Amount,
                            Description = $"[مصروف #{expense.Id}] مصروف {expense.Category}: {expense.Name}" + (!string.IsNullOrWhiteSpace(expense.Notes) ? $" ({expense.Notes})" : ""),
                            TransactionDate = expense.ExpenseDate,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Notes = $"[مصروف #{expense.Id}] {expense.Notes}"
                        };
                        await context.CashBoxTransactions.AddAsync(newCashTx);

                        string shiftTag = $"[وردية #{activeShift.Id}]";
                        if (!expense.Notes.Contains("[وردية #"))
                        {
                            expense.Notes = $"{expense.Notes} {shiftTag}".Trim();
                        }
                    }
                }
                else if (oldMethod == PaymentMethod.Cash && newMethod == PaymentMethod.Cash)
                {
                    // الحالة ج: بقي نقداً، نقوم بتحديث المبلغ والبيان والتاريخ
                    foreach (var ct in cashTransactions)
                    {
                        ct.Amount = expense.Amount;
                        ct.Name = $"مصروف #{expense.Id} - {expense.Name}";
                        ct.Description = $"[مصروف #{expense.Id}] مصروف {expense.Category}: {expense.Name}" + (!string.IsNullOrWhiteSpace(expense.Notes) ? $" ({expense.Notes})" : "");
                        ct.TransactionDate = expense.ExpenseDate;
                        ct.UpdatedAt = now;
                        context.CashBoxTransactions.Update(ct);
                    }
                }

                // 2. تحديث سندات الصرف المرتبطة بالمصروف (ReceiptVouchers)
                var vouchers = await context.ReceiptVouchers
                    .Where(v => v.Type == VoucherType.Payment &&
                        ((v.Notes != null && v.Notes.Contains(expenseTag)) ||
                         (!string.IsNullOrWhiteSpace(expense.InvoiceNumber) && v.VoucherNumber == expense.InvoiceNumber)))
                    .ToListAsync();

                foreach (var v in vouchers)
                {
                    v.Amount = expense.Amount;
                    v.Name = $"سند صرف مصروف #{expense.Id} - {expense.Name}";
                    v.PaymentMethod = expense.PaymentMethod;
                    v.CustomerName = !string.IsNullOrWhiteSpace(expense.SupplierName)
                        ? expense.SupplierName
                        : (!string.IsNullOrWhiteSpace(expense.Category) ? $"مصروفات: {expense.Category}" : "مصروفات عامة");
                    var updatedNotes = $"[مصروف #{expense.Id}] مصروف: {expense.Name} | الفئة: {expense.Category} | النوع: {expense.TypeText} | طريقة الدفع: {expense.PaymentMethodText}" +
                                       (!string.IsNullOrWhiteSpace(expense.Notes) ? $" | {expense.Notes}" : "");
                    if (updatedNotes.Length > 2000) updatedNotes = updatedNotes.Substring(0, 2000);
                    v.Notes = updatedNotes;
                    v.UpdatedAt = now;
                    context.ReceiptVouchers.Update(v);
                }

                _unitOfWork.ClearEntityTracking();
                _unitOfWork.Expenses.Update(expense);
                await _unitOfWork.SaveChangesAsync();
            });

            return expense;
        }

        public async Task DeleteExpenseAsync(int id)
        {
            var context = _unitOfWork.GetContext();
            var expense = await context.Expenses.FirstOrDefaultAsync(e => e.Id == id);
            if (expense == null)
                throw new ArgumentException("المصروف غير موجود");

            // 1. فحص حركات الصندوق وسندات الصرف المرتبطة
            string expenseTag = $"[مصروف #{expense.Id}]";
            string expenseNameTag = $"مصروف #{expense.Id}";

            var cashTransactions = await context.CashBoxTransactions
                .Include(t => t.Shift)
                .Where(t => t.Type == CashBoxTransactionType.Expense &&
                    (t.Description.Contains(expenseTag) ||
                     t.Name.Contains(expenseNameTag) ||
                     (!string.IsNullOrWhiteSpace(t.Notes) && t.Notes.Contains(expenseTag)) ||
                     (t.Amount == expense.Amount && t.TransactionDate.Date == expense.ExpenseDate.Date && (t.Name.Contains(expense.Name) || t.Description.Contains(expense.Name)))))
                .ToListAsync();

            // التحقق من حالة الوردية/المناوبة: منع الحذف إذا كانت الوردية مغلقة
            foreach (var tx in cashTransactions)
            {
                var shift = tx.Shift ?? await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == tx.ShiftId);
                if (shift != null && (!shift.IsActive || shift.EndTime.HasValue))
                {
                    throw new InvalidOperationException($"لا يمكن حذف هذا المصروف لأن الوردية المرتبطة به (مناوبة #{shift.Id}) مغلقة بالفعل.\nتعديل أو حذف المصروفات بعد إغلاق الوردية ممنوع نظامياً ومحاسبياً لضمان سلامة التدقيق المالي ومطابقة الصندوق.");
                }
            }

            // فحص إضافي عبر كود الوردية المسجل في ملاحظات المصروف
            if (expense.PaymentMethod == PaymentMethod.Cash && !string.IsNullOrWhiteSpace(expense.Notes))
            {
                var match = System.Text.RegularExpressions.Regex.Match(expense.Notes, @"\[وردية #(\d+)\]");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int shiftId))
                {
                    var shift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId);
                    if (shift != null && (!shift.IsActive || shift.EndTime.HasValue))
                    {
                        throw new InvalidOperationException($"لا يمكن حذف هذا المصروف لأن الوردية المرتبطة به (مناوبة #{shift.Id}) مغلقة بالفعل.\nتعديل أو حذف المصروفات بعد إغلاق الوردية ممنوع نظامياً ومحاسبياً لضمان سلامة التدقيق المالي ومطابقة الصندوق.");
                    }
                }
            }

            // 2. إذا كانت الوردية نشطة (أو لم يكن نقداً): عكس الحركات وحذف المصروف وسند الصرف المرتبط به
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // أ. عكس / إزالة حركات الصندوق المرتبطة ليعود رصيد الصندوق فوراً لحالته الصحيحة
                if (cashTransactions.Count > 0)
                {
                    context.CashBoxTransactions.RemoveRange(cashTransactions);
                }

                // ب. عكس / إزالة سند الصرف المرتبط (ReceiptVoucher)
                var vouchers = await context.ReceiptVouchers
                    .Where(v => v.Type == VoucherType.Payment &&
                        ((v.Notes != null && v.Notes.Contains(expenseTag)) ||
                         v.Name.Contains(expenseNameTag) ||
                         (!string.IsNullOrWhiteSpace(expense.InvoiceNumber) && v.VoucherNumber == expense.InvoiceNumber) ||
                         (expense.Notes != null && !string.IsNullOrWhiteSpace(v.VoucherNumber) && expense.Notes.Contains(v.VoucherNumber)) ||
                         (v.Amount == expense.Amount && v.VoucherDate.Date == expense.ExpenseDate.Date && v.Name.Contains(expense.Name))))
                    .ToListAsync();

                if (vouchers.Count > 0)
                {
                    context.ReceiptVouchers.RemoveRange(vouchers);
                }

                // ج. حذف المصروف نفسه
                context.Expenses.Remove(expense);

                await _unitOfWork.SaveChangesAsync();
                _unitOfWork.ClearEntityTracking();
            });

            ServiceLocator.NotifyInventoryChanged();
        }

        public async Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Expenses.GetTotalByDateRangeAsync(startDate, endDate);
        }

        public async Task<decimal> GetTotalByCategoryAsync(string category, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _unitOfWork.Expenses.GetTotalByCategoryAsync(category, startDate, endDate);
        }

        public async Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            return await _unitOfWork.GetContext().Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= start && e.ExpenseDate < endExclusive)
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(e => (double)e.Amount) })
                .ToDictionaryAsync(x => x.Category, x => (decimal)x.Total);
        }
    }
}
