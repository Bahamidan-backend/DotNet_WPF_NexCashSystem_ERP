using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.Assertions;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.Wallets
{
    public class WalletTests
    {
        [Fact]
        public async Task WithdrawFromWallet_WhenBalanceIs60AndAmountIs120_ShouldRejectAndLeaveBalanceUnchanged()
        {
            using var db = new IsolatedSqliteTestDb();
            var accountService = db.ServiceProvider.GetRequiredService<IFinancialAccountService>();

            var wallet = await accountService.CreateAccountAsync(
                FinancialEntityBuilders.BuildWalletAccount("محفظة جيب اختبار", initialBalance: 60m, allowOverdraft: false));

            Func<Task> act = () => accountService.RecordWithdrawalAsync(
                wallet.Id,
                120m,
                "WTH-TEST-1",
                "محاولة سحب أكبر من الرصيد",
                null);

            await act.Should().ThrowAsync<InvalidOperationException>();

            var reloaded = await accountService.GetAccountByIdAsync(wallet.Id);
            reloaded!.CurrentBalance.ShouldMatchAmount(60m);

            var txs = await db.Context.FinancialAccountTransactions
                .AsNoTracking()
                .Where(t => t.FinancialAccountId == wallet.Id && t.TransactionType == FinancialTransactionType.Withdrawal)
                .ToListAsync();
            txs.Should().BeEmpty();
        }

        [Fact]
        public async Task WithdrawFromWallet_EqualToBalance_ShouldSucceedWithZeroBalance()
        {
            using var db = new IsolatedSqliteTestDb();
            var accountService = db.ServiceProvider.GetRequiredService<IFinancialAccountService>();

            var wallet = await accountService.CreateAccountAsync(
                FinancialEntityBuilders.BuildWalletAccount("محفظة كريمي اختبار", initialBalance: 60m));

            await accountService.RecordWithdrawalAsync(wallet.Id, 60m, "WTH-EXACT", "سحب كامل الرصيد", null);

            var reloaded = await accountService.GetAccountByIdAsync(wallet.Id);
            reloaded!.CurrentBalance.ShouldMatchAmount(0m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public async Task DepositOrWithdraw_ZeroOrNegative_ShouldThrowArgumentException(decimal amount)
        {
            using var db = new IsolatedSqliteTestDb();
            var accountService = db.ServiceProvider.GetRequiredService<IFinancialAccountService>();
            var wallet = await accountService.CreateAccountAsync(
                FinancialEntityBuilders.BuildWalletAccount("محفظة اختبار", initialBalance: 100m));

            Func<Task> depAct = () => accountService.RecordDepositAsync(wallet.Id, amount, "DEP-BAD", "إيداع غير صالح", null);
            Func<Task> wthAct = () => accountService.RecordWithdrawalAsync(wallet.Id, amount, "WTH-BAD", "سحب غير صالح", null);

            await depAct.Should().ThrowAsync<ArgumentException>();
            await wthAct.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task TransferBetweenAccounts_ShouldConserveTotalMoneyAcrossBothAccounts()
        {
            using var db = new IsolatedSqliteTestDb();
            var accountService = db.ServiceProvider.GetRequiredService<IFinancialAccountService>();

            var source = await accountService.CreateAccountAsync(
                FinancialEntityBuilders.BuildWalletAccount("محفظة مصدر", initialBalance: 10_000m));
            var dest = await accountService.CreateAccountAsync(
                FinancialEntityBuilders.BuildWalletAccount("محفظة وجهة", initialBalance: 2_500m));

            decimal totalBefore = source.CurrentBalance + dest.CurrentBalance;

            await accountService.TransferBetweenAccountsAsync(source.Id, dest.Id, 3_750.50m, "TRF-001", "تحويل اختبار");

            var sourceAfter = await accountService.GetAccountByIdAsync(source.Id);
            var destAfter = await accountService.GetAccountByIdAsync(dest.Id);

            sourceAfter!.CurrentBalance.ShouldMatchAmount(6_249.50m);
            destAfter!.CurrentBalance.ShouldMatchAmount(6_250.50m);
            (sourceAfter.CurrentBalance + destAfter.CurrentBalance).ShouldMatchAmount(totalBefore);
        }
    }
}
