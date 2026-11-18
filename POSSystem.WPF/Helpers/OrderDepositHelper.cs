using System;
using System.Text;

namespace POSSystem.WPF.Helpers
{
    public class OrderDepositInfo
    {
        public string Currency { get; set; } = "ريال يمني";
        public decimal Amount { get; set; }
        public string BankOrWallet { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
    }

    public static class OrderDepositHelper
    {
        public static string FormatDepositTag(string currency, decimal amount, string? bank, string? reference)
        {
            var sb = new StringBuilder();
            sb.Append($"[إيداع: {currency} | المبلغ: {amount:N0}");
            if (!string.IsNullOrWhiteSpace(bank)) sb.Append($" | الجهة: {bank.Trim()}");
            if (!string.IsNullOrWhiteSpace(reference)) sb.Append($" | المرجع: {reference.Trim()}");
            sb.Append("]");
            return sb.ToString();
        }

        public static OrderDepositInfo? ParseDepositInfo(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes) || !notes.Contains("[إيداع:")) return null;

            int start = notes.IndexOf("[إيداع:", StringComparison.Ordinal);
            int end = notes.IndexOf(']', start);
            if (end == -1) end = notes.Length;

            string tag = notes.Substring(start, end - start);
            string[] parts = tag.Trim('[', ']').Split('|');

            var info = new OrderDepositInfo();
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("إيداع:", StringComparison.Ordinal))
                {
                    info.Currency = trimmed.Substring("إيداع:".Length).Trim();
                }
                else if (trimmed.StartsWith("المبلغ:", StringComparison.Ordinal))
                {
                    var valStr = trimmed.Substring("المبلغ:".Length).Replace(",", "").Trim();
                    if (decimal.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amt))
                        info.Amount = amt;
                }
                else if (trimmed.StartsWith("الجهة:", StringComparison.Ordinal))
                {
                    info.BankOrWallet = trimmed.Substring("الجهة:".Length).Trim();
                }
                else if (trimmed.StartsWith("المرجع:", StringComparison.Ordinal))
                {
                    info.ReferenceNumber = trimmed.Substring("المرجع:".Length).Trim();
                }
            }
            return info;
        }

        public static string CleanNotesWithoutDepositTag(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return string.Empty;
            int start = notes.IndexOf("[إيداع:", StringComparison.Ordinal);
            if (start == -1) return notes.Trim();

            int end = notes.IndexOf(']', start);
            if (end == -1) end = notes.Length - 1;

            string before = notes.Substring(0, start).Trim();
            string after = (end + 1 < notes.Length) ? notes.Substring(end + 1).Trim() : string.Empty;

            return $"{before} {after}".Trim();
        }
    }
}

