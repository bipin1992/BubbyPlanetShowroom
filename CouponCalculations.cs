using System;
using System.Collections.Generic;
using System.Globalization;

namespace BubbyPlanetShowroom
{
    public sealed class CouponCheckResult
    {
        public bool Found { get; init; }
        public bool Applied { get; init; }
        public string Code { get; init; } = "";
        public string Status { get; init; } = "";
        public decimal MinPurchaseAmount { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal AppliedDiscount { get; init; }
        public string Message { get; init; } = "";
    }

    public static class CouponCalculations
    {
        public static string NormalizeCode(string? code)
        {
            return (code ?? "").Trim().ToUpperInvariant();
        }

        public static bool IsDateRangeValid(DateTime validFrom, DateTime validTo)
        {
            return validTo.Date >= validFrom.Date;
        }

        public static bool MeetsMinimumPurchase(decimal purchaseAmount, decimal minPurchaseAmount)
        {
            if (minPurchaseAmount <= 0m)
                return true;
            return purchaseAmount >= minPurchaseAmount;
        }

        public static decimal ClampRupeeDiscount(decimal discountAmount, decimal payableAmount)
        {
            if (discountAmount <= 0m || payableAmount <= 0m)
                return 0m;
            return Math.Min(discountAmount, payableAmount);
        }

        public static decimal ExtraPercentFromRupeeDiscount(decimal discountAmount, decimal originalGross)
        {
            if (originalGross <= 0m || discountAmount <= 0m)
                return 0m;
            decimal capped = Math.Min(discountAmount, originalGross);
            return Math.Round((capped / originalGross) * 100m, 4, MidpointRounding.AwayFromZero);
        }

        public static decimal PayableAfterCoupon(decimal itemsTotal, decimal discountAmount)
        {
            decimal applied = ClampRupeeDiscount(discountAmount, itemsTotal);
            decimal payable = itemsTotal - applied;
            return payable < 0m ? 0m : payable;
        }

        /// <summary>
        /// Amount that actually hit the bill: remaining item nets minus remaining coupon.
        /// Closing cash, Selling Total Sale, and Revenue sales all use this.
        /// </summary>
        public static decimal BilledSale(decimal remainingItemsNet, decimal remainingCouponDiscount)
        {
            return PayableAfterCoupon(remainingItemsNet, remainingCouponDiscount);
        }

        /// <summary>
        /// Selling, Revenue and closing cash all read this.
        /// New bills already reduced each line, so the coupon is not subtracted again.
        /// Older bills still subtract the order-level coupon once.
        /// </summary>
        public static decimal ReportedBilledSale(
            decimal itemsNet,
            decimal couponDiscount,
            bool couponInsideLineNets)
        {
            if (itemsNet < 0m)
                itemsNet = 0m;
            if (couponInsideLineNets)
                return Round2(itemsNet);
            return BilledSale(itemsNet, couponDiscount);
        }

        public static decimal Profit(decimal billedSale, decimal cost)
        {
            return Round2(billedSale - cost);
        }

        public static decimal AllocateLineSale(
            decimal lineNet,
            decimal remainingItemsNet,
            decimal remainingCouponDiscount)
        {
            if (lineNet <= 0m || remainingItemsNet <= 0m)
                return 0m;
            decimal billed = BilledSale(remainingItemsNet, remainingCouponDiscount);
            return Math.Round(billed * (lineNet / remainingItemsNet), 2, MidpointRounding.AwayFromZero);
        }

        public const string SqlOrderItemsNet =
            "IFNULL((SELECT SUM(IFNULL(x.net_amount,0)) FROM inv_order_details x WHERE x.order_id = o.id),0)";

        /// <summary>
        /// Coupon still stored on the order for the receipt, but new bills already
        /// folded that amount into each line net. Subtract it only for older bills.
        /// </summary>
        public const string SqlCouponOutsideLines =
            "CASE WHEN IFNULL(o.coupon_allocated,0)=1 THEN 0 ELSE IFNULL(o.coupon_discount,0) END";

        public const string SqlOrderBilledSale =
            "GREATEST(0, " + SqlOrderItemsNet + " - " + SqlCouponOutsideLines + ")";

        /// <summary>
        /// One line's sale after coupon. Needs aliases o, d, and ord.items_net.
        /// </summary>
        public const string SqlLineBilledNet =
            "CASE WHEN IFNULL(o.coupon_allocated,0)=1 THEN IFNULL(d.net_amount,0) " +
            "ELSE IFNULL(d.net_amount,0) * GREATEST(0, ord.items_net - IFNULL(o.coupon_discount,0)) / NULLIF(ord.items_net, 0) END";

