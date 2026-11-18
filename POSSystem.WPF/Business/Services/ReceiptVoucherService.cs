using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.WPF.Business.Services
{
    public class ReceiptVoucherService : IReceiptVoucherService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialChannelService _financialChannelService;

        public ReceiptVoucherService(
            IUnitOfWork unitOfWork,
            IFinancialChannelService? financialChannelService = null)
        {
            _unitOfWork = unitOfWork;
            _financialChannelService = financialChannelService ?? ServiceLocator.FinancialChannelService;
        }

        public async Task<IEnumerable<ReceiptVoucher>> GetAllVouchersAsync()
        {
            return await _unitOfWork.ReceiptVouchers.GetAllAsync();
        }

        public async Task<IEnumerable<ReceiptVoucher>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.ReceiptVouchers.GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<ReceiptVoucher>> GetByTypeAsync(VoucherType type)
        {
            return await _unitOfWork.ReceiptVouchers.GetByTypeAsync(type);
        }

        public async Task<IEnumerable<ReceiptVoucher>> GetByCustomerNameAsync(string customerName)
        {
            return await _unitOfWork.ReceiptVouchers.GetByCustomerNameAsync(customerName);
        }

        public async Task<ReceiptVoucher?> GetVoucherByIdAsync(int id)
        {
            return await _unitOfWork.ReceiptVouchers.GetByIdAsync(id);
        }

        public async Task<ReceiptVoucher> CreateVoucherAsync(ReceiptVoucher voucher)
        {
            // We'll generate a guaranteed-unique VoucherNumber server-side in two steps
            // to avoid duplicate key errors when multiple vouchers are created concurrently.

            if (voucher.Amount <= 0)
                throw new ArgumentException("المبلغ يجب أن يكون أكبر من الصفر");

            if (string.IsNullOrWhiteSpace(voucher.CustomerName))
                throw new ArgumentException("اسم العميل مطلوب");

            // Basic length validation to avoid DB truncation errors
            if (voucher.VoucherNumber != null && voucher.VoucherNumber.Length > 100)
                throw new ArgumentException("رقم السند طويل جدًا");

            if (voucher.CustomerName != null && voucher.CustomerName.Length > 250)
                throw new ArgumentException("اسم العميل طويل جدًا");

            if (voucher.Notes != null && voucher.Notes.Length > 2000)
                throw new ArgumentException("الملاحظات طويلة جدًا");

            var now = DateTime.Now;
            voucher.CreatedAt = now;
            voucher.UpdatedAt = now;
            voucher.VoucherDate = voucher.VoucherDate == default ? now : voucher.VoucherDate;
            // Ensure DB required Name column is populated and never null (defensive)
            voucher.Name = voucher.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(voucher.Name))
            {
                voucher.Name = !string.IsNullOrWhiteSpace(voucher.CustomerName)
                    ? voucher.CustomerName
                    : (voucher.VoucherNumber ?? string.Empty);
            }
            // Pre-resolve Shift and validate drawer cash balance if Cash payment
            Shift? activeShift = null;
            if (voucher.PaymentMethod == PaymentMethod.Cash)
            {
                var context = _unitOfWork.GetContext();
                if (voucher.TargetShiftId.HasValue && voucher.TargetShiftId.Value > 0)
                {
                    activeShift = await context.Shifts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == voucher.TargetShiftId.Value && s.IsActive);

                    if (activeShift == null)
                    {
                        throw new InvalidOperationException($"المناوبة المحددة (#{voucher.TargetShiftId.Value}) غير نشطة أو مغلقة حالياً.");
                    }
                }
                else
                {
                    activeShift = await context.Shifts
                        .AsNoTracking()
                        .Where(s => s.IsActive)
                        .OrderByDescending(s => voucher.UserId.HasValue && s.UserId == voucher.UserId.Value)
                        .FirstOrDefaultAsync();
                }

                if (activeShift == null && voucher.Type == VoucherType.Payment)
                {
                    throw new InvalidOperationException("لا توجد مناوبة كاشير نشطة لصرف السند نقداً. يرجى فتح مناوبة أو اختيار طريقة دفع أخرى.");
                }
            }
            else
            {
                // Non-cash voucher: record payment method in notes
                string methodTag = $"[طريقة الدفع: {voucher.PaymentMethodText}]";
                if (string.IsNullOrWhiteSpace(voucher.Notes))
                {
                    voucher.Notes = methodTag;
                }
                else if (!voucher.Notes.Contains(voucher.PaymentMethodText))
                {
                    voucher.Notes = $"{voucher.Notes} {methodTag}";
                }
            }

            // التحقق الصارم من كفاية الرصيد لجميع جهات الصرف (صندوق الكاشير، المحافظ الإلكترونية، الحسابات البنكية)
            if (voucher.Type == VoucherType.Payment)
            {
                var channelTag = FinancialChannelHelper.ExtractChannel(voucher.PaymentMethod, voucher.Notes);
                await _financialChannelService.ValidateSufficientBalanceAsync(channelTag, voucher.Amount, activeShift?.Id);
            }

            // Use a transaction to ensure atomic creation of voucher and cash transaction
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // التحقق المزدوج ضد العمليات المتزامنة قبل الحفظ مباشرة داخل المعاملة
                if (voucher.Type == VoucherType.Payment)
                {
                    var channelTag = FinancialChannelHelper.ExtractChannel(voucher.PaymentMethod, voucher.Notes);
                    await _financialChannelService.ValidateSufficientBalanceAsync(channelTag, voucher.Amount, activeShift?.Id);
                }

                if (string.IsNullOrWhiteSpace(voucher.VoucherNumber) ||
                    voucher.VoucherNumber == "سيتم التوليد تلقائياً" ||
                    await _unitOfWork.GetContext().ReceiptVouchers.AsNoTracking().AnyAsync(v => v.VoucherNumber == voucher.VoucherNumber))
                {
                    voucher.VoucherNumber = await GenerateVoucherNumberAsync();
                }

                if (string.IsNullOrWhiteSpace(voucher.Name))
                {
                    voucher.Name = voucher.VoucherNumber;
                }

                await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

                // Link manually created voucher to CashBoxTransaction ONLY if Cash and activeShift exists
                if (voucher.PaymentMethod == PaymentMethod.Cash && activeShift != null)
                {
                    var cashTransaction = new CashBoxTransaction
                    {
                        Name = voucher.Type == VoucherType.Payment
                            ? $"سند صرف - {voucher.VoucherNumber}"
                            : $"سند قبض - {voucher.VoucherNumber}",
                        ShiftId = activeShift.Id,
                        UserId = voucher.UserId ?? activeShift.UserId,
                        Type = voucher.Type == VoucherType.Payment
                            ? CashBoxTransactionType.Expense
                            : CashBoxTransactionType.Income,
                        Amount = voucher.Amount,
                        Description = $"سند {(voucher.Type == VoucherType.Payment ? "صرف" : "قبض")} ({voucher.CustomerName})" +
                                      (!string.IsNullOrWhiteSpace(voucher.Notes) ? $" - {voucher.Notes}" : ""),
                        TransactionDate = voucher.VoucherDate,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Notes = voucher.Notes
                    };
                    await _unitOfWork.CashBoxTransactions.AddAsync(cashTransaction);
                }

                // Single atomic save for both voucher and cash transaction
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return voucher;
            }
            catch (DbUpdateException dbEx)
            {
                // Rollback transaction if started
                if (_unitOfWork.HasActiveTransaction)
                    await _unitOfWork.RollbackTransactionAsync();

                // Include voucher state to aid debugging if the DB rejects the insert
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                var voucherState = $"Id={voucher.Id}, VoucherNumber={voucher.VoucherNumber ?? "(null)"}, Name={voucher.Name ?? "(null)"}, CustomerName={voucher.CustomerName ?? "(null)"}, Amount={voucher.Amount}";
                throw new InvalidOperationException($"فشل حفظ السند: {innerMessage}. VoucherState: {voucherState}", dbEx);
            }
            catch (Exception ex)
            {
                if (_unitOfWork.HasActiveTransaction)
                    await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ReceiptVoucher> UpdateVoucherAsync(ReceiptVoucher voucher)
        {
            if (voucher.Amount <= 0)
                throw new ArgumentException("المبلغ يجب أن يكون أكبر من الصفر");

            if (string.IsNullOrWhiteSpace(voucher.CustomerName))
                throw new ArgumentException("اسم العميل مطلوب");

            var context = _unitOfWork.GetContext();
            var existingVoucher = await context.ReceiptVouchers.FirstOrDefaultAsync(v => v.Id == voucher.Id);
            if (existingVoucher == null)
                throw new ArgumentException($"السند برقم {voucher.Id} غير موجود");

            var now = DateTime.Now;

            // البحث عن حركة الصندوق النقدية المرتبطة برقم هذا السند
            CashBoxTransaction? linkedCashTx = null;
            if (!string.IsNullOrWhiteSpace(existingVoucher.VoucherNumber))
            {
                linkedCashTx = await context.CashBoxTransactions
                    .FirstOrDefaultAsync(t =>
                        (t.Name != null && t.Name.Contains(existingVoucher.VoucherNumber)) ||
                        (t.Notes != null && t.Notes.Contains(existingVoucher.VoucherNumber)) ||
                        (t.Description != null && t.Description.Contains(existingVoucher.VoucherNumber)));
            }

            EmployeeAdvance? linkedAdvanceForUpdate = null;
            Expense? linkedExpenseForUpdate = null;

            if (existingVoucher.EmployeeAdvanceId.HasValue && existingVoucher.EmployeeAdvanceId.Value > 0)
            {
                linkedAdvanceForUpdate = await context.EmployeeAdvances.FirstOrDefaultAsync(a => a.Id == existingVoucher.EmployeeAdvanceId.Value);
            }

            if (linkedAdvanceForUpdate == null)
            {
                linkedAdvanceForUpdate = await context.EmployeeAdvances.FirstOrDefaultAsync(a => a.ReceiptVoucherId == existingVoucher.Id);
            }

            if (linkedAdvanceForUpdate == null && existingVoucher.Notes != null)
            {
                var advMatch = System.Text.RegularExpressions.Regex.Match(existingVoucher.Notes, @"\[سلفة:\s*(\d+)\]");
                if (advMatch.Success && int.TryParse(advMatch.Groups[1].Value, out int advId))
                {
                    linkedAdvanceForUpdate = await context.EmployeeAdvances.FirstOrDefaultAsync(a => a.Id == advId);
                }
            }

            if (linkedAdvanceForUpdate != null && linkedCashTx == null)
            {
                linkedCashTx = await context.CashBoxTransactions
                    .FirstOrDefaultAsync(t => t.EmployeeAdvanceId == linkedAdvanceForUpdate.Id ||
                                             (t.Notes != null && t.Notes.Contains($"[سلفة: {linkedAdvanceForUpdate.Id}]")));
            }

            if (existingVoucher.Notes != null)
            {

                var expMatch = System.Text.RegularExpressions.Regex.Match(existingVoucher.Notes, @"\[مصروف #(\d+)\]");
                if (expMatch.Success && int.TryParse(expMatch.Groups[1].Value, out int expId))
                {
                    linkedExpenseForUpdate = await context.Expenses.FirstOrDefaultAsync(e => e.Id == expId);
                    if (linkedCashTx == null)
                    {
                        linkedCashTx = await context.CashBoxTransactions
                            .FirstOrDefaultAsync(t => (t.Notes != null && t.Notes.Contains($"[مصروف #{expId}]")) ||
                                                     (t.Name != null && t.Name.Contains($"مصروف #{expId}")));
                    }
                }
            }

            // منع تعديل سند نقدي مرتبط بمناوبة مغلقة ومرحلة
            if (linkedCashTx != null)
            {
                var shift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == linkedCashTx.ShiftId);
                if (shift != null && !shift.IsActive)
                {
                    throw new InvalidOperationException(
                        $"لا يمكن تعديل السند #{existingVoucher.VoucherNumber} لأن المناوبة المرتبطة به (#{shift.Id}) مغلقة ومرحلة بالفعل. " +
                        "يرجى إنشاء سند تسوية جديد بدلاً من تعديل سند سابق مرحل.");
                }
            }

            int? targetShiftId = linkedCashTx?.ShiftId ?? voucher.TargetShiftId;

            // التحقق الصارم من كفاية الرصيد لجميع جهات الصرف (صندوق، محفظة، بنك)
            if (voucher.Type == VoucherType.Payment)
            {
                var channelTag = FinancialChannelHelper.ExtractChannel(voucher.PaymentMethod, voucher.Notes);
                await _financialChannelService.ValidateSufficientBalanceAsync(channelTag, voucher.Amount, targetShiftId, excludedVoucherId: existingVoucher.Id);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // التحقق المزدوج ضد العمليات المتزامنة داخل المعاملة
                if (voucher.Type == VoucherType.Payment)
                {
                    var channelTag = FinancialChannelHelper.ExtractChannel(voucher.PaymentMethod, voucher.Notes);
                    await _financialChannelService.ValidateSufficientBalanceAsync(channelTag, voucher.Amount, targetShiftId, excludedVoucherId: existingVoucher.Id);
                }
                // مزامنة حركة الصندوق المرتبطة بالسند
                if (voucher.PaymentMethod == PaymentMethod.Cash)
                {
                    if (linkedCashTx != null)
                    {
                        linkedCashTx.Amount = voucher.Amount;
                        linkedCashTx.Type = voucher.Type == VoucherType.Payment
                            ? CashBoxTransactionType.Expense
                            : CashBoxTransactionType.Income;
                        linkedCashTx.Name = voucher.Type == VoucherType.Payment
                            ? $"سند صرف - {existingVoucher.VoucherNumber}"
                            : $"سند قبض - {existingVoucher.VoucherNumber}";
                        linkedCashTx.Description = $"سند {(voucher.Type == VoucherType.Payment ? "صرف" : "قبض")} ({voucher.CustomerName})" +
                                                   (!string.IsNullOrWhiteSpace(voucher.Notes) ? $" - {voucher.Notes}" : "");
                        linkedCashTx.TransactionDate = voucher.VoucherDate;
                        linkedCashTx.UpdatedAt = now;
                        linkedCashTx.Notes = voucher.Notes;

                        _unitOfWork.CashBoxTransactions.Update(linkedCashTx);
                    }
                    else
                    {
                        // كان غير نقدي وتحول إلى نقدي: إنشاء حركة صندوق في المناوبة النشطة
                        var activeShift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.IsActive);
                        if (activeShift != null)
                        {
                            var newCashTx = new CashBoxTransaction
                            {
                                Name = voucher.Type == VoucherType.Payment
                                    ? $"سند صرف - {existingVoucher.VoucherNumber}"
                                    : $"سند قبض - {existingVoucher.VoucherNumber}",
                                ShiftId = activeShift.Id,
                                UserId = voucher.UserId ?? activeShift.UserId,
                                Type = voucher.Type == VoucherType.Payment
                                    ? CashBoxTransactionType.Expense
                                    : CashBoxTransactionType.Income,
                                Amount = voucher.Amount,
                                Description = $"سند {(voucher.Type == VoucherType.Payment ? "صرف" : "قبض")} ({voucher.CustomerName})" +
                                              (!string.IsNullOrWhiteSpace(voucher.Notes) ? $" - {voucher.Notes}" : ""),
                                TransactionDate = voucher.VoucherDate,
                                CreatedAt = now,
                                UpdatedAt = now,
                                Notes = voucher.Notes
                            };
                            await _unitOfWork.CashBoxTransactions.AddAsync(newCashTx);
                        }
                    }
                }
                else
                {
                    // تحول السند إلى غير نقدي (بنك / حوالة): نحذف حركة الصندوق لأنها لم تعد نقدية
                    if (linkedCashTx != null)
                    {
                        context.CashBoxTransactions.Remove(linkedCashTx);
                    }
                }

                // تحديث حقول كائن السند
                existingVoucher.Type = voucher.Type;
                existingVoucher.Amount = voucher.Amount;
                existingVoucher.CustomerName = voucher.CustomerName;
                existingVoucher.VoucherDate = voucher.VoucherDate;
                existingVoucher.PaymentMethod = voucher.PaymentMethod;
                existingVoucher.Notes = voucher.Notes;
                existingVoucher.UpdatedAt = now;
                if (!string.IsNullOrWhiteSpace(voucher.Name))
                    existingVoucher.Name = voucher.Name;

                // إذا كان السند لسلفة أو مصروف، نحدث قيمة السلفة أو المصروف الأصلية
                if (linkedAdvanceForUpdate != null)
                {
                    linkedAdvanceForUpdate.Amount = voucher.Amount;
                    linkedAdvanceForUpdate.UpdatedAt = now;
                    context.EmployeeAdvances.Update(linkedAdvanceForUpdate);
                }

                if (linkedExpenseForUpdate != null)
                {
                    linkedExpenseForUpdate.Amount = voucher.Amount;
                    linkedExpenseForUpdate.PaymentMethod = voucher.PaymentMethod;
                    linkedExpenseForUpdate.UpdatedAt = now;
                    context.Expenses.Update(linkedExpenseForUpdate);
                }

                _unitOfWork.ReceiptVouchers.Update(existingVoucher);
                await _unitOfWork.SaveChangesAsync();

                if (linkedAdvanceForUpdate != null)
                {
                    await RecalculateUnpaidSalariesForEmployeeAsync(linkedAdvanceForUpdate.EmployeeId);
                }
            });

            return existingVoucher;
        }

        public async Task DeleteVoucherAsync(int id)
        {
            var context = _unitOfWork.GetContext();
            var voucher = await context.ReceiptVouchers.FirstOrDefaultAsync(v => v.Id == id);
            if (voucher == null)
                throw new ArgumentException("السند غير موجود");

            // 1. فحص ما إذا كان السند مرتبطاً بسلفة موظف
            EmployeeAdvance? linkedAdvance = null;
            if (voucher.Type == VoucherType.Payment)
            {
                // أ) الربط المباشر بالمفتاح الأجنبي الصريح في السند
                if (voucher.EmployeeAdvanceId.HasValue && voucher.EmployeeAdvanceId.Value > 0)
                {
                    linkedAdvance = await context.EmployeeAdvances
                        .Include(a => a.Employee)
                        .FirstOrDefaultAsync(a => a.Id == voucher.EmployeeAdvanceId.Value);
                }

                // ب) الربط العكسي بالمفتاح الأجنبي في السلفة
                if (linkedAdvance == null)
                {
                    linkedAdvance = await context.EmployeeAdvances
                        .Include(a => a.Employee)
                        .FirstOrDefaultAsync(a => a.ReceiptVoucherId == voucher.Id);
                }

                // ج) البحث برقم السلفة من الملاحظات [سلفة: X]
                if (linkedAdvance == null && !string.IsNullOrWhiteSpace(voucher.Notes))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(voucher.Notes, @"\[سلفة:\s*(\d+)\]");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int advId) && advId > 0)
                    {
                        linkedAdvance = await context.EmployeeAdvances
                            .Include(a => a.Employee)
                            .FirstOrDefaultAsync(a => a.Id == advId);
                    }
                }

                // د) البحث بنمط رقم السند التلقائي PV-ADV-yyyyMMdd-XXXX-
                if (linkedAdvance == null && !string.IsNullOrWhiteSpace(voucher.VoucherNumber))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(voucher.VoucherNumber, @"PV-ADV-\d+-(\d+)-");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int advId) && advId > 0)
                    {
                        linkedAdvance = await context.EmployeeAdvances
                            .Include(a => a.Employee)
                            .FirstOrDefaultAsync(a => a.Id == advId);
                    }
                }

                // هـ) مطابقة السندات المعرفة كسلفة بالاسم أو الملاحظات مع موظف ومبلغ مطابق
                if (linkedAdvance == null)
                {
                    bool isAdvanceNamed = (voucher.VoucherNumber != null && voucher.VoucherNumber.StartsWith("PV-ADV-")) ||
                                          (voucher.Notes != null && (voucher.Notes.Contains("سند صرف سلفة") || voucher.Notes.Contains("سلفة"))) ||
                                          (voucher.Name != null && voucher.Name.Contains("سلفة"));

                    if (isAdvanceNamed)
                    {
                        if (voucher.UserId.HasValue)
                        {
                            linkedAdvance = await context.EmployeeAdvances
                                .Include(a => a.Employee)
                                .FirstOrDefaultAsync(a => a.EmployeeId == voucher.UserId.Value &&
                                                          a.Amount == voucher.Amount &&
                                                          !a.IsRepaid && a.PaidAmount == 0);
                        }

                        if (linkedAdvance == null && !string.IsNullOrWhiteSpace(voucher.CustomerName))
                        {
                            var emp = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Name == voucher.CustomerName.Trim());
                            if (emp != null)
                            {
                                linkedAdvance = await context.EmployeeAdvances
                                    .Include(a => a.Employee)
                                    .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id &&
                                                              a.Amount == voucher.Amount &&
                                                              !a.IsRepaid && a.PaidAmount == 0);
                            }
                        }
                    }
                }

                // و) فحص حالة السداد (Requirement 6): منع حذف سند السلفة إذا بدأ تسديدها أو استقطاعها
                if (linkedAdvance != null && (linkedAdvance.PaidAmount > 0 || linkedAdvance.IsRepaid))
                {
                    throw new InvalidOperationException(
                        $"لا يمكن حذف سند صرف السلفة #{voucher.VoucherNumber} لأن السلفة مسددة أو تم استقطاع جزء منها ({linkedAdvance.PaidAmount:N0} ريال من أصل {linkedAdvance.Amount:N0} ريال).\n\n" +
                        "لحماية السجلات المحاسبية والمالية، يجب إلغاء أو عكس عمليات السداد / مسيرات الرواتب المرتبطة أولاً قبل التمكن من إلغاء السلفة.");
                }
            }

            // 2. فحص ما إذا كان السند مرتبطاً بمصروف
            Expense? linkedExpense = null;
            if (voucher.Type == VoucherType.Payment &&
                ((voucher.VoucherNumber != null && voucher.VoucherNumber.StartsWith("PV-EXP-")) ||
                 (voucher.Notes != null && voucher.Notes.Contains("[مصروف #")) ||
                 (voucher.Name != null && voucher.Name.Contains("مصروف #"))))
            {
                int expId = 0;
                if (!string.IsNullOrWhiteSpace(voucher.Notes))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(voucher.Notes, @"\[مصروف #(\d+)\]");
                    if (m.Success) int.TryParse(m.Groups[1].Value, out expId);
                }
                if (expId > 0)
                {
                    linkedExpense = await context.Expenses.FirstOrDefaultAsync(e => e.Id == expId);
                }
            }

            // 3. فحص ما إذا كان السند مرتبطاً بمسير رواتب
            EmployeeSalary? linkedSalary = null;
            if (voucher.Type == VoucherType.Payment &&
                ((voucher.VoucherNumber != null && voucher.VoucherNumber.StartsWith("PV-SAL-")) ||
                 (voucher.Notes != null && (voucher.Notes.Contains("سند صرف راتب") || voucher.Notes.Contains("[راتب:"))) ||
                 (voucher.Name != null && voucher.Name.Contains("صرف راتب"))))
            {
                int salId = 0;
                if (!string.IsNullOrWhiteSpace(voucher.Notes))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(voucher.Notes, @"\[راتب:\s*(\d+)\]");
                    if (m.Success) int.TryParse(m.Groups[1].Value, out salId);
                }
                if (salId == 0 && !string.IsNullOrWhiteSpace(voucher.VoucherNumber))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(voucher.VoucherNumber, @"PV-SAL-\d+-(\d+)-");
                    if (m.Success) int.TryParse(m.Groups[1].Value, out salId);
                }
                if (salId > 0)
                {
                    linkedSalary = await context.EmployeeSalaries.FirstOrDefaultAsync(s => s.Id == salId);
                }
            }

            // 4. تجميع كافة حركات الصندوق المرتبطة بالسند
            var linkedCashTransactions = new List<CashBoxTransaction>();

            if (!string.IsNullOrWhiteSpace(voucher.VoucherNumber))
            {
                var directByNumber = await context.CashBoxTransactions
                    .Where(t => (t.Name != null && t.Name.Contains(voucher.VoucherNumber)) ||
                                (t.Notes != null && t.Notes.Contains(voucher.VoucherNumber)) ||
                                (t.Description != null && t.Description.Contains(voucher.VoucherNumber)))
                    .ToListAsync();
                linkedCashTransactions.AddRange(directByNumber);
            }

            if (linkedAdvance != null)
            {
                string advTag = $"[سلفة: {linkedAdvance.Id}]";
                string advName = $"سلفة موظف #{linkedAdvance.Id}";
                var advCashTxs = await context.CashBoxTransactions
                    .Where(t => t.EmployeeAdvanceId == linkedAdvance.Id ||
                                (t.Notes != null && t.Notes.Contains(advTag)) ||
                                (t.Description != null && t.Description.Contains(advTag)) ||
                                (t.Name != null && t.Name.Contains(advName)) ||
                                (t.Type == CashBoxTransactionType.Advance && t.UserId == linkedAdvance.EmployeeId && t.Amount == linkedAdvance.Amount && t.TransactionDate.Date == linkedAdvance.AdvanceDate.Date))
                    .ToListAsync();

                foreach (var tx in advCashTxs)
                {
                    if (!linkedCashTransactions.Any(x => x.Id == tx.Id))
                        linkedCashTransactions.Add(tx);
                }
            }

            if (linkedExpense != null)
            {
                string expTag = $"[مصروف #{linkedExpense.Id}]";
                var expCashTxs = await context.CashBoxTransactions
                    .Where(t => (t.Notes != null && t.Notes.Contains(expTag)) ||
                                (t.Description != null && t.Description.Contains(expTag)) ||
                                (t.Name != null && t.Name.Contains($"مصروف #{linkedExpense.Id}")))
                    .ToListAsync();

                foreach (var tx in expCashTxs)
                {
                    if (!linkedCashTransactions.Any(x => x.Id == tx.Id))
                        linkedCashTransactions.Add(tx);
                }
            }

            if (linkedSalary != null)
            {
                string salTag = $"[راتب: {linkedSalary.Id}]";
                var salCashTxs = await context.CashBoxTransactions
                    .Where(t => (t.Notes != null && t.Notes.Contains(salTag)) ||
                                (t.Description != null && t.Description.Contains(salTag)) ||
                                (t.Name != null && t.Name.Contains($"صرف راتب #{linkedSalary.Id}")) ||
                                (t.Type == CashBoxTransactionType.Expense && t.UserId == linkedSalary.EmployeeId && t.Amount == linkedSalary.NetSalary && t.Name != null && t.Name.Contains("صرف راتب")))
                    .ToListAsync();

                foreach (var tx in salCashTxs)
                {
                    if (!linkedCashTransactions.Any(x => x.Id == tx.Id))
                        linkedCashTransactions.Add(tx);
                }
            }

            // 5. التحقق من حالة المناوبات لجميع الحركات المرتبطة: منع الحذف إذا كانت المناوبة مغلقة
            foreach (var tx in linkedCashTransactions)
            {
                var shift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == tx.ShiftId);
                if (shift != null && !shift.IsActive)
                {
                    throw new InvalidOperationException(
                        $"لا يمكن حذف السند #{voucher.VoucherNumber} لأن المناوبة المرتبطة به (#{shift.Id}) مغلقة ومرحلة بالفعل.\n" +
                        "حذف السند سيؤدي إلى كسر المطابقة النقدية التاريخية للمناوبة. يرجى إنشاء سند عكسي بدلاً من الحذف.");
                }
            }

            // 6. التنفيذ الذري لعملية الحذف داخل Transaction
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // أ. حذف حركات الصندوق المرتبطة لعكس الخصم فوراً وإعادة الرصيد للدرج
                if (linkedCashTransactions.Count > 0)
                {
                    context.CashBoxTransactions.RemoveRange(linkedCashTransactions);
                }

                // ب. إذا كان السند لسلفة، نحذف السلفة من سجل السلفيات حتى لا تخصم من الراتب
                if (linkedAdvance != null)
                {
                    voucher.EmployeeAdvanceId = null;
                    linkedAdvance.ReceiptVoucherId = null;
                    context.EmployeeAdvances.Remove(linkedAdvance);
                }

                // ج. إذا كان السند لمصروف، نحذف المصروف المرتبط
                if (linkedExpense != null)
                {
                    context.Expenses.Remove(linkedExpense);
                }

                // د. إذا كان السند لراتب، نعيد حالة الراتب إلى غير مدفوع ونعكس استقطاعات السلف المرتبطة به
                if (linkedSalary != null)
                {
                    if (linkedSalary.AdvanceDeduction > 0)
                    {
                        var empAdvances = await context.EmployeeAdvances
                            .Where(a => a.EmployeeId == linkedSalary.EmployeeId && a.PaidAmount > 0)
                            .OrderByDescending(a => a.UpdatedAt)
                            .ToListAsync();

                        decimal toRestore = linkedSalary.AdvanceDeduction;
                        foreach (var adv in empAdvances)
                        {
                            if (toRestore <= 0) break;
                            decimal rev = Math.Min(toRestore, adv.PaidAmount);
                            adv.PaidAmount -= rev;
                            toRestore -= rev;
                            if (adv.RemainingAmount > 0)
                            {
                                adv.IsRepaid = false;
                                adv.RepaymentDate = null;
                            }
                            adv.UpdatedAt = DateTime.Now;
                            context.EmployeeAdvances.Update(adv);
                        }
                    }

                    linkedSalary.IsPaid = false;
                    linkedSalary.PaymentDate = default;
                    context.EmployeeSalaries.Update(linkedSalary);
                }

                // د-2. إذا كان السند سند قبض لسداد دين عميل أو سداد سلفة موظف، نعكس المبلغ المسدد
                if (voucher.Type == VoucherType.Receipt)
                {
                    if (voucher.CustomerDebtId.HasValue && voucher.CustomerDebtId.Value > 0)
                    {
                        var debt = await context.CustomerDebts.FirstOrDefaultAsync(d => d.Id == voucher.CustomerDebtId.Value);
                        if (debt != null)
                        {
                            debt.PaidAmount = Math.Max(0m, debt.PaidAmount - voucher.Amount);
                            if (debt.RemainingAmount > 0)
                            {
                                debt.IsPaid = false;
                                debt.PaymentDate = null;
                            }
                            debt.UpdatedAt = DateTime.Now;
                            context.CustomerDebts.Update(debt);
                        }
                    }
                    else if (voucher.VoucherNumber != null && voucher.VoucherNumber.StartsWith("RV-ADV-"))
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(voucher.VoucherNumber, @"RV-ADV-\d+-(\d+)-");
                        if (m.Success && int.TryParse(m.Groups[1].Value, out int advId) && advId > 0)
                        {
                            var adv = await context.EmployeeAdvances.FirstOrDefaultAsync(a => a.Id == advId);
                            if (adv != null)
                            {
                                adv.PaidAmount = Math.Max(0m, adv.PaidAmount - voucher.Amount);
                                if (adv.RemainingAmount > 0)
                                {
                                    adv.IsRepaid = false;
                                    adv.RepaymentDate = null;
                                }
                                adv.UpdatedAt = DateTime.Now;
                                context.EmployeeAdvances.Update(adv);
                            }
                        }
                    }
                }

                // هـ. حذف السند نفسه
                context.ReceiptVouchers.Remove(voucher);

                await _unitOfWork.SaveChangesAsync();

                // و. إذا كانت سلفة، نعيد احتساب استقطاعات الرواتب للموظف
                if (linkedAdvance != null)
                {
                    await RecalculateUnpaidSalariesForEmployeeAsync(linkedAdvance.EmployeeId);
                }
            });

            _unitOfWork.ClearEntityTracking();
        }

        private async Task RecalculateUnpaidSalariesForEmployeeAsync(int employeeId)
        {
            var unpaidSalaries = await _unitOfWork.EmployeeSalaries.GetUnpaidSalariesByEmployeeIdAsync(employeeId);
            var sortedSalaries = unpaidSalaries.OrderBy(s => s.SalaryDate).ToList();
            if (!sortedSalaries.Any()) return;

            var unpaidAdvances = await _unitOfWork.EmployeeAdvances.GetUnpaidAdvancesAsync(employeeId);
            var monthlyAdvances = unpaidAdvances
                .Where(a => a.RepaymentMethod == AdvanceRepaymentMethod.MonthlyDeduction)
                .OrderBy(a => a.AdvanceDate)
                .ToList();

            var now = DateTime.Now;
            var tempDeductions = new Dictionary<int, decimal>();

            foreach (var salary in sortedSalaries)
            {
                decimal totalAvailable = 0m;
                foreach (var advance in monthlyAdvances)
                {
                    decimal deductedSoFar = tempDeductions.TryGetValue(advance.Id, out var d) ? d : 0m;
                    decimal remaining = advance.RemainingAmount - deductedSoFar;
                    if (remaining > 0)
                        totalAvailable += remaining;
                }

                var advanceDeduction = Math.Min(totalAvailable, salary.BaseSalary);
                salary.AdvanceDeduction = advanceDeduction;
                salary.NetSalary = salary.BaseSalary - advanceDeduction;
                salary.UpdatedAt = now;
                _unitOfWork.EmployeeSalaries.Update(salary);

                if (advanceDeduction > 0)
                {
                    decimal remainingToDeduct = advanceDeduction;
                    foreach (var advance in monthlyAdvances)
                    {
                        if (remainingToDeduct <= 0) break;
                        decimal deductedSoFar = tempDeductions.TryGetValue(advance.Id, out var d) ? d : 0m;
                        decimal availableInThisAdvance = advance.RemainingAmount - deductedSoFar;
                        if (availableInThisAdvance <= 0) continue;

                        decimal take = Math.Min(remainingToDeduct, availableInThisAdvance);
                        tempDeductions[advance.Id] = deductedSoFar + take;
                        remainingToDeduct -= take;
                    }
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<decimal> GetTotalReceiptsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.ReceiptVouchers.GetTotalReceiptsByDateRangeAsync(startDate, endDate);
        }

        public async Task<decimal> GetTotalPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.ReceiptVouchers.GetTotalPaymentsByDateRangeAsync(startDate, endDate);
        }

        public async Task<string> GenerateVoucherNumberAsync()
        {
            var today = DateTime.Now.Date;
            var prefix = $"VCH-{today:yyyyMMdd}-";
            var context = _unitOfWork.GetContext();

            var existingNumbers = await context.ReceiptVouchers
                .AsNoTracking()
                .Where(v => v.VoucherNumber != null && v.VoucherNumber.StartsWith(prefix))
                .Select(v => v.VoucherNumber!)
                .ToListAsync();

            var existingSet = new HashSet<string>(existingNumbers, StringComparer.OrdinalIgnoreCase);
            int maxSequence = 0;
            foreach (var num in existingNumbers)
            {
                if (num.Length > prefix.Length)
                {
                    var suffix = num.Substring(prefix.Length);
                    var dashIdx = suffix.IndexOf('-');
                    if (dashIdx > 0)
                        suffix = suffix.Substring(0, dashIdx);

                    if (int.TryParse(suffix, out int seq) && seq > maxSequence)
                    {
                        maxSequence = seq;
                    }
                }
            }

            int nextSeq = maxSequence + 1;
            string candidate = $"{prefix}{nextSeq:D4}";
            while (existingSet.Contains(candidate) ||
                   await context.ReceiptVouchers.AsNoTracking().AnyAsync(v => v.VoucherNumber == candidate))
            {
                nextSeq++;
                candidate = $"{prefix}{nextSeq:D4}";
            }

            return candidate;
        }
    }
}

