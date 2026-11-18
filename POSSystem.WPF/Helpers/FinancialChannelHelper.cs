using System;
using System.Collections.Generic;
using System.Linq;
using POSSystem.WPF.Data;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;

namespace POSSystem.WPF.Helpers
{
    public static class FinancialChannelHelper
    {
        public const string ChannelCash = "Cash";
        public const string ChannelKuraimi = "Wallet_Kuraimi";
        public const string ChannelJeeb = "Wallet_Jeeb";
        public const string ChannelFloosak = "Wallet_Floosak";
        public const string ChannelOneCash = "Wallet_OneCash";
        public const string ChannelJawali = "Wallet_Jawali";
        public const string ChannelMobileMoney = "Wallet_MobileMoney";
        public const string ChannelYemenWallet = "Wallet_YemenWallet";
        public const string ChannelMahfathati = "Wallet_Mahfathati";
        public const string ChannelCacCash = "Wallet_CacCash";
        public const string ChannelOtherWallet = "MobilePayment";
        public const string ChannelBankTransfer = "BankTransfer";
        public const string ChannelDebitCard = "DebitCard";
        public const string ChannelOther = "Other";

        /// <summary>
        /// القائمة الموحدة للمحافظ المعتمدة في النظام بالكامل
        /// </summary>
        public static readonly string[] UnifiedWallets = new[]
        {
            "حاسب كريمي",
            "محفظة جيب",
            "محفظة فلوسك",
            "محفظة ون كاش",
            "محفظة جوالي",
            "محفظة موبايل موني",
            "محفظة يمن والت"
        };

        public static readonly string[] StandardWalletKeys = new[]
        {
            ChannelKuraimi,
            ChannelJeeb,
            ChannelFloosak,
            ChannelOneCash,
            ChannelJawali,
            ChannelMobileMoney,
            ChannelYemenWallet,
            ChannelMahfathati,
            ChannelCacCash,
            ChannelOtherWallet,
            ChannelBankTransfer,
            ChannelDebitCard
        };

