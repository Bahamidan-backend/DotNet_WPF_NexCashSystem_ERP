using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services.Logging;

namespace POSSystem.WPF.Business.Services
{
    public class FinancialAccountService : IFinancialAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FinancialAccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task EnsureDefaultAccountsSeededAsync()
        {
            var context = _unitOfWork.GetContext();
            var existing = await context.FinancialAccounts.ToListAsync();
            var now = DateTime.Now;
            bool changed = false;

            // 1. Migrate legacy seeded names ("حساب بنك الكريمي" -> "حاسب كريمي", remove unused "حساب بنك القطيبي")
            var oldKuraimi = existing.FirstOrDefault(a => a.AccountName == "حساب بنك الكريمي");
            if (oldKuraimi != null)
            {
                if (!existing.Any(a => a.AccountName == "حاسب كريمي"))
                {
                    oldKuraimi.AccountName = "حاسب كريمي";
                    oldKuraimi.Name = "حاسب كريمي";
                    oldKuraimi.BankOrWalletName = "حاسب كريمي";
                    oldKuraimi.AccountType = FinancialAccountType.ElectronicWallet;
                    context.FinancialAccounts.Update(oldKuraimi);
                    changed = true;
                }
            }

            var oldQutaibi = existing.FirstOrDefault(a => a.AccountName == "حساب بنك القطيبي");
            if (oldQutaibi != null)
            {
                bool hasTxs = await context.FinancialAccountTransactions.AnyAsync(t => t.FinancialAccountId == oldQutaibi.Id);
                if (!hasTxs && oldQutaibi.CurrentBalance == 0)
                {
                    context.FinancialAccounts.Remove(oldQutaibi);
                    existing.Remove(oldQutaibi);
                    changed = true;
                }
            }

