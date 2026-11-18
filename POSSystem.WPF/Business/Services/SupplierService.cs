using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace POSSystem.WPF.Business.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinancialChannelService _financialChannelService;

        public SupplierService(
            IUnitOfWork unitOfWork,
            IFinancialChannelService? financialChannelService = null)
        {
            _unitOfWork = unitOfWork;
            _financialChannelService = financialChannelService ?? ServiceLocator.FinancialChannelService;
        }

        public async Task<IEnumerable<Supplier>> GetAllSuppliersAsync()
        {
            return await _unitOfWork.GetContext().Suppliers
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .ToListAsync();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            return await _unitOfWork.Suppliers.GetByIdAsync(id);
        }

        public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
        {
            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new ArgumentException("اسم المورد مطلوب");

            var cleanName = supplier.Name.Trim();
            supplier.Name = cleanName;

            _unitOfWork.ClearEntityTracking();
            var context = _unitOfWork.GetContext();

            var existing = await context.Suppliers
                .FirstOrDefaultAsync(s => s.Name.ToLower() == cleanName.ToLower());

            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.IsActive = true;
                    existing.Name = cleanName;
                    existing.VatNumber = supplier.VatNumber;
                    existing.PhoneNumber = supplier.PhoneNumber;
                    existing.Address = supplier.Address;
                    existing.UpdatedAt = DateTime.Now;
                    context.Suppliers.Update(existing);
                    await _unitOfWork.SaveChangesAsync();
                    return existing;
                }
                else
                {
                    throw new InvalidOperationException($"يوجد مورد نشط مسجل بنفس الاسم ({cleanName}) مسبقاً. يمكنك تعديل بياناته مباشرة من جدول الموردين.");
                }
            }

            await _unitOfWork.Suppliers.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();
            return supplier;
        }

        public async Task UpdateSupplierAsync(Supplier supplier)
        {
            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new ArgumentException("اسم المورد مطلوب");

            var cleanName = supplier.Name.Trim();
            supplier.Name = cleanName;

            _unitOfWork.ClearEntityTracking();
            var context = _unitOfWork.GetContext();

            var duplicate = await context.Suppliers
                .FirstOrDefaultAsync(s => s.Id != supplier.Id && s.Name.ToLower() == cleanName.ToLower());

            if (duplicate != null)
            {
                if (duplicate.IsDeleted)
                {
                    duplicate.Name = $"{duplicate.Name}_محذوف_{duplicate.Id}_{DateTime.Now:HHmmss}";
                    duplicate.UpdatedAt = DateTime.Now;
                    context.Suppliers.Update(duplicate);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    throw new InvalidOperationException($"يوجد مورد آخر نشط مسجل بنفس الاسم ({cleanName}).");
                }
            }

            var tracked = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id);
            if (tracked != null)
            {
                string oldName = tracked.Name?.Trim() ?? string.Empty;
                tracked.Name = cleanName;
                tracked.VatNumber = supplier.VatNumber;
                tracked.PhoneNumber = supplier.PhoneNumber;
                tracked.Address = supplier.Address;
                tracked.IsDeleted = false;
                tracked.IsActive = true;
                tracked.UpdatedAt = DateTime.Now;
                context.Suppliers.Update(tracked);

                // تحديث اسم المورد تلقائياً في كافة فواتير المشتريات والمصروفات والسندات المرتبطة به
                var linkedPurchases = await context.Purchases
                    .Where(p => p.SupplierId == supplier.Id ||
                                (!string.IsNullOrEmpty(oldName) && p.SupplierName != null && p.SupplierName.ToLower() == oldName.ToLower()))
                    .ToListAsync();

                foreach (var p in linkedPurchases)
                {
                    p.SupplierId = supplier.Id;
                    p.SupplierName = cleanName;
                    if (string.IsNullOrWhiteSpace(p.Name) || (!string.IsNullOrEmpty(oldName) && p.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                    {
                        p.Name = cleanName;
                    }
                    p.UpdatedAt = DateTime.Now;
                }

                if (!string.IsNullOrEmpty(oldName) && !oldName.Equals(cleanName, StringComparison.OrdinalIgnoreCase))
                {
                    var linkedExpenses = await context.Expenses
                        .Where(e => e.SupplierName != null && e.SupplierName.ToLower() == oldName.ToLower())
                        .ToListAsync();
                    foreach (var exp in linkedExpenses)
                    {
                        exp.SupplierName = cleanName;
                        exp.UpdatedAt = DateTime.Now;
                    }

                    var linkedVouchers = await context.ReceiptVouchers
                        .Where(v => v.CustomerName != null && v.CustomerName.ToLower() == oldName.ToLower())
                        .ToListAsync();
                    foreach (var v in linkedVouchers)
                    {
                        v.CustomerName = cleanName;
                        v.UpdatedAt = DateTime.Now;
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                return;
            }

            supplier.UpdatedAt = DateTime.Now;
            _unitOfWork.Suppliers.Update(supplier);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteSupplierAsync(int id)
        {
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier != null)
            {
                supplier.IsDeleted = true;
                supplier.UpdatedAt = DateTime.Now;
                _unitOfWork.ClearEntityTracking();
                _unitOfWork.Suppliers.Update(supplier);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task RecordPaymentAsync(SupplierPayment payment, int? shiftId)
        {
            var now = DateTime.Now;
            var context = _unitOfWork.GetContext();

            Shift? targetShift = null;
            if (shiftId.HasValue && shiftId.Value > 0 && payment.PaymentMethod == PaymentMethod.Cash)
            {
                targetShift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId.Value && s.IsActive);
                if (targetShift == null)
                {
                    throw new InvalidOperationException($"المناوبة المحددة (#{shiftId.Value}) غير نشطة أو مغلقة حالياً.");
                }
            }

            var paymentChannel = FinancialChannelHelper.ExtractChannel(payment.PaymentMethod, payment.Notes);
            await _financialChannelService.ValidateSufficientBalanceAsync(paymentChannel, payment.Amount, targetShift?.Id);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _financialChannelService.ValidateSufficientBalanceAsync(paymentChannel, payment.Amount, targetShift?.Id);

                payment.PaymentDate = payment.PaymentDate == default ? now : payment.PaymentDate;
                payment.CreatedAt = now;
                payment.UpdatedAt = now;

                if (payment.PaymentMethod != PaymentMethod.Cash)
                {
                    shiftId = null;
                }

                await _unitOfWork.SupplierPayments.AddAsync(payment);

                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(payment.SupplierId);
                if (supplier != null)
                {
                    supplier.TotalBalance -= payment.Amount;
                    supplier.UpdatedAt = now;
                    _unitOfWork.Suppliers.Update(supplier);
                }

                var voucherNumber = $"PV-SUP-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                var voucher = new ReceiptVoucher
                {
                    VoucherNumber = voucherNumber,
                    Name = $"سند صرف مورد - {supplier?.Name ?? "مورد"}",
                    Type = VoucherType.Payment,
                    PaymentMethod = payment.PaymentMethod,
                    Amount = payment.Amount,
                    CustomerName = supplier?.Name ?? "مورد",
                    VoucherDate = payment.PaymentDate,
                    Notes = string.IsNullOrWhiteSpace(payment.Notes)
                        ? $"دفعة لمورد {supplier?.Name}"
                        : $"دفعة لمورد {supplier?.Name} - {payment.Notes}",
                    UserId = payment.UserId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

                if (targetShift != null && payment.PaymentMethod == PaymentMethod.Cash)
                {
                    var cashTx = new CashBoxTransaction
                    {
                        ShiftId = targetShift.Id,
                        Type = CashBoxTransactionType.Expense,
                        Amount = payment.Amount,
                        Name = $"دفعة لمورد: {supplier?.Name ?? "مورد"}",
                        Description = $"سداد دفعة للمورد ({supplier?.Name}) بموجب سند صرف رقم {voucherNumber}",
                        TransactionDate = now,
                        UserId = payment.UserId,
                        Notes = payment.Notes,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.CashBoxTransactions.AddAsync(cashTx);
                }

                await _unitOfWork.SaveChangesAsync();
            });
        }

        public async Task<IEnumerable<Purchase>> GetSupplierPurchasesAsync(int supplierId)
        {
            return await _unitOfWork.GetContext().Purchases
                .AsNoTracking()
                .Include(p => p.PurchaseItems)
                .ThenInclude(pi => pi.Product)
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SupplierPayment>> GetSupplierPaymentsAsync(int supplierId)
        {
            return await _unitOfWork.GetContext().SupplierPayments
                .AsNoTracking()
                .Where(sp => sp.SupplierId == supplierId)
                .OrderByDescending(sp => sp.PaymentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseReturn>> GetSupplierReturnsAsync(int supplierId)
        {
            return await _unitOfWork.GetContext().PurchaseReturns
                .AsNoTracking()
                .Include(pr => pr.OriginalPurchase)
                .Include(pr => pr.PurchaseReturnItems)
                .Where(pr => pr.OriginalPurchase != null && pr.OriginalPurchase.SupplierId == supplierId)
                .OrderByDescending(pr => pr.ReturnDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Supplier>> GetSuppliersWithDebtAsync()
        {
            return await _unitOfWork.GetContext().Suppliers
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.TotalBalance > 0)
                .OrderByDescending(s => (double)s.TotalBalance)
                .ToListAsync();
        }

        public async Task<IEnumerable<SupplierLedgerEntry>> GetSupplierLedgerAsync(int supplierId)
        {
            var purchases = await GetSupplierPurchasesAsync(supplierId);
            var payments = await GetSupplierPaymentsAsync(supplierId);
            var returns = await GetSupplierReturnsAsync(supplierId);

            var entries = new List<(SupplierLedgerEntry Entry, int OrderSeq)>();
            int seq = 0;

            foreach (var p in purchases.Where(x => x.PaymentMethod == PaymentMethod.OnCredit).OrderBy(x => x.PurchaseDate).ThenBy(x => x.Id))
            {
                entries.Add((new SupplierLedgerEntry
                {
                    Date = p.PurchaseDate,
                    TransactionType = "فاتورة مشتريات آجلة",
                    ReferenceNumber = p.PurchaseNumber,
                    Debit = 0,
                    Credit = p.TotalAmount,
                    Notes = p.Notes ?? string.Empty
                }, ++seq));
            }

            foreach (var pay in payments.OrderBy(x => x.PaymentDate).ThenBy(x => x.Id))
            {
                entries.Add((new SupplierLedgerEntry
                {
                    Date = pay.PaymentDate,
                    TransactionType = "سند صرف / سداد دفعة",
                    ReferenceNumber = $"PAY-{pay.Id}",
                    Debit = pay.Amount,
                    Credit = 0,
                    Notes = pay.Notes ?? string.Empty
                }, ++seq));
            }

            foreach (var ret in returns
                .Where(r => r.SettlementMode != PurchaseReturnSettlementMode.CashReceipt &&
                            r.ReturnMethod != PaymentMethod.Cash &&
                            (r.OriginalPurchase == null || r.OriginalPurchase.PaymentMethod == PaymentMethod.OnCredit))
                .OrderBy(x => x.ReturnDate).ThenBy(x => x.Id))
            {
                entries.Add((new SupplierLedgerEntry
                {
                    Date = ret.ReturnDate,
                    TransactionType = "مرتجع مشتريات آجل (إشعار مدين)",
                    ReferenceNumber = ret.ReturnNumber,
                    Debit = ret.TotalAmount,
                    Credit = 0,
                    Notes = ret.Reason ?? string.Empty
                }, ++seq));
            }

            var ordered = entries.OrderBy(e => e.Entry.Date).ThenBy(e => e.OrderSeq).ToList();
            decimal runningBalance = 0;
            foreach (var item in ordered)
            {
                runningBalance += (item.Entry.Credit - item.Entry.Debit);
                item.Entry.Balance = runningBalance;
            }

            return ordered
                .OrderByDescending(e => e.Entry.Date)
                .ThenByDescending(e => e.OrderSeq)
                .Select(e => e.Entry)
                .ToList();
        }
    }
}
