using System.Globalization;
using System.Threading;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class ReceiptCalculationsTests
    {
        public ReceiptCalculationsTests()
        {
            var culture = CultureInfo.GetCultureInfo("en-IN");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }

        [Fact]
        public void MembershipName_ShowsTierAndThreshold()
        {
            Assert.Equal(
                "BRONZE (₹2,000+, 2% off)",
                ReceiptCalculations.FormatMembershipLabel("BRONZE", 2000m, 2m));
        }

        [Fact]
        public void EmptyMembershipName_KeepsOldThresholdLabel()
        {
            Assert.Equal(
                "₹2,000+ (2% off)",
                ReceiptCalculations.FormatMembershipLabel("", 2000m, 2m));
            Assert.Equal(
                "₹3,000+ (3% off)",
                ReceiptCalculations.FormatMembershipLabel("   ", 3000m, 3m));
            Assert.Equal(
                "₹5,000+ (5% off)",
                ReceiptCalculations.FormatMembershipLabel(null, 5000m, 5m));
        }

        [Theory]
        [InlineData("SILVER", 3000, 3, "SILVER (₹3,000+, 3% off)")]
        [InlineData("GOLD", 5000, 5, "GOLD (₹5,000+, 5% off)")]
        [InlineData("PLATINUM", 10000, 7, "PLATINUM (₹10,000+, 7% off)")]
        [InlineData("DIAMOND", 15000, 10, "DIAMOND (₹15,000+, 10% off)")]
        public void ShopRewardTiers_MatchDumpRules(
            string name,
            int minPurchase,
            int percent,
            string expected)
        {
            Assert.Equal(
                expected,
                ReceiptCalculations.FormatMembershipLabel(name, minPurchase, percent));
        }
    }
}
