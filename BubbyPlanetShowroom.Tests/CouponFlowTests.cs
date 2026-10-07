using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class CouponFlowTests
    {
        private static CouponSaleMath.LineIn Item(decimal price, int qty, decimal discountPercent = 0m, decimal gst = 0m, decimal cost = 0m)
        {
            return new CouponSaleMath.LineIn
            {
                Price = price,
                Qty = qty,
                DiscountPercent = discountPercent,
                GstPercent = gst,
                CostPerUnit = cost
            };
        }

        [Fact]
        public void NoCoupon_BillMatchesPreCouponTotals()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(200m, 2, cost: 80m), Item(150m, 1, cost: 40m) },
                couponCode: "",
                couponRupees: 0m);

            Assert.False(bill.CouponApplied);
            Assert.Empty(bill.CouponLines);
            Assert.Equal(550m, bill.Payable);
            Assert.Equal(550m, bill.SellingSale);
            Assert.Equal(550m, bill.RevenueSales);
            Assert.Equal(350m, bill.Profit);
            Assert.Equal(350m, bill.RevenueProfit);
            Assert.Equal(550m, bill.ClosingCash);
            Assert.Equal("", ReceiptCalculations.CouponBillLines(null, 0m, bill.Payable).Length == 0 ? "" : "shown");
        }

        [Fact]
        public void BlankOrZeroInputs_DoNotCreateCouponOrSale()
        {
            Assert.Equal(0m, CouponSaleMath.CombinedDiscountPercent(0m, 0m, 0m));
            Assert.Equal("", CouponSaleMath.RememberHoldCoupon("   "));
            Assert.Equal("", CouponSaleMath.RememberHoldCoupon(null));

            CouponSaleMath.BillOut empty = CouponSaleMath.Settle(null, "  ", 100m);
            Assert.False(empty.CouponApplied);
            Assert.Empty(empty.CouponLines);
            Assert.Equal(0m, empty.Payable);
            Assert.Equal(0m, empty.SellingSale);
            Assert.Equal(0m, empty.RevenueSales);
            Assert.Equal(0m, empty.ClosingCash);
            Assert.Equal(0m, empty.Profit);

            CouponSaleMath.BillOut zeros = CouponSaleMath.Settle(
                new[] { Item(0m, 0), Item(-5m, 2), Item(100m, 0) },
                "SAVE",
                0m);
            Assert.True(zeros.CouponApplied);
            Assert.Equal("SAVE", zeros.CouponCode);
            Assert.Equal(0m, zeros.CouponDiscount);
            Assert.Equal(0m, zeros.Payable);
            Assert.Contains("Coupon: SAVE", zeros.CouponLines);
        }

        [Fact]
        public void CouponCodeWithoutAmount_StillAppliesAndDoesNotChangeMoney()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(500m, 1, cost: 200m) },
                "DIWALI",
                0m);

            Assert.True(bill.CouponApplied);
            Assert.Equal("DIWALI", bill.CouponCode);
            Assert.Equal(0m, bill.CouponDiscount);
            Assert.Equal(500m, bill.Payable);
            Assert.Equal(500m, bill.SellingSale);
            Assert.Equal(300m, bill.Profit);
            Assert.Equal(500m, bill.ClosingCash);
            Assert.Equal(new[]
            {
                "Coupon: DIWALI",
                "Coupon Discount: -0.00",
                "PAYABLE: 500.00"
            }, bill.CouponLines);
        }

        [Fact]
        public void AmountWithoutCode_IsNotPrintedOrApplied()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(500m, 1) },
                "   ",
                100m);

            Assert.False(bill.CouponApplied);
            Assert.Empty(bill.CouponLines);
            Assert.Equal(500m, bill.Payable);
            Assert.Equal(500m, bill.RevenueSales);
        }

        [Fact]
        public void AppliedCoupon_PrintsCodeAndDiscountAndSplitsByItemThenQty()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(500m, 2, cost: 200m), Item(300m, 1, cost: 100m) },
                " save100 ",
                100m);

            Assert.True(bill.CouponApplied);
            Assert.Equal("SAVE100", bill.CouponCode);
            Assert.Equal(100m, bill.CouponDiscount);
            Assert.Equal(50m, bill.Lines[0].CouponShare);
            Assert.Equal(25m, bill.Lines[0].PerUnitCoupon);
            Assert.Equal(50m, bill.Lines[1].CouponShare);
            Assert.Equal(50m, bill.Lines[1].PerUnitCoupon);
            Assert.Equal(950m, bill.Lines[0].Net);
            Assert.Equal(250m, bill.Lines[1].Net);
            Assert.Equal(1200m, bill.Payable);
            Assert.Equal(new[]
            {
                "Coupon: SAVE100",
                "Coupon Discount: -100.00",
                "PAYABLE: 1200.00"
            }, bill.CouponLines);

            Assert.Equal(1200m, bill.SellingSale);
            Assert.Equal(1200m, bill.RevenueSales);
            Assert.Equal(700m, bill.Profit);
            Assert.Equal(700m, bill.RevenueProfit);
            Assert.Equal(1200m, bill.ClosingCash);
            Assert.Equal(1200m, CouponCalculations.ReportedBilledSale(bill.Payable, bill.CouponDiscount, true));
        }

        [Fact]
        public void RewardPercent_AppliesBeforeCouponRupees()
        {
            decimal percent = CouponSaleMath.CombinedDiscountPercent(autoPercent: 0m, manualPercent: 0m, rewardPercent: 10m);
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(1000m, 1, percent, cost: 400m) },
                "REWARD100",
                100m);

            Assert.Equal(900m, bill.ItemsBeforeCoupon);
            Assert.Equal(800m, bill.Payable);
            Assert.Equal(100m, bill.Lines[0].CouponShare);
            Assert.Equal(400m, bill.Profit);
            Assert.Equal(800m, bill.SellingSale);
            Assert.Equal(800m, bill.RevenueSales);
            Assert.Equal(800m, bill.ClosingCash);
            Assert.Contains("Coupon: REWARD100", bill.CouponLines);
            Assert.Contains("Coupon Discount: -100.00", bill.CouponLines);
        }

        [Fact]
        public void RewardAndManual_ClampAt100Percent()
        {
            Assert.Equal(100m, CouponSaleMath.CombinedDiscountPercent(40m, 50m, 30m));
            Assert.Equal(0m, CouponSaleMath.CombinedDiscountPercent(-5m, 0m, 0m));

            CouponSaleMath.BillOut free = CouponSaleMath.Settle(
                new[] { Item(200m, 1, 100m, cost: 50m) },
                "FREE",
                100m);
            Assert.Equal(0m, free.Payable);
            Assert.True(free.CouponApplied);
            Assert.Equal(0m, free.CouponDiscount);
            Assert.Contains("Coupon: FREE", free.CouponLines);
            Assert.Equal(-50m, free.Profit);
        }

        [Fact]
        public void GstLine_CouponKeepsTaxablePlusGstEqualToNet()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(118m, 1, gst: 18m, cost: 40m) },
                "GST10",
                18m);

            Assert.Equal(100m, bill.Payable);
            Assert.Equal(bill.Lines[0].Net, bill.Lines[0].Taxable + bill.Lines[0].Gst);
            Assert.Equal(100m, bill.SellingSale);
            Assert.Equal(60m, bill.RevenueProfit);
            Assert.Equal(100m, bill.ClosingCash);
        }

        [Fact]
        public void CouponLargerThanBill_StopsAtPayableZero()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(40m, 1, cost: 10m) },
                "BIG",
                100m);

            Assert.Equal(0m, bill.Payable);
            Assert.Equal(40m, bill.CouponDiscount);
            Assert.Equal(0m, bill.SellingSale);
            Assert.Equal(0m, bill.ClosingCash);
            Assert.Equal(-10m, bill.Profit);
            Assert.Contains("PAYABLE: 0.00", bill.CouponLines);
        }

        [Fact]
        public void HoldBill_RestoresCouponCodeAndSamePayable()
        {
            string held = CouponSaleMath.RememberHoldCoupon("  bubby100 ");
            Assert.Equal("BUBBY100", held);

            CouponSaleMath.BillOut resumed = CouponSaleMath.Settle(
                new[] { Item(200m, 2), Item(100m, 1) },
                held,
                100m);
            CouponSaleMath.BillOut direct = CouponSaleMath.Settle(
                new[] { Item(200m, 2), Item(100m, 1) },
                "BUBBY100",
                100m);

            Assert.Equal(direct.Payable, resumed.Payable);
            Assert.Equal(direct.CouponLines, resumed.CouponLines);
            Assert.Equal(400m, resumed.SellingSale);

            CouponSaleMath.BillOut heldWithoutCoupon = CouponSaleMath.Settle(
                new[] { Item(200m, 1) },
                CouponSaleMath.RememberHoldCoupon(""),
                50m);
            Assert.Empty(heldWithoutCoupon.CouponLines);
            Assert.Equal(200m, heldWithoutCoupon.Payable);
        }

        [Fact]
        public void PartialReturn_RefundsPostCouponNetAndReportsUseWhatIsLeft()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(500m, 2, cost: 200m), Item(300m, 1, cost: 100m) },
                "SAVE100",
                100m);

            decimal refund = CouponSaleMath.CashRefund(bill.Lines[0], 1);
            Assert.Equal(475m, refund);

            CouponSaleMath.BillOut left = CouponSaleMath.AfterPartialReturn(bill, 0, 1);
            Assert.Equal(725m, left.Payable);
            Assert.Equal(725m, left.SellingSale);
            Assert.Equal(725m, left.RevenueSales);
            Assert.Equal(425m, left.Profit);
            Assert.Equal(725m, left.ClosingCash);
            Assert.Equal(25m, left.Lines[0].CouponShare);
            Assert.Contains("Coupon: SAVE100", left.CouponLines);

            decimal sameDayRefundLine = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", refund, new DateTime(2026, 9, 26), new DateTime(2026, 9, 26), "Cash");
            Assert.Equal(0m, sameDayRefundLine);
            Assert.Equal(725m, ClosingCashCalculations.CombineCashFromDb(left.ClosingCash, sameDayRefundLine));
        }

        [Fact]
        public void ReturnWithoutCoupon_StillRefundsFullNet()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(200m, 2, cost: 50m) },
                "",
                0m);

            Assert.Empty(bill.CouponLines);
            Assert.Equal(200m, CouponSaleMath.CashRefund(bill.Lines[0], 1));
            CouponSaleMath.BillOut left = CouponSaleMath.AfterPartialReturn(bill, 0, 1);
            Assert.Equal(200m, left.Payable);
            Assert.Equal(200m, left.SellingSale);
            Assert.Equal(150m, left.Profit);
            Assert.Equal(200m, left.ClosingCash);
            Assert.Empty(left.CouponLines);
            Assert.Empty(ReceiptCalculations.ReturnCouponLines("", 0m, false));
        }

        [Fact]
        public void PreviousDayReturn_ClosingDropsCashRefund_SellingIgnoresIt()
        {
            decimal refund = 475m;
            DateTime today = new DateTime(2026, 9, 26);
            decimal drawer = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", refund, today, today.AddDays(-1), "Cash");

            Assert.Equal(-475m, drawer);
            Assert.Equal(525m, ClosingCashCalculations.CombineCashFromDb(1000m, drawer));
            Assert.Equal(1000m, SellingCalculations.CombineTotalSale(1000m, refund));
        }

        [Fact]
        public void ExchangeEqualValue_KeepsCouponAndDoesNotAskExtraCash()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(500m, 2, cost: 200m), Item(300m, 1, cost: 100m) },
                "SAVE100",
                100m);

            CouponSaleMath.ExchangeOut swap = CouponSaleMath.Exchange(
                bill,
                new[] { 0, 1 },
                new[] { Item(300m, 1, cost: 90m) });

            Assert.Equal(300m, swap.ReturnCredit);
            Assert.Equal(300m, swap.NewItemsBeforeCoupon);
            Assert.Equal(0m, swap.Collect);
            Assert.Equal(0m, swap.Refund);
            Assert.Equal(1200m, swap.Bill.Payable);
            Assert.Equal(1200m, swap.Bill.SellingSale);
            Assert.Equal(1200m, swap.Bill.RevenueSales);
            Assert.Equal(1200m, swap.SameDayClosingCash);
            Assert.Equal(100m, swap.Bill.CouponDiscount);
            Assert.Contains("Coupon: SAVE100", swap.Bill.CouponLines);
            Assert.Equal(
                new[] { "Coupon: SAVE100", "Coupon on this bill: -100.00" },
                ReceiptCalculations.ReturnCouponLines("SAVE100", 100m, exchange: true));
        }

        [Fact]
        public void ExchangeCheaperItem_RefundsDifference_ReportsUseRemainingBill()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(500m, 1, cost: 200m) },
                "SAVE100",
                100m);

            Assert.Equal(400m, bill.Payable);

            CouponSaleMath.ExchangeOut swap = CouponSaleMath.Exchange(
                bill,
                new[] { 1 },
                new[] { Item(300m, 1, cost: 80m) });

            Assert.Equal(500m, swap.ReturnCredit);
            Assert.Equal(200m, swap.Refund);
            Assert.Equal(0m, swap.Collect);
            Assert.Equal(200m, swap.Bill.Payable);
            Assert.Equal(200m, swap.Bill.SellingSale);
            Assert.Equal(200m, swap.Bill.RevenueSales);
            Assert.Equal(120m, swap.Bill.Profit);
            Assert.Equal(200m, swap.SameDayClosingCash);
            Assert.Contains("Coupon Discount: -100.00", swap.Bill.CouponLines);
        }

        [Fact]
        public void ExchangeDearerItem_CollectsDifference_ClosingUsesNewPayable()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(400m, 1, cost: 100m) },
                "OFF50",
                50m);

            CouponSaleMath.ExchangeOut swap = CouponSaleMath.Exchange(
                bill,
                new[] { 1 },
                new[] { Item(600m, 1, cost: 200m) });

            Assert.Equal(200m, swap.Collect);
            Assert.Equal(550m, swap.Bill.Payable);
            Assert.Equal(550m, swap.Bill.SellingSale);
            Assert.Equal(550m, swap.Bill.RevenueSales);
            Assert.Equal(350m, swap.Bill.RevenueProfit);
            Assert.Equal(550m, swap.SameDayClosingCash);
            Assert.Equal(0m, ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Cash", swap.Collect, DateTime.Today, DateTime.Today, "Cash"));
        }

        [Fact]
        public void OnlineBill_CouponDoesNotEnterCashDrawer()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(500m, 1) },
                "SAVE100",
                100m,
                cashBill: false);

            Assert.Equal(400m, bill.SellingSale);
            Assert.Equal(400m, bill.RevenueSales);
            Assert.Equal(0m, bill.ClosingCash);
        }

        [Fact]
        public void OldBill_CouponOutsideLines_IsSubtractedOnce()
        {
            decimal oldSale = CouponCalculations.ReportedBilledSale(1300m, 100m, couponInsideLineNets: false);
            decimal newSale = CouponCalculations.ReportedBilledSale(1200m, 100m, couponInsideLineNets: true);

            Assert.Equal(1200m, oldSale);
            Assert.Equal(1200m, newSale);
            Assert.Equal(700m, CouponCalculations.Profit(oldSale, 500m));
            Assert.Equal(1200m, SellingCalculations.BilledSaleTotal(oldSale));
            Assert.Equal(1200m, ClosingCashCalculations.CashBillContribution(newSale));
        }

        [Fact]
        public void ReturnSlip_HidesCouponWhenNothingWasApplied()
        {
            Assert.Empty(ReceiptCalculations.ReturnCouponLines(null, 100m, false));
            Assert.Empty(ReceiptCalculations.ReturnCouponLines("SAVE", 0m, true));
            Assert.Equal(
                new[] { "Coupon: SAVE", "Coupon Discount: -25.00" },
                ReceiptCalculations.ReturnCouponLines(" save ", 25m, false));
        }

        [Fact]
        public void ThreeItems_RemainderPaise_StillMatchPayableAndProfit()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Item(100m, 1, cost: 10m), Item(100m, 1, cost: 10m), Item(100m, 1, cost: 10m) },
                "SPLIT",
                100m);

            Assert.Equal(100m, bill.CouponDiscount);
            Assert.Equal(200m, bill.Payable);
            Assert.Equal(200m, bill.SellingSale);
            Assert.Equal(200m, bill.RevenueSales);
            Assert.Equal(170m, bill.Profit);
            Assert.Equal(200m, bill.ClosingCash);
            Assert.Equal(33.33m, bill.Lines[0].PerUnitCoupon);
            Assert.Equal(33.34m, bill.Lines[2].PerUnitCoupon);
        }
    }
}
