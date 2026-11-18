using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.Assertions;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.ConcurrencyTests.Wallets
{
    public class WalletConcurrencyTests
    {
        [Fact]
        public async Task ConcurrentWithdrawals_Balance100_TwoSimultaneous80_OnlyOneShouldSucceed()
        {
            using var db = new IsolatedSqliteTestDb(useTempFile: true);
            var accountService = db.ServiceProvider.GetRequiredService<IFinancialAccountService>();

            var wallet = await accountService.CreateAccountAsync(
                FinancialEntityBuilders.BuildWalletAccount("محفظة تزامن", initialBalance: 100m, allowOverdraft: false));

            var t1 = Task.Run(async () =>
            {
                try
                {
                    await accountService.RecordWithdrawalAsync(wallet.Id, 80m, "CONC-1", "سحب متزامن 1", null);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            });

            var t2 = Task.Run(async () =>
            {
                try
                {
                    await accountService.RecordWithdrawalAsync(wallet.Id, 80m, "CONC-2", "سحب متزامن 2", null);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            });

            var results = await Task.WhenAll(t1, t2);
            results.Count(r => r).Should().Be(1, "only one concurrent withdrawal of 80 from 100 can succeed");

            var finalAcc = await accountService.GetAccountByIdAsync(wallet.Id);
            finalAcc!.CurrentBalance.ShouldMatchAmount(20m);
        }
    }
}