        /// <summary>
        /// إرجاع القائمة الموحدة للمحافظ مضافاً إليها أي محفظة مخصصة أضافها المستخدم
        /// </summary>
        public static List<string> GetActiveWalletsList()
        {
            var list = new List<string>(UnifiedWallets);

            try
            {
                var settings = SettingsService.LoadSettings();
                if (settings?.CustomWallets != null)
                {
                    foreach (var cw in settings.CustomWallets)
                    {
                        if (!string.IsNullOrWhiteSpace(cw) && !list.Any(w => w.Equals(cw.Trim(), StringComparison.OrdinalIgnoreCase)))
                        {
                            list.Add(cw.Trim());
                        }
                    }
                }
            }
            catch { }

            try
            {
                using var context = new POSDbContext();
                var dbAccounts = context.FinancialAccounts
                    .Where(a => a.IsActive && a.AccountType != FinancialAccountType.MainCashBox)
                    .Select(a => a.AccountName)
                    .ToList();

                foreach (var accName in dbAccounts)
                {
                    if (!string.IsNullOrWhiteSpace(accName) &&
                        accName != "حساب بنك الكريمي" &&
                        accName != "حساب بنك القطيبي" &&
                        !list.Any(w => w.Equals(accName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Add(accName.Trim());
                    }
                }
            }
            catch { }

            return list;
        }

        /// <summary>
        /// إضافة محفظة جديدة وتعميمها فوراً في الإعدادات وفي شاشة إدارة الحسابات المالية والمحافظ والبنوك
        /// </summary>
        public static bool AddCustomWallet(string? walletName)
        {
            if (string.IsNullOrWhiteSpace(walletName)) return false;
            var clean = walletName.Trim();

            try
            {
                var settings = SettingsService.LoadSettings();
                settings.CustomWallets ??= new List<string>();
                if (!UnifiedWallets.Any(w => w.Equals(clean, StringComparison.OrdinalIgnoreCase)) &&
                    !settings.CustomWallets.Any(w => w.Equals(clean, StringComparison.OrdinalIgnoreCase)))
                {
                    settings.CustomWallets.Add(clean);
                }
                settings.DefaultBankOrWallet = clean;
                SettingsService.SaveSettings(settings);
            }
            catch { }

            try
            {
                using var context = new POSDbContext();
                bool exists = context.FinancialAccounts.Any(a =>
                    a.AccountName.ToLower() == clean.ToLower() ||
                    a.BankOrWalletName.ToLower() == clean.ToLower());

                if (!exists)
                {
                    var now = DateTime.Now;
                    context.FinancialAccounts.Add(new FinancialAccount
                    {
                        AccountName = clean,
                        Name = clean,
                        AccountType = FinancialAccountType.ElectronicWallet,
                        BankOrWalletName = clean,
                        Currency = "ريال يمني",
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    context.SaveChanges();
                }
            }
            catch { }

            return true;
        }

        public static string NormalizeChannelKey(string? rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return ChannelCash;
            var s = rawInput.Trim().ToLowerInvariant();

            // الفصل الدقيق بين "محفظة جيب" و "حاسب كريمي"
            if (s == "wallet_jeeb" || s.Contains("جيب") || s.Contains("jeeb"))
                return ChannelJeeb;

            if (s == "wallet_kuraimi" || s.Contains("كريمي") || s.Contains("kuraimi") || s.Contains("حاسب كريمي"))
                return ChannelKuraimi;

            if (s == "wallet_floosak" || s.Contains("فلوسك") || s.Contains("floosak") || s.Contains("اليمن والكويت") || s.Contains("ykb"))
                return ChannelFloosak;

            if (s == "wallet_onecash" || s.Contains("ون كاش") || s.Contains("onecash") || s.Contains("one_cash") || s.Contains("قطيبي"))
                return ChannelOneCash;

            if (s == "wallet_jawali" || s.Contains("جوالي") || s.Contains("jawali") || s.Contains("الشامل"))
                return ChannelJawali;

            if (s == "wallet_mobilemoney" || s.Contains("موبايل موني") || s.Contains("mobilemoney") || s.Contains("mobile_money"))
                return ChannelMobileMoney;

            if (s == "wallet_yemenwallet" || s.Contains("يمن والت") || s.Contains("yemenwallet") || s.Contains("yemen_wallet"))
                return ChannelYemenWallet;

            if (s.Contains("محفظتي") || s.Contains("mahfathati") || s.Contains("تضامن"))
                return ChannelMahfathati;

            if (s.Contains("كاك") || s.Contains("caccash") || s.Contains("cac") || s.Contains("كاش كاك"))
                return ChannelCacCash;

            if (s.Contains("تحويل") || s.Contains("حوالة") || s.Contains("banktransfer") || s.Contains("bank") || s.Contains("بنك"))
                return ChannelBankTransfer;

            if (s.Contains("شبكة") || s.Contains("بطاقة") || s.Contains("مدى") || s.Contains("debit") || s.Contains("credit"))
                return ChannelDebitCard;

            if (s == "other" || s.Contains("خزينة") || s.Contains("إدارة") || s.Contains("خارج الصندوق"))
                return ChannelOther;

            if (s == "cash" || s.Contains("نقد") || s.Contains("صندوق") || s.Contains("درج"))
                return ChannelCash;

            if (s.Contains("محفظ") || s.Contains("wallet") || s.Contains("mobilepayment") || s.Contains("mobile"))
                return s; // الحفاظ على اسم المحفظة المخصصة الجديدة بدلاً من دمجها في قناة عامة

            return s;
        }

        public static string GetChannelDisplayName(string channelKey)
        {
            var normalized = NormalizeChannelKey(channelKey);
            return normalized switch
            {
                ChannelCash => "صندوق الكاشير (نقداً)",
                ChannelKuraimi => "حاسب كريمي",
                ChannelJeeb => "محفظة جيب",
                ChannelFloosak => "محفظة فلوسك",
                ChannelOneCash => "محفظة ون كاش",
                ChannelJawali => "محفظة جوالي",
                ChannelMobileMoney => "محفظة موبايل موني",
                ChannelYemenWallet => "محفظة يمن والت",
                ChannelMahfathati => "محفظتي (بنك التضامن)",
                ChannelCacCash => "محفظة كاش (كاك بنك)",
                ChannelOtherWallet => "محفظة إلكترونية عامة",
                ChannelBankTransfer => "حساب بنكي / تحويل بنكي",
                ChannelDebitCard => "شبكة / بطاقة بنكية",
                ChannelOther => "خزينة الإدارة (خارج الصندوق)",
                _ => channelKey
            };
        }

        public static PaymentMethod GetPaymentMethodForChannel(string channelKey)
        {
            var normalized = NormalizeChannelKey(channelKey);
            return normalized switch
            {
                ChannelCash => PaymentMethod.Cash,
                ChannelBankTransfer => PaymentMethod.BankTransfer,
                ChannelDebitCard => PaymentMethod.DebitCard,
                ChannelOther => PaymentMethod.Cash,
                _ => PaymentMethod.MobilePayment
            };
        }

        public static string ExtractChannel(PaymentMethod method, string? notes = null)
        {
            if (!string.IsNullOrWhiteSpace(notes))
            {
                if (notes.Contains("خزينة الإدارة") || notes.Contains("خارج الصندوق"))
                    return ChannelOther;

                string[] tags = { "[محفظة:", "[جهة الصرف:", "[طريقة الصرف:", "[حساب:" };
                foreach (var prefix in tags)
                {
                    if (notes.Contains(prefix))
                    {
                        var start = notes.IndexOf(prefix, StringComparison.Ordinal);
                        var end = notes.IndexOf(']', start);
                        if (end > start)
                        {
                            var tag = notes.Substring(start + prefix.Length, end - start - prefix.Length).Trim();
                            return NormalizeChannelKey(tag);
                        }
                    }
                }
            }

            if (method == PaymentMethod.Cash) return ChannelCash;
            if (method == PaymentMethod.BankTransfer) return ChannelBankTransfer;
            if (method == PaymentMethod.DebitCard || method == PaymentMethod.CreditCard) return ChannelDebitCard;

            if (!string.IsNullOrWhiteSpace(notes))
            {
                return NormalizeChannelKey(notes);
            }

            return ChannelOtherWallet;
        }
    }
}
