//using System;
//using System.Data;
//using System.Drawing;
//using System.Drawing.Printing;
//using System.Windows.Forms;

//namespace BubbyPlanetShowroom
//{
//    public class Master : UserControl
//    {
//        DataGridView grid = new DataGridView();

//        ComboBox cmbMainCategory = new ComboBox();
//        ComboBox cmbSubCategory = new ComboBox();
//        ComboBox cmbGender = new ComboBox();
//        ComboBox cmbItemType = new ComboBox();

//        TextBox txtSearch = new TextBox();

//        Panel leftPanel = new Panel();
//        Panel rightPanel = new Panel();

//        //bool rowEdited = false;

//        PrintDocument printDoc = new PrintDocument();
//        Bitmap barcodeImage;

//        string _code = "";
//        string _size = "";
//        string _price = "";
//        string _date = DateTime.Now.ToString("dd-MM-yyyy");

//        int printQty = 1;
//        int printedCount = 0;

//        public Master()
//        {
//            InitializeUI();
//            LoadMainCategory();
//            LoadData();
//        }

//        void InitializeUI()
//        {
//            this.Dock = DockStyle.Fill;

//            leftPanel.Dock = DockStyle.Top;
//            leftPanel.Height = 70;
//            leftPanel.Padding = new Padding(10);
//            leftPanel.BackColor = Color.WhiteSmoke;

//            rightPanel.Dock = DockStyle.Fill;

//            Label lblSearch = new Label();
//            lblSearch.Text = "Search";
//            lblSearch.Left = 10;
//            lblSearch.Top = 10;
//            lblSearch.AutoSize = true;

//            txtSearch.Left = 10;
//            txtSearch.Top = 30;
//            txtSearch.Width = 150;
//            txtSearch.TextChanged += (s, e) => ApplyFilter();

//            Label lblMain = new Label();
//            lblMain.Text = "Main Category";
//            lblMain.Left = 180;
//            lblMain.Top = 10;
//            lblMain.AutoSize = true;

//            cmbMainCategory.Left = 180;
//            cmbMainCategory.Top = 30;
//            cmbMainCategory.Width = 150;
//            cmbMainCategory.SelectedIndexChanged += CmbMainCategory_SelectedIndexChanged;

//            Label lblSub = new Label();
//            lblSub.Text = "Sub Category";
//            lblSub.Left = 350;
//            lblSub.Top = 10;
//            lblSub.AutoSize = true;

//            cmbSubCategory.Left = 350;
//            cmbSubCategory.Top = 30;
//            cmbSubCategory.Width = 150;
//            cmbSubCategory.SelectedIndexChanged += CmbSubCategory_SelectedIndexChanged;

//            Label lblGender = new Label();
//            lblGender.Text = "Gender";
//            lblGender.Left = 520;
//            lblGender.Top = 10;
//            lblGender.AutoSize = true;

//            cmbGender.Left = 520;
//            cmbGender.Top = 30;
//            cmbGender.Width = 120;
//            cmbGender.SelectedIndexChanged += CmbGender_SelectedIndexChanged;

//            Label lblType = new Label();
//            lblType.Text = "Item Type";
//            lblType.Left = 660;
//            lblType.Top = 10;
//            lblType.AutoSize = true;

//            cmbItemType.Left = 660;
//            cmbItemType.Top = 30;
//            cmbItemType.Width = 150;
//            cmbItemType.SelectedIndexChanged += (s, e) => ApplyFilter();

//            leftPanel.Controls.Add(lblSearch);
//            leftPanel.Controls.Add(txtSearch);
//            leftPanel.Controls.Add(lblMain);
//            leftPanel.Controls.Add(cmbMainCategory);
//            leftPanel.Controls.Add(lblSub);
//            leftPanel.Controls.Add(cmbSubCategory);
//            leftPanel.Controls.Add(lblGender);
//            leftPanel.Controls.Add(cmbGender);
//            leftPanel.Controls.Add(lblType);
//            leftPanel.Controls.Add(cmbItemType);

//            grid.Dock = DockStyle.Fill;
//            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
//            grid.AllowUserToAddRows = false;

//            grid.CellClick += Grid_CellClick;
//            //grid.CellValueChanged += Grid_CellValueChanged;

//            grid.CurrentCellDirtyStateChanged += (s, e) =>
//            {
//                if (grid.IsCurrentCellDirty)
//                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
//            };

//            rightPanel.Controls.Add(grid);

//            this.Controls.Add(rightPanel);
//            this.Controls.Add(leftPanel);

//            printDoc.PrintPage += PrintDoc_PrintPage;
//        }

//        void LoadData(string where = "")
//        {
//            string query = "SELECT * FROM inv_items_master";

//            if (where != "")
//                query += " WHERE " + where;

//            query += " ORDER BY id DESC";

//            DataTable dt = DB.GetData(query);

//            grid.Columns.Clear();
//            grid.DataSource = dt;

//            foreach (DataGridViewColumn col in grid.Columns)
//                col.ReadOnly = true;

//            AddButtons();
//        }

//        void AddButtons()
//        {
//            //DataGridViewButtonColumn edit = new DataGridViewButtonColumn();
//            //edit.Name = "Edit";
//            //edit.Text = "Edit";
//            //edit.UseColumnTextForButtonValue = true;

//            DataGridViewButtonColumn print = new DataGridViewButtonColumn();
//            print.Name = "Print";
//            print.Text = "Print";
//            print.UseColumnTextForButtonValue = true;

//            //grid.Columns.Add(edit);
//            grid.Columns.Add(print);
//        }

