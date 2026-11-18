using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IFinancialAccountService
    {
        Task EnsureDefaultAccountsSeededAsync();
        Task<List<FinancialAccount>> GetAllAccountsAsync(bool includeInactive = true);
        Task<FinancialAccount?> GetAccountByIdAsync(int id);
        Task<FinancialAccount?> FindAccountByNameOrBankAsync(string? nameOrBank);
        Task<FinancialAccount> CreateAccountAsync(FinancialAccount account, int? userId = null, string? userName = null);
        Task<FinancialAccount> UpdateAccountAsync(FinancialAccount account, int? userId = null, string? userName = null);
        Task ToggleAccountStatusAsync(int accountId, bool isActive, int? userId = null, string? userName = null);
        Task<FinancialAccountTransaction> RecordDepositAsync(int accountId, decimal amount, string? referenceNumber, string description, string? notes, int? userId = null, string? userName = null, string? sourceType = "Manual", int? sourceId = null, FinancialTransactionType txType = FinancialTransactionType.Deposit);
        Task<FinancialAccountTransaction> RecordWithdrawalAsync(int accountId, decimal amount, string? referenceNumber, string description, string? notes, int? userId = null, string? userName = null, string? sourceType = "Manual", int? sourceId = null, FinancialTransactionType txType = FinancialTransactionType.Withdrawal, bool allowNegativeOverride = false);
        Task<(FinancialAccountTransaction OutTx, FinancialAccountTransaction InTx)> TransferBetweenAccountsAsync(int fromAccountId, int toAccountId, decimal amount, string? referenceNumber, string? notes, int? userId = null, string? userName = null, bool isOwnerDeposit = false);
        Task<List<FinancialAccountTransaction>> GetAccountStatementAsync(int accountId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
