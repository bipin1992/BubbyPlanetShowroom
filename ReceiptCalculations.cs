namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Receipt reward-label text. DB membership name is preferred;
    /// otherwise the old threshold-only label is kept.
    /// </summary>
    public static class ReceiptCalculations
    {
        public static string FormatMembershipLabel(
            string? membershipName,
            decimal minPurchase,
            decimal discountPercent)
        {
            string dbName = (membershipName ?? "").Trim();
            if (dbName.Length > 0)
                return $"{dbName} (₹{minPurchase:N0}+, {discountPercent:0.##}% off)";

            return $"₹{minPurchase:N0}+ ({discountPercent:0.##}% off)";
        }
    }
}
