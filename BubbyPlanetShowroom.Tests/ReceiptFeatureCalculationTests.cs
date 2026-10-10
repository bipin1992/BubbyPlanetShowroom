using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    /// <summary>
    /// Receipt screen rules: reward percent is added before the coupon rupees,
    /// a manual-sale line does not take reward, hold restores the same payable,
    /// and the cash to return is paid minus that payable.
    /// </summary>
    public class ReceiptFeatureCalculationTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 7);

        private sealed class Line
        {
            public decimal Price;
            public int Qty;
            public decimal Gst;
            public decimal Auto;
            public decimal Manual;
            public decimal Reward;
            public bool ManualRow;
            public decimal Cost;
        }

        private sealed class Settled
        {
            public decimal ItemsBeforeCoupon;
            public decimal Payable;
            public decimal Taxable;
            public decimal Gst;
            public decimal Profit;
            public decimal[] Nets = Array.Empty<decimal>();
            public string CouponCode = "";
            public decimal CouponDiscount;
        }

        private static decimal LinePercent(Line line)
        {
            return CouponSaleMath.LinePercent(line.Auto, line.Manual, line.Reward, line.ManualRow);
        }

        private static Settled Checkout(Line[] lines, string? couponCode, decimal couponRupees, bool cashBill = true)
        {
            var nets = new decimal[lines.Length];
            var taxables = new decimal[lines.Length];
            var gsts = new decimal[lines.Length];
            var discounts = new decimal[lines.Length];
            decimal items = 0m;
            decimal cost = 0m;

            for (int i = 0; i < lines.Length; i++)
            {
                ReturnCalculations.CalculateLineAmounts(
                    lines[i].Price,
                    lines[i].Gst,
                    LinePercent(lines[i]),
                    lines[i].Qty,
                    out taxables[i],
                    out gsts[i],
                    out decimal gross,
                    out nets[i]);
                discounts[i] = Math.Round(gross - (taxables[i] + gsts[i]), 2, MidpointRounding.AwayFromZero);
                items += nets[i];
                cost += Math.Round(Math.Max(0m, lines[i].Cost) * lines[i].Qty, 2, MidpointRounding.AwayFromZero);
            }

            items = Math.Round(items, 2, MidpointRounding.AwayFromZero);
            string code = CouponCalculations.NormalizeCode(couponCode);
            CouponCheckResult check = CouponCalculations.Evaluate(
                code, !string.IsNullOrWhiteSpace(code), Today,
                new DateTime(2026, 10, 1), new DateTime(2026, 10, 31),
                true, 0m, couponRupees, items);
            decimal applied = check.Applied ? check.AppliedDiscount : 0m;
            decimal payable = CouponCalculations.PayableAfterCoupon(items, applied);
            decimal[] shares = CouponCalculations.AllocateEvenByItem(applied, QtyOf(lines), nets);

            decimal savedTax = 0m;
            decimal savedGst = 0m;
            decimal savedNet = 0m;
            var savedNets = new decimal[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                CouponCalculations.CouponAdjustedLine adjusted = CouponCalculations.AddCouponToLine(
                    discounts[i], taxables[i], gsts[i], nets[i], i < shares.Length ? shares[i] : 0m);
                savedNets[i] = adjusted.NetAmount;
                savedTax += adjusted.TaxableAmount;
                savedGst += adjusted.GstAmount;
                savedNet += adjusted.NetAmount;
            }

            savedTax = Math.Round(savedTax, 2, MidpointRounding.AwayFromZero);
            savedGst = Math.Round(savedGst, 2, MidpointRounding.AwayFromZero);
            savedNet = Math.Round(savedNet, 2, MidpointRounding.AwayFromZero);
            decimal reported = CouponCalculations.ReportedBilledSale(savedNet, applied, applied > 0m);

            Assert.Equal(payable, savedNet);
            Assert.Equal(payable, savedTax + savedGst);
            Assert.Equal(payable, reported);
            Assert.Equal(payable, SellingCalculations.BilledSaleTotal(reported));
            Assert.Equal(cashBill ? payable : 0m, cashBill ? ClosingCashCalculations.CashBillContribution(reported) : 0m);

            return new Settled
            {
                ItemsBeforeCoupon = items,
                Payable = payable,
                Taxable = savedTax,
                Gst = savedGst,
                Profit = CouponCalculations.Profit(reported, Math.Round(cost, 2, MidpointRounding.AwayFromZero)),
                Nets = savedNets,
                CouponCode = check.Applied ? check.Code : "",
                CouponDiscount = applied
            };
        }

        private static int[] QtyOf(Line[] lines)
        {
            var qty = new int[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                qty[i] = lines[i].Qty;
            return qty;
        }

        private static decimal ChangeDue(decimal paid, decimal payable)
        {
            return Math.Round(paid - payable, 2, MidpointRounding.AwayFromZero);
        }

        [Fact]
        public void NoDiscount_Auto_Manual_AndReward_ThenCoupon_StayOnOnePayable()
        {
            var plain = new Line { Price = 200m, Qty = 1, Cost = 80m };
            Settled none = Checkout(new[] { plain }, "", 0m);
            Assert.Equal(200m, none.Payable);
            Assert.Equal(120m, none.Profit);

            plain.Auto = 10m;
            Settled autoOnly = Checkout(new[] { plain }, "", 0m);
            Assert.Equal(180m, autoOnly.Payable);

            plain.Manual = 25m;
            plain.ManualRow = true;
            Settled manualReplacesAuto = Checkout(new[] { plain }, "", 0m);
            Assert.Equal(150m, manualReplacesAuto.Payable);

            plain.Reward = 10m;
            Settled manualAndReward = Checkout(new[] { plain }, "SAVE20", 20m);
            Assert.Equal(130m, manualAndReward.ItemsBeforeCoupon);
            Assert.Equal(110m, manualAndReward.Payable);
            Assert.Equal(110m, manualAndReward.Taxable + manualAndReward.Gst);
            Assert.Equal(30m, manualAndReward.Profit);
            Assert.Equal(110m, ClosingCashCalculations.CashBillContribution(manualAndReward.Payable));
            Assert.Equal(-10m, ChangeDue(100m, manualAndReward.Payable));

            plain.Reward = 0m;
            Settled rewardOff = Checkout(new[] { plain }, "SAVE20", 20m);
            Assert.Equal(130m, rewardOff.Payable);

            Assert.Equal(0m, CouponSaleMath.ManualBaseFromDiscountEdit(0m, 25m, 10m));
            Assert.Equal(25m, CouponSaleMath.ManualBaseFromDiscountEdit(35m, 25m, 10m));
            Assert.Equal(20m, CouponSaleMath.ManualBaseFromDiscountEdit(20m, 25m, 10m));
        }

        [Fact]
        public void GstManualAndReward_TaxEqualsPayable_ReturnUsesSavedNet()
        {
            Settled bill = Checkout(
                new[]
                {
                    new Line { Price = 1180m, Qty = 2, Gst = 18m, Manual = 10m, ManualRow = true, Reward = 5m, Cost = 400m }
                },
                "GST50",
                50m);

            decimal percent = CouponSaleMath.LinePercent(0m, 10m, 5m, true);
            Assert.Equal(15m, percent);
            Assert.Equal(bill.Payable, bill.Taxable + bill.Gst);
            Assert.Equal(2006m, bill.ItemsBeforeCoupon);
            Assert.Equal(1956m, bill.Payable);
            Assert.Equal(978m, ReturnCalculations.CalculateLineRefund(2, 0, bill.Nets[0], 1));
            Assert.Equal(1956m, SellingCalculations.BilledSaleTotal(bill.Payable));
            Assert.Equal(0m, ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", 978m, Today, Today, "Cash"));
        }

        [Fact]
        public void RewardThenCoupon_CutsPercentBeforeRupees()
        {
            Settled bill = Checkout(
                new[] { new Line { Price = 1000m, Qty = 1, Reward = 10m, Cost = 400m } },
                "DIWALI",
                100m);

            Assert.Equal(900m, bill.ItemsBeforeCoupon);
            Assert.Equal(800m, bill.Payable);
            Assert.Equal(400m, bill.Profit);
            Assert.Equal(800m, bill.Nets[0]);
            Assert.Equal(200m, ChangeDue(1000m, bill.Payable));
        }

        [Fact]
        public void RewardRemoved_CouponStaysAndPayableGoesBackUp()
        {
            var item = new Line { Price = 1000m, Qty = 1, Reward = 10m, Cost = 400m };
            Settled withReward = Checkout(new[] { item }, "DIWALI", 100m);

            item.Reward = 0m;
            Settled rewardOff = Checkout(new[] { item }, CouponSaleMath.RememberHoldCoupon(" diwali "), 100m);

            Assert.Equal(800m, withReward.Payable);
            Assert.Equal("DIWALI", rewardOff.CouponCode);
            Assert.Equal(100m, rewardOff.CouponDiscount);
            Assert.Equal(900m, rewardOff.Payable);
            Assert.Equal(500m, rewardOff.Profit);
        }

        [Fact]
        public void ManualDiscount_StillTakesReward_CouponSplitsBothLines()
        {
            Settled bill = Checkout(
                new[]
                {
                    new Line { Price = 1000m, Qty = 1, Manual = 20m, ManualRow = true, Reward = 10m, Cost = 400m },
                    new Line { Price = 1000m, Qty = 1, Reward = 10m, Cost = 400m }
                },
                "SAVE100",
                100m);

            Assert.Equal(1600m, bill.ItemsBeforeCoupon);
            Assert.Equal(1500m, bill.Payable);
            Assert.Equal(650m, bill.Nets[0]);
            Assert.Equal(850m, bill.Nets[1]);
            Assert.Equal(700m, bill.Profit);
            Assert.Equal(850m, ReturnCalculations.CalculateLineRefund(1, 0, bill.Nets[1], 1));
        }

        [Fact]
        public void HoldAndResume_KeepsRewardCouponPayableAndChange()
        {
            var lines = new[]
            {
                new Line { Price = 500m, Qty = 2, Auto = 0m, Reward = 10m, Cost = 150m },
                new Line { Price = 200m, Qty = 1, Manual = 10m, ManualRow = true, Cost = 80m }
            };

            Settled held = Checkout(lines, "  save100 ", 100m);
            string resumedCode = CouponSaleMath.RememberHoldCoupon("  save100 ");
            Settled resumed = Checkout(lines, resumedCode, 100m);

            Assert.Equal("SAVE100", held.CouponCode);
            Assert.Equal(held.Payable, resumed.Payable);
            Assert.Equal(held.Taxable, resumed.Taxable);
            Assert.Equal(held.Gst, resumed.Gst);
            Assert.Equal(held.Profit, resumed.Profit);
            Assert.Equal(held.Nets, resumed.Nets);
            Assert.Equal(80m, ChangeDue(held.Payable + 80m, resumed.Payable));
        }

        [Fact]
        public void ShortPayment_IsNegativeChangeOnPayableNotOnPreCouponTotal()
        {
            Settled bill = Checkout(
                new[] { new Line { Price = 500m, Qty = 2, Cost = 100m } },
                "SAVE100",
                100m);

            Assert.Equal(900m, bill.Payable);
            Assert.Equal(-100m, ChangeDue(800m, bill.Payable));
            Assert.NotEqual(ChangeDue(800m, bill.ItemsBeforeCoupon), ChangeDue(800m, bill.Payable));
        }

        [Fact]
        public void GstRewardAndCoupon_TaxablePlusGstEqualsPayable()
        {
            Settled bill = Checkout(
                new[] { new Line { Price = 1180m, Qty = 1, Gst = 18m, Reward = 10m, Cost = 400m } },
                "GST100",
                100m);

            Assert.Equal(1062m, bill.ItemsBeforeCoupon);
            Assert.Equal(962m, bill.Payable);
            Assert.Equal(bill.Payable, bill.Taxable + bill.Gst);
            Assert.Equal(562m, bill.Profit);
        }

        [Fact]
        public void AutoPlusReward_StopsAtFree_CouponCannotGoBelowZero()
        {
            Settled bill = Checkout(
                new[] { new Line { Price = 200m, Qty = 1, Auto = 60m, Reward = 50m, Cost = 50m } },
                "FREE",
                100m);

            Assert.Equal(0m, bill.ItemsBeforeCoupon);
            Assert.Equal(0m, bill.Payable);
            Assert.Equal(0m, bill.CouponDiscount);
            Assert.Equal(-50m, bill.Profit);
            Assert.Equal(0m, ChangeDue(0m, bill.Payable));
        }

        [Fact]
        public void OnlineRewardCoupon_StaysOnSaleTabs_NotInTheDrawer()
        {
            Settled bill = Checkout(
                new[] { new Line { Price = 1000m, Qty = 1, Reward = 10m, Cost = 400m } },
                "DIWALI",
                100m,
                cashBill: false);

            Assert.Equal(800m, bill.Payable);
            Assert.Equal(800m, SellingCalculations.CombineTotalSale(bill.Payable, 0m));
            Assert.Equal(0m, ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Online", bill.Payable, Today, Today, "Online"));
        }

        [Fact]
        public void PartialReturn_AfterRewardAndCoupon_UsesSavedNet()
        {
            Settled bill = Checkout(
                new[] { new Line { Price = 500m, Qty = 2, Reward = 10m, Cost = 150m } },
                "SAVE100",
                100m);

            Assert.Equal(800m, bill.Payable);
            Assert.Equal(400m, ReturnCalculations.CalculateLineRefund(2, 0, bill.Nets[0], 1));

            decimal sameDay = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", 400m, Today, Today, "Cash");
            Assert.Equal(0m, sameDay);
            Assert.Equal(400m, ClosingCashCalculations.CashBillContribution(400m));
        }
    }
}
