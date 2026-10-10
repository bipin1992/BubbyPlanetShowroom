using System;
using System.Collections.Generic;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Sale, reward, hold, return and exchange money used by Selling,
    /// Revenue and closing cash. Blank numbers stay zero and do not invent a coupon.
    /// </summary>
    public static class CouponSaleMath
    {
        public sealed class LineIn
        {
            public decimal Price { get; init; }
            public int Qty { get; init; }
            public decimal DiscountPercent { get; init; }
            public decimal GstPercent { get; init; }
            public decimal CostPerUnit { get; init; }
        }

        public sealed class LineOut
        {
            public int Qty { get; init; }
            public decimal Gross { get; init; }
            public decimal NetBeforeCoupon { get; init; }
            public decimal CouponShare { get; init; }
            public decimal PerUnitCoupon { get; init; }
            public decimal DiscountAmount { get; init; }
            public decimal Taxable { get; init; }
            public decimal Gst { get; init; }
            public decimal Net { get; init; }
            public decimal Cost { get; init; }
        }

        public sealed class BillOut
        {
            public LineOut[] Lines { get; init; } = Array.Empty<LineOut>();
            public decimal ItemsBeforeCoupon { get; init; }
            public decimal CouponDiscount { get; init; }
            public string CouponCode { get; init; } = "";
            public bool CouponApplied { get; init; }
            public decimal Payable { get; init; }
            public decimal Cost { get; init; }
            public decimal Profit { get; init; }
            public decimal SellingSale { get; init; }
            public decimal RevenueSales { get; init; }
            public decimal RevenueProfit { get; init; }
            public decimal ClosingCash { get; init; }
            public string[] CouponLines { get; init; } = Array.Empty<string>();
        }

        public sealed class ExchangeOut
        {
            public decimal ReturnCredit { get; init; }
            public decimal NewItemsBeforeCoupon { get; init; }
            public decimal Collect { get; init; }
            public decimal Refund { get; init; }
            public BillOut Bill { get; init; } = new BillOut();
            public decimal SameDayClosingCash { get; init; }
        }

        public static decimal CombinedDiscountPercent(decimal autoPercent, decimal manualPercent, decimal rewardPercent)
        {
            decimal sum = autoPercent + manualPercent + rewardPercent;
            if (sum < 0m)
                return 0m;
            if (sum > 100m)
                return 100m;
            return sum;
        }

        public static decimal ClampPercent(decimal value)
        {
            if (value < 0m)
                return 0m;
            if (value > 100m)
                return 100m;
            return value;
        }

        /// <summary>
        /// One line percent. A typed manual percent replaces auto.
        /// Reward is added on top of whichever base is active.
        /// </summary>
        public static decimal LinePercent(decimal auto, decimal manual, decimal reward, bool manualRow)
        {
            decimal basePercent = manualRow ? Math.Max(0m, manual) : Math.Max(0m, auto);
            return ClampPercent(basePercent + Math.Max(0m, reward));
        }

        /// <summary>
        /// Discount % shows base + reward. Leaving that cell must not add reward again.
        /// A new number is a new manual base. Zero clears the manual lock.
        /// </summary>
        public static decimal ManualBaseFromDiscountEdit(decimal typedPercent, decimal storedManual, decimal rewardPercent)
        {
            decimal typed = ClampPercent(typedPercent);
            decimal reward = Math.Max(0m, rewardPercent);
            if (reward > 100m)
                reward = 100m;
            if (typed <= 0m)
                return 0m;

            decimal stored = Math.Max(0m, storedManual);
            decimal shown = ClampPercent(stored + reward);
            if (stored > 0m && typed == shown)
                return ClampPercent(stored);
            return typed;
        }

        /// <summary>
        /// Manual mall % replaces reward. Reward cancel drops only the reward part.
        /// Mall 0% adds nothing.
        /// </summary>
        public static decimal BillDiscountPercent(
            decimal autoPercent,
            decimal mallPercent,
            decimal rewardPercent,
            bool rewardOn,
            bool mallIsManual)
        {
            if (mallIsManual)
                return CombinedDiscountPercent(mallPercent, 0m, 0m);
            return CombinedDiscountPercent(autoPercent, mallPercent, rewardOn ? rewardPercent : 0m);
        }

        public static int StockAfterSale(int onHand, int soldQty)
        {
            if (soldQty <= 0 || onHand < soldQty)
                return onHand;
            return onHand - soldQty;
        }

        public static int StockAfterReturn(int onHand, int returnQty)
        {
            if (returnQty <= 0)
                return onHand;
            return onHand + returnQty;
        }

        public static int StockAfterExchange(int onHand, int returnedQty, int issuedQty)
        {
            return StockAfterSale(StockAfterReturn(onHand, returnedQty), issuedQty);
        }

        public static string RememberHoldCoupon(string? typedCode)
        {
            return CouponCalculations.NormalizeCode(typedCode);
        }

        public static BillOut Settle(IReadOnlyList<LineIn>? lines, string? couponCode, decimal couponRupees, bool cashBill = true)
        {
            var prepared = new List<(LineIn Input, decimal Gross, decimal Taxable, decimal Gst, decimal Net)>();
            if (lines != null)
            {
                foreach (LineIn line in lines)
                {
                    if (line == null || line.Qty <= 0 || line.Price < 0m)
                        continue;

                    decimal percent = CombinedDiscountPercent(line.DiscountPercent, 0m, 0m);
                    ReturnCalculations.CalculateLineAmounts(
                        line.Price,
                        line.GstPercent,
                        percent,
                        line.Qty,
                        out decimal taxable,
                        out decimal gst,
                        out decimal gross,
                        out decimal net);
                    prepared.Add((line, gross, taxable, gst, net));
                }
            }

            decimal before = 0m;
            foreach (var row in prepared)
                before += row.Net;
            before = Round2(before);

            string code = CouponCalculations.NormalizeCode(couponCode);
            bool hasCode = !string.IsNullOrWhiteSpace(code);
            decimal applied = hasCode
                ? CouponCalculations.ClampRupeeDiscount(couponRupees, before)
                : 0m;
            bool couponOn = hasCode;

            int[] qtys = new int[prepared.Count];
            decimal[] nets = new decimal[prepared.Count];
            for (int i = 0; i < prepared.Count; i++)
            {
                qtys[i] = prepared[i].Input.Qty;
                nets[i] = prepared[i].Net;
            }

            decimal[] shares = couponOn
                ? CouponCalculations.AllocateEvenByItem(applied, qtys, nets)
                : new decimal[prepared.Count];

            var settled = new LineOut[prepared.Count];
            decimal payable = 0m;
            decimal cost = 0m;
            decimal couponUsed = 0m;
            for (int i = 0; i < prepared.Count; i++)
            {
                var row = prepared[i];
                decimal discountBefore = Round2(row.Gross - row.Net);
                CouponCalculations.CouponAdjustedLine adjusted = CouponCalculations.AddCouponToLine(
                    discountBefore,
                    row.Taxable,
                    row.Gst,
                    row.Net,
                    shares.Length > i ? shares[i] : 0m);

                decimal lineCost = Round2(Math.Max(0m, row.Input.CostPerUnit) * row.Input.Qty);
                settled[i] = new LineOut
                {
                    Qty = row.Input.Qty,
                    Gross = row.Gross,
                    NetBeforeCoupon = row.Net,
                    CouponShare = adjusted.CouponShare,
                    PerUnitCoupon = CouponCalculations.PerUnitShare(adjusted.CouponShare, row.Input.Qty),
                    DiscountAmount = adjusted.DiscountAmount,
                    Taxable = adjusted.TaxableAmount,
                    Gst = adjusted.GstAmount,
                    Net = adjusted.NetAmount,
                    Cost = lineCost
                };
                payable += adjusted.NetAmount;
                cost += lineCost;
                couponUsed += adjusted.CouponShare;
            }

            payable = Round2(payable);
            cost = Round2(cost);
            couponUsed = Round2(couponUsed);
            decimal reported = CouponCalculations.ReportedBilledSale(payable, couponUsed, couponOn);
            decimal profit = CouponCalculations.Profit(reported, cost);

            return new BillOut
            {
                Lines = settled,
                ItemsBeforeCoupon = before,
                CouponDiscount = couponOn ? couponUsed : 0m,
                CouponCode = couponOn ? code : "",
                CouponApplied = couponOn,
                Payable = payable,
                Cost = cost,
                Profit = profit,
                SellingSale = SellingCalculations.BilledSaleTotal(reported),
                RevenueSales = reported,
                RevenueProfit = profit,
                ClosingCash = cashBill ? ClosingCashCalculations.CashBillContribution(reported) : 0m,
                CouponLines = ReceiptCalculations.CouponBillLines(couponOn ? code : "", couponOn ? couponUsed : 0m, payable)
            };
        }

        public static decimal CashRefund(LineOut line, int returnQty)
        {
            if (line == null)
                return 0m;
            return ReturnCalculations.CalculateLineRefund(line.Qty, 0, line.Net, returnQty);
        }

        public static BillOut AfterPartialReturn(BillOut bill, int lineIndex, int returnQty, bool cashBill = true)
        {
            if (bill == null || bill.Lines == null || lineIndex < 0 || lineIndex >= bill.Lines.Length)
                return bill ?? new BillOut();

            LineOut line = bill.Lines[lineIndex];
            if (!ReturnCalculations.CanReturn(line.Qty, 0, returnQty))
                return bill;

            decimal refund = CashRefund(line, returnQty);
            int leftQty = line.Qty - returnQty;
            decimal leftShare = CouponCalculations.RemainingCouponShare(line.CouponShare, line.Qty, returnQty, leftQty);
            decimal leftNet = Round2(line.Net - refund);
            decimal leftCost = leftQty <= 0
                ? 0m
                : Round2(line.Cost - Round2(line.Cost / line.Qty * returnQty));

            var lines = new List<LineOut>();
            for (int i = 0; i < bill.Lines.Length; i++)
            {
                if (i != lineIndex)
                {
                    lines.Add(bill.Lines[i]);
                    continue;
                }

                if (leftQty <= 0)
                    continue;

                lines.Add(new LineOut
                {
                    Qty = leftQty,
                    Gross = Round2(line.Gross - Round2(line.Gross / line.Qty * returnQty)),
                    NetBeforeCoupon = Round2(leftNet + leftShare),
                    CouponShare = leftShare,
                    PerUnitCoupon = CouponCalculations.PerUnitShare(leftShare, leftQty),
                    DiscountAmount = Round2(line.DiscountAmount - Round2(line.DiscountAmount / line.Qty * returnQty)),
                    Taxable = 0m,
                    Gst = 0m,
                    Net = leftNet,
                    Cost = leftCost
                });
            }

            return FinishRemaining(lines, bill.CouponCode, cashBill);
        }

        public static ExchangeOut Exchange(BillOut bill, IReadOnlyList<int>? returnQtyByLine, IReadOnlyList<LineIn>? newItems, bool cashBill = true)
        {
            var kept = new List<LineOut>();
            decimal returnCredit = 0m;
            if (bill?.Lines != null)
            {
                for (int i = 0; i < bill.Lines.Length; i++)
                {
                    LineOut line = bill.Lines[i];
                    int take = 0;
                    if (returnQtyByLine != null && i < returnQtyByLine.Count)
                        take = returnQtyByLine[i];
                    if (take < 0)
                        take = 0;
                    if (take > line.Qty)
                        take = line.Qty;

                    if (take > 0)
                    {
                        decimal post = CashRefund(line, take);
                        decimal share = CouponCalculations.CouponShareForUnits(line.CouponShare, line.Qty, take);
                        returnCredit += Round2(post + share);
                    }

                    int left = line.Qty - take;
                    if (left <= 0)
                        continue;

                    decimal leftShare = CouponCalculations.RemainingCouponShare(line.CouponShare, line.Qty, take, left);
                    decimal leftNet = take <= 0
                        ? line.Net
                        : Round2(line.Net - CashRefund(line, take));
                    decimal leftCost = take <= 0
                        ? line.Cost
                        : Round2(line.Cost - Round2(line.Cost / line.Qty * take));
                    decimal leftDiscount = take <= 0
                        ? line.DiscountAmount
                        : Round2(line.DiscountAmount - Round2(line.DiscountAmount / line.Qty * take));
                    kept.Add(new LineOut
                    {
                        Qty = left,
                        Gross = line.Gross,
                        NetBeforeCoupon = Round2(leftNet + leftShare),
                        CouponShare = leftShare,
                        PerUnitCoupon = CouponCalculations.PerUnitShare(leftShare, left),
                        DiscountAmount = leftDiscount,
                        Net = leftNet,
                        Cost = leftCost
                    });
                }
            }

            returnCredit = Round2(returnCredit);
            BillOut freshNew = Settle(newItems, "", 0m, cashBill);
            decimal newBefore = freshNew.ItemsBeforeCoupon;

            var poolQty = new List<int>();
            var poolNet = new List<decimal>();
            var poolCost = new List<decimal>();
            var poolDiscount = new List<decimal>();
            var poolTaxable = new List<decimal>();
            var poolGst = new List<decimal>();
            foreach (LineOut line in kept)
            {
                poolQty.Add(line.Qty);
                poolNet.Add(line.NetBeforeCoupon);
                poolCost.Add(line.Cost);
                poolDiscount.Add(Round2(Math.Max(0m, line.DiscountAmount - line.CouponShare)));
                poolTaxable.Add(line.Taxable);
                poolGst.Add(line.Gst);
            }

            foreach (LineOut line in freshNew.Lines)
            {
                poolQty.Add(line.Qty);
                poolNet.Add(line.Net);
                poolCost.Add(line.Cost);
                poolDiscount.Add(line.DiscountAmount);
                poolTaxable.Add(line.Taxable);
                poolGst.Add(line.Gst);
            }

            decimal couponPool = bill?.CouponDiscount ?? 0m;
            decimal[] shares = CouponCalculations.AllocateEvenByItem(couponPool, poolQty.ToArray(), poolNet.ToArray());
            var finalLines = new List<LineOut>();
            for (int i = 0; i < poolQty.Count; i++)
            {
                CouponCalculations.CouponAdjustedLine adjusted = CouponCalculations.AddCouponToLine(
                    poolDiscount[i],
                    poolTaxable[i],
                    poolGst[i],
                    poolNet[i],
                    shares[i]);
                finalLines.Add(new LineOut
                {
                    Qty = poolQty[i],
                    NetBeforeCoupon = poolNet[i],
                    CouponShare = adjusted.CouponShare,
                    PerUnitCoupon = CouponCalculations.PerUnitShare(adjusted.CouponShare, poolQty[i]),
                    DiscountAmount = adjusted.DiscountAmount,
                    Taxable = adjusted.TaxableAmount,
                    Gst = adjusted.GstAmount,
                    Net = adjusted.NetAmount,
                    Cost = poolCost[i]
                });
            }

            BillOut finalBill = FinishRemaining(finalLines, bill?.CouponCode, cashBill);
            decimal collect = Round2(Math.Max(0m, newBefore - returnCredit));
            decimal refund = Round2(Math.Max(0m, returnCredit - newBefore));
            decimal sameDayExtra = ClosingCashCalculations.SettlementEffectOnDrawer(
                collect > 0m ? "collect" : "refund",
                "Cash",
                collect > 0m ? collect : refund,
                DateTime.Today,
                DateTime.Today,
                "Cash");

            return new ExchangeOut
            {
                ReturnCredit = returnCredit,
                NewItemsBeforeCoupon = newBefore,
                Collect = collect,
                Refund = refund,
                Bill = finalBill,
                SameDayClosingCash = ClosingCashCalculations.CombineCashFromDb(finalBill.ClosingCash, sameDayExtra)
            };
        }

        private static BillOut FinishRemaining(List<LineOut> lines, string? couponCode, bool cashBill)
        {
            decimal payable = 0m;
            decimal cost = 0m;
            decimal coupon = 0m;
            foreach (LineOut line in lines)
            {
                payable += line.Net;
                cost += line.Cost;
                coupon += line.CouponShare;
            }

            payable = Round2(payable);
            cost = Round2(cost);
            coupon = Round2(coupon);
            bool applied = coupon > 0m && !string.IsNullOrWhiteSpace(couponCode);
            decimal reported = CouponCalculations.ReportedBilledSale(payable, coupon, applied || coupon > 0m);
            decimal profit = CouponCalculations.Profit(reported, cost);
            string code = applied ? CouponCalculations.NormalizeCode(couponCode) : "";

            return new BillOut
            {
                Lines = lines.ToArray(),
                ItemsBeforeCoupon = Round2(payable + coupon),
                CouponDiscount = coupon,
                CouponCode = code,
                CouponApplied = applied,
                Payable = payable,
                Cost = cost,
                Profit = profit,
                SellingSale = SellingCalculations.BilledSaleTotal(reported),
                RevenueSales = reported,
                RevenueProfit = profit,
                ClosingCash = cashBill ? ClosingCashCalculations.CashBillContribution(reported) : 0m,
                CouponLines = ReceiptCalculations.CouponBillLines(code, coupon, payable)
            };
        }

        private static decimal Round2(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
