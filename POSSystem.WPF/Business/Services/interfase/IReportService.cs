
using POSSystem.WPF.Models.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IReportService
    {
        Task<IEnumerable<DailySalesReport>> GetDailySalesReportsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CashierSalesReport>> GetCashierSalesReportsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DeliveryReport>> GetDeliveryReportsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CashierProfitReport>> GetCashierProfitReportsAsync(DateTime startDate, DateTime endDate);

        Task<IEnumerable<CashBoxReport>> GetCashBoxReportsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ShiftReport>> GetShiftReportsAsync(DateTime startDate, DateTime endDate);
        Task<ShiftReportDto> GetUserShiftReportAsync(User user);
        Task<CashierAuditSummaryDto> GetCashierAuditSummaryAsync(DateTime startDate, DateTime endDate, int? cashierId = null);
    }
}
