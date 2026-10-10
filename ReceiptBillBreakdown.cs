using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Receipt bar: amount, then each rupee cut, then the payable.
    /// Reward percent is cut before the coupon rupees, same as the bill.
    /// </summary>
    public static class ReceiptBillBreakdown
    {
        public readonly struct Line
        {
            public Line(
                decimal price,
                int qty,
                decimal autoPercent,
                decimal manualPercent,
                decimal rewardPercent,
                bool manualRow)
            {
                Price = price;
                Qty = qty;
                AutoPercent = autoPercent;
                ManualPercent = manualPercent;
                RewardPercent = rewardPercent;
                ManualRow = manualRow;
            }

            public decimal Price { get; }
            public int Qty { get; }
            public decimal AutoPercent { get; }
            public decimal ManualPercent { get; }
            public decimal RewardPercent { get; }
            public bool ManualRow { get; }
        }

        public readonly struct Totals
        {
            public Totals(
                decimal netTotal,
                decimal discountRupees,
                decimal manualRupees,
                decimal rewardRupees,
                decimal couponRupees,
                decimal grandTotal,
                string text)
            {
                NetTotal = netTotal;
                DiscountRupees = discountRupees;
                ManualRupees = manualRupees;
                RewardRupees = rewardRupees;
                CouponRupees = couponRupees;
                GrandTotal = grandTotal;
                Text = text;
            }

            public decimal NetTotal { get; }
            public decimal DiscountRupees { get; }
            public decimal ManualRupees { get; }
            public decimal RewardRupees { get; }
            public decimal CouponRupees { get; }
            public decimal GrandTotal { get; }
            public string Text { get; }
        }

        public static Totals Build(IEnumerable<Line> lines, decimal couponRupees)
        {
            decimal netTotal = 0m;
            decimal discountRupees = 0m;
            decimal manualRupees = 0m;
            decimal rewardRupees = 0m;

            if (lines != null)
            {
                foreach (Line line in lines)
                {
                    SplitLine(
                        line,
                        out decimal gross,
                        out decimal autoOff,
                        out decimal manualOff,
                        out decimal rewardOff);
                    netTotal += gross;
                    discountRupees += autoOff;
                    manualRupees += manualOff;
                    rewardRupees += rewardOff;
                }
            }

            netTotal = Round2(netTotal);
            discountRupees = Round2(discountRupees);
            manualRupees = Round2(manualRupees);
            rewardRupees = Round2(rewardRupees);

            decimal itemsNet = Round2(netTotal - discountRupees - manualRupees - rewardRupees);
            if (itemsNet < 0m)
                itemsNet = 0m;

            decimal coupon = CouponCalculations.ClampRupeeDiscount(couponRupees, itemsNet);
            decimal grand = CouponCalculations.PayableAfterCoupon(itemsNet, coupon);

            return new Totals(
                netTotal,
                discountRupees,
                manualRupees,
                rewardRupees,
                coupon,
                grand,
                Format(netTotal, discountRupees, manualRupees, rewardRupees, coupon, grand));
        }

        public static string Format(
            decimal netTotal,
            decimal discountRupees,
            decimal manualRupees,
            decimal rewardRupees,
            decimal couponRupees,
            decimal grandTotal)
        {
            if (netTotal <= 0m && grandTotal <= 0m)
                return "";

            var parts = new StringBuilder();
            parts.Append(Money(netTotal));
            parts.Append(" (net total)");

            AppendCut(parts, "discount", discountRupees);
            AppendCut(parts, "manual discount", manualRupees);
            AppendCut(parts, "reward taken", rewardRupees);
            AppendCut(parts, "coupon discount", couponRupees);

            parts.Append("   →   ");
            parts.Append(Money(grandTotal));
            return parts.ToString();
        }

        private static void SplitLine(
            Line line,
            out decimal gross,
            out decimal autoRupees,
            out decimal manualRupees,
            out decimal rewardRupees)
        {
            gross = 0m;
            autoRupees = 0m;
            manualRupees = 0m;
            rewardRupees = 0m;

            if (line.Qty <= 0 || line.Price <= 0m)
                return;

            decimal combined = CouponSaleMath.LinePercent(
                line.AutoPercent,
                line.ManualPercent,
                line.RewardPercent,
                line.ManualRow);
            decimal perUnit = line.Price - (line.Price * combined / 100m);
            decimal net = Round2(perUnit * line.Qty);
            gross = Round2(line.Price * line.Qty);
            decimal cut = gross - net;
            if (cut <= 0m || combined <= 0m)
                return;

            decimal basePercent = line.ManualRow
                ? Math.Max(0m, line.ManualPercent)
                : Math.Max(0m, line.AutoPercent);
            if (basePercent > 100m)
                basePercent = 100m;
            decimal rewardPercent = combined - basePercent;
            if (rewardPercent < 0m)
                rewardPercent = 0m;

            rewardRupees = Round2(cut * rewardPercent / combined);
            decimal baseRupees = cut - rewardRupees;
            if (line.ManualRow)
                manualRupees = baseRupees;
            else
                autoRupees = baseRupees;
        }

        private static void AppendCut(StringBuilder parts, string name, decimal rupees)
        {
            if (rupees <= 0m)
                return;

            parts.Append("   →   ");
            parts.Append(name);
            parts.Append(" (");
            parts.Append(Money(rupees));
            parts.Append(')');
        }

        private static string Money(decimal value)
        {
            return "₹" + value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static decimal Round2(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
