using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    /// <summary>
    /// One ₹1000 item, cost ₹400, 10 in stock.
    /// Selling, Revenue, closing, receipt, hold, return and stock must agree.
    /// </summary>
    public class ChannelValidationTests
    {
        private const decimal Price = 1000m;
        private const decimal Cost = 400m;
        private const int Stock = 10;

        private static CouponSaleMath.BillOut Checkout(
            decimal autoPercent,
            decimal mallPercent,
            decimal rewardPercent,
            bool rewardOn,
            bool mallIsManual,
            string? couponCode,
            decimal couponRupees,
            bool cashBill = true)
        {
            decimal percent = CouponSaleMath.BillDiscountPercent(
                autoPercent, mallPercent, rewardPercent, rewardOn, mallIsManual);

            return CouponSaleMath.Settle(
                new[]
                {
                    new CouponSaleMath.LineIn
                    {
                        Price = Price,
                        Qty = 1,
                        DiscountPercent = percent,
                        CostPerUnit = Cost
                    }
                },
                couponCode,
                couponRupees,
                cashBill);
        }

        private static void AssertMoney(
            CouponSaleMath.BillOut bill,
            decimal payable,
            decimal profit,
            bool cash,
            bool showCoupon)
        {
            Assert.Equal(payable, bill.Payable);
            Assert.Equal(payable, bill.SellingSale);
            Assert.Equal(payable, bill.RevenueSales);
            Assert.Equal(profit, bill.Profit);
            Assert.Equal(profit, bill.RevenueProfit);
            Assert.Equal(cash ? payable : 0m, bill.ClosingCash);
            if (showCoupon)
                Assert.Contains("Coupon:", string.Join("|", bill.CouponLines));
            else
                Assert.Empty(bill.CouponLines);
        }

        [Fact]
        public void Case1_CouponCodeOnly_NoRupees_ShowsCodeAndDoesNotCutMoney()
        {
            CouponSaleMath.BillOut bill = Checkout(0, 0, 0, false, false, "DIWALI", 0m);

            AssertMoney(bill, 1000m, 600m, cash: true, showCoupon: true);
            Assert.Equal("DIWALI", bill.CouponCode);
            Assert.Equal(0m, bill.CouponDiscount);
            Assert.Contains("Coupon Discount: -0.00", bill.CouponLines);
            Assert.Contains("PAYABLE: 1000.00", bill.CouponLines);
            Assert.Equal(9, CouponSaleMath.StockAfterSale(Stock, 1));
        }

        [Fact]
        public void Case2_CouponCodePlusReward()
        {
            CouponSaleMath.BillOut bill = Checkout(0, 0, 10m, rewardOn: true, mallIsManual: false, "DIWALI", 100m);

            Assert.Equal(900m, bill.ItemsBeforeCoupon);
            AssertMoney(bill, 800m, 400m, cash: true, showCoupon: true);
            Assert.Equal(100m, bill.CouponDiscount);
            Assert.Contains("Coupon Discount: -100.00", bill.CouponLines);
            Assert.Contains("PAYABLE: 800.00", bill.CouponLines);
            Assert.Equal(9, CouponSaleMath.StockAfterSale(Stock, 1));
        }

        [Fact]
        public void Case3_RewardCancel_KeepsCouponAndRestoresRewardPercent()
        {
            CouponSaleMath.BillOut withReward = Checkout(0, 0, 10m, true, false, "DIWALI", 100m);
            CouponSaleMath.BillOut cancelled = Checkout(0, 0, 10m, rewardOn: false, mallIsManual: false, "DIWALI", 100m);

            Assert.Equal(800m, withReward.Payable);
            AssertMoney(cancelled, 900m, 500m, cash: true, showCoupon: true);
            Assert.Equal("DIWALI", cancelled.CouponCode);
            Assert.Equal(100m, cancelled.CouponDiscount);
            Assert.Equal(9, CouponSaleMath.StockAfterSale(Stock, 1));
        }

        [Fact]
        public void Case4_NoCoupon_NoReward()
        {
            CouponSaleMath.BillOut bill = Checkout(0, 0, 0, false, false, "", 0m);

            Assert.False(bill.CouponApplied);
            AssertMoney(bill, 1000m, 600m, cash: true, showCoupon: false);
            Assert.Equal(9, CouponSaleMath.StockAfterSale(Stock, 1));
        }

        [Fact]
        public void Case5_RewardOnly()
        {
            CouponSaleMath.BillOut bill = Checkout(0, 0, 10m, true, false, "", 0m);

            Assert.False(bill.CouponApplied);
            AssertMoney(bill, 900m, 500m, cash: true, showCoupon: false);
        }

        [Fact]
        public void Case6_RewardPlusCoupon_MallDiscountZero_SameAsRewardPlusCoupon()
        {
            CouponSaleMath.BillOut mallZero = Checkout(0, mallPercent: 0m, 10m, true, false, "DIWALI", 100m);
            CouponSaleMath.BillOut rewardAndCoupon = Checkout(0, 0, 10m, true, false, "DIWALI", 100m);

            Assert.Equal(rewardAndCoupon.Payable, mallZero.Payable);
            AssertMoney(mallZero, 800m, 400m, cash: true, showCoupon: true);
        }

        [Fact]
        public void Case7_MallAndRewardStack_ThenCoupon_AndManualMallBlocksReward()
        {
            CouponSaleMath.BillOut stacked = Checkout(0, mallPercent: 15m, rewardPercent: 10m, true, mallIsManual: false, "SAVE", 100m);
            Assert.Equal(750m, stacked.ItemsBeforeCoupon);
            AssertMoney(stacked, 650m, 250m, cash: true, showCoupon: true);

            CouponSaleMath.BillOut manualMall = Checkout(0, mallPercent: 0m, rewardPercent: 10m, true, mallIsManual: true, "SAVE", 100m);
            AssertMoney(manualMall, 900m, 500m, cash: true, showCoupon: true);

            decimal twoItemPercent = CouponSaleMath.BillDiscountPercent(0, 0, 0, false, false);
            CouponSaleMath.BillOut split = CouponSaleMath.Settle(
                new[]
                {
                    new CouponSaleMath.LineIn { Price = 500m, Qty = 2, DiscountPercent = twoItemPercent, CostPerUnit = 200m },
                    new CouponSaleMath.LineIn { Price = 300m, Qty = 1, DiscountPercent = twoItemPercent, CostPerUnit = 100m }
                },
                "SAVE100",
                100m);

            Assert.Equal(25m, split.Lines[0].PerUnitCoupon);
            Assert.Equal(50m, split.Lines[1].PerUnitCoupon);
            Assert.Equal(1200m, split.SellingSale);
            Assert.Equal(1200m, split.RevenueSales);
            Assert.Equal(1200m, split.ClosingCash);
        }

        [Fact]
        public void Case8_CashAndOnline_SaleReturnAndReceipt()
        {
            CouponSaleMath.BillOut cash = Checkout(0, 0, 10m, true, false, "DIWALI", 100m, cashBill: true);
            CouponSaleMath.BillOut online = Checkout(0, 0, 10m, true, false, "DIWALI", 100m, cashBill: false);

            Assert.Equal(800m, cash.Payable);
            Assert.Equal(800m, cash.ClosingCash);
            Assert.Equal(800m, online.SellingSale);
            Assert.Equal(800m, online.RevenueSales);
            Assert.Equal(0m, online.ClosingCash);
            Assert.Equal(cash.CouponLines, online.CouponLines);

            CouponSaleMath.BillOut afterCashReturn = CouponSaleMath.AfterPartialReturn(cash, 0, 1, cashBill: true);
            Assert.Equal(0m, afterCashReturn.Payable);
            Assert.Equal(0m, afterCashReturn.SellingSale);
            Assert.Equal(0m, afterCashReturn.RevenueSales);
            Assert.Equal(0m, afterCashReturn.ClosingCash);
            Assert.Equal(800m, CouponSaleMath.CashRefund(cash.Lines[0], 1));
            Assert.Equal(10, CouponSaleMath.StockAfterReturn(CouponSaleMath.StockAfterSale(Stock, 1), 1));

            decimal onlineRefundOnCashDrawer = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Online", 800m, new System.DateTime(2026, 9, 26), new System.DateTime(2026, 9, 26), "Online");
            Assert.Equal(0m, onlineRefundOnCashDrawer);
            Assert.Equal(0m, ClosingCashCalculations.CombineCashFromDb(online.ClosingCash, onlineRefundOnCashDrawer));

            decimal cashRefundNextDay = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", 800m, new System.DateTime(2026, 9, 26), new System.DateTime(2026, 9, 25), "Cash");
            Assert.Equal(-800m, cashRefundNextDay);
        }

        [Fact]
        public void Case9_HoldBill_RestoresCouponAndReward()
        {
            string heldCode = CouponSaleMath.RememberHoldCoupon("  diwali ");
            Assert.Equal("DIWALI", heldCode);

            CouponSaleMath.BillOut original = Checkout(0, 0, 10m, true, false, "DIWALI", 100m);
            CouponSaleMath.BillOut resumed = Checkout(0, 0, 10m, true, false, heldCode, 100m);
            CouponSaleMath.BillOut heldWithoutReward = Checkout(0, 0, 10m, rewardOn: false, mallIsManual: false, heldCode, 100m);

            Assert.Equal(original.Payable, resumed.Payable);
            Assert.Equal(original.CouponLines, resumed.CouponLines);
            Assert.Equal(800m, resumed.SellingSale);
            Assert.Equal(900m, heldWithoutReward.Payable);
        }

        [Fact]
        public void Case10_ReturnedItemGoesBackToStock_ExchangeMovesBoth()
        {
            Assert.Equal(9, CouponSaleMath.StockAfterSale(10, 1));
            Assert.Equal(10, CouponSaleMath.StockAfterSale(10, 0));
            Assert.Equal(2, CouponSaleMath.StockAfterSale(2, 5));

            int afterSale = CouponSaleMath.StockAfterSale(10, 2);
            Assert.Equal(8, afterSale);
            Assert.Equal(9, CouponSaleMath.StockAfterReturn(afterSale, 1));
            Assert.Equal(10, CouponSaleMath.StockAfterReturn(afterSale, 2));
            Assert.Equal(8, CouponSaleMath.StockAfterReturn(afterSale, 0));

            int sweater = CouponSaleMath.StockAfterReturn(CouponSaleMath.StockAfterSale(6, 1), 1);
            int newShirt = CouponSaleMath.StockAfterExchange(4, returnedQty: 0, issuedQty: 1);
            Assert.Equal(6, sweater);
            Assert.Equal(3, newShirt);
        }
    }
}