//        void LoadMainCategory()
//        {
//            cmbMainCategory.Items.Clear();
//            cmbMainCategory.Items.Add("All");

//            DataTable dt = DB.GetData("SELECT DISTINCT main_category FROM inv_items_master");

//            foreach (DataRow r in dt.Rows)
//                cmbMainCategory.Items.Add(r[0].ToString());

//            cmbMainCategory.SelectedIndex = 0;
//        }

//        void CmbMainCategory_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            cmbSubCategory.Items.Clear();
//            cmbSubCategory.Items.Add("All");

//            string query = "SELECT DISTINCT sub_category FROM inv_items_master";

//            if (cmbMainCategory.Text != "All")
//                query += " WHERE main_category='" + cmbMainCategory.Text + "'";

//            DataTable dt = DB.GetData(query);

//            foreach (DataRow r in dt.Rows)
//                cmbSubCategory.Items.Add(r[0].ToString());

//            cmbSubCategory.SelectedIndex = 0;

//            if (cmbMainCategory.Text == "Toys")
//            {
//                cmbGender.Enabled = false;
//                cmbGender.Items.Clear();
//                cmbGender.Items.Add("NA");
//                cmbGender.SelectedIndex = 0;
//            }
//            else
//            {
//                cmbGender.Enabled = true;
//                LoadGender();
//            }

//            ApplyFilter();
//        }

//        void CmbSubCategory_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            LoadGender();
//            ApplyFilter();
//        }

//        void LoadGender()
//        {
//            cmbGender.Items.Clear();
//            cmbGender.Items.Add("All");

//            string query = "SELECT DISTINCT gender FROM inv_items_master WHERE 1=1";

//            if (cmbMainCategory.Text != "All")
//                query += " AND main_category='" + cmbMainCategory.Text + "'";

//            if (cmbSubCategory.Text != "All")
//                query += " AND sub_category='" + cmbSubCategory.Text + "'";

//            DataTable dt = DB.GetData(query);

//            foreach (DataRow r in dt.Rows)
//                cmbGender.Items.Add(r[0].ToString());

//            cmbGender.SelectedIndex = 0;
//        }

//        void CmbGender_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            LoadItemType();
//            ApplyFilter();
//        }

//        void LoadItemType()
//        {
//            cmbItemType.Items.Clear();
//            cmbItemType.Items.Add("All");

//            string query = "SELECT DISTINCT item_type FROM inv_items_master WHERE 1=1";

//            if (cmbMainCategory.Text != "All")
//                query += " AND main_category='" + cmbMainCategory.Text + "'";

//            if (cmbSubCategory.Text != "All")
//                query += " AND sub_category='" + cmbSubCategory.Text + "'";

//            if (cmbGender.Enabled && cmbGender.Text != "All")
//                query += " AND gender='" + cmbGender.Text + "'";

//            DataTable dt = DB.GetData(query);

//            foreach (DataRow r in dt.Rows)
//                cmbItemType.Items.Add(r[0].ToString());

//            cmbItemType.SelectedIndex = 0;
//        }

//        void ApplyFilter()
//        {
//            string where = "1=1";

//            if (cmbMainCategory.Text != "All")
//                where += " AND main_category='" + cmbMainCategory.Text + "'";

//            if (cmbSubCategory.Text != "All")
//                where += " AND sub_category='" + cmbSubCategory.Text + "'";

//            if (cmbGender.Enabled && cmbGender.Text != "All")
//                where += " AND gender='" + cmbGender.Text + "'";

//            if (cmbItemType.Text != "All")
//                where += " AND item_type='" + cmbItemType.Text + "'";

//            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
//                where += " AND item_name LIKE '%" + txtSearch.Text + "%'";

//            LoadData(where);
//        }

//        //void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
//        //{
//        //    if (e.RowIndex < 0) return;

//        //    var row = grid.Rows[e.RowIndex];

//        //    if (grid.Columns[e.ColumnIndex].Name == "Edit")
//        //    {
//        //        if (row.Cells["Edit"].Value.ToString() == "Edit")
//        //        {
//        //            foreach (DataGridViewCell c in row.Cells)
//        //                c.ReadOnly = false;

//        //            row.Cells["id"].ReadOnly = true;

//        //            row.Cells["Edit"].Value = "Save";
//        //        }
//        //        else
//        //        {
//        //            //UpdateRow(row);

//        //            foreach (DataGridViewCell c in row.Cells)
//        //                c.ReadOnly = true;

//        //            row.Cells["Edit"].Value = "Edit";
//        //        }
//        //    }
//        //}

//        //void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
//        //{
//        //    if (e.RowIndex < 0) return;

//        //    var row = grid.Rows[e.RowIndex];

//        //    decimal pbt = 0;
//        //    decimal gst = 0;

//        //    decimal.TryParse(row.Cells["price_before_tax"].Value?.ToString(), out pbt);
//        //    decimal.TryParse(row.Cells["gst"].Value?.ToString(), out gst);

//        //    decimal gstAmount = pbt * gst / 100;

//        //    row.Cells["selling_price"].Value = Math.Round(pbt + gstAmount, 2);
//        //}

//        void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
//        {
//            if (e.RowIndex < 0) return;

//            if (grid.Columns[e.ColumnIndex].Name == "Print")
//            {
//                var row = grid.Rows[e.RowIndex];

//                _code = row.Cells["item_code"].Value.ToString();
//                _size = row.Cells["size"].Value.ToString();
//                _price = row.Cells["selling_price"].Value.ToString();

//                GenerateBarcode(_code);

//                PrintLabel();
//            }
//        }


//        void UpdateRow(DataGridViewRow row)
//        {
//            int id = Convert.ToInt32(row.Cells["id"].Value);

