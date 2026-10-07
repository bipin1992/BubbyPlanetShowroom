using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Every bill that has a return or an exchange, with item price.
    /// Menu is limited to Master Admin and Admin.
    /// </summary>
    public class ReturnBills : UserControl
    {
        private static readonly Color PageBg = Color.FromArgb(241, 245, 249);
        private static readonly Color Slate = Color.FromArgb(15, 23, 42);
        private static readonly Color HeaderBg = Color.FromArgb(30, 41, 59);
        private static readonly Color Teal = Color.FromArgb(13, 148, 136);
        private static readonly Color Sky = Color.FromArgb(14, 165, 233);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);

        private readonly bool allowed;

        private ComboBox cmbPeriod = null!;
        private ComboBox cmbUser = null!;
        private ComboBox cmbPayment = null!;
        private ComboBox cmbType = null!;
        private ComboBox cmbCategory = null!;
        private DateTimePicker dtFrom = null!;
        private DateTimePicker dtTo = null!;
        private TextBox txtSearch = null!;
        private bool applyingFilter;

        private Label lblBills = null!;
        private Label lblPieces = null!;
        private Label lblReturnValue = null!;
        private Label lblSettlement = null!;
        private Label lblBillsHint = null!;
        private Label lblDetailsHint = null!;

        private DataGridView dgvBills = null!;
        private DataGridView dgvDetails = null!;
        private bool loadingBills;

        public ReturnBills(string role = "")
        {
            allowed = string.Equals(role, "Master Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!allowed)
            {
                BuildDenied();
                return;
            }

            BuildPage();
            Load += (_, _) =>
            {
                applyingFilter = true;
                LoadUsers();
                LoadCategories();
                SyncDatePickersFromPeriod();
                applyingFilter = false;
                ApplyFilter();
            };
        }

        private void BuildDenied()
        {
            Dock = DockStyle.Fill;
            BackColor = PageBg;
            Controls.Add(new Label
            {
                Text = "Return Bills is available only to Master Admin and Admin.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Slate
            });
        }

        private void BuildPage()
        {
            Dock = DockStyle.Fill;
            BackColor = PageBg;
            Padding = new Padding(12);

            TableLayoutPanel main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = PageBg
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 96f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 132f));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
            Controls.Add(main);

            Panel header = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
            header.Paint += (_, e) =>
            {
                Rectangle bounds = header.ClientRectangle;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using LinearGradientBrush brush = new LinearGradientBrush(bounds, Teal, Sky, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brush, bounds);
                using Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "Return Bills", titleFont,
                    new Rectangle(16, 10, 360, 28), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                using Font hintFont = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(
                    e.Graphics,
                    "Master Admin and Admin  ·  returned and exchange items with price",
                    hintFont,
                    new Rectangle(16, 38, 640, 20),
                    Color.FromArgb(204, 251, 241),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };

            Panel summary = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(8, 8, 8, 8)
            };
            TableLayoutPanel summaryRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.White
            };
            for (int i = 0; i < 4; i++)
                summaryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            lblBills = new Label { Text = "0" };
            lblPieces = new Label { Text = "0" };
            lblReturnValue = new Label { Text = "₹0.00" };
            lblSettlement = new Label { Text = "₹0.00" };
            summaryRow.Controls.Add(Stat("RETURN AMOUNT", lblReturnValue, Color.FromArgb(254, 226, 226), Color.FromArgb(220, 38, 38)), 0, 0);
            summaryRow.Controls.Add(Stat("EXCHANGE PRICE", lblPieces, Color.FromArgb(254, 243, 199), Color.FromArgb(217, 119, 6)), 1, 0);
            summaryRow.Controls.Add(Stat("EXCHANGE − RETURN", lblSettlement, Color.FromArgb(186, 230, 253), Sky), 2, 0);
            summaryRow.Controls.Add(Stat("BILLS", lblBills, Color.FromArgb(167, 243, 208), Teal), 3, 0);
            summary.Controls.Add(summaryRow);

            Panel filterCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(10, 8, 10, 4)
            };
            FlowLayoutPanel filters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                AutoScroll = false
            };

            cmbPeriod = DropDown(148);
            cmbPeriod.Items.AddRange(new object[]
            {
                "All", "Today", "Last 7 Days", "Weekly", "Monthly", "Yearly", "Till Date", "Custom"
            });
            cmbPeriod.SelectedIndex = 1;
            cmbPeriod.SelectedIndexChanged += (_, _) => PeriodChanged();

            cmbUser = DropDown(140);
            cmbUser.SelectedIndexChanged += (_, _) => FilterChanged();

            cmbPayment = DropDown(110);
            cmbPayment.Items.AddRange(new object[] { "All", "Cash", "Online" });
            cmbPayment.SelectedIndex = 0;
            cmbPayment.SelectedIndexChanged += (_, _) => FilterChanged();

            cmbType = DropDown(120);
            cmbType.Items.AddRange(new object[] { "All", "Return", "Exchange" });
            cmbType.SelectedIndex = 0;
            cmbType.SelectedIndexChanged += (_, _) => FilterChanged();

            cmbCategory = DropDown(150);
            cmbCategory.Items.Add("All");
            cmbCategory.SelectedIndex = 0;
            cmbCategory.SelectedIndexChanged += (_, _) => FilterChanged();

            dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 118, Font = new Font("Segoe UI", 9.5f) };
            dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 118, Font = new Font("Segoe UI", 9.5f) };
            dtFrom.ValueChanged += DatePickerChanged;
            dtTo.ValueChanged += DatePickerChanged;

            txtSearch = new TextBox
            {
                Width = 220,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Bill, name, phone, item"
            };
            txtSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplyFilter();
                }
            };

            Button btnSearch = new Button
            {
                Text = "Search",
                Size = new Size(96, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (_, _) => ApplyFilter();

            filters.Controls.Add(Field("PERIOD", cmbPeriod, 156));
            filters.Controls.Add(Field("USER", cmbUser, 148));
            filters.Controls.Add(Field("PAYMENT", cmbPayment, 118));
            filters.Controls.Add(Field("TYPE", cmbType, 128));
            filters.Controls.Add(Field("CATEGORY", cmbCategory, 158));
            filters.Controls.Add(Field("FROM", dtFrom, 126));
            filters.Controls.Add(Field("TO", dtTo, 126));
            filters.Controls.Add(Field("SEARCH", txtSearch, 228));
            filters.Controls.Add(Field("", btnSearch, 104));
            filterCard.Controls.Add(filters);

            Panel billsCard = Card();
            dgvBills = Grid();
            dgvBills.Columns.Add("BillNo", "Bill");
            dgvBills.Columns.Add("BillDate", "Bill Date");
            dgvBills.Columns.Add("ReturnDate", "Return");
            dgvBills.Columns.Add("Customer", "Customer");
            dgvBills.Columns.Add("Mobile", "Mobile");
            dgvBills.Columns.Add("Payment", "Pay");
            dgvBills.Columns.Add("Type", "Type");
            dgvBills.Columns.Add("Discount", "Disc");
            dgvBills.Columns.Add("Coupon", "Coupon");
            dgvBills.Columns.Add("BilledBy", "User");
            dgvBills.Columns.Add("Returned", "Qty");
            dgvBills.Columns.Add("ReturnValue", "Return ₹");
            dgvBills.Columns.Add("ExchangePrice", "Exchange ₹");
            dgvBills.Columns.Add("Refund", "Refund");
            dgvBills.Columns.Add("Collect", "Collect");
            dgvBills.Columns.Add("Remaining", "Now");
            dgvBills.Resize += (_, _) => FitColumns(dgvBills);
            foreach (string money in new[] { "Returned", "ReturnValue", "ExchangePrice", "Refund", "Collect", "Remaining" })
                dgvBills.Columns[money].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvBills.SelectionChanged += (_, _) => LoadSelectedDetails();
            Panel billsHeader = Section("RETURN BILLS", "Filter ki saari return bills", out lblBillsHint);
            billsCard.Controls.Add(dgvBills);
            billsCard.Controls.Add(billsHeader);

            Panel detailsCard = Card();
            detailsCard.Margin = new Padding(0);
            dgvDetails = Grid();
            dgvDetails.Columns.Add("LineType", "Type");
            dgvDetails.Columns.Add("Item", "Item");
            dgvDetails.Columns.Add("Code", "Code");
            dgvDetails.Columns.Add("Size", "Size");
            dgvDetails.Columns.Add("Category", "Category");
            dgvDetails.Columns.Add("Price", "Price");
            dgvDetails.Columns.Add("Qty", "Sold");
            dgvDetails.Columns.Add("Returned", "Back");
            dgvDetails.Columns.Add("NetQty", "Left");
            dgvDetails.Columns.Add("Discount", "Disc");
            dgvDetails.Columns.Add("Net", "Left ₹");
            dgvDetails.Columns.Add("ReturnValue", "Return ₹");
            dgvDetails.Columns.Add("ExchangePrice", "Exchange ₹");
            dgvDetails.Resize += (_, _) => FitColumns(dgvDetails);
            foreach (string money in new[] { "Price", "Net", "ReturnValue", "ExchangePrice" })
                dgvDetails.Columns[money].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            Panel detailsHeader = Section("ITEM DETAILS", "Return amount and the price of the item taken in exchange", out lblDetailsHint);
            detailsCard.Controls.Add(dgvDetails);
            detailsCard.Controls.Add(detailsHeader);

            main.Controls.Add(header, 0, 0);
            main.Controls.Add(summary, 0, 1);
            main.Controls.Add(filterCard, 0, 2);
            main.Controls.Add(billsCard, 0, 3);
            main.Controls.Add(detailsCard, 0, 4);
        }

        private void PeriodChanged()
        {
            if (applyingFilter)
                return;
            applyingFilter = true;
            SyncDatePickersFromPeriod();
            applyingFilter = false;
            ApplyFilter();
        }

        private void FilterChanged()
        {
            if (!applyingFilter)
                ApplyFilter();
        }

        private void DatePickerChanged(object? sender, EventArgs e)
        {
            if (applyingFilter)
                return;
            applyingFilter = true;
            if (cmbPeriod.Text != "Custom")
                cmbPeriod.SelectedItem = "Custom";
            applyingFilter = false;
            ApplyFilter();
        }

        private void SyncDatePickersFromPeriod()
        {
            dtFrom.Enabled = true;
            dtTo.Enabled = true;

            if (cmbPeriod.Text == "Custom")
                return;

            if (cmbPeriod.Text is "All" or "Till Date")
            {
                dtFrom.Value = new DateTime(2020, 1, 1);
                dtTo.Value = DateTime.Today;
                return;
            }

            SellingCalculations.ResolveFilterRange(
                cmbPeriod.Text,
                DateTime.Today,
                dtFrom.Value,
                dtTo.Value,
                out DateTime from,
                out DateTime to);
            dtFrom.Value = from;
            dtTo.Value = to;
        }

        private void ApplyFilter()
        {
            if (!allowed)
                return;
            LoadBills();
        }

        private void AddDateParams(MySqlCommand cmd)
        {
            DateTime from = dtFrom.Value.Date;
            DateTime to = dtTo.Value.Date;
            if (from > to)
            {
                DateTime swap = from;
                from = to;
                to = swap;
            }

            cmd.Parameters.AddWithValue("@fromDate", from);
            cmd.Parameters.AddWithValue("@toDateExclusive", SellingCalculations.RangeEndExclusive(to));
        }

        private string BuildWhere(MySqlCommand cmd)
        {
            var parts = new List<string>
            {
                "(IFNULL(ret.returned_qty,0) > 0 OR IFNULL(ex.exchange_lines,0) > 0 OR st.order_id IS NOT NULL)",
                SellingCalculations.SqlDateTimeInFilter("o.date_updated")
            };

            if (cmbUser.Text.Length > 0 && cmbUser.Text != "All Users")
            {
                parts.Add("o.created_by = @user");
                cmd.Parameters.AddWithValue("@user", cmbUser.Text);
            }

            if (cmbPayment.Text is "Cash" or "Online")
            {
                parts.Add("LOWER(TRIM(IFNULL(o.payment_method,'Cash'))) = @pay");
                cmd.Parameters.AddWithValue("@pay", cmbPayment.Text.ToLowerInvariant());
            }

            if (cmbType.Text == "Return")
                parts.Add("IFNULL(ret.returned_qty,0) > 0");
            else if (cmbType.Text == "Exchange")
                parts.Add("IFNULL(ex.exchange_lines,0) > 0");

            if (cmbCategory.Text.Length > 0 && cmbCategory.Text != "All")
            {
                parts.Add(@"EXISTS (
                    SELECT 1
                    FROM inv_order_details dc
                    INNER JOIN inv_items_master ic ON ic.id = dc.item_id
                    WHERE dc.order_id = o.id
                      AND IFNULL(ic.main_category,'') = @cat
                      AND (IFNULL(dc.return_qty,0) > 0 OR IFNULL(dc.is_exchange,0) = 1)
                )");
                cmd.Parameters.AddWithValue("@cat", cmbCategory.Text);
            }

            string search = (txtSearch.Text ?? "").Trim();
            if (search.Length > 0)
            {
                parts.Add(@"(
                    CAST(o.id AS CHAR) LIKE @q
                    OR CONCAT(IFNULL(c.first_name,''),' ',IFNULL(c.sur_name,'')) LIKE @q
                    OR IFNULL(c.phone,'') LIKE @q
                    OR IFNULL(o.created_by,'') LIKE @q
                    OR IFNULL(o.coupon_code,'') LIKE @q
                    OR EXISTS (
                        SELECT 1
                        FROM inv_order_details ds
                        INNER JOIN inv_items_master im ON im.id = ds.item_id
                        WHERE ds.order_id = o.id
                          AND (
                              im.item_name LIKE @q
                              OR im.item_code LIKE @q
                              OR IFNULL(im.size,'') LIKE @q
                          )
                    )
                )");
                cmd.Parameters.AddWithValue("@q", "%" + search + "%");
            }

            AddDateParams(cmd);
            return string.Join(" AND ", parts);
        }

        private const string FromSql = @"
FROM inv_orders o
LEFT JOIN inv_customers c ON c.id = o.customer_id
LEFT JOIN (
    SELECT
        order_id,
        SUM(IFNULL(return_qty,0)) AS returned_qty,
        GROUP_CONCAT(ROUND(IFNULL(discount_percent,0), 1) SEPARATOR ',') AS discount_pcts
    FROM inv_order_details
    GROUP BY order_id
) ret ON ret.order_id = o.id
LEFT JOIN (
    SELECT order_id,
           SUM(CASE WHEN IFNULL(is_exchange,0) = 1 THEN 1 ELSE 0 END) AS exchange_lines
    FROM inv_order_details
    GROUP BY order_id
) ex ON ex.order_id = o.id
LEFT JOIN (
    SELECT
        order_id,
        SUM(CASE WHEN LOWER(settlement_type) = 'refund' THEN amount ELSE 0 END) AS refund_amt,
        SUM(CASE WHEN LOWER(settlement_type) = 'collect' THEN amount ELSE 0 END) AS collect_amt
    FROM inv_return_settlements
    GROUP BY order_id
) st ON st.order_id = o.id
";

        private void LoadBills()
        {
            loadingBills = true;
            dgvBills.Rows.Clear();
            dgvDetails.Rows.Clear();
            if (lblDetailsHint != null)
                lblDetailsHint.Text = "Select a bill to see item price";

            var headers = new List<BillRow>();
            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureOrderCouponColumns(conn);
                DB.EnsureReturnSettlementSchema(conn);
                DB.EnsureColumnExists(conn, "inv_order_details", "is_exchange", "TINYINT(1) NOT NULL DEFAULT 0");

                using MySqlCommand cmd = conn.CreateCommand();
                string where = BuildWhere(cmd);
                cmd.CommandText = @"
SELECT
    o.id,
    o.date_added,
    o.date_updated,
    IFNULL(o.payment_method,'Cash') AS payment_method,
    IFNULL(o.grand_total,0) AS grand_total,
    IFNULL(NULLIF(TRIM(o.created_by), ''), '') AS created_by,
    IFNULL(o.coupon_code,'') AS coupon_code,
    IFNULL(o.coupon_discount,0) AS coupon_discount,
    IFNULL(c.first_name,'') AS first_name,
    IFNULL(c.sur_name,'') AS sur_name,
    IFNULL(c.phone,'') AS phone,
    IFNULL(ret.returned_qty,0) AS returned_qty,
    IFNULL(ret.discount_pcts,'') AS discount_pcts,
    IFNULL(ex.exchange_lines,0) AS exchange_lines,
    IFNULL(st.refund_amt,0) AS refund_amt,
    IFNULL(st.collect_amt,0) AS collect_amt
" + FromSql + " WHERE " + where + " ORDER BY o.date_updated DESC, o.id DESC";

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        headers.Add(new BillRow
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BillDate = reader["date_added"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["date_added"]),
                            ReturnDate = reader["date_updated"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["date_updated"]),
                            Payment = reader["payment_method"]?.ToString() ?? "Cash",
                            Grand = Money(reader["grand_total"]),
                            BilledBy = reader["created_by"]?.ToString() ?? "",
                            CouponCode = CouponCalculations.NormalizeCode(reader["coupon_code"]?.ToString()),
                            CouponDiscount = Money(reader["coupon_discount"]),
                            Customer = (reader["first_name"]?.ToString() + " " + reader["sur_name"]?.ToString()).Trim(),
                            Phone = reader["phone"]?.ToString() ?? "",
                            ReturnedQty = Convert.ToInt32(reader["returned_qty"]),
                            DiscountPcts = reader["discount_pcts"]?.ToString() ?? "",
                            ExchangeLines = Convert.ToInt32(reader["exchange_lines"]),
                            Refund = Money(reader["refund_amt"]),
                            Collect = Money(reader["collect_amt"])
                        });
                    }
                }

                FillReturnValues(conn, headers);
            }
            catch (Exception ex)
            {
                loadingBills = false;
                MessageBox.Show(ex.Message, "Return Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal returnValue = 0;
            decimal exchangePrice = 0;
            foreach (BillRow bill in headers)
            {
                string type = bill.ExchangeLines > 0 && bill.ReturnedQty > 0
                    ? "Both"
                    : bill.ExchangeLines > 0 ? "Exchange" : "Return";
                string coupon = string.IsNullOrWhiteSpace(bill.CouponCode)
                    ? (bill.CouponDiscount > 0 ? "₹" + bill.CouponDiscount.ToString("0.00") : "—")
                    : bill.CouponCode + (bill.CouponDiscount > 0 ? "  ₹" + bill.CouponDiscount.ToString("0.00") : "");
                string billedBy = string.IsNullOrWhiteSpace(bill.BilledBy) ? "—" : bill.BilledBy;

                int rowIndex = dgvBills.Rows.Add(
                    bill.Id,
                    bill.BillDate == DateTime.MinValue ? "" : bill.BillDate.ToString("dd-MM-yy HH:mm"),
                    bill.ReturnDate == DateTime.MinValue ? "" : bill.ReturnDate.ToString("dd-MM-yy HH:mm"),
                    bill.Customer,
                    bill.Phone,
                    bill.Payment,
                    type,
                    SellingCalculations.FormatDiscountPercentLabel(bill.DiscountPcts),
                    coupon,
                    billedBy,
                    bill.ReturnedQty,
                    bill.ReturnValue.ToString("N2"),
                    bill.ExchangePrice.ToString("N2"),
                    bill.Refund.ToString("N2"),
                    bill.Collect.ToString("N2"),
                    bill.Grand.ToString("N2"));

                if (bill.ReturnedQty > 0)
                    dgvBills.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 241, 242);

                returnValue += bill.ReturnValue;
                exchangePrice += bill.ExchangePrice;
            }

            decimal gap = ReturnCalculations.Round2(exchangePrice - returnValue);
            lblBills.Text = headers.Count.ToString("N0");
            lblPieces.Text = "₹" + exchangePrice.ToString("N2");
            lblReturnValue.Text = "₹" + returnValue.ToString("N2");
            lblSettlement.Text = (gap > 0 ? "+ ₹" : gap < 0 ? "− ₹" : "₹") + Math.Abs(gap).ToString("N2");
            if (lblBillsHint != null)
            {
                DateTime from = dtFrom.Value.Date;
                DateTime to = dtTo.Value.Date;
                if (from > to)
                {
                    DateTime swap = from;
                    from = to;
                    to = swap;
                }

                lblBillsHint.Text = from.ToString("dd-MM-yyyy")
                    + (from == to ? "" : " to " + to.ToString("dd-MM-yyyy"))
                    + "  ·  " + headers.Count + " return bills";
            }

            FitColumns(dgvBills);
            loadingBills = false;
            if (dgvBills.Rows.Count > 0)
            {
                dgvBills.ClearSelection();
                dgvBills.Rows[0].Selected = true;
                LoadDetails(headers[0].Id);
            }
        }

        private static void FillReturnValues(MySqlConnection conn, List<BillRow> headers)
        {
            if (headers.Count == 0)
                return;

            var byId = new Dictionary<int, BillRow>();
            var names = new List<string>();
            using MySqlCommand cmd = conn.CreateCommand();
            for (int i = 0; i < headers.Count; i++)
            {
                string name = "@oid" + i.ToString(CultureInfo.InvariantCulture);
                names.Add(name);
                cmd.Parameters.AddWithValue(name, headers[i].Id);
                byId[headers[i].Id] = headers[i];
            }

            cmd.CommandText = @"
SELECT
    d.order_id,
    d.qty,
    IFNULL(d.return_qty,0) AS return_qty,
    d.selling_price,
    IFNULL(d.discount_percent,0) AS discount_percent,
    IFNULL(d.net_amount,0) AS net_amount,
    IFNULL(d.is_exchange,0) AS is_exchange,
    IFNULL(i.GST,0) AS gst_percent
FROM inv_order_details d
INNER JOIN inv_items_master i ON i.id = d.item_id
WHERE d.order_id IN (" + string.Join(",", names) + ")";

            using MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int orderId = Convert.ToInt32(reader["order_id"]);
                if (!byId.TryGetValue(orderId, out BillRow? bill))
                    continue;

                int qty = Convert.ToInt32(reader["qty"]);
                int returned = Convert.ToInt32(reader["return_qty"]);
                bool exchange = Convert.ToInt32(reader["is_exchange"]) != 0;
                decimal price = Money(reader["selling_price"]);
                decimal gst = Money(reader["gst_percent"]);
                decimal disc = Money(reader["discount_percent"]);
                decimal net = Money(reader["net_amount"]);
                decimal returnedValue = ReturnCalculations.ReturnedPiecesValue(price, gst, disc, qty, returned, net);
                if (exchange)
                {
                    bill.ExchangePrice += ReturnCalculations.ExchangeTakenValue(price, gst, disc, qty, returned, net);
                }

                bill.ReturnValue += returnedValue;
            }

            foreach (BillRow bill in headers)
            {
                bill.ReturnValue = ReturnCalculations.Round2(bill.ReturnValue);
                bill.ExchangePrice = ReturnCalculations.Round2(bill.ExchangePrice);
            }
        }

        private void LoadSelectedDetails()
        {
            if (loadingBills)
                return;
            if (dgvBills.CurrentRow == null || dgvBills.CurrentRow.IsNewRow)
                return;
            if (dgvBills.CurrentRow.Cells["BillNo"].Value == null)
                return;
            if (!int.TryParse(dgvBills.CurrentRow.Cells["BillNo"].Value.ToString(), out int orderId))
                return;

            LoadDetails(orderId);
        }

        private void LoadDetails(int orderId)
        {
            dgvDetails.Rows.Clear();
            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureOrderCouponColumns(conn);
                DB.EnsureReturnSettlementSchema(conn);

                string couponCode = "";
                decimal couponDisc = 0;
                using (MySqlCommand couponCmd = new MySqlCommand(@"
SELECT IFNULL(coupon_code,''), IFNULL(coupon_discount,0)
FROM inv_orders WHERE id=@id LIMIT 1", conn))
                {
                    couponCmd.Parameters.AddWithValue("@id", orderId);
                    using MySqlDataReader couponReader = couponCmd.ExecuteReader();
                    if (couponReader.Read())
                    {
                        couponCode = CouponCalculations.NormalizeCode(couponReader.GetValue(0)?.ToString());
                        couponDisc = Money(couponReader.GetValue(1));
                    }
                }

                var settlements = new List<string>();
                using (MySqlCommand settleCmd = new MySqlCommand(@"
SELECT settlement_type, payment_method, amount, created_at
FROM inv_return_settlements
WHERE order_id=@id
ORDER BY id", conn))
                {
                    settleCmd.Parameters.AddWithValue("@id", orderId);
                    using MySqlDataReader settleReader = settleCmd.ExecuteReader();
                    while (settleReader.Read())
                    {
                        string kind = settleReader["settlement_type"]?.ToString() ?? "";
                        string method = settleReader["payment_method"]?.ToString() ?? "";
                        decimal amount = Money(settleReader["amount"]);
                        DateTime at = settleReader["created_at"] == DBNull.Value
                            ? DateTime.MinValue
                            : Convert.ToDateTime(settleReader["created_at"]);
                        string when = at == DateTime.MinValue ? "" : " on " + at.ToString("dd-MM-yyyy HH:mm");
                        settlements.Add(kind + " " + method + " ₹" + amount.ToString("0.00") + when);
                    }
                }

                using MySqlCommand cmd = new MySqlCommand(@"
SELECT
    i.item_name,
    i.item_code,
    IFNULL(i.size,'') AS size,
    IFNULL(i.main_category,'') AS main_category,
    IFNULL(i.GST,0) AS gst_percent,
    d.qty,
    IFNULL(d.return_qty,0) AS return_qty,
    d.selling_price,
    IFNULL(d.discount_percent,0) AS discount_percent,
    d.gross_amount,
    d.discount_amount,
    d.taxable_amount,
    d.gst_amount,
    d.net_amount,
    IFNULL(d.is_exchange,0) AS is_exchange
FROM inv_order_details d
INNER JOIN inv_items_master i ON i.id = d.item_id
WHERE d.order_id=@id
ORDER BY d.id", conn);
                cmd.Parameters.AddWithValue("@id", orderId);
                decimal billReturn = 0;
                decimal billExchange = 0;
                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int qty = Convert.ToInt32(reader["qty"]);
                    int returned = Convert.ToInt32(reader["return_qty"]);
                    int left = qty - returned;
                    if (left < 0)
                        left = 0;
                    bool exchange = Convert.ToInt32(reader["is_exchange"]) != 0;
                    decimal price = Money(reader["selling_price"]);
                    decimal discPct = Money(reader["discount_percent"]);
                    decimal gstPct = Money(reader["gst_percent"]);
                    decimal net = Money(reader["net_amount"]);
                    decimal returnValue = ReturnCalculations.ReturnedPiecesValue(
                        price, gstPct, discPct, qty, returned, net);
                    decimal exchangePrice = exchange
                        ? ReturnCalculations.ExchangeTakenValue(price, gstPct, discPct, qty, returned, net)
                        : 0m;
                    billReturn += returnValue;
                    billExchange += exchangePrice;

                    string lineType = exchange
                        ? "Exchange"
                        : returned > 0 ? "Returned" : "Kept";

                    int rowIndex = dgvDetails.Rows.Add(
                        lineType,
                        reader["item_name"],
                        reader["item_code"],
                        reader["size"],
                        reader["main_category"],
                        price.ToString("0.00"),
                        qty,
                        returned,
                        left,
                        discPct.ToString("0.##") + "%",
                        net.ToString("0.00"),
                        returnValue.ToString("0.00"),
                        exchangePrice.ToString("0.00"));

                    if (exchange)
                        dgvDetails.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                    else if (returned > 0)
                        dgvDetails.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                }

                string hint = "Return amount ₹" + billReturn.ToString("N2")
                    + "   ·   Exchange price ₹" + billExchange.ToString("N2");
                if (!string.IsNullOrWhiteSpace(couponCode))
                    hint += "  Coupon " + couponCode + " -₹" + couponDisc.ToString("0.00") + ".";
                if (settlements.Count > 0)
                    hint += "  " + string.Join("  |  ", settlements);
                if (lblDetailsHint != null)
                    lblDetailsHint.Text = hint;
                FitColumns(dgvDetails);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Return Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadUsers()
        {
            string keep = cmbUser.Text;
            cmbUser.Items.Clear();
            cmbUser.Items.Add("All Users");
            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                using MySqlCommand cmd = new MySqlCommand(
                    "SELECT DISTINCT created_by FROM inv_orders WHERE created_by IS NOT NULL AND TRIM(created_by) <> '' ORDER BY created_by",
                    conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string name = reader["created_by"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(name))
                        cmbUser.Items.Add(name);
                }
            }
            catch
            {
            }

            int index = cmbUser.Items.IndexOf(keep);
            cmbUser.SelectedIndex = index >= 0 ? index : 0;
        }

        private void LoadCategories()
        {
            string keep = cmbCategory.Text;
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("All");
            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                using MySqlCommand cmd = new MySqlCommand(@"
SELECT DISTINCT main_category
FROM inv_items_master
WHERE main_category IS NOT NULL AND TRIM(main_category) <> ''
ORDER BY main_category", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string name = reader["main_category"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(name))
                        cmbCategory.Items.Add(name);
                }
            }
            catch
            {
            }

            int index = cmbCategory.Items.IndexOf(string.IsNullOrWhiteSpace(keep) ? "All" : keep);
            cmbCategory.SelectedIndex = index >= 0 ? index : 0;
        }

        private static decimal Money(object? value)
        {
            return ReturnCalculations.Round2(CouponCalculations.ToAmount(value));
        }

        private static ComboBox DropDown(int width)
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = width,
                Font = new Font("Segoe UI", 9.5f),
                FlatStyle = FlatStyle.Flat
            };
        }

        private static Panel Field(string caption, Control input, int width)
        {
            Panel panel = new Panel
            {
                Width = width,
                Height = 52,
                Margin = new Padding(0, 0, 12, 4),
                BackColor = Color.White
            };
            if (caption.Length > 0)
            {
                panel.Controls.Add(new Label
                {
                    Text = caption,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = Muted,
                    AutoSize = true,
                    Location = new Point(0, 0)
                });
                input.Location = new Point(0, 18);
            }
            else
            {
                input.Location = new Point(0, 18);
            }

            input.Width = Math.Max(40, width - 4);
            panel.Controls.Add(input);
            return panel;
        }

        private static Panel Card()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(1)
            };
        }

        private static Panel Section(string title, string hint, out Label hintLabel)
        {
            Panel bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = HeaderBg,
                Padding = new Padding(12, 0, 12, 0)
            };
            bar.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Left,
                Width = 150,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            });
            hintLabel = new Label
            {
                Text = hint,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleRight
            };
            bar.Controls.Add(hintLabel);
            return bar;
        }

        private static Panel Stat(string caption, Label valueLabel, Color bg, Color accent)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bg,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 6, 8, 6)
            };
            card.Paint += (_, e) =>
            {
                using SolidBrush bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, 0, 0, 4, card.Height);
            };
            card.Controls.Add(new Label
            {
                Text = caption,
                Dock = DockStyle.Top,
                Height = 16,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted,
                BackColor = Color.Transparent
            });
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            valueLabel.ForeColor = Slate;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.BackColor = Color.Transparent;
            valueLabel.AutoEllipsis = false;
            valueLabel.Padding = new Padding(6, 0, 4, 0);
            card.Controls.Add(valueLabel);
            return card;
        }

        private static DataGridView Grid()
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(226, 232, 240),
                ScrollBars = ScrollBars.Vertical,
                RowTemplate = { Height = 30 },
                ColumnHeadersHeight = 34,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return grid;
        }

        private bool fittingColumns;

        private void FitColumns(DataGridView grid)
        {
            if (fittingColumns || grid.Columns.Count == 0 || grid.ClientSize.Width < 80)
                return;

            fittingColumns = true;
            try
            {
                int available = grid.ClientSize.Width;
                int rowsHeight = grid.ColumnHeadersHeight;
                foreach (DataGridViewRow row in grid.Rows)
                    rowsHeight += row.Height;
                if (rowsHeight > grid.ClientSize.Height)
                    available -= SystemInformation.VerticalScrollBarWidth;
                if (available < 80)
                    return;

                int weightTotal = 0;
                foreach (DataGridViewColumn col in grid.Columns)
                    weightTotal += ColumnWeight(col.Name);
                if (weightTotal <= 0)
                    return;

                int used = 0;
                for (int i = 0; i < grid.Columns.Count; i++)
                {
                    DataGridViewColumn col = grid.Columns[i];
                    int width = available * ColumnWeight(col.Name) / weightTotal;
                    if (i == grid.Columns.Count - 1)
                        width = Math.Max(1, available - used);
                    col.Width = width;
                    used += col.Width;
                }
            }
            finally
            {
                fittingColumns = false;
            }
        }

        private static int ColumnWeight(string name) => name switch
        {
            "Customer" or "Item" => 16,
            "BillDate" or "ReturnDate" or "Mobile" or "BilledBy" or "Coupon" or "Category" or "Code" => 10,
            "ReturnValue" or "ExchangePrice" => 9,
            _ => 6
        };

        private sealed class BillRow
        {
            public int Id { get; init; }
            public DateTime BillDate { get; init; }
            public DateTime ReturnDate { get; init; }
            public string Payment { get; init; } = "";
            public decimal Grand { get; init; }
            public string BilledBy { get; init; } = "";
            public string CouponCode { get; init; } = "";
            public decimal CouponDiscount { get; init; }
            public string Customer { get; init; } = "";
            public string Phone { get; init; } = "";
            public int ReturnedQty { get; init; }
            public string DiscountPcts { get; init; } = "";
            public int ExchangeLines { get; init; }
            public decimal Refund { get; init; }
            public decimal Collect { get; init; }
            public decimal ReturnValue { get; set; }
            public decimal ExchangePrice { get; set; }
        }
    }
}
