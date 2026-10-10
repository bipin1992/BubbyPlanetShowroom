using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    /// <summary>
    /// One bill is checked on Receipt, Selling, Revenue and closing cash,
    /// then again after return. A common coupon has no assigned mobile, so
    /// a new or old customer can both use it. An assigned coupon cannot.
    /// </summary>
    public class ReceiptChannelCouponTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 7);
        private static readonly DateTime Yesterday = new DateTime(2026, 10, 6);

        private static CouponSaleMath.LineIn Shirt()
        {
            return new CouponSaleMath.LineIn
            {
                Price = 500m,
                Qty = 2,
                CostPerUnit = 150m
            };
        }

        private static CouponSaleMath.LineIn Toy()
        {
            return new CouponSaleMath.LineIn
            {
                Price = 200m,
                Qty = 1,
                DiscountPercent = 10m,
                CostPerUnit = 80m
            };
        }

        private static void AssertChannels(CouponSaleMath.BillOut bill, decimal payable, decimal profit, bool cash)
        {
            Assert.Equal(payable, bill.Payable);
            Assert.Equal(payable, bill.LinesSum());
            Assert.Equal(payable, bill.SellingSale);
            Assert.Equal(payable, SellingCalculations.BilledSaleTotal(bill.SellingSale));
            Assert.Equal(payable, SellingCalculations.CombineTotalSale(bill.SellingSale, 999m));
            Assert.Equal(payable, bill.RevenueSales);
            Assert.Equal(profit, bill.Profit);
            Assert.Equal(profit, bill.RevenueProfit);
            Assert.Equal(bill.Payable, bill.RevenueSales);
            if (cash)
            {
                Assert.Equal(payable, bill.ClosingCash);
                Assert.Equal(payable, ClosingCashCalculations.CashBillContribution(bill.RevenueSales));
            }
            else
            {
                Assert.Equal(0m, bill.ClosingCash);
            }
        }

        [Fact]
        public void ReceiptPageSteps_SavedCouponNetsMatchSellingReturnAndClosing()
        {
            ReturnCalculations.CalculateLineAmounts(500m, 0m, 0m, 2, out decimal tax1, out decimal gst1, out decimal gross1, out decimal net1);
            ReturnCalculations.CalculateLineAmounts(200m, 0m, 10m, 1, out decimal tax2, out decimal gst2, out decimal gross2, out decimal net2);

            decimal itemsGrandTotal = Math.Round(net1 + net2, 2, MidpointRounding.AwayFromZero);
            CouponCheckResult check = CouponCalculations.Evaluate(
                "SAVE100", true, Today,
                new DateTime(2026, 10, 1), new DateTime(2026, 10, 31),
                true, 500m, 100m, itemsGrandTotal);
            decimal payable = CouponCalculations.PayableAfterCoupon(itemsGrandTotal, check.AppliedDiscount);
            decimal[] shares = CouponCalculations.AllocateEvenByItem(check.AppliedDiscount, new[] { 2, 1 }, new[] { net1, net2 });

            CouponCalculations.CouponAdjustedLine shirt = CouponCalculations.AddCouponToLine(
                Math.Round(gross1 - (tax1 + gst1), 2, MidpointRounding.AwayFromZero), tax1, gst1, net1, shares[0]);
            CouponCalculations.CouponAdjustedLine toy = CouponCalculations.AddCouponToLine(
                Math.Round(gross2 - (tax2 + gst2), 2, MidpointRounding.AwayFromZero), tax2, gst2, net2, shares[1]);

            decimal savedNets = Math.Round(shirt.NetAmount + toy.NetAmount, 2, MidpointRounding.AwayFromZero);
            decimal savedTax = Math.Round(shirt.TaxableAmount + toy.TaxableAmount, 2, MidpointRounding.AwayFromZero);
            decimal savedGst = Math.Round(shirt.GstAmount + toy.GstAmount, 2, MidpointRounding.AwayFromZero);

            Assert.Equal(1080m, payable);
            Assert.Equal(payable, savedNets);
            Assert.Equal(payable, savedTax + savedGst);
            Assert.Equal(payable, CouponCalculations.ReportedBilledSale(savedNets, check.AppliedDiscount, couponInsideLineNets: true));
            Assert.Equal(payable, SellingCalculations.BilledSaleTotal(savedNets));
            Assert.Equal(payable, ClosingCashCalculations.CashBillContribution(savedNets));
            Assert.Equal(475m, ReturnCalculations.CalculateLineRefund(2, 0, shirt.NetAmount, 1));
        }

        [Fact]
        public void WithoutCoupon_ReceiptSellingRevenueAndClosingMatch()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Shirt(), Toy() },
                couponCode: "",
                couponRupees: 0m);

            Assert.False(bill.CouponApplied);
            Assert.Empty(bill.CouponLines);
            Assert.Equal(1000m, bill.Lines[0].Net);
            Assert.Equal(180m, bill.Lines[1].Net);
            AssertChannels(bill, 1180m, 800m, cash: true);
            Assert.Equal(7, CouponSaleMath.StockAfterSale(10, 3));
        }

        [Fact]
        public void WithoutCoupon_PartialThenFullReturn_ZerosEveryTab()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(new[] { Shirt(), Toy() }, "", 0m);

            Assert.Equal(500m, CouponSaleMath.CashRefund(bill.Lines[0], 1));
            CouponSaleMath.BillOut half = CouponSaleMath.AfterPartialReturn(bill, 0, 1);
            Assert.Empty(half.CouponLines);
            AssertChannels(half, 680m, 450m, cash: true);
            Assert.Equal(8, CouponSaleMath.StockAfterReturn(CouponSaleMath.StockAfterSale(10, 3), 1));

            decimal sameDayRefund = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", 500m, Today, Today, "Cash");
            Assert.Equal(0m, sameDayRefund);
            Assert.Equal(680m, ClosingCashCalculations.CombineCashFromDb(half.ClosingCash, sameDayRefund));

            CouponSaleMath.BillOut none = CouponSaleMath.AfterPartialReturn(
                CouponSaleMath.AfterPartialReturn(half, 0, 1),
                0,
                1);
            AssertChannels(none, 0m, 0m, cash: true);
            Assert.Equal(0m, ClosingCashCalculations.CombineCashFromDb(none.ClosingCash, 0m));
            Assert.Equal(10, CouponSaleMath.StockAfterReturn(7, 3));
        }

        [Fact]
        public void CommonCoupon_AppliesToNewAndOldCustomers()
        {
            CouponPhoneGateResult nobodyTyped = CouponCustomerAccess.CheckAssignedUse(
                "SAVE100", "", hasAssignments: false, phoneIsAssigned: false, usesPerPhone: 1, redeemedCount: 0);
            CouponPhoneGateResult newCustomer = CouponCustomerAccess.CheckAssignedUse(
                "SAVE100", "9000000001", hasAssignments: false, phoneIsAssigned: false, usesPerPhone: 1, redeemedCount: 0);
            CouponPhoneGateResult oldCustomer = CouponCustomerAccess.CheckAssignedUse(
                "SAVE100", "9876543210", hasAssignments: false, phoneIsAssigned: false, usesPerPhone: 2, redeemedCount: 1);
            CouponPhoneGateResult oldCustomerDone = CouponCustomerAccess.CheckAssignedUse(
                "SAVE100", "9876543210", hasAssignments: false, phoneIsAssigned: false, usesPerPhone: 1, redeemedCount: 1);

            Assert.False(nobodyTyped.Allowed);
            Assert.Equal("Enter mobile", nobodyTyped.Status);
            Assert.True(newCustomer.Allowed);
            Assert.True(oldCustomer.Allowed);
            Assert.False(oldCustomerDone.Allowed);

            CouponCheckResult check = CouponCalculations.Evaluate(
                "save100",
                found: true,
                Today,
                new DateTime(2026, 10, 1),
                new DateTime(2026, 10, 31),
                isActive: true,
                minPurchaseAmount: 500m,
                discountAmount: 100m,
                grandTotal: 1180m);
            Assert.True(check.Applied);
            Assert.Equal(100m, check.AppliedDiscount);

            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Shirt(), Toy() },
                check.Code,
                check.AppliedDiscount);

            Assert.Equal("SAVE100", bill.CouponCode);
            Assert.Equal(50m, bill.Lines[0].CouponShare);
            Assert.Equal(50m, bill.Lines[1].CouponShare);
            Assert.Equal(950m, bill.Lines[0].Net);
            Assert.Equal(130m, bill.Lines[1].Net);
            Assert.Contains("Coupon: SAVE100", bill.CouponLines);
            Assert.Contains("Coupon Discount: -100.00", bill.CouponLines);
            Assert.Contains("PAYABLE: 1080.00", bill.CouponLines);
            AssertChannels(bill, 1080m, 700m, cash: true);
        }

        [Fact]
        public void CommonCoupon_BelowMinimum_DoesNotChangeTheBill()
        {
            CouponCheckResult check = CouponCalculations.Evaluate(
                "SAVE100",
                true,
                Today,
                new DateTime(2026, 10, 1),
                new DateTime(2026, 10, 31),
                true,
                minPurchaseAmount: 2000m,
                discountAmount: 100m,
                grandTotal: 1180m);

            Assert.False(check.Applied);
            Assert.Equal(0m, check.AppliedDiscount);

            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(new[] { Shirt(), Toy() }, "", 0m);
            AssertChannels(bill, 1180m, 800m, cash: true);
            Assert.Empty(bill.CouponLines);
        }

        [Fact]
        public void CommonCoupon_PartialReturn_KeepsSamePayableOnEveryTab()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(new[] { Shirt(), Toy() }, "SAVE100", 100m);

            Assert.Equal(475m, CouponSaleMath.CashRefund(bill.Lines[0], 1));
            CouponSaleMath.BillOut left = CouponSaleMath.AfterPartialReturn(bill, 0, 1);

            Assert.Equal(25m, left.Lines[0].CouponShare);
            Assert.Equal(50m, left.Lines[1].CouponShare);
            Assert.Equal(75m, left.CouponDiscount);
            Assert.Contains("Coupon: SAVE100", left.CouponLines);
            AssertChannels(left, 605m, 375m, cash: true);

            decimal sameDay = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", 475m, Today, Today, "Cash");
            Assert.Equal(0m, sameDay);
            Assert.Equal(605m, ClosingCashCalculations.CombineCashFromDb(left.ClosingCash, sameDay));
            Assert.Equal(605m, SellingCalculations.CombineTotalSale(left.SellingSale, 475m));
        }

        [Fact]
        public void CommonCoupon_FullReturnSameDay_DrawerAndSaleBecomeZero()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(new[] { Shirt(), Toy() }, "SAVE100", 100m);
            CouponSaleMath.BillOut gone = CouponSaleMath.AfterPartialReturn(
                CouponSaleMath.AfterPartialReturn(bill, 0, 2),
                0,
                1);

            AssertChannels(gone, 0m, 0m, cash: true);
            Assert.Equal(0m, gone.Payable);
            Assert.Equal(0m, gone.CouponDiscount);

            decimal sameDay = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", bill.Payable, Today, Today, "Cash");
            Assert.Equal(0m, sameDay);
            Assert.Equal(0m, ClosingCashCalculations.CombineCashFromDb(gone.ClosingCash, sameDay));
            Assert.True(CouponCustomerAccess.CheckAssignedUse(
                "SAVE100", "9876543210", false, false, 1, redeemedCount: 0).Allowed);
        }

        [Fact]
        public void CommonCoupon_YesterdayBillReturnedToday_ClosingDropsCash_SellingDoesNot()
        {
            decimal refund = 1080m;
            decimal drawer = ClosingCashCalculations.SettlementEffectOnDrawer(
                "refund", "Cash", refund, Today, Yesterday, "Cash");

            Assert.Equal(-1080m, drawer);
            Assert.Equal(0m, ClosingCashCalculations.CombineCashFromDb(1080m, drawer));
            Assert.Equal(0m, SellingCalculations.CombineTotalSale(0m, refund));
            Assert.False(SellingCalculations.ExtraCountsInTotalSale(Today, Today, Yesterday));
        }

        [Fact]
        public void CommonCoupon_OnlineBill_StaysOnSellingAndRevenue_NotInDrawer()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[] { Shirt(), Toy() },
                "SAVE100",
                100m,
                cashBill: false);

            AssertChannels(bill, 1080m, 700m, cash: false);
            Assert.Equal(0m, bill.ClosingCash);
        }

        [Fact]
        public void CommonCoupon_WithRewardPercent_CutsPercentFirstThenRupees()
        {
            decimal percent = CouponSaleMath.BillDiscountPercent(0m, 0m, 10m, rewardOn: true, mallIsManual: false);
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[]
                {
                    new CouponSaleMath.LineIn { Price = 1000m, Qty = 1, DiscountPercent = percent, CostPerUnit = 400m }
                },
                "SAVE100",
                100m);

            Assert.Equal(900m, bill.ItemsBeforeCoupon);
            Assert.Equal(100m, bill.CouponDiscount);
            AssertChannels(bill, 800m, 400m, cash: true);
            Assert.Contains("PAYABLE: 800.00", bill.CouponLines);
        }

        [Fact]
        public void CommonCoupon_GstLine_TaxablePlusGstEqualsNetOnEveryTab()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(
                new[]
                {
                    new CouponSaleMath.LineIn { Price = 1180m, Qty = 1, GstPercent = 18m, CostPerUnit = 400m }
                },
                "GST100",
                180m);

            Assert.Equal(bill.Lines[0].Net, bill.Lines[0].Taxable + bill.Lines[0].Gst);
            Assert.Equal(1000m, bill.Payable);
            AssertChannels(bill, 1000m, 600m, cash: true);
        }

        [Fact]
        public void AssignedCoupon_BlocksOtherNumbers_CommonCouponDoesNot()
        {
            CouponPhoneGateResult stranger = CouponCustomerAccess.CheckAssignedUse(
                "VIP50", "9000000001", hasAssignments: true, phoneIsAssigned: false, usesPerPhone: 2, redeemedCount: 0);
            CouponPhoneGateResult member = CouponCustomerAccess.CheckAssignedUse(
                "VIP50", "9876543210", hasAssignments: true, phoneIsAssigned: true, usesPerPhone: 2, redeemedCount: 1);
            CouponPhoneGateResult finished = CouponCustomerAccess.CheckAssignedUse(
                "VIP50", "9876543210", hasAssignments: true, phoneIsAssigned: true, usesPerPhone: 2, redeemedCount: 2);

            Assert.False(stranger.Allowed);
            Assert.True(member.Allowed);
            Assert.Equal(1, CouponCustomerAccess.UsesLeft(2, 1));
            Assert.False(finished.Allowed);

            CouponSaleMath.BillOut vip = CouponSaleMath.Settle(new[] { Shirt() }, "VIP50", 50m);
            AssertChannels(vip, 950m, 650m, cash: true);
        }

        [Fact]
        public void MixedDay_CouponBillPlusPlainBill_TabsAddThePayables()
        {
            CouponSaleMath.BillOut plain = CouponSaleMath.Settle(new[] { Toy() }, "", 0m);
            CouponSaleMath.BillOut coupon = CouponSaleMath.Settle(new[] { Shirt() }, "SAVE100", 100m);

            decimal sale = plain.SellingSale + coupon.SellingSale;
            decimal profit = plain.RevenueProfit + coupon.RevenueProfit;
            decimal drawer = plain.ClosingCash + coupon.ClosingCash;

            Assert.Equal(180m, plain.Payable);
            Assert.Equal(900m, coupon.Payable);
            Assert.Equal(1080m, sale);
            Assert.Equal(1080m, SellingCalculations.BilledSaleTotal(sale));
            Assert.Equal(100m + 600m, profit);
            Assert.Equal(1080m, drawer);
            Assert.Equal(1080m, ClosingCashCalculations.CounterTotal(0m, drawer));
        }

        [Fact]
        public void TodayBill_IsInsideSellingMonthAndNotLastMonth()
        {
            SellingCalculations.ResolveFilterRange("Monthly", Today, Today, Today, out DateTime from, out DateTime to);
            Assert.True(SellingCalculations.BillDateTimeInFilter(Today.AddHours(15), from, to));

            SellingCalculations.ResolveFilterRange(
                "Custom",
                Today,
                new DateTime(2026, 9, 1),
                new DateTime(2026, 9, 30),
                out DateTime oldFrom,
                out DateTime oldTo);
            Assert.False(SellingCalculations.BillDateTimeInFilter(Today, oldFrom, oldTo));
        }

        [Fact]
        public void ExchangeAfterCommonCoupon_ClosingUsesTheNewPayable()
        {
            CouponSaleMath.BillOut bill = CouponSaleMath.Settle(new[] { Shirt() }, "SAVE100", 100m);
            AssertChannels(bill, 900m, 600m, cash: true);

            CouponSaleMath.ExchangeOut swap = CouponSaleMath.Exchange(
                bill,
                new[] { 1 },
                new[] { new CouponSaleMath.LineIn { Price = 700m, Qty = 1, CostPerUnit = 200m } });

            Assert.Equal(500m, swap.ReturnCredit);
            Assert.Equal(200m, swap.Collect);
            Assert.Equal(0m, swap.Refund);
            Assert.Equal(1100m, swap.Bill.Payable);
            Assert.Equal(1100m, swap.Bill.SellingSale);
            Assert.Equal(1100m, swap.Bill.RevenueSales);
            Assert.Equal(750m, swap.Bill.Profit);
            Assert.Equal(750m, swap.Bill.RevenueProfit);
            Assert.Equal(1100m, swap.Bill.ClosingCash);
            Assert.Equal(1100m, swap.SameDayClosingCash);
            Assert.Contains("Coupon: SAVE100", swap.Bill.CouponLines);
            Assert.Equal(0m, ClosingCashCalculations.SettlementEffectOnDrawer(
                "collect", "Cash", swap.Collect, Today, Today, "Cash"));
        }
    }

    internal static class ReceiptChannelCouponMath
    {
        public static decimal LinesSum(this CouponSaleMath.BillOut bill)
        {
            decimal sum = 0m;
            foreach (CouponSaleMath.LineOut line in bill.Lines)
                sum += line.Net;
            return Math.Round(sum, 2, MidpointRounding.AwayFromZero);
        }
    }
}
