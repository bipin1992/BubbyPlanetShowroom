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

        /// <summary>
        /// Price of the pieces already returned. While some units remain,
        /// the leftover net is split by quantity. A fully returned line
        /// has its amounts zeroed, so the value is rebuilt from price and discount.
        /// </summary>
        public static decimal ReturnedPiecesValue(
            decimal sellingPrice,
            decimal gstPercent,
            decimal discountPercent,
            int qty,
            int returnedQty,
            decimal remainingNet)
        {
            if (returnedQty <= 0 || qty <= 0)
                return 0;

            if (returnedQty > qty)
                returnedQty = qty;

            int remaining = qty - returnedQty;
            if (remaining > 0)
                return Round2(remainingNet / remaining * returnedQty);

            CalculateLineAmounts(
                sellingPrice,
                gstPercent,
                discountPercent,
                returnedQty,
                out _,
                out _,
                out _,
                out decimal net);
            return net;
        }

        /// <summary>
        /// Price of the item taken in exchange. Leftover net plus any
        /// part of that same line that was returned later.
        /// </summary>
        public static decimal ExchangeTakenValue(
            decimal sellingPrice,
            decimal gstPercent,
            decimal discountPercent,
            int qty,
            int returnedQty,
            decimal remainingNet)
        {
            if (qty <= 0)
                return 0;

            decimal returnedPart = ReturnedPiecesValue(
                sellingPrice, gstPercent, discountPercent, qty, returnedQty, remainingNet);
            if (remainingNet < 0)
                remainingNet = 0;
            return Round2(remainingNet + returnedPart);
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

            decimal newNet = Round2(netAmount - refund);
            decimal newGross = Round2(grossAmount - refundGross);
            decimal newDiscount = Round2(discountAmount - refundDiscount);
            decimal newTaxable = Round2(taxableAmount - refundTaxable);
            // Keep GST as residual so taxable + gst always equals net (no paisa drift).
            decimal newGst = Round2(newNet - newTaxable);

            return new ReturnLineResult
            {
                NewReturnQty = newReturnQty,
                NewRemainingQty = newRemaining,
                Refund = refund,
                NewGrossAmount = newGross,
                NewDiscountAmount = newDiscount,
                NewTaxableAmount = newTaxable,
                NewGstAmount = newGst,
                NewNetAmount = newNet
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

        /// <summary>
        /// Mall exchange rule: new items must be equal or higher than return value.
        /// Balance due is what customer pays (new - return). Cash refund is 0 on exchange.
        /// </summary>
        public static decimal RefundAfterCoupon(decimal lineRefund, decimal couponShare)
        {
            decimal refund = Round2(lineRefund) - Round2(Math.Max(0, couponShare));
            return refund < 0 ? 0 : refund;
        }

        /// <summary>
        /// Splits remaining bill-level coupon across the line nets being
        /// returned now. Full remaining-bill return takes the leftover coupon
        /// so paisa does not stick on the order.
        /// </summary>
        public static decimal[] AllocateCouponShares(
            decimal remainingCoupon,
            decimal remainingItemsNet,
            decimal[] returnedLineNets)
        {
            int count = returnedLineNets?.Length ?? 0;
            decimal[] shares = new decimal[count];
            if (count == 0 || remainingCoupon <= 0 || remainingItemsNet <= 0)
                return shares;

            decimal totalReturned = 0;
            for (int i = 0; i < count; i++)
                totalReturned += Math.Max(0, returnedLineNets[i]);
            totalReturned = Round2(totalReturned);
            if (totalReturned <= 0)
                return shares;

            bool returningAllRemaining = totalReturned >= remainingItemsNet - 0.009m;
            decimal couponToAllocate = returningAllRemaining
                ? remainingCoupon
                : Round2(remainingCoupon * (totalReturned / remainingItemsNet));

            if (couponToAllocate > remainingCoupon)
                couponToAllocate = remainingCoupon;
            if (couponToAllocate > totalReturned)
                couponToAllocate = totalReturned;

            decimal assigned = 0;
            for (int i = 0; i < count; i++)
            {
                decimal lineNet = Math.Max(0, returnedLineNets[i]);
                if (i == count - 1)
                {
                    decimal last = Round2(couponToAllocate - assigned);
                    if (last < 0) last = 0;
                    if (last > lineNet) last = lineNet;
                    shares[i] = last;
                    break;
                }

                decimal share = totalReturned <= 0
                    ? 0
                    : Round2(couponToAllocate * (lineNet / totalReturned));
                if (share > lineNet)
                    share = lineNet;
                shares[i] = share;
                assigned += share;
            }

            return shares;
        }

        public static decimal RemainingCouponAfterReturn(decimal remainingCoupon, decimal couponShareReturned)
        {
            decimal left = Round2(remainingCoupon) - Round2(Math.Max(0, couponShareReturned));
            return left < 0 ? 0 : left;
        }

        /// <summary>
        /// Exchange keeps the bill-level coupon on whatever is left
        /// (remaining original items + new items). Never more than remaining nets.
        /// </summary>
        public static decimal CouponOnRemainingBill(decimal remainingCoupon, decimal remainingItemsNet)
        {
            remainingCoupon = Round2(Math.Max(0, remainingCoupon));
            remainingItemsNet = Round2(Math.Max(0, remainingItemsNet));
            return remainingCoupon > remainingItemsNet ? remainingItemsNet : remainingCoupon;
        }

        public static ExchangeSummary CalculateExchange(decimal returnValue, decimal newItemsValue)
        {
            returnValue = Round2(Math.Max(0, returnValue));
            newItemsValue = Round2(Math.Max(0, newItemsValue));

            decimal balanceDue = Round2(Math.Max(0, newItemsValue - returnValue));
            decimal shortfall = Round2(Math.Max(0, returnValue - newItemsValue));

            return new ExchangeSummary
            {
                ReturnValue = returnValue,
                NewItemsValue = newItemsValue,
                BalanceDue = balanceDue,
                Shortfall = shortfall,
                IsValid = shortfall <= 0 && (returnValue > 0 || newItemsValue > 0),
                MeetsEqualOrMoreRule = shortfall <= 0 && returnValue > 0 && newItemsValue > 0
            };
        }

        public static void CalculateLineAmounts(
            decimal sellingPrice,
            decimal gstPercent,
            decimal discountPercent,
            int qty,
            out decimal taxable,
            out decimal gstAmount,
            out decimal gross,
            out decimal net)
        {
            if (discountPercent < 0) discountPercent = 0;
            if (discountPercent > 100) discountPercent = 100;
            if (qty < 0) qty = 0;
            if (gstPercent < 0) gstPercent = 0;

            decimal discountAmountPerUnit = (sellingPrice * discountPercent) / 100m;
            decimal netAmountPerUnit = sellingPrice - discountAmountPerUnit;
            net = Round2(netAmountPerUnit * qty);
            gross = Round2(sellingPrice * qty);
            taxable = gstPercent <= 0
                ? net
                : Round2((net * 100m) / (100m + gstPercent));
            gstAmount = Round2(net - taxable);
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

    public sealed class ExchangeSummary
    {
        public decimal ReturnValue { get; init; }
        public decimal NewItemsValue { get; init; }
        public decimal BalanceDue { get; init; }
        public decimal Shortfall { get; init; }
        public bool IsValid { get; init; }
        public bool MeetsEqualOrMoreRule { get; init; }
    }
}
