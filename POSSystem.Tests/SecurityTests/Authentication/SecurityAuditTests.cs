using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Services;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.SecurityTests.Authentication
{
    public class SecurityAuditTests
    {
        [Fact]
        public void PasswordHasher_ShouldUsePbkdf2WithUniqueSaltAndVerifyCorrectly()
        {
            string h1 = PasswordHasher.HashPassword("Secret@123");
            string h2 = PasswordHasher.HashPassword("Secret@123");

            h1.Should().StartWith("PBKDF2$100000$");
            h1.Should().NotBe(h2, "each password hash must have a unique cryptographic salt");
            PasswordHasher.VerifyPassword("Secret@123", h1).Should().BeTrue();
            PasswordHasher.VerifyPassword("WrongPassword", h1).Should().BeFalse();
        }

        [Fact]
        public async Task ValidateUserAsync_InactiveUser_ShouldBeRejected()
        {
            using var db = new IsolatedSqliteTestDb();
            var userService = db.ServiceProvider.GetRequiredService<IUserService>();

            var inactiveUser = FinancialEntityBuilders.BuildUser(email: "disabled@nexcash.test", isActive: false);
            inactiveUser.Password = "StrongPass@123";
            await userService.CreateUserAsync(inactiveUser);

            // Force inactive state
            inactiveUser.IsActive = false;
            await db.Context.SaveChangesAsync();

            bool canLogin = await userService.ValidateUserAsync("disabled@nexcash.test", "StrongPass@123");
            canLogin.Should().BeFalse("disabled/inactive accounts must not pass authentication");
        }

        [Fact]
        public void LicenseService_TamperedSignatureOrWrongMachine_MustBeRejected()
        {
            var licenseService = new LicenseService();
            string fakeKey = "2099-12-31|WRONG-MACHINE|INVALID-SIGNATURE";

            bool valid = licenseService.ValidateLicense(fakeKey, out string message);
            valid.Should().BeFalse();
            message.Should().NotBeNullOrWhiteSpace();
        }
    }
}