//            string query = $@"
//            UPDATE inv_items_master SET
//            item_name='{row.Cells["item_name"].Value}',
//            main_category='{row.Cells["main_category"].Value}',
//            sub_category='{row.Cells["sub_category"].Value}',
//            gender='{row.Cells["gender"].Value}',
//            item_type='{row.Cells["item_type"].Value}',
//            price_before_tax='{row.Cells["price_before_tax"].Value}',
//            gst='{row.Cells["gst"].Value}',
//            selling_price='{row.Cells["selling_price"].Value}',
//            stock='{row.Cells["stock"].Value}'
//            WHERE id={id}";

//            DB.Execute(query);
//        }

//        void GenerateBarcode(string text)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(text))
//                {
//                    barcodeImage?.Dispose();
//                    barcodeImage = null;
//                    return;
//                }

//                var writer = new ZXing.BarcodeWriter<Bitmap>
//                {
//                    Format = ZXing.BarcodeFormat.CODE_128,
//                    Options = new ZXing.Common.EncodingOptions
//                    {
//                        Width = 180,
//                        Height = 50,
//                        Margin = 1
//                    },
//                    Renderer = new ZXing.Windows.Compatibility.BitmapRenderer()
//                };

//                barcodeImage?.Dispose();
//                barcodeImage = writer.Write(text.Trim());
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Barcode Error: " + ex.Message);
//                barcodeImage = null;
//            }
//        }

//        void DrawLabel(Graphics g, Rectangle bounds)
//        {
//            g.Clear(Color.White);

//            int pageWidth = bounds.Width;
//            int topMargin = 20;

//            Font textFont = new Font("Arial", 8, FontStyle.Bold);
//            Brush brush = Brushes.Black;

//            DateTime dt = DateTime.Parse(_date);

//            string formattedDate = (dt.Year / 100).ToString("00") + dt.ToString("MMddyy");

//            string firstLine = $"{formattedDate}  Size:{_size}  ₹{_price}";
//            SizeF textSize = g.MeasureString(firstLine, textFont);

//            float textX = (pageWidth - textSize.Width) / 2;
//            float textY = topMargin;

//            g.DrawString(firstLine, textFont, brush, textX, textY);

//            if (barcodeImage != null)
//            {
//                int barcodeWidth = 180;
//                int barcodeHeight = 45;

//                float barcodeX = (pageWidth - barcodeWidth) / 2;
//                float barcodeY = textY + textSize.Height + 4;

//                g.DrawImage(barcodeImage, barcodeX, barcodeY, barcodeWidth, barcodeHeight);
//            }
//        }

//        void PrintLabel()
//        {
//            PrintDialog pd = new PrintDialog();
//            pd.Document = printDoc;
//            pd.AllowSomePages = false;

//            if (pd.ShowDialog() == DialogResult.OK)
//            {
//                printDoc.PrinterSettings = pd.PrinterSettings;

//                // ✅ Printer copies (user selects here)
//                // e.g. 5 copies → same label 5 times

//                PaperSize labelSize = new PaperSize("Custom", 216, 98);

//                printDoc.DefaultPageSettings.PaperSize = labelSize;
//                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
//                printDoc.OriginAtMargins = false;

//                printDoc.Print();
//            }
//        }

//        void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
//        {
//            e.Graphics.TranslateTransform(
//                -e.PageSettings.HardMarginX,
//                -e.PageSettings.HardMarginY);

//            Rectangle printArea = new Rectangle(
//                0,
//                0,
//                e.PageSettings.PaperSize.Width,
//                e.PageSettings.PaperSize.Height);

//            DrawLabel(e.Graphics, printArea);

//            e.HasMorePages = false;
//        }


//    }
//}