        /// <summary>
        /// Coupon rupees are split equally across line items, then that item share
        /// belongs to the line (each unit carries share / qty). A line never takes
        /// more than its own net; leftover paise go to lines that still have room.
        /// </summary>
        public static decimal[] AllocateEvenByItem(decimal couponAmount, int[] quantities, decimal[] lineNets)
        {
            int count = quantities?.Length ?? 0;
            decimal[] shares = new decimal[count];
            if (count == 0 || lineNets == null || lineNets.Length != count)
                return shares;

            long remaining = ToPaise(couponAmount);
            if (remaining <= 0)
                return shares;

            long[] caps = new long[count];
            long[] assigned = new long[count];
            List<int> open = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (quantities[i] <= 0)
                    continue;
                long cap = ToPaise(lineNets[i]);
                if (cap <= 0)
                    continue;
                caps[i] = cap;
                open.Add(i);
            }

            while (remaining > 0 && open.Count > 0)
            {
                long baseShare = remaining / open.Count;
                long extra = remaining % open.Count;
                long used = 0;
                List<int> blocked = new List<int>();

                for (int n = 0; n < open.Count; n++)
                {
                    int index = open[n];
                    long give = baseShare;
                    if (extra > 0 && n >= open.Count - extra)
                        give += 1;

                    long room = caps[index] - assigned[index];
                    if (give > room)
                    {
                        give = room;
                        blocked.Add(index);
                    }

                    assigned[index] += give;
                    used += give;
                }

                if (used <= 0)
                    break;

                remaining -= used;
                if (blocked.Count == 0)
                    break;

                open.RemoveAll(index => caps[index] - assigned[index] <= 0);
            }

            for (int i = 0; i < count; i++)
                shares[i] = FromPaise(assigned[i]);
            return shares;
        }

        public static decimal PerUnitShare(decimal lineShare, int qty)
        {
            if (lineShare <= 0m || qty <= 0)
                return 0m;
            return Round2(lineShare / qty);
        }

        public static decimal CouponShareForUnits(decimal lineCouponShare, int remainingQty, int units)
        {
            if (lineCouponShare <= 0m || remainingQty <= 0 || units <= 0)
                return 0m;
            if (units >= remainingQty)
                return Round2(lineCouponShare);
            return Round2(lineCouponShare / remainingQty * units);
        }

        public static decimal RemainingCouponShare(decimal couponShare, int currentRemaining, int returnNow, int newRemaining)
        {
            if (couponShare <= 0m || newRemaining <= 0 || currentRemaining <= 0)
                return 0m;
            decimal removed = Round2(couponShare / currentRemaining * returnNow);
            decimal left = Round2(couponShare - removed);
            return left < 0m ? 0m : left;
        }

        public readonly struct CouponAdjustedLine
        {
            public decimal DiscountAmount { get; init; }
            public decimal TaxableAmount { get; init; }
            public decimal GstAmount { get; init; }
            public decimal NetAmount { get; init; }
            public decimal CouponShare { get; init; }
        }

        /// <summary>
        /// Adds the line's coupon rupees into its discount and reduces the GST-inclusive net.
        /// </summary>
        public static CouponAdjustedLine AddCouponToLine(
            decimal discountAmount,
            decimal taxableAmount,
            decimal gstAmount,
            decimal netAmount,
            decimal couponShare)
        {
            if (couponShare < 0m)
                couponShare = 0m;
            if (netAmount < 0m)
                netAmount = 0m;
            if (couponShare > netAmount)
                couponShare = netAmount;
            couponShare = Round2(couponShare);

            decimal newNet = Round2(netAmount - couponShare);
            decimal gstPercent = taxableAmount > 0m
                ? (gstAmount / taxableAmount) * 100m
                : 0m;
            decimal newTaxable = gstPercent <= 0m
                ? newNet
                : Round2((newNet * 100m) / (100m + gstPercent));
            decimal newGst = Round2(newNet - newTaxable);
            if (newGst < 0m)
                newGst = 0m;

            return new CouponAdjustedLine
            {
                DiscountAmount = Round2(discountAmount + couponShare),
                TaxableAmount = newTaxable,
                GstAmount = newGst,
                NetAmount = newNet,
                CouponShare = couponShare
            };
        }

