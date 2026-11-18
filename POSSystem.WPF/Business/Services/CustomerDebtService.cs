using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services
{
    public class CustomerDebtService : ICustomerDebtService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerDebtService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<CustomerDebt>> GetAllDebtsAsync()
        {
            return await _unitOfWork.CustomerDebts.GetAllAsync();
        }

        public async Task<IEnumerable<CustomerDebt>> GetUnpaidDebtsAsync()
        {
            return await _unitOfWork.CustomerDebts.GetUnpaidDebtsAsync();
        }

        public async Task<IEnumerable<CustomerDebt>> GetPaidDebtsAsync()
        {
            return await _unitOfWork.CustomerDebts.GetPaidDebtsAsync();
        }

        public async Task<IEnumerable<CustomerDebt>> GetPaidDebtsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.CustomerDebts.GetPaidDebtsInRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<CustomerDebt>> GetDebtsByCustomerNameAsync(string customerName)
        {
            return await _unitOfWork.CustomerDebts.GetDebtsByCustomerNameAsync(customerName);
        }

        public async Task<IEnumerable<CustomerDebt>> GetOldDebtsAsync(int days = 30)
        {
            return await _unitOfWork.CustomerDebts.GetOldDebtsAsync(days);
        }

        public async Task<CustomerDebt?> GetDebtByIdAsync(int id)
        {
            return await _unitOfWork.CustomerDebts.GetByIdAsync(id);
        }

        public async Task<CustomerDebt> CreateDebtAsync(CustomerDebt debt)
        {
            if (string.IsNullOrWhiteSpace(debt.CustomerName))
                throw new ArgumentException("اسم العميل مطلوب");

            if (debt.Amount <= 0)
                throw new ArgumentException("المبلغ يجب أن يكون أكبر من الصفر");

            var context = _unitOfWork.GetContext();

            // 1. استعلام واحد مدمج للبحث عن العميل بدلاً من 3 استعلامات متتالية
            Customer? customer = null;
            if (debt.CustomerId > 0 || !string.IsNullOrWhiteSpace(debt.PhoneNumber) || !string.IsNullOrWhiteSpace(debt.CustomerName))
            {
                customer = await context.Customers.FirstOrDefaultAsync(c =>
                    (debt.CustomerId > 0 && c.Id == debt.CustomerId) ||
                    (!string.IsNullOrWhiteSpace(debt.PhoneNumber) && c.PhoneNumber == debt.PhoneNumber) ||
                    c.Name == debt.CustomerName);
            }

            // 2. ربط أو إنشاء العميل
            if (customer == null)
            {
                customer = new Customer
                {
                    Name = debt.CustomerName,
                    PhoneNumber = debt.PhoneNumber ?? string.Empty,
                    CreditLimit = (debt.CreditLimit.HasValue && debt.CreditLimit.Value >= 0) ? debt.CreditLimit.Value : 0m,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                debt.Customer = customer;
            }
            else
            {
                debt.CustomerId = customer.Id;
                debt.Customer = customer;
                if (!string.IsNullOrWhiteSpace(debt.PhoneNumber) && string.IsNullOrWhiteSpace(customer.PhoneNumber))
                {
                    customer.PhoneNumber = debt.PhoneNumber;
                }
                if (debt.CreditLimit.HasValue && debt.CreditLimit.Value >= 0)
                {
                    customer.CreditLimit = debt.CreditLimit.Value;
                }
            }

            // 3. التحقق من سقف المديونية (Credit Limit Enforcement)
            if (customer.CreditLimit > 0)
            {
                if (customer.Id > 0)
                {
                    var currentUnpaid = (decimal)(await context.CustomerDebts
                        .AsNoTracking()
                        .Where(d => d.CustomerId == customer.Id && !d.IsPaid)
                        .SumAsync(d => (double?)(d.Amount - d.PaidAmount)) ?? 0.0);

                    if (currentUnpaid + debt.Amount > customer.CreditLimit)
                    {
                        decimal availableCredit = Math.Max(0m, customer.CreditLimit - currentUnpaid);
                        throw new InvalidOperationException(
                            $"عفواً، العميل ({customer.Name}) تجاوز سقف المديونية المسموح به!\n" +
                            $"سقف المديونية: {customer.CreditLimit:N0} ريال\n" +
                            $"المديونية الحالية: {currentUnpaid:N0} ريال\n" +
                            $"المتاح للاستدانة: {availableCredit:N0} ريال\n" +
                            $"مبلغ الفاتورة الحالية: {debt.Amount:N0} ريال");
                    }
                }
                else if (debt.Amount > customer.CreditLimit)
                {
                    decimal availableCredit = customer.CreditLimit;
                    throw new InvalidOperationException(
                        $"عفواً، العميل ({customer.Name}) تجاوز سقف المديونية المسموح به!\n" +
                        $"سقف المديونية: {customer.CreditLimit:N0} ريال\n" +
                        $"المديونية الحالية: 0.00 ريال\n" +
                        $"المتاح للاستدانة: {availableCredit:N0} ريال\n" +
                        $"مبلغ الفاتورة الحالية: {debt.Amount:N0} ريال");
                }
            }

            // 3. جلب بيانات الفاتورة المحددة فقط عبر Projection
            if (debt.OrderId.HasValue)
            {
                var orderInfo = await context.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == debt.OrderId.Value)
                    .Select(o => new { o.OrderNumber, o.CreatedAt })
                    .FirstOrDefaultAsync();

                if (orderInfo != null)
                {
                    string orderDetail = $"فاتورة {orderInfo.OrderNumber} ({orderInfo.CreatedAt:hh:mm tt}: {debt.Amount:N0} ريال)";
                    debt.Notes = string.IsNullOrWhiteSpace(debt.Notes)
                        ? orderDetail
                        : $"{orderDetail} | {debt.Notes}";
                }
            }

            if (debt.DebtDate == default)
            {
                debt.DebtDate = DateTime.Now;
            }

            debt.IsPaid = false;
            debt.CreatedAt = DateTime.Now;
            debt.UpdatedAt = DateTime.Now;

            // 4. حفظ جميع التغييرات دفعة واحدة
            await context.CustomerDebts.AddAsync(debt);
            await _unitOfWork.SaveChangesAsync();

            return debt;
        }

        public Task ConsolidateOpenSameDayDebtsAsync()
        {
            // Accounting Audit Fix: We deliberately do NOT merge or delete individual customer order debts.
            // Each invoice must remain an independent debit entry with its original OrderId intact
            // to support item/order returns, customer account statements, and auditable accounting ledgers.
            return Task.CompletedTask;
        }

        public async Task<CustomerDebt> UpdateDebtAsync(CustomerDebt debt)
        {
            if (string.IsNullOrWhiteSpace(debt.CustomerName))
                throw new ArgumentException("اسم العميل مطلوب");

            if (debt.Amount <= 0)
                throw new ArgumentException("المبلغ يجب أن يكون أكبر من الصفر");

            var context = _unitOfWork.GetContext();

            // فحص الوجود باستعلام خفيف جداً بدون تحميل شجرة الفاتورة وعناصرها
            bool exists = await context.CustomerDebts.AsNoTracking().AnyAsync(d => d.Id == debt.Id);
            if (!exists)
                throw new ArgumentException("الدين غير موجود");

            _unitOfWork.ClearEntityTracking();
            debt.UpdatedAt = DateTime.Now;

            if (debt.CreditLimit.HasValue && debt.CreditLimit.Value >= 0 && debt.CustomerId > 0)
            {
                var cust = await context.Customers.FirstOrDefaultAsync(c => c.Id == debt.CustomerId);
                if (cust != null)
                {
                    cust.CreditLimit = debt.CreditLimit.Value;
                }
            }

            _unitOfWork.CustomerDebts.Update(debt);
            await _unitOfWork.SaveChangesAsync();

            return debt;
        }

        public async Task MarkDebtAsPaidAsync(int debtId, int userId, int? shiftId, PaymentMethod paymentMethod = PaymentMethod.Cash)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var debt = await _unitOfWork.CustomerDebts.GetByIdAsync(debtId);
                if (debt == null)
                    throw new ArgumentException("الدين غير موجود");

                var now = DateTime.Now;
                var paymentAmount = debt.RemainingAmount;

                debt.PaidAmount = debt.Amount; // Fully paid
                debt.IsPaid = true;
                debt.PaymentDate = now;
                debt.UpdatedAt = now;
                debt.UserId = userId;
                debt.ShiftId = shiftId;

                // 1. Create a Receipt Voucher (سند قبض) for the settlement
                var voucherNotes = $"سداد دين كامل {debt.CustomerName} - رقم الدين: {debt.Id}" +
                                   (paymentMethod != PaymentMethod.Cash ? $" [طريقة السداد: {paymentMethod}]" : "");

                var voucher = new ReceiptVoucher
                {
                    Amount = paymentAmount,
                    Type = VoucherType.Receipt,
                    PaymentMethod = paymentMethod,
                    CustomerName = debt.CustomerName,
                    VoucherDate = now,
                    Notes = voucherNotes,
                    UserId = userId,
                    CustomerDebtId = debt.Id,
                    Name = $"سداد دين - {debt.CustomerName}",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                voucher.VoucherNumber = $"VCH-DEBT-{debt.Id}-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}";

                await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

                // 2. Create cash box transaction ONLY if Cash payment
                if (paymentMethod == PaymentMethod.Cash)
                {
                    if (!shiftId.HasValue || shiftId.Value <= 0)
                    {
                        shiftId = await _unitOfWork.GetContext().Shifts
                            .AsNoTracking()
                            .Where(s => s.IsActive)
                            .OrderByDescending(s => s.UserId == userId)
                            .Select(s => (int?)s.Id)
                            .FirstOrDefaultAsync();
                    }

                    if (shiftId.HasValue && shiftId.Value > 0)
                    {
                        var transaction = new CashBoxTransaction
                        {
                            Name = $"سداد دين - {debt.CustomerName}",
                            ShiftId = shiftId.Value,
                            UserId = userId,
                            Type = CashBoxTransactionType.Income,
                            Amount = paymentAmount,
                            Description = $"سداد دين كامل - {debt.CustomerName}",
                            CreatedAt = now,
                            UpdatedAt = now,
                            TransactionDate = now
                        };
                        await _unitOfWork.CashBoxTransactions.AddAsync(transaction);
                    }
                }

                _unitOfWork.CustomerDebts.Update(debt);
                await _unitOfWork.SaveChangesAsync();
            });
        }

        public async Task RepayDebtAsync(int debtId, decimal amount, int userId, int? shiftId, string? notes = null, PaymentMethod paymentMethod = PaymentMethod.Cash)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var debt = await _unitOfWork.CustomerDebts.GetByIdAsync(debtId);
                if (debt == null)
                    throw new ArgumentException("الدين غير موجود");

                if (amount <= 0)
                    throw new ArgumentException("مبلغ السداد يجب أن يكون أكبر من الصفر");

                if (amount > debt.RemainingAmount)
                    amount = debt.RemainingAmount;

                var now = DateTime.Now;
                debt.PaidAmount += amount;
                
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    debt.Notes = string.IsNullOrWhiteSpace(debt.Notes) 
                        ? $"[سداد جزئي {amount:N0} ريال: {notes}]" 
                        : $"{debt.Notes}\n[سداد جزئي {amount:N0} ريال: {notes}]";
                }

                if (debt.RemainingAmount <= 0)
                {
                    debt.IsPaid = true;
                    debt.PaymentDate = now;
                }

                debt.UpdatedAt = now;
                debt.UserId = userId;
                debt.ShiftId = shiftId;

                // 1. Create a Receipt Voucher (سند قبض)
                var voucherNotes = $"سداد جزئي دين {debt.CustomerName} - رقم الدين: {debt.Id} {(string.IsNullOrWhiteSpace(notes) ? "" : $" - {notes}")}" +
                                   (paymentMethod != PaymentMethod.Cash ? $" [طريقة السداد: {paymentMethod}]" : "");

                var voucher = new ReceiptVoucher
                {
                    Amount = amount,
                    Type = VoucherType.Receipt,
                    PaymentMethod = paymentMethod,
                    CustomerName = debt.CustomerName,
                    VoucherDate = now,
                    Notes = voucherNotes,
                    UserId = userId,
                    CustomerDebtId = debt.Id,
                    Name = $"سداد دين جزئي - {debt.CustomerName}",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                voucher.VoucherNumber = $"VCH-DEBT-P-{debt.Id}-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}";

                await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

                // 2. Create cash box transaction ONLY if Cash payment
                if (paymentMethod == PaymentMethod.Cash)
                {
                    if (!shiftId.HasValue || shiftId.Value <= 0)
                    {
                        shiftId = await _unitOfWork.GetContext().Shifts
                            .AsNoTracking()
                            .Where(s => s.IsActive)
                            .OrderByDescending(s => s.UserId == userId)
                            .Select(s => (int?)s.Id)
                            .FirstOrDefaultAsync();
                    }

                    if (shiftId.HasValue && shiftId.Value > 0)
                    {
                        var transaction = new CashBoxTransaction
                        {
                            Name = $"سداد دين جزئي - {debt.CustomerName}",
                            ShiftId = shiftId.Value,
                            UserId = userId,
                            Type = CashBoxTransactionType.Income,
                            Amount = amount,
                            Description = $"سداد دين جزئي - {debt.CustomerName}",
                            CreatedAt = now,
                            UpdatedAt = now,
                            TransactionDate = now
                        };
                        await _unitOfWork.CashBoxTransactions.AddAsync(transaction);
                    }
                }

                _unitOfWork.CustomerDebts.Update(debt);
                await _unitOfWork.SaveChangesAsync();
            });
        }

        public async Task DeleteDebtAsync(int debtId)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var debt = await _unitOfWork.CustomerDebts.GetByIdAsync(debtId);
                if (debt == null)
                    throw new ArgumentException("الدين غير موجود");

                var context = _unitOfWork.GetContext();
                var vouchers = await context.ReceiptVouchers.Where(v => v.CustomerDebtId == debtId).ToListAsync();
                // CRITICAL AUDIT FIX: NEVER hard-delete official receipt vouchers!
                // Unlink vouchers and record cancellation audit note so paper trail is preserved
                foreach (var voucher in vouchers)
                {
                    voucher.CustomerDebtId = null;
                    var auditTag = $"[دين مشطوب #{debt.Id} - العميل: {debt.CustomerName}]";
                    voucher.Notes = string.IsNullOrWhiteSpace(voucher.Notes) ? auditTag : $"{voucher.Notes} | {auditTag}";
                    voucher.UpdatedAt = DateTime.Now;
                }
                context.ReceiptVouchers.UpdateRange(vouchers);

                _unitOfWork.CustomerDebts.Remove(debt);
                await _unitOfWork.SaveChangesAsync();
            });
        }

        public async Task<decimal> GetTotalUnpaidDebtsAsync()
        {
            return await _unitOfWork.CustomerDebts.GetTotalUnpaidDebtsAsync();
        }

        public async Task<decimal> GetTotalPaidDebtsAsync()
        {
            return await _unitOfWork.CustomerDebts.GetTotalPaidDebtsAsync();
        }

        public async Task<IEnumerable<CustomerLedgerEntry>> GetCustomerLedgerAsync(int customerId)
        {
            var context = _unitOfWork.GetContext();
            var customer = await context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
            string? custName = customer?.Name;
            string? custPhone = customer?.PhoneNumber;

            var debts = await context.CustomerDebts
                .AsNoTracking()
                .Include(d => d.Customer)
                .Include(d => d.Order)
                    .ThenInclude(o => o.User)
                .Where(d => d.CustomerId == customerId || 
                           (custName != null && d.Customer != null && d.Customer.Name == custName) ||
                           (custPhone != null && d.Customer != null && d.Customer.PhoneNumber == custPhone))
                .ToListAsync();

            if (!debts.Any() && customer == null)
            {
                return Enumerable.Empty<CustomerLedgerEntry>();
            }

            var debtIds = debts.Select(d => d.Id).ToList();
            var orderIds = debts.Where(d => d.OrderId.HasValue).Select(d => d.OrderId!.Value).ToList();

            // 1. Sales on Credit (Debit movements - increase debt)
            var salesEntries = debts.Select(d => new CustomerLedgerEntry
            {
                Date = d.DebtDate != default ? d.DebtDate : (d.CreatedAt != default ? d.CreatedAt : DateTime.Now),
                TransactionType = "فاتورة بيع (آجل)",
                ReferenceNumber = d.Order != null ? d.Order.OrderNumber : (d.OrderId.HasValue ? $"ORD-{d.OrderId}" : $"DEBT-{d.Id}"),
                Debit = d.Amount,
                Credit = 0m,
                Notes = d.Notes,
                CashierName = d.Order?.User?.Name
            }).ToList();

            // 2. Receipt Vouchers / Payments (Credit movements - reduce debt)
            var vouchers = await context.ReceiptVouchers
                .AsNoTracking()
                .Include(v => v.User)
                .Where(v => v.Type == VoucherType.Receipt &&
                           ((v.CustomerDebtId.HasValue && debtIds.Contains(v.CustomerDebtId.Value)) ||
                            (custName != null && v.CustomerName == custName)))
                .ToListAsync();

            var paymentEntries = vouchers.Select(v => new CustomerLedgerEntry
            {
                Date = v.VoucherDate != default ? v.VoucherDate : (v.CreatedAt != default ? v.CreatedAt : DateTime.Now),
                TransactionType = (v.Notes != null && v.Notes.Contains("جزئي")) ? "سداد دفعة جزئية (سند قبض)" : "سداد دين (سند قبض)",
                ReferenceNumber = string.IsNullOrWhiteSpace(v.VoucherNumber) ? $"VCH-{v.Id}" : v.VoucherNumber,
                Debit = 0m,
                Credit = v.Amount,
                Notes = v.Notes,
                CashierName = v.User?.Name
            }).ToList();

            // Audit fallback: synthesize payments for any debt where PaidAmount exceeds vouchers
            var voucherSumByDebt = vouchers.Where(v => v.CustomerDebtId.HasValue)
                .GroupBy(v => v.CustomerDebtId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.Amount));

            foreach (var d in debts)
            {
                decimal vouchered = voucherSumByDebt.TryGetValue(d.Id, out var sum) ? sum : 0m;
                if (d.PaidAmount > vouchered)
                {
                    decimal missing = d.PaidAmount - vouchered;
                    paymentEntries.Add(new CustomerLedgerEntry
                    {
                        Date = d.PaymentDate ?? d.UpdatedAt,
                        TransactionType = "سداد دفعة (سجل دين)",
                        ReferenceNumber = $"PAY-DEBT-{d.Id}",
                        Debit = 0m,
                        Credit = missing,
                        Notes = $"سداد مسجل على الدين #{d.Id}",
                        CashierName = d.Order?.User?.Name
                    });
                }
            }

            // 3. Sales Returns on Credit (Credit movements - reduce debt)
            var returnOrders = orderIds.Any()
                ? await context.ReturnOrders
                    .AsNoTracking()
                    .Include(r => r.Cashier)
                    .Include(r => r.OriginalOrder)
                    .Where(r => orderIds.Contains(r.OriginalOrderId) && r.ReturnMethod == PaymentMethod.OnCredit)
                    .ToListAsync()
                : new List<ReturnOrder>();

            var returnEntries = returnOrders.Select(r => new CustomerLedgerEntry
            {
                Date = r.ReturnDate,
                TransactionType = "مرتجع مبيعات (تسوية دين)",
                ReferenceNumber = string.IsNullOrWhiteSpace(r.ReturnNumber) ? $"RET-{r.Id}" : r.ReturnNumber,
                Debit = 0m,
                Credit = r.TotalAmount,
                Notes = r.Reason,
                CashierName = r.Cashier?.Name
            }).ToList();

            // 4. Chronological sort & running balance calculation
            var sortedEntries = salesEntries
                .Concat(paymentEntries)
                .Concat(returnEntries)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.ReferenceNumber)
                .ToList();

            decimal runningBalance = 0m;
            foreach (var entry in sortedEntries)
            {
                runningBalance += (entry.Debit - entry.Credit);
                entry.Balance = runningBalance;
            }

            sortedEntries.Reverse();
            return sortedEntries;
        }

        public async Task<IEnumerable<CustomerLedgerEntry>> GetCustomerLedgerByNameOrPhoneAsync(string customerNameOrPhone)
        {
            if (string.IsNullOrWhiteSpace(customerNameOrPhone))
                return Enumerable.Empty<CustomerLedgerEntry>();

            var trimmed = customerNameOrPhone.Trim();
            var context = _unitOfWork.GetContext();

            var customer = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == trimmed || (c.PhoneNumber != null && c.PhoneNumber == trimmed));

            if (customer != null)
            {
                return await GetCustomerLedgerAsync(customer.Id);
            }

            // Find debts by matching Customer.Name or Customer.PhoneNumber directly
            var matchingDebts = await context.CustomerDebts
                .AsNoTracking()
                .Include(d => d.Customer)
                .Include(d => d.Order)
                    .ThenInclude(o => o.User)
                .Where(d => (d.Customer != null && d.Customer.Name == trimmed) || 
                           (d.Customer != null && d.Customer.PhoneNumber == trimmed))
                .ToListAsync();

            if (matchingDebts.Any() && matchingDebts.Any(d => d.CustomerId > 0))
            {
                int firstValidCustId = matchingDebts.First(d => d.CustomerId > 0).CustomerId;
                return await GetCustomerLedgerAsync(firstValidCustId);
            }

            if (!matchingDebts.Any())
                return Enumerable.Empty<CustomerLedgerEntry>();

            var debtIds = matchingDebts.Select(d => d.Id).ToList();
            var orderIds = matchingDebts.Where(d => d.OrderId.HasValue).Select(d => d.OrderId!.Value).ToList();

            var salesEntries = matchingDebts.Select(d => new CustomerLedgerEntry
            {
                Date = d.DebtDate != default ? d.DebtDate : (d.CreatedAt != default ? d.CreatedAt : DateTime.Now),
                TransactionType = "فاتورة بيع (آجل)",
                ReferenceNumber = d.OrderId.HasValue ? $"ORD-{d.OrderId}" : $"DEBT-{d.Id}",
                Debit = d.Amount,
                Credit = 0m,
                Notes = d.Notes,
                CashierName = d.Order?.User?.Name
            }).ToList();

            var vouchers = await context.ReceiptVouchers
                .AsNoTracking()
                .Include(v => v.User)
                .Where(v => v.Type == VoucherType.Receipt &&
                           ((v.CustomerDebtId.HasValue && debtIds.Contains(v.CustomerDebtId.Value)) ||
                            v.CustomerName == trimmed))
                .ToListAsync();

            var paymentEntries = vouchers.Select(v => new CustomerLedgerEntry
            {
                Date = v.VoucherDate != default ? v.VoucherDate : (v.CreatedAt != default ? v.CreatedAt : DateTime.Now),
                TransactionType = (v.Notes != null && v.Notes.Contains("جزئي")) ? "سداد دفعة جزئية (سند قبض)" : "سداد دين (سند قبض)",
                ReferenceNumber = string.IsNullOrWhiteSpace(v.VoucherNumber) ? $"VCH-{v.Id}" : v.VoucherNumber,
                Debit = 0m,
                Credit = v.Amount,
                Notes = v.Notes,
                CashierName = v.User?.Name
            }).ToList();

            var voucherSumByDebt = vouchers.Where(v => v.CustomerDebtId.HasValue)
                .GroupBy(v => v.CustomerDebtId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.Amount));

            foreach (var d in matchingDebts)
            {
                decimal vouchered = voucherSumByDebt.TryGetValue(d.Id, out var sum) ? sum : 0m;
                if (d.PaidAmount > vouchered)
                {
                    decimal missing = d.PaidAmount - vouchered;
                    paymentEntries.Add(new CustomerLedgerEntry
                    {
                        Date = d.PaymentDate ?? d.UpdatedAt,
                        TransactionType = "سداد دفعة (سجل دين)",
                        ReferenceNumber = $"PAY-DEBT-{d.Id}",
                        Debit = 0m,
                        Credit = missing,
                        Notes = $"سداد مسجل على الدين #{d.Id}",
                        CashierName = d.Order?.User?.Name
                    });
                }
            }

            var returnOrders = orderIds.Any()
                ? await context.ReturnOrders
                    .AsNoTracking()
                    .Include(r => r.Cashier)
                    .Include(r => r.OriginalOrder)
                    .Where(r => orderIds.Contains(r.OriginalOrderId) && r.ReturnMethod == PaymentMethod.OnCredit)
                    .ToListAsync()
                : new List<ReturnOrder>();

            var returnEntries = returnOrders.Select(r => new CustomerLedgerEntry
            {
                Date = r.ReturnDate,
                TransactionType = "مرتجع مبيعات (تسوية دين)",
                ReferenceNumber = string.IsNullOrWhiteSpace(r.ReturnNumber) ? $"RET-{r.Id}" : r.ReturnNumber,
                Debit = 0m,
                Credit = r.TotalAmount,
                Notes = r.Reason,
                CashierName = r.Cashier?.Name
            }).ToList();

            var sorted = salesEntries.Concat(paymentEntries).Concat(returnEntries)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.ReferenceNumber)
                .ToList();

            decimal runningBal = 0m;
            foreach (var entry in sorted)
            {
                runningBal += (entry.Debit - entry.Credit);
                entry.Balance = runningBal;
            }

            sorted.Reverse();
            return sorted;
        }
    }
}
