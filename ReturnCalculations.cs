using System;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Pure return amount math used by Return UI / process flow.
    /// Kept UI-free so unit tests can lock refund + remaining totals.
    /// </summary>
    public static class ReturnCalculations
    {
        public static decimal Round2(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Refund for returning <paramref name="returnQty"/> units when
        /// <paramref name="netAmount"/> is the amount still left on the line
        /// for <c>qty - returned</c> remaining units.
        /// </summary>
        public static decimal CalculateLineRefund(int qty, int returned, decimal netAmount, int returnQty)
        {
            if (returnQty <= 0)
                return 0;

            int remaining = qty - returned;
            if (remaining <= 0)
                return 0;

            if (returnQty > remaining)
                return 0;

            return Round2(netAmount / remaining * returnQty);
        }

        public static bool CanReturn(int qty, int returnedAlready, int returnNow)
        {
            if (returnNow <= 0)
                return false;

            if (qty <= 0)
                return false;

            return returnedAlready + returnNow <= qty;
        }

        public static int RemainingQty(int qty, int returned)
        {
            int remaining = qty - returned;
            return remaining < 0 ? 0 : remaining;
        }

        /// <summary>
        /// Applies a return to one order-detail line: scales remaining amounts
        /// and returns the refund for the units being returned now.
        /// Remaining amounts are derived as (current - refunded portion) so
        /// refund + newRemaining always equals the pre-return line amount.
        /// </summary>
        public static ReturnLineResult ApplyReturn(
            int qty,
            int returnedAlready,
            int returnNow,
            decimal grossAmount,
            decimal discountAmount,
            decimal taxableAmount,
            decimal gstAmount,
            decimal netAmount)
        {
            if (!CanReturn(qty, returnedAlready, returnNow))
                throw new InvalidOperationException("Return quantity exceeds remaining quantity.");

            int currentRemaining = qty - returnedAlready;
            int newReturnQty = returnedAlready + returnNow;
            int newRemaining = qty - newReturnQty;

            // Full remaining return: zero the line, refund exact leftover amounts.
            if (newRemaining == 0)
            {
                return new ReturnLineResult
                {
                    NewReturnQty = newReturnQty,
                    NewRemainingQty = 0,
                    Refund = Round2(netAmount),
                    NewGrossAmount = 0,
                    NewDiscountAmount = 0,
                    NewTaxableAmount = 0,
                    NewGstAmount = 0,
                    NewNetAmount = 0
                };
            }

            decimal refund = Round2(netAmount / currentRemaining * returnNow);
            decimal refundGross = Round2(grossAmount / currentRemaining * returnNow);
            decimal refundDiscount = Round2(discountAmount / currentRemaining * returnNow);
            decimal refundTaxable = Round2(taxableAmount / currentRemaining * returnNow);
            decimal refundGst = Round2(gstAmount / currentRemaining * returnNow);

            return new ReturnLineResult
            {
                NewReturnQty = newReturnQty,
                NewRemainingQty = newRemaining,
                Refund = refund,
                NewGrossAmount = Round2(grossAmount - refundGross),
                NewDiscountAmount = Round2(discountAmount - refundDiscount),
                NewTaxableAmount = Round2(taxableAmount - refundTaxable),
                NewGstAmount = Round2(gstAmount - refundGst),
                NewNetAmount = Round2(netAmount - refund)
            };
        }

        /// <summary>
        /// After successive partial returns, refunds + final remaining net
        /// must equal the original net (within 1 paisa * number of returns rounding budget).
        /// </summary>
        public static decimal SumRounded(params decimal[] values)
        {
            decimal total = 0;
            foreach (decimal value in values)
                total += value;
            return Round2(total);
        }
    }

    public sealed class ReturnLineResult
    {
        public int NewReturnQty { get; init; }
        public int NewRemainingQty { get; init; }
        public decimal Refund { get; init; }
        public decimal NewGrossAmount { get; init; }
        public decimal NewDiscountAmount { get; init; }
        public decimal NewTaxableAmount { get; init; }
        public decimal NewGstAmount { get; init; }
        public decimal NewNetAmount { get; init; }
    }
}
