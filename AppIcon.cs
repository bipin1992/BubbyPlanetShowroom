using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Window + taskbar icon: BP using the logo wordmark colors
    /// (B = Bubby teal, P = Planet lime).
    /// </summary>
    internal static class AppIcon
    {
        // Sampled from Assets/bubbyplanet-logo.png wordmark.
        private static readonly Color BubbyTeal = Color.FromArgb(65, 174, 213);
        private static readonly Color PlanetLime = Color.FromArgb(154, 199, 24);

        private static readonly Lazy<Icon> CurrentLazy = new(Create);

        public static Icon Current => CurrentLazy.Value;

        public static void Apply(Form form)
        {
            form.Icon = Current;
            form.ShowIcon = true;
        }

        private static Icon Create()
        {
            using MemoryStream stream = new();
            WriteIco(stream, new[] { 16, 24, 32, 48, 64 });
            stream.Position = 0;
            return new Icon(stream);
        }

        internal static void WriteIco(Stream output, int[] sizes)
        {
            byte[][] images = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
            {
                using Bitmap bmp = Draw(sizes[i]);
                images[i] = ToBmpIconImage(bmp);
            }

            using BinaryWriter writer = new(output, System.Text.Encoding.UTF8, leaveOpen: true);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)sizes.Length);

            int offset = 6 + (16 * sizes.Length);
            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];
                byte dim = size >= 256 ? (byte)0 : (byte)size;
                writer.Write(dim);
                writer.Write(dim);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }

            foreach (byte[] image in images)
                writer.Write(image);

            writer.Flush();
        }

        private static byte[] ToBmpIconImage(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            int xorStride = width * 4;
            int andStride = ((width + 31) / 32) * 4;
            byte[] xor = new byte[xorStride * height];
            byte[] and = new byte[andStride * height];

            for (int y = 0; y < height; y++)
            {
                int destY = height - 1 - y;
                for (int x = 0; x < width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    int xorIndex = (destY * xorStride) + (x * 4);
                    xor[xorIndex] = pixel.B;
                    xor[xorIndex + 1] = pixel.G;
                    xor[xorIndex + 2] = pixel.R;
                    xor[xorIndex + 3] = pixel.A;

                    if (pixel.A < 16)
                    {
                        int andIndex = (destY * andStride) + (x / 8);
                        and[andIndex] |= (byte)(0x80 >> (x % 8));
                    }
                }
            }

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            writer.Write(40);
            writer.Write(width);
            writer.Write(height * 2);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(0);
            writer.Write(xor.Length + and.Length);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(xor);
            writer.Write(and);
            writer.Flush();
            return stream.ToArray();
        }

        internal static Bitmap Draw(int size)
        {
            Bitmap bmp = new(size, size, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.White);

            int pad = Math.Max(1, size / 18);
            int body = size - (pad * 2);
            int radius = Math.Max(2, size / 6);
            using (GraphicsPath plate = RoundedRect(pad, pad, body, body, radius))
            using (SolidBrush fill = new(Color.White))
            using (Pen border = new(BubbyTeal, Math.Max(1f, size / 18f)))
            {
                g.FillPath(fill, plate);
                g.DrawPath(border, plate);
            }

            float fontPx = size * (size <= 24 ? 0.52f : 0.56f);
            using Font font = new("Segoe UI", fontPx, FontStyle.Bold, GraphicsUnit.Pixel);
            using StringFormat format = new(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            };

            SizeF bSize = g.MeasureString("B", font, int.MaxValue, format);
            SizeF pSize = g.MeasureString("P", font, int.MaxValue, format);
            float gap = size * 0.02f;
            float totalW = bSize.Width + pSize.Width + gap;
            float x = (size - totalW) / 2f;

            using SolidBrush bBrush = new(BubbyTeal);
            using SolidBrush pBrush = new(PlanetLime);
            g.DrawString("B", font, bBrush, new RectangleF(x, 0, bSize.Width + 1, size), format);
            g.DrawString("P", font, pBrush, new RectangleF(x + bSize.Width + gap, 0, pSize.Width + 1, size), format);

            return bmp;
        }

        private static GraphicsPath RoundedRect(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new();
            int d = Math.Min(radius * 2, Math.Min(width, height));
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
