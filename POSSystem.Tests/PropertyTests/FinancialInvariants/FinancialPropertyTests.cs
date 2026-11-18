using FsCheck;
using FsCheck.Xunit;
using POSSystem.Tests.TestInfrastructure.Helpers;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;

namespace POSSystem.Tests.PropertyTests.FinancialInvariants
{
    public class FinancialPropertyTests
    {
        [Property(MaxTest = 200)]
        public bool PositiveDeposit_AlwaysIncreasesBalanceByExactAmount(PositiveInt initial, PositiveInt deposit)
        {
            decimal start = initial.Get;
            decimal dep = deposit.Get;
            decimal after = AccountingReferenceEngine.ComputeExpectedWalletBalance(start, new[] { (dep, 0m) });
            return after == start + dep && after > start;
        }

        [Property(MaxTest = 200)]
        public bool TransferBetweenTwoAccounts_AlwaysConservesTotalMoney(PositiveInt balA, PositiveInt balB, PositiveInt rawAmount)
        {
            decimal a = balA.Get;
            decimal b = balB.Get;
            decimal amount = Math.Min(a, (decimal)rawAmount.Get);

            decimal aAfter = AccountingReferenceEngine.ComputeExpectedWalletBalance(a, new[] { (0m, amount) });
            decimal bAfter = AccountingReferenceEngine.ComputeExpectedWalletBalance(b, new[] { (amount, 0m) });

            return aAfter >= 0m && (aAfter + bAfter) == (a + b);
        }

        [Property(MaxTest = 200)]
        public bool CustomerDebtRemaining_NeverExceedsOriginalAndNeverGoesNegative(PositiveInt debt, PositiveInt pay1, PositiveInt pay2)
        {
            decimal orig = debt.Get;
            decimal rem = AccountingReferenceEngine.ComputeExpectedCustomerDebtRemaining(
                orig,
                new decimal[] { pay1.Get, pay2.Get },
                Array.Empty<decimal>());

            return rem >= 0m && rem <= orig;
        }

        [Property(MaxTest = 200)]
        public bool UnitConversion_KgToGramAndBack_PreservesQuantity(PositiveInt grams)
        {
            decimal g = grams.Get;
            decimal kg = UnitConversionHelper.ConvertQuantity(g, Recipe.PortionUnit.Gram, Recipe.PortionUnit.Kg);
            decimal backToG = UnitConversionHelper.ConvertQuantity(kg, Recipe.PortionUnit.Kg, Recipe.PortionUnit.Gram);
            return Math.Abs(backToG - g) < 0.0001m;
        }
    }
}
