using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class ClosingCashCalculationsTests
    {
        private static readonly DateTime Today = new DateTime(2026, 8, 26);
        private static readonly DateTime Yesterday = Today.AddDays(-1);

        [Fact]
        public void PreviousDayBill_CashExtraToday_IsAdded()
        {
            decimal extra = ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Cash", 200m, Today, Yesterday, "Cash");

            Assert.Equal(200m, extra);
            Assert.Equal(1200m, ClosingCashCalculations.CombineCashFromDb(1000m, extra));
        }

        [Fact]
        public void T99ToT399_SameDayCashExtra_ClosingShowsOnlyExtra255()
        {
            decimal extraInDrawer = ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Cash", 255m, Today, Today, "Cash");

            Assert.Equal(0m, extraInDrawer);
            Assert.True(ClosingCashCalculations.HideSaleLineWhenExtraCollected(255m));
            Assert.Equal(255m, ClosingCashCalculations.CashBillContribution(339.15m, 255m));
            Assert.Equal(339.15m, ClosingCashCalculations.CashBillContribution(339.15m, 0m));
        }

        [Fact]
        public void SameDayOnlineBill_CashExtraToday_IsAdded()
        {
            decimal extra = ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Cash", 150m, Today, Today, "Online");

            Assert.Equal(150m, extra);
        }

        [Fact]
        public void PreviousDayBill_CashRefundToday_IsSubtracted()
        {
            decimal refund = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", 400m, Today, Yesterday, "Cash");

            Assert.Equal(-400m, refund);
            Assert.Equal(600m, ClosingCashCalculations.CombineCashFromDb(1000m, refund));
        }

        [Fact]
        public void OnlineExtra_DoesNotChangeCashDrawer()
        {
            decimal extra = ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Online", 200m, Today, Yesterday, "Cash");

            Assert.Equal(0m, extra);
        }

        [Fact]
        public void T99CashToT399_OnlineExtra_CashDrawerKeepsOnlyT99()
        {
            decimal onlineOnCashBill = ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Online", 255m, Today, Today, "Cash");

            Assert.Equal(-255m, onlineOnCashBill);
            Assert.Equal(84.15m, ClosingCashCalculations.CombineCashFromDb(339.15m, onlineOnCashBill));
        }

        [Fact]
        public void Order1211_OnlineExtraOnCashBill_DrawerKeepsOriginal169()
        {
            decimal onlineExtra = ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Online", 85m, Today, Today, "Cash");

            Assert.Equal(-85m, onlineExtra);
            Assert.Equal(169.15m, ClosingCashCalculations.CombineCashFromDb(254.15m, onlineExtra));
            Assert.Equal(0m, ClosingCashCalculations.CountableCashOut(
                new[] { 85m },
                new[] { "Online payment" },
                new[] { 85m }));
        }

        [Fact]
        public void SameDayCashBill_OnlineRefund_CashStaysInDrawer()
        {
            decimal onlineRefund = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Online", 84.15m, Today, Today, "Cash");

            Assert.Equal(84.15m, onlineRefund);
            Assert.Equal(84.15m, ClosingCashCalculations.CombineCashFromDb(0m, onlineRefund));
        }

        [Fact]
        public void MissingOriginalBill_CashExtra_IsAdded()
        {
            decimal extra = ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Cash", 80m, Today, null, "Cash");

            Assert.Equal(80m, extra);
        }

        [Fact]
        public void LateBillsAfterClosing_ShowInDifference_LeftoverUnchanged()
        {
            decimal leftover = 1370.75m;
            decimal owner = 2000m;
            decimal opening = 2985m;
            decimal salesBefore = 339.15m;
            decimal salesAfter = 385.90m;

            decimal before = ClosingCashCalculations.ShownDifference(
                ClosingCashCalculations.CounterTotal(opening, salesBefore), leftover, owner);
            decimal after = ClosingCashCalculations.ShownDifference(
                ClosingCashCalculations.CounterTotal(opening, salesAfter), leftover, owner);

            Assert.Equal(1370.75m, leftover);
            Assert.Equal(
                ClosingCashCalculations.LateCashDelta(salesBefore, salesAfter),
                after - before);
        }

        [Fact]
        public void CounterTotal_IsOpeningPlusCashSale()
        {
            Assert.Equal(3324.15m, ClosingCashCalculations.CounterTotal(0m, 3324.15m));
            Assert.Equal(5000m, ClosingCashCalculations.CounterTotal(1000m, 4000m));
        }

        [Fact]
        public void EmptyOwnerAndLeftover_DoesNotShowCounterAsDifference()
        {
            Assert.Equal(0m, ClosingCashCalculations.ShownDifference(3324.15m, 0m, 0m));
            Assert.Equal(3324.15m, ClosingCashCalculations.ActualDifference(3324.15m, 0m, 0m));
        }

        [Fact]
        public void FilledBoxes_ShowRealDifference()
        {
            Assert.Equal(24.15m, ClosingCashCalculations.ShownDifference(3324.15m, 300m, 3000m));
            Assert.Equal(-50m, ClosingCashCalculations.ShownDifference(1000m, 200m, 850m));
        }

        [Fact]
        public void OnlineExtraCashOut_IsNotCountedTwice()
        {
            decimal countable = ClosingCashCalculations.CountableCashOut(
                new[] { 85m },
                new[] { "Online payment" },
                new[] { 85m });

            Assert.Equal(0m, countable);

            decimal counter = ClosingCashCalculations.CounterTotal(3490m, 3260.80m, 15m, countable);
            Assert.Equal(6765.80m, counter);
            Assert.Equal(5.80m, ClosingCashCalculations.ActualDifference(counter, 2760m, 4000m));
        }

        [Fact]
        public void OnlineExtraCashOut_KeepsRealExpense()
        {
            decimal countable = ClosingCashCalculations.CountableCashOut(
                new[] { 85m, 100m },
                new[] { "Online payment", "petrol" },
                new[] { 85m });

            Assert.Equal(100m, countable);
        }

        [Fact]
        public void MisspelledOnlineCashOut_IsStillDeduped()
        {
            Assert.Equal(0m, ClosingCashCalculations.CountableCashOut(
                new[] { 254m },
                new[] { "onlince pement kiyatha" },
                new[] { 254m }));
        }

        [Fact]
        public void UnrelatedCashOut_SameAmount_StillCounts()
        {
            Assert.Equal(85m, ClosingCashCalculations.CountableCashOut(
                new[] { 85m },
                new[] { "chai" },
                new[] { 85m }));
        }

        [Fact]
        public void DoubleCountWithoutDedup_IsTheWrongDifference()
        {
            decimal counter = ClosingCashCalculations.CounterTotal(3490m, 3260.80m, 15m, 85m);
            Assert.Equal(-79.20m, ClosingCashCalculations.ActualDifference(counter, 2760m, 4000m));
        }
    }
}
