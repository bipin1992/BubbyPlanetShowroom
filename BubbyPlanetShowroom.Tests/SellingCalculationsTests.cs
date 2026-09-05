using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class SellingCalculationsTests
    {
        private static readonly DateTime Today = new DateTime(2026, 9, 4);
        private static readonly DateTime Yesterday = Today.AddDays(-1);

        [Fact]
        public void T99ToT399_SellingTotalIsOnlyExtra255()
        {
            bool includeSale = SellingCalculations.IncludeOrderSaleInTotal(hasCollectExtraInPeriod: true);
            decimal orderSale = includeSale ? 339.15m : 0m;
            decimal extra = SellingCalculations.ExtraCountsInTotalSale(Today, Today, Today)
                ? 255m
                : 0m;

            Assert.False(includeSale);
            Assert.Equal(255m, SellingCalculations.CombineTotalSale(orderSale, extra));
        }

        [Fact]
        public void SameDayBill_WithoutExtra_CountsGrandTotal()
        {
            Assert.True(SellingCalculations.IncludeOrderSaleInTotal(false));
            Assert.Equal(339.15m, SellingCalculations.CombineTotalSale(339.15m, 0m));
        }

        [Fact]
        public void SameDayBill_339Plus255_MustNotBecome594()
        {
            decimal extra = 255m;
            decimal orderSale = SellingCalculations.IncludeOrderSaleInTotal(true) ? 339.15m : 0m;
            Assert.Equal(255m, SellingCalculations.CombineTotalSale(orderSale, extra));
        }

        [Fact]
        public void PreviousDayBill_ExtraToday_IsAddedToTotalSale()
        {
            Assert.True(SellingCalculations.ExtraCountsInTotalSale(Today, Today, Yesterday));
            Assert.True(SellingCalculations.IncludeOrderSaleInTotal(false));
            Assert.Equal(594.15m, SellingCalculations.CombineTotalSale(339.15m, 255m));
        }

        [Fact]
        public void MissingOriginalBill_ExtraIsAdded()
        {
            Assert.True(SellingCalculations.ExtraCountsInTotalSale(Today, Today, null));
        }

        [Fact]
        public void Order1211_SameDayOnlineExtra_TotalSaleIsOnly85()
        {
            bool includeSale = SellingCalculations.IncludeOrderSaleInTotal(hasCollectExtraInPeriod: true);
            decimal orderSale = includeSale ? 254.15m : 0m;
            decimal extra = 85m;

            Assert.False(includeSale);
            Assert.Equal(85m, SellingCalculations.CombineTotalSale(orderSale, extra));
        }

        [Fact]
        public void Dump04Sep_SellingTotal_MatchesScreenshot4381()
        {
            // Cash/online bills that had no same-day extra, plus extras 85 + 300.
            decimal salesWithoutExtraBills =
                652.8m + 677.45m + 398m + 227.8m + 1613.3m + 173.4m + 254.15m;
            decimal extras = 85m + 300m;

            Assert.Equal(3996.90m, salesWithoutExtraBills);
            Assert.Equal(4381.90m, SellingCalculations.CombineTotalSale(salesWithoutExtraBills, extras));
        }
    }
}
