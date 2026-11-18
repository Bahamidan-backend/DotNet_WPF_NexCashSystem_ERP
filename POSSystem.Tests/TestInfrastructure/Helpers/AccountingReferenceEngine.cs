using System;
using System.Collections.Generic;
using System.Linq;

namespace POSSystem.Tests.TestInfrastructure.Helpers
{
    /// <summary>
    /// Independent accounting oracle for financial reconciliation.
    /// Does NOT reuse any calculation logic from POSSystem.WPF.
    /// </summary>
    public static class AccountingReferenceEngine
    {
        public static decimal ComputeExpectedCashBoxBalance(
            decimal startingCash,
            IEnumerable<(bool IsInflow, decimal Amount, bool IsPostCloseSettlement)> transactions)
        {
            decimal total = startingCash;
            foreach (var tx in transactions)
            {
                if (tx.IsPostCloseSettlement) continue;
                total += tx.IsInflow ? tx.Amount : -tx.Amount;
            }
            return Math.Round(total, 2);
        }

        public static decimal ComputeExpectedWalletBalance(
            decimal openingBalance,
            IEnumerable<(decimal Inflow, decimal Outflow)> entries)
        {
            decimal balance = openingBalance;
            foreach (var e in entries)
            {
                balance += e.Inflow - e.Outflow;
            }
            return Math.Round(balance, 2);
        }

        public static decimal ComputeExpectedOrderTotal(
            IEnumerable<(decimal UnitPrice, int Quantity)> items,
            decimal discountAmount,
            decimal serviceCharge,
            decimal taxAmount)
        {
            decimal subTotal = items.Sum(i => Math.Round(i.UnitPrice * i.Quantity, 2));
            decimal afterDiscount = subTotal - discountAmount;
            return Math.Round(afterDiscount + serviceCharge + taxAmount, 2);
        }

        public static decimal ComputeExpectedCustomerDebtRemaining(
            decimal originalDebt,
            IEnumerable<decimal> payments,
            IEnumerable<decimal> creditReturns)
        {
            decimal paid = payments.Sum();
            decimal returned = creditReturns.Sum();
            return Math.Max(0m, Math.Round(originalDebt - paid - returned, 2));
        }

        public static decimal ComputeExpectedSupplierBalance(
            IEnumerable<decimal> creditPurchases,
            IEnumerable<decimal> payments,
            IEnumerable<decimal> creditPurchaseReturns)
        {
            return Math.Round(
                creditPurchases.Sum() - payments.Sum() - creditPurchaseReturns.Sum(),
                2);
        }

        public static ReferencePnLResult ComputeReferencePnL(
            decimal grossSales,
            decimal discounts,
            decimal returns,
            decimal cogs,
            decimal packagingCost,
            decimal salaries,
            decimal rent,
            decimal utilities,
            decimal otherExpenses,
            decimal stockWasteAndShortage)
        {
            decimal netSales = Math.Round(grossSales - discounts - returns, 2);
            decimal totalDirectCost = Math.Round(cogs + packagingCost, 2);
            decimal grossProfit = Math.Round(netSales - totalDirectCost, 2);
            decimal operatingExpenses = Math.Round(salaries + rent + utilities + otherExpenses + stockWasteAndShortage, 2);
            decimal netOperatingProfit = Math.Round(grossProfit - operatingExpenses, 2);

            return new ReferencePnLResult(
                NetSales: netSales,
                TotalDirectCost: totalDirectCost,
                GrossProfit: grossProfit,
                OperatingExpenses: operatingExpenses,
                NetOperatingProfit: netOperatingProfit);
        }
    }

    public sealed record ReferencePnLResult(
        decimal NetSales,
        decimal TotalDirectCost,
        decimal GrossProfit,
        decimal OperatingExpenses,
        decimal NetOperatingProfit);
}
