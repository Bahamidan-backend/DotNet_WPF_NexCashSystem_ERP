using System;
using System.Collections.Generic;
using System.Linq;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Helpers
{
    public static class SalesMetricsHelper
    {
        public static bool IsCountableSale(Order? order)
        {
            if (order == null) return false;
            return order.Status == OrderStatus.Completed
                || order.Status == OrderStatus.Returned
                || order.Status == OrderStatus.PartiallyReturned;
        }

        public static IEnumerable<Order> FilterCountableSales(IEnumerable<Order> orders) =>
            orders.Where(IsCountableSale);

        public static decimal SumGrossSales(IEnumerable<Order> orders) =>
            FilterCountableSales(orders).Sum(o => o.Total);

        public static int CountCountableSales(IEnumerable<Order> orders) =>
            FilterCountableSales(orders).Count();

        public static IEnumerable<ReturnOrder> FilterReturnsByDate(
            IEnumerable<ReturnOrder> returns, DateTime startDate, DateTime endDate) =>
            returns.Where(r => r != null && r.ReturnDate >= startDate && r.ReturnDate <= endDate);

        public static decimal SumReturns(IEnumerable<ReturnOrder> returns, DateTime startDate, DateTime endDate) =>
            FilterReturnsByDate(returns, startDate, endDate).Sum(r => r.TotalAmount);

        public static decimal CalculateNetSales(
            IEnumerable<Order> orders,
            IEnumerable<ReturnOrder> returns,
            DateTime startDate,
            DateTime endDate) =>
            SumGrossSales(orders) - SumReturns(returns, startDate, endDate);

        public static (decimal cash, decimal card, decimal debt) SumByPaymentMethod(IEnumerable<Order> orders)
        {
            var sales = FilterCountableSales(orders).ToList();
            decimal cash = sales.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total)
                + sales.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => POSSystem.WPF.Helpers.OrderSplitHelper.ParseSplit(o.Notes)?.CashAmount ?? 0m);
            decimal card = sales.Where(o => 
                o.PaymentMethod == PaymentMethod.CreditCard || 
                o.PaymentMethod == PaymentMethod.DebitCard || 
                o.PaymentMethod == PaymentMethod.MobilePayment || 
                o.PaymentMethod == PaymentMethod.BankTransfer).Sum(o => o.Total)
                + sales.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => POSSystem.WPF.Helpers.OrderSplitHelper.ParseSplit(o.Notes)?.CardAmount ?? 0m);
            decimal debt = sales.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).Sum(o => o.Total);
            return (cash, card, debt);
        }
    }
}