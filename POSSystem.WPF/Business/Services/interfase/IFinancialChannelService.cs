using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IFinancialChannelService
    {
        /// <summary>
        /// الحصول على الرصيد المتاح حالياً لجهة صرف محددة (صندوق أو محفظة أو بنك)
        /// </summary>
        Task<decimal> GetAvailableBalanceAsync(string channelKeyOrTag, int? shiftId = null);

        /// <summary>
        /// التحقق الصارم من كفاية الرصيد قبل الصرف. يرمي استثناءً واضحاً إذا كان الرصيد غير كافٍ.
        /// </summary>
        Task ValidateSufficientBalanceAsync(string channelKeyOrTag, decimal requiredAmount, int? shiftId = null, int? excludedVoucherId = null);

        /// <summary>
        /// جلب قائمة أرصدة كافة المحافظ والصناديق النشطة دفعة واحدة لعرضها في الواجهات
        /// </summary>
        Task<List<FinancialChannelInfo>> GetAllChannelBalancesAsync(int? activeShiftId = null);
    }
}

