using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

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

        /// <summary>
        /// A saved coupon code applies even when its rupee amount is blank or zero.
        /// Money off still needs a number; the code itself does not.
        /// </summary>
        public static bool ShouldPrintSaleCoupon(string? couponCode)
        {
            return !string.IsNullOrWhiteSpace(couponCode);
        }

        public static bool ShouldPrintCoupon(string? couponCode, decimal couponDiscount)
        {
            return ShouldPrintSaleCoupon(couponCode) && couponDiscount > 0m;
        }

        /// <summary>
        /// Extra receipt lines when a coupon code was applied.
        /// Discount may be 0. No code means the old grand-total footer.
        /// </summary>
        public static string[] CouponBillLines(string? couponCode, decimal couponDiscount, decimal payable)
        {
            if (!ShouldPrintSaleCoupon(couponCode))
                return Array.Empty<string>();

            return new[]
            {
                "Coupon: " + CouponCalculations.NormalizeCode(couponCode),
                "Coupon Discount: -" + couponDiscount.ToString("0.00"),
                "PAYABLE: " + payable.ToString("0.00")
            };
        }

        public const string InstagramUrl = "https://www.instagram.com/bubbyplanet/";

        /// <summary>Extra paper height for the follow block under Thank you.</summary>
        public const int InstagramFooterHeight = 220;

        public static float DrawInstagramFollow(Graphics g, float pageWidth, float y, Font font)
        {
            y += 8;
            string line1 = "Follow BUBBYPLANET on Instagram";
            string line2 = "for the latest sales and discounts.";
            SizeF size1 = g.MeasureString(line1, font);
            g.DrawString(line1, font, Brushes.Black, (pageWidth - size1.Width) / 2f, y);
            y += 14;
            SizeF size2 = g.MeasureString(line2, font);
            g.DrawString(line2, font, Brushes.Black, (pageWidth - size2.Width) / 2f, y);
            y += 16;

            float dpi = g.DpiX;
            if (float.IsNaN(dpi) || float.IsInfinity(dpi) || dpi < 72f)
                dpi = 203f;

            using Bitmap qr = CreateInstagramQr(dpi);
            // Receipt coordinates are hundredths of an inch. Match bitmap pixels to printer dots
            // so each square stays solid. A stretched gray QR scans in a PDF and fails on thermal paper.
            float dest = qr.Width * 100f / dpi;
            float qrX = (pageWidth - dest) / 2f;

            GraphicsState state = g.Save();
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.None;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.DrawImage(qr, qrX, y, dest, dest);
            g.Restore(state);

            y += dest + 6;

            string handle = "instagram.com/bubbyplanet";
            SizeF handleSize = g.MeasureString(handle, font);
            g.DrawString(handle, font, Brushes.Black, (pageWidth - handleSize.Width) / 2f, y);
            y += 16;
            return y;
        }

        /// <summary>
        /// 1-bit QR sized for the printer. Thermal heads turn soft gray edges into speckles a phone cannot read.
        /// </summary>
        private static Bitmap CreateInstagramQr(float dpi)
        {
            var hints = new Dictionary<EncodeHintType, object>
            {
                { EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M },
                { EncodeHintType.MARGIN, 4 },
                { EncodeHintType.CHARACTER_SET, "UTF-8" }
            };

            BitMatrix matrix = new QRCodeWriter().encode(InstagramUrl, BarcodeFormat.QR_CODE, 0, 0, hints);
            int modules = matrix.Width;
            int dots = DotsPerModule(modules, dpi);
            int px = modules * dots;

            var bmp = new Bitmap(px, px, PixelFormat.Format1bppIndexed);
            bmp.SetResolution(dpi, dpi);
            ColorPalette palette = bmp.Palette;
            palette.Entries[0] = Color.Black;
            palette.Entries[1] = Color.White;
            bmp.Palette = palette;

            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, px, px),
                ImageLockMode.WriteOnly,
                PixelFormat.Format1bppIndexed);
            try
            {
                int stride = data.Stride;
                byte[] raw = new byte[stride * px];
                for (int i = 0; i < raw.Length; i++)
                    raw[i] = 0xFF;

                for (int moduleY = 0; moduleY < modules; moduleY++)
                {
                    for (int moduleX = 0; moduleX < modules; moduleX++)
                    {
                        if (!matrix[moduleX, moduleY])
                            continue;

                        for (int dy = 0; dy < dots; dy++)
                        {
                            int row = (moduleY * dots + dy) * stride;
                            for (int dx = 0; dx < dots; dx++)
                            {
                                int bit = moduleX * dots + dx;
                                int index = row + (bit >> 3);
                                raw[index] &= (byte)~(0x80 >> (bit & 7));
                            }
                        }
                    }
                }

                Marshal.Copy(raw, 0, data.Scan0, raw.Length);
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            return bmp;
        }

        private static int DotsPerModule(int modules, float dpi)
        {
            const float targetInches = 1.28f;
            int dots = (int)Math.Round(targetInches * dpi / modules);
            if (dots < 4)
                dots = 4;

            int maxDots = (int)Math.Floor(1.70f * dpi / modules);
            if (maxDots < 4)
                maxDots = 4;
            if (dots > maxDots)
                dots = maxDots;

            while (modules * dots > 900 && dots > 4)
                dots--;

            return dots;
        }

        public static string[] ReturnCouponLines(string? couponCode, decimal couponShare, bool exchange)
        {
            if (!ShouldPrintCoupon(couponCode, couponShare))
                return Array.Empty<string>();

            string amountLine = exchange
                ? "Coupon on this bill: -" + couponShare.ToString("0.00")
                : "Coupon Discount: -" + couponShare.ToString("0.00");

            return new[]
            {
                "Coupon: " + CouponCalculations.NormalizeCode(couponCode),
                amountLine
            };
        }
    }
}
