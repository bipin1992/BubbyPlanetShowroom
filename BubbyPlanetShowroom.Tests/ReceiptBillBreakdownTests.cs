using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class ReceiptBillBreakdownTests
    {
        [Fact]
        public void PlainBill_ShowsAmountThenSameGrandTotal()
        {
            ReceiptBillBreakdown.Totals bill = ReceiptBillBreakdown.Build(
                new[] { new ReceiptBillBreakdown.Line(1000m, 1, 0m, 0m, 0m, false) },
                0m);

            Assert.Equal(1000m, bill.NetTotal);
            Assert.Equal(1000m, bill.GrandTotal);
            Assert.Equal("₹1000.00 (net total)   →   ₹1000.00", bill.Text);
        }

        [Fact]
        public void RewardThenCoupon_SubtractInThatOrder()
        {
            ReceiptBillBreakdown.Totals bill = ReceiptBillBreakdown.Build(
                new[] { new ReceiptBillBreakdown.Line(1000m, 1, 0m, 0m, 3m, false) },
                20m);

            Assert.Equal(30m, bill.RewardRupees);
            Assert.Equal(20m, bill.CouponRupees);
            Assert.Equal(950m, bill.GrandTotal);
            Assert.Equal(
                "₹1000.00 (net total)   →   reward taken (₹30.00)   →   coupon discount (₹20.00)   →   ₹950.00",
                bill.Text);
            Assert.Equal(bill.GrandTotal, bill.NetTotal - bill.RewardRupees - bill.CouponRupees);
        }

        [Fact]
        public void ManualAndReward_ThenCoupon_PartsAddToGrandTotal()
        {
            ReceiptBillBreakdown.Totals bill = ReceiptBillBreakdown.Build(
                new[] { new ReceiptBillBreakdown.Line(200m, 1, 10m, 25m, 10m, true) },
                20m);

            Assert.Equal(0m, bill.DiscountRupees);
            Assert.Equal(50m, bill.ManualRupees);
            Assert.Equal(20m, bill.RewardRupees);
            Assert.Equal(20m, bill.CouponRupees);
            Assert.Equal(110m, bill.GrandTotal);
            Assert.Equal(
                bill.GrandTotal,
                bill.NetTotal - bill.ManualRupees - bill.RewardRupees - bill.CouponRupees);
        }

        [Fact]
        public void AutoDiscount_IsSeparateFromManualOnAnotherLine()
        {
            ReceiptBillBreakdown.Totals bill = ReceiptBillBreakdown.Build(
                new[]
                {
                    new ReceiptBillBreakdown.Line(200m, 1, 10m, 0m, 0m, false),
                    new ReceiptBillBreakdown.Line(500m, 2, 0m, 10m, 5m, true)
                },
                50m);

            Assert.Equal(20m, bill.DiscountRupees);
            Assert.Equal(100m, bill.ManualRupees);
            Assert.Equal(50m, bill.RewardRupees);
            Assert.Equal(980m, bill.GrandTotal);
            Assert.Equal(
                bill.GrandTotal,
                bill.NetTotal - bill.DiscountRupees - bill.ManualRupees - bill.RewardRupees - bill.CouponRupees);
        }

        [Fact]
        public void GstLine_MatchesSavedPayable()
        {
            ReceiptBillBreakdown.Totals bill = ReceiptBillBreakdown.Build(
                new[] { new ReceiptBillBreakdown.Line(1180m, 2, 0m, 10m, 5m, true) },
                50m);

            Assert.Equal(2360m, bill.NetTotal);
            Assert.Equal(236m, bill.ManualRupees);
            Assert.Equal(118m, bill.RewardRupees);
            Assert.Equal(1956m, bill.GrandTotal);
        }

        [Fact]
        public void EmptyBill_ShowsNothing()
        {
            ReceiptBillBreakdown.Totals bill = ReceiptBillBreakdown.Build(
                System.Array.Empty<ReceiptBillBreakdown.Line>(),
                20m);

            Assert.Equal("", bill.Text);
            Assert.Equal(0m, bill.GrandTotal);
        }
    }
}
