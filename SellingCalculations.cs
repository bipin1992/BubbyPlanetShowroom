using System;
using System.Collections.Generic;
using System.Globalization;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Selling Total Sale is billed sales only — same as Revenue.
    /// Return extra is listed on Selling for cash tracking; it never
    /// replaces a bill and never changes Total Sale.
    /// </summary>
    public static class SellingCalculations
    {
        public static decimal BilledSaleTotal(decimal orderSaleInPeriod)
        {
            return orderSaleInPeriod;
        }

        public static decimal CombineTotalSale(decimal orderSaleInPeriod, decimal extraInPeriod)
        {
            _ = extraInPeriod;
            return BilledSaleTotal(orderSaleInPeriod);
        }

        public static bool IncludeOrderSaleInTotal(bool hasCollectExtraInPeriod)
        {
            return true;
        }

        public static bool ExtraCountsInTotalSale(
            DateTime periodFrom,
            DateTime periodTo,
            DateTime? originalBillDate)
        {
            return false;
        }

        /// <summary>
        /// Last 7 Days matches Revenue: DATE_SUB(CURDATE(), INTERVAL 7 DAY)
        /// through today. Weekly is Monday–Sunday.
        /// </summary>
        public static void ResolveFilterRange(
            string filter,
            DateTime today,
            DateTime customFrom,
            DateTime customTo,
            out DateTime from,
            out DateTime to)
        {
            today = today.Date;
            customFrom = customFrom.Date;
            customTo = customTo.Date;

            switch ((filter ?? "").Trim())
            {
                case "Last 7 Days":
                    from = today.AddDays(-7);
                    to = today;
                    break;
                case "Weekly":
                    int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
                    from = today.AddDays(-daysFromMonday);
                    to = from.AddDays(6);
                    break;
                case "Monthly":
                    from = new DateTime(today.Year, today.Month, 1);
                    to = from.AddMonths(1).AddDays(-1);
                    break;
                case "Yearly":
                    from = new DateTime(today.Year, 1, 1);
                    to = new DateTime(today.Year, 12, 31);
                    break;
                case "Custom":
                    from = customFrom;
                    to = customTo;
                    break;
                default:
                    from = today;
                    to = today;
                    break;
            }

            if (from > to)
            {
                DateTime swap = from;
                from = to;
                to = swap;
            }
        }

        public static bool BillDateInRange(DateTime billDate, DateTime from, DateTime to)
        {
            DateTime day = billDate.Date;
            return day >= from.Date && day <= to.Date;
        }

        public static DateTime RangeEndExclusive(DateTime toInclusive)
        {
            return toInclusive.Date.AddDays(1);
        }

        /// <summary>
        /// Includes every bill from the start of <paramref name="fromInclusive"/>
        /// through the last moment of <paramref name="toInclusive"/>.
        /// </summary>
        public static bool BillDateTimeInFilter(DateTime billAt, DateTime fromInclusive, DateTime toInclusive)
        {
            DateTime start = fromInclusive.Date;
            DateTime endExclusive = RangeEndExclusive(toInclusive);
            return billAt >= start && billAt < endExclusive;
        }

        public static string SqlDateTimeInFilter(string dateColumn)
        {
            return dateColumn + " >= @fromDate AND " + dateColumn + " < @toDateExclusive";
        }

        /// <summary>
        /// Item percents on the bill, never a blended average like 11.1%.
        /// </summary>
        public static string FormatDiscountPercentLabel(string? csvPercents)
        {
            var unique = new SortedSet<decimal>();
            if (!string.IsNullOrWhiteSpace(csvPercents))
            {
                foreach (string part in csvPercents.Split(new[] { ',', '|', '/' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string token = part.Trim().TrimEnd('%').Trim();
                    if (decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal pct))
                        unique.Add(Math.Round(Math.Max(0, pct), 1, MidpointRounding.AwayFromZero));
                }
            }

            if (unique.Count == 0)
                unique.Add(0);

            var labels = new List<string>();
            foreach (decimal pct in unique)
            {
                labels.Add(pct == decimal.Truncate(pct)
                    ? pct.ToString("0", CultureInfo.InvariantCulture) + "%"
                    : pct.ToString("0.0", CultureInfo.InvariantCulture) + "%");
            }

            return string.Join(" / ", labels);
        }
    }
}
