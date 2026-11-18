using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Data;
using POSSystem.WPF.Models;
using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.WPF.Business.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialChannelService _financialChannelService;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            IFinancialChannelService? financialChannelService = null)
        {
            _unitOfWork = unitOfWork;
            _financialChannelService = financialChannelService ?? ServiceLocator.FinancialChannelService;
        }

        // Employee management
        public async Task<IEnumerable<User>> GetAllEmployeesAsync()
        {
            return await _unitOfWork.Users.GetAllAsync();
        }

        public async Task<User?> GetEmployeeByIdAsync(int id)
        {
            return await _unitOfWork.Users.GetByIdAsync(id);
        }

        public async Task<User> CreateEmployeeAsync(User employee)
        {
            if (string.IsNullOrWhiteSpace(employee.Name))
                throw new ArgumentException("Employee name is required");

          
            //// Hash password if provided
            if (!string.IsNullOrWhiteSpace(employee.Password))
            {
                employee.Password = POSDbContext.HashPassword(employee.Password);
            }

            employee.Name = employee.Name.Trim();
            employee.Email = string.IsNullOrWhiteSpace(employee.Email) ? null : employee.Email.Trim();

            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                bool emailTaken = await _unitOfWork.GetContext().Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Email != null && u.Email.ToLower() == employee.Email.ToLower());
                if (emailTaken)
                    throw new InvalidOperationException("البريد الإلكتروني المدخل مسجل مسبقاً لمستخدم آخر.");
            }

            employee.CreatedAt = DateTime.Now;
            employee.UpdatedAt = DateTime.Now;
            employee.IsActive = true;

            await _unitOfWork.Users.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            return employee;
        }

        public async Task<User> UpdateEmployeeAsync(User employee)
        {
            var existingEmployee = await _unitOfWork.Users.GetByIdAsync(employee.Id);
            if (existingEmployee == null)
                throw new ArgumentException("Employee not found");

            // Check if email already exists (excluding current employee)
            //var duplicateEmployee = await _unitOfWork.Users.GetUserByEmailAsync(employee.Email);
            //if (duplicateEmployee != null && duplicateEmployee.Id != employee.Id)
            //    throw new InvalidOperationException("Employee with this email already exists");

            // Update password if provided and changed
            if (!string.IsNullOrWhiteSpace(employee.Password) && employee.Password != existingEmployee.Password)
            {
                existingEmployee.Password = POSDbContext.HashPassword(employee.Password);
            }

            var normalizedEmail = string.IsNullOrWhiteSpace(employee.Email) ? null : employee.Email.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                bool emailTaken = await _unitOfWork.GetContext().Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Id != employee.Id && u.Email != null && u.Email.ToLower() == normalizedEmail.ToLower());
                if (emailTaken)
                    throw new InvalidOperationException("البريد الإلكتروني المدخل مسجل مسبقاً لمستخدم آخر.");
            }

            existingEmployee.Name = employee.Name.Trim();
            existingEmployee.Phone = employee.Phone;
            existingEmployee.JobTitle = employee.JobTitle;
            existingEmployee.BaseSalary = employee.BaseSalary;
            existingEmployee.HireDate = employee.HireDate;
            existingEmployee.Email = normalizedEmail;
            if (existingEmployee.Id != 1 && existingEmployee.Role != UserRole.Admin)
            {
                existingEmployee.IsActive = employee.IsActive;
                existingEmployee.RoleTemplateName = employee.RoleTemplateName;
                existingEmployee.PermissionsJson = employee.PermissionsJson;
            }

            existingEmployee.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveChangesAsync();
            ServiceLocator.NotifyUserPermissionsChanged(existingEmployee);

            // Auto-Clean & Recalculate Unpaid Salaries when HireDate or BaseSalary is updated:
            // 1. Determine effective hireDate and hireMonthStart
            DateTime hireDate = (existingEmployee.HireDate ?? existingEmployee.CreatedAt).Date;
            DateTime hireMonthStart = new DateTime(hireDate.Year, hireDate.Month, 1);

            // 2. Delete any unpaid (!s.IsPaid) salary records strictly before hireMonthStart
            var context = _unitOfWork.GetContext();
            foreach (var localSalary in context.EmployeeSalaries.Local
                .Where(s => s.EmployeeId == existingEmployee.Id && !s.IsPaid && s.SalaryDate < hireMonthStart)
                .ToList())
            {
                context.Entry(localSalary).State = EntityState.Detached;
            }

            await context.EmployeeSalaries
                .Where(s => s.EmployeeId == existingEmployee.Id && !s.IsPaid && s.SalaryDate < hireMonthStart)
                .ExecuteDeleteAsync();

            // 3. For any remaining unpaid records in or after hireMonthStart, recalculate BaseSalary and Notes
            var remainingUnpaidSalaries = await _unitOfWork.EmployeeSalaries.GetUnpaidSalariesByEmployeeIdAsync(existingEmployee.Id);
            if (remainingUnpaidSalaries.Any())
            {
                decimal employeeBaseSalary = existingEmployee.BaseSalary ?? 0m;
                foreach (var salary in remainingUnpaidSalaries)
                {
                    int salaryYear = salary.SalaryDate.Year;
                    int salaryMonth = salary.SalaryDate.Month;
                    int daysInMonth = DateTime.DaysInMonth(salaryYear, salaryMonth);
                    bool isHireMonth = (salaryYear == hireDate.Year && salaryMonth == hireDate.Month);

                    if (isHireMonth && hireDate.Day > 1)
                    {
                        int workedDays = daysInMonth - hireDate.Day + 1;
                        decimal ratio = (decimal)workedDays / daysInMonth;
                        salary.BaseSalary = Math.Round(employeeBaseSalary * ratio, 2);
                        salary.Notes = $"راتب نسبي عن شهر {salaryMonth:D2}/{salaryYear} ({workedDays} يوم عمل من أصل {daysInMonth} - تاريخ التعيين: {hireDate.Year}/{hireDate.Month:D2}/{hireDate.Day:D2})";
                    }
                    else
                    {
                        salary.BaseSalary = employeeBaseSalary;
                        salary.Notes = $"راتب شهر {salaryMonth:D2}/{salaryYear} كامل ({daysInMonth} يوم عمل)";
                    }

                    _unitOfWork.EmployeeSalaries.Update(salary);
                }

                await _unitOfWork.SaveChangesAsync();
                await UpdateUnpaidSalaryForEmployeeAsync(existingEmployee.Id);
            }

            return employee;
        }

        public async Task DeleteEmployeeAsync(int employeeId)
        {
            var context = _unitOfWork.GetContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // حذف الرواتب والسلف مباشرة بأوامر SQL مجمعة دون سحبها للذاكرة
                await context.EmployeeSalaries
                    .Where(s => s.EmployeeId == employeeId)
                    .ExecuteDeleteAsync();

                await context.EmployeeAdvances
                    .Where(a => a.EmployeeId == employeeId)
                    .ExecuteDeleteAsync();

                int deleted = await context.Users
                    .Where(u => u.Id == employeeId)
                    .ExecuteDeleteAsync();

                if (deleted == 0)
                    throw new ArgumentException("الموظف غير موجود");

                await transaction.CommitAsync();
                _unitOfWork.ClearEntityTracking();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Employee Advances
        public async Task<IEnumerable<EmployeeAdvance>> GetEmployeeAdvancesAsync(int employeeId)
        {
            return await _unitOfWork.EmployeeAdvances.GetByEmployeeIdAsync(employeeId);
        }

        public async Task<IEnumerable<EmployeeAdvance>> GetAllUnpaidAdvancesAsync()
        {
            return await _unitOfWork.EmployeeAdvances.GetUnpaidAdvancesAsync();
        }

        public async Task<EmployeeAdvance> CreateAdvanceAsync(EmployeeAdvance advance)
        {
            if (advance.Amount <= 0)
                throw new ArgumentException("Advance amount must be greater than zero");

            var employee = await _unitOfWork.Users.GetByIdAsync(advance.EmployeeId);
            if (employee == null)
                throw new ArgumentException("Employee not found");

            var now = DateTime.Now;
            advance.CreatedAt = now;
            advance.UpdatedAt = now;
            advance.IsRepaid = false;

            // Pre-resolve Shift if Cash payout
            Shift? targetShift = null;
            if (advance.PaymentMethod == PaymentMethod.Cash)
            {
                var context = _unitOfWork.GetContext();

                if (advance.TargetShiftId.HasValue && advance.TargetShiftId.Value > 0)
                {
                    targetShift = await context.Shifts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == advance.TargetShiftId.Value && s.IsActive);

                    if (targetShift == null)
                    {
                        throw new InvalidOperationException($"المناوبة المختارة (#{advance.TargetShiftId.Value}) غير نشطة أو مغلقة حالياً. يرجى اختيار مناوبة كاشير نشطة.");
                    }
                }
                else
                {
                    targetShift = await context.Shifts
                        .AsNoTracking()
                        .Where(s => s.IsActive)
                        .OrderByDescending(s => s.UserId == advance.EmployeeId)
                        .FirstOrDefaultAsync();
                }

                if (targetShift == null)
                {
                    throw new InvalidOperationException("لا توجد مناوبة كاشير نشطة لصرف السلفة نقداً من الصندوق. يرجى اختيار طريقة دفع أخرى (تحويل بنكي / محفظة / خزينة خارجية) أو فتح مناوبة كاشير.");
                }
            }

            // التحقق الصارم من كفاية الرصيد لجميع جهات الصرف (صندوق الكاشير، المحافظ الإلكترونية، الحسابات البنكية)
            var advanceChannel = FinancialChannelHelper.ExtractChannel(advance.PaymentMethod, advance.Notes);
            await _financialChannelService.ValidateSufficientBalanceAsync(advanceChannel, advance.Amount, targetShift?.Id);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // التحقق المزدوج ضد العمليات المتزامنة قبل الخصم داخل المعاملة
                await _financialChannelService.ValidateSufficientBalanceAsync(advanceChannel, advance.Amount, targetShift?.Id);

                await _unitOfWork.EmployeeAdvances.AddAsync(advance);
                await _unitOfWork.SaveChangesAsync();

                // 1. Create official Disbursement Voucher (سند صرف سلفة)
                var voucherNumber = $"PV-ADV-{now:yyyyMMdd}-{advance.Id:D4}-{Guid.NewGuid().ToString("N")[..4]}";
                var voucherNotes = $"[سلفة: {advance.Id}] سند صرف سلفة موظف: {employee.Name} | مصدر الصرف: {advance.PayoutMethodText} | طريقة السداد: {advance.RepaymentMethodText}" +
                                   (!string.IsNullOrWhiteSpace(advance.Notes) ? $" | {advance.Notes}" : "");
                if (voucherNotes.Length > 2000) voucherNotes = voucherNotes.Substring(0, 2000);

                var voucher = new ReceiptVoucher
                {
                    VoucherNumber = voucherNumber,
                    Name = $"سند صرف سلفة #{advance.Id} - {employee.Name}",
                    Type = VoucherType.Payment,
                    PaymentMethod = advance.PaymentMethod,
                    Amount = advance.Amount,
                    CustomerName = employee.Name,
                    VoucherDate = advance.AdvanceDate == default ? now : advance.AdvanceDate,
                    Notes = voucherNotes,
                    UserId = advance.EmployeeId,
                    EmployeeAdvanceId = advance.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _unitOfWork.ReceiptVouchers.AddAsync(voucher);
                await _unitOfWork.SaveChangesAsync();

                advance.ReceiptVoucherId = voucher.Id;
                _unitOfWork.EmployeeAdvances.Update(advance);

                // 2. Record CashBoxTransaction from active shift if Cash payout
                if (advance.PaymentMethod == PaymentMethod.Cash && targetShift != null)
                {
                    var cashTransaction = new CashBoxTransaction
                    {
                        Name = $"سلفة موظف #{advance.Id} - {employee.Name}",
                        ShiftId = targetShift.Id,
                        UserId = advance.EmployeeId,
                        Type = CashBoxTransactionType.Advance,
                        Amount = advance.Amount,
                        Description = $"سند صرف سلفة للموظف {employee.Name} [سند: {voucherNumber}] [سلفة: {advance.Id}] ({advance.RepaymentMethodText})",
                        TransactionDate = advance.AdvanceDate == default ? now : advance.AdvanceDate,
                        EmployeeAdvanceId = advance.Id,
                        IsPersonal = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Notes = $"[سند: {voucherNumber}] [سلفة: {advance.Id}] {advance.Notes}".Trim()
                    };
                    await _unitOfWork.CashBoxTransactions.AddAsync(cashTransaction);
                }

                await UpdateUnpaidSalaryForEmployeeAsync(advance.EmployeeId);
            });

            return advance;
        }

        public async Task<EmployeeAdvance> UpdateAdvanceAsync(EmployeeAdvance advance)
        {
            var existingAdvance = await _unitOfWork.EmployeeAdvances.GetByIdAsync(advance.Id);
            if (existingAdvance == null)
                throw new ArgumentException("Advance not found");

            advance.UpdatedAt = DateTime.Now;
            _unitOfWork.EmployeeAdvances.Update(advance);
            await _unitOfWork.SaveChangesAsync();

            await UpdateUnpaidSalaryForEmployeeAsync(advance.EmployeeId);

            return advance;
        }

        public async Task DeleteAdvanceAsync(int advanceId)
        {
            var context = _unitOfWork.GetContext();
            var advance = await context.EmployeeAdvances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == advanceId);

            if (advance == null)
                throw new ArgumentException("السلفة غير موجودة");

            // فحص الأعمال المالية: منع حذف سلفة تم تسديدها أو خصمها من الراتب
            if (advance.PaidAmount > 0 || advance.IsRepaid)
            {
                throw new InvalidOperationException(
                    $"لا يمكن حذف السلفة #{advance.Id} لأنها مسددة أو تم استقطاع جزء منها ({advance.PaidAmount:N0} ريال).\n" +
                    "يجب إلغاء أو عكس عمليات السداد / الرواتب المرتبطة أولاً قبل التمكن من حذف السلفة.");
            }

            // فحص حركات الصندوق المرتبطة بالسلفة
            string advIdTag = $"[سلفة: {advance.Id}]";
            string advNameTag = $"سلفة موظف #{advance.Id}";
            var linkedCashTransactions = await context.CashBoxTransactions
                .Where(t => t.EmployeeAdvanceId == advance.Id ||
                            (t.Type == CashBoxTransactionType.Advance && t.UserId == advance.EmployeeId && t.Amount == advance.Amount && t.TransactionDate.Date == advance.AdvanceDate.Date) ||
                            (t.Notes != null && t.Notes.Contains(advIdTag)) ||
                            (t.Description != null && t.Description.Contains(advIdTag)) ||
                            (t.Name != null && t.Name.Contains(advNameTag)))
                .ToListAsync();

            // التحقق من حالة المناوبة / الوردية
            foreach (var tx in linkedCashTransactions)
            {
                var shift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == tx.ShiftId);
                if (shift != null && !shift.IsActive)
                {
                    throw new InvalidOperationException(
                        $"لا يمكن حذف السلفة #{advance.Id} لأن المناوبة المرتبطة بها (#{shift.Id}) مغلقة ومرحلة بالفعل.\n" +
                        "حذف السلف بعد إغلاق المناوبة يؤدي إلى خلل في التدقيق المحاسبي التاريخي للصندوق.");
                }
            }

            // فحص سندات الصرف المرتبطة بالسلفة
            string advVoucherPattern = $"-{advance.Id:D4}-";
            var linkedVouchers = await context.ReceiptVouchers
                .Where(v => v.Type == VoucherType.Payment &&
                    (v.EmployeeAdvanceId == advance.Id ||
                     (advance.ReceiptVoucherId.HasValue && v.Id == advance.ReceiptVoucherId.Value) ||
                     (v.VoucherNumber != null && v.VoucherNumber.StartsWith("PV-ADV-") && v.VoucherNumber.Contains(advVoucherPattern)) ||
                     (v.Notes != null && v.Notes.Contains(advIdTag)) ||
                     (v.Name != null && v.Name.Contains(advNameTag)) ||
                     (v.UserId == advance.EmployeeId && v.Amount == advance.Amount && v.VoucherDate.Date == advance.AdvanceDate.Date &&
                      (v.Name.Contains("سلفة") || (v.Notes != null && v.Notes.Contains("سلفة"))))))
                .ToListAsync();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // 1. حذف حركات الصندوق المرتبطة لعكس الخصم فوراً من الدرج
                if (linkedCashTransactions.Count > 0)
                {
                    context.CashBoxTransactions.RemoveRange(linkedCashTransactions);
                }

                // 2. حذف سندات الصرف المرتبطة
                if (linkedVouchers.Count > 0)
                {
                    foreach (var v in linkedVouchers)
                    {
                        v.EmployeeAdvanceId = null;
                    }
                    context.ReceiptVouchers.RemoveRange(linkedVouchers);
                }

                // 3. حذف السلفة نفسها
                advance.ReceiptVoucherId = null;
                context.EmployeeAdvances.Remove(advance);

                await _unitOfWork.SaveChangesAsync();

                // 4. إعادة احتساب استحقاقات الموظف
                await UpdateUnpaidSalaryForEmployeeAsync(advance.EmployeeId);
            });

            _unitOfWork.ClearEntityTracking();
        }

        public async Task MarkAdvanceAsRepaidAsync(int advanceId)
        {
            var advance = await _unitOfWork.EmployeeAdvances.GetByIdAsync(advanceId);
            if (advance == null)
                throw new ArgumentException("Advance not found");

            var remaining = advance.RemainingAmount;
            if (remaining <= 0)
            {
                advance.IsRepaid = true;
                advance.RepaymentDate = DateTime.Now;
                advance.UpdatedAt = DateTime.Now;
                _unitOfWork.EmployeeAdvances.Update(advance);
                await _unitOfWork.SaveChangesAsync();
                await UpdateUnpaidSalaryForEmployeeAsync(advance.EmployeeId);
                return;
            }

            await RepayAdvanceAsync(advanceId, remaining, "تسديد السلفة بالكامل");
        }

        public async Task RepayAdvanceAsync(int advanceId, decimal amount, string? notes = null)
        {
            if (amount <= 0)
                throw new ArgumentException("Repayment amount must be greater than zero");

            var advance = await _unitOfWork.EmployeeAdvances.GetByIdAsync(advanceId);
            if (advance == null)
                throw new ArgumentException("Advance not found");

            var employee = await _unitOfWork.Users.GetByIdAsync(advance.EmployeeId);
            var empName = employee?.Name ?? "موظف";
            var now = DateTime.Now;

            if (amount > advance.RemainingAmount)
                amount = advance.RemainingAmount;

            advance.PaidAmount += amount;
            
            if (!string.IsNullOrWhiteSpace(notes))
            {
                advance.Notes = string.IsNullOrWhiteSpace(advance.Notes) 
                    ? $"[سداد {amount:N0} ريال: {notes}]" 
                    : $"{advance.Notes}\n[سداد {amount:N0} ريال: {notes}]";
            }

            if (advance.RemainingAmount <= 0)
            {
                advance.IsRepaid = true;
                advance.RepaymentDate = now;
            }

            advance.UpdatedAt = now;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                _unitOfWork.EmployeeAdvances.Update(advance);
                await _unitOfWork.SaveChangesAsync();

                // 1. Create official Receipt Voucher (سند قبض سداد سلفة)
                var voucherNumber = $"RV-ADV-{now:yyyyMMdd}-{advance.Id:D4}-{now:HHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}";
                var voucherNotes = $"سند قبض سداد سلفة موظف: {empName} | رقم السلفة: {advance.Id}" +
                                   (!string.IsNullOrWhiteSpace(notes) ? $" | {notes}" : "");
                if (voucherNotes.Length > 2000) voucherNotes = voucherNotes.Substring(0, 2000);

                var voucher = new ReceiptVoucher
                {
                    VoucherNumber = voucherNumber,
                    Name = $"سند قبض سداد سلفة - {empName}",
                    Type = VoucherType.Receipt,
                    Amount = amount,
                    CustomerName = empName,
                    VoucherDate = now,
                    Notes = voucherNotes,
                    UserId = advance.EmployeeId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

                // 2. Record CashBoxTransaction (Income)
                var activeShift = await _unitOfWork.GetContext().Shifts
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.UserId == advance.EmployeeId)
                    .FirstOrDefaultAsync();

                if (activeShift != null)
                {
                    var cashTransaction = new CashBoxTransaction
                    {
                        Name = $"سداد سلفة - {empName}",
                        ShiftId = activeShift.Id,
                        UserId = advance.EmployeeId,
                        Type = CashBoxTransactionType.Income,
                        Amount = amount,
                        Description = $"سداد سلفة موظف: {empName} (المتبقي: {advance.RemainingAmount:N0} ريال)",
                        TransactionDate = now,
                        EmployeeAdvanceId = advance.Id,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Notes = notes
                    };
                    await _unitOfWork.CashBoxTransactions.AddAsync(cashTransaction);
                }

                await UpdateUnpaidSalaryForEmployeeAsync(advance.EmployeeId);
            });
        }

        private async Task UpdateUnpaidSalaryForEmployeeAsync(int employeeId)
        {
            // Find all unpaid salary records for the employee
            var unpaidSalaries = await _unitOfWork.EmployeeSalaries.GetUnpaidSalariesByEmployeeIdAsync(employeeId);
            var sortedSalaries = unpaidSalaries.OrderBy(s => s.SalaryDate).ToList();
            
            if (!sortedSalaries.Any()) return;

            // Get all unpaid monthly deduction advances
            var unpaidAdvances = await _unitOfWork.EmployeeAdvances.GetUnpaidAdvancesAsync(employeeId);
            var monthlyAdvances = unpaidAdvances
                .Where(a => a.RepaymentMethod == AdvanceRepaymentMethod.MonthlyDeduction)
                .OrderBy(a => a.AdvanceDate)
                .ToList();

            var now = DateTime.Now;
            var tempDeductions = new Dictionary<int, decimal>(); // Track deductions made across salaries

            foreach (var salary in sortedSalaries)
            {
                // Include all unpaid advances
                var applicableAdvances = monthlyAdvances.ToList();

                decimal totalAvailable = 0m;
                foreach (var advance in applicableAdvances)
                {
                    decimal deductedSoFar = tempDeductions.TryGetValue(advance.Id, out var d) ? d : 0m;
                    decimal remaining = advance.RemainingAmount - deductedSoFar;
                    if (remaining > 0)
                        totalAvailable += remaining;
                }

                // Calculate deduction for this salary
                var advanceDeduction = Math.Min(totalAvailable, salary.BaseSalary);

                salary.AdvanceDeduction = advanceDeduction;
                salary.NetSalary = salary.BaseSalary - advanceDeduction;
                salary.UpdatedAt = now;

                _unitOfWork.EmployeeSalaries.Update(salary);

                // Update temporary tracker for the next salary in the loop
                if (advanceDeduction > 0)
                {
                    decimal remainingToDeduct = advanceDeduction;
                    foreach (var advance in applicableAdvances.OrderBy(a => a.AdvanceDate))
                    {
                        if (remainingToDeduct <= 0) break;

                        decimal deductedSoFar = tempDeductions.TryGetValue(advance.Id, out var d) ? d : 0m;
                        decimal remaining = advance.RemainingAmount - deductedSoFar;

                        if (remaining > 0)
                        {
                            decimal take = Math.Min(remainingToDeduct, remaining);
                            tempDeductions[advance.Id] = deductedSoFar + take;
                            remainingToDeduct -= take;
                        }
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<decimal> GetTotalUnpaidAdvancesAsync(int employeeId)
        {
            return await _unitOfWork.EmployeeAdvances.GetTotalUnpaidAdvancesAsync(employeeId);
        }

        // Employee Salaries
        public async Task<IEnumerable<EmployeeSalary>> GetEmployeeSalariesAsync(int employeeId)
        {
            return await _unitOfWork.EmployeeSalaries.GetByEmployeeIdAsync(employeeId);
        }

        public async Task<IEnumerable<EmployeeSalary>> GetSalariesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.EmployeeSalaries.GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<EmployeeSalary>> GetAllUnpaidSalariesAsync()
        {
            return await _unitOfWork.EmployeeSalaries.GetUnpaidSalariesAsync();
        }

        public async Task<EmployeeSalary> CreateSalaryAsync(EmployeeSalary salary)
        {
            var employee = await _unitOfWork.Users.GetByIdAsync(salary.EmployeeId);
            if (employee == null)
                throw new ArgumentException("Employee not found");

            if (employee.BaseSalary == null || employee.BaseSalary <= 0)
                throw new ArgumentException("Employee base salary is not set");

            // Calculate advance deduction
            var unpaidAdvances = await _unitOfWork.EmployeeAdvances.GetUnpaidAdvancesAsync(salary.EmployeeId);
            var totalRemainingAdvances = unpaidAdvances.Sum(a => a.RemainingAmount);

            if (salary.BaseSalary <= 0)
            {
                DateTime hireDate = (employee.HireDate ?? employee.CreatedAt).Date;
                DateTime periodMonth = salary.SalaryDate;
                int daysInMonth = DateTime.DaysInMonth(periodMonth.Year, periodMonth.Month);

                if (periodMonth.Year == hireDate.Year && periodMonth.Month == hireDate.Month && hireDate.Day > 1)
                {
                    int workedDays = daysInMonth - hireDate.Day + 1;
                    decimal ratio = (decimal)workedDays / daysInMonth;
                    salary.BaseSalary = Math.Round(employee.BaseSalary.Value * ratio, 2);
                    if (string.IsNullOrWhiteSpace(salary.Notes))
                    {
                        salary.Notes = $"راتب نسبي عن شهر {periodMonth.Month:D2}/{periodMonth.Year} ({workedDays} يوم عمل من أصل {daysInMonth} - تاريخ التعيين: {hireDate.Year}/{hireDate.Month:D2}/{hireDate.Day:D2})";
                    }
                }
                else
                {
                    salary.BaseSalary = employee.BaseSalary.Value;
                    if (string.IsNullOrWhiteSpace(salary.Notes))
                    {
                        salary.Notes = $"راتب شهر {periodMonth.Month:D2}/{periodMonth.Year} كامل ({daysInMonth} يوم عمل)";
                    }
                }
            }
            
            // Cap deduction to not exceed base salary
            salary.AdvanceDeduction = Math.Min(totalRemainingAdvances, salary.BaseSalary);

            // Calculate net salary
            salary.NetSalary = salary.BaseSalary - salary.AdvanceDeduction;

            salary.CreatedAt = DateTime.Now;
            salary.UpdatedAt = DateTime.Now;

            await _unitOfWork.EmployeeSalaries.AddAsync(salary);
            await _unitOfWork.SaveChangesAsync();

            return salary;
        }

        public async Task<EmployeeSalary> UpdateSalaryAsync(EmployeeSalary salary)
        {
            var existingSalary = await _unitOfWork.EmployeeSalaries.GetByIdAsync(salary.Id);
            if (existingSalary == null)
                throw new ArgumentException("Salary not found");

            salary.UpdatedAt = DateTime.Now;
            _unitOfWork.EmployeeSalaries.Update(salary);
            await _unitOfWork.SaveChangesAsync();

            return salary;
        }

        public async Task MarkSalaryAsPaidAsync(int salaryId, PaymentMethod paymentMethod = PaymentMethod.Cash, int? targetShiftId = null, string? walletOrChannelName = null, string? extraNotes = null)
        {
            var salary = await _unitOfWork.EmployeeSalaries.GetByIdAsync(salaryId);
            if (salary == null)
                throw new ArgumentException("Salary not found");

            // التحقق من حالة الراتب قبل بدء أي معاملة لتجنب ترك Transaction معلقة
            if (salary.IsPaid) return;

            var context = _unitOfWork.GetContext();
            var now = DateTime.Now;

            // تضمين اسم المحفظة أو القناة المختارة في ملاحظات الراتب ليتم خصمها من المحفظة الصحيحة
            if (paymentMethod != PaymentMethod.Cash && !string.IsNullOrWhiteSpace(walletOrChannelName))
            {
                string walletTag = $"[محفظة: {walletOrChannelName.Trim()}]";
                if (string.IsNullOrWhiteSpace(salary.Notes))
                    salary.Notes = walletTag;
                else if (!salary.Notes.Contains(walletTag, StringComparison.OrdinalIgnoreCase))
                    salary.Notes = $"{salary.Notes} {walletTag}";
            }

            if (!string.IsNullOrWhiteSpace(extraNotes))
            {
                salary.Notes = string.IsNullOrWhiteSpace(salary.Notes)
                    ? extraNotes.Trim()
                    : $"{salary.Notes} | {extraNotes.Trim()}";
            }

            // إذا كان الصرف نقداً من الصندوق، نتحقق من وجود مناوبة نشطة وكفاية السيولة النقدية في الدرج
            Shift? activeShift = null;
            if (paymentMethod == PaymentMethod.Cash)
            {
                if (targetShiftId.HasValue && targetShiftId.Value > 0)
                {
                    activeShift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == targetShiftId.Value && s.IsActive);
                    if (activeShift == null)
                        throw new InvalidOperationException($"المناوبة المحددة (#{targetShiftId.Value}) غير نشطة أو مغلقة حالياً.");
                }
                else
                {
                    activeShift = await context.Shifts.AsNoTracking()
                        .Where(s => s.IsActive)
                        .OrderByDescending(s => s.UserId == salary.EmployeeId)
                        .FirstOrDefaultAsync();
                }

                if (activeShift == null && salary.NetSalary > 0)
                {
                    throw new InvalidOperationException("لا توجد مناوبة كاشير نشطة لصرف صافي الراتب نقداً من الصندوق. يرجى فتح مناوبة أو اختيار صرف الراتب عبر (محفظة إلكترونية / تحويل بنكي).");
                }
            }

            // التحقق الصارم من كفاية الرصيد لجميع قنوات صرف الرواتب (صندوق الكاشير، المحافظ الإلكترونية، الحسابات البنكية)
            string salaryChannel = !string.IsNullOrWhiteSpace(walletOrChannelName) && paymentMethod != PaymentMethod.Cash
                ? FinancialChannelHelper.NormalizeChannelKey(walletOrChannelName)
                : FinancialChannelHelper.ExtractChannel(paymentMethod, salary.Notes);

            if (salary.NetSalary > 0)
            {
                await _financialChannelService.ValidateSufficientBalanceAsync(salaryChannel, salary.NetSalary, activeShift?.Id);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // التحقق المزدوج ضد العمليات المتزامنة قبل الحفظ داخل المعاملة
                if (salary.NetSalary > 0)
                {
                    await _financialChannelService.ValidateSufficientBalanceAsync(salaryChannel, salary.NetSalary, activeShift?.Id);
                }

                var employee = await _unitOfWork.Users.GetByIdAsync(salary.EmployeeId);
                var empName = employee?.Name ?? "موظف";

                salary.IsPaid = true;
                salary.PaymentDate = now;
                salary.PaymentMethod = paymentMethod;
                salary.UpdatedAt = now;
                _unitOfWork.EmployeeSalaries.Update(salary);

                // Process advance deductions if any
                if (salary.AdvanceDeduction > 0)
                {
                    var deductionAdvances = await context.EmployeeAdvances
                        .Where(a => a.EmployeeId == salary.EmployeeId && !a.IsRepaid && a.RepaymentMethod == AdvanceRepaymentMethod.MonthlyDeduction)
                        .OrderBy(a => a.AdvanceDate)
                        .ToListAsync();

                    decimal remainingDeduction = salary.AdvanceDeduction;

                    foreach (var advance in deductionAdvances)
                    {
                        if (remainingDeduction <= 0) break;

                        decimal amountToApply = Math.Min(remainingDeduction, advance.RemainingAmount);
                        advance.PaidAmount += amountToApply;
                        remainingDeduction -= amountToApply;

                        if (advance.RemainingAmount <= 0)
                        {
                            advance.IsRepaid = true;
                            advance.RepaymentDate = salary.PaymentDate;
                        }

                        advance.UpdatedAt = now;
                    }
                }

                // 1. Create official Payroll Disbursement Voucher (سند صرف مسير رواتب)
                if (salary.NetSalary > 0)
                {
                    var voucherNumber = $"PV-SAL-{now:yyyyMMdd}-{salary.Id:D4}-{Guid.NewGuid().ToString("N")[..4]}";
                    var voucherNotes = $"[راتب: {salary.Id}] سند صرف راتب شهري: {empName} | طريقة الصرف: {salary.PayoutMethodText} | لشهر: {salary.SalaryDate:yyyy/MM} | الراتب الأساسي: {salary.BaseSalary:N0} | استقطاع السلف: {salary.AdvanceDeduction:N0} | صافي المستلم: {salary.NetSalary:N0}" +
                                       (!string.IsNullOrWhiteSpace(salary.Notes) ? $" | {salary.Notes}" : "");
                    if (voucherNotes.Length > 2000) voucherNotes = voucherNotes.Substring(0, 2000);

                    var voucher = new ReceiptVoucher
                    {
                        VoucherNumber = voucherNumber,
                        Name = $"سند صرف راتب #{salary.Id} ({salary.PayoutMethodText}) - {empName}",
                        Type = VoucherType.Payment,
                        Amount = salary.NetSalary,
                        CustomerName = empName,
                        VoucherDate = now,
                        PaymentMethod = paymentMethod,
                        Notes = voucherNotes,
                        UserId = salary.EmployeeId,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

                    // 2. Record CashBoxTransaction ONLY if payment was made in cash and activeShift exists
                    if (paymentMethod == PaymentMethod.Cash && activeShift != null)
                    {
                        var cashTransaction = new CashBoxTransaction
                        {
                            Name = $"صرف راتب #{salary.Id} - {empName}",
                            ShiftId = activeShift.Id,
                            UserId = salary.EmployeeId,
                            Type = CashBoxTransactionType.Expense,
                            Amount = salary.NetSalary,
                            Description = $"[سند: {voucherNumber}] [راتب: {salary.Id}] صرف راتب شهر {salary.SalaryDate:yyyy/MM} للموظف {empName} (صافي بعد الخصم: {salary.NetSalary:N0} ريال)",
                            TransactionDate = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            Notes = $"[سند: {voucherNumber}] [راتب: {salary.Id}] {salary.Notes}".Trim()
                        };
                        await _unitOfWork.CashBoxTransactions.AddAsync(cashTransaction);
                    }

                    // 3. تسجيل تلقائي في إدارة المصروفات وجرد الحسابات تحت "حساب رواتب وموظفين" (كمرجع محاسبي مباشر دون خصم مزدوج)
                    string salaryTag = $"[راتب: {salary.Id}]";
                    bool alreadyInExpenses = await context.Expenses.AnyAsync(e => e.Notes != null && e.Notes.Contains(salaryTag));
                    if (!alreadyInExpenses)
                    {
                        var salaryExpense = new Expense
                        {
                            Name = $"صرف راتب شهر {salary.SalaryDate:yyyy/MM} - {empName}",
                            Category = "حساب رواتب وموظفين",
                            Amount = salary.NetSalary,
                            Type = ExpenseType.Monthly,
                            ExpenseDate = now,
                            PaymentMethod = paymentMethod,
                            InvoiceNumber = voucherNumber,
                            SupplierName = empName,
                            UserId = salary.EmployeeId,
                            Notes = $"{salaryTag} [سند صرف: {voucherNumber}] {salary.Notes}".Trim(),
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        await _unitOfWork.Expenses.AddAsync(salaryExpense);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            });

            FinancialChannelService.InvalidateCache();
            ServiceLocator.NotifyCashBoxChanged();
        }

        public async Task ProcessMonthlySalariesAsync(DateTime processingDate)
        {
            var employees = (await _unitOfWork.Users.GetActiveUsersAsync()).ToList();
            var employeeIds = employees.Select(e => e.Id).ToList();
            
            // OPTIMIZATION: Prevent N+1 queries by loading all unpaid advances into memory once
            var allUnpaidAdvances = await _unitOfWork.EmployeeAdvances.GetUnpaidAdvancesAsync();
            var advancesByEmployee = allUnpaidAdvances
                .Where(a => a.RepaymentMethod == AdvanceRepaymentMethod.MonthlyDeduction)
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.AdvanceDate).ToList());

            // OPTIMIZATION: Prevent N+1 queries by pre-fetching all existing salaries for active employees once
            var allExistingSalaries = await _unitOfWork.GetContext().EmployeeSalaries
                .Where(s => employeeIds.Contains(s.EmployeeId))
                .ToListAsync();
            var salariesByEmployee = allExistingSalaries
                .GroupBy(s => s.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            var now = DateTime.Now;

            foreach (var employee in employees)
            {
                if (employee.BaseSalary == null || employee.BaseSalary <= 0)
                    continue;

                DateTime hireDate = (employee.HireDate ?? employee.CreatedAt).Date;

                // If hired in the future relative to processing date, skip
                if (hireDate > processingDate.Date)
                    continue;

                // Retrieve preloaded existing salaries for this employee
                if (!salariesByEmployee.TryGetValue(employee.Id, out var empSalaries))
                {
                    empSalaries = new List<EmployeeSalary>();
                    salariesByEmployee[employee.Id] = empSalaries;
                }

                DateTime hireMonthStart = new DateTime(hireDate.Year, hireDate.Month, 1);

                // 1. Remove any existing unpaid salaries before hireMonthStart
                var oldUnpaidSalaries = empSalaries
                    .Where(s => !s.IsPaid && s.SalaryDate < hireMonthStart)
                    .ToList();

                foreach (var oldSalary in oldUnpaidSalaries)
                {
                    _unitOfWork.EmployeeSalaries.Remove(oldSalary);
                    empSalaries.Remove(oldSalary);
                }

                // Process sequentially from the hire month up to the processing month
                DateTime currentMonthCursor = hireMonthStart;
                DateTime targetMonthCursor = new DateTime(processingDate.Year, processingDate.Month, 1);

                while (currentMonthCursor <= targetMonthCursor)
                {
                    int monthKey = currentMonthCursor.Year * 100 + currentMonthCursor.Month;
                    int daysInMonth = DateTime.DaysInMonth(currentMonthCursor.Year, currentMonthCursor.Month);
                    DateTime monthEndDate = new DateTime(currentMonthCursor.Year, currentMonthCursor.Month, daysInMonth);

                    decimal baseSalaryForPeriod;
                    string notes;

                    // Check if this is the employee's hire month and they started mid-month
                    bool isHireMonth = (currentMonthCursor.Year == hireDate.Year && currentMonthCursor.Month == hireDate.Month);
                    if (isHireMonth && hireDate.Day > 1)
                    {
                        // Prorated salary (تكسير الراتب) based on actual worked days
                        int workedDays = daysInMonth - hireDate.Day + 1;
                        decimal ratio = (decimal)workedDays / daysInMonth;
                        baseSalaryForPeriod = Math.Round(employee.BaseSalary.Value * ratio, 2);
                        notes = $"راتب نسبي عن شهر {currentMonthCursor.Month:D2}/{currentMonthCursor.Year} ({workedDays} يوم عمل من أصل {daysInMonth} - تاريخ التعيين: {hireDate.Year}/{hireDate.Month:D2}/{hireDate.Day:D2})";
                    }
                    else
                    {
                        // Full monthly salary (e.g. month 3 onwards, or started on day 1)
                        baseSalaryForPeriod = employee.BaseSalary.Value;
                        notes = $"راتب شهر {currentMonthCursor.Month:D2}/{currentMonthCursor.Year} كامل ({daysInMonth} يوم عمل)";
                    }

                    var existingSalary = empSalaries.FirstOrDefault(s => s.SalaryDate.Year * 100 + s.SalaryDate.Month == monthKey);

                    if (existingSalary == null)
                    {
                        var salary = new EmployeeSalary
                        {
                            EmployeeId = employee.Id,
                            BaseSalary = baseSalaryForPeriod,
                            AdvanceDeduction = 0m,
                            NetSalary = baseSalaryForPeriod,
                            SalaryDate = monthEndDate,
                            PaymentDate = processingDate,
                            IsPaid = false,
                            Notes = notes,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        await _unitOfWork.EmployeeSalaries.AddAsync(salary);
                        empSalaries.Add(salary);
                    }
                    else if (!existingSalary.IsPaid)
                    {
                        // 2. If an unpaid salary already exists for this valid month, recalculate its BaseSalary and Notes
                        existingSalary.BaseSalary = baseSalaryForPeriod;
                        existingSalary.Notes = notes;
                        existingSalary.SalaryDate = monthEndDate;
                        existingSalary.UpdatedAt = now;
                        _unitOfWork.EmployeeSalaries.Update(existingSalary);
                    }

                    currentMonthCursor = currentMonthCursor.AddMonths(1);
                }

                // Compute AdvanceDeduction and NetSalary for all unpaid salaries directly in memory
                var unpaidSalaries = empSalaries.Where(s => !s.IsPaid).OrderBy(s => s.SalaryDate).ToList();
                if (unpaidSalaries.Any())
                {
                    var monthlyAdvances = advancesByEmployee.TryGetValue(employee.Id, out var advList)
                        ? advList
                        : new List<EmployeeAdvance>();

                    var tempDeductions = new Dictionary<int, decimal>();

                    foreach (var salary in unpaidSalaries)
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

                        if (salary.Id > 0)
                        {
                            _unitOfWork.EmployeeSalaries.Update(salary);
                        }

                        if (advanceDeduction > 0)
                        {
                            decimal remainingToDeduct = advanceDeduction;
                            foreach (var advance in monthlyAdvances)
                            {
                                if (remainingToDeduct <= 0) break;

                                decimal deductedSoFar = tempDeductions.TryGetValue(advance.Id, out var d) ? d : 0m;
                                decimal remaining = advance.RemainingAmount - deductedSoFar;

                                if (remaining > 0)
                                {
                                    decimal take = Math.Min(remainingToDeduct, remaining);
                                    tempDeductions[advance.Id] = deductedSoFar + take;
                                    remainingToDeduct -= take;
                                }
                            }
                        }
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }
        public async Task<List<User>?> GetUserByjobnamenameAsync(string DriverName)
        {

            return await _unitOfWork.Users.GetUserByjobnamenameAsync(DriverName);
        }





        public async Task<List<EmployeeAdvance>> GetAllAdvancesAsync()
        {
            return await _unitOfWork.EmployeeAdvances.GetAllAdvancesAsync();
        }

        public async Task<List<EmployeeLedgerEntry>> GetEmployeeStatementAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var context = _unitOfWork.GetContext();

            var employee = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == employeeId);
            if (employee == null) return new List<EmployeeLedgerEntry>();

            // 1. Accrued Salaries (استحقاقات الرواتب الشهرية)
            var salaries = await context.EmployeeSalaries
                .AsNoTracking()
                .Where(s => s.EmployeeId == employeeId)
                .ToListAsync();

            // 2. Advances Taken (السلفيات المصروفة للموظف)
            var advances = await context.EmployeeAdvances
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId)
                .ToListAsync();

            // 3. Direct Cash/Transfer Repayments of Advances (سدادات السلف المباشرة)
            var repayments = await context.ReceiptVouchers
                .AsNoTracking()
                .Where(v => v.UserId == employeeId && v.Type == VoucherType.Receipt &&
                            (v.VoucherNumber.StartsWith("RV-ADV") || (v.Notes != null && v.Notes.Contains("سداد سلفة"))))
                .ToListAsync();

            // Build all raw movements chronologically
            var rawMovements = new List<EmployeeLedgerEntry>();

            // Add Accrued Salaries
            foreach (var s in salaries)
            {
                var sDate = s.SalaryDate == default ? s.CreatedAt : s.SalaryDate;
                rawMovements.Add(new EmployeeLedgerEntry
                {
                    Date = sDate,
                    TransactionType = "استحقاق راتب شهري",
                    ReferenceNumber = $"راتب-{sDate:yyyy/MM}",
                    Debit = 0m,
                    Credit = s.BaseSalary, // للموظف (دائن)
                    Notes = s.Notes ?? $"استحقاق راتب شهر {sDate:yyyy/MM}"
                });

                // If paid, add Salary Payment movement
                if (s.IsPaid && s.NetSalary > 0)
                {
                    var pDate = s.PaymentDate == default ? s.UpdatedAt : s.PaymentDate;
                    rawMovements.Add(new EmployeeLedgerEntry
                    {
                        Date = pDate,
                        TransactionType = $"صرف وتسليم راتب ({s.PayoutMethodText})",
                        ReferenceNumber = $"مسير-{sDate:yyyy/MM}",
                        Debit = s.NetSalary, // استلام الراتب (مدين على الموظف كمنصرف)
                        Credit = 0m,
                        Notes = $"صافي الراتب المستلم ({s.PayoutMethodText}) بعد استقطاع سلف بقيمة {s.AdvanceDeduction:N0} ريال",
                        PayoutMethod = s.PayoutMethodText
                    });
                }
            }

            // Add Advances Taken
            foreach (var a in advances)
            {
                var aDate = a.AdvanceDate == default ? a.CreatedAt : a.AdvanceDate;
                rawMovements.Add(new EmployeeLedgerEntry
                {
                    Date = aDate,
                    TransactionType = $"صرف سلفة ({a.PayoutMethodText})",
                    ReferenceNumber = $"سلفة-ADV-{a.Id:D4}",
                    Debit = a.Amount, // مسحوبات على الموظف (مدين)
                    Credit = 0m,
                    Notes = a.Notes ?? $"سلفة نقدية ({a.PayoutMethodText})",
                    PayoutMethod = a.PayoutMethodText
                });
            }

            // Add Direct Repayments (excluding advance deductions that are settled via NetSalary)
            foreach (var r in repayments)
            {
                rawMovements.Add(new EmployeeLedgerEntry
                {
                    Date = r.VoucherDate,
                    TransactionType = "سداد دفعة سلفة",
                    ReferenceNumber = r.VoucherNumber,
                    Debit = 0m,
                    Credit = r.Amount, // سداد يدخل في حساب الموظف (دائن)
                    Notes = r.Notes ?? "سداد نقدي / تحويل لسلفة موظف",
                    PayoutMethod = r.PaymentMethodText
                });
            }

            // Order chronologically
            var sortedMovements = rawMovements.OrderBy(m => m.Date).ToList();

            // Calculate Opening Balance if startDate is specified
            decimal openingBalance = 0m;
            var inPeriodMovements = new List<EmployeeLedgerEntry>();

            if (startDate.HasValue)
            {
                var sDate = startDate.Value.Date;
                var priorMovements = sortedMovements.Where(m => m.Date.Date < sDate).ToList();
                openingBalance = priorMovements.Sum(m => m.Credit - m.Debit);

                var eDate = endDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;
                inPeriodMovements = sortedMovements.Where(m => m.Date.Date >= sDate && m.Date <= eDate).ToList();
            }
            else
            {
                var eDate = endDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;
                inPeriodMovements = sortedMovements.Where(m => m.Date <= eDate).ToList();
            }

            var result = new List<EmployeeLedgerEntry>();

            // Add opening balance entry if non-zero
            if (startDate.HasValue && openingBalance != 0m)
            {
                result.Add(new EmployeeLedgerEntry
                {
                    Date = startDate.Value.Date,
                    TransactionType = "رصيد سابق (افتتاحي)",
                    ReferenceNumber = "-",
                    Debit = openingBalance < 0 ? Math.Abs(openingBalance) : 0m,
                    Credit = openingBalance > 0 ? openingBalance : 0m,
                    Balance = openingBalance,
                    Notes = openingBalance > 0 ? "رصيد مستحق للموظف من فترات سابقة" : "رصيد متبقي على الموظف من فترات سابقة"
                });
            }

            decimal currentRunningBalance = openingBalance;
            foreach (var entry in inPeriodMovements)
            {
                currentRunningBalance += (entry.Credit - entry.Debit);
                entry.Balance = currentRunningBalance;
                result.Add(entry);
            }

            // Reverse so latest transaction is first for UI display
            result.Reverse();
            return result;
        }

    }
}