        private static decimal Round2(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static long ToPaise(decimal amount)
        {
            if (amount <= 0m)
                return 0;
            return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        }

        private static decimal FromPaise(long paise)
        {
            return paise / 100m;
        }

        public static CouponCheckResult Evaluate(
            string? code,
            bool found,
            DateTime today,
            DateTime validFrom,
            DateTime validTo,
            bool isActive,
            decimal minPurchaseAmount,
            decimal discountAmount,
            decimal grandTotal)
        {
            string normalized = NormalizeCode(code);
            if (string.IsNullOrWhiteSpace(normalized))
                return new CouponCheckResult();

            if (!found)
            {
                return new CouponCheckResult
                {
                    Code = normalized,
                    Found = false,
                    Status = "Invalid",
                    Message = $"Coupon {normalized} not found."
                };
            }

            string status = StatusOn(today, validFrom, validTo, isActive);
            if (status == "Valid" && !IsWithinValidity(today, validFrom, validTo))
                status = CalendarDate(today) < CalendarDate(validFrom) ? "Upcoming" : "Expired";

            if (status != "Valid")
            {
                string window =
                    $"{CalendarDate(validFrom):dd-MMM-yyyy} to {CalendarDate(validTo):dd-MMM-yyyy}";
                string todayText = $"{CalendarDate(today):dd-MMM-yyyy}";
                string message = status switch
                {
                    "Expired" => $"Coupon {normalized} is expired.\nValid: {window}\nToday: {todayText}",
                    "Upcoming" => $"Coupon {normalized} is not valid yet.\nValid: {window}\nToday: {todayText}",
                    "Inactive" => $"Coupon {normalized} is inactive.",
                    _ => $"Coupon {normalized} is not valid.\nValid: {window}\nToday: {todayText}"
                };
                return new CouponCheckResult
                {
                    Found = true,
                    Code = normalized,
                    Status = status,
                    MinPurchaseAmount = minPurchaseAmount,
                    DiscountAmount = discountAmount,
                    Message = message
                };
            }

            if (!MeetsMinimumPurchase(grandTotal, minPurchaseAmount))
            {
                return new CouponCheckResult
                {
                    Found = true,
                    Code = normalized,
                    Status = status,
                    MinPurchaseAmount = minPurchaseAmount,
                    DiscountAmount = discountAmount,
                    Message = $"Coupon {normalized} needs minimum purchase of {minPurchaseAmount:0.00}. Current bill: {grandTotal:0.00}."
                };
            }

            decimal applied = ClampRupeeDiscount(discountAmount, grandTotal);
            return new CouponCheckResult
            {
                Found = true,
                Applied = true,
                Code = normalized,
                Status = status,
                MinPurchaseAmount = minPurchaseAmount,
                DiscountAmount = discountAmount,
                AppliedDiscount = applied
            };
        }

        public static decimal ToAmount(object? value)
        {
            if (value == null || value is DBNull)
                return 0m;
            if (value is decimal decimalValue)
                return decimalValue;
            if (value is double doubleValue)
                return (decimal)doubleValue;
            if (value is float floatValue)
                return (decimal)floatValue;
            if (value is int intValue)
                return intValue;
            if (value is long longValue)
                return longValue;
            if (decimal.TryParse(value.ToString(), out decimal parsed))
                return parsed;
            return 0m;
        }

        public static DateTime ToDate(object? value)
        {
            if (value == null || value is DBNull)
                return DateTime.MinValue;

            if (value is DateTime dateTime)
                return CalendarDate(dateTime);

            if (value is DateTimeOffset dateTimeOffset)
                return dateTimeOffset.Date;

            string text = (value.ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return DateTime.MinValue;

            string[] formats =
            {
                "yyyy-MM-dd",
                "yyyy-MM-dd HH:mm:ss",
                "dd-MM-yyyy",
                "dd/MM/yyyy",
                "dd-MMM-yyyy"
            };

            if (DateTime.TryParseExact(
                text,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime exact))
            {
                return CalendarDate(exact);
            }

            if (DateTime.TryParse(text, new CultureInfo("en-GB"), DateTimeStyles.None, out DateTime british))
                return CalendarDate(british);

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invariant))
                return CalendarDate(invariant);

            if (DateTime.TryParse(text, out DateTime parsed))
                return CalendarDate(parsed);

            return DateTime.MinValue;
        }

        public static DateTime CalendarDate(DateTime value)
        {
            return new DateTime(value.Year, value.Month, value.Day);
        }

        public static bool IsWithinValidity(DateTime today, DateTime validFrom, DateTime validTo)
        {
            DateTime day = CalendarDate(today);
            DateTime from = CalendarDate(validFrom);
            DateTime to = CalendarDate(validTo);
            if (from == DateTime.MinValue || to == DateTime.MinValue)
                return false;
            return day >= from && day <= to;
        }

        public static string StatusOn(DateTime today, DateTime validFrom, DateTime validTo, bool isActive)
        {
            if (!isActive)
                return "Inactive";
            DateTime day = CalendarDate(today);
            DateTime from = CalendarDate(validFrom);
            DateTime to = CalendarDate(validTo);
            if (from == DateTime.MinValue || to == DateTime.MinValue)
                return "Expired";
            if (day < from)
                return "Upcoming";
            if (day > to)
                return "Expired";
            return "Valid";
        }

        public static bool ToBool(object? value)
        {
            if (value == null || value is DBNull)
                return false;
            if (value is bool flag)
                return flag;
            if (value is byte byteValue)
                return byteValue != 0;
            if (value is sbyte sbyteValue)
                return sbyteValue != 0;
            if (value is int intValue)
                return intValue != 0;
            if (value is long longValue)
                return longValue != 0;
            if (value is short shortValue)
                return shortValue != 0;
            string text = value.ToString() ?? "";
            return text == "1"
                || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
