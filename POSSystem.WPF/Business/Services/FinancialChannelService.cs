using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services
{
    public class FinancialChannelService : IFinancialChannelService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICashBoxTransactionService _cashBoxTransactionService;

        // Ultra-fast short-lived batch cache (TTL: 1200ms) to avoid redundant queries during UI refreshes
        private static Dictionary<string, decimal>? _cachedWalletBalances;
        private static DateTime _cacheTimestamp = DateTime.MinValue;
        private static readonly object _cacheLock = new();

        public static void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cachedWalletBalances = null;
                _cacheTimestamp = DateTime.MinValue;
            }
        }

        public FinancialChannelService(
            IUnitOfWork unitOfWork,
            ICashBoxTransactionService cashBoxTransactionService)
        {
            _unitOfWork = unitOfWork;
            _cashBoxTransactionService = cashBoxTransactionService;
        }

        public async Task<decimal> GetAvailableBalanceAsync(string channelKeyOrTag, int? shiftId = null)
        {
            var normalizedKey = FinancialChannelHelper.NormalizeChannelKey(channelKeyOrTag);

            if (normalizedKey.Equals(FinancialChannelHelper.ChannelOther, StringComparison.OrdinalIgnoreCase))
            {
                return decimal.MaxValue;
            }

            // الحالة الأولى: صندوق الكاشير النقدي (Cash Drawer)
            if (normalizedKey == FinancialChannelHelper.ChannelCash)
            {
                var context = _unitOfWork.GetContext();
                Shift? targetShift = null;

                if (shiftId.HasValue && shiftId.Value > 0)
                {
                    targetShift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId.Value);
                }
                else
                {
                    targetShift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.IsActive);
                }

                if (targetShift == null)
                {
                    return 0m;
                }

                return await _cashBoxTransactionService.GetCashBoxBalanceAsync(targetShift.Id);
            }

            // الحالة الثانية: المحافظ الإلكترونية والحسابات والشبكات البنكية (Single-Pass Batch Calculation)
            var allBalances = await CalculateAllWalletBalancesBatchAsync();
            return allBalances.TryGetValue(normalizedKey, out var bal) ? bal : 0m;
        }

        public async Task ValidateSufficientBalanceAsync(string channelKeyOrTag, decimal requiredAmount, int? shiftId = null, int? excludedVoucherId = null)
        {
            if (requiredAmount <= 0) return;

            var normalizedKey = FinancialChannelHelper.NormalizeChannelKey(channelKeyOrTag);

            if (normalizedKey.Equals(FinancialChannelHelper.ChannelOther, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Force fresh balance calculation before any financial disbursement validation
            InvalidateCache();

            var channelDisplayName = FinancialChannelHelper.GetChannelDisplayName(normalizedKey);
            decimal availableBalance = await GetAvailableBalanceAsync(channelKeyOrTag, shiftId);

            if (excludedVoucherId.HasValue && excludedVoucherId.Value > 0)
            {
                var context = _unitOfWork.GetContext();
                var oldVoucher = await context.ReceiptVouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == excludedVoucherId.Value);
                if (oldVoucher != null && oldVoucher.Type == VoucherType.Payment)
                {
                    var oldChannel = FinancialChannelHelper.ExtractChannel(oldVoucher.PaymentMethod, oldVoucher.Notes);
                    if (oldChannel == normalizedKey)
                    {
                        availableBalance += oldVoucher.Amount;
                    }
                }
            }

            if (availableBalance < requiredAmount)
            {
                var shortage = requiredAmount - availableBalance;
                throw new InvalidOperationException(
                    $"عفواً، لا يمكن إتمام عملية الصرف لعدم كفاية الرصيد في جهة الصرف المحددة!\n\n" +
                    $"• جهة الصرف: {channelDisplayName}\n" +
                    $"• الرصيد الفعلي المتاح: {availableBalance:N0} ريال\n" +
                    $"• المبلغ المطلوب صرفه: {requiredAmount:N0} ريال\n" +
                    $"• مقدار العجز: {shortage:N0} ريال\n\n" +
                    "يرجى تغذية رصيد جهة الصرف أولاً (عبر سند قبض / إيداع مبيعات) أو اختيار جهة صرف أخرى بها سيولة كافية.");
            }
        }

        public async Task<List<FinancialChannelInfo>> GetAllChannelBalancesAsync(int? activeShiftId = null)
        {
            var results = new List<FinancialChannelInfo>();

            // 1. صندوق الكاشير النقدي
            var cashBalance = await GetAvailableBalanceAsync(FinancialChannelHelper.ChannelCash, activeShiftId);
            results.Add(new FinancialChannelInfo
            {
                ChannelKey = FinancialChannelHelper.ChannelCash,
                ChannelName = FinancialChannelHelper.GetChannelDisplayName(FinancialChannelHelper.ChannelCash),
                PaymentMethod = PaymentMethod.Cash,
                CurrentBalance = cashBalance,
                IsCashDrawer = true,
                ShiftId = activeShiftId
            });

            // 2. جلب أرصدة كافة المحافظ الإلكترونية والبنوك بتمريرة واحدة مجمعة (Single-Pass Batch) بدلاً من 84 استعلاماً
            var batchBalances = await CalculateAllWalletBalancesBatchAsync(forceRefresh: true);

            foreach (var walletKey in FinancialChannelHelper.StandardWalletKeys)
            {
                if (walletKey == FinancialChannelHelper.ChannelCash) continue;

                decimal balance = batchBalances.TryGetValue(walletKey, out var b) ? b : 0m;
                results.Add(new FinancialChannelInfo
                {
                    ChannelKey = walletKey,
                    ChannelName = FinancialChannelHelper.GetChannelDisplayName(walletKey),
                    PaymentMethod = FinancialChannelHelper.GetPaymentMethodForChannel(walletKey),
                    CurrentBalance = balance,
                    IsCashDrawer = false
                });
            }

            return results;
        }

        /// <summary>
        /// FIX ISSUE-PERF-001: Single-Pass Batch Wallet Balance Engine.
        /// Queries each table ONCE instead of N times per wallet, reducing 84 table scans down to 6 fast indexed projections.
        /// </summary>
        private async Task<Dictionary<string, decimal>> CalculateAllWalletBalancesBatchAsync(bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                lock (_cacheLock)
                {
                    if (_cachedWalletBalances != null && (DateTime.UtcNow - _cacheTimestamp).TotalMilliseconds < 1200)
                    {
                        return new Dictionary<string, decimal>(_cachedWalletBalances, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }

            var context = _unitOfWork.GetContext();
            var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            void AddToChannel(string? rawChannel, decimal delta)
            {
                if (delta == 0m) return;
                var key = FinancialChannelHelper.NormalizeChannelKey(rawChannel);
                if (string.IsNullOrWhiteSpace(key) || key == FinancialChannelHelper.ChannelCash) return;
                balances[key] = balances.GetValueOrDefault(key) + delta;
            }

            // 1. المقبوضات الواردة من طلبات المبيعات الإلكترونية والمجزأة (1 query)
            var candidateOrders = await context.Orders
                .AsNoTracking()
                .Where(o => o.Status != OrderStatus.Cancelled &&
                            (o.PaymentMethod == PaymentMethod.MobilePayment ||
                             o.PaymentMethod == PaymentMethod.BankTransfer ||
                             o.PaymentMethod == PaymentMethod.DebitCard ||
                             o.PaymentMethod == PaymentMethod.CreditCard ||
                             o.PaymentMethod == PaymentMethod.Split ||
                             (o.Notes != null && o.Notes.Contains("[إيداع:"))))
                .Select(o => new
                {
                    o.Total,
                    o.PaymentMethod,
                    o.Notes
                })
                .ToListAsync();

            foreach (var o in candidateOrders)
            {
                if (o.PaymentMethod == PaymentMethod.Split)
                {
                    var split = OrderSplitHelper.ParseSplit(o.Notes);
                    if (split != null && split.CardAmount > 0)
                    {
                        AddToChannel(split.BankOrWallet, split.CardAmount);
                    }
                }
                else
                {
                    var depInfo = OrderDepositHelper.ParseDepositInfo(o.Notes);
                    string? bankName = !string.IsNullOrWhiteSpace(depInfo?.BankOrWallet) ? depInfo!.BankOrWallet : o.Notes;
                    decimal amt = (depInfo != null && depInfo.Amount > 0) ? depInfo.Amount : o.Total;
                    AddToChannel(bankName ?? o.PaymentMethod.ToString(), amt);
                }
            }

            // 2. المقبوضات الواردة من الإيداعات البنكية المعتمدة (1 query)
            var bankDeposits = await context.BankDeposits
                .AsNoTracking()
                .Where(b => b.Status != "Rejected")
                .Select(b => new { b.Amount, b.Source, b.Notes })
                .ToListAsync();

            foreach (var b in bankDeposits)
            {
                if (!string.IsNullOrWhiteSpace(b.Source) && b.Source.Contains("صندوق الكاشير"))
                    continue;
                if (!string.IsNullOrWhiteSpace(b.Notes) && (b.Notes.Contains("توريد كاش عند إقفال") || FinancialAccountService.IsInternalTransferMirrorRow(b.Source, b.Notes)))
                    continue;

                AddToChannel(b.Source ?? b.Notes, b.Amount);
            }

            // 3. المقبوضات الواردة من توريد كاش إغلاق المناوبات (1 query)
            var shiftEndDeposits = await context.CashBoxTransactions
                .AsNoTracking()
                .Where(t => t.Type == CashBoxTransactionType.BankDeposit)
                .Select(t => new { t.Amount, t.Description, t.Notes })
                .ToListAsync();

            foreach (var s in shiftEndDeposits)
            {
                if (FinancialAccountService.IsInternalTransferMirrorRow(s.Description, s.Notes))
                    continue;
                AddToChannel(s.Description ?? s.Notes, s.Amount);
            }

            // 4 & 5. سندات القبض وسندات الصرف غير النقدية في استعلام واحد مدمج (1 query instead of 2)
            var nonCashVouchers = await context.ReceiptVouchers
                .AsNoTracking()
                .Where(v => v.PaymentMethod != PaymentMethod.Cash)
                .Select(v => new { v.Type, v.VoucherNumber, v.Amount, v.PaymentMethod, v.Notes })
                .ToListAsync();

            foreach (var v in nonCashVouchers)
            {
                // Skip electronic return payment vouchers (PV-RET-) because ReturnOrders are deducted directly in step 6
                if (v.Type == VoucherType.Payment && v.VoucherNumber != null && v.VoucherNumber.StartsWith("PV-RET-", StringComparison.OrdinalIgnoreCase))
                    continue;

                var vChannel = FinancialChannelHelper.ExtractChannel(v.PaymentMethod, v.Notes);
                if (v.Type == VoucherType.Receipt)
                    AddToChannel(vChannel, v.Amount);
                else if (v.Type == VoucherType.Payment)
                    AddToChannel(vChannel, -v.Amount);
            }

            // 6. المبالغ الخارجة عبر مرتجعات المبيعات الإلكترونية (1 query)
            var returnOrders = await context.ReturnOrders
                .AsNoTracking()
                .Where(r => r.ReturnMethod != PaymentMethod.Cash &&
                            r.ReturnMethod != PaymentMethod.OnCredit &&
                            r.ReturnMethod != PaymentMethod.Remake)
                .Select(r => new { r.TotalAmount, r.RefundWallet, r.Reason, r.ReturnMethod })
                .ToListAsync();

            foreach (var r in returnOrders)
            {
                AddToChannel(r.RefundWallet ?? r.Reason ?? r.ReturnMethod.ToString(), -r.TotalAmount);
            }

            // 7. الحركات المباشرة على بطاقات الحسابات المالية (1 query)
            try
            {
                var allAccounts = await context.FinancialAccounts
                    .AsNoTracking()
                    .Where(a => a.IsActive && a.AccountType != FinancialAccountType.MainCashBox)
                    .Select(a => new { a.Id, a.BankOrWalletName, a.AccountName })
                    .ToListAsync();

                if (allAccounts.Count > 0)
                {
                    var accountChannelById = allAccounts.ToDictionary(
                        a => a.Id,
                        a => FinancialChannelHelper.NormalizeChannelKey(!string.IsNullOrWhiteSpace(a.BankOrWalletName) ? a.BankOrWalletName : a.AccountName));

                    var directTxs = await context.FinancialAccountTransactions
                        .AsNoTracking()
                        .Where(t => t.SourceEntityType == "Manual" ||
                                    t.SourceEntityType == "OpeningBalance" ||
                                    t.SourceEntityType == "InternalTransfer" ||
                                    t.SourceEntityType == "OwnerBankDeposit")
                        .Select(t => new { t.FinancialAccountId, t.AmountIn, t.AmountOut })
                        .ToListAsync();

                    foreach (var tx in directTxs)
                    {
                        if (accountChannelById.TryGetValue(tx.FinancialAccountId, out var chKey))
                        {
                            AddToChannel(chKey, tx.AmountIn - tx.AmountOut);
                        }
                    }
                }
            }
            catch { }

            lock (_cacheLock)
            {
                _cachedWalletBalances = new Dictionary<string, decimal>(balances, StringComparer.OrdinalIgnoreCase);
                _cacheTimestamp = DateTime.UtcNow;
            }

            return balances;
        }
    }
}
