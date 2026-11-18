using System;
using System.Globalization;

namespace POSSystem.WPF.Helpers
{
    public class OrderSplitInfo
    {
        public decimal CashAmount { get; set; }
        public decimal CardAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public string BankOrWallet { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    public static class OrderSplitHelper
    {
        public static string FormatSplitTag(
            decimal cashAmount,
            decimal cardAmount,
            string? bankOrWallet = null,
            decimal debtAmount = 0m,
            string? customerName = null)
        {
            var tag = $"[SPLIT:CASH={cashAmount.ToString("F2", CultureInfo.InvariantCulture)};CARD={cardAmount.ToString("F2", CultureInfo.InvariantCulture)}";
            if (debtAmount > 0)
            {
                tag += $";DEBT={debtAmount.ToString("F2", CultureInfo.InvariantCulture)}";
            }
            if (!string.IsNullOrWhiteSpace(bankOrWallet))
            {
                tag += $";BANK={bankOrWallet.Trim()}";
            }
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                tag += $";CUST={customerName.Trim()}";
            }
            tag += "]";
            return tag;
        }

        public static OrderSplitInfo? ParseSplit(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return null;
            int start = notes.IndexOf("[SPLIT:", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;
            int end = notes.IndexOf("]", start);
            if (end < 0) return null;

            string content = notes.Substring(start + 7, end - (start + 7));
            var parts = content.Split(';');
            decimal cash = 0;
            decimal card = 0;
            decimal debt = 0;
            string bank = string.Empty;
            string cust = string.Empty;

            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length == 2)
                {
                    var key = kv[0].Trim();
                    var val = kv[1].Trim();
                    if (key.Equals("CASH", StringComparison.OrdinalIgnoreCase))
                        decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out cash);
                    else if (key.Equals("CARD", StringComparison.OrdinalIgnoreCase))
                        decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out card);
                    else if (key.Equals("DEBT", StringComparison.OrdinalIgnoreCase))
                        decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out debt);
                    else if (key.Equals("BANK", StringComparison.OrdinalIgnoreCase))
                        bank = val;
                    else if (key.Equals("CUST", StringComparison.OrdinalIgnoreCase))
                        cust = val;
                }
            }

            return new OrderSplitInfo
            {
                CashAmount = cash,
                CardAmount = card,
                DebtAmount = debt,
                BankOrWallet = bank,
                CustomerName = cust
            };
        }

        public static string CleanNotesWithoutSplitTag(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return string.Empty;
            int start = notes.IndexOf("[SPLIT:", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return notes.Trim();
            int end = notes.IndexOf("]", start);
            if (end < 0) return notes.Trim();
            string removed = notes.Remove(start, (end - start) + 1);
            return removed.Trim();
        }
    }
}