            // 2. Ensure MainCashBox exists
            if (!existing.Any(a => a.AccountType == FinancialAccountType.MainCashBox))
            {
                var cashAcc = new FinancialAccount
                {
                    AccountName = "الصندوق الرئيسي",
                    Name = "الصندوق الرئيسي",
                    AccountType = FinancialAccountType.MainCashBox,
                    BankOrWalletName = "صندوق الكاشير النقدي",
                    Currency = "ريال يمني",
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await context.FinancialAccounts.AddAsync(cashAcc);
                existing.Add(cashAcc);
                changed = true;
            }

            // 3. Ensure all 7 Unified Wallets + any custom wallets in Settings exist
            var desiredWallets = new List<string>(FinancialChannelHelper.UnifiedWallets);
            try
            {
                var settings = POSSystem.WPF.Services.SettingsService.LoadSettings();
                if (settings?.CustomWallets != null)
                {
                    foreach (var cw in settings.CustomWallets)
                    {
                        if (!string.IsNullOrWhiteSpace(cw) && !desiredWallets.Any(w => w.Equals(cw.Trim(), StringComparison.OrdinalIgnoreCase)))
                            desiredWallets.Add(cw.Trim());
                    }
                }
            }
            catch { }

            foreach (var walletName in desiredWallets)
            {
                if (!existing.Any(a => a.AccountName.Equals(walletName, StringComparison.OrdinalIgnoreCase) ||
                                       a.BankOrWalletName.Equals(walletName, StringComparison.OrdinalIgnoreCase)))
                {
                    var newAcc = new FinancialAccount
                    {
                        AccountName = walletName,
                        Name = walletName,
                        AccountType = FinancialAccountType.ElectronicWallet,
                        BankOrWalletName = walletName,
                        Currency = "ريال يمني",
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await context.FinancialAccounts.AddAsync(newAcc);
                    existing.Add(newAcc);
                    changed = true;
                }
            }

            // Auto-heal any existing Purchase Return ReceiptVouchers (RV-PRET-) that were saved with PaymentMethod=Cash when the original purchase was paid via an electronic wallet (e.g. حاسب كريمي)
            try
            {
                var pretVouchers = await context.ReceiptVouchers
                    .Where(v => v.Type == VoucherType.Receipt && v.PaymentMethod == PaymentMethod.Cash && v.VoucherNumber != null && v.VoucherNumber.StartsWith("RV-PRET-"))
                    .ToListAsync();

                if (pretVouchers.Count > 0)
                {
                    var allPretReturns = await context.PurchaseReturns
                        .Include(pr => pr.OriginalPurchase)
                        .ToListAsync();

                    foreach (var rv in pretVouchers)
                    {
                        var matchedReturn = allPretReturns.FirstOrDefault(pr =>
                            !string.IsNullOrWhiteSpace(pr.ReturnNumber) &&
                            (rv.VoucherNumber!.Contains(pr.ReturnNumber) || (rv.Notes != null && rv.Notes.Contains(pr.ReturnNumber))));

                        var origPurchase = matchedReturn?.OriginalPurchase;
                        if (origPurchase != null && origPurchase.PaymentMethod != PaymentMethod.Cash && origPurchase.PaymentMethod != PaymentMethod.OnCredit)
                        {
                            string? walletName = origPurchase.DepositInfo?.BankOrWallet;
                            if (string.IsNullOrWhiteSpace(walletName) && !string.IsNullOrWhiteSpace(origPurchase.Notes))
                            {
                                foreach (var w in FinancialChannelHelper.UnifiedWallets)
                                {
                                    if (origPurchase.Notes.Contains(w, StringComparison.OrdinalIgnoreCase))
                                    {
                                        walletName = w;
                                        break;
                                    }
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(walletName))
                            {
                                rv.PaymentMethod = PaymentMethod.MobilePayment;
                                string depTag = OrderDepositHelper.FormatDepositTag("ريال يمني", rv.Amount, walletName, matchedReturn!.ReturnNumber);
                                rv.Notes = $"{depTag} {rv.Notes}";
                                context.ReceiptVouchers.Update(rv);
                                changed = true;
                            }
                        }
                    }
                }
            }
            catch { }

            if (changed)
            {
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<FinancialAccount>> GetAllAccountsAsync(bool includeInactive = true)
        {
            await EnsureDefaultAccountsSeededAsync();
            var context = _unitOfWork.GetContext();
            var query = context.FinancialAccounts.AsNoTracking();
            if (!includeInactive)
                query = query.Where(a => a.IsActive);

            var accounts = await query
                .OrderBy(a => a.AccountType)
                .ThenBy(a => a.Id)
                .ToListAsync();

            // Pre-fetch shared tables ONCE across all accounts instead of 8x6=48 separate queries
            var allDirectTxs = await context.FinancialAccountTransactions
                .AsNoTracking()
                .Include(t => t.RelatedAccount)
                .ToListAsync();
            var firstShift = await context.Shifts.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
            var allCashTxs = await context.CashBoxTransactions.AsNoTracking().OrderBy(t => t.TransactionDate).ToListAsync();
            var allCandidateOrders = await context.Orders
                .AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled &&
                            (o.PaymentMethod == PaymentMethod.MobilePayment ||
                             o.PaymentMethod == PaymentMethod.BankTransfer ||
                             o.PaymentMethod == PaymentMethod.DebitCard ||
                             o.PaymentMethod == PaymentMethod.CreditCard ||
                             o.PaymentMethod == PaymentMethod.Split ||
                             (o.Notes != null && o.Notes.Contains("[إيداع:"))))
                .ToListAsync();
            var allBankDeposits = await context.BankDeposits.AsNoTracking().Where(b => b.Status != "Rejected").ToListAsync();
            var allNonCashVouchers = await context.ReceiptVouchers.AsNoTracking().Where(v => v.PaymentMethod != PaymentMethod.Cash).ToListAsync();
            var allElectronicReturns = await context.ReturnOrders
                .AsNoTracking()
                .Where(r => r.ReturnMethod != PaymentMethod.Cash &&
                            r.ReturnMethod != PaymentMethod.OnCredit &&
                            r.ReturnMethod != PaymentMethod.Remake)
                .ToListAsync();

            foreach (var acc in accounts)
            {
                var statementRows = BuildCombinedStatementRowsInMemory(
                    acc,
                    allDirectTxs.Where(t => t.FinancialAccountId == acc.Id).ToList(),
                    firstShift,
                    allCashTxs,
                    allCandidateOrders,
                    allBankDeposits,
                    allNonCashVouchers,
                    allElectronicReturns,
                    null,
                    null);

                if (statementRows.Count > 0)
                {
                    decimal dynamicIn = statementRows.Sum(r => r.AmountIn);
                    decimal dynamicOut = statementRows.Sum(r => r.AmountOut);
                    acc.TotalInflow = dynamicIn;
                    acc.TotalOutflow = dynamicOut;
                    acc.CurrentBalance = statementRows.First().BalanceAfter;
                    acc.LastTransactionDate = statementRows.Max(r => r.TransactionDate);
                }
                else
                {
                    acc.TotalInflow = 0m;
                    acc.TotalOutflow = 0m;
                    acc.CurrentBalance = 0m;
                }
            }

            return accounts;
        }

        public async Task<FinancialAccount?> GetAccountByIdAsync(int id)
        {
            var context = _unitOfWork.GetContext();
            return await context.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<FinancialAccount?> FindAccountByNameOrBankAsync(string? nameOrBank)
        {
            if (string.IsNullOrWhiteSpace(nameOrBank)) return null;
            await EnsureDefaultAccountsSeededAsync();

            var trimmed = nameOrBank.Trim();
            var context = _unitOfWork.GetContext();
            var all = await context.FinancialAccounts.Where(a => a.IsActive).ToListAsync();

            var exact = all.FirstOrDefault(a =>
                a.AccountName.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                a.BankOrWalletName.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            var normalizedTarget = FinancialChannelHelper.NormalizeChannelKey(trimmed);
            return all.FirstOrDefault(a =>
                FinancialChannelHelper.NormalizeChannelKey(a.BankOrWalletName) == normalizedTarget ||
                FinancialChannelHelper.NormalizeChannelKey(a.AccountName) == normalizedTarget);
        }

        public async Task<FinancialAccount> CreateAccountAsync(FinancialAccount account, int? userId = null, string? userName = null)
        {
            if (string.IsNullOrWhiteSpace(account.AccountName))
                throw new ArgumentException("اسم الحساب المالي مطلوب.");

            if (string.IsNullOrWhiteSpace(account.BankOrWalletName))
                account.BankOrWalletName = account.AccountName.Trim();

            var context = _unitOfWork.GetContext();
            bool duplicate = await context.FinancialAccounts.AnyAsync(a =>
                a.AccountName.ToLower() == account.AccountName.Trim().ToLower());
            if (duplicate)
                throw new InvalidOperationException("يوجد حساب مالي مسجل بنفس الاسم مسبقاً.");

            var now = DateTime.Now;
            account.AccountName = account.AccountName.Trim();
            account.BankOrWalletName = account.BankOrWalletName.Trim();
            account.Name = account.AccountName;
            account.CreatedAt = now;
            account.UpdatedAt = now;
            account.IsActive = true;

            decimal openingBalance = account.CurrentBalance;
            account.CurrentBalance = 0m;
            account.TotalInflow = 0m;
            account.TotalOutflow = 0m;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await context.FinancialAccounts.AddAsync(account);
                await _unitOfWork.SaveChangesAsync();

                if (openingBalance > 0)
                {
                    account.CurrentBalance = openingBalance;
                    account.TotalInflow = openingBalance;
                    account.LastTransactionDate = now;
                    context.FinancialAccounts.Update(account);

                    await context.FinancialAccountTransactions.AddAsync(new FinancialAccountTransaction
                    {
                        FinancialAccountId = account.Id,
                        TransactionType = FinancialTransactionType.Deposit,
                        AmountIn = openingBalance,
                        AmountOut = 0m,
                        BalanceAfter = openingBalance,
                        ReferenceNumber = $"OPEN-{account.Id}",
                        SourceEntityType = "OpeningBalance",
                        SourceEntityId = account.Id,
                        PerformedByUserId = userId,
                        PerformedByUserName = userName ?? "مدير النظام",
                        TransactionDate = now,
                        Name = "رصيد افتتاحي للحساب",
                        Description = $"رصيد افتتاحي عند إنشاء الحساب ({account.AccountName})",
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsActive = true
                    });
                    await _unitOfWork.SaveChangesAsync();
                }
            });

            AppLogger.LogAudit(userId?.ToString() ?? "System", "CREATE_FINANCIAL_ACCOUNT", $"Created account '{account.AccountName}' ({account.AccountTypeText})");
            return account;
        }

        public async Task<FinancialAccount> UpdateAccountAsync(FinancialAccount account, int? userId = null, string? userName = null)
        {
            var context = _unitOfWork.GetContext();
            var existing = await context.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == account.Id);
            if (existing == null)
                throw new ArgumentException("الحساب المالي غير موجود.");

            if (string.IsNullOrWhiteSpace(account.AccountName))
                throw new ArgumentException("اسم الحساب المالي مطلوب.");

            // Do not allow direct balance modification without a financial transaction
            existing.AccountName = account.AccountName.Trim();
            existing.Name = existing.AccountName;
            existing.AccountType = account.AccountType;
            existing.BankOrWalletName = string.IsNullOrWhiteSpace(account.BankOrWalletName) ? existing.AccountName : account.BankOrWalletName.Trim();
            existing.AccountNumber = account.AccountNumber?.Trim();
            existing.Currency = string.IsNullOrWhiteSpace(account.Currency) ? "ريال يمني" : account.Currency.Trim();
            existing.Description = account.Description;
            existing.AllowOverdraft = account.AllowOverdraft;
            existing.IsActive = account.IsActive;
            existing.UpdatedAt = DateTime.Now;

            context.FinancialAccounts.Update(existing);
            await _unitOfWork.SaveChangesAsync();

            AppLogger.LogAudit(userId?.ToString() ?? "System", "UPDATE_FINANCIAL_ACCOUNT", $"Updated account #{existing.Id} '{existing.AccountName}'");
            return existing;
        }

        public async Task ToggleAccountStatusAsync(int accountId, bool isActive, int? userId = null, string? userName = null)
        {
            var context = _unitOfWork.GetContext();
            var account = await context.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == accountId);
            if (account == null)
                throw new ArgumentException("الحساب المالي غير موجود.");

            account.IsActive = isActive;
            account.UpdatedAt = DateTime.Now;
            context.FinancialAccounts.Update(account);
            await _unitOfWork.SaveChangesAsync();

            AppLogger.LogAudit(userId?.ToString() ?? "System", "TOGGLE_FINANCIAL_ACCOUNT", $"Account #{account.Id} '{account.AccountName}' active={isActive}");
        }

        private async Task SyncAccountLiveBalanceAsync(FinancialAccount acc)
        {
            if (acc == null) return;
            var rows = await BuildCombinedStatementRowsAsync(acc, null, null);
            if (rows.Count > 0)
            {
                acc.TotalInflow = rows.Sum(r => r.AmountIn);
                acc.TotalOutflow = rows.Sum(r => r.AmountOut);
                acc.CurrentBalance = rows.First().BalanceAfter;
                acc.LastTransactionDate = rows.Max(r => r.TransactionDate);
            }
            else
            {
                acc.TotalInflow = 0m;
                acc.TotalOutflow = 0m;
                acc.CurrentBalance = 0m;
            }
        }

        public async Task<FinancialAccountTransaction> RecordDepositAsync(
            int accountId,
            decimal amount,
            string? referenceNumber,
            string description,
            string? notes,
            int? userId = null,
            string? userName = null,
            string? sourceType = "Manual",
            int? sourceId = null,
            FinancialTransactionType txType = FinancialTransactionType.Deposit)
        {
            if (amount <= 0)
                throw new ArgumentException("مبلغ الإيداع يجب أن يكون أكبر من الصفر.");

            var context = _unitOfWork.GetContext();
            FinancialAccountTransaction createdTx = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var account = await context.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == accountId);
                if (account == null)
                    throw new InvalidOperationException("الحساب المالي غير موجود.");
                if (!account.IsActive)
                    throw new InvalidOperationException($"الحساب '{account.AccountName}' معطل حالياً ولا يقبل حركات جديدة.");

                // Prevent duplicate transaction if sourceType + sourceId or non-empty referenceNumber on same account already exists
                if (sourceId.HasValue && sourceId.Value > 0 && !string.IsNullOrWhiteSpace(sourceType) && sourceType != "Manual")
                {
                    bool duplicateSource = await context.FinancialAccountTransactions.AnyAsync(t =>
                        t.FinancialAccountId == accountId &&
                        t.SourceEntityType == sourceType &&
                        t.SourceEntityId == sourceId.Value &&
                        t.TransactionType == txType);
                    if (duplicateSource)
                        throw new InvalidOperationException("تم تسجيل هذه الحركة المالية مسبقاً لنفس المستند الأصلي (منع التكرار المالي).");
                }

                await SyncAccountLiveBalanceAsync(account);
                var now = DateTime.Now;
                account.CurrentBalance += amount;
                account.TotalInflow += amount;
                account.LastTransactionDate = now;
                account.UpdatedAt = now;
                context.FinancialAccounts.Update(account);

                createdTx = new FinancialAccountTransaction
                {
                    FinancialAccountId = account.Id,
                    TransactionType = txType,
                    AmountIn = amount,
                    AmountOut = 0m,
                    BalanceAfter = account.CurrentBalance,
                    ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? $"DEP-{now:yyyyMMddHHmmss}" : referenceNumber.Trim(),
                    SourceEntityType = sourceType,
                    SourceEntityId = sourceId,
                    PerformedByUserId = userId,
                    PerformedByUserName = string.IsNullOrWhiteSpace(userName) ? "مدير النظام" : userName,
                    TransactionDate = now,
                    Name = string.IsNullOrWhiteSpace(description) ? "إيداع مالي" : description.Trim(),
                    Description = string.IsNullOrWhiteSpace(description) ? "إيداع مالي" : description.Trim(),
                    Notes = notes,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                await context.FinancialAccountTransactions.AddAsync(createdTx);

                // If depositing into MainCashBox while a shift is active, also record in CashBoxTransactions if manual
                if (account.AccountType == FinancialAccountType.MainCashBox && sourceType == "Manual")
                {
                    var activeShift = await context.Shifts.FirstOrDefaultAsync(s => s.IsActive);
                    if (activeShift != null)
                    {
                        await context.CashBoxTransactions.AddAsync(new CashBoxTransaction
                        {
                            ShiftId = activeShift.Id,
                            UserId = userId ?? activeShift.UserId,
                            Type = CashBoxTransactionType.Income,
                            Amount = amount,
                            Name = createdTx.Name,
                            Description = $"{createdTx.Description} (مرجع: {createdTx.ReferenceNumber})",
                            Notes = notes,
                            TransactionDate = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsActive = true
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            });

            FinancialChannelService.InvalidateCache();
            AppLogger.LogAudit(userId?.ToString() ?? "System", "ACCOUNT_DEPOSIT", $"Deposited {amount:N0} into account #{accountId}");
            return createdTx;
        }

        public async Task<FinancialAccountTransaction> RecordWithdrawalAsync(
            int accountId,
            decimal amount,
            string? referenceNumber,
            string description,
            string? notes,
            int? userId = null,
            string? userName = null,
            string? sourceType = "Manual",
            int? sourceId = null,
            FinancialTransactionType txType = FinancialTransactionType.Withdrawal,
            bool allowNegativeOverride = false)
        {
            if (amount <= 0)
                throw new ArgumentException("مبلغ السحب يجب أن يكون أكبر من الصفر.");

            var context = _unitOfWork.GetContext();
            FinancialAccountTransaction createdTx = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var account = await context.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == accountId);
                if (account == null)
                    throw new InvalidOperationException("الحساب المالي غير موجود.");
                if (!account.IsActive)
                    throw new InvalidOperationException($"الحساب '{account.AccountName}' معطل حالياً ولا يقبل حركات جديدة.");

                await SyncAccountLiveBalanceAsync(account);
                if (!account.AllowOverdraft && !allowNegativeOverride && account.CurrentBalance < amount)
                {
                    throw new InvalidOperationException(
                        $"الرصيد المتاح في الحساب '{account.AccountName}' ({account.CurrentBalance:N0} {account.Currency}) غير كافٍ لسحب مبلغ ({amount:N0} {account.Currency}).");
                }

                if (sourceId.HasValue && sourceId.Value > 0 && !string.IsNullOrWhiteSpace(sourceType) && sourceType != "Manual")
                {
                    bool duplicateSource = await context.FinancialAccountTransactions.AnyAsync(t =>
                        t.FinancialAccountId == accountId &&
                        t.SourceEntityType == sourceType &&
                        t.SourceEntityId == sourceId.Value &&
                        t.TransactionType == txType);
                    if (duplicateSource)
                        throw new InvalidOperationException("تم خصم هذه العملية مسبقاً من الحساب (منع الخصم المزدوج).");
                }

                var now = DateTime.Now;
                account.CurrentBalance -= amount;
                account.TotalOutflow += amount;
                account.LastTransactionDate = now;
                account.UpdatedAt = now;
                context.FinancialAccounts.Update(account);

                createdTx = new FinancialAccountTransaction
                {
                    FinancialAccountId = account.Id,
                    TransactionType = txType,
                    AmountIn = 0m,
                    AmountOut = amount,
                    BalanceAfter = account.CurrentBalance,
                    ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? $"WTH-{now:yyyyMMddHHmmss}" : referenceNumber.Trim(),
                    SourceEntityType = sourceType,
                    SourceEntityId = sourceId,
                    PerformedByUserId = userId,
                    PerformedByUserName = string.IsNullOrWhiteSpace(userName) ? "مدير النظام" : userName,
                    TransactionDate = now,
                    Name = string.IsNullOrWhiteSpace(description) ? "سحب مالي" : description.Trim(),
                    Description = string.IsNullOrWhiteSpace(description) ? "سحب مالي" : description.Trim(),
                    Notes = notes,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                await context.FinancialAccountTransactions.AddAsync(createdTx);

                if (account.AccountType == FinancialAccountType.MainCashBox && sourceType == "Manual")
                {
                    var activeShift = await context.Shifts.FirstOrDefaultAsync(s => s.IsActive);
                    if (activeShift != null)
                    {
                        await context.CashBoxTransactions.AddAsync(new CashBoxTransaction
                        {
                            ShiftId = activeShift.Id,
                            UserId = userId ?? activeShift.UserId,
                            Type = CashBoxTransactionType.Expense,
                            Amount = amount,
                            Name = createdTx.Name,
                            Description = $"{createdTx.Description} (مرجع: {createdTx.ReferenceNumber})",
                            Notes = notes,
                            TransactionDate = now,
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsActive = true
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            });

            FinancialChannelService.InvalidateCache();
            AppLogger.LogAudit(userId?.ToString() ?? "System", "ACCOUNT_WITHDRAWAL", $"Withdrew {amount:N0} from account #{accountId}");
            return createdTx;
        }

        public async Task<(FinancialAccountTransaction OutTx, FinancialAccountTransaction InTx)> TransferBetweenAccountsAsync(
            int fromAccountId,
            int toAccountId,
            decimal amount,
            string? referenceNumber,
            string? notes,
            int? userId = null,
            string? userName = null,
            bool isOwnerDeposit = false)
        {
            if (fromAccountId == toAccountId)
                throw new ArgumentException("لا يمكن التحويل من الحساب إلى نفس الحساب.");
            if (amount <= 0)
                throw new ArgumentException("مبلغ التحويل يجب أن يكون أكبر من الصفر.");

            var context = _unitOfWork.GetContext();
            FinancialAccountTransaction outTx = null!;
            FinancialAccountTransaction inTx = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var fromAcc = await context.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == fromAccountId);
                var toAcc = await context.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == toAccountId);

                if (fromAcc == null || toAcc == null)
                    throw new InvalidOperationException("أحد طرفي التحويل غير موجود.");
                if (!fromAcc.IsActive || !toAcc.IsActive)
                    throw new InvalidOperationException("يجب أن يكون الحساب المحول منه والمحول إليه نشطين.");

                await SyncAccountLiveBalanceAsync(fromAcc);
                await SyncAccountLiveBalanceAsync(toAcc);

                // If transferring from MainCashBox, also check shift cashbox balance if account balance wasn't initialized
                var activeShift = await context.Shifts.FirstOrDefaultAsync(s => s.IsActive);
                if (fromAcc.AccountType == FinancialAccountType.MainCashBox && activeShift != null)
                {
                    var drawerBalance = await ServiceLocator.CashBoxTransactionService.GetCashBoxBalanceAsync(activeShift.Id);
                    if (fromAcc.CurrentBalance == 0 && fromAcc.TotalInflow == 0 && drawerBalance >= amount)
                    {
                        fromAcc.CurrentBalance = drawerBalance;
                        fromAcc.TotalInflow = drawerBalance;
                    }
                }

                if (!fromAcc.AllowOverdraft && fromAcc.CurrentBalance < amount)
                {
                    throw new InvalidOperationException(
                        $"الرصيد المتاح في '{fromAcc.AccountName}' ({fromAcc.CurrentBalance:N0} ريال) غير كافٍ لتحويل ({amount:N0} ريال).");
                }

                var now = DateTime.Now;
                string refNo = string.IsNullOrWhiteSpace(referenceNumber)
                    ? $"TRF-{now:yyyyMMddHHmmss}"
                    : referenceNumber.Trim();

                fromAcc.CurrentBalance -= amount;
                fromAcc.TotalOutflow += amount;
                fromAcc.LastTransactionDate = now;
                fromAcc.UpdatedAt = now;

                toAcc.CurrentBalance += amount;
                toAcc.TotalInflow += amount;
                toAcc.LastTransactionDate = now;
                toAcc.UpdatedAt = now;

                context.FinancialAccounts.Update(fromAcc);
                context.FinancialAccounts.Update(toAcc);

                var outType = isOwnerDeposit ? FinancialTransactionType.OwnerBankDeposit : FinancialTransactionType.TransferOut;
                var inType = isOwnerDeposit ? FinancialTransactionType.OwnerBankDeposit : FinancialTransactionType.TransferIn;
                string label = isOwnerDeposit
                    ? $"توريد/إيداع من {fromAcc.AccountName} إلى {toAcc.AccountName} (تحويل داخلي)"
                    : $"تحويل مالي من {fromAcc.AccountName} إلى {toAcc.AccountName}";

                outTx = new FinancialAccountTransaction
                {
                    FinancialAccountId = fromAcc.Id,
                    RelatedAccountId = toAcc.Id,
                    TransactionType = outType,
                    AmountIn = 0m,
                    AmountOut = amount,
                    BalanceAfter = fromAcc.CurrentBalance,
                    ReferenceNumber = refNo,
                    SourceEntityType = isOwnerDeposit ? "OwnerBankDeposit" : "InternalTransfer",
                    PerformedByUserId = userId,
                    PerformedByUserName = string.IsNullOrWhiteSpace(userName) ? "مدير النظام" : userName,
                    TransactionDate = now,
                    Name = label,
                    Description = label,
                    Notes = notes,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                inTx = new FinancialAccountTransaction
                {
                    FinancialAccountId = toAcc.Id,
                    RelatedAccountId = fromAcc.Id,
                    TransactionType = inType,
                    AmountIn = amount,
                    AmountOut = 0m,
                    BalanceAfter = toAcc.CurrentBalance,
                    ReferenceNumber = refNo,
                    SourceEntityType = isOwnerDeposit ? "OwnerBankDeposit" : "InternalTransfer",
                    PerformedByUserId = userId,
                    PerformedByUserName = string.IsNullOrWhiteSpace(userName) ? "مدير النظام" : userName,
                    TransactionDate = now,
                    Name = label,
                    Description = label,
                    Notes = notes,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                await context.FinancialAccountTransactions.AddRangeAsync(outTx, inTx);

                // If source is MainCashBox and active shift exists, deduct from CashBoxTransactions as BankDeposit (not Expense/Revenue)
                if (fromAcc.AccountType == FinancialAccountType.MainCashBox && activeShift != null)
                {
                    await context.CashBoxTransactions.AddAsync(new CashBoxTransaction
                    {
                        ShiftId = activeShift.Id,
                        UserId = userId ?? activeShift.UserId,
                        Type = CashBoxTransactionType.BankDeposit,
                        Amount = amount,
                        Name = label,
                        Description = $"{label} [تحويل داخلي مرجع: {refNo}]",
                        Notes = $"[تحويل داخلي] {notes}".Trim(),
                        TransactionDate = now,
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsActive = true
                    });
                }

                // If destination is a BankAccount and isOwnerDeposit, record in BankDeposits tagged as [تحويل داخلي من الصندوق] so reconciliation separates it from customer sales deposits
                if (toAcc.AccountType == FinancialAccountType.BankAccount && (isOwnerDeposit || fromAcc.AccountType == FinancialAccountType.MainCashBox))
                {
                    await context.BankDeposits.AddAsync(new BankDeposit
                    {
                        DepositDate = now.Date,
                        Amount = amount,
                        DepositorName = string.IsNullOrWhiteSpace(userName) ? "صاحب المطعم / الإدارة (من الصندوق)" : $"{userName} (تحويل من الصندوق)",
                        Source = toAcc.BankOrWalletName,
                        ReferenceNumber = refNo,
                        Status = "Approved",
                        CreatedBy = string.IsNullOrWhiteSpace(userName) ? "مدير النظام" : userName,
                        Notes = $"[تحويل داخلي من الصندوق] {notes}".Trim(),
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                await _unitOfWork.SaveChangesAsync();
            });

            FinancialChannelService.InvalidateCache();
            AppLogger.LogAudit(userId?.ToString() ?? "System", "ACCOUNT_TRANSFER", $"Transferred {amount:N0} from #{fromAccountId} to #{toAccountId} (OwnerDeposit={isOwnerDeposit})");
            return (outTx, inTx);
        }

        public async Task<List<FinancialAccountTransaction>> GetAccountStatementAsync(int accountId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var context = _unitOfWork.GetContext();
            var acc = await context.FinancialAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);
            if (acc == null) return new List<FinancialAccountTransaction>();

            return await BuildCombinedStatementRowsAsync(acc, startDate, endDate);
        }

        public static bool IsInternalTransferMirrorRow(string? description, string? notes)
        {
            if (!string.IsNullOrWhiteSpace(description) &&
                (description.Contains("(مرجع: TRF-", StringComparison.OrdinalIgnoreCase) ||
                 description.Contains("[تحويل داخلي", StringComparison.OrdinalIgnoreCase) ||
                 description.Contains("مرجع: TRF-", StringComparison.OrdinalIgnoreCase) ||
                 description.Contains("مرجع: DEP-", StringComparison.OrdinalIgnoreCase) ||
                 description.Contains("مرجع: WTH-", StringComparison.OrdinalIgnoreCase) ||
                 description.Contains("تحويل مالي من", StringComparison.OrdinalIgnoreCase) ||
                 description.Contains("توريد/إيداع من", StringComparison.OrdinalIgnoreCase)))
                return true;

            if (!string.IsNullOrWhiteSpace(notes) &&
                (notes.Contains("[تحويل داخلي", StringComparison.OrdinalIgnoreCase) ||
                 notes.Contains("تحويل داخلي من الصندوق", StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        private static bool MatchesAccountChannel(FinancialAccount acc, string? rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return false;
            var s = rawText.Trim();
            if (!string.IsNullOrWhiteSpace(acc.BankOrWalletName) && s.Contains(acc.BankOrWalletName.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(acc.AccountName) && s.Contains(acc.AccountName.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            bool accIsKuraimi = acc.AccountName.Contains("كريمي") || acc.BankOrWalletName.Contains("كريمي");
            bool accIsJeeb = acc.AccountName.Contains("جيب") || acc.BankOrWalletName.Contains("جيب");
            if (accIsKuraimi && !accIsJeeb)
                return s.Contains("كريمي") && !s.Contains("جيب");
            if (accIsJeeb)
                return s.Contains("جيب");

            bool accIsQutaibiBank = acc.AccountType == FinancialAccountType.BankAccount && (acc.AccountName.Contains("قطيبي") || acc.BankOrWalletName.Contains("قطيبي"));
            bool accIsOneCashWallet = acc.AccountType == FinancialAccountType.ElectronicWallet && (acc.AccountName.Contains("ون كاش") || acc.BankOrWalletName.Contains("ون كاش"));
            if (accIsQutaibiBank)
                return s.Contains("قطيبي") && !s.Contains("ون كاش");
            if (accIsOneCashWallet)
                return s.Contains("ون كاش");

            var accKey = FinancialChannelHelper.NormalizeChannelKey(acc.BankOrWalletName);
            var txtKey = FinancialChannelHelper.NormalizeChannelKey(s);
            return accKey == txtKey && accKey != FinancialChannelHelper.ChannelCash;
        }


        private static List<FinancialAccountTransaction> BuildCombinedStatementRowsInMemory(
            FinancialAccount acc,
            List<FinancialAccountTransaction> directTxs,
            Shift? firstShift,
            List<CashBoxTransaction> allCashTxs,
            List<Order> candidateOrders,
            List<BankDeposit> bankDeposits,
            List<ReceiptVoucher> vouchers,
            List<ReturnOrder> returnOrders,
            DateTime? startDate,
            DateTime? endDate)
        {
            var rows = new List<FinancialAccountTransaction>(directTxs);

            var existingRefs = new HashSet<string>(
                directTxs.Where(t => !string.IsNullOrWhiteSpace(t.ReferenceNumber)).Select(t => t.ReferenceNumber!),
                StringComparer.OrdinalIgnoreCase);

            if (acc.AccountType == FinancialAccountType.MainCashBox)
            {
                if (firstShift != null && firstShift.StartingCash > 0 && !existingRefs.Contains($"SHF-OPEN-{firstShift.Id}"))
                {
                    rows.Add(new FinancialAccountTransaction
                    {
                        Id = 0,
                        FinancialAccountId = acc.Id,
                        TransactionType = FinancialTransactionType.Deposit,
                        AmountIn = firstShift.StartingCash,
                        AmountOut = 0m,
                        ReferenceNumber = $"SHF-OPEN-{firstShift.Id}",
                        Name = "عهدة افتتاحية للصندوق",
                        Description = $"العهدة الافتتاحية للمناوبة #{firstShift.ShiftNumber ?? firstShift.Id.ToString()}",
                        PerformedByUserName = "الكاشير / الصندوق",
                        TransactionDate = firstShift.StartTime,
                        Notes = firstShift.Notes
                    });
                }

                foreach (var ct in allCashTxs)
                {
                    string refCode = $"CBX-{ct.Id}";
                    if (existingRefs.Contains(refCode)) continue;
                    if (IsInternalTransferMirrorRow(ct.Description, ct.Notes))
                        continue;

                    bool isInflow = ct.Type == CashBoxTransactionType.Income;
                    rows.Add(new FinancialAccountTransaction
                    {
                        Id = ct.Id,
                        FinancialAccountId = acc.Id,
                        TransactionType = isInflow ? FinancialTransactionType.SaleCollection : FinancialTransactionType.ExpensePayment,
                        AmountIn = isInflow ? ct.Amount : 0m,
                        AmountOut = isInflow ? 0m : ct.Amount,
                        ReferenceNumber = refCode,
                        Name = ct.Name ?? "حركة صندوق",
                        Description = ct.Description ?? ct.Name ?? "حركة صندوق الكاشير",
                        PerformedByUserName = "الكاشير / الصندوق",
                        TransactionDate = ct.TransactionDate,
                        Notes = ct.Notes
                    });
                }
            }
            else
            {
                foreach (var o in candidateOrders)
                {
                    string refCode = $"ORD-{o.OrderNumber}";
                    if (existingRefs.Contains(refCode)) continue;

                    if (o.PaymentMethod == PaymentMethod.Split)
                    {
                        var split = OrderSplitHelper.ParseSplit(o.Notes);
                        if (split != null && split.CardAmount > 0 && MatchesAccountChannel(acc, split.BankOrWallet))
                        {
                            rows.Add(new FinancialAccountTransaction
                            {
                                FinancialAccountId = acc.Id,
                                TransactionType = FinancialTransactionType.SaleCollection,
                                AmountIn = split.CardAmount,
                                AmountOut = 0m,
                                ReferenceNumber = refCode,
                                Name = $"مبيعات مجزأة #{o.OrderNumber}",
                                Description = $"تحصيل مبيعات فاتورة #{o.OrderNumber} ({split.BankOrWallet})",
                                PerformedByUserName = "نقطة البيع",
                                TransactionDate = o.CreatedAt,
                                Notes = o.Notes
                            });
                        }
                    }
                    else
                    {
                        var depInfo = OrderDepositHelper.ParseDepositInfo(o.Notes);
                        string? bankText = !string.IsNullOrWhiteSpace(depInfo?.BankOrWallet) ? depInfo!.BankOrWallet : o.Notes;
                        if (MatchesAccountChannel(acc, bankText))
                        {
                            decimal amt = (depInfo != null && depInfo.Amount > 0) ? depInfo.Amount : o.Total;
                            rows.Add(new FinancialAccountTransaction
                            {
                                FinancialAccountId = acc.Id,
                                TransactionType = FinancialTransactionType.SaleCollection,
                                AmountIn = amt,
                                AmountOut = 0m,
                                ReferenceNumber = !string.IsNullOrWhiteSpace(depInfo?.ReferenceNumber) ? depInfo!.ReferenceNumber : refCode,
                                Name = $"مبيعات إيداع #{o.OrderNumber}",
                                Description = $"تحصيل فاتورة مبيعات #{o.OrderNumber}",
                                PerformedByUserName = "نقطة البيع",
                                TransactionDate = o.CreatedAt,
                                Notes = o.Notes
                            });
                        }
                    }
                }

                foreach (var b in bankDeposits)
                {
                    if (!string.IsNullOrWhiteSpace(b.ReferenceNumber) && existingRefs.Contains(b.ReferenceNumber))
                        continue;
                    if (IsInternalTransferMirrorRow(b.Source, b.Notes) && directTxs.Any())
                        continue;

                    if (MatchesAccountChannel(acc, b.Source ?? b.Notes))
                    {
                        rows.Add(new FinancialAccountTransaction
                        {
                            FinancialAccountId = acc.Id,
                            TransactionType = FinancialTransactionType.Deposit,
                            AmountIn = b.Amount,
                            AmountOut = 0m,
                            ReferenceNumber = b.ReferenceNumber ?? $"DEP-{b.Id}",
                            Name = $"سند إيداع - {b.DepositorName}",
                            Description = $"إيداع بنكي ({b.DepositorName}) - {b.Source}",
                            PerformedByUserName = b.CreatedBy ?? "الإدارة",
                            TransactionDate = b.DepositDate,
                            Notes = b.Notes
                        });
                    }
                }

                foreach (var sd in allCashTxs.Where(t => t.Type == CashBoxTransactionType.BankDeposit))
                {
                    if (IsInternalTransferMirrorRow(sd.Description, sd.Notes))
                        continue;
                    if (MatchesAccountChannel(acc, sd.Description ?? sd.Notes))
                    {
                        rows.Add(new FinancialAccountTransaction
                        {
                            FinancialAccountId = acc.Id,
                            TransactionType = FinancialTransactionType.OwnerBankDeposit,
                            AmountIn = sd.Amount,
                            AmountOut = 0m,
                            ReferenceNumber = $"SHF-DEP-{sd.Id}",
                            Name = sd.Name ?? "توريد إغلاق وردية",
                            Description = sd.Description ?? "توريد نقدية من الصندوق عند إغلاق الوردية",
                            PerformedByUserName = "إغلاق المناوبة",
                            TransactionDate = sd.TransactionDate,
                            Notes = sd.Notes
                        });
                    }
                }

                foreach (var v in vouchers)
                {
                    if (!string.IsNullOrWhiteSpace(v.VoucherNumber) && existingRefs.Contains(v.VoucherNumber))
                        continue;
                    if (v.Type == VoucherType.Payment && v.VoucherNumber != null && v.VoucherNumber.StartsWith("PV-RET-", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (MatchesAccountChannel(acc, v.Notes))
                    {
                        bool isReceipt = v.Type == VoucherType.Receipt;
                        rows.Add(new FinancialAccountTransaction
                        {
                            FinancialAccountId = acc.Id,
                            TransactionType = isReceipt ? FinancialTransactionType.DebtCollection : FinancialTransactionType.ExpensePayment,
                            AmountIn = isReceipt ? v.Amount : 0m,
                            AmountOut = isReceipt ? 0m : v.Amount,
                            ReferenceNumber = v.VoucherNumber,
                            Name = v.Name,
                            Description = isReceipt
                                ? $"سند قبض ({v.CustomerName}) - {v.Name}"
                                : $"سند صرف ({v.CustomerName}) - {v.Name}",
                            PerformedByUserName = "المحاسبة / السندات",
                            TransactionDate = v.VoucherDate,
                            Notes = v.Notes
                        });
                    }
                }

                foreach (var r in returnOrders)
                {
                    string refCode = $"RET-{r.Id}";
                    if (existingRefs.Contains(refCode)) continue;

                    if (MatchesAccountChannel(acc, r.RefundWallet ?? r.Reason))
                    {
                        rows.Add(new FinancialAccountTransaction
                        {
                            FinancialAccountId = acc.Id,
                            TransactionType = FinancialTransactionType.SalesReturn,
                            AmountIn = 0m,
                            AmountOut = r.TotalAmount,
                            ReferenceNumber = !string.IsNullOrWhiteSpace(r.RefundReferenceNumber) ? r.RefundReferenceNumber : refCode,
                            Name = $"مرتجع مبيعات #{r.OriginalOrderId}",
                            Description = $"استرداد مرتجع طلب #{r.OriginalOrderId} ({r.RefundWallet})",
                            PerformedByUserName = "إدارة المرتجعات",
                            TransactionDate = r.ReturnDate,
                            Notes = r.Reason
                        });
                    }
                }
            }

            var orderedAsc = rows
                .OrderBy(r => r.TransactionDate)
                .ThenBy(r => r.Id)
                .ToList();

            decimal running = 0m;
            foreach (var r in orderedAsc)
            {
                running += (r.AmountIn - r.AmountOut);
                r.BalanceAfter = running;
            }

            IEnumerable<FinancialAccountTransaction> filtered = orderedAsc;
            if (startDate.HasValue)
            {
                var s = startDate.Value.Date;
                filtered = filtered.Where(t => t.TransactionDate >= s);
            }
            if (endDate.HasValue)
            {
                var e = endDate.Value.Date.AddDays(1).AddTicks(-1);
                filtered = filtered.Where(t => t.TransactionDate <= e);
            }

            return filtered
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .ToList();
        }

        private async Task<List<FinancialAccountTransaction>> BuildCombinedStatementRowsAsync(
            FinancialAccount acc,
            DateTime? startDate,
            DateTime? endDate)
        {
            var context = _unitOfWork.GetContext();
            var rows = new List<FinancialAccountTransaction>();

            // 1. Direct FinancialAccountTransactions recorded on this account
            var directTxs = await context.FinancialAccountTransactions
                .AsNoTracking()
                .Include(t => t.RelatedAccount)
                .Where(t => t.FinancialAccountId == acc.Id)
                .ToListAsync();
            rows.AddRange(directTxs);

            var existingRefs = new HashSet<string>(
                directTxs.Where(t => !string.IsNullOrWhiteSpace(t.ReferenceNumber)).Select(t => t.ReferenceNumber!),
                StringComparer.OrdinalIgnoreCase);

            if (acc.AccountType == FinancialAccountType.MainCashBox)
            {
                // Include initial shift StartingCash if not already represented
                var firstShift = await context.Shifts.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
                if (firstShift != null && firstShift.StartingCash > 0 && !existingRefs.Contains($"SHF-OPEN-{firstShift.Id}"))
                {
                    rows.Add(new FinancialAccountTransaction
                    {
                        Id = 0,
                        FinancialAccountId = acc.Id,
                        TransactionType = FinancialTransactionType.Deposit,
                        AmountIn = firstShift.StartingCash,
                        AmountOut = 0m,
                        ReferenceNumber = $"SHF-OPEN-{firstShift.Id}",
                        Name = "عهدة افتتاحية للصندوق",
                        Description = $"العهدة الافتتاحية للمناوبة #{firstShift.ShiftNumber ?? firstShift.Id.ToString()}",
                        PerformedByUserName = "الكاشير / الصندوق",
                        TransactionDate = firstShift.StartTime,
                        Notes = firstShift.Notes
                    });
                }

                // Include CashBoxTransactions if not already represented in directTxs
                var cashTxs = await context.CashBoxTransactions
                    .AsNoTracking()
                    .OrderBy(t => t.TransactionDate)
                    .ToListAsync();

                foreach (var ct in cashTxs)
                {
                    string refCode = $"CBX-{ct.Id}";
                    if (existingRefs.Contains(refCode)) continue;
                    // Avoid duplicating manual/transfer rows created by FinancialAccountService
                    if (IsInternalTransferMirrorRow(ct.Description, ct.Notes))
                        continue;

                    bool isInflow = ct.Type == CashBoxTransactionType.Income;
                    rows.Add(new FinancialAccountTransaction
                    {
                        Id = ct.Id,
                        FinancialAccountId = acc.Id,
                        TransactionType = isInflow ? FinancialTransactionType.SaleCollection : FinancialTransactionType.ExpensePayment,
                        AmountIn = isInflow ? ct.Amount : 0m,
                        AmountOut = isInflow ? 0m : ct.Amount,
                        ReferenceNumber = refCode,
                        Name = ct.Name ?? "حركة صندوق",
                        Description = ct.Description ?? ct.Name ?? "حركة صندوق الكاشير",
                        PerformedByUserName = "الكاشير / الصندوق",
                        TransactionDate = ct.TransactionDate,
                        Notes = ct.Notes
                    });
                }
            }
            else
            {
                // 2. Sales Orders paid into this Bank/Wallet
                var candidateOrders = await context.Orders
                    .AsNoTracking()
                    .Where(o => o.Status != OrderStatus.Cancelled &&
                                (o.PaymentMethod == PaymentMethod.MobilePayment ||
                                 o.PaymentMethod == PaymentMethod.BankTransfer ||
                                 o.PaymentMethod == PaymentMethod.DebitCard ||
                                 o.PaymentMethod == PaymentMethod.CreditCard ||
                                 o.PaymentMethod == PaymentMethod.Split ||
                                 (o.Notes != null && o.Notes.Contains("[إيداع:"))))
                    .ToListAsync();

                foreach (var o in candidateOrders)
                {
                    string refCode = $"ORD-{o.OrderNumber}";
                    if (existingRefs.Contains(refCode)) continue;

                    if (o.PaymentMethod == PaymentMethod.Split)
                    {
                        var split = OrderSplitHelper.ParseSplit(o.Notes);
                        if (split != null && split.CardAmount > 0 && MatchesAccountChannel(acc, split.BankOrWallet))
                        {
                            rows.Add(new FinancialAccountTransaction
                            {
                                FinancialAccountId = acc.Id,
                                TransactionType = FinancialTransactionType.SaleCollection,
                                AmountIn = split.CardAmount,
                                AmountOut = 0m,
                                ReferenceNumber = refCode,
                                Name = $"مبيعات مجزأة #{o.OrderNumber}",
                                Description = $"تحصيل مبيعات فاتورة #{o.OrderNumber} ({split.BankOrWallet})",
                                PerformedByUserName = "نقطة البيع",
                                TransactionDate = o.CreatedAt,
                                Notes = o.Notes
                            });
                        }
                    }
                    else
                    {
                        var depInfo = OrderDepositHelper.ParseDepositInfo(o.Notes);
                        string? bankText = !string.IsNullOrWhiteSpace(depInfo?.BankOrWallet) ? depInfo!.BankOrWallet : o.Notes;
                        if (MatchesAccountChannel(acc, bankText))
                        {
                            decimal amt = (depInfo != null && depInfo.Amount > 0) ? depInfo.Amount : o.Total;
                            rows.Add(new FinancialAccountTransaction
                            {
                                FinancialAccountId = acc.Id,
                                TransactionType = FinancialTransactionType.SaleCollection,
                                AmountIn = amt,
                                AmountOut = 0m,
                                ReferenceNumber = !string.IsNullOrWhiteSpace(depInfo?.ReferenceNumber) ? depInfo!.ReferenceNumber : refCode,
                                Name = $"مبيعات إيداع #{o.OrderNumber}",
                                Description = $"تحصيل فاتورة مبيعات #{o.OrderNumber}",
                                PerformedByUserName = "نقطة البيع",
                                TransactionDate = o.CreatedAt,
                                Notes = o.Notes
                            });
                        }
                    }
                }

                // 3. Approved BankDeposits (excluding internal transfers already in directTxs)
                var bankDeposits = await context.BankDeposits
                    .AsNoTracking()
                    .Where(b => b.Status != "Rejected")
                    .ToListAsync();

                foreach (var b in bankDeposits)
                {
                    if (!string.IsNullOrWhiteSpace(b.ReferenceNumber) && existingRefs.Contains(b.ReferenceNumber))
                        continue;
                    if (IsInternalTransferMirrorRow(b.Source, b.Notes) && directTxs.Any())
                        continue;

                    if (MatchesAccountChannel(acc, b.Source ?? b.Notes))
                    {
                        rows.Add(new FinancialAccountTransaction
                        {
                            FinancialAccountId = acc.Id,
                            TransactionType = FinancialTransactionType.Deposit,
                            AmountIn = b.Amount,
                            AmountOut = 0m,
                            ReferenceNumber = b.ReferenceNumber ?? $"DEP-{b.Id}",
                            Name = $"سند إيداع - {b.DepositorName}",
                            Description = $"إيداع بنكي ({b.DepositorName}) - {b.Source}",
                            PerformedByUserName = b.CreatedBy ?? "الإدارة",
                            TransactionDate = b.DepositDate,
                            Notes = b.Notes
                        });
                    }
                }

                // 4. Shift-end CashBox transfers to Bank/Wallet
                var shiftDeposits = await context.CashBoxTransactions
                    .AsNoTracking()
                    .Where(t => t.Type == CashBoxTransactionType.BankDeposit)
                    .ToListAsync();

                foreach (var sd in shiftDeposits)
                {
                    if (IsInternalTransferMirrorRow(sd.Description, sd.Notes))
                        continue;
                    if (MatchesAccountChannel(acc, sd.Description ?? sd.Notes))
                    {
                        rows.Add(new FinancialAccountTransaction
                        {
                            FinancialAccountId = acc.Id,
                            TransactionType = FinancialTransactionType.OwnerBankDeposit,
                            AmountIn = sd.Amount,
                            AmountOut = 0m,
                            ReferenceNumber = $"SHF-DEP-{sd.Id}",
                            Name = sd.Name ?? "توريد إغلاق وردية",
                            Description = sd.Description ?? "توريد نقدية من الصندوق عند إغلاق الوردية",
                            PerformedByUserName = "إغلاق المناوبة",
                            TransactionDate = sd.TransactionDate,
                            Notes = sd.Notes
                        });
                    }
                }

                // 5. ReceiptVouchers (both Receipt inflows and Payment outflows / سندات الصرف والقبض)
                var vouchers = await context.ReceiptVouchers
                    .AsNoTracking()
                    .Where(v => v.PaymentMethod != PaymentMethod.Cash)
                    .ToListAsync();

                foreach (var v in vouchers)
                {
                    if (!string.IsNullOrWhiteSpace(v.VoucherNumber) && existingRefs.Contains(v.VoucherNumber))
                        continue;
                    if (v.Type == VoucherType.Payment && v.VoucherNumber != null && v.VoucherNumber.StartsWith("PV-RET-", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (MatchesAccountChannel(acc, v.Notes))
                    {
                        bool isReceipt = v.Type == VoucherType.Receipt;
                        rows.Add(new FinancialAccountTransaction
                        {
                            FinancialAccountId = acc.Id,
                            TransactionType = isReceipt ? FinancialTransactionType.DebtCollection : FinancialTransactionType.ExpensePayment,
                            AmountIn = isReceipt ? v.Amount : 0m,
                            AmountOut = isReceipt ? 0m : v.Amount,
                            ReferenceNumber = v.VoucherNumber,
                            Name = v.Name,
                            Description = isReceipt
                                ? $"سند قبض ({v.CustomerName}) - {v.Name}"
                                : $"سند صرف ({v.CustomerName}) - {v.Name}",
                            PerformedByUserName = "المحاسبة / السندات",
                            TransactionDate = v.VoucherDate,
                            Notes = v.Notes
                        });
                    }
                }

                // 6. ReturnOrders (مرتجعات المبيعات الإلكترونية الخارجة من المحفظة)
                var returnOrders = await context.ReturnOrders
                    .AsNoTracking()
                    .Where(r => r.ReturnMethod != PaymentMethod.Cash &&
                                r.ReturnMethod != PaymentMethod.OnCredit &&
                                r.ReturnMethod != PaymentMethod.Remake)
                    .ToListAsync();

                foreach (var r in returnOrders)
                {
                    string refCode = $"RET-{r.Id}";
                    if (existingRefs.Contains(refCode)) continue;

                    if (MatchesAccountChannel(acc, r.RefundWallet ?? r.Reason))
                    {
                        rows.Add(new FinancialAccountTransaction
                        {
                            FinancialAccountId = acc.Id,
                            TransactionType = FinancialTransactionType.SalesReturn,
                            AmountIn = 0m,
                            AmountOut = r.TotalAmount,
                            ReferenceNumber = !string.IsNullOrWhiteSpace(r.RefundReferenceNumber) ? r.RefundReferenceNumber : refCode,
                            Name = $"مرتجع مبيعات #{r.OriginalOrderId}",
                            Description = $"استرداد مرتجع طلب #{r.OriginalOrderId} ({r.RefundWallet})",
                            PerformedByUserName = "إدارة المرتجعات",
                            TransactionDate = r.ReturnDate,
                            Notes = r.Reason
                        });
                    }
                }
            }

            // Compute running BalanceAfter chronologically
            var orderedAsc = rows
                .OrderBy(r => r.TransactionDate)
                .ThenBy(r => r.Id)
                .ToList();

            decimal running = 0m;
            foreach (var r in orderedAsc)
            {
                running += (r.AmountIn - r.AmountOut);
                r.BalanceAfter = running;
            }

            IEnumerable<FinancialAccountTransaction> filtered = orderedAsc;
            if (startDate.HasValue)
            {
                var s = startDate.Value.Date;
                filtered = filtered.Where(t => t.TransactionDate >= s);
            }
            if (endDate.HasValue)
            {
                var e = endDate.Value.Date.AddDays(1).AddTicks(-1);
                filtered = filtered.Where(t => t.TransactionDate <= e);
            }

            return filtered
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .ToList();
        }
    }
}
