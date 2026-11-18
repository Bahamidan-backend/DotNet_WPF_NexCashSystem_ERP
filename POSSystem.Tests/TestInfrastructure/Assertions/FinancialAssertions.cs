using FluentAssertions;

namespace POSSystem.Tests.TestInfrastructure.Assertions
{
    public static class FinancialAssertions
    {
        public static void ShouldMatchAmount(this decimal actual, decimal expected, string because = "")
        {
            actual.Should().BeApproximately(expected, 0.01m, because);
        }

        public static void ShouldNeverBeNegative(this decimal actual, string accountName)
        {
            actual.Should().BeGreaterThanOrEqualTo(0m, $"رصيد '{accountName}' لا يجب أن يصبح سالباً دون تصريح");
        }
    }
}
