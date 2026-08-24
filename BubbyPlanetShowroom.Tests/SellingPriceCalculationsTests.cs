using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class SellingPriceCalculationsTests
    {
        private static PricingSettings ExampleSettings()
        {
            return PricingSettings.CreateDefaults();
        }

        // ---------- RoundUpToEndingDigit (plan §9) ----------

        [Theory]
        [InlineData(35.50, 39)]
        [InlineData(42.35, 49)]
        [InlineData(101, 109)]
        [InlineData(126, 129)]
        [InlineData(203, 209)]
        [InlineData(907.06, 909)]
        [InlineData(1123, 1129)]
        public void RoundUpToEndingDigit_PlanExamples(decimal input, decimal expected)
        {
            Assert.Equal(expected, SellingPriceCalculations.RoundUpToEndingDigit(input, 9));
        }

        [Fact]
        public void RoundUpToEndingDigit_KeepsExactNine()
        {
            Assert.Equal(39m, SellingPriceCalculations.RoundUpToEndingDigit(39m, 9));
        }

        [Fact]
        public void RoundUpToEndingDigit_JustAboveNineGoesToNext()
        {
            Assert.Equal(49m, SellingPriceCalculations.RoundUpToEndingDigit(39.01m, 9));
        }

        // ---------- Profit margin slabs (plan §15) ----------

        [Theory]
        [InlineData(0, 50)]
        [InlineData(499.99, 50)]
        [InlineData(500, 45)]
        [InlineData(999.99, 45)]
        [InlineData(1000, 40)]
        [InlineData(1499.99, 40)]
        [InlineData(1500, 35)]
        [InlineData(1999.99, 35)]
        [InlineData(2000, 30)]
        [InlineData(5000, 30)]
        public void GetMarginPercent_MatchesDefaultSlabs(decimal purchasePerPiece, decimal expectedMargin)
        {
            decimal margin = SellingPriceCalculations.GetMarginPercent(
                purchasePerPiece,
                PricingSettings.CreateDefaultSlabs());
            Assert.Equal(expectedMargin, margin);
        }

        // ---------- Complete example: quantity 1 (plan §11) ----------

        [Fact]
        public void Calculate_Quantity1_PlanExample()
        {
            SellingPriceResult result = SellingPriceCalculations.Calculate(
                totalPurchaseCost: 500m,
                quantity: 1,
                totalTransportCost: 1m,
                totalParcelQuantity: 1,
                settings: ExampleSettings());

            Assert.Equal(500.00m, result.PurchaseCostPerPiece);
            Assert.Equal(45m, result.ProfitMarginPercent);
            Assert.Equal(225.00m, result.ProfitPerPiece);
            Assert.Equal(1.00m, result.TransportPerPiece);
            Assert.Equal(10.00m, result.RentPerPiece);
            Assert.Equal(10.00m, result.SalaryPerPiece);
            Assert.Equal(746.00m, result.RequiredNetPrice);
            Assert.Equal(877.65m, result.PriceBeforeRounding);
            Assert.Equal(879m, result.FinalSellingPrice);
            Assert.Equal(131.85m, result.DiscountAmount);
            Assert.Equal(747.15m, result.CustomerPayable);
            Assert.Equal(521.00m, result.ActualTotalCost);
            Assert.Equal(226.15m, result.ActualProfit);
        }

        // ---------- Complete example: quantity 10 (plan §12) ----------

        [Fact]
        public void Calculate_Quantity10_PlanExample()
        {
            // Transport ₹1 per piece: ₹30 total / 30 parcel pieces, or ₹1 / 1.
            SellingPriceResult result = SellingPriceCalculations.Calculate(
                totalPurchaseCost: 100m,
                quantity: 10,
                totalTransportCost: 1m,
                totalParcelQuantity: 1,
                settings: ExampleSettings());

            Assert.Equal(10.00m, result.PurchaseCostPerPiece);
            Assert.Equal(50m, result.ProfitMarginPercent);
            Assert.Equal(5.00m, result.ProfitPerPiece);
            Assert.Equal(1.00m, result.TransportPerPiece);
            Assert.Equal(10.00m, result.RentPerPiece);
            Assert.Equal(10.00m, result.SalaryPerPiece);
            Assert.Equal(36.00m, result.RequiredNetPrice);
            Assert.Equal(42.35m, result.PriceBeforeRounding);
            Assert.Equal(49m, result.FinalSellingPrice);
            Assert.Equal(7.35m, result.DiscountAmount);
            Assert.Equal(41.65m, result.CustomerPayable);
            Assert.Equal(31.00m, result.ActualTotalCost);
            Assert.Equal(10.65m, result.ActualProfit);
        }

        [Fact]
        public void Calculate_TransportAllocatedByParcelQuantity_NotItemCost()
        {
            // Plan §2: ₹3000 / 30 pieces = ₹100 each; item qty 6 does not change per-piece transport.
            SellingPriceResult result = SellingPriceCalculations.Calculate(
                totalPurchaseCost: 100m,
                quantity: 6,
                totalTransportCost: 3000m,
                totalParcelQuantity: 30,
                settings: ExampleSettings());

            Assert.Equal(100.00m, result.TransportPerPiece);
            Assert.Equal(16.67m, result.PurchaseCostPerPiece);
        }

        [Fact]
        public void Calculate_ProfitAppliesOnlyToPurchaseCost()
        {
            SellingPriceResult result = SellingPriceCalculations.Calculate(
                totalPurchaseCost: 100m,
                quantity: 1,
                totalTransportCost: 1000m,
                totalParcelQuantity: 1,
                settings: ExampleSettings());

            // 50% of ₹100 purchase — not of transport/rent/salary.
            Assert.Equal(50.00m, result.ProfitPerPiece);
            Assert.Equal(50m, result.ProfitMarginPercent);
        }

        [Fact]
        public void Calculate_RejectsZeroQuantity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SellingPriceCalculations.Calculate(100m, 0, 0m, 1, ExampleSettings()));
        }

        [Fact]
        public void Calculate_RejectsZeroParcelQuantity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SellingPriceCalculations.Calculate(100m, 1, 0m, 0, ExampleSettings()));
        }
    }
}
