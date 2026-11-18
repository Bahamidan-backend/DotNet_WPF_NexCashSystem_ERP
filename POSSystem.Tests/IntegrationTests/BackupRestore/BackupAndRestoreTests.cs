using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.IntegrationTests.BackupRestore
{
    public class BackupAndRestoreTests
    {
        [Fact]
        public async Task BackupAndRestore_ValidSnapshot_ShouldPreserveAllFinancialData()
        {
            using var db = new IsolatedSqliteTestDb(useTempFile: true);
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 75_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var cashBox = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();
            await cashBox.CreateTransactionAsync(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.Income,
                Amount = 25_000m,
                Name = "مبيعات قبل النسخ"
            });

            var backupService = db.ServiceProvider.GetRequiredService<IBackupService>();
            var backupDir = Path.Combine(Path.GetDirectoryName(db.TempDbFilePath!)!, "Backups");
            var backupResult = await backupService.CreateBackupAsync("TestBackup", shift.Id, shift.ShiftNumber, backupDir);
            backupResult.IsSuccess.Should().BeTrue();
            File.Exists(backupResult.FilePath).Should().BeTrue();

            // Mutate database after backup
            await cashBox.CreateTransactionAsync(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.Expense,
                Amount = 50_000m,
                Name = "مصروف بعد النسخ"
            });

            // Restore from backup
            var restoreResult = await backupService.RestoreBackupAsync(backupResult.FilePath!);
            restoreResult.IsSuccess.Should().BeTrue();

            using var verifyCtx = db.CreateIndependentContext();
            var txCount = await verifyCtx.CashBoxTransactions.CountAsync();
            txCount.Should().Be(1);
        }

        [Fact]
        public async Task RestoreBackup_CorruptedZip_ShouldFailAndNotOverwriteLiveDb()
        {
            using var db = new IsolatedSqliteTestDb(useTempFile: true);
            var backupService = db.ServiceProvider.GetRequiredService<IBackupService>();

            var corruptZip = Path.Combine(Path.GetDirectoryName(db.TempDbFilePath!)!, "corrupt.zip");
            await File.WriteAllTextAsync(corruptZip, "NOT_A_VALID_ZIP_ARCHIVE");

            var result = await backupService.RestoreBackupAsync(corruptZip);
            result.IsSuccess.Should().BeFalse();
        }
    }
}
