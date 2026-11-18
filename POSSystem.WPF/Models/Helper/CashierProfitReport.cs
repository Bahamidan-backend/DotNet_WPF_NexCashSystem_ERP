using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models.Helper
{
    public class CashierProfitReport
    {
        public int UserId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }

        /// <summary>
        /// مجمل الربح من المبيعات (إجمالي المبيعات - تكلفة المنتجات المباعة COGS)
        /// Gross Profit = Total Revenue - Cost of Goods Sold
        /// </summary>
        public decimal GrossProfit { get; set; }

        /// <summary>
        /// للتوافق الخلفي: يشير إلى مجمل ربح مبيعات الكاشير قبل خصم المصروفات العامة والرواتب
        /// </summary>
        public decimal NetProfit
        {
            get => GrossProfit;
            set => GrossProfit = value;
        }

        public decimal ProfitPercentage { get; set; }
        public int InvoiceCount { get; set; }
    }
}
