using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Return : UserControl
    {
        private const string StoreName = "Bubbyplanet";
        private const string StoreAddressLine1 = "Daudnagar Branch, Aurangabad";
        private const string StoreAddressLine2 = "Bihar - 824143";
        private const string StorePhone = "7870828400";
        private static readonly Color PageBg = Color.FromArgb(241, 245, 249);
        private static readonly Color Slate = Color.FromArgb(15, 23, 42);
        private static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);
        private static readonly Color SuccessGreen = Color.FromArgb(22, 163, 74);
        private static readonly Color ResetEnabledColor = Color.FromArgb(217, 119, 6);
        private static readonly Color DisabledButtonColor = Color.FromArgb(156, 163, 175);
        private static readonly Color CardBorder = Color.FromArgb(226, 232, 240);
        private static readonly Color MutedText = Color.FromArgb(100, 116, 139);

        TextBox txtOrderId = new TextBox();
        Button btnSearch = new Button();
        Button btnReset = new Button();
        Button btnProcess = new Button();

        Label lblCustomer = new Label();
        Label lblPhone = new Label();
        Label lblDate = new Label();
        Label lblSubtotal = new Label();
        Label lblTax = new Label();
        Label lblTotal = new Label();
        Label lblRefund = new Label();
        Label lblHint = new Label();
        Panel infoCard = new Panel();

        DataGridView grid = new DataGridView();

        PrintDocument printDoc = new PrintDocument();
        private bool isProcessingReturn = false;
        private readonly List<ReturnReceiptLine> pendingPrintLines = new();
        private decimal pendingTotalRefund = 0;
        private string currentCustomerName = "";
        private string currentCustomerPhone = "";

        private sealed class ReturnReceiptLine
        {
            public string ItemName { get; set; } = "";
            public int Qty { get; set; }
            public decimal Refund { get; set; }
        }

        private decimal Round2(decimal value) => ReturnCalculations.Round2(value);

        private decimal CalculateLineRefund(int qty, int returned, decimal netAmount, int returnQty)
            => ReturnCalculations.CalculateLineRefund(qty, returned, netAmount, returnQty);

        private void CommitGridEdits()
        {
            if (grid.IsCurrentCellInEditMode)
                grid.EndEdit();

            if (grid.CurrentCell != null && grid.CurrentCell.IsInEditMode)
                grid.EndEdit();
        }

        private void RefreshAllRefundCells()
        {
            if (!grid.Columns.Contains("ReturnQty") || !grid.Columns.Contains("Refund"))
                return;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                int returnQty = 0;
                int.TryParse(row.Cells["ReturnQty"].Value?.ToString(), out returnQty);

                int qty = Convert.ToInt32(row.Cells["qty"].Value);
                int returned = 0;
                int.TryParse(row.Cells["return_qty"].Value?.ToString(), out returned);

                decimal netAmount = Convert.ToDecimal(row.Cells["net_amount"].Value);
                decimal refund = CalculateLineRefund(qty, returned, netAmount, returnQty);
                row.Cells["Refund"].Value = refund.ToString("0.00");
            }

            CalculateTotalRefund();
        }

        public Return()
        {
            InitializeUI();
            printDoc.PrintPage += PrintDoc_PrintPage;
        }

        private void InitializeUI()
        {
            Dock = DockStyle.Fill;
            BackColor = PageBg;
            Padding = new Padding(12);

            Font labelFont = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            Font valueFont = new Font("Segoe UI", 10f);
            Font hintFont = new Font("Segoe UI", 8.5f);

            // Margins inside Absolute rows shrink the cell and cause overlap —
            // use dedicated spacer rows instead of control Margin.
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                BackColor = PageBg,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));  // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));  // gap
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86f));  // search
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));  // gap
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112f)); // summary
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));  // gap
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // grid
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));  // footer
            Controls.Add(root);

            // ----- Page header -----
            Panel headerBar = CreateCard(0);
            headerBar.BackColor = Color.White;
            headerBar.Margin = new Padding(0);
            headerBar.Padding = new Padding(0);

            Panel accent = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = PrimaryBlue
            };

            Panel headerContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 16, 8)
            };

            Label lblTitle = new Label
            {
                Text = "Sales Return",
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                ForeColor = Slate,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblHint.Text = "Search an order · enter return qty · process refund  ·  Allowed within 7 days";
            lblHint.Font = hintFont;
            lblHint.ForeColor = MutedText;
            lblHint.AutoSize = false;
            lblHint.Dock = DockStyle.Fill;
            lblHint.TextAlign = ContentAlignment.MiddleLeft;

            headerContent.Controls.Add(lblHint);
            headerContent.Controls.Add(lblTitle);
            headerBar.Controls.Add(headerContent);
            headerBar.Controls.Add(accent);

            // ----- Search card -----
            Panel searchCard = CreateCard(0);
            searchCard.Padding = new Padding(16, 12, 16, 12);
            searchCard.Margin = new Padding(0);

            Label lblOrder = new Label
            {
                Text = "ORDER ID",
                Font = labelFont,
                ForeColor = MutedText,
                AutoSize = true,
                Left = 4,
                Top = 0
            };

            txtOrderId.PlaceholderText = "Enter invoice / order number";
            txtOrderId.Font = new Font("Segoe UI", 11f);
            txtOrderId.BorderStyle = BorderStyle.FixedSingle;
            txtOrderId.Width = 220;
            txtOrderId.Height = 34;
            txtOrderId.Left = 4;
            txtOrderId.Top = 22;

            StyleButton(btnSearch, "Search", PrimaryBlue, 110, 34);
            btnSearch.Left = 236;
            btnSearch.Top = 22;
            btnSearch.Click += BtnSearch_Click;

            StyleButton(btnReset, "Reset", DisabledButtonColor, 100, 34);
            btnReset.Left = 356;
            btnReset.Top = 22;
            btnReset.Enabled = false;
            btnReset.Click += BtnReset_Click;

            searchCard.Controls.Add(lblOrder);
            searchCard.Controls.Add(txtOrderId);
            searchCard.Controls.Add(btnSearch);
            searchCard.Controls.Add(btnReset);

            // ----- Order summary card -----
            infoCard = CreateCard(0);
            infoCard.Padding = new Padding(10);
            infoCard.Margin = new Padding(0);

            TableLayoutPanel infoGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.White
            };
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            StyleInfoLabel(lblCustomer, "Customer: —", valueFont);
            StyleInfoLabel(lblPhone, "Phone: —", valueFont);
            StyleInfoLabel(lblDate, "Date: —", valueFont);
            StyleInfoLabel(lblSubtotal, "Subtotal: —", valueFont);
            StyleInfoLabel(lblTax, "Tax: —", valueFont);
            StyleInfoLabel(lblTotal, "Order Total: —", new Font("Segoe UI Semibold", 10f, FontStyle.Bold));
            lblTotal.ForeColor = Slate;

            infoGrid.Controls.Add(lblCustomer, 0, 0);
            infoGrid.Controls.Add(lblPhone, 1, 0);
            infoGrid.Controls.Add(lblDate, 2, 0);
            infoGrid.Controls.Add(lblSubtotal, 0, 1);
            infoGrid.Controls.Add(lblTax, 1, 1);
            infoGrid.Controls.Add(lblTotal, 2, 1);
            infoCard.Controls.Add(infoGrid);

            // ----- Items grid card -----
            Panel gridCard = CreateCard(0);
            gridCard.Margin = new Padding(0);
            gridCard.Padding = new Padding(1);

            Panel itemsHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Slate,
                Padding = new Padding(12, 0, 12, 0)
            };
            Label lblItems = new Label
            {
                Text = "ORDER ITEMS",
                Dock = DockStyle.Left,
                Width = 130,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblItemsHint = new Label
            {
                Text = "Select Code + Ctrl+C to copy  ·  edit only Return column",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleRight
            };
            itemsHeader.Controls.Add(lblItemsHint);
            itemsHeader.Controls.Add(lblItems);

            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            // CellSelect so Code can be selected + Ctrl+C copied (still ReadOnly)
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.MultiSelect = false;
            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            grid.RowHeadersVisible = false;
            grid.ScrollBars = ScrollBars.Both;
            grid.RowTemplate.Height = 36;
            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = CardBorder;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.EnableHeadersVisualStyles = false;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.CellEndEdit += Grid_CellEndEdit;
            grid.CellBeginEdit += Grid_CellBeginEdit;
            grid.KeyDown += Grid_KeyDown;

            gridCard.Controls.Add(grid);
            gridCard.Controls.Add(itemsHeader);

            // ----- Footer action bar -----
            Panel bottomPanel = CreateCard(0);
            bottomPanel.Margin = new Padding(0);
            bottomPanel.Padding = new Padding(16, 14, 16, 14);

            lblRefund.Text = "Total Refund  ₹ 0.00";
            lblRefund.Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold);
            lblRefund.ForeColor = Color.White;
            lblRefund.BackColor = Slate;
            lblRefund.AutoSize = false;
            lblRefund.Width = 280;
            lblRefund.Height = 44;
            lblRefund.Left = 16;
            lblRefund.Top = 14;
            lblRefund.TextAlign = ContentAlignment.MiddleLeft;
            lblRefund.Padding = new Padding(14, 0, 0, 0);

            StyleButton(btnProcess, "Process Return", DisabledButtonColor, 180, 44);
            btnProcess.Enabled = false;
            btnProcess.Click += BtnProcess_Click;
            bottomPanel.Resize += (s, e) =>
            {
                btnProcess.Left = Math.Max(16, bottomPanel.ClientSize.Width - btnProcess.Width - 16);
                btnProcess.Top = 14;
            };

            bottomPanel.Controls.Add(lblRefund);
            bottomPanel.Controls.Add(btnProcess);

            Panel Gap() => new Panel { Dock = DockStyle.Fill, BackColor = PageBg, Margin = new Padding(0) };

            root.Controls.Add(headerBar, 0, 0);
            root.Controls.Add(Gap(), 0, 1);
            root.Controls.Add(searchCard, 0, 2);
            root.Controls.Add(Gap(), 0, 3);
            root.Controls.Add(infoCard, 0, 4);
            root.Controls.Add(Gap(), 0, 5);
            root.Controls.Add(gridCard, 0, 6);
            root.Controls.Add(bottomPanel, 0, 7);

            txtOrderId.TextChanged += TxtOrderId_TextChanged;
            Load += (s, e) =>
            {
                btnProcess.Left = Math.Max(16, bottomPanel.ClientSize.Width - btnProcess.Width - 16);
                btnProcess.Top = 14;
                txtOrderId.Focus();
            };
            txtOrderId.KeyDown += txtOrderId_KeyDown;
            txtOrderId.KeyPress += TxtOrderId_KeyPress;
        }

        private static Panel CreateCard(int height)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            if (height > 0)
                card.Height = height;
            return card;
        }

        private static void StyleButton(Button btn, string text, Color back, int width, int height)
        {
            btn.Text = text;
            btn.Width = width;
            btn.Height = height;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = back;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        private static void StyleInfoLabel(Label lbl, string text, Font font)
        {
            lbl.Text = text;
            lbl.Font = font;
            lbl.ForeColor = Color.FromArgb(51, 65, 85);
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Padding = new Padding(8, 0, 8, 0);
            lbl.Margin = new Padding(2);
            lbl.BackColor = Color.FromArgb(248, 250, 252);
        }

        private void TxtOrderId_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void txtOrderId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // beep/extra enter rokne ke liye

                BtnSearch_Click(sender, e); // 🔥 direct call
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (txtOrderId.Text == "")
            {
                MessageBox.Show("Enter Order ID");
                return;
            }
            if (!int.TryParse(txtOrderId.Text.Trim(), out int parsedOrderId) || parsedOrderId <= 0)
            {
                MessageBox.Show("Enter valid numeric Order ID");
                return;
            }

            try
            {
                LoadOrder(parsedOrderId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadOrder(int parsedOrderId)
        {
            using (MySqlConnection con = DB.GetConnection())
            {
                con.Open();

                string orderQuery =
                @"SELECT 
                    o.subtotal AS subtotal,
                    o.total_tax AS tax,
                    o.grand_total AS grand_total,
                    o.date_added,
                    c.first_name,
                    c.sur_name,
                    c.phone
                FROM inv_orders o
                JOIN inv_customers c ON c.id = o.customer_id
                WHERE o.id = @orderId";

                MySqlCommand cmd = new MySqlCommand(orderQuery, con);
                cmd.Parameters.AddWithValue("@orderId", parsedOrderId);

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        DateTime orderDate = Convert.ToDateTime(dr["date_added"]);
                        currentCustomerName = (dr["first_name"] + " " + dr["sur_name"]).Trim();
                        currentCustomerPhone = dr["phone"]?.ToString()?.Trim() ?? "";

                        lblCustomer.Text = "Customer: " + (string.IsNullOrWhiteSpace(currentCustomerName) ? "—" : currentCustomerName);
                        lblPhone.Text = "Phone: " + (string.IsNullOrWhiteSpace(currentCustomerPhone) ? "—" : currentCustomerPhone);
                        lblDate.Text = "Date: " + orderDate.ToString("dd-MM-yyyy HH:mm");

                        decimal subtotal = Convert.ToDecimal(dr["subtotal"].ToString());
                        decimal tax = Convert.ToDecimal(dr["tax"].ToString());
                        decimal total = Convert.ToDecimal(dr["grand_total"].ToString());

                        lblSubtotal.Text = "Subtotal: ₹ " + subtotal.ToString("0.00");
                        lblTax.Text = "Tax: ₹ " + tax.ToString("0.00");
                        lblTotal.Text = "Order Total: ₹ " + total.ToString("0.00");

                        if (!IsReturnAllowedWithin7Days(orderDate))
                        {
                            MessageBox.Show("Return allowed only within 7 days. 8th day se return allowed nahi hai.");
                            grid.DataSource = null;
                            grid.Rows.Clear();
                            grid.Columns.Clear();
                            grid.Enabled = false;
                            SetRefundDisplay(0);
                            SetProcessEnabled(false);
                            btnReset.Enabled = true;
                            btnReset.BackColor = ResetEnabledColor;
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Order not found");
                        return;
                    }
                }

                string itemQuery =
                @"SELECT
                    od.id,
                    od.item_id,
                    i.item_code,
                    i.item_name,
                    od.qty,
                    IFNULL(od.return_qty,0) return_qty,
                    od.selling_price,
                    od.gross_amount,
                    od.discount_percent,
                    od.discount_amount,
                    od.taxable_amount,
                    od.gst_amount,
                    od.net_amount
                FROM inv_order_details od
                JOIN inv_items_master i ON i.id = od.item_id
                WHERE od.order_id = @orderId";

                MySqlDataAdapter da = new MySqlDataAdapter(itemQuery, con);
                da.SelectCommand.Parameters.AddWithValue("@orderId", parsedOrderId);

                DataTable dt = new DataTable();
                da.Fill(dt);

                bool hasReturnableItem = false;
                foreach (DataRow drItem in dt.Rows)
                {
                    int qty = Convert.ToInt32(drItem["qty"]);
                    int returned = Convert.ToInt32(drItem["return_qty"]);
                    if (qty - returned > 0)
                    {
                        hasReturnableItem = true;
                        break;
                    }
                }

                if (!hasReturnableItem)
                {
                    MessageBox.Show("All items are already fully returned for this order.");
                    grid.DataSource = null;
                    grid.Rows.Clear();
                    grid.Columns.Clear();
                    SetProcessEnabled(false);
                    btnReset.Enabled = true;
                    btnReset.BackColor = ResetEnabledColor;
                    return;
                }

                BindOrderItemsGrid(dt);
                grid.Enabled = true;
                SetProcessEnabled(grid.Rows.Count > 0);
                btnReset.Enabled = true;
                btnReset.BackColor = ResetEnabledColor;
            }
        }

        private void SetProcessEnabled(bool enabled)
        {
            btnProcess.Enabled = enabled;
            btnProcess.BackColor = enabled ? SuccessGreen : DisabledButtonColor;
        }

        private void SetRefundDisplay(decimal amount)
        {
            lblRefund.Text = "Total Refund  ₹ " + amount.ToString("0.00");
            lblRefund.BackColor = amount > 0 ? SuccessGreen : Slate;
        }

        private void BindOrderItemsGrid(DataTable dt)
        {
            // Clear previous bind completely. Unbound ReturnQty/Refund columns
            // otherwise survive DataSource reassignment and scramble the UI
            // when the same order is searched again after a partial return.
            grid.SuspendLayout();
            try
            {
                grid.DataSource = null;
                grid.Rows.Clear();
                grid.Columns.Clear();
                grid.AutoGenerateColumns = true;
                grid.DataSource = dt;

                grid.Columns["discount_percent"].HeaderText = "Disc %";
                grid.Columns["discount_percent"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grid.Columns["discount_percent"].Width = 80;
                grid.Columns["discount_percent"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns["discount_percent"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                if (grid.Columns.Contains("id"))
                    grid.Columns["id"].Visible = false;
                if (grid.Columns.Contains("item_id"))
                    grid.Columns["item_id"].Visible = false;
                if (grid.Columns.Contains("gross_amount"))
                    grid.Columns["gross_amount"].Visible = false;
                if (grid.Columns.Contains("discount_amount"))
                    grid.Columns["discount_amount"].Visible = false;
                if (grid.Columns.Contains("taxable_amount"))
                    grid.Columns["taxable_amount"].Visible = false;

                Color headerBg = Color.FromArgb(30, 41, 59);

                grid.Columns["item_code"].HeaderText = "Code";
                grid.Columns["item_name"].HeaderText = "Item Name";
                grid.Columns["qty"].HeaderText = "Qty";
                grid.Columns["return_qty"].HeaderText = "Returned";
                grid.Columns["selling_price"].HeaderText = "Price";
                grid.Columns["discount_percent"].HeaderText = "Disc %";
                grid.Columns["gst_amount"].HeaderText = "GST";
                grid.Columns["net_amount"].HeaderText = "Net";

                grid.Columns["selling_price"].DefaultCellStyle.Format = "0.00";
                grid.Columns["gst_amount"].DefaultCellStyle.Format = "0.00";
                grid.Columns["net_amount"].DefaultCellStyle.Format = "0.00";
                grid.Columns["discount_percent"].DefaultCellStyle.Format = "0.##";

                // Fixed widths sized so full header text shows; Item takes remaining space.
                // Horizontal scroll appears instead of truncating headers.
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                grid.Columns["item_name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns["item_name"].MinimumWidth = 160;
                grid.Columns["item_name"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                grid.Columns["item_name"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;

                void FixCol(string name, int width, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
                {
                    var col = grid.Columns[name];
                    col.Visible = true;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.Width = width;
                    col.MinimumWidth = width;
                    col.Resizable = DataGridViewTriState.False;
                    col.DefaultCellStyle.Alignment = align;
                    col.HeaderCell.Style.Alignment = align;
                    col.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
                }

                Font codeFont = new Font("Consolas", 9f);
                int codeWidth = TextRenderer.MeasureText("Code", grid.ColumnHeadersDefaultCellStyle.Font).Width + 28;
                foreach (DataRow row in dt.Rows)
                {
                    string code = row["item_code"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(code))
                        continue;
                    int measured = TextRenderer.MeasureText(code, codeFont).Width + 28;
                    if (measured > codeWidth)
                        codeWidth = measured;
                }
                codeWidth = Math.Clamp(codeWidth, 130, 320);

                FixCol("item_code", codeWidth, DataGridViewContentAlignment.MiddleLeft);
                grid.Columns["item_code"].DefaultCellStyle.Font = codeFont;
                grid.Columns["item_code"].DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
                grid.Columns["item_code"].DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                grid.Columns["item_code"].DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

                FixCol("qty", 55);
                FixCol("return_qty", 92);   // "Returned"
                FixCol("selling_price", 78, DataGridViewContentAlignment.MiddleRight);
                FixCol("discount_percent", 78); // "Disc %"
                FixCol("gst_amount", 70, DataGridViewContentAlignment.MiddleRight);
                FixCol("net_amount", 85, DataGridViewContentAlignment.MiddleRight);

                DataGridViewTextBoxColumn returnQtyCol = new DataGridViewTextBoxColumn();
                returnQtyCol.Name = "ReturnQty";
                returnQtyCol.HeaderText = "Return";
                returnQtyCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                returnQtyCol.Width = 78;
                returnQtyCol.MinimumWidth = 78;
                returnQtyCol.Resizable = DataGridViewTriState.False;
                returnQtyCol.SortMode = DataGridViewColumnSortMode.NotSortable;
                grid.Columns.Add(returnQtyCol);

                DataGridViewTextBoxColumn refundCol = new DataGridViewTextBoxColumn();
                refundCol.Name = "Refund";
                refundCol.HeaderText = "Refund";
                refundCol.ReadOnly = true;
                refundCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                refundCol.Width = 90;
                refundCol.MinimumWidth = 90;
                refundCol.Resizable = DataGridViewTriState.False;
                refundCol.SortMode = DataGridViewColumnSortMode.NotSortable;
                refundCol.DefaultCellStyle.Format = "0.00";
                grid.Columns.Add(refundCol);

                // Code before Item Name
                grid.Columns["item_code"].DisplayIndex = 0;
                grid.Columns["item_name"].DisplayIndex = 1;
                grid.Columns["qty"].DisplayIndex = 2;
                grid.Columns["return_qty"].DisplayIndex = 3;
                grid.Columns["selling_price"].DisplayIndex = 4;
                grid.Columns["discount_percent"].DisplayIndex = 5;
                grid.Columns["gst_amount"].DisplayIndex = 6;
                grid.Columns["net_amount"].DisplayIndex = 7;
                grid.Columns["ReturnQty"].DisplayIndex = 8;
                grid.Columns["Refund"].DisplayIndex = 9;

                foreach (DataGridViewColumn column in grid.Columns)
                {
                    column.ReadOnly = true;
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    column.HeaderCell.Style.BackColor = headerBg;
                    column.HeaderCell.Style.ForeColor = Color.White;
                    column.HeaderCell.Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    column.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
                }

                // Only Return is editable. Code/others are ReadOnly but selectable for copy.
                grid.Columns["item_code"].ReadOnly = true;
                grid.Columns["ReturnQty"].ReadOnly = false;
                grid.Columns["ReturnQty"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns["ReturnQty"].DefaultCellStyle.BackColor = Color.FromArgb(239, 246, 255);
                grid.Columns["ReturnQty"].DefaultCellStyle.SelectionBackColor = Color.FromArgb(191, 219, 254);
                grid.Columns["ReturnQty"].DefaultCellStyle.SelectionForeColor = Color.Black;
                grid.Columns["ReturnQty"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns["Refund"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                grid.Columns["Refund"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns["Refund"].DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    row.Cells["ReturnQty"].Value = 0;
                    row.Cells["Refund"].Value = "0.00";
                }

                SetRefundDisplay(0);
            }
            finally
            {
                grid.ResumeLayout();
            }
        }

        private void Grid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            // Block edit on every column except Return (Code stays copyable via selection)
            string colName = grid.Columns[e.ColumnIndex].Name;
            if (!string.Equals(colName, "ReturnQty", StringComparison.Ordinal))
                e.Cancel = true;
        }

        private void Grid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C && grid.CurrentCell != null)
            {
                object? value = grid.CurrentCell.Value;
                if (value != null)
                {
                    try
                    {
                        Clipboard.SetText(value.ToString() ?? "");
                        e.Handled = true;
                    }
                    catch
                    {
                        // clipboard busy — ignore
                    }
                }
            }
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name == "ReturnQty")
            {
                DataGridViewRow row = grid.Rows[e.RowIndex];

                int qty = Convert.ToInt32(row.Cells["qty"].Value);

                int returned = 0;
                int.TryParse(row.Cells["return_qty"].Value?.ToString(), out returned);

                int returnQty = 0;
                string rawReturnQty = row.Cells["ReturnQty"].Value?.ToString()?.Trim() ?? "";
                if (rawReturnQty == "")
                {
                    row.Cells["ReturnQty"].Value = 0;
                    row.Cells["Refund"].Value = 0;
                    CalculateTotalRefund();
                    return;
                }
                if (!int.TryParse(rawReturnQty, out returnQty) || returnQty < 0)
                {
                    MessageBox.Show("Return Qty must be a positive whole number.");
                    row.Cells["ReturnQty"].Value = 0;
                    row.Cells["Refund"].Value = 0;
                    CalculateTotalRefund();
                    return;
                }

                decimal total = Convert.ToDecimal(row.Cells["net_amount"].Value);

                int allowed = qty - returned;

                if (returnQty > allowed)
                {
                    MessageBox.Show("Return qty exceeds allowed limit");
                    row.Cells["ReturnQty"].Value = 0;
                    row.Cells["Refund"].Value = 0;
                    CalculateTotalRefund();
                    return;
                }

                decimal refund = CalculateLineRefund(qty, returned, total, returnQty);
                row.Cells["Refund"].Value = refund.ToString("0.00");

                CalculateTotalRefund();
            }
        }

        private void CalculateTotalRefund()
        {
            decimal total = 0;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells["Refund"].Value != null)
                {
                    decimal val;
                    if (decimal.TryParse(row.Cells["Refund"].Value.ToString(), out val))
                        total += val;
                }
            }

            SetRefundDisplay(total);
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            if (isProcessingReturn)
                return;

            if (!int.TryParse(txtOrderId.Text, out int parsedOrderId))
            {
                MessageBox.Show("Invalid order id.");
                return;
            }

            if (!IsReturnAllowedForOrder(parsedOrderId))
            {
                MessageBox.Show("Return allowed only within 7 days. 8th day se return allowed nahi hai.");
                SetProcessEnabled(false);
                return;
            }

            CommitGridEdits();
            RefreshAllRefundCells();

            bool hasReturn = false;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (row.Cells["ReturnQty"].Value != null &&
                    int.TryParse(row.Cells["ReturnQty"].Value.ToString(), out int qty) &&
                    qty > 0)
                {
                    hasReturn = true;
                    break;
                }
            }

            if (!hasReturn)
            {
                MessageBox.Show("Please enter return quantity first.");
                return;
            }

            isProcessingReturn = true;
            SetProcessEnabled(false);
            bool returnCompleted = false;
            pendingPrintLines.Clear();
            pendingTotalRefund = 0;

            try
            {
                using (MySqlConnection con = DB.GetConnection())
                {
                    con.Open();
                    using MySqlTransaction transaction = con.BeginTransaction();
                    try
                    {
                        decimal totalRefund = 0;
                        int orderId = parsedOrderId;

                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (row.IsNewRow)
                                continue;

                            int returnNow = 0;
                            int.TryParse(row.Cells["ReturnQty"].Value?.ToString(), out returnNow);
                            if (returnNow <= 0)
                                continue;

                            int detailId = Convert.ToInt32(row.Cells["id"].Value);
                            string itemName = row.Cells["item_name"].Value?.ToString() ?? "";

                            int qty;
                            int returnedAlready;
                            decimal gross;
                            decimal discountAmount;
                            decimal total;
                            decimal subtotalCurrent;
                            decimal tax;
                            string itemCode;

                            using (MySqlCommand fetchCmd = new MySqlCommand(@"
                                SELECT
                                    od.qty,
                                    IFNULL(od.return_qty, 0) AS return_qty,
                                    od.gross_amount,
                                    od.discount_amount,
                                    od.taxable_amount,
                                    od.gst_amount,
                                    od.net_amount,
                                    i.item_code
                                FROM inv_order_details od
                                JOIN inv_items_master i ON i.id = od.item_id
                                WHERE od.id = @id
                                FOR UPDATE", con, transaction))
                            {
                                fetchCmd.Parameters.AddWithValue("@id", detailId);
                                using MySqlDataReader detailReader = fetchCmd.ExecuteReader();
                                if (!detailReader.Read())
                                    throw new Exception("Order item not found while processing return.");

                                qty = Convert.ToInt32(detailReader["qty"]);
                                returnedAlready = Convert.ToInt32(detailReader["return_qty"]);
                                gross = Convert.ToDecimal(detailReader["gross_amount"]);
                                discountAmount = Convert.ToDecimal(detailReader["discount_amount"]);
                                total = Convert.ToDecimal(detailReader["net_amount"]);
                                subtotalCurrent = Convert.ToDecimal(detailReader["taxable_amount"]);
                                tax = Convert.ToDecimal(detailReader["gst_amount"]);
                                itemCode = detailReader["item_code"]?.ToString() ?? "";
                            }

                            if (returnedAlready + returnNow > qty)
                            {
                                MessageBox.Show("Return exceeds quantity. Order may have changed. Please search again.");
                                transaction.Rollback();
                                return;
                            }

                            int currentRemaining = qty - returnedAlready;
                            if (currentRemaining <= 0)
                                continue;

                            ReturnLineResult lineResult = ReturnCalculations.ApplyReturn(
                                qty,
                                returnedAlready,
                                returnNow,
                                gross,
                                discountAmount,
                                subtotalCurrent,
                                tax,
                                total);

                            decimal refund = lineResult.Refund;
                            totalRefund += refund;

                            using MySqlCommand cmd = new MySqlCommand(@"
                                UPDATE inv_order_details
                                SET
                                    return_qty = @rqty,
                                    gross_amount = @gross,
                                    discount_amount = @disc,
                                    taxable_amount = @sub,
                                    gst_amount = @tax,
                                    net_amount = @total
                                WHERE id = @id", con, transaction);
                            cmd.Parameters.AddWithValue("@rqty", lineResult.NewReturnQty);
                            cmd.Parameters.AddWithValue("@gross", lineResult.NewGrossAmount);
                            cmd.Parameters.AddWithValue("@disc", lineResult.NewDiscountAmount);
                            cmd.Parameters.AddWithValue("@sub", lineResult.NewTaxableAmount);
                            cmd.Parameters.AddWithValue("@tax", lineResult.NewGstAmount);
                            cmd.Parameters.AddWithValue("@total", lineResult.NewNetAmount);
                            cmd.Parameters.AddWithValue("@id", detailId);
                            cmd.ExecuteNonQuery();

                            using MySqlCommand stockCmd = new MySqlCommand(@"
                                UPDATE inv_stock
                                SET quantity = quantity + @qty,
                                    last_updated = NOW()
                                WHERE LOWER(TRIM(item_code)) = LOWER(TRIM(@code))", con, transaction);
                            stockCmd.Parameters.AddWithValue("@qty", returnNow);
                            stockCmd.Parameters.AddWithValue("@code", itemCode);
                            int stockRows = stockCmd.ExecuteNonQuery();
                            if (stockRows == 0)
                                throw new Exception($"Stock update failed for returned item: {itemCode}");

                            pendingPrintLines.Add(new ReturnReceiptLine
                            {
                                ItemName = itemName,
                                Qty = returnNow,
                                Refund = refund
                            });
                        }

                        decimal subtotal = 0, taxTotal = 0, grandTotal = 0, totalDiscount = 0;
                        using (MySqlCommand cmd2 = new MySqlCommand(@"
                            SELECT
                                IFNULL(SUM(taxable_amount),0),
                                IFNULL(SUM(gst_amount),0),
                                IFNULL(SUM(net_amount),0),
                                IFNULL(SUM(discount_amount),0)
                            FROM inv_order_details
                            WHERE order_id = @oid", con, transaction))
                        {
                            cmd2.Parameters.AddWithValue("@oid", orderId);
                            using MySqlDataReader dr = cmd2.ExecuteReader();
                            if (dr.Read())
                            {
                                subtotal = dr.GetDecimal(0);
                                taxTotal = dr.GetDecimal(1);
                                grandTotal = dr.GetDecimal(2);
                                totalDiscount = dr.GetDecimal(3);
                            }
                        }

                        using MySqlCommand cmd3 = new MySqlCommand(@"
                            UPDATE inv_orders
                            SET
                                subtotal = @sub,
                                total_discount = @disc,
                                total_tax = @tax,
                                grand_total = @gt,
                                date_updated = NOW()
                            WHERE id = @id", con, transaction);
                        cmd3.Parameters.AddWithValue("@sub", Round2(subtotal));
                        cmd3.Parameters.AddWithValue("@disc", Round2(totalDiscount));
                        cmd3.Parameters.AddWithValue("@tax", Round2(taxTotal));
                        cmd3.Parameters.AddWithValue("@gt", Round2(grandTotal));
                        cmd3.Parameters.AddWithValue("@id", orderId);
                        cmd3.ExecuteNonQuery();

                        transaction.Commit();
                        pendingTotalRefund = totalRefund;
                        returnCompleted = true;
                        MessageBox.Show("Return Completed Successfully\n\nTotal Refund Amount: ₹ " + totalRefund.ToString("0.00"));
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Return failed: " + ex.Message);
                return;
            }
            finally
            {
                isProcessingReturn = false;
                SetProcessEnabled(grid != null && grid.Enabled && grid.Rows.Count > 0);
            }

            if (!returnCompleted)
                return;

            try
            {
                int dynamicHeight = CalculateReturnPrintHeight();
                PaperSize customSize = new PaperSize("Custom", 300, dynamicHeight);
                printDoc.DefaultPageSettings.PaperSize = customSize;
                PrinterRouting.ApplyReceiptReturnPrinter(printDoc);
                if (!printDoc.PrinterSettings.IsValid)
                {
                    MessageBox.Show("Return saved, but no valid receipt/return printer found.");
                }
                else
                {
                    printDoc.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Return saved, but receipt print failed: " + ex.Message);
            }

            try
            {
                LoadOrder(parsedOrderId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Return saved, but order reload failed: " + ex.Message);
            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            Font font = new Font("Segoe UI", 9);
            Font bold = new Font("Segoe UI", 9, FontStyle.Bold);
            Font header = new Font("Segoe UI", 10, FontStyle.Bold);

            float y = 5;
            string returnId = "RET-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            int pageWidth = e.PageSettings.PaperSize.Width;

            // 1) Store details (centered)
            SizeF storeSize = g.MeasureString(StoreName, header);
            g.DrawString(StoreName, header, Brushes.Black, (pageWidth - storeSize.Width) / 2, y);
            y += 15;
            SizeF add1 = g.MeasureString(StoreAddressLine1, font);
            g.DrawString(StoreAddressLine1, font, Brushes.Black, (pageWidth - add1.Width) / 2, y);
            y += 13;
            SizeF add2 = g.MeasureString(StoreAddressLine2, font);
            g.DrawString(StoreAddressLine2, font, Brushes.Black, (pageWidth - add2.Width) / 2, y);
            y += 13;
            string phoneLine = "Phone: " + StorePhone;
            SizeF phoneSize = g.MeasureString(phoneLine, font);
            g.DrawString(phoneLine, font, Brushes.Black, (pageWidth - phoneSize.Width) / 2, y);
            y += 15;

            // 2) Heading (centered)
            string heading = "RETURN RECEIPT";
            SizeF headingSize = g.MeasureString(heading, bold);
            g.DrawString(heading, bold, Brushes.Black, (pageWidth - headingSize.Width) / 2, y);
            y += 15;

            // 3) Return id
            g.DrawString("Return ID: " + returnId, font, Brushes.Black, 5, y);
            y += 13;

            // 4) Original invoice id
            g.DrawString("Original Invoice ID: " + txtOrderId.Text, font, Brushes.Black, 5, y);
            y += 13;

            // 5) Date-time
            g.DrawString("Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm"), font, Brushes.Black, 5, y);
            y += 13;

            // 6) Customer details
            string customer = string.IsNullOrWhiteSpace(currentCustomerName) ? "Walk-in Customer" : currentCustomerName;
            string phone = currentCustomerPhone ?? "";
            g.DrawString("Customer: " + customer, font, Brushes.Black, 5, y);
            y += 13;
            if (!string.IsNullOrWhiteSpace(phone))
                g.DrawString("Phone: " + phone, font, Brushes.Black, 5, y);
            else
                g.DrawString("Phone: —", font, Brushes.Black, 5, y);
            y += 12;

            g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
            y += 12;

            // Column headers
            g.DrawString("Item", bold, Brushes.Black, 5, y);
            g.DrawString("Qty", bold, Brushes.Black, 160, y);
            g.DrawString("Amt", bold, Brushes.Black, 220, y);
            y += 15;

            g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
            y += 10;
            decimal totalRefundAmount = pendingTotalRefund;

            foreach (ReturnReceiptLine line in pendingPrintLines)
            {
                string name = line.ItemName;
                int qty = line.Qty;
                decimal refund = line.Refund;

                if (name.Length > 18)
                {
                    string line1 = name.Substring(0, 18);
                    string line2 = name.Substring(18);

                    g.DrawString(line1, font, Brushes.Black, 5, y);
                    y += 12;
                    g.DrawString(line2, font, Brushes.Black, 5, y);
                }
                else
                {
                    g.DrawString(name, font, Brushes.Black, 5, y);
                }

                g.DrawString(qty.ToString(), font, Brushes.Black, 160, y);
                g.DrawString(refund.ToString("0.00"), font, Brushes.Black, 220, y);
                y += 18;
            }

            y += 10;
            g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
            y += 15;

            g.DrawString("TOTAL REFUND : ₹ " + totalRefundAmount.ToString("0.00"), bold, Brushes.Black, 5, y);
            y += 20;

            g.DrawString("Thank You!", font, Brushes.Black, 90, y);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtOrderId.Text = "";
            ResetReturnForm();
            txtOrderId.Focus();
        }

        private void TxtOrderId_TextChanged(object sender, EventArgs e)
        {
            ResetReturnForm();
        }

        private void ResetReturnForm()
        {
            currentCustomerName = "";
            currentCustomerPhone = "";
            lblCustomer.Text = "Customer: —";
            lblPhone.Text = "Phone: —";
            lblDate.Text = "Date: —";
            lblSubtotal.Text = "Subtotal: —";
            lblTax.Text = "Tax: —";
            lblTotal.Text = "Order Total: —";
            SetRefundDisplay(0);

            grid.DataSource = null;
            grid.Rows.Clear();
            grid.Columns.Clear();
            grid.Enabled = true;
            SetProcessEnabled(false);
            btnReset.Enabled = false;
            btnReset.BackColor = DisabledButtonColor;
        }

        private int CalculateReturnPrintHeight()
        {
            int baseHeight = 290;
            int perLineHeight = 18;
            int printableLines = 0;

            foreach (ReturnReceiptLine line in pendingPrintLines)
            {
                string name = line.ItemName ?? "";
                printableLines += name.Length > 18 ? 2 : 1;
            }

            if (printableLines <= 0)
                printableLines = 1;

            return baseHeight + (printableLines * perLineHeight);
        }

        private bool IsReturnAllowedWithin7Days(DateTime orderDate)
        {
            double elapsedDays = (DateTime.Now.Date - orderDate.Date).TotalDays;
            return elapsedDays < 8;
        }

        private bool IsReturnAllowedForOrder(int orderId)
        {
            using (MySqlConnection con = DB.GetConnection())
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT date_added FROM inv_orders WHERE id=@id LIMIT 1", con);
                cmd.Parameters.AddWithValue("@id", orderId);
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return false;

                if (!DateTime.TryParse(result.ToString(), out DateTime orderDate))
                    return false;

                return IsReturnAllowedWithin7Days(orderDate);
            }
        }
    }
}