using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Master : UserControl
    {
        DataGridView grid = new DataGridView();

        ComboBox cmbMainCategory = new ComboBox();
        ComboBox cmbSubCategory = new ComboBox();
        ComboBox cmbGender = new ComboBox();
        ComboBox cmbItemType = new ComboBox();

        TextBox txtSearch = new TextBox();

        Panel leftPanel = new Panel();
        Panel rightPanel = new Panel();

        //bool rowEdited = false;

        PrintDocument printDoc = new PrintDocument();
        Bitmap barcodeImage;

        string _code = "";
        string _size = "";
        string _price = "";
        string _date = DateTime.Now.ToString("dd-MM-yyyy");

        int printQty = 1;
        int printedCount = 0;
        bool isLoading = false;
        bool suppressPriceRecalc = false;
        int editingRowIndex = -1;
        readonly string currentRole;

        public Master(string role = "")
        {
            currentRole = role ?? "";
            InitializeUI();
            LoadMainCategory();
            LoadData();
        }

        void InitializeUI()
        {
            this.Dock = DockStyle.Fill;

            leftPanel.Dock = DockStyle.Top;
            leftPanel.Height = 70;
            leftPanel.Padding = new Padding(10);
            leftPanel.BackColor = Color.WhiteSmoke;

            rightPanel.Dock = DockStyle.Fill;

            Label lblSearch = new Label();
            lblSearch.Text = "Search";
            lblSearch.Left = 10;
            lblSearch.Top = 10;
            lblSearch.AutoSize = true;

            txtSearch.Left = 10;
            txtSearch.Top = 30;
            txtSearch.Width = 150;
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            Label lblMain = new Label();
            lblMain.Text = "Main Category";
            lblMain.Left = 180;
            lblMain.Top = 10;
            lblMain.AutoSize = true;

            cmbMainCategory.Left = 180;
            cmbMainCategory.Top = 30;
            cmbMainCategory.Width = 150;
            cmbMainCategory.SelectedIndexChanged += CmbMainCategory_SelectedIndexChanged;

            Label lblSub = new Label();
            lblSub.Text = "Sub Category";
            lblSub.Left = 350;
            lblSub.Top = 10;
            lblSub.AutoSize = true;

            cmbSubCategory.Left = 350;
            cmbSubCategory.Top = 30;
            cmbSubCategory.Width = 150;
            cmbSubCategory.SelectedIndexChanged += CmbSubCategory_SelectedIndexChanged;

            Label lblGender = new Label();
            lblGender.Text = "Gender";
            lblGender.Left = 520;
            lblGender.Top = 10;
            lblGender.AutoSize = true;

            cmbGender.Left = 520;
            cmbGender.Top = 30;
            cmbGender.Width = 120;
            cmbGender.SelectedIndexChanged += CmbGender_SelectedIndexChanged;

            Label lblType = new Label();
            lblType.Text = "Item Type";
            lblType.Left = 660;
            lblType.Top = 10;
            lblType.AutoSize = true;

            cmbItemType.Left = 660;
            cmbItemType.Top = 30;
            cmbItemType.Width = 150;
            cmbItemType.SelectedIndexChanged += (s, e) => ApplyFilter();

            leftPanel.Controls.Add(lblSearch);
            leftPanel.Controls.Add(txtSearch);
            leftPanel.Controls.Add(lblMain);
            leftPanel.Controls.Add(cmbMainCategory);
            leftPanel.Controls.Add(lblSub);
            leftPanel.Controls.Add(cmbSubCategory);
            leftPanel.Controls.Add(lblGender);
            leftPanel.Controls.Add(cmbGender);
            leftPanel.Controls.Add(lblType);
            leftPanel.Controls.Add(cmbItemType);

            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AllowUserToAddRows = false;

            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.CellClick += Grid_CellClick;
            grid.CellValueChanged += Grid_CellValueChanged;

            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            rightPanel.Controls.Add(grid);

            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);

            printDoc.PrintPage += PrintDoc_PrintPage;


            Button btnReset = new Button();
            btnReset.Text = "Reset";
            btnReset.Width = 80;
            btnReset.Height = 25;
            btnReset.Left = 830;
            btnReset.Top = 28;

            btnReset.Click += (s, e) => ResetFilters();

            leftPanel.Controls.Add(btnReset);

            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.MultiSelect = false;
            // Columns stay ReadOnly until Edit; id + item_code always locked.
            grid.ReadOnly = false;
            grid.RowHeadersVisible = false; // left side arrow wala part
            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

            //grid.CellClick += (s, e) =>
            //{
            //    if (e.RowIndex < 0) return;

            //    // 👉 ID column name check karo (exact name DB ka)
            //    if (grid.Columns[e.ColumnIndex].Name == "id")
            //    {
            //        // 🔥 Full row select
            //        grid.ClearSelection();
            //        grid.Rows[e.RowIndex].Selected = true;
            //    }
            //    else
            //    {
            //        // 🔥 Normal cell select
            //        grid.ClearSelection();
            //        grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
            //    }
            //};

            //grid.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
            //grid.DefaultCellStyle.BackColor = Color.White;

            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkRed;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;

            grid.ColumnHeadersVisible = true;
            grid.ScrollBars = ScrollBars.Both;
        }

        void LoadData(string where = "")
        {
            try
            {
                string query = "SELECT * FROM inv_items_master";

                if (!string.IsNullOrEmpty(where))
                    query += " WHERE " + where;

                query += " ORDER BY id DESC";

                DataTable dt = DB.GetData(query);

                if (dt == null)
                {
                    grid.DataSource = null;
                    return;
                }

                grid.Columns.Clear();
                grid.DataSource = dt;
                editingRowIndex = -1;

                foreach (DataGridViewColumn col in grid.Columns)
                    col.ReadOnly = true;

                ApplyRoleColumnVisibility();
                AddButtons();
                InitEditButtonLabels();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Data Load Error: " + ex.Message);
            }
        }

        void ApplyRoleColumnVisibility()
        {
            bool isAdminOrMaster =
                string.Equals(currentRole, "Master Admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase);

            if (grid.Columns.Contains("cost_price"))
                grid.Columns["cost_price"].Visible = isAdminOrMaster;

            if (grid.Columns.Contains("price_before_tax"))
                grid.Columns["price_before_tax"].Visible = isAdminOrMaster;
        }

        void AddButtons()
        {
            if (!grid.Columns.Contains("Edit"))
            {
                DataGridViewButtonColumn edit = new DataGridViewButtonColumn();
                edit.Name = "Edit";
                edit.HeaderText = "Edit";
                edit.Text = "Edit";
                edit.UseColumnTextForButtonValue = false;
                edit.ReadOnly = true;
                grid.Columns.Add(edit);
            }

            if (!grid.Columns.Contains("Print"))
            {
                DataGridViewButtonColumn print = new DataGridViewButtonColumn();
                print.Name = "Print";
                print.HeaderText = "Print";
                print.Text = "Print";
                print.UseColumnTextForButtonValue = true;
                print.ReadOnly = true;
                grid.Columns.Add(print);
            }
        }

        void InitEditButtonLabels()
        {
            if (!grid.Columns.Contains("Edit"))
                return;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (!row.IsNewRow)
                    row.Cells["Edit"].Value = "Edit";
            }
        }

        void SetRowEditable(DataGridViewRow row, bool editable)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                string name = col.Name;
                if (name == "Edit" || name == "Print")
                {
                    col.ReadOnly = true;
                    continue;
                }

                // Always locked — barcode identity must not change.
                if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "item_code", StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells[name].ReadOnly = true;
                    continue;
                }

                // Stock qty lives in inv_stock — do not edit master stock here.
                if (string.Equals(name, "stock", StringComparison.OrdinalIgnoreCase))
                {
                    row.Cells[name].ReadOnly = true;
                    continue;
                }

                row.Cells[name].ReadOnly = !editable;
            }
        }

        void LoadMainCategory()
        {
            cmbMainCategory.Items.Clear();
            cmbMainCategory.Items.Add("All");

            DataTable dt = DB.GetData("SELECT DISTINCT main_category FROM inv_items_master");

            foreach (DataRow r in dt.Rows)
                cmbMainCategory.Items.Add(r[0].ToString());

            cmbMainCategory.SelectedIndex = 0;
        }

        void CmbMainCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading) return;

            if (cmbMainCategory.Text == "Toys")
            {
                cmbGender.Enabled = false;
                cmbGender.Items.Clear();
                cmbGender.Items.Add("NA");
                cmbGender.SelectedIndex = 0;
            }
            else
            {
                cmbGender.Enabled = true;
            }

            LoadAllFilters();
            ApplyFilter();
        }

        void CmbSubCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading) return;

            LoadGender();     // 🔥 only next level
            LoadItemType();   // 🔥 next level

            ApplyFilter();
        }

        void LoadGender()
        {
            cmbGender.Items.Clear();
            cmbGender.Items.Add("All");

            string query = "SELECT DISTINCT gender FROM inv_items_master WHERE 1=1";

            if (cmbMainCategory.Text != "All")
                query += " AND main_category='" + cmbMainCategory.Text + "'";

            if (cmbSubCategory.Text != "All")
                query += " AND sub_category='" + cmbSubCategory.Text + "'";

            DataTable dt = DB.GetData(query);

            foreach (DataRow r in dt.Rows)
                cmbGender.Items.Add(r[0].ToString());

            cmbGender.SelectedIndex = 0;
        }

        void CmbGender_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoading) return;

            LoadItemType();   // 🔥 only next level

            ApplyFilter();
        }

        void LoadItemType()
        {
            cmbItemType.Items.Clear();
            cmbItemType.Items.Add("All");

            string query = "SELECT DISTINCT item_type FROM inv_items_master WHERE 1=1";

            if (cmbMainCategory.Text != "All")
                query += " AND main_category='" + cmbMainCategory.Text + "'";

            if (cmbSubCategory.Text != "All")
                query += " AND sub_category='" + cmbSubCategory.Text + "'";

            if (cmbGender.Enabled && cmbGender.Text != "All")
                query += " AND gender='" + cmbGender.Text + "'";

            DataTable dt = DB.GetData(query);

            foreach (DataRow r in dt.Rows)
                cmbItemType.Items.Add(r[0].ToString());

            cmbItemType.SelectedIndex = 0;
        }

        //void ApplyFilter()
        //{
        //    string where = "1=1";

        //    if (cmbMainCategory.Text != "All")
        //        where += " AND main_category='" + cmbMainCategory.Text + "'";

        //    if (cmbSubCategory.Text != "All")
        //        where += " AND sub_category='" + cmbSubCategory.Text + "'";

        //    if (cmbGender.Enabled && cmbGender.Text != "All")
        //        where += " AND gender='" + cmbGender.Text + "'";

        //    if (cmbItemType.Text != "All")
        //        where += " AND item_type='" + cmbItemType.Text + "'";

        //    if (!string.IsNullOrWhiteSpace(txtSearch.Text))
        //        where += " AND (item_name LIKE '%" + txtSearch.Text + "%' OR item_code LIKE '%" + txtSearch.Text + "%')";
        //    //where += " AND item_name LIKE '%" + txtSearch.Text + "%'";

        //    LoadData(where);
        //}

        void ApplyFilter()
        {
            try
            {
                string where = "1=1";

                string main = SafeText(cmbMainCategory.Text);
                string sub = SafeText(cmbSubCategory.Text);
                string gender = SafeText(cmbGender.Text);
                string type = SafeText(cmbItemType.Text);
                string search = SafeText(txtSearch.Text);

                if (!string.IsNullOrEmpty(main) && main != "All")
                    where += " AND main_category='" + main + "'";

                if (!string.IsNullOrEmpty(sub) && sub != "All")
                    where += " AND sub_category='" + sub + "'";

                if (cmbGender.Enabled && !string.IsNullOrEmpty(gender) && gender != "All")
                    where += " AND gender='" + gender + "'";

                if (!string.IsNullOrEmpty(type) && type != "All")
                    where += " AND item_type='" + type + "'";

                if (!string.IsNullOrWhiteSpace(search))
                    where += " AND (item_name LIKE '%" + search + "%' OR item_code LIKE '%" + search + "%')";

                LoadData(where);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Filter Error: " + ex.Message);
            }
        }

        //void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex < 0) return;

        //    var row = grid.Rows[e.RowIndex];

        //    if (grid.Columns[e.ColumnIndex].Name == "Edit")
        //    {
        //        if (row.Cells["Edit"].Value.ToString() == "Edit")
        //        {
        //            foreach (DataGridViewCell c in row.Cells)
        //                c.ReadOnly = false;

        //            row.Cells["id"].ReadOnly = true;

        //            row.Cells["Edit"].Value = "Save";
        //        }
        //        else
        //        {
        //            //UpdateRow(row);

        //            foreach (DataGridViewCell c in row.Cells)
        //                c.ReadOnly = true;

        //            row.Cells["Edit"].Value = "Edit";
        //        }
        //    }
        //}

        //void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex < 0) return;

        //    var row = grid.Rows[e.RowIndex];

        //    decimal pbt = 0;
        //    decimal gst = 0;

        //    decimal.TryParse(row.Cells["price_before_tax"].Value?.ToString(), out pbt);
        //    decimal.TryParse(row.Cells["gst"].Value?.ToString(), out gst);

        //    decimal gstAmount = pbt * gst / 100;

        //    row.Cells["selling_price"].Value = Math.Round(pbt + gstAmount, 2);
        //}

        //void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex < 0) return;

        //    if (grid.Columns[e.ColumnIndex].Name == "Print")
        //    {
        //        var row = grid.Rows[e.RowIndex];

        //        _code = row.Cells["item_code"].Value.ToString();
        //        _size = row.Cells["size"].Value.ToString();
        //        _price = row.Cells["selling_price"].Value.ToString();

        //        GenerateBarcode(_code);

        //        PrintLabel();
        //    }
        //}

        void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;

                string colName = grid.Columns[e.ColumnIndex].Name;

                // 🔥 ID COLUMN → FULL ROW SELECT
                if (colName.ToLower() == "id")   // safe check
                {
                    grid.ClearSelection();
                    grid.Rows[e.RowIndex].Selected = true;
                }
                else if (colName != "Edit" && colName != "Print")
                {
                    grid.ClearSelection();
                    grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
                }

                // EDIT / SAVE
                if (colName == "Edit")
                {
                    DataGridViewRow row = grid.Rows[e.RowIndex];
                    string mode = row.Cells["Edit"].Value?.ToString() ?? "Edit";

                    if (string.Equals(mode, "Edit", StringComparison.OrdinalIgnoreCase))
                    {
                        if (editingRowIndex >= 0 && editingRowIndex != e.RowIndex)
                        {
                            MessageBox.Show("Pehle current row Save karein, phir dusri row Edit karein.");
                            return;
                        }

                        SetRowEditable(row, true);
                        row.Cells["Edit"].Value = "Save";
                        editingRowIndex = e.RowIndex;
                    }
                    else
                    {
                        if (!ValidateEditableRow(row))
                            return;

                        UpdateRow(row);
                        SetRowEditable(row, false);
                        row.Cells["Edit"].Value = "Edit";
                        editingRowIndex = -1;
                        MessageBox.Show(
                            "Item updated ✅\n\n" +
                            "Nayi sale / exchange / label ab updated price & details use karenge.\n" +
                            "Purane bills ke amounts change nahi hote.");
                    }
                    return;
                }

                // PRINT — reprint label for already-added item
                if (colName == "Print")
                {
                    if (editingRowIndex == e.RowIndex)
                    {
                        MessageBox.Show("Pehle Save karein, phir Print.");
                        return;
                    }

                    var row = grid.Rows[e.RowIndex];

                    if (row.Cells["item_code"].Value == null)
                    {
                        MessageBox.Show("Invalid Item Code");
                        return;
                    }

                    _code = SafeText(row.Cells["item_code"].Value);
                    _size = SafeText(GetCellValue(row, "size"));
                    _price = SafeText(GetCellValue(row, "selling_price"));

                    if (string.IsNullOrWhiteSpace(_code))
                    {
                        MessageBox.Show("Barcode not found");
                        return;
                    }

                    GenerateBarcode(_code);
                    PrintLabel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Grid Error: " + ex.Message);
            }
        }

        void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (suppressPriceRecalc || isLoading || e.RowIndex < 0)
                return;
            if (e.RowIndex != editingRowIndex)
                return;

            string col = grid.Columns[e.ColumnIndex].Name;
            if (!string.Equals(col, "price_before_tax", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(col, "gst", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(col, "GST", StringComparison.OrdinalIgnoreCase))
                return;

            if (!grid.Columns.Contains("selling_price"))
                return;

            DataGridViewRow row = grid.Rows[e.RowIndex];
            if (!TryParseDecimal(GetCellValue(row, "price_before_tax"), out decimal pbt))
                return;
            if (!TryParseDecimal(GetGstCellValue(row), out decimal gstPct))
                return;

            // Same formula as AddItem.CalcPrice (integer GST on PBT).
            decimal gstAmount = Math.Floor(pbt * gstPct / 100m);
            decimal selling = Math.Round(pbt + gstAmount, 2, MidpointRounding.AwayFromZero);

            suppressPriceRecalc = true;
            try
            {
                row.Cells["selling_price"].Value = selling;
            }
            finally
            {
                suppressPriceRecalc = false;
            }
        }

        static object? GetCellValue(DataGridViewRow row, string columnName)
        {
            if (row.DataGridView == null)
                return null;
            if (!row.DataGridView.Columns.Contains(columnName))
                return null;
            return row.Cells[columnName].Value;
        }

        static string GetGstCellValue(DataGridViewRow row)
        {
            object? v = GetCellValue(row, "GST");
            if (v != null && !string.IsNullOrWhiteSpace(v.ToString()))
                return v.ToString() ?? "";
            v = GetCellValue(row, "gst");
            return v?.ToString() ?? "";
        }

        static bool TryParseDecimal(object? value, out decimal result)
        {
            result = 0;
            if (value == null || value == DBNull.Value)
                return false;
            return decimal.TryParse(
                value.ToString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out result)
                || decimal.TryParse(value.ToString(), out result);
        }

        bool ValidateEditableRow(DataGridViewRow row)
        {
            string name = SafeText(GetCellValue(row, "item_name"));
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Item name required.");
                return false;
            }

            if (!TryParseDecimal(GetCellValue(row, "selling_price"), out decimal sell) || sell < 0)
            {
                MessageBox.Show("Valid selling price required.");
                return false;
            }

            string gstRaw = GetGstCellValue(row);
            if (!string.IsNullOrWhiteSpace(gstRaw) &&
                (!TryParseDecimal(gstRaw, out decimal gst) || gst < 0 || gst > 100))
            {
                MessageBox.Show("GST must be between 0 and 100.");
                return false;
            }

            return true;
        }

        void UpdateRow(DataGridViewRow row)
        {
            int id = Convert.ToInt32(row.Cells["id"].Value);

            using MySqlConnection con = DB.GetConnection();
            con.Open();

            using MySqlCommand cmd = new MySqlCommand(@"
                UPDATE inv_items_master SET
                    item_name = @item_name,
                    main_category = @main_category,
                    sub_category = @sub_category,
                    gender = @gender,
                    item_type = @item_type,
                    actual_item = @actual_item,
                    size = @size,
                    color = @color,
                    cost_price = @cost_price,
                    price_before_tax = @price_before_tax,
                    selling_price = @selling_price,
                    brand = @brand,
                    supplier_name = @supplier_name,
                    GST = @gst
                WHERE id = @id", con);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@item_name", SafeText(GetCellValue(row, "item_name")));
            cmd.Parameters.AddWithValue("@main_category", SafeText(GetCellValue(row, "main_category")));
            cmd.Parameters.AddWithValue("@sub_category", SafeText(GetCellValue(row, "sub_category")));
            cmd.Parameters.AddWithValue("@gender", SafeText(GetCellValue(row, "gender")));
            cmd.Parameters.AddWithValue("@item_type", SafeText(GetCellValue(row, "item_type")));
            cmd.Parameters.AddWithValue("@actual_item", SafeText(GetCellValue(row, "actual_item")));
            cmd.Parameters.AddWithValue("@size", SafeText(GetCellValue(row, "size")));
            cmd.Parameters.AddWithValue("@color", SafeText(GetCellValue(row, "color")));
            cmd.Parameters.AddWithValue("@brand", SafeText(GetCellValue(row, "brand")));
            cmd.Parameters.AddWithValue("@supplier_name", SafeText(GetCellValue(row, "supplier_name")));

            TryParseDecimal(GetCellValue(row, "cost_price"), out decimal cost);
            TryParseDecimal(GetCellValue(row, "price_before_tax"), out decimal pbt);
            TryParseDecimal(GetCellValue(row, "selling_price"), out decimal sell);
            TryParseDecimal(GetGstCellValue(row), out decimal gst);

            cmd.Parameters.AddWithValue("@cost_price", cost);
            cmd.Parameters.AddWithValue("@price_before_tax", pbt);
            cmd.Parameters.AddWithValue("@selling_price", sell);
            cmd.Parameters.AddWithValue("@gst", gst);

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                throw new Exception("Update failed — item not found.");
        }

        //void GenerateBarcode(string text)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(text))
        //        {
        //            barcodeImage?.Dispose();
        //            barcodeImage = null;
        //            return;
        //        }

        //        var writer = new ZXing.BarcodeWriter<Bitmap>
        //        {
        //            Format = ZXing.BarcodeFormat.CODE_128,
        //            Options = new ZXing.Common.EncodingOptions
        //            {
        //                Width = 180,
        //                Height = 50,
        //                Margin = 1
        //            },
        //            Renderer = new ZXing.Windows.Compatibility.BitmapRenderer()
        //        };

        //        barcodeImage?.Dispose();
        //        barcodeImage = writer.Write(text.Trim());
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Barcode Error: " + ex.Message);
        //        barcodeImage = null;
        //    }
        //}

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
            catch
            {
                barcodeImage = null;
                MessageBox.Show("Barcode generation failed");
            }
        }

        void DrawLabel(Graphics g, Rectangle bounds)
        {
            g.Clear(Color.White);

            int pageWidth = bounds.Width;
            int topMargin = 20;

            Font textFont = new Font("Arial", 8, FontStyle.Bold);
            Brush brush = Brushes.Black;

            DateTime dt = DateTime.Parse(_date);

            string formattedDate = (dt.Year / 100).ToString("00") + dt.ToString("MMddyy");

            string firstLine = $"{formattedDate}  Size:{_size}  ₹{_price}";
            SizeF textSize = g.MeasureString(firstLine, textFont);

            float textX = (pageWidth - textSize.Width) / 2;
            float textY = topMargin;

            g.DrawString(firstLine, textFont, brush, textX, textY);

            if (barcodeImage != null)
            {
                int barcodeWidth = 180;
                int barcodeHeight = 45;

                float barcodeX = (pageWidth - barcodeWidth) / 2;
                float barcodeY = textY + textSize.Height + 4;

                g.DrawImage(barcodeImage, barcodeX, barcodeY, barcodeWidth, barcodeHeight);
            }
        }

        //void PrintLabel()
        //{
        //    PrintDialog pd = new PrintDialog();
        //    pd.Document = printDoc;
        //    pd.AllowSomePages = false;

        //    if (pd.ShowDialog() == DialogResult.OK)
        //    {
        //        printDoc.PrinterSettings = pd.PrinterSettings;

        //        // ✅ Printer copies (user selects here)
        //        // e.g. 5 copies → same label 5 times

        //        PaperSize labelSize = new PaperSize("Custom", 216, 98);

        //        printDoc.DefaultPageSettings.PaperSize = labelSize;
        //        printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        //        printDoc.OriginAtMargins = false;

        //        printDoc.Print();
        //    }
        //}

        void PrintLabel()
        {
            try
            {
                if (barcodeImage == null)
                {
                    MessageBox.Show("Nothing to print");
                    return;
                }

                int stockQty = GetStockQuantity(_code);
                string defaultQty = stockQty > 0 ? stockQty.ToString() : "1";

                string input = Interaction.InputBox(
                    "Enter label quantity\n\nStock quantity: " + stockQty,
                    "Print Labels",
                    defaultQty);

                if (string.IsNullOrWhiteSpace(input))
                    return;

                if (!int.TryParse(input.Trim(), out int qty) || qty <= 0)
                {
                    MessageBox.Show("Enter valid quantity.");
                    return;
                }

                if (qty > short.MaxValue)
                {
                    MessageBox.Show("Quantity too large.");
                    return;
                }

                PrinterRouting.ApplyLabelPrinter(printDoc);
                printDoc.PrinterSettings.Copies = (short)qty;

                PaperSize labelSize = new PaperSize("Custom", 216, 98);
                printDoc.DefaultPageSettings.PaperSize = labelSize;
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                printDoc.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Print Failed: " + ex.Message);
            }
        }

        int GetStockQuantity(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
                return 0;

            try
            {
                using MySqlConnection con = DB.GetConnection();
                con.Open();
                using MySqlCommand cmd = new MySqlCommand(@"
                    SELECT IFNULL(quantity, 0)
                    FROM inv_stock
                    WHERE LOWER(TRIM(item_code)) = LOWER(TRIM(@code))
                    LIMIT 1", con);
                cmd.Parameters.AddWithValue("@code", itemCode.Trim());
                object? result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return 0;
                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            e.Graphics.TranslateTransform(
                -e.PageSettings.HardMarginX,
                -e.PageSettings.HardMarginY);

            Rectangle printArea = new Rectangle(
                0,
                0,
                e.PageSettings.PaperSize.Width,
                e.PageSettings.PaperSize.Height);

            DrawLabel(e.Graphics, printArea);

            e.HasMorePages = false;
        }

        void LoadAllFilters()
        {
            try
            {
                isLoading = true;

                bool isMainAll = cmbMainCategory.Text == "All";

                // 🧠 OLD VALUES (sirf tab use honge jab Main != All)
                string selectedSub = isMainAll ? "All" : cmbSubCategory.Text;
                string selectedGender = isMainAll ? "All" : cmbGender.Text;
                string selectedType = isMainAll ? "All" : cmbItemType.Text;

                // ---------- SUB CATEGORY ----------
                cmbSubCategory.Items.Clear();
                cmbSubCategory.Items.Add("All");

                string subQuery = "SELECT DISTINCT sub_category FROM inv_items_master WHERE 1=1";

                if (!isMainAll)
                    subQuery += " AND main_category='" + cmbMainCategory.Text + "'";

                DataTable dtSub = DB.GetData(subQuery);

                foreach (DataRow r in dtSub.Rows)
                    cmbSubCategory.Items.Add(r[0].ToString());

                cmbSubCategory.Text = cmbSubCategory.Items.Contains(selectedSub) ? selectedSub : "All";


                // ---------- GENDER ----------
                cmbGender.Items.Clear();
                cmbGender.Items.Add("All");

                string genderQuery = "SELECT DISTINCT gender FROM inv_items_master WHERE 1=1";

                if (!isMainAll)
                    genderQuery += " AND main_category='" + cmbMainCategory.Text + "'";

                if (cmbSubCategory.Text != "All")
                    genderQuery += " AND sub_category='" + cmbSubCategory.Text + "'";

                DataTable dtGender = DB.GetData(genderQuery);

                foreach (DataRow r in dtGender.Rows)
                    cmbGender.Items.Add(r[0].ToString());

                cmbGender.Text = cmbGender.Items.Contains(selectedGender) ? selectedGender : "All";


                // ---------- ITEM TYPE ----------
                cmbItemType.Items.Clear();
                cmbItemType.Items.Add("All");

                string typeQuery = "SELECT DISTINCT item_type FROM inv_items_master WHERE 1=1";

                if (!isMainAll)
                    typeQuery += " AND main_category='" + cmbMainCategory.Text + "'";

                if (cmbSubCategory.Text != "All")
                    typeQuery += " AND sub_category='" + cmbSubCategory.Text + "'";

                if (cmbGender.Text != "All")
                    typeQuery += " AND gender='" + cmbGender.Text + "'";

                DataTable dtType = DB.GetData(typeQuery);

                foreach (DataRow r in dtType.Rows)
                    cmbItemType.Items.Add(r[0].ToString());

                cmbItemType.Text = cmbItemType.Items.Contains(selectedType) ? selectedType : "All";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Filter Load Error: " + ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        string SafeText(object value)
        {
            return value == null ? "" : value.ToString().Replace("'", "''");
        }

        void ResetFilters()
        {
            try
            {
                // 🔹 Search clear
                txtSearch.Text = "";

                // 🔹 Main Category reset
                if (cmbMainCategory.Items.Count > 0)
                    cmbMainCategory.SelectedIndex = 0;

                // 🔹 Reload all dependent dropdowns
                LoadAllFilters();

                // 🔹 Ensure all set to "All"
                if (cmbSubCategory.Items.Count > 0)
                    cmbSubCategory.SelectedIndex = 0;

                if (cmbGender.Items.Count > 0)
                    cmbGender.SelectedIndex = 0;

                if (cmbItemType.Items.Count > 0)
                    cmbItemType.SelectedIndex = 0;

                // 🔹 Load full data
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reset Error: " + ex.Message);
            }
        }


    }
}
