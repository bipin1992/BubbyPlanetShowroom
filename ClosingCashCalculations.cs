using System;
using System.Collections.Generic;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Cash From DB = today's cash bills + return extra/refund that is not
    /// already inside those bills (same-day cash extra is already in grand_total).
    /// </summary>
    public static class ClosingCashCalculations
    {
        public const string OnlineExtraKind = "Online extra";
        public const string OnlineRefundKind = "Online refund";

        public static decimal CombineCashFromDb(decimal todayCashBillTotal, decimal settlementCash)
        {
            return todayCashBillTotal + settlementCash;
        }

        public static bool IsOnlineExtraLine(string kind, decimal amount)
        {
            return amount < 0
                && string.Equals((kind ?? "").Trim(), OnlineExtraKind, StringComparison.OrdinalIgnoreCase);
        }

        public static List<decimal> OnlineExtraAmounts(IEnumerable<ClosingCashBillLine> lines)
        {
            var extras = new List<decimal>();
            if (lines == null)
                return extras;

            foreach (ClosingCashBillLine line in lines)
            {
                if (line != null && IsOnlineExtraLine(line.Kind, line.Amount))
                    extras.Add(-line.Amount);
            }

            return extras;
        }

        /// <summary>
        /// Shop used to Cash-OUT "Online payment" by hand. That extra is now
        /// already deducted from today's cash bills, so the same Cash-OUT
        /// must not subtract again.
        /// </summary>
        public static bool LooksLikeOnlineCashOut(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return false;

            string text = reason.Trim().ToLowerInvariant();
            return text.Contains("onlin")
                || text.Contains("upi")
                || text.Contains("gpay")
                || text.Contains("google pay")
                || text.Contains("phonepe")
                || text.Contains("phone pe")
                || text.Contains("paytm");
        }

        public static bool TryConsumeDuplicateOnlineCashOut(
            decimal amount,
            string reason,
            IList<decimal> remainingOnlineExtras)
        {
            if (amount <= 0 || remainingOnlineExtras == null || remainingOnlineExtras.Count == 0)
                return false;

            if (!LooksLikeOnlineCashOut(reason))
                return false;

            for (int i = 0; i < remainingOnlineExtras.Count; i++)
            {
                if (remainingOnlineExtras[i] == amount)
                {
                    remainingOnlineExtras.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public static decimal CountableCashOut(
            IReadOnlyList<decimal> cashOutAmounts,
            IReadOnlyList<string> cashOutReasons,
            IReadOnlyList<decimal> onlineExtras)
        {
            decimal total = 0;
            var remaining = onlineExtras == null
                ? new List<decimal>()
                : new List<decimal>(onlineExtras);

            int count = cashOutAmounts == null ? 0 : cashOutAmounts.Count;
            for (int i = 0; i < count; i++)
            {
                decimal amount = cashOutAmounts[i];
                string reason = cashOutReasons != null && i < cashOutReasons.Count
                    ? cashOutReasons[i]
                    : "";

                if (TryConsumeDuplicateOnlineCashOut(amount, reason, remaining))
                    continue;

                total += amount;
            }

            return total;
        }

        /// <summary>
        /// Extra collect increases the drawer. Refund decreases it.
        /// Same-day extra on a cash bill is already in grand_total (T99 → T399
        /// bill becomes 339.15, which already includes the 255 extra), so skip it.
        /// Extra on an older bill, or cash extra on today's online bill, is added.
        /// Online extra/refund on today's cash bill is NOT in the drawer: extra
        /// paid online must be taken out of cash From DB; online refund means
        /// cash stayed in the drawer.
        /// </summary>
        public static decimal SettlementEffectOnDrawer(
            string settlementType,
            string settlementPaymentMethod,
            decimal amount,
            DateTime saleDate,
            DateTime? originalBillDate,
            string originalPaymentMethod)
        {
            bool todayCashBill = ExtraAlreadyInTodayCashBills(
                saleDate, originalBillDate, originalPaymentMethod);

            if (todayCashBill)
            {
                if (IsCash(settlementPaymentMethod))
                    return 0;

                string type = (settlementType ?? "").Trim();
                if (string.Equals(type, "collect", StringComparison.OrdinalIgnoreCase))
                    return -amount;
                if (string.Equals(type, "refund", StringComparison.OrdinalIgnoreCase))
                    return amount;
                return 0;
            }

            return SignedCashAmount(settlementType, settlementPaymentMethod, amount);
        }

        public static bool ExtraAlreadyInTodayCashBills(
            DateTime saleDate,
            DateTime? originalBillDate,
            string originalPaymentMethod)
        {
            return originalBillDate.HasValue
                && originalBillDate.Value.Date == saleDate.Date
                && IsCash(originalPaymentMethod);
        }

        /// <summary>
        /// After a same-day cash extra collect, the returned T99 is gone.
        /// Closing lists only the extra (255), not leftover original (84.15).
        /// </summary>
        public static bool HideSaleLineWhenExtraCollected(decimal sameDayCashExtraNet)
        {
            return sameDayCashExtraNet > 0;
        }

        /// <summary>
        /// Cash contribution for one of today's cash bills.
        /// Extra collect after return → only extra. Otherwise the bill total.
        /// </summary>
        public static decimal CashBillContribution(decimal grandTotal, decimal sameDayCashExtraNet)
        {
            if (sameDayCashExtraNet > 0)
                return sameDayCashExtraNet;

            return grandTotal;
        }

        public static decimal SignedCashAmount(
            string settlementType,
            string paymentMethod,
            decimal amount)
        {
            if (!IsCash(paymentMethod))
                return 0;

            if (string.Equals((settlementType ?? "").Trim(), "collect", StringComparison.OrdinalIgnoreCase))
                return amount;

            if (string.Equals((settlementType ?? "").Trim(), "refund", StringComparison.OrdinalIgnoreCase))
                return -amount;

            return 0;
        }

        public static bool IsCash(string paymentMethod)
        {
            return string.Equals(
                string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                "Cash",
                StringComparison.OrdinalIgnoreCase);
        }

        public static decimal LateCashDelta(decimal savedOrShownCashSales, decimal liveCashSales)
        {
            return liveCashSales - savedOrShownCashSales;
        }

        public static decimal CounterTotal(decimal opening, decimal cashSales, decimal cashIn = 0, decimal cashOut = 0)
        {
            return opening + cashSales + cashIn - cashOut;
        }

        public static decimal ActualDifference(decimal counterCash, decimal leftover, decimal ownerCash)
        {
            return (counterCash - leftover) - ownerCash;
        }

        /// <summary>
        /// Empty owner + leftover means the page is not filled yet.
        /// Do not show the whole counter as a shortage.
        /// </summary>
        public static decimal ShownDifference(decimal counterCash, decimal leftover, decimal ownerCash)
        {
            if (leftover == 0 && ownerCash == 0)
                return 0;

            return ActualDifference(counterCash, leftover, ownerCash);
        }
    }
}
