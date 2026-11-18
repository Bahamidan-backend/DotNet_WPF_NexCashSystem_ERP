using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IBankDepositService
    {
        Task<IEnumerable<BankDeposit>> GetAllDepositsAsync();
        Task<BankDeposit?> GetDepositByIdAsync(int id);
        Task<BankDeposit> CreateDepositAsync(BankDeposit deposit);
        Task UpdateDepositAsync(BankDeposit deposit);
        Task DeleteDepositAsync(int id);
        Task<IEnumerable<BankDeposit>> GetDepositsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
