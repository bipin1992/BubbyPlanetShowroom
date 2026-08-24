using System;
using System.Collections.Generic;
using System.Linq;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Business settings for marked-price calculation. Loaded from DB so values
    /// are not hard-coded in the calculator.
    /// </summary>
    public sealed class PricingSettings
    {
        public decimal MonthlyRent { get; set; } = 30000m;
        public decimal MonthlySalary { get; set; } = 30000m;
        public decimal ExpectedMonthlySales { get; set; } = 3000m;
        public decimal DiscountPercent { get; set; } = 15m;
        public int PriceEndingDigit { get; set; } = 9;
        public List<ProfitMarginSlab> Slabs { get; set; } = CreateDefaultSlabs();

        public static List<ProfitMarginSlab> CreateDefaultSlabs()
        {
            return new List<ProfitMarginSlab>
            {
                new ProfitMarginSlab { MinPurchaseCost = 0m, MaxPurchaseCost = 500m, MarginPercent = 50m },
                new ProfitMarginSlab { MinPurchaseCost = 500m, MaxPurchaseCost = 1000m, MarginPercent = 45m },
                new ProfitMarginSlab { MinPurchaseCost = 1000m, MaxPurchaseCost = 1500m, MarginPercent = 40m },
                new ProfitMarginSlab { MinPurchaseCost = 1500m, MaxPurchaseCost = 2000m, MarginPercent = 35m },
                new ProfitMarginSlab { MinPurchaseCost = 2000m, MaxPurchaseCost = null, MarginPercent = 30m }
            };
        }

        public static PricingSettings CreateDefaults()
        {
            return new PricingSettings();
        }
    }

    /// <summary>
    /// Profit % applied only to purchase cost per piece.
    /// Min is inclusive; Max is exclusive. Null Max means no upper bound.
    /// </summary>
    public sealed class ProfitMarginSlab
    {
        public decimal MinPurchaseCost { get; set; }
        public decimal? MaxPurchaseCost { get; set; }
        public decimal MarginPercent { get; set; }
    }

    public sealed class SellingPriceResult
    {
        public decimal PurchaseCostPerPiece { get; init; }
        public decimal TransportPerPiece { get; init; }
        public decimal RentPerPiece { get; init; }
        public decimal SalaryPerPiece { get; init; }
        public decimal ProfitMarginPercent { get; init; }
        public decimal ProfitPerPiece { get; init; }
        public decimal RequiredNetPrice { get; init; }
        public decimal DiscountKeepRatio { get; init; }
        public decimal PriceBeforeRounding { get; init; }
        public decimal FinalSellingPrice { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal CustomerPayable { get; init; }
        public decimal ActualTotalCost { get; init; }
        public decimal ActualProfit { get; init; }
    }

    /// <summary>
    /// Pure selling-price math. UI-free so unit tests can lock the business examples.
    /// Profit margin is applied only to purchase cost per piece — never to
    /// transport, rent, or salary.
    /// </summary>
    public static class SellingPriceCalculations
    {
        public static decimal Round2(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Rounds up to the next higher price whose last digit is
        /// <paramref name="endingDigit"/>. An exact integer that already ends
        /// on that digit is kept (e.g. 39.00 → 39).
        /// </summary>
        public static decimal RoundUpToEndingDigit(decimal price, int endingDigit)
        {
            if (endingDigit < 0 || endingDigit > 9)
                throw new ArgumentOutOfRangeException(nameof(endingDigit), "Price ending digit must be 0–9.");

            if (price <= 0m)
                return endingDigit == 0 ? 10m : endingDigit;

            decimal n = Math.Ceiling(price);
            int lastDigit = (int)(n % 10m);
            int add = (endingDigit - lastDigit + 10) % 10;
            return n + add;
        }

        public static decimal GetMarginPercent(decimal purchaseCostPerPiece, IReadOnlyList<ProfitMarginSlab> slabs)
        {
            if (slabs == null || slabs.Count == 0)
                throw new InvalidOperationException("At least one profit-margin slab is required.");

            foreach (ProfitMarginSlab slab in slabs.OrderBy(s => s.MinPurchaseCost))
            {
                bool minOk = purchaseCostPerPiece >= slab.MinPurchaseCost;
                bool maxOk = !slab.MaxPurchaseCost.HasValue || purchaseCostPerPiece < slab.MaxPurchaseCost.Value;
                if (minOk && maxOk)
                    return slab.MarginPercent;
            }

            return slabs.OrderBy(s => s.MinPurchaseCost).Last().MarginPercent;
        }

        public static SellingPriceResult Calculate(
            decimal totalPurchaseCost,
            int quantity,
            decimal totalTransportCost,
            int totalParcelQuantity,
            PricingSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (totalPurchaseCost < 0m)
                throw new ArgumentOutOfRangeException(nameof(totalPurchaseCost), "Purchase cost cannot be negative.");
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");
            if (totalTransportCost < 0m)
                throw new ArgumentOutOfRangeException(nameof(totalTransportCost), "Transport cost cannot be negative.");
            if (totalParcelQuantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalParcelQuantity), "Parcel quantity must be at least 1.");
            if (settings.ExpectedMonthlySales <= 0m)
                throw new InvalidOperationException("Expected monthly sales must be greater than 0.");
            if (settings.DiscountPercent < 0m || settings.DiscountPercent >= 100m)
                throw new InvalidOperationException("Discount percent must be 0 or more and less than 100.");
            if (settings.MonthlyRent < 0m || settings.MonthlySalary < 0m)
                throw new InvalidOperationException("Monthly rent and salary cannot be negative.");

            decimal purchasePerPiece = totalPurchaseCost / quantity;
            decimal transportPerPiece = totalTransportCost / totalParcelQuantity;
            decimal rentPerPiece = settings.MonthlyRent / settings.ExpectedMonthlySales;
            decimal salaryPerPiece = settings.MonthlySalary / settings.ExpectedMonthlySales;

            decimal marginPercent = GetMarginPercent(purchasePerPiece, settings.Slabs);
            decimal profitPerPiece = purchasePerPiece * (marginPercent / 100m);

            decimal requiredNet =
                purchasePerPiece
                + profitPerPiece
                + transportPerPiece
                + rentPerPiece
                + salaryPerPiece;

            decimal keepRatio = (100m - settings.DiscountPercent) / 100m;
            decimal priceBeforeRounding = requiredNet / keepRatio;
            decimal finalPrice = RoundUpToEndingDigit(priceBeforeRounding, settings.PriceEndingDigit);

            decimal discountAmount = Round2(finalPrice * settings.DiscountPercent / 100m);
            decimal customerPayable = Round2(finalPrice - discountAmount);
            decimal actualTotalCost = purchasePerPiece + transportPerPiece + rentPerPiece + salaryPerPiece;
            decimal actualProfit = customerPayable - actualTotalCost;

            return new SellingPriceResult
            {
                PurchaseCostPerPiece = Round2(purchasePerPiece),
                TransportPerPiece = Round2(transportPerPiece),
                RentPerPiece = Round2(rentPerPiece),
                SalaryPerPiece = Round2(salaryPerPiece),
                ProfitMarginPercent = marginPercent,
                ProfitPerPiece = Round2(profitPerPiece),
                RequiredNetPrice = Round2(requiredNet),
                DiscountKeepRatio = keepRatio,
                PriceBeforeRounding = Round2(priceBeforeRounding),
                FinalSellingPrice = finalPrice,
                DiscountAmount = discountAmount,
                CustomerPayable = customerPayable,
                ActualTotalCost = Round2(actualTotalCost),
                ActualProfit = Round2(actualProfit)
            };
        }
    }
}
