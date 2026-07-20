using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Two-tone brand mark: "Bubby" sky-blue + "planet" lime-green.
    /// </summary>
    public sealed class BrandNameLabel : Control
    {
        public static readonly Color BubbyColor = Color.FromArgb(56, 189, 248);
        public static readonly Color PlanetColor = Color.FromArgb(132, 204, 22);

        private const string BubbyPart = "Bubby";
        private const string PlanetPart = "planet";

        public string Suffix { get; set; } = "";
        public Color SuffixColor { get; set; } = Color.White;

        /// <summary>When false, skips solid fill so a painted parent gradient can show through.</summary>
        public bool FillBackground { get; set; } = true;

        public BrandNameLabel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 41, 59);
            Font = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
            Size = new Size(200, 36);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (FillBackground)
                e.Graphics.Clear(BackColor);

            DrawBrand(e.Graphics, Font, new Point(0, (Height - PreferredContentHeight(Font)) / 2), Suffix, SuffixColor);
        }

        public static int PreferredContentHeight(Font font)
        {
            Size size = Measure(font, "");
            return size.Height;
        }

        public static Size Measure(Font font, string suffix = "")
        {
            using Bitmap bmp = new Bitmap(1, 1);
            using Graphics g = Graphics.FromImage(bmp);
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Size b = TextRenderer.MeasureText(g, BubbyPart, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            Size p = TextRenderer.MeasureText(g, PlanetPart, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            int w = b.Width + p.Width;
            int h = Math.Max(b.Height, p.Height) + 6;

            if (!string.IsNullOrEmpty(suffix))
            {
                Size s = TextRenderer.MeasureText(g, suffix, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                w += s.Width;
                h = Math.Max(h, s.Height + 6);
            }

            return new Size(Math.Max(w, 40), Math.Max(h, 24));
        }

        public static void DrawBrand(Graphics g, Font font, Point location, string suffix = "", Color? suffixColor = null)
        {
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Size bubbySize = TextRenderer.MeasureText(
                g, BubbyPart, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            Size planetSize = TextRenderer.MeasureText(
                g, PlanetPart, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            int h = Math.Max(bubbySize.Height, planetSize.Height);
            int x = location.X;
            int y = location.Y;

            TextRenderer.DrawText(
                g, BubbyPart, font,
                new Rectangle(x, y, bubbySize.Width + 4, h),
                BubbyColor,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);

            x += Math.Max(bubbySize.Width - 3, 1);

            TextRenderer.DrawText(
                g, PlanetPart, font,
                new Rectangle(x, y, planetSize.Width + 4, h),
                PlanetColor,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);

            if (!string.IsNullOrEmpty(suffix))
            {
                x += planetSize.Width;
                Size suffixSize = TextRenderer.MeasureText(
                    g, suffix, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(
                    g, suffix, font,
                    new Rectangle(x, y, suffixSize.Width + 4, h),
                    suffixColor ?? Color.White,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
