using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class CouponCalculationsTests
    {
        [Fact]
        public void NormalizeCode_TrimsAndUppercases()
        {
            Assert.Equal("BUBBY10", CouponCalculations.NormalizeCode("  bubby10 "));
        }

        [Fact]
        public void IsOneCouponCode_RejectsASecondCode()
        {
            Assert.True(CouponCalculations.IsOneCouponCode("P10"));
            Assert.True(CouponCalculations.IsOneCouponCode(""));
            Assert.False(CouponCalculations.IsOneCouponCode("P10 P20"));
            Assert.False(CouponCalculations.IsOneCouponCode("P10,C30"));
            Assert.False(CouponCalculations.IsOneCouponCode("P10+P20"));
        }

        [Fact]
        public void IsDateRangeValid_RejectsEndBeforeStart()
        {
            Assert.False(CouponCalculations.IsDateRangeValid(
                new DateTime(2026, 9, 20),
                new DateTime(2026, 9, 10)));
            Assert.True(CouponCalculations.IsDateRangeValid(
                new DateTime(2026, 9, 10),
                new DateTime(2026, 9, 20)));
        }

        [Fact]
        public void IsWithinValidity_IsInclusiveAndRejectsOutsideDays()
        {
            DateTime from = new DateTime(2026, 9, 18);
            DateTime to = new DateTime(2026, 10, 18);
            Assert.True(CouponCalculations.IsWithinValidity(new DateTime(2026, 9, 18), from, to));
            Assert.True(CouponCalculations.IsWithinValidity(new DateTime(2026, 9, 19), from, to));
            Assert.True(CouponCalculations.IsWithinValidity(new DateTime(2026, 10, 18), from, to));
            Assert.False(CouponCalculations.IsWithinValidity(new DateTime(2026, 9, 17), from, to));
            Assert.False(CouponCalculations.IsWithinValidity(new DateTime(2026, 10, 19), from, to));
        }

        [Fact]
        public void Evaluate_ExpiredCoupon_DoesNotApplyEvenIfMinMet()
        {
            CouponCheckResult result = CouponCalculations.Evaluate(
                "100",
                true,
                new DateTime(2026, 10, 19),
                new DateTime(2026, 9, 18),
                new DateTime(2026, 10, 18),
                true,
                1000m,
                100m,
                2000m);
            Assert.False(result.Applied);
            Assert.Equal("Expired", result.Status);
            Assert.Equal(0m, result.AppliedDiscount);
            Assert.Contains("expired", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ToDate_ReadsIsoDateStringsFromMysql()
        {
            Assert.Equal(new DateTime(2026, 9, 18), CouponCalculations.ToDate("2026-09-18"));
            Assert.Equal(new DateTime(2026, 10, 18), CouponCalculations.ToDate("2026-10-18 00:00:00"));
        }

        [Theory]
        [InlineData("2026-09-16", "2026-09-01", "2026-09-30", true, "Valid")]
        [InlineData("2026-08-31", "2026-09-01", "2026-09-30", true, "Upcoming")]
        [InlineData("2026-10-01", "2026-09-01", "2026-09-30", true, "Expired")]
        [InlineData("2026-09-16", "2026-09-01", "2026-09-30", false, "Inactive")]
        public void StatusOn_MatchesValidityWindow(
            string today,
            string from,
            string to,
            bool active,
            string expected)
        {
            Assert.Equal(
                expected,
                CouponCalculations.StatusOn(
                    DateTime.Parse(today),
                    DateTime.Parse(from),
                    DateTime.Parse(to),
                    active));
        }
        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(500, 0, true)]
        [InlineData(999, 999, true)]
        [InlineData(1000, 999, true)]
        [InlineData(998.99, 999, false)]
        [InlineData(0, 500, false)]
        public void MeetsMinimumPurchase_RequiresCartToReachMinimum(
            double purchaseAmount,
            double minPurchaseAmount,
            bool expected)
        {
            Assert.Equal(
                expected,
                CouponCalculations.MeetsMinimumPurchase(
                    (decimal)purchaseAmount,
                    (decimal)minPurchaseAmount));
        }

        [Theory]
        [InlineData(200, 2000, 200)]
        [InlineData(2500, 2000, 2000)]
        [InlineData(0, 2000, 0)]
        [InlineData(200, 0, 0)]
        public void ClampRupeeDiscount_NeverExceedsPayable(
            double discountAmount,
            double payableAmount,
            double expected)
        {
            Assert.Equal(
                (decimal)expected,
                CouponCalculations.ClampRupeeDiscount(
                    (decimal)discountAmount,
                    (decimal)payableAmount));
        }

        [Fact]
        public void ExtraPercentFromRupeeDiscount_ConvertsRupeesToPercentOfGross()
        {
            Assert.Equal(10m, CouponCalculations.ExtraPercentFromRupeeDiscount(200m, 2000m));
            Assert.Equal(0m, CouponCalculations.ExtraPercentFromRupeeDiscount(200m, 0m));
            Assert.Equal(0m, CouponCalculations.ExtraPercentFromRupeeDiscount(0m, 2000m));
        }

        [Fact]
        public void Evaluate_EmptyCode_DoesNotApply()
        {
            CouponCheckResult result = CouponCalculations.Evaluate(
                "  ", true, DateTime.Today, DateTime.Today, DateTime.Today, true, 0, 100, 500);
            Assert.False(result.Found);
            Assert.False(result.Applied);
            Assert.Equal("", result.Message);
        }

        [Fact]
        public void Evaluate_UnknownCode_FailsBeforeDateCheck()
        {
            CouponCheckResult result = CouponCalculations.Evaluate(
                "NOPE", false, DateTime.Today, DateTime.Today, DateTime.Today, true, 999, 100, 2000);
            Assert.False(result.Found);
            Assert.False(result.Applied);
            Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("2026-10-01", "2026-09-01", "2026-09-30", true, "expired")]
        [InlineData("2026-08-31", "2026-09-01", "2026-09-30", true, "not valid yet")]
        [InlineData("2026-09-16", "2026-09-01", "2026-09-30", false, "inactive")]
        public void Evaluate_RejectsDateOrActiveBeforeMinAmount(
            string today,
            string from,
            string to,
            bool active,
            string expectedMessagePart)
        {
            CouponCheckResult result = CouponCalculations.Evaluate(
                "BUBBY100",
                true,
                DateTime.Parse(today),
                DateTime.Parse(from),
                DateTime.Parse(to),
                active,
                999m,
                100m,
                50m);
            Assert.True(result.Found);
            Assert.False(result.Applied);
            Assert.Contains(expectedMessagePart, result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Evaluate_ValidCouponBelowMin_DoesNotApply()
        {
            CouponCheckResult result = CouponCalculations.Evaluate(
                "BUBBY100",
                true,
                new DateTime(2026, 9, 16),
                new DateTime(2026, 9, 1),
                new DateTime(2026, 9, 30),
                true,
                999m,
                100m,
                500m);
            Assert.True(result.Found);
            Assert.False(result.Applied);
            Assert.Contains("minimum purchase", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0m, result.AppliedDiscount);
        }

        [Fact]
        public void Evaluate_ValidCouponMeetingMin_SubtractsFromGrandTotal()
        {
            CouponCheckResult result = CouponCalculations.Evaluate(
                "bubby100",
                true,
                new DateTime(2026, 9, 16),
                new DateTime(2026, 9, 1),
                new DateTime(2026, 9, 30),
                true,
                999m,
                100m,
                1500m);
            Assert.True(result.Applied);
            Assert.Equal("BUBBY100", result.Code);
            Assert.Equal(100m, result.AppliedDiscount);
            Assert.Equal(1400m, CouponCalculations.PayableAfterCoupon(1500m, result.AppliedDiscount));
        }

        [Fact]
        public void Evaluate_DiscountCannotExceedGrandTotal()
        {
            CouponCheckResult result = CouponCalculations.Evaluate(
                "BUBBY100",
                true,
                new DateTime(2026, 9, 16),
                new DateTime(2026, 9, 1),
                new DateTime(2026, 9, 30),
                true,
                0m,
                500m,
                200m);
            Assert.True(result.Applied);
            Assert.Equal(200m, result.AppliedDiscount);
            Assert.Equal(0m, CouponCalculations.PayableAfterCoupon(200m, result.AppliedDiscount));
        }

        [Fact]
        public void BilledSale_SubtractsCouponFromItemNets()
        {
            Assert.Equal(1900.00m, CouponCalculations.BilledSale(2000m, 100m));
            Assert.Equal(9.50m, CouponCalculations.BilledSale(49.50m, 40m));
            Assert.Equal(0m, CouponCalculations.BilledSale(40m, 40m));
        }

        [Fact]
        public void AllocateEvenByItem_SplitsCouponAcrossItemsThenQuantity()
        {
            decimal[] shares = CouponCalculations.AllocateEvenByItem(
                100m,
                new[] { 2, 1 },
                new[] { 800m, 500m });

            Assert.Equal(50m, shares[0]);
            Assert.Equal(50m, shares[1]);
            Assert.Equal(25m, CouponCalculations.PerUnitShare(shares[0], 2));
            Assert.Equal(50m, CouponCalculations.PerUnitShare(shares[1], 1));
        }

        [Fact]
        public void AllocateEvenByItem_PutsRemainderOnLastItemAndCapsAtNet()
        {
            decimal[] uneven = CouponCalculations.AllocateEvenByItem(
                100m,
                new[] { 1, 1, 1 },
                new[] { 500m, 500m, 500m });
            Assert.Equal(33.33m, uneven[0]);
            Assert.Equal(33.33m, uneven[1]);
            Assert.Equal(33.34m, uneven[2]);
            Assert.Equal(100m, uneven[0] + uneven[1] + uneven[2]);

            decimal[] capped = CouponCalculations.AllocateEvenByItem(
                100m,
                new[] { 1, 1 },
                new[] { 40m, 500m });
            Assert.Equal(40m, capped[0]);
            Assert.Equal(60m, capped[1]);
        }

        [Fact]
        public void AddCouponToLine_AddsRupeesIntoDiscountAndReducesNet()
        {
            CouponCalculations.CouponAdjustedLine line = CouponCalculations.AddCouponToLine(
                0m, 1000m, 0m, 1000m, 50m);
            Assert.Equal(50m, line.DiscountAmount);
            Assert.Equal(50m, line.CouponShare);
            Assert.Equal(950m, line.NetAmount);
        }

        [Fact]
        public void AllocateLineSale_SplitsCouponAcrossMultipleItems()
        {
            decimal hoodie = CouponCalculations.AllocateLineSale(1200m, 2000m, 100m);
            decimal ball = CouponCalculations.AllocateLineSale(800m, 2000m, 100m);
            Assert.Equal(1140.00m, hoodie);
            Assert.Equal(760.00m, ball);
            Assert.Equal(1900.00m, hoodie + ball);
        }

        [Fact]
        public void ToAmount_ReadsDecimalAndTextValues()
        {
            Assert.Equal(0m, CouponCalculations.ToAmount(null));
            Assert.Equal(999.50m, CouponCalculations.ToAmount(999.50m));
            Assert.Equal(500m, CouponCalculations.ToAmount("500"));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData((byte)1, true)]
        [InlineData((byte)0, false)]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData("Yes", true)]
        [InlineData("1", true)]
        public void ToBool_AcceptsMysqlTinyIntShapes(object value, bool expected)
        {
            Assert.Equal(expected, CouponCalculations.ToBool(value));
        }
    }
}
