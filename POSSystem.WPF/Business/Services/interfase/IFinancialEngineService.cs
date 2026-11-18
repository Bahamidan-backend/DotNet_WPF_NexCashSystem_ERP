using System;
using System.Threading.Tasks;
using POSSystem.WPF.Models.DTOs;

namespace POSSystem.WPF.Business.Services.interfase
{
    /// <summary>
    /// واجهة المحرك المالي والمحاسبي المخصص لقطاع المطاعم (Domain Financial & P&L Engine)
    /// يعزل الحسابات المحاسبية والقوائم المالية عن طبقة واجهة المستخدم (Presentation Layer).
    /// </summary>
    public interface IFinancialEngineService
    {
        /// <summary>
        /// توليد القوائم المالية الشاملة: المبيعات، المرتجعات، COGS، المصروفات، الرواتب، والأرباح والخسائر.
        /// </summary>
        Task<FinancialReportDto> GenerateFinancialStatementAsync(DateTime startDate, DateTime endDate);
    }
}

