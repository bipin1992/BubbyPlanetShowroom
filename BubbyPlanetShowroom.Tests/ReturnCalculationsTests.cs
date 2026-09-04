using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class ReturnCalculationsTests
    {
        // ---------- Round2 ----------

        [Theory]
        [InlineData(10.004, 10.00)]
        [InlineData(10.005, 10.01)]
        [InlineData(10.014, 10.01)]
        [InlineData(10.015, 10.02)]
        public void Round2_UsesAwayFromZero(decimal input, decimal expected)
        {
            Assert.Equal(expected, ReturnCalculations.Round2(input));
        }

        // ---------- CalculateLineRefund (UI preview) ----------

        [Fact]
        public void CalculateLineRefund_PartialOfFive_ReturnsOneFifth()
        {
            // qty 5, nothing returned yet, net 500 → return 1 = 100
            decimal refund = ReturnCalculations.CalculateLineRefund(5, 0, 500m, 1);
            Assert.Equal(100.00m, refund);
        }

        [Fact]
        public void CalculateLineRefund_AfterOneAlreadyReturned_UsesRemainingNet()
        {
            // After 1 of 5 returned, remaining net on line is 400 for 4 units
            decimal refund = ReturnCalculations.CalculateLineRefund(5, 1, 400m, 1);
            Assert.Equal(100.00m, refund);
        }

        [Fact]
        public void CalculateLineRefund_ReturnAllRemaining_EqualsFullNet()
        {
            decimal refund = ReturnCalculations.CalculateLineRefund(5, 1, 400m, 4);
            Assert.Equal(400.00m, refund);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CalculateLineRefund_NonPositiveReturnQty_IsZero(int returnQty)
        {
            Assert.Equal(0m, ReturnCalculations.CalculateLineRefund(5, 0, 500m, returnQty));
        }

        [Fact]
        public void CalculateLineRefund_WhenFullyReturned_IsZero()
        {
            Assert.Equal(0m, ReturnCalculations.CalculateLineRefund(5, 5, 0m, 1));
        }

        [Fact]
        public void CalculateLineRefund_ExceedsRemaining_IsZero()
        {
            Assert.Equal(0m, ReturnCalculations.CalculateLineRefund(5, 0, 500m, 6));
        }

        [Fact]
        public void CalculateLineRefund_UnevenDivision_RoundsToTwoDecimals()
        {
            // 100 / 3 * 1 = 33.333... → 33.33
            decimal refund = ReturnCalculations.CalculateLineRefund(3, 0, 100m, 1);
            Assert.Equal(33.33m, refund);
        }

        // ---------- CanReturn / RemainingQty ----------

        [Theory]
        [InlineData(5, 0, 1, true)]
        [InlineData(5, 1, 4, true)]
        [InlineData(5, 1, 5, false)]
        [InlineData(5, 5, 1, false)]
        [InlineData(5, 0, 0, false)]
        public void CanReturn_ValidatesAgainstRemaining(int qty, int returned, int returnNow, bool expected)
        {
            Assert.Equal(expected, ReturnCalculations.CanReturn(qty, returned, returnNow));
        }

        [Theory]
        [InlineData(5, 0, 5)]
        [InlineData(5, 1, 4)]
        [InlineData(5, 5, 0)]
        [InlineData(5, 6, 0)]
        public void RemainingQty_NeverNegative(int qty, int returned, int expected)
        {
            Assert.Equal(expected, ReturnCalculations.RemainingQty(qty, returned));
        }

        // ---------- ApplyReturn: single partial ----------

        [Fact]
        public void ApplyReturn_OneOfFive_RefundAndRemainingSplitCorrectly()
        {
            var result = ReturnCalculations.ApplyReturn(
                qty: 5,
                returnedAlready: 0,
                returnNow: 1,
                grossAmount: 500m,
                discountAmount: 50m,
                taxableAmount: 450m,
                gstAmount: 81m,
                netAmount: 531m);

            Assert.Equal(1, result.NewReturnQty);
            Assert.Equal(4, result.NewRemainingQty);
            Assert.Equal(106.20m, result.Refund);          // 531/5
            Assert.Equal(424.80m, result.NewNetAmount);    // 531 - 106.20
            Assert.Equal(400.00m, result.NewGrossAmount);  // 500/5*4
            Assert.Equal(40.00m, result.NewDiscountAmount);
            Assert.Equal(360.00m, result.NewTaxableAmount);
            Assert.Equal(64.80m, result.NewGstAmount);     // residual of net - taxable
            Assert.Equal(531m, result.Refund + result.NewNetAmount);
            Assert.Equal(result.NewNetAmount, result.NewTaxableAmount + result.NewGstAmount);
        }

        [Fact]
        public void ApplyReturn_FullReturn_LeavesZeroAmounts()
        {
            var result = ReturnCalculations.ApplyReturn(
                qty: 5,
                returnedAlready: 0,
                returnNow: 5,
                grossAmount: 500m,
                discountAmount: 50m,
                taxableAmount: 450m,
                gstAmount: 81m,
                netAmount: 531m);

            Assert.Equal(5, result.NewReturnQty);
            Assert.Equal(0, result.NewRemainingQty);
            Assert.Equal(531.00m, result.Refund);
            Assert.Equal(0.00m, result.NewNetAmount);
            Assert.Equal(0.00m, result.NewGrossAmount);
            Assert.Equal(0.00m, result.NewDiscountAmount);
            Assert.Equal(0.00m, result.NewTaxableAmount);
            Assert.Equal(0.00m, result.NewGstAmount);
        }

        [Fact]
        public void ApplyReturn_ExceedsQty_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ReturnCalculations.ApplyReturn(5, 0, 6, 500m, 0m, 500m, 0m, 500m));
        }

        // ---------- Successive partial returns (the real UI scenario) ----------

        [Fact]
        public void SuccessiveReturns_FiveUnits_OneThenOneThenThree_PreserveOriginalNet()
        {
            decimal originalNet = 500m;
            decimal originalGross = 500m;
            decimal originalDiscount = 0m;
            decimal originalTaxable = 500m;
            decimal originalGst = 0m;

            // Return 1 of 5
            var r1 = ReturnCalculations.ApplyReturn(5, 0, 1, originalGross, originalDiscount, originalTaxable, originalGst, originalNet);
            Assert.Equal(100.00m, r1.Refund);
            Assert.Equal(400.00m, r1.NewNetAmount);

            // Search again / return another 1 of remaining 4 (line now holds remaining amounts)
            var r2 = ReturnCalculations.ApplyReturn(5, r1.NewReturnQty, 1, r1.NewGrossAmount, r1.NewDiscountAmount, r1.NewTaxableAmount, r1.NewGstAmount, r1.NewNetAmount);
            Assert.Equal(100.00m, r2.Refund);
            Assert.Equal(300.00m, r2.NewNetAmount);

            // Return last 3
            var r3 = ReturnCalculations.ApplyReturn(5, r2.NewReturnQty, 3, r2.NewGrossAmount, r2.NewDiscountAmount, r2.NewTaxableAmount, r2.NewGstAmount, r2.NewNetAmount);
            Assert.Equal(300.00m, r3.Refund);
            Assert.Equal(0.00m, r3.NewNetAmount);
            Assert.Equal(5, r3.NewReturnQty);

            decimal totalRefunded = r1.Refund + r2.Refund + r3.Refund;
            Assert.Equal(originalNet, totalRefunded);
        }

        [Fact]
        public void SuccessiveReturns_WithGst_TotalRefundPlusRemainingEqualsOriginal()
        {
            decimal originalNet = 1180m; // e.g. 1000 + 18% GST
            decimal originalGross = 1000m;
            decimal originalDiscount = 0m;
            decimal originalTaxable = 1000m;
            decimal originalGst = 180m;
            int qty = 5;

            decimal refunded = 0m;
            int returned = 0;
            decimal gross = originalGross;
            decimal discount = originalDiscount;
            decimal taxable = originalTaxable;
            decimal gst = originalGst;
            decimal net = originalNet;

            // return 1, then 2, then 2
            int[] steps = { 1, 2, 2 };
            foreach (int step in steps)
            {
                var r = ReturnCalculations.ApplyReturn(qty, returned, step, gross, discount, taxable, gst, net);
                refunded += r.Refund;
                returned = r.NewReturnQty;
                gross = r.NewGrossAmount;
                discount = r.NewDiscountAmount;
                taxable = r.NewTaxableAmount;
                gst = r.NewGstAmount;
                net = r.NewNetAmount;
            }

            Assert.Equal(5, returned);
            Assert.Equal(0.00m, net);
            Assert.Equal(originalNet, refunded);
            Assert.Equal(0.00m, gst);
            Assert.Equal(0.00m, taxable);
            Assert.Equal(0.00m, gross);
        }

        [Fact]
        public void SuccessiveReturns_UnevenAmounts_StayWithinOnePaisaOfOriginal()
        {
            // Classic awkward split: 100 net across 3 units
            decimal originalNet = 100m;
            int qty = 3;

            var r1 = ReturnCalculations.ApplyReturn(qty, 0, 1, 100m, 0m, 100m, 0m, originalNet);
            var r2 = ReturnCalculations.ApplyReturn(qty, r1.NewReturnQty, 1, r1.NewGrossAmount, r1.NewDiscountAmount, r1.NewTaxableAmount, r1.NewGstAmount, r1.NewNetAmount);
            var r3 = ReturnCalculations.ApplyReturn(qty, r2.NewReturnQty, 1, r2.NewGrossAmount, r2.NewDiscountAmount, r2.NewTaxableAmount, r2.NewGstAmount, r2.NewNetAmount);

            decimal totalRefunded = r1.Refund + r2.Refund + r3.Refund;

            // Each step rounds; final remaining must be 0 and total refund must match original
            Assert.Equal(0.00m, r3.NewNetAmount);
            Assert.Equal(originalNet, totalRefunded);
        }

        [Fact]
        public void UiPreviewMatchesProcessRefund_AfterPartialReturn()
        {
            // First process return of 1/5
            var processed = ReturnCalculations.ApplyReturn(5, 0, 1, 500m, 0m, 500m, 0m, 500m);

            // UI preview when user enters Return Qty = 2 on reloaded row
            decimal preview = ReturnCalculations.CalculateLineRefund(
                qty: 5,
                returned: processed.NewReturnQty,
                netAmount: processed.NewNetAmount,
                returnQty: 2);

            var secondProcess = ReturnCalculations.ApplyReturn(
                5,
                processed.NewReturnQty,
                2,
                processed.NewGrossAmount,
                processed.NewDiscountAmount,
                processed.NewTaxableAmount,
                processed.NewGstAmount,
                processed.NewNetAmount);

            Assert.Equal(secondProcess.Refund, preview);
        }

        // ---------- Multi-line order totals ----------

        [Fact]
        public void MultiLineOrder_PartialReturns_OrderTotalsEqualSumOfLineNets()
        {
            // Line A: qty 5 net 500 → return 1
            var a = ReturnCalculations.ApplyReturn(5, 0, 1, 500m, 0m, 500m, 0m, 500m);
            // Line B: qty 2 net 200 → return 1
            var b = ReturnCalculations.ApplyReturn(2, 0, 1, 200m, 0m, 200m, 0m, 200m);

            decimal orderNetAfter = a.NewNetAmount + b.NewNetAmount;
            decimal totalRefund = a.Refund + b.Refund;

            Assert.Equal(400.00m, a.NewNetAmount);
            Assert.Equal(100.00m, b.NewNetAmount);
            Assert.Equal(500.00m, orderNetAfter);
            Assert.Equal(200.00m, totalRefund);
            Assert.Equal(700.00m, orderNetAfter + totalRefund); // original 500+200
        }

        [Fact]
        public void DiscountedLine_PartialReturn_PreservesDiscountProportion()
        {
            // Sold 4 @ gross 400, discount 40, taxable 360, gst 64.80, net 424.80
            var result = ReturnCalculations.ApplyReturn(
                qty: 4,
                returnedAlready: 0,
                returnNow: 1,
                grossAmount: 400m,
                discountAmount: 40m,
                taxableAmount: 360m,
                gstAmount: 64.80m,
                netAmount: 424.80m);

            Assert.Equal(106.20m, result.Refund);
            Assert.Equal(318.60m, result.NewNetAmount);
            Assert.Equal(30.00m, result.NewDiscountAmount);
            Assert.Equal(270.00m, result.NewTaxableAmount);
            Assert.Equal(48.60m, result.NewGstAmount);
            Assert.Equal(424.80m, result.Refund + result.NewNetAmount);
        }

        [Fact]
        public void ApplyReturn_DoesNotInflateNet_AcrossAnyStepOfFive()
        {
            decimal original = 999.99m;
            decimal gross = 999.99m;
            decimal discount = 0m;
            decimal taxable = 999.99m;
            decimal gst = 0m;
            decimal net = original;
            int returned = 0;
            decimal refunded = 0m;

            for (int i = 0; i < 5; i++)
            {
                var r = ReturnCalculations.ApplyReturn(5, returned, 1, gross, discount, taxable, gst, net);
                refunded += r.Refund;
                returned = r.NewReturnQty;
                gross = r.NewGrossAmount;
                discount = r.NewDiscountAmount;
                taxable = r.NewTaxableAmount;
                gst = r.NewGstAmount;
                net = r.NewNetAmount;

                // At every step: refunded so far + remaining net == original (within rounding of completed steps)
                Assert.True(refunded + net <= original + 0.05m,
                    $"Step {i + 1}: refunded+remaining ({refunded + net}) exceeded original {original}");
            }

            Assert.Equal(0.00m, net);
            Assert.Equal(original, refunded);
        }

        // ---------- Fresh bill → multiple return visits ----------

        [Fact]
        public void FreshBill_ThreeVisits_MultiLine_TotalRefundEqualsOriginalBill()
        {
            // Line0: 5 × 200 @12% GST
            ReturnCalculations.CalculateLineAmounts(200m, 12m, 0m, 5, out var t0, out var g0, out var gr0, out var n0);
            // Line1: 2 × 450 @18% GST, 10% disc
            ReturnCalculations.CalculateLineAmounts(450m, 18m, 10m, 2, out var t1, out var g1, out var gr1, out var n1);
            // Line2: 3 × 99.99 @5% GST
            ReturnCalculations.CalculateLineAmounts(99.99m, 5m, 0m, 3, out var t2, out var g2, out var gr2, out var n2);

            var lines = new[]
            {
                new LineState(5, gr0, ReturnCalculations.Round2(gr0 - n0), t0, g0, n0, 0),
                new LineState(2, gr1, ReturnCalculations.Round2(gr1 - n1), t1, g1, n1, 0),
                new LineState(3, gr2, ReturnCalculations.Round2(gr2 - n2), t2, g2, n2, 0),
            };

            decimal originalBill = n0 + n1 + n2;
            decimal totalRefund = 0m;

            // Visit 1: return 2 from L0, 1 from L1
            totalRefund += ApplyVisit(lines, new[] { (0, 2), (1, 1) });
            AssertBillInvariant(lines, originalBill, totalRefund);

            // Visit 2: return 1 from L0, 2 from L2
            totalRefund += ApplyVisit(lines, new[] { (0, 1), (2, 2) });
            AssertBillInvariant(lines, originalBill, totalRefund);

            // Visit 3: finish remaining
            totalRefund += ApplyVisit(lines, new[] { (0, 2), (1, 1), (2, 1) });
            AssertBillInvariant(lines, originalBill, totalRefund);

            Assert.Equal(0.00m, lines[0].Net + lines[1].Net + lines[2].Net);
            Assert.Equal(originalBill, totalRefund);
            Assert.Equal(5, lines[0].Returned);
            Assert.Equal(2, lines[1].Returned);
            Assert.Equal(3, lines[2].Returned);
        }

        [Fact]
        public void PartialReturns_TaxablePlusGstAlwaysEqualsNet()
        {
            ReturnCalculations.CalculateLineAmounts(500m, 12m, 10m, 4, out var taxable, out var gst, out var gross, out var net);
            decimal disc = ReturnCalculations.Round2(gross - net);
            int returned = 0;

            foreach (int step in new[] { 1, 1, 2 })
            {
                var r = ReturnCalculations.ApplyReturn(4, returned, step, gross, disc, taxable, gst, net);
                returned = r.NewReturnQty;
                gross = r.NewGrossAmount;
                disc = r.NewDiscountAmount;
                taxable = r.NewTaxableAmount;
                gst = r.NewGstAmount;
                net = r.NewNetAmount;

                Assert.Equal(net, ReturnCalculations.Round2(taxable + gst));
            }

            Assert.Equal(0.00m, net);
        }

        [Fact]
        public void AwkwardSevenUnits_EachStep_NoTaxNetMismatch_AndFullRefund()
        {
            ReturnCalculations.CalculateLineAmounts(999.99m, 5m, 7.5m, 7, out var taxable, out var gst, out var gross, out var net);
            decimal disc = ReturnCalculations.Round2(gross - net);
            decimal original = net;
            decimal refunded = 0m;
            int returned = 0;

            for (int i = 0; i < 7; i++)
            {
                decimal preview = ReturnCalculations.CalculateLineRefund(7, returned, net, 1);
                var r = ReturnCalculations.ApplyReturn(7, returned, 1, gross, disc, taxable, gst, net);
                Assert.Equal(preview, r.Refund);

                refunded += r.Refund;
                returned = r.NewReturnQty;
                gross = r.NewGrossAmount;
                disc = r.NewDiscountAmount;
                taxable = r.NewTaxableAmount;
                gst = r.NewGstAmount;
                net = r.NewNetAmount;

                Assert.Equal(net, ReturnCalculations.Round2(taxable + gst));
                Assert.Equal(original, ReturnCalculations.Round2(refunded + net));
            }

            Assert.Equal(0.00m, net);
            Assert.Equal(original, refunded);
        }

        [Theory]
        [InlineData(500, 500, 0, 0, true)]
        [InlineData(500, 600, 100, 0, true)]
        [InlineData(500, 499.99, 0, 0.01, false)]
        public void Exchange_EqualOrMoreRule(decimal ret, decimal neu, decimal balance, decimal shortfall, bool ok)
        {
            var ex = ReturnCalculations.CalculateExchange(ret, neu);
            Assert.Equal(balance, ex.BalanceDue);
            Assert.Equal(shortfall, ex.Shortfall);
            Assert.Equal(ok, ex.MeetsEqualOrMoreRule);
        }

        [Fact]
        public void T99Return_T399Exchange_15Percent_ExtraIs255()
        {
            ReturnCalculations.CalculateLineAmounts(99m, 0m, 15m, 1, out _, out _, out _, out decimal t99Net);
            ReturnCalculations.CalculateLineAmounts(399m, 0m, 15m, 1, out _, out _, out _, out decimal t399Net);

            Assert.Equal(84.15m, t99Net);
            Assert.Equal(339.15m, t399Net);

            var exchange = ReturnCalculations.CalculateExchange(t99Net, t399Net);
            Assert.Equal(255.00m, exchange.BalanceDue);
            Assert.True(exchange.MeetsEqualOrMoreRule);
        }

        [Fact]
        public void ExchangeThenReturnExchangeLine_PreservesValues()
        {
            // Original line partially returned
            var sold = ReturnCalculations.ApplyReturn(3, 0, 1, 300m, 0m, 300m, 0m, 300m);
            Assert.Equal(100.00m, sold.Refund);
            Assert.Equal(200.00m, sold.NewNetAmount);

            // Exchange adds new line net 150 (equal-or-more vs return 100)
            var ex = ReturnCalculations.CalculateExchange(sold.Refund, 150m);
            Assert.True(ex.MeetsEqualOrMoreRule);
            Assert.Equal(50.00m, ex.BalanceDue);

            // Later visit: return the exchange item fully
            var back = ReturnCalculations.ApplyReturn(1, 0, 1, 150m, 0m, 150m, 0m, 150m);
            Assert.Equal(150.00m, back.Refund);
            Assert.Equal(0.00m, back.NewNetAmount);
        }

        private sealed class LineState
        {
            public LineState(int qty, decimal gross, decimal disc, decimal taxable, decimal gst, decimal net, int returned)
            {
                Qty = qty;
                Gross = gross;
                Disc = disc;
                Taxable = taxable;
                Gst = gst;
                Net = net;
                Returned = returned;
            }

            public int Qty { get; }
            public decimal Gross { get; set; }
            public decimal Disc { get; set; }
            public decimal Taxable { get; set; }
            public decimal Gst { get; set; }
            public decimal Net { get; set; }
            public int Returned { get; set; }
        }

        private static decimal ApplyVisit(LineState[] lines, (int Index, int Qty)[] returns)
        {
            decimal visitRefund = 0m;
            foreach ((int index, int qty) in returns)
            {
                LineState line = lines[index];
                var r = ReturnCalculations.ApplyReturn(
                    line.Qty, line.Returned, qty,
                    line.Gross, line.Disc, line.Taxable, line.Gst, line.Net);

                visitRefund += r.Refund;
                line.Returned = r.NewReturnQty;
                line.Gross = r.NewGrossAmount;
                line.Disc = r.NewDiscountAmount;
                line.Taxable = r.NewTaxableAmount;
                line.Gst = r.NewGstAmount;
                line.Net = r.NewNetAmount;

                Assert.Equal(line.Net, ReturnCalculations.Round2(line.Taxable + line.Gst));
            }

            return visitRefund;
        }

        private static void AssertBillInvariant(LineState[] lines, decimal originalBill, decimal totalRefund)
        {
            decimal remaining = 0m;
            decimal orderSubtotal = 0m;
            decimal orderTax = 0m;
            decimal orderGrand = 0m;
            foreach (LineState line in lines)
            {
                remaining += line.Net;
                orderSubtotal += line.Taxable;
                orderTax += line.Gst;
                orderGrand += line.Net;
            }

            Assert.Equal(originalBill, ReturnCalculations.Round2(totalRefund + remaining));
            Assert.Equal(ReturnCalculations.Round2(orderGrand), ReturnCalculations.Round2(orderSubtotal + orderTax));
        }
    }
}
