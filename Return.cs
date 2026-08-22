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
        DataGridView dgvExchange = new DataGridView();
        TextBox txtExchangeCode = new TextBox();
        Button btnAddExchange = new Button();
        Button btnRemoveExchange = new Button();
        Label lblCalcReturn = new Label();
        Label lblCalcNew = new Label();
        Label lblCalcBalance = new Label();
        Label lblCalcHint = new Label();

        PrintDocument printDoc = new PrintDocument();
        private bool isProcessingReturn = false;
        private readonly List<ReturnReceiptLine> pendingPrintLines = new();
        private readonly List<ReturnReceiptLine> pendingExchangePrintLines = new();
        private decimal pendingTotalRefund = 0;
        private decimal pendingBalanceDue = 0;
        private bool pendingIsExchange = false;
        private string currentCustomerName = "";
        private string currentCustomerPhone = "";
        private DateTime currentOrderDate = DateTime.Now;

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

        private bool pendingReturnResumeChecked = false;

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
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // grids
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f)); // footer calc + action
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
                Text = "Return / Exchange",
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                ForeColor = Slate,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblHint.Text = "Return + optional exchange on same bill  ·  New items ≥ return value  ·  7 days";
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

            Panel midPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBg,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            TableLayoutPanel midLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = PageBg,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            midLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));
            midLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8f));
            midLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));
            midLayout.Controls.Add(gridCard, 0, 0);
            midLayout.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = PageBg }, 0, 1);
            midLayout.Controls.Add(BuildExchangeCard(), 0, 2);
            midPanel.Controls.Add(midLayout);

            // ----- Footer action bar -----
            Panel bottomPanel = CreateCard(0);
            bottomPanel.Margin = new Padding(0);
            bottomPanel.Padding = new Padding(12, 10, 12, 10);

            Panel calcPanel = new Panel
            {
                Left = 12,
                Top = 8,
                Width = 520,
                Height = 110,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10)
            };
            calcPanel.Paint += (_, e) =>
            {
                if (calcPanel.Width <= 0 || calcPanel.Height <= 0) return;
                using Pen p = new Pen(CardBorder);
                e.Graphics.DrawRectangle(p, 0, 0, calcPanel.Width - 1, calcPanel.Height - 1);
            };

            lblCalcReturn.Text = "Return value: ₹ 0.00";
            lblCalcReturn.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            lblCalcReturn.ForeColor = Slate;
            lblCalcReturn.Location = new Point(10, 8);
            lblCalcReturn.AutoSize = true;

            lblCalcNew.Text = "New items: ₹ 0.00";
            lblCalcNew.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            lblCalcNew.ForeColor = PrimaryBlue;
            lblCalcNew.Location = new Point(10, 34);
            lblCalcNew.AutoSize = true;

            lblCalcBalance.Text = "Balance due: ₹ 0.00";
            lblCalcBalance.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            lblCalcBalance.ForeColor = SuccessGreen;
            lblCalcBalance.Location = new Point(10, 60);
            lblCalcBalance.AutoSize = true;

            lblCalcHint.Text = "Pure return = refund  ·  Exchange = same bill, pay only extra";
            lblCalcHint.Font = new Font("Segoe UI", 8f);
            lblCalcHint.ForeColor = MutedText;
            lblCalcHint.Location = new Point(240, 12);
            lblCalcHint.Size = new Size(260, 80);

            calcPanel.Controls.Add(lblCalcReturn);
            calcPanel.Controls.Add(lblCalcNew);
            calcPanel.Controls.Add(lblCalcBalance);
            calcPanel.Controls.Add(lblCalcHint);

            StyleButton(btnProcess, "Process", DisabledButtonColor, 180, 44);
            btnProcess.Enabled = false;
            btnProcess.Click += BtnProcess_Click;
            bottomPanel.Resize += (s, e) =>
            {
                btnProcess.Left = Math.Max(16, bottomPanel.ClientSize.Width - btnProcess.Width - 16);
                btnProcess.Top = 40;
            };

            bottomPanel.Controls.Add(calcPanel);
            bottomPanel.Controls.Add(btnProcess);

            Panel Gap() => new Panel { Dock = DockStyle.Fill, BackColor = PageBg, Margin = new Padding(0) };

            root.Controls.Add(headerBar, 0, 0);
            root.Controls.Add(Gap(), 0, 1);
            root.Controls.Add(searchCard, 0, 2);
            root.Controls.Add(Gap(), 0, 3);
            root.Controls.Add(infoCard, 0, 4);
            root.Controls.Add(Gap(), 0, 5);
            root.Controls.Add(midPanel, 0, 6);
            root.Controls.Add(bottomPanel, 0, 7);

            txtOrderId.TextChanged += TxtOrderId_TextChanged;
            Load += (s, e) =>
            {
                btnProcess.Left = Math.Max(16, bottomPanel.ClientSize.Width - btnProcess.Width - 16);
                btnProcess.Top = 40;
                BeginInvoke(new Action(TryResumePendingReturnOnStartup));
                txtOrderId.Focus();
            };
            txtOrderId.KeyDown += txtOrderId_KeyDown;
            txtOrderId.KeyPress += TxtOrderId_KeyPress;
        }

        private Panel BuildExchangeCard()
        {
            Panel card = CreateCard(0);
            card.Margin = new Padding(0);
            card.Padding = new Padding(1);

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(13, 148, 136),
                Padding = new Padding(12, 0, 12, 0)
            };
            Label title = new Label
            {
                Text = "EXCHANGE ITEMS (same bill)",
                Dock = DockStyle.Left,
                Width = 240,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label hint = new Label
            {
                Text = "Add replacement items · value must be ≥ return",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(204, 251, 241),
                TextAlign = ContentAlignment.MiddleRight
            };
            header.Controls.Add(hint);
            header.Controls.Add(title);

            Panel addBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 6)
            };
            txtExchangeCode.PlaceholderText = "Item code / barcode";
            txtExchangeCode.Font = new Font("Segoe UI", 10f);
            txtExchangeCode.BorderStyle = BorderStyle.FixedSingle;
            txtExchangeCode.Width = 220;
            txtExchangeCode.Height = 30;
            txtExchangeCode.Left = 8;
            txtExchangeCode.Top = 6;
            txtExchangeCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    TryAddExchangeItem();
                }
            };

            StyleButton(btnAddExchange, "Add Item", SuccessGreen, 100, 30);
            btnAddExchange.Left = 238;
            btnAddExchange.Top = 6;
            btnAddExchange.Click += (_, _) => TryAddExchangeItem();

            StyleButton(btnRemoveExchange, "Remove", Color.FromArgb(220, 38, 38), 100, 30);
            btnRemoveExchange.Left = 348;
            btnRemoveExchange.Top = 6;
            btnRemoveExchange.Click += (_, _) => RemoveSelectedExchangeItem();

            addBar.Controls.Add(txtExchangeCode);
            addBar.Controls.Add(btnAddExchange);
            addBar.Controls.Add(btnRemoveExchange);

            dgvExchange.Dock = DockStyle.Fill;
            dgvExchange.BackgroundColor = Color.White;
            dgvExchange.BorderStyle = BorderStyle.None;
            dgvExchange.AllowUserToAddRows = false;
            dgvExchange.AllowUserToResizeRows = false;
            dgvExchange.RowHeadersVisible = false;
            dgvExchange.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvExchange.MultiSelect = false;
            dgvExchange.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvExchange.RowTemplate.Height = 30;
            dgvExchange.ColumnHeadersHeight = 32;
            dgvExchange.EnableHeadersVisualStyles = false;
            dgvExchange.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgvExchange.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvExchange.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            dgvExchange.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            dgvExchange.DefaultCellStyle.ForeColor = Color.Black;
            dgvExchange.DefaultCellStyle.BackColor = Color.White;
            dgvExchange.DefaultCellStyle.SelectionBackColor = Color.FromArgb(167, 243, 208);
            dgvExchange.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvExchange.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvExchange.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            dgvExchange.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(167, 243, 208);
            dgvExchange.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.Black;
            dgvExchange.Columns.Add("ItemName", "Item");
            dgvExchange.Columns.Add("ItemCode", "Code");
            dgvExchange.Columns.Add("Price", "Price");
            dgvExchange.Columns.Add("Qty", "Qty");
            dgvExchange.Columns.Add("GstPercent", "GST%");
            dgvExchange.Columns.Add("Net", "Net");
            dgvExchange.Columns.Add("ItemId", "ItemId");
            dgvExchange.Columns.Add("Taxable", "Taxable");
            dgvExchange.Columns.Add("GstAmt", "GstAmt");
            dgvExchange.Columns.Add("Gross", "Gross");
            dgvExchange.Columns["ItemId"].Visible = false;
            dgvExchange.Columns["Taxable"].Visible = false;
            dgvExchange.Columns["GstAmt"].Visible = false;
            dgvExchange.Columns["Gross"].Visible = false;
            dgvExchange.Columns["Qty"].ReadOnly = false;
            dgvExchange.Columns["ItemName"].ReadOnly = true;
            dgvExchange.Columns["ItemCode"].ReadOnly = true;
            dgvExchange.Columns["Price"].ReadOnly = true;
            dgvExchange.Columns["GstPercent"].ReadOnly = true;
            dgvExchange.Columns["Net"].ReadOnly = true;
            dgvExchange.CellEndEdit += DgvExchange_CellEndEdit;

            card.Controls.Add(dgvExchange);
            card.Controls.Add(addBar);
            card.Controls.Add(header);
            return card;
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
                        currentOrderDate = orderDate;
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
                ClearExchangeItems();
                RefreshExchangeCalculation();
            }
        }

        private void SetProcessEnabled(bool enabled)
        {
            btnProcess.Enabled = enabled;
            btnProcess.BackColor = enabled ? SuccessGreen : DisabledButtonColor;
        }

        private void SetRefundDisplay(decimal amount)
        {
            // Kept for compatibility; live calc panel is the primary display.
            lblRefund.Text = "Total Refund  ₹ " + amount.ToString("0.00");
            RefreshExchangeCalculation();
        }

        private decimal GetCurrentReturnValue()
        {
            decimal total = 0;
            if (!grid.Columns.Contains("Refund"))
                return 0;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                if (decimal.TryParse(row.Cells["Refund"].Value?.ToString(), out decimal val))
                    total += val;
            }
            return Round2(total);
        }

        private decimal GetCurrentExchangeValue()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvExchange.Rows)
            {
                if (row.IsNewRow) continue;
                if (decimal.TryParse(row.Cells["Net"].Value?.ToString(), out decimal val))
                    total += val;
            }
            return Round2(total);
        }

        private bool HasExchangeItems() => dgvExchange.Rows.Count > 0;

        private void RefreshExchangeCalculation()
        {
            decimal returnValue = GetCurrentReturnValue();
            decimal newValue = GetCurrentExchangeValue();
            ExchangeSummary summary = ReturnCalculations.CalculateExchange(returnValue, newValue);

            lblCalcReturn.Text = "Return value: ₹ " + summary.ReturnValue.ToString("0.00");
            lblCalcNew.Text = "New items: ₹ " + summary.NewItemsValue.ToString("0.00");

            if (summary.NewItemsValue <= 0)
            {
                lblCalcBalance.Text = "Refund to customer: ₹ " + summary.ReturnValue.ToString("0.00");
                lblCalcBalance.ForeColor = summary.ReturnValue > 0 ? SuccessGreen : Slate;
                lblCalcHint.Text = "Pure return mode — cash refund.\nAdd exchange items to keep same bill.";
                btnProcess.Text = "Process Return";
            }
            else if (summary.Shortfall > 0)
            {
                lblCalcBalance.Text = "Short by: ₹ " + summary.Shortfall.ToString("0.00");
                lblCalcBalance.ForeColor = Color.FromArgb(220, 38, 38);
                lblCalcHint.Text = "Mall rule: new items must be ≥ return value.\nAdd more / higher value items.";
                btnProcess.Text = "Process Exchange";
            }
            else
            {
                lblCalcBalance.Text = "Balance due (collect): ₹ " + summary.BalanceDue.ToString("0.00");
                lblCalcBalance.ForeColor = PrimaryBlue;
                lblCalcHint.Text = summary.BalanceDue == 0
                    ? "Even exchange — no cash.\nSame bill will be updated + printed."
                    : "Customer pays only the extra amount.\nSame bill updated — no new invoice.";
                btnProcess.Text = "Process Exchange";
            }
        }

        private void TryAddExchangeItem()
        {
            if (grid.Rows.Count == 0)
            {
                MessageBox.Show("Pehle order search karein.");
                return;
            }

            string code = txtExchangeCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Item code enter karein.");
                return;
            }

            try
            {
                using MySqlConnection con = DB.GetConnection();
                con.Open();
                using MySqlCommand cmd = new MySqlCommand(@"
                    SELECT
                        i.id,
                        i.item_code,
                        i.item_name,
                        i.selling_price,
                        IFNULL(i.GST, 0) AS GST,
                        IFNULL(s.quantity, 0) AS stock_qty
                    FROM inv_items_master i
                    LEFT JOIN inv_stock s ON LOWER(TRIM(i.item_code)) = LOWER(TRIM(s.item_code))
                    WHERE LOWER(TRIM(i.item_code)) = LOWER(TRIM(@code))
                    LIMIT 1", con);
                cmd.Parameters.AddWithValue("@code", code);

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    MessageBox.Show("Item not found.");
                    return;
                }

                int itemId = Convert.ToInt32(reader["id"]);
                string itemCode = reader["item_code"]?.ToString() ?? code;
                string itemName = reader["item_name"]?.ToString() ?? "";
                decimal price = Convert.ToDecimal(reader["selling_price"]);
                decimal gstPercent = Convert.ToDecimal(reader["GST"]);
                int stockQty = Convert.ToInt32(reader["stock_qty"]);

                if (stockQty <= 0)
                {
                    MessageBox.Show("Stock not available for this item.");
                    return;
                }

                foreach (DataGridViewRow existing in dgvExchange.Rows)
                {
                    if (string.Equals(existing.Cells["ItemCode"].Value?.ToString(), itemCode, StringComparison.OrdinalIgnoreCase))
                    {
                        int qty = Convert.ToInt32(existing.Cells["Qty"].Value);
                        if (qty + 1 > stockQty)
                        {
                            MessageBox.Show("Stock limit reached.");
                            return;
                        }
                        existing.Cells["Qty"].Value = qty + 1;
                        RecalcExchangeRow(existing);
                        RefreshExchangeCalculation();
                        txtExchangeCode.Clear();
                        txtExchangeCode.Focus();
                        return;
                    }
                }

                ReturnCalculations.CalculateLineAmounts(price, gstPercent, 0, 1,
                    out decimal taxable, out decimal gstAmt, out decimal gross, out decimal net);

                dgvExchange.Rows.Add(
                    itemName,
                    itemCode,
                    price.ToString("0.00"),
                    1,
                    gstPercent.ToString("0.##"),
                    net.ToString("0.00"),
                    itemId,
                    taxable.ToString("0.00"),
                    gstAmt.ToString("0.00"),
                    gross.ToString("0.00"));

                RefreshExchangeCalculation();
                txtExchangeCode.Clear();
                txtExchangeCode.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not add item: " + ex.Message);
            }
        }

        private void RemoveSelectedExchangeItem()
        {
            if (dgvExchange.CurrentRow == null || dgvExchange.CurrentRow.IsNewRow)
                return;
            dgvExchange.Rows.Remove(dgvExchange.CurrentRow);
            RefreshExchangeCalculation();
        }

        private void DgvExchange_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvExchange.Rows[e.RowIndex];
            if (dgvExchange.Columns[e.ColumnIndex].Name != "Qty")
                return;

            if (!int.TryParse(row.Cells["Qty"].Value?.ToString(), out int qty) || qty <= 0)
            {
                row.Cells["Qty"].Value = 1;
                qty = 1;
            }

            RecalcExchangeRow(row);
            RefreshExchangeCalculation();
        }

        private void RecalcExchangeRow(DataGridViewRow row)
        {
            decimal.TryParse(row.Cells["Price"].Value?.ToString(), out decimal price);
            decimal.TryParse(row.Cells["GstPercent"].Value?.ToString(), out decimal gstPercent);
            int.TryParse(row.Cells["Qty"].Value?.ToString(), out int qty);
            if (qty <= 0) qty = 1;

            ReturnCalculations.CalculateLineAmounts(price, gstPercent, 0, qty,
                out decimal taxable, out decimal gstAmt, out decimal gross, out decimal net);

            row.Cells["Qty"].Value = qty;
            row.Cells["Net"].Value = net.ToString("0.00");
            row.Cells["Taxable"].Value = taxable.ToString("0.00");
            row.Cells["GstAmt"].Value = gstAmt.ToString("0.00");
            row.Cells["Gross"].Value = gross.ToString("0.00");
        }

        private void ClearExchangeItems()
        {
            dgvExchange.Rows.Clear();
            txtExchangeCode.Clear();
            RefreshExchangeCalculation();
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

            // Already committed earlier (paper out / crash) → reprint only.
            PendingReturnCheckpoint? existing = PendingReturnStore.Load();
            if (existing != null &&
                existing.OrderId > 0 &&
                (existing.Stage == PendingReturnStage.DbCommitted ||
                 existing.Stage == PendingReturnStage.PrintStarted) &&
                IsReturnAlreadyAppliedInDb(existing))
            {
                ResumeReprintOnly(existing);
                return;
            }

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

            bool isExchange = HasExchangeItems();
            decimal returnValue = GetCurrentReturnValue();
            decimal exchangeValue = GetCurrentExchangeValue();
            ExchangeSummary exchangeSummary = ReturnCalculations.CalculateExchange(returnValue, exchangeValue);

            if (isExchange && !exchangeSummary.MeetsEqualOrMoreRule)
            {
                MessageBox.Show(
                    "Exchange rule: naye items ki value return value se kam nahi ho sakti.\n\n" +
                    "Return: ₹ " + exchangeSummary.ReturnValue.ToString("0.00") + "\n" +
                    "New items: ₹ " + exchangeSummary.NewItemsValue.ToString("0.00") + "\n" +
                    "Short by: ₹ " + exchangeSummary.Shortfall.ToString("0.00"));
                return;
            }

            // Resume policy: ONLY after Process Return starts (this point).
            // Search / qty typing before this — no checkpoint; crash = re-enter return.
            PendingReturnCheckpoint pending = BuildPendingReturnCheckpoint(
                PendingReturnStage.ProcessClicked,
                parsedOrderId,
                isExchange,
                returnValue,
                exchangeValue,
                0,
                exchangeSummary.BalanceDue);
            PendingReturnStore.Save(pending);

            isProcessingReturn = true;
            SetProcessEnabled(false);
            bool returnCompleted = false;
            pendingPrintLines.Clear();
            pendingExchangePrintLines.Clear();
            pendingTotalRefund = 0;
            pendingBalanceDue = 0;
            pendingIsExchange = isExchange;

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

                        if (isExchange)
                        {
                            foreach (DataGridViewRow exRow in dgvExchange.Rows)
                            {
                                if (exRow.IsNewRow) continue;

                                int itemId = Convert.ToInt32(exRow.Cells["ItemId"].Value);
                                string itemCode = exRow.Cells["ItemCode"].Value?.ToString() ?? "";
                                string itemName = exRow.Cells["ItemName"].Value?.ToString() ?? "";
                                int qty = Convert.ToInt32(exRow.Cells["Qty"].Value);
                                decimal price = Convert.ToDecimal(exRow.Cells["Price"].Value);
                                decimal gstPercent = Convert.ToDecimal(exRow.Cells["GstPercent"].Value);
                                decimal gross = Convert.ToDecimal(exRow.Cells["Gross"].Value);
                                decimal taxable = Convert.ToDecimal(exRow.Cells["Taxable"].Value);
                                decimal gstAmt = Convert.ToDecimal(exRow.Cells["GstAmt"].Value);
                                decimal net = Convert.ToDecimal(exRow.Cells["Net"].Value);
                                decimal discountAmount = Round2(gross - (taxable + gstAmt));

                                using (MySqlCommand stockCheck = new MySqlCommand(@"
                                    SELECT IFNULL(quantity, 0)
                                    FROM inv_stock
                                    WHERE LOWER(TRIM(item_code)) = LOWER(TRIM(@code))
                                    LIMIT 1
                                    FOR UPDATE", con, transaction))
                                {
                                    stockCheck.Parameters.AddWithValue("@code", itemCode);
                                    object stockObj = stockCheck.ExecuteScalar();
                                    int stockQty = stockObj == null || stockObj == DBNull.Value ? 0 : Convert.ToInt32(stockObj);
                                    if (stockQty < qty)
                                        throw new Exception($"Insufficient stock for {itemCode}. Available: {stockQty}");
                                }

                                using MySqlCommand insertCmd = new MySqlCommand(@"
                                    INSERT INTO inv_order_details
                                    (
                                        order_id, item_id, qty, selling_price,
                                        gross_amount, discount_percent, discount_amount,
                                        taxable_amount, gst_amount, net_amount
                                    )
                                    VALUES
                                    (
                                        @oid, @iid, @qty, @price,
                                        @gross, @discPercent, @discAmt,
                                        @taxable, @gst, @net
                                    )", con, transaction);
                                insertCmd.Parameters.AddWithValue("@oid", orderId);
                                insertCmd.Parameters.AddWithValue("@iid", itemId);
                                insertCmd.Parameters.AddWithValue("@qty", qty);
                                insertCmd.Parameters.AddWithValue("@price", price);
                                insertCmd.Parameters.AddWithValue("@gross", Round2(gross));
                                insertCmd.Parameters.AddWithValue("@discPercent", 0);
                                insertCmd.Parameters.AddWithValue("@discAmt", Round2(discountAmount));
                                insertCmd.Parameters.AddWithValue("@taxable", Round2(taxable));
                                insertCmd.Parameters.AddWithValue("@gst", Round2(gstAmt));
                                insertCmd.Parameters.AddWithValue("@net", Round2(net));
                                insertCmd.ExecuteNonQuery();

                                using MySqlCommand stockOut = new MySqlCommand(@"
                                    UPDATE inv_stock
                                    SET quantity = quantity - @qty,
                                        last_updated = NOW()
                                    WHERE LOWER(TRIM(item_code)) = LOWER(TRIM(@code))
                                      AND quantity >= @qty", con, transaction);
                                stockOut.Parameters.AddWithValue("@qty", qty);
                                stockOut.Parameters.AddWithValue("@code", itemCode);
                                if (stockOut.ExecuteNonQuery() == 0)
                                    throw new Exception($"Stock update failed for exchange item: {itemCode}");

                                pendingExchangePrintLines.Add(new ReturnReceiptLine
                                {
                                    ItemName = itemName,
                                    Qty = qty,
                                    Refund = net
                                });
                            }
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

                        // Return + exchange must NOT change reward_last_order_id.

                        // Persist print payload BEFORE commit (crash-safe resume).
                        if (isExchange)
                        {
                            pendingTotalRefund = 0;
                            pendingBalanceDue = exchangeSummary.BalanceDue;
                        }
                        else
                        {
                            pendingTotalRefund = totalRefund;
                            pendingBalanceDue = 0;
                        }

                        pending = BuildPendingReturnCheckpoint(
                            PendingReturnStage.DbCommitted,
                            orderId,
                            isExchange,
                            returnValue,
                            exchangeValue,
                            pendingTotalRefund,
                            pendingBalanceDue);
                        FillPendingPrintLines(pending);
                        PendingReturnStore.Save(pending);

                        transaction.Commit();
                        returnCompleted = true;

                        if (isExchange)
                        {
                            MessageBox.Show(
                                "Exchange Completed on Same Bill\n\n" +
                                "Order ID: " + orderId + "\n" +
                                "Return value: ₹ " + exchangeSummary.ReturnValue.ToString("0.00") + "\n" +
                                "New items: ₹ " + exchangeSummary.NewItemsValue.ToString("0.00") + "\n" +
                                "Collect from customer: ₹ " + exchangeSummary.BalanceDue.ToString("0.00"));
                        }
                        else
                        {
                            MessageBox.Show(
                                "Return Completed Successfully\n\n" +
                                "Total Refund Amount: ₹ " + totalRefund.ToString("0.00"));
                        }
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
                try
                {
                    PendingReturnStore.Save(BuildPendingReturnCheckpoint(
                        PendingReturnStage.ProcessClicked,
                        parsedOrderId,
                        isExchange,
                        returnValue,
                        exchangeValue,
                        0,
                        exchangeSummary.BalanceDue));
                }
                catch { }

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
                pending = PendingReturnStore.Load() ?? pending;
                pending.Stage = PendingReturnStage.PrintStarted;
                PendingReturnStore.Save(pending);

                int dynamicHeight = CalculateReturnPrintHeight();
                PaperSize customSize = new PaperSize("Custom", 300, dynamicHeight);
                printDoc.DefaultPageSettings.PaperSize = customSize;
                PrinterRouting.ApplyReceiptReturnPrinter(printDoc);
                if (!printDoc.PrinterSettings.IsValid)
                {
                    MessageBox.Show(
                        "Return saved, but no valid receipt/return printer found.\n" +
                        "App dubara khologe to reprint resume ho sakta hai.");
                }
                else
                {
                    printDoc.Print();
                    PendingReturnStore.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Return saved, but receipt print failed: " + ex.Message +
                    "\n\nApp dubara khologe to reprint resume ho sakta hai.");
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

        private PendingReturnCheckpoint BuildPendingReturnCheckpoint(
            PendingReturnStage stage,
            int orderId,
            bool isExchange,
            decimal returnValue,
            decimal exchangeValue,
            decimal totalRefund,
            decimal balanceDue)
        {
            var checkpoint = new PendingReturnCheckpoint
            {
                Stage = stage,
                OrderId = orderId,
                StartedAtUtc = DateTime.UtcNow,
                CustomerName = currentCustomerName ?? "",
                CustomerPhone = currentCustomerPhone ?? "",
                OrderDate = currentOrderDate,
                IsExchange = isExchange,
                TotalRefund = totalRefund,
                BalanceDue = balanceDue,
                ReturnValue = returnValue,
                ExchangeValue = exchangeValue
            };

            if (grid.Columns.Contains("id") && grid.Columns.Contains("ReturnQty"))
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (!int.TryParse(row.Cells["ReturnQty"].Value?.ToString(), out int returnNow) || returnNow <= 0)
                        continue;

                    int detailId = Convert.ToInt32(row.Cells["id"].Value);
                    int returnedAlready = 0;
                    if (grid.Columns.Contains("return_qty"))
                        int.TryParse(row.Cells["return_qty"].Value?.ToString(), out returnedAlready);

                    checkpoint.ReturnLines.Add(new PendingReturnLine
                    {
                        DetailId = detailId,
                        ItemName = row.Cells["item_name"].Value?.ToString() ?? "",
                        ItemCode = grid.Columns.Contains("item_code")
                            ? (row.Cells["item_code"].Value?.ToString() ?? "")
                            : "",
                        ReturnQty = returnNow,
                        ExpectedReturnQtyAfter = returnedAlready + returnNow
                    });
                }
            }

            foreach (DataGridViewRow exRow in dgvExchange.Rows)
            {
                if (exRow.IsNewRow) continue;
                checkpoint.ExchangeLines.Add(new PendingExchangeLine
                {
                    ItemId = Convert.ToInt32(exRow.Cells["ItemId"].Value),
                    ItemCode = exRow.Cells["ItemCode"].Value?.ToString() ?? "",
                    ItemName = exRow.Cells["ItemName"].Value?.ToString() ?? "",
                    Qty = Convert.ToInt32(exRow.Cells["Qty"].Value),
                    Price = Convert.ToDecimal(exRow.Cells["Price"].Value),
                    GstPercent = Convert.ToDecimal(exRow.Cells["GstPercent"].Value),
                    Gross = Convert.ToDecimal(exRow.Cells["Gross"].Value),
                    Taxable = Convert.ToDecimal(exRow.Cells["Taxable"].Value),
                    GstAmt = Convert.ToDecimal(exRow.Cells["GstAmt"].Value),
                    Net = Convert.ToDecimal(exRow.Cells["Net"].Value)
                });
            }

            return checkpoint;
        }

        private void FillPendingPrintLines(PendingReturnCheckpoint checkpoint)
        {
            checkpoint.PrintReturnLines.Clear();
            checkpoint.PrintExchangeLines.Clear();

            foreach (ReturnReceiptLine line in pendingPrintLines)
            {
                checkpoint.PrintReturnLines.Add(new PendingReturnPrintLine
                {
                    ItemName = line.ItemName,
                    Qty = line.Qty,
                    Amount = line.Refund
                });
            }

            foreach (ReturnReceiptLine line in pendingExchangePrintLines)
            {
                checkpoint.PrintExchangeLines.Add(new PendingReturnPrintLine
                {
                    ItemName = line.ItemName,
                    Qty = line.Qty,
                    Amount = line.Refund
                });
            }
        }

        private void ApplyPendingPrintLinesToMemory(PendingReturnCheckpoint pending)
        {
            pendingPrintLines.Clear();
            pendingExchangePrintLines.Clear();
            pendingIsExchange = pending.IsExchange;
            pendingTotalRefund = pending.TotalRefund;
            pendingBalanceDue = pending.BalanceDue;
            currentCustomerName = pending.CustomerName ?? "";
            currentCustomerPhone = pending.CustomerPhone ?? "";
            currentOrderDate = pending.OrderDate;

            foreach (PendingReturnPrintLine line in pending.PrintReturnLines)
            {
                pendingPrintLines.Add(new ReturnReceiptLine
                {
                    ItemName = line.ItemName,
                    Qty = line.Qty,
                    Refund = line.Amount
                });
            }

            foreach (PendingReturnPrintLine line in pending.PrintExchangeLines)
            {
                pendingExchangePrintLines.Add(new ReturnReceiptLine
                {
                    ItemName = line.ItemName,
                    Qty = line.Qty,
                    Refund = line.Amount
                });
            }
        }

        private bool IsReturnAlreadyAppliedInDb(PendingReturnCheckpoint pending)
        {
            if (pending.ReturnLines == null || pending.ReturnLines.Count == 0)
                return false;

            try
            {
                using MySqlConnection con = DB.GetConnection();
                con.Open();
                foreach (PendingReturnLine line in pending.ReturnLines)
                {
                    using MySqlCommand cmd = new MySqlCommand(
                        "SELECT IFNULL(return_qty,0) FROM inv_order_details WHERE id=@id",
                        con);
                    cmd.Parameters.AddWithValue("@id", line.DetailId);
                    object? result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        return false;
                    int actual = Convert.ToInt32(result);
                    if (actual < line.ExpectedReturnQtyAfter)
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ResumeReprintOnly(PendingReturnCheckpoint pending)
        {
            ApplyPendingPrintLinesToMemory(pending);
            if (pendingPrintLines.Count == 0 && pendingExchangePrintLines.Count == 0)
            {
                MessageBox.Show("Pending return print data missing. Clearing resume file.");
                PendingReturnStore.Clear();
                return;
            }

            try
            {
                pending.Stage = PendingReturnStage.PrintStarted;
                PendingReturnStore.Save(pending);

                int dynamicHeight = CalculateReturnPrintHeight();
                PaperSize customSize = new PaperSize("Custom", 300, dynamicHeight);
                printDoc.DefaultPageSettings.PaperSize = customSize;
                PrinterRouting.ApplyReceiptReturnPrinter(printDoc);
                if (!printDoc.PrinterSettings.IsValid)
                {
                    MessageBox.Show(
                        "Return already saved in DB, but no printer found.\nFix printer and open Return again.");
                    return;
                }

                printDoc.Print();
                PendingReturnStore.Clear();
                MessageBox.Show(
                    pending.IsExchange
                        ? "Exchange reprint done ✅\nOrder ID: " + pending.OrderId
                        : "Return reprint done ✅\nOrder ID: " + pending.OrderId);

                try { LoadOrder(pending.OrderId); }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Reprint failed: " + ex.Message +
                    "\nReturn already in DB. Open Return again to retry.");
            }
        }

        /// <summary>
        /// Crash recovery for incomplete Process/Print only.
        /// Pending file is created only after Process Return starts — not while searching/entering qtys.
        /// </summary>
        private void TryResumePendingReturnOnStartup()
        {
            if (pendingReturnResumeChecked)
                return;
            pendingReturnResumeChecked = true;

            if (!PendingReturnStore.Exists())
                return;

            PendingReturnCheckpoint? pending = PendingReturnStore.Load();
            if (pending == null || pending.OrderId <= 0)
            {
                if (pending != null)
                    PendingReturnStore.Clear();
                return;
            }

            bool applied = IsReturnAlreadyAppliedInDb(pending);
            bool hasPrintData =
                (pending.PrintReturnLines != null && pending.PrintReturnLines.Count > 0) ||
                (pending.PrintExchangeLines != null && pending.PrintExchangeLines.Count > 0);

            string msg;
            if (applied && hasPrintData)
            {
                msg =
                    "Incomplete return/exchange print found.\n\n" +
                    $"Order ID: {pending.OrderId}\n" +
                    (pending.IsExchange
                        ? $"Balance due: ₹{pending.BalanceDue:N2}\n"
                        : $"Refund: ₹{pending.TotalRefund:N2}\n") +
                    "DB me save ho chuka, print incomplete.\n\n" +
                    "Reprint now?\n\nYes = reprint  |  No = discard resume";
            }
            else if (applied && !hasPrintData)
            {
                PendingReturnStore.Clear();
                MessageBox.Show(
                    "Return already saved in DB for Order " + pending.OrderId +
                    ", but print snapshot missing. Resume cleared — search order manually if needed.");
                return;
            }
            else
            {
                msg =
                    "Incomplete return/exchange found (Process clicked, DB save not finished).\n\n" +
                    $"Order ID: {pending.OrderId}\n" +
                    $"Return lines: {pending.ReturnLines?.Count ?? 0}\n\n" +
                    "Continue process & print now?\n\nYes = retry  |  No = discard";
            }

            DialogResult dr = MessageBox.Show(
                msg,
                "Resume Return",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr != DialogResult.Yes)
            {
                PendingReturnStore.Clear();
                return;
            }

            if (applied && hasPrintData)
            {
                ResumeReprintOnly(pending);
                return;
            }

            // Restore UI and retry full process.
            try
            {
                txtOrderId.Text = pending.OrderId.ToString();
                LoadOrder(pending.OrderId);

                if (grid.Columns.Contains("id") && grid.Columns.Contains("ReturnQty"))
                {
                    foreach (PendingReturnLine line in pending.ReturnLines)
                    {
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (row.IsNewRow) continue;
                            if (Convert.ToInt32(row.Cells["id"].Value) != line.DetailId)
                                continue;
                            row.Cells["ReturnQty"].Value = line.ReturnQty;
                            break;
                        }
                    }
                    RefreshAllRefundCells();
                }

                ClearExchangeItems();
                foreach (PendingExchangeLine ex in pending.ExchangeLines)
                {
                    dgvExchange.Rows.Add(
                        ex.ItemName,
                        ex.ItemCode,
                        ex.Price.ToString("0.00"),
                        ex.Qty,
                        ex.GstPercent.ToString("0.##"),
                        ex.Net.ToString("0.00"),
                        ex.ItemId,
                        ex.Taxable.ToString("0.00"),
                        ex.GstAmt.ToString("0.00"),
                        ex.Gross.ToString("0.00"));
                }
                RefreshExchangeCalculation();

                BtnProcess_Click(btnProcess, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not resume return: " + ex.Message);
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
            string heading = pendingIsExchange ? "EXCHANGE BILL" : "RETURN RECEIPT";
            SizeF headingSize = g.MeasureString(heading, bold);
            g.DrawString(heading, bold, Brushes.Black, (pageWidth - headingSize.Width) / 2, y);
            y += 15;

            // 3) Return id
            g.DrawString((pendingIsExchange ? "Exchange ID: " : "Return ID: ") + returnId, font, Brushes.Black, 5, y);
            y += 13;

            // 4) Original invoice id
            g.DrawString("Invoice ID: " + txtOrderId.Text, font, Brushes.Black, 5, y);
            y += 13;

            // 5) Date-time (original bill date + process time)
            g.DrawString("Bill Date: " + currentOrderDate.ToString("dd-MM-yyyy HH:mm"), font, Brushes.Black, 5, y);
            y += 13;
            g.DrawString("Processed: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm"), font, Brushes.Black, 5, y);
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

            g.DrawString(pendingIsExchange ? "RETURNED ITEMS" : "ITEMS", bold, Brushes.Black, 5, y);
            y += 14;
            g.DrawString("Item", bold, Brushes.Black, 5, y);
            g.DrawString("Qty", bold, Brushes.Black, 160, y);
            g.DrawString("Amt", bold, Brushes.Black, 220, y);
            y += 15;

            g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
            y += 10;
            decimal totalReturnAmount = 0;

            foreach (ReturnReceiptLine line in pendingPrintLines)
            {
                string name = line.ItemName;
                int qty = line.Qty;
                decimal refund = line.Refund;
                totalReturnAmount += refund;

                if (name.Length > 18)
                {
                    g.DrawString(name.Substring(0, 18), font, Brushes.Black, 5, y);
                    y += 12;
                    g.DrawString(name.Substring(18), font, Brushes.Black, 5, y);
                }
                else
                {
                    g.DrawString(name, font, Brushes.Black, 5, y);
                }

                g.DrawString(qty.ToString(), font, Brushes.Black, 160, y);
                g.DrawString(refund.ToString("0.00"), font, Brushes.Black, 220, y);
                y += 18;
            }

            if (pendingIsExchange)
            {
                y += 6;
                g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
                y += 12;
                g.DrawString("NEW ITEMS", bold, Brushes.Black, 5, y);
                y += 14;
                g.DrawString("Item", bold, Brushes.Black, 5, y);
                g.DrawString("Qty", bold, Brushes.Black, 160, y);
                g.DrawString("Amt", bold, Brushes.Black, 220, y);
                y += 15;
                g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
                y += 10;

                decimal newTotal = 0;
                foreach (ReturnReceiptLine line in pendingExchangePrintLines)
                {
                    string name = line.ItemName;
                    int qty = line.Qty;
                    decimal amt = line.Refund;
                    newTotal += amt;

                    if (name.Length > 18)
                    {
                        g.DrawString(name.Substring(0, 18), font, Brushes.Black, 5, y);
                        y += 12;
                        g.DrawString(name.Substring(18), font, Brushes.Black, 5, y);
                    }
                    else
                    {
                        g.DrawString(name, font, Brushes.Black, 5, y);
                    }

                    g.DrawString(qty.ToString(), font, Brushes.Black, 160, y);
                    g.DrawString(amt.ToString("0.00"), font, Brushes.Black, 220, y);
                    y += 18;
                }

                y += 8;
                g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
                y += 14;
                g.DrawString("Return value : ₹ " + totalReturnAmount.ToString("0.00"), font, Brushes.Black, 5, y);
                y += 14;
                g.DrawString("New items    : ₹ " + newTotal.ToString("0.00"), font, Brushes.Black, 5, y);
                y += 14;
                g.DrawString("BALANCE DUE  : ₹ " + pendingBalanceDue.ToString("0.00"), bold, Brushes.Black, 5, y);
                y += 18;
            }
            else
            {
                y += 10;
                g.DrawString("-----------------------------------------------", font, Brushes.Black, 5, y);
                y += 15;
                g.DrawString("TOTAL REFUND : ₹ " + pendingTotalRefund.ToString("0.00"), bold, Brushes.Black, 5, y);
                y += 20;
            }

            g.DrawString("Thank You!", font, Brushes.Black, 90, y);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            PendingReturnStore.Clear();
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
            currentOrderDate = DateTime.Now;
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
            ClearExchangeItems();
            SetProcessEnabled(false);
            btnReset.Enabled = false;
            btnReset.BackColor = DisabledButtonColor;
        }

        private int CalculateReturnPrintHeight()
        {
            int baseHeight = pendingIsExchange ? 380 : 290;
            int perLineHeight = 18;
            int printableLines = 0;

            foreach (ReturnReceiptLine line in pendingPrintLines)
            {
                string name = line.ItemName ?? "";
                printableLines += name.Length > 18 ? 2 : 1;
            }

            foreach (ReturnReceiptLine line in pendingExchangePrintLines)
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
