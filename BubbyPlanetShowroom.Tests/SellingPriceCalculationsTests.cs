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
        [InlineData(0, 45)]
        [InlineData(499.99, 45)]
        [InlineData(500, 40)]
        [InlineData(999.99, 40)]
        [InlineData(1000, 35)]
        [InlineData(1499.99, 35)]
        [InlineData(1500, 30)]
        [InlineData(1999.99, 30)]
        [InlineData(2000, 25)]
        [InlineData(5000, 25)]
        public void GetMarginPercent_MatchesDefaultSlabs(decimal purchasePerPiece, decimal expectedMargin)
        {
            decimal margin = SellingPriceCalculations.GetMarginPercent(
                purchasePerPiece,
                PricingSettings.CreateDefaultSlabs());
            Assert.Equal(expectedMargin, margin);
        }

        // ---------- Per piece rate is not divided by quantity ----------

        [Fact]
        public void Calculate_PerPieceRate_NotDividedByQuantity()
        {
            SellingPriceResult result = SellingPriceCalculations.Calculate(
                perPieceRate: 500m,
                quantity: 1,
                settings: ExampleSettings());

            Assert.Equal(500.00m, result.PurchaseCostPerPiece);
            Assert.Equal(40m, result.ProfitMarginPercent);
            Assert.Equal(200.00m, result.ProfitPerPiece);
            Assert.Equal(0m, result.TransportPerPiece);
            Assert.Equal(10.00m, result.RentPerPiece);
            Assert.Equal(10.00m, result.SalaryPerPiece);
            Assert.Equal(720.00m, result.RequiredNetPrice);
            Assert.Equal(720.00m, result.PriceBeforeRounding);
            Assert.Equal(729m, result.FinalSellingPrice);
            Assert.Equal(0m, result.DiscountAmount);
            Assert.Equal(729m, result.CustomerPayable);
            Assert.Equal(520.00m, result.ActualTotalCost);
            Assert.Equal(209.00m, result.ActualProfit);
        }

        [Fact]
        public void Calculate_ItemTransportSplitByQuantity()
        {
            // Transport ₹100 for this item, 10 pieces, cost rate ₹50 / piece.
            SellingPriceResult result = SellingPriceCalculations.Calculate(
                perPieceRate: 50m,
                quantity: 10,
                settings: ExampleSettings(),
                itemTransportCost: 100m);

            Assert.Equal(50.00m, result.PurchaseCostPerPiece);
            Assert.Equal(45m, result.ProfitMarginPercent);
            Assert.Equal(22.50m, result.ProfitPerPiece);
            Assert.Equal(100.00m, result.TotalTransportCost);
            Assert.Equal(10.00m, result.TransportPerPiece);
            Assert.Equal(10.00m, result.RentPerPiece);
            Assert.Equal(10.00m, result.SalaryPerPiece);
            Assert.Equal(102.50m, result.RequiredNetPrice);
            Assert.Equal(109m, result.FinalSellingPrice);
            Assert.Equal(80.00m, result.ActualTotalCost);
            Assert.Equal(29.00m, result.ActualProfit);
        }

        [Fact]
        public void Calculate_IgnoresTransportInSettings_UsesItemTransport()
        {
            PricingSettings settings = ExampleSettings();
            settings.TotalTransportCost = 3000m;
            settings.TotalParcelQuantity = 30;

            SellingPriceResult result = SellingPriceCalculations.Calculate(
                perPieceRate: 100m,
                quantity: 10,
                settings: settings,
                itemTransportCost: 50m);

            Assert.Equal(100.00m, result.PurchaseCostPerPiece);
            Assert.Equal(5.00m, result.TransportPerPiece);
            Assert.Equal(50.00m, result.TotalTransportCost);
        }

        [Fact]
        public void Calculate_ProfitAppliesOnlyToPurchaseCost()
        {
            SellingPriceResult result = SellingPriceCalculations.Calculate(
                perPieceRate: 100m,
                quantity: 1,
                settings: ExampleSettings());

            // 45% of ₹100 purchase — not of rent/salary.
            Assert.Equal(45.00m, result.ProfitPerPiece);
            Assert.Equal(45m, result.ProfitMarginPercent);
        }

        [Fact]
        public void Calculate_RejectsZeroQuantity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SellingPriceCalculations.Calculate(100m, 0, ExampleSettings()));
        }
    }
}
