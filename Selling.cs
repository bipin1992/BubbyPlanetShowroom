using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Selling : UserControl
    {
        private static readonly Color PageBg = Color.FromArgb(241, 245, 249);
        private static readonly Color Slate = Color.FromArgb(15, 23, 42);
        private static readonly Color HeaderBg = Color.FromArgb(30, 41, 59);
        private static readonly Color Teal = Color.FromArgb(13, 148, 136);
        private static readonly Color Sky = Color.FromArgb(14, 165, 233);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);

        private Label lblTodaySale;
        private Label lblTodayOrders;

        private ComboBox cmbFilter;
        private ComboBox cmbUser;

        private DateTimePicker dtFrom;
        private DateTimePicker dtTo;

        private Button btnSearch;

        private DataGridView dgvOrders;
        private DataGridView dgvDetails;

        public Selling()
        {
            InitializeUI();

            Load += (s, e) =>
            {
                LoadUsers();
                LoadSummary();
                LoadOrders();
            };
        }

        private void InitializeUI()
        {
            Dock = DockStyle.Fill;
            BackColor = PageBg;
            Padding = new Padding(12);

            TableLayoutPanel main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = PageBg,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 78f));   // page header (title + subtitle)
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 118f));  // summary + filters
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));    // orders
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));    // details
            Controls.Add(main);

            // ----- Page header -----
            Panel header = CreateCard();
            header.Margin = new Padding(0, 0, 0, 8);
            header.Padding = new Padding(0);
            header.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using LinearGradientBrush brush = new LinearGradientBrush(
                    header.ClientRectangle, Teal, Sky, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brush, header.ClientRectangle);
                using Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "Selling Report", titleFont,
                    new Rectangle(16, 12, 320, 28), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                using Font hintFont = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(e.Graphics, "Filter sales  ·  open an order to view item details", hintFont,
                    new Rectangle(16, 40, 480, 20), Color.FromArgb(204, 251, 241),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };

            // ----- Summary + filters -----
            Panel topPanel = CreateCard();
            topPanel.Margin = new Padding(0, 0, 0, 10);
            topPanel.Padding = new Padding(12, 10, 12, 10);

            TableLayoutPanel topLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White
            };
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420f));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Summary cards
            TableLayoutPanel summaryRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 8, 0)
            };
            summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            lblTodaySale = new Label { Text = "Sale : ₹0.00" };
            lblTodayOrders = new Label { Text = "Orders : 0" };
            summaryRow.Controls.Add(CreateStatCard("TOTAL SALE", lblTodaySale, Color.FromArgb(167, 243, 208), Teal), 0, 0);
            summaryRow.Controls.Add(CreateStatCard("TOTAL ORDERS", lblTodayOrders, Color.FromArgb(186, 230, 253), Sky), 1, 0);

            // Filters
            Panel filterPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(12, 8, 12, 8) };

            Label lblPeriod = MakeTinyLabel("PERIOD", 8, 4);
            cmbFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(8, 24),
                Width = 110,
                Font = new Font("Segoe UI", 9.5f),
                FlatStyle = FlatStyle.Flat
            };
            cmbFilter.Items.AddRange(new object[] { "Today", "Weekly", "Monthly", "Yearly", "Custom" });
            cmbFilter.SelectedIndex = 0;

            Label lblUser = MakeTinyLabel("USER", 130, 4);
            cmbUser = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(130, 24),
                Width = 130,
                Font = new Font("Segoe UI", 9.5f),
                FlatStyle = FlatStyle.Flat
            };

            Label lblFrom = MakeTinyLabel("FROM", 272, 4);
            dtFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(272, 24),
                Width = 110,
                Font = new Font("Segoe UI", 9.5f)
            };

            Label lblTo = MakeTinyLabel("TO", 394, 4);
            dtTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(394, 24),
                Width = 110,
                Font = new Font("Segoe UI", 9.5f)
            };

            btnSearch = new Button
            {
                Text = "Search",
                Location = new Point(518, 20),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) =>
            {
                LoadSummary();
                LoadOrders();
            };

            filterPanel.Controls.Add(lblPeriod);
            filterPanel.Controls.Add(cmbFilter);
            filterPanel.Controls.Add(lblUser);
            filterPanel.Controls.Add(cmbUser);
            filterPanel.Controls.Add(lblFrom);
            filterPanel.Controls.Add(dtFrom);
            filterPanel.Controls.Add(lblTo);
            filterPanel.Controls.Add(dtTo);
            filterPanel.Controls.Add(btnSearch);

            topLayout.Controls.Add(summaryRow, 0, 0);
            topLayout.Controls.Add(filterPanel, 1, 0);
            topPanel.Controls.Add(topLayout);

            // ----- Orders grid card -----
            Panel ordersCard = CreateCard();
            ordersCard.Padding = new Padding(1);
            dgvOrders = CreateGrid();
            dgvOrders.Columns.Add("OrderId", "Order ID");
            dgvOrders.Columns.Add("Date", "Date");
            dgvOrders.Columns.Add("Customer", "Customer");
            dgvOrders.Columns.Add("Mobile", "Mobile");
            dgvOrders.Columns.Add("Payment", "Payment");
            dgvOrders.Columns.Add("User", "User");
            dgvOrders.Columns.Add("Amount", "Amount");
            dgvOrders.SelectionChanged += DgvOrders_SelectionChanged;
            Panel ordersHeaderBar = CreateSectionHeader("ORDERS", "Select a row to load item breakdown");
            ordersCard.Controls.Add(dgvOrders);
            ordersCard.Controls.Add(ordersHeaderBar);

            // ----- Details grid card -----
            Panel detailsCard = CreateCard();
            detailsCard.Margin = new Padding(0);
            detailsCard.Padding = new Padding(1);
            dgvDetails = CreateGrid();
            dgvDetails.Columns.Add("Item", "Item");
            dgvDetails.Columns.Add("Size", "Size");
            dgvDetails.Columns.Add("Qty", "Sold Qty");
            dgvDetails.Columns.Add("ReturnedQty", "Returned");
            dgvDetails.Columns.Add("NetQty", "Net Qty");
            dgvDetails.Columns.Add("Price", "Price");
            dgvDetails.Columns.Add("Gross", "Gross");
            dgvDetails.Columns.Add("Discount", "Discount");
            dgvDetails.Columns.Add("DiscountAmt", "Discount Amt");
            dgvDetails.Columns.Add("Taxable", "Taxable");
            dgvDetails.Columns.Add("GST", "GST");
            dgvDetails.Columns.Add("Total", "Total");
            Panel detailsHeaderBar = CreateSectionHeader("ORDER DETAILS", "Line items for the selected order");
            dgvDetails.Dock = DockStyle.Fill;
            detailsCard.Controls.Add(dgvDetails);
            detailsCard.Controls.Add(detailsHeaderBar);

            main.Controls.Add(header, 0, 0);
            main.Controls.Add(topPanel, 0, 1);
            main.Controls.Add(ordersCard, 0, 2);
            main.Controls.Add(detailsCard, 0, 3);
        }

        private static Panel CreateCard()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0)
            };
        }

        private static Panel CreateSectionHeader(string title, string hint)
        {
            Panel bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = HeaderBg,
                Padding = new Padding(12, 0, 12, 0)
            };
            Label lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Left,
                Width = 140,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblHint = new Label
            {
                Text = hint,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleRight
            };
            bar.Controls.Add(lblHint);
            bar.Controls.Add(lblTitle);
            return bar;
        }

        private static Panel CreateStatCard(string caption, Label valueLabel, Color bg, Color accent)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bg,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 10, 12, 10)
            };
            card.Paint += (_, e) =>
            {
                using SolidBrush bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, 0, 0, 4, card.Height);
            };

            Label lblCap = new Label
            {
                Text = caption,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Muted,
                BackColor = Color.Transparent
            };

            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            valueLabel.ForeColor = Slate;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.BackColor = Color.Transparent;

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblCap);
            return card;
        }

        private static Label MakeTinyLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(x, y),
                BackColor = Color.Transparent
            };
        }

        private static DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(226, 232, 240),
                RowTemplate = { Height = 32 },
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return grid;
        }

        private void LoadSummary()
        {
            try
            {
                using (MySqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    string where = GetDateCondition();

                    if (cmbUser.Text != "All Users")
                        where += " AND o.created_by=@user";

                    string query = @"
            SELECT
                COUNT(*) TotalOrders,
                IFNULL(SUM(grand_total),0) TotalSale
            FROM inv_orders o
            WHERE " + where;

                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    if (cmbUser.Text != "All Users")
                        cmd.Parameters.AddWithValue("@user", cmbUser.Text);

                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        lblTodayOrders.Text =
                            "Orders : " + reader["TotalOrders"];

                        lblTodaySale.Text =
                            "Sale : ₹" +
                            Convert.ToDecimal(reader["TotalSale"])
                            .ToString("N2");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadOrders()
        {
            dgvOrders.Rows.Clear();

            try
            {
                using (MySqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    string where = GetDateCondition();

                    if (cmbUser.Text != "All Users")
                        where += " AND o.created_by=@user";

                    string query = @"
            SELECT
                o.id,
                o.date_added,
                o.payment_method,
                o.grand_total,
                o.created_by,
                c.first_name,
                c.sur_name,
                c.phone
            FROM inv_orders o
            LEFT JOIN inv_customers c
                ON c.id=o.customer_id
            WHERE " + where + @"
            ORDER BY o.id DESC";

                    MySqlCommand cmd =
                        new MySqlCommand(query, conn);

                    if (cmbUser.Text != "All Users")
                        cmd.Parameters.AddWithValue("@user",
                            cmbUser.Text);

                    MySqlDataReader reader =
                        cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        dgvOrders.Rows.Add(
                            reader["id"],
                            Convert.ToDateTime(
                                reader["date_added"])
                            .ToString("dd-MM-yyyy HH:mm"),

                            reader["first_name"] + " " +
                            reader["sur_name"],

                            reader["phone"],

                            reader["payment_method"],

                            reader["created_by"],

                            reader["grand_total"]
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DgvOrders_SelectionChanged(
            object sender,
            EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
                return;

            int orderId =
                Convert.ToInt32(
                    dgvOrders.CurrentRow.Cells["OrderId"].Value);

            LoadOrderDetails(orderId);
        }

        private void LoadOrderDetails(int orderId)
        {
            dgvDetails.Rows.Clear();

            try
            {
                using (MySqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    string query = @"
            SELECT
                i.item_name,
                i.size,
                d.qty,
                IFNULL(d.return_qty,0) return_qty,
                (d.qty - IFNULL(d.return_qty,0)) net_qty,

                d.selling_price,
                d.gross_amount,

                IFNULL(d.discount_percent,0)
                    discount_percent,

                d.discount_amount,
                d.taxable_amount,
                d.gst_amount,
                d.net_amount

            FROM inv_order_details d
            INNER JOIN inv_items_master i
                ON i.id = d.item_id
            WHERE d.order_id = @orderId";

                    MySqlCommand cmd =
                        new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue(
                        "@orderId",
                        orderId);

                    MySqlDataReader reader =
                        cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        dgvDetails.Rows.Add(
                            reader["item_name"],          // Item
                            reader["size"],               // Size
                            reader["qty"],                // Qty
                            reader["return_qty"],         // Returned
                            reader["net_qty"],            // Net Qty

                            reader["selling_price"],      // Price
                            reader["gross_amount"],       // Gross

                            reader["discount_percent"] + "%",

                            reader["discount_amount"],    // Discount Amt
                            reader["taxable_amount"],     // Taxable
                            reader["gst_amount"],         // GST
                            reader["net_amount"]          // Net
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadUsers()
        {
            try
            {
                cmbUser.Items.Clear();

                cmbUser.Items.Add("All Users");

                using (MySqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    string query =
                        @"SELECT DISTINCT created_by
                  FROM inv_orders
                  ORDER BY created_by";

                    MySqlCommand cmd =
                        new MySqlCommand(query, conn);

                    MySqlDataReader reader =
                        cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        cmbUser.Items.Add(
                            reader["created_by"].ToString());
                    }
                }

                cmbUser.SelectedIndex = 0;
            }
            catch
            {
            }
        }

        private string GetDateCondition()
        {
            switch (cmbFilter.Text)
            {
                case "Today":
                    return "DATE(o.date_added)=CURDATE()";

                case "Weekly":
                    return "YEARWEEK(o.date_added)=YEARWEEK(CURDATE())";

                case "Monthly":
                    return "MONTH(o.date_added)=MONTH(CURDATE()) AND YEAR(o.date_added)=YEAR(CURDATE())";

                case "Yearly":
                    return "YEAR(o.date_added)=YEAR(CURDATE())";

                case "Custom":
                    return string.Format(
                        "DATE(o.date_added) BETWEEN '{0}' AND '{1}'",
                        dtFrom.Value.ToString("yyyy-MM-dd"),
                        dtTo.Value.ToString("yyyy-MM-dd"));

                default:
                    return "1=1";
            }
        }
    }
}
