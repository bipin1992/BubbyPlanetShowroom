using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using ZXing;
using ZXing.Windows.Compatibility;

namespace BubbyPlanetShowroom
{
    public class LabelPrint : UserControl
    {
        TextBox txtSize, txtPrice, txtDate, txtCode;
        Panel labelPanel;
        Button btnPrint;

        PrintDocument printDoc = new PrintDocument();
        Bitmap barcodeImage;

        Font textFont = new Font("Arial", 9, FontStyle.Bold);
        Brush textBrush = Brushes.Black;

        public LabelPrint()
        {
            InitializeUI();
            printDoc.PrintPage += PrintDoc_PrintPage;
            txtDate.Text = DateTime.Now.ToString("dd-MM-yyyy");
        }

        void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.DoubleBuffered = true;

            txtDate = new TextBox() { Location = new Point(20, 20), Width = 150, PlaceholderText = "Date" };
            txtSize = new TextBox() { Location = new Point(20, 55), Width = 150, PlaceholderText = "Size" };
            txtPrice = new TextBox() { Location = new Point(20, 90), Width = 150, PlaceholderText = "Price" };
            txtCode = new TextBox() { Location = new Point(20, 125), Width = 150, PlaceholderText = "Item Code" };

            txtDate.TextChanged += (s, e) => labelPanel.Invalidate();
            txtSize.TextChanged += (s, e) => labelPanel.Invalidate();
            txtPrice.TextChanged += (s, e) => labelPanel.Invalidate();

            txtCode.TextChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCode.Text))
                    return;

                try
                {
                    string q = $@"
        SELECT item_name,size,selling_price 
        FROM inv_items_master 
        WHERE item_code='{txtCode.Text.Trim()}' 
        LIMIT 1";

                    var dt = DB.GetData(q);

                    if (dt.Rows.Count > 0)
                    {
                        txtSize.Text = dt.Rows[0]["size"].ToString();
                        txtPrice.Text = dt.Rows[0]["selling_price"].ToString();
                    }
                    else
                    {
                        txtSize.Text = "";
                        txtPrice.Text = "";
                    }
                }
                catch { }

                GenerateBarcode(txtCode.Text);
                labelPanel.Invalidate();
            };

            btnPrint = new Button()
            {
                Text = "Print",
                Location = new Point(20, 165),
                Width = 150,
                Height = 35
            };
            btnPrint.Click += (s, e) => PrintLabel();

            labelPanel = new Panel()
            {
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(250, 20),
                Size = new Size(320, 130),
                BackColor = Color.White
            };

            labelPanel.Paint += LabelPanel_Paint;

            txtCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    LoadItemDetails();
                }
            };

            Controls.Add(txtDate);
            Controls.Add(txtSize);
            Controls.Add(txtPrice);
            Controls.Add(txtCode);
            Controls.Add(btnPrint);
            Controls.Add(labelPanel);
        }

        void LoadItemDetails()
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text))
                return;

            string q = $@"
    SELECT item_name,size,selling_price
    FROM inv_items_master
    WHERE item_code='{txtCode.Text.Trim()}'
    LIMIT 1";

            var dt = DB.GetData(q);

            if (dt.Rows.Count > 0)
            {
                txtSize.Text = dt.Rows[0]["size"].ToString();
                txtPrice.Text = dt.Rows[0]["selling_price"].ToString();
            }
        }

        void GenerateBarcode(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    barcodeImage?.Dispose();
                    barcodeImage = null;
                    return;
                }

                var writer = new ZXing.BarcodeWriter<Bitmap>
                {
                    Format = ZXing.BarcodeFormat.CODE_128,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = 180,
                        Height = 50,
                        Margin = 1
                    },
                    Renderer = new ZXing.Windows.Compatibility.BitmapRenderer()
                };

                barcodeImage?.Dispose();
                barcodeImage = writer.Write(text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Barcode Error: " + ex.Message);
                barcodeImage = null;
            }
        }

        void LabelPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawLabel(e.Graphics, labelPanel.ClientRectangle);
        }

        void DrawLabel(Graphics g, Rectangle bounds)
        {
            g.Clear(Color.White);

            int pageWidth = bounds.Width;
            int topMargin = 20;   // 🔹 Adjust this for more/less top space

            // 🔹 Bigger readable font
            Font textFont = new Font("Arial", 9, FontStyle.Bold);
            Brush brush = Brushes.Black;

            // -------- First Line (Centered) --------
            DateTime dt = DateTime.Parse(txtDate.Text);

            string formattedDate = (dt.Year / 100).ToString("00") + dt.ToString("MMddyy");

            string firstLine = $"{formattedDate}  Size:{txtSize.Text}  ₹{txtPrice.Text}";
            SizeF textSize = g.MeasureString(firstLine, textFont);

            float textX = (pageWidth - textSize.Width) / 2;
            float textY = topMargin;

            g.DrawString(firstLine, textFont, brush, textX, textY);

            // -------- Barcode --------
            if (barcodeImage != null)
            {
                int barcodeWidth = 180;
                int barcodeHeight = 45;

                float barcodeX = (pageWidth - barcodeWidth) / 2;
                float barcodeY = textY + textSize.Height + 4;   // 🔹 Space after text

                g.DrawImage(barcodeImage, barcodeX, barcodeY, barcodeWidth, barcodeHeight);
            }
        }

        void PrintLabel()
        {
            string qtyInput = Interaction.InputBox("Enter label quantity", "Print Labels", "1");
            if (string.IsNullOrWhiteSpace(qtyInput))
                return;

            if (!short.TryParse(qtyInput, out short copies) || copies <= 0)
            {
                MessageBox.Show("Enter valid quantity.");
                return;
            }

            PrinterRouting.ApplyLabelPrinter(printDoc);
            printDoc.PrinterSettings.Copies = copies;

            // 55mm × 25mm
            PaperSize labelSize = new PaperSize("Custom", 216, 98);

            printDoc.DefaultPageSettings.PaperSize = labelSize;
            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            printDoc.OriginAtMargins = false;

            printDoc.Print();
        }

        void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            // Remove hard margin shift (important for thermal printers)
            e.Graphics.TranslateTransform(
                -e.PageSettings.HardMarginX,
                -e.PageSettings.HardMarginY);

            Rectangle printArea = new Rectangle(
                0,
                0,
                e.PageSettings.PaperSize.Width,
                e.PageSettings.PaperSize.Height);

            DrawLabel(e.Graphics, printArea);
        }
    }
}
