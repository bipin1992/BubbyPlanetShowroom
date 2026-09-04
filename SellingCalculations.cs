using System;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// After a same-day return extra, T99 is gone — Total Sale is only the extra.
    /// Bills without extra still count at grand_total. Extra on an older bill
    /// is added because that bill is not in today's list.
    /// </summary>
    public static class SellingCalculations
    {
        public static decimal CombineTotalSale(decimal orderSaleInPeriod, decimal extraInPeriod)
        {
            return orderSaleInPeriod + extraInPeriod;
        }

        public static bool IncludeOrderSaleInTotal(bool hasCollectExtraInPeriod)
        {
            return !hasCollectExtraInPeriod;
        }

        public static bool ExtraCountsInTotalSale(
            DateTime periodFrom,
            DateTime periodTo,
            DateTime? originalBillDate)
        {
            return true;
        }
    }
}
