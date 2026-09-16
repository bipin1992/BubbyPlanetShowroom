using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class SellingCalculationsTests
    {
        private static readonly DateTime Today = new DateTime(2026, 9, 8);
        private static readonly DateTime Yesterday = Today.AddDays(-1);

        [Fact]
        public void SameDayBill_WithExtra_StillCountsFullSale()
        {
            Assert.True(SellingCalculations.IncludeOrderSaleInTotal(true));
            Assert.False(SellingCalculations.ExtraCountsInTotalSale(Today, Today, Today));
            Assert.Equal(2364.3m, SellingCalculations.BilledSaleTotal(2364.3m));
        }

        [Fact]
        public void SameDayBill_WithoutExtra_CountsGrandTotal()
        {
            Assert.True(SellingCalculations.IncludeOrderSaleInTotal(false));
            Assert.Equal(339.15m, SellingCalculations.BilledSaleTotal(339.15m));
        }

        [Fact]
        public void PreviousDayBill_ExtraToday_DoesNotChangeBilledSale()
        {
            Assert.False(SellingCalculations.ExtraCountsInTotalSale(Today, Today, Yesterday));
            Assert.Equal(339.15m, SellingCalculations.CombineTotalSale(339.15m, 255m));
        }

        [Fact]
        public void MissingOriginalBill_ExtraStillNotInBilledSale()
        {
            Assert.False(SellingCalculations.ExtraCountsInTotalSale(Today, Today, null));
        }

        [Fact]
        public void Dump08Sep_SellingTotal_MatchesRevenueBills()
        {
            decimal todayBills =
                142.2m + 2364.3m + 99m + 648.2m + 856.8m + 305.1m + 269.1m + 269.1m;

            Assert.Equal(4953.8m, todayBills);
            Assert.Equal(4953.8m, SellingCalculations.CombineTotalSale(todayBills, 36m));
        }

        [Fact]
        public void DumpSepDays_SellingMatchesRevenueLineTotals()
        {
            Assert.Equal(3466.70m, SellingCalculations.BilledSaleTotal(3466.70m));
            Assert.Equal(5243.55m, SellingCalculations.BilledSaleTotal(5243.55m));
            Assert.Equal(6750.70m, SellingCalculations.BilledSaleTotal(6750.70m));
            Assert.Equal(4251.05m, SellingCalculations.BilledSaleTotal(4251.05m));
            Assert.Equal(5020.55m, SellingCalculations.BilledSaleTotal(5020.55m));
            Assert.Equal(7220.65m, SellingCalculations.BilledSaleTotal(7220.65m));
            Assert.Equal(3094.25m, SellingCalculations.BilledSaleTotal(3094.25m));
            Assert.Equal(4953.80m, SellingCalculations.BilledSaleTotal(4953.80m));

            decimal month = 3466.70m + 5243.55m + 6750.70m + 4251.05m
                + 5020.55m + 7220.65m + 3094.25m + 4953.80m;
            Assert.Equal(40001.25m, month);
            Assert.Equal(40001.25m, SellingCalculations.CombineTotalSale(month, 659.93m + 170m + 7.65m + 30.05m + 36m));
        }

        [Fact]
        public void ResolveFilterRange_Today_IsSingleDay()
        {
            SellingCalculations.ResolveFilterRange("Today", Today, Yesterday, Today.AddDays(3), out DateTime from, out DateTime to);
            Assert.Equal(Today, from);
            Assert.Equal(Today, to);
        }

        [Fact]
        public void ResolveFilterRange_Last7Days_MatchesRevenue()
        {
            SellingCalculations.ResolveFilterRange("Last 7 Days", Today, Today, Today, out DateTime from, out DateTime to);
            Assert.Equal(new DateTime(2026, 9, 1), from);
            Assert.Equal(Today, to);
        }

        [Fact]
        public void ResolveFilterRange_Weekly_IsMondayToSunday()
        {
            SellingCalculations.ResolveFilterRange("Weekly", Today, Today, Today, out DateTime from, out DateTime to);
            Assert.Equal(new DateTime(2026, 9, 7), from);
            Assert.Equal(new DateTime(2026, 9, 13), to);
        }

        [Fact]
        public void ResolveFilterRange_MonthlyAndYearly()
        {
            SellingCalculations.ResolveFilterRange("Monthly", Today, Today, Today, out DateTime from, out DateTime to);
            Assert.Equal(new DateTime(2026, 9, 1), from);
            Assert.Equal(new DateTime(2026, 9, 30), to);

            SellingCalculations.ResolveFilterRange("Yearly", Today, Today, Today, out from, out to);
            Assert.Equal(new DateTime(2026, 1, 1), from);
            Assert.Equal(new DateTime(2026, 12, 31), to);
        }

        [Fact]
        public void ResolveFilterRange_Custom_SwapsReversedDates()
        {
            SellingCalculations.ResolveFilterRange("Custom", Today, Today, Yesterday, out DateTime from, out DateTime to);
            Assert.Equal(Yesterday, from);
            Assert.Equal(Today, to);
        }

        [Fact]
        public void Dump08Sep_FilterRanges_KeepBilledDays()
        {
            SellingCalculations.ResolveFilterRange("Today", Today, Today, Today, out DateTime from, out DateTime to);
            Assert.True(SellingCalculations.BillDateInRange(new DateTime(2026, 9, 8, 20, 31, 19), from, to));
            Assert.False(SellingCalculations.BillDateInRange(new DateTime(2026, 9, 7), from, to));

            SellingCalculations.ResolveFilterRange("Monthly", Today, Today, Today, out from, out to);
            Assert.True(SellingCalculations.BillDateInRange(new DateTime(2026, 9, 1), from, to));
            Assert.True(SellingCalculations.BillDateInRange(Today, from, to));
            Assert.False(SellingCalculations.BillDateInRange(new DateTime(2026, 8, 31), from, to));
        }
    }
}
