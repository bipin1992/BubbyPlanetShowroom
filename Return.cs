using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using ZXing;
using ZXing.Windows.Compatibility;

namespace BubbyPlanetShowroom
{
    public class Return : UserControl
    {
        private const string StoreName = "Bubbyplanet";
        private const string StoreAddressLine1 = "Daudnagar Branch, Aurangabad";
        private const string StoreAddressLine2 = "Bihar - 824143";
        private const string StorePhone = "7870828400";
        private const string StoreEmail = "bubbyplanet@gmail.com";
        private const string StoreWebsite = "bubbyplanet.com";
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
        Label lblPaymentMethod = new Label();
        ComboBox cmbPaymentMethod = new ComboBox();

        PrintDocument printDoc = new PrintDocument();
        private bool isProcessingReturn = false;
        private readonly List<ReturnReceiptLine> pendingPrintLines = new();
        private readonly List<ReturnReceiptLine> pendingExchangePrintLines = new();
        private decimal pendingTotalRefund = 0;
        private decimal pendingBalanceDue = 0;
        private bool pendingIsExchange = false;
        private string pendingPaymentMethod = "Cash";
        private string pendingSettlementType = "";
        private decimal pendingSettlementAmount = 0;
        private string currentCustomerName = "";
        private string currentCustomerPhone = "";
        private DateTime currentOrderDate = DateTime.Now;

        private sealed class ReturnReceiptLine
        {
            public string ItemName { get; set; } = "";
            public string ItemCode { get; set; } = "";
            public string Size { get; set; } = "";
            public int Qty { get; set; }
            public decimal Price { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal Gross { get; set; }
            public decimal Taxable { get; set; }
            public decimal Gst { get; set; }
            public decimal Net { get; set; }
            /// <summary>Alias used by older print/resume paths.</summary>
            public decimal Refund
            {
                get => Net;
                set => Net = value;
            }
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 136f)); // footer calc + action
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

            Panel processHost = new Panel
            {
                Dock = DockStyle.Right,
                Width = 196,
                BackColor = Color.White,
                Padding = new Padding(8, 0, 8, 0)
            };

            Panel calcPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
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
            lblCalcHint.AutoSize = true;
            lblCalcHint.Location = new Point(320, 8);

            lblPaymentMethod.Text = "Payment:";
            lblPaymentMethod.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            lblPaymentMethod.ForeColor = Slate;
            lblPaymentMethod.AutoSize = true;
            lblPaymentMethod.Location = new Point(10, 64);
            lblPaymentMethod.Visible = false;

            cmbPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentMethod.Items.AddRange(new object[] { "Cash", "Online" });
            cmbPaymentMethod.SelectedIndex = 0;
            cmbPaymentMethod.Size = new Size(120, 28);
            cmbPaymentMethod.Font = new Font("Segoe UI", 9.5f);
            cmbPaymentMethod.Location = new Point(118, 60);
            cmbPaymentMethod.Visible = false;

            calcPanel.Controls.Add(lblCalcReturn);
            calcPanel.Controls.Add(lblCalcNew);
            calcPanel.Controls.Add(lblCalcBalance);
            calcPanel.Controls.Add(lblCalcHint);
            calcPanel.Controls.Add(lblPaymentMethod);
            calcPanel.Controls.Add(cmbPaymentMethod);
            calcPanel.Resize += (_, _) => LayoutReturnFooterControls(calcPanel);

            StyleButton(btnProcess, "Process", DisabledButtonColor, 180, 44);
            btnProcess.Enabled = false;
            btnProcess.Click += BtnProcess_Click;
            processHost.Resize += (_, _) =>
            {
                btnProcess.Left = Math.Max(0, (processHost.ClientSize.Width - btnProcess.Width) / 2);
                btnProcess.Top = Math.Max(8, (processHost.ClientSize.Height - btnProcess.Height) / 2);
            };
            processHost.Controls.Add(btnProcess);

            bottomPanel.Controls.Add(calcPanel);
            bottomPanel.Controls.Add(processHost);

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
                LayoutReturnFooterControls(calcPanel);
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
            dgvExchange.Columns.Add("Discount", "Disc %");
            dgvExchange.Columns.Add("Qty", "Qty");
            dgvExchange.Columns.Add("GstPercent", "GST%");
            dgvExchange.Columns.Add("Net", "Net");
            dgvExchange.Columns.Add("ItemId", "ItemId");
            dgvExchange.Columns.Add("Taxable", "Taxable");
            dgvExchange.Columns.Add("GstAmt", "GstAmt");
            dgvExchange.Columns.Add("Gross", "Gross");
            dgvExchange.Columns.Add("AutoDiscount", "AutoDiscount");
            dgvExchange.Columns.Add("ManualDiscount", "ManualDiscount");
            dgvExchange.Columns.Add("DiscountManual", "DiscountManual");
            dgvExchange.Columns["ItemId"].Visible = false;
            dgvExchange.Columns["Taxable"].Visible = false;
            dgvExchange.Columns["GstAmt"].Visible = false;
            dgvExchange.Columns["Gross"].Visible = false;
            dgvExchange.Columns["AutoDiscount"].Visible = false;
            dgvExchange.Columns["ManualDiscount"].Visible = false;
            dgvExchange.Columns["DiscountManual"].Visible = false;
            dgvExchange.Columns["Qty"].ReadOnly = false;
            dgvExchange.Columns["Discount"].ReadOnly = false;
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
                    IFNULL(i.size, '') AS size,
                    od.qty,
                    IFNULL(od.return_qty,0) AS return_qty,
                    (od.qty - IFNULL(od.return_qty,0)) AS remaining_qty,
                    od.selling_price,
                    od.gross_amount,
                    od.discount_percent,
                    od.discount_amount,
                    od.taxable_amount,
                    od.gst_amount,
                    od.net_amount,
                    IFNULL(od.is_exchange, 0) AS is_exchange
                FROM inv_order_details od
                JOIN inv_items_master i ON i.id = od.item_id
                WHERE od.order_id = @orderId
                ORDER BY od.id ASC";

                MySqlDataAdapter da = new MySqlDataAdapter(itemQuery, con);
                da.SelectCommand.Parameters.AddWithValue("@orderId", parsedOrderId);

                // Older DBs may not have is_exchange yet.
                EnsureOrderDetailExchangeColumn(con);

                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("No items found on this order.");
                    return;
                }

                bool hasReturnableItem = false;
                foreach (DataRow drItem in dt.Rows)
                {
                    int remaining = Convert.ToInt32(drItem["remaining_qty"]);
                    if (remaining > 0)
                    {
                        hasReturnableItem = true;
                        break;
                    }
                }

                BindOrderItemsGrid(dt);
                grid.Enabled = true;
                btnReset.Enabled = true;
                btnReset.BackColor = ResetEnabledColor;
                ClearExchangeItems();
                RefreshExchangeCalculation();

                if (!hasReturnableItem)
                {
                    MessageBox.Show(
                        "Is bill ke saare items pehle hi return ho chuke hain.\n" +
                        "Neeche pehle returned / exchange items ka detail dikh raha hai.");
                    SetProcessEnabled(false);
                }
                else
                {
                    SetProcessEnabled(true);
                }
            }
        }

        private void EnsureOrderDetailExchangeColumn(MySqlConnection con)
        {
            try
            {
                DB.EnsureColumnExists(con, "inv_order_details", "is_exchange", "TINYINT(1) NOT NULL DEFAULT 0");
            }
            catch
            {
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
                lblCalcHint.Text = "Pure return mode.\nAdd exchange items to keep same bill.";
                btnProcess.Text = "Process Return";
                UpdateSettlementPaymentUi(
                    summary.ReturnValue > 0 ? "refund" : "",
                    summary.ReturnValue);
            }
            else if (summary.Shortfall > 0)
            {
                lblCalcBalance.Text = "Short by: ₹ " + summary.Shortfall.ToString("0.00");
                lblCalcBalance.ForeColor = Color.FromArgb(220, 38, 38);
                lblCalcHint.Text =
                    "Process band — new items value return se kam hai.\n" +
                    "Aur / mehange items add karein.";
                btnProcess.Text = "Cannot Process";
                UpdateSettlementPaymentUi("", 0);
            }
            else
            {
                lblCalcBalance.Text = "Balance due (collect): ₹ " + summary.BalanceDue.ToString("0.00");
                lblCalcBalance.ForeColor = PrimaryBlue;
                lblCalcHint.Text = summary.BalanceDue == 0
                    ? "Even exchange — no money move.\nSame bill updated + printed."
                    : "Customer pays only the extra.\nCash = counter cash badhega.";
                btnProcess.Text = "Process Exchange";
                UpdateSettlementPaymentUi(
                    summary.BalanceDue > 0 ? "collect" : "",
                    summary.BalanceDue);
            }

            RefreshProcessButtonState(summary);
        }

        /// <summary>
        /// Exchange mall rule: new items &lt; return value → Process stays off.
        /// </summary>
        private void RefreshProcessButtonState(ExchangeSummary? summary = null)
        {
            if (isProcessingReturn)
            {
                SetProcessEnabled(false);
                return;
            }

            if (grid == null || !grid.Enabled || grid.Rows.Count == 0)
            {
                SetProcessEnabled(false);
                return;
            }

            summary ??= ReturnCalculations.CalculateExchange(
                GetCurrentReturnValue(),
                GetCurrentExchangeValue());

            // New items present but cheaper than return → block process.
            if (summary.NewItemsValue > 0 && summary.Shortfall > 0)
            {
                SetProcessEnabled(false);
                return;
            }

            bool hasReturnableLeft = false;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                int qty = Convert.ToInt32(row.Cells["qty"].Value);
                int returned = 0;
                if (grid.Columns.Contains("return_qty"))
                    int.TryParse(row.Cells["return_qty"].Value?.ToString(), out returned);
                if (qty - returned > 0)
                {
                    hasReturnableLeft = true;
                    break;
                }
            }

            SetProcessEnabled(hasReturnableLeft);
        }

        private void UpdateSettlementPaymentUi(string settlementType, decimal amount)
        {
            bool needPayment = amount > 0 &&
                (settlementType == "collect" || settlementType == "refund");

            lblPaymentMethod.Visible = needPayment;
            cmbPaymentMethod.Visible = needPayment;
            cmbPaymentMethod.Enabled = needPayment;

            if (!needPayment)
            {
                lblPaymentMethod.Text = "Payment:";
                LayoutReturnFooterControls(cmbPaymentMethod.Parent);
                return;
            }

            if (settlementType == "collect")
                lblPaymentMethod.Text = "Collect via:";
            else
                lblPaymentMethod.Text = "Refund via:";

            if (cmbPaymentMethod.SelectedIndex < 0)
                cmbPaymentMethod.SelectedIndex = 0;

            LayoutReturnFooterControls(cmbPaymentMethod.Parent);
        }

        private void LayoutReturnFooterControls(Control? calcPanel)
        {
            if (calcPanel == null)
                return;

            int right = calcPanel.ClientSize.Width - 12;
            if (right < 140)
                return;

            lblCalcHint.MaximumSize = new Size(Math.Max(160, right - 240), 0);
            lblCalcHint.Left = Math.Max(lblCalcNew.Right + 16, right - Math.Max(lblCalcHint.Width, 160));
            lblCalcHint.Top = 8;

            cmbPaymentMethod.Width = 120;
            cmbPaymentMethod.Left = right - cmbPaymentMethod.Width;
            cmbPaymentMethod.Top = 58;

            lblPaymentMethod.AutoSize = true;
            lblPaymentMethod.Left = Math.Max(10, cmbPaymentMethod.Left - lblPaymentMethod.Width - 8);
            lblPaymentMethod.Top = 62;
        }

        private string GetSelectedPaymentMethod()
        {
            string method = cmbPaymentMethod?.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(method))
                return "Cash";
            return method;
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
                DB.EnsureAgeDiscountSchema(con);

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
                reader.Close();

                if (stockQty <= 0)
                {
                    MessageBox.Show("Stock not available for this item.");
                    return;
                }

                // Same auto-discount path as Receipt (age/category/staff). No reward on exchange add.
                bool isStaff = AutoDiscountHelper.IsStaffMobile(con, currentCustomerPhone);
                decimal autoDiscount = AutoDiscountHelper.GetAutoDiscountPercent(con, itemCode, isStaff);

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

                        if (!IsExchangeManualDiscountRow(existing))
                        {
                            existing.Cells["AutoDiscount"].Value = autoDiscount;
                            existing.Cells["ManualDiscount"].Value = 0;
                            existing.Cells["DiscountManual"].Value = 0;
                            existing.Cells["Discount"].Value = autoDiscount;
                        }

                        RecalcExchangeRow(existing);
                        RefreshExchangeCalculation();
                        txtExchangeCode.Clear();
                        txtExchangeCode.Focus();
                        return;
                    }
                }

                ReturnCalculations.CalculateLineAmounts(price, gstPercent, autoDiscount, 1,
                    out decimal taxable, out decimal gstAmt, out decimal gross, out decimal net);

                dgvExchange.Rows.Add(
                    itemName,
                    itemCode,
                    price.ToString("0.00"),
                    autoDiscount.ToString("0.##"),
                    1,
                    gstPercent.ToString("0.##"),
                    net.ToString("0.00"),
                    itemId,
                    taxable.ToString("0.00"),
                    gstAmt.ToString("0.00"),
                    gross.ToString("0.00"),
                    autoDiscount,
                    0,
                    0);

                RefreshExchangeCalculation();
                txtExchangeCode.Clear();
                txtExchangeCode.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not add item: " + ex.Message);
            }
        }

        private bool IsExchangeManualDiscountRow(DataGridViewRow row)
        {
            object? manualVal = row.Cells["DiscountManual"]?.Value ?? 0;
            if (manualVal is bool b)
                return b;
            return manualVal.ToString() == "1";
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
            string colName = dgvExchange.Columns[e.ColumnIndex].Name;

            if (colName == "Discount")
            {
                // Manual sale discount lock — same idea as Receipt (no reward stacking here).
                if (!decimal.TryParse(row.Cells["Discount"].Value?.ToString(), out decimal disc) || disc < 0)
                    disc = 0;
                disc = AutoDiscountHelper.ClampDiscount(disc);
                row.Cells["DiscountManual"].Value = 1;
                row.Cells["ManualDiscount"].Value = disc;
                row.Cells["AutoDiscount"].Value = 0;
                row.Cells["Discount"].Value = disc.ToString("0.##");
                RecalcExchangeRow(row);
                RefreshExchangeCalculation();
                return;
            }

            if (colName != "Qty")
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

            decimal discount = 0;
            if (IsExchangeManualDiscountRow(row))
                decimal.TryParse(row.Cells["ManualDiscount"].Value?.ToString(), out discount);
            else if (!decimal.TryParse(row.Cells["Discount"].Value?.ToString(), out discount))
                decimal.TryParse(row.Cells["AutoDiscount"].Value?.ToString(), out discount);

            discount = AutoDiscountHelper.ClampDiscount(discount);
            row.Cells["Discount"].Value = discount.ToString("0.##");

            ReturnCalculations.CalculateLineAmounts(price, gstPercent, discount, qty,
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
                if (grid.Columns.Contains("size"))
                    grid.Columns["size"].HeaderText = "Size";
                grid.Columns["qty"].HeaderText = "Qty";
                grid.Columns["return_qty"].HeaderText = "Returned";
                if (grid.Columns.Contains("remaining_qty"))
                    grid.Columns["remaining_qty"].HeaderText = "Left";
                grid.Columns["selling_price"].HeaderText = "Price";
                grid.Columns["discount_percent"].HeaderText = "Disc %";
                grid.Columns["gst_amount"].HeaderText = "GST";
                grid.Columns["net_amount"].HeaderText = "Net";

                if (grid.Columns.Contains("is_exchange"))
                    grid.Columns["is_exchange"].Visible = false;

                grid.Columns["selling_price"].DefaultCellStyle.Format = "0.00";
                grid.Columns["gst_amount"].DefaultCellStyle.Format = "0.00";
                grid.Columns["net_amount"].DefaultCellStyle.Format = "0.00";
                grid.Columns["discount_percent"].DefaultCellStyle.Format = "0.##";

                // Fixed widths sized so full header text shows; Item takes remaining space.
                // Horizontal scroll appears instead of truncating headers.
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                grid.Columns["item_name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns["item_name"].MinimumWidth = 140;
                grid.Columns["item_name"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                grid.Columns["item_name"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;

                void FixCol(string name, int width, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
                {
                    if (!grid.Columns.Contains(name))
                        return;
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
                codeWidth = Math.Clamp(codeWidth, 110, 280);

                FixCol("item_code", codeWidth, DataGridViewContentAlignment.MiddleLeft);
                grid.Columns["item_code"].DefaultCellStyle.Font = codeFont;
                grid.Columns["item_code"].DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
                grid.Columns["item_code"].DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                grid.Columns["item_code"].DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

                FixCol("size", 70);
                FixCol("qty", 50);
                FixCol("return_qty", 78);
                FixCol("remaining_qty", 55);
                FixCol("selling_price", 72, DataGridViewContentAlignment.MiddleRight);
                FixCol("discount_percent", 70);
                FixCol("gst_amount", 65, DataGridViewContentAlignment.MiddleRight);
                FixCol("net_amount", 80, DataGridViewContentAlignment.MiddleRight);

                if (!grid.Columns.Contains("LineType"))
                {
                    DataGridViewTextBoxColumn typeCol = new DataGridViewTextBoxColumn();
                    typeCol.Name = "LineType";
                    typeCol.HeaderText = "Type";
                    typeCol.ReadOnly = true;
                    typeCol.SortMode = DataGridViewColumnSortMode.NotSortable;
                    grid.Columns.Add(typeCol);
                }
                FixCol("LineType", 85);

                // Returned qty (return_qty) already shows what came back — no separate Status column.
                grid.Columns["return_qty"].HeaderText = "Returned";
                FixCol("return_qty", 78);
                grid.Columns["return_qty"].DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);

                DataGridViewTextBoxColumn returnQtyCol = new DataGridViewTextBoxColumn();
                returnQtyCol.Name = "ReturnQty";
                returnQtyCol.HeaderText = "Return";
                returnQtyCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                returnQtyCol.Width = 70;
                returnQtyCol.MinimumWidth = 70;
                returnQtyCol.Resizable = DataGridViewTriState.False;
                returnQtyCol.SortMode = DataGridViewColumnSortMode.NotSortable;
                grid.Columns.Add(returnQtyCol);

                DataGridViewTextBoxColumn refundCol = new DataGridViewTextBoxColumn();
                refundCol.Name = "Refund";
                refundCol.HeaderText = "Refund";
                refundCol.ReadOnly = true;
                refundCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                refundCol.Width = 85;
                refundCol.MinimumWidth = 85;
                refundCol.Resizable = DataGridViewTriState.False;
                refundCol.SortMode = DataGridViewColumnSortMode.NotSortable;
                refundCol.DefaultCellStyle.Format = "0.00";
                grid.Columns.Add(refundCol);

                int di = 0;
                grid.Columns["item_code"].DisplayIndex = di++;
                grid.Columns["item_name"].DisplayIndex = di++;
                if (grid.Columns.Contains("size"))
                    grid.Columns["size"].DisplayIndex = di++;
                grid.Columns["LineType"].DisplayIndex = di++;
                grid.Columns["qty"].DisplayIndex = di++;
                grid.Columns["return_qty"].DisplayIndex = di++;
                if (grid.Columns.Contains("remaining_qty"))
                    grid.Columns["remaining_qty"].DisplayIndex = di++;
                grid.Columns["selling_price"].DisplayIndex = di++;
                grid.Columns["discount_percent"].DisplayIndex = di++;
                grid.Columns["gst_amount"].DisplayIndex = di++;
                grid.Columns["net_amount"].DisplayIndex = di++;
                grid.Columns["ReturnQty"].DisplayIndex = di++;
                grid.Columns["Refund"].DisplayIndex = di++;

                foreach (DataGridViewColumn column in grid.Columns)
                {
                    column.ReadOnly = true;
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    column.HeaderCell.Style.BackColor = headerBg;
                    column.HeaderCell.Style.ForeColor = Color.White;
                    column.HeaderCell.Style.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
                    column.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
                }

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

                Color fullyReturnedBg = Color.FromArgb(241, 245, 249);
                Color exchangeBg = Color.FromArgb(236, 253, 245);
                Color returnedQtyHighlight = Color.FromArgb(254, 226, 226);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    int qty = Convert.ToInt32(row.Cells["qty"].Value);
                    int returned = Convert.ToInt32(row.Cells["return_qty"].Value);
                    int remaining = grid.Columns.Contains("remaining_qty")
                        ? Convert.ToInt32(row.Cells["remaining_qty"].Value)
                        : Math.Max(0, qty - returned);

                    bool isExchange = false;
                    if (grid.Columns.Contains("is_exchange"))
                    {
                        object? exVal = row.Cells["is_exchange"].Value;
                        isExchange = exVal != null && exVal != DBNull.Value && Convert.ToInt32(exVal) == 1;
                    }

                    row.Cells["LineType"].Value = isExchange ? "New/Exchange" : "Original";
                    row.Cells["ReturnQty"].Value = 0;
                    row.Cells["Refund"].Value = "0.00";

                    // Returned column itself marks prior returns (0 = not returned yet).
                    if (returned > 0)
                    {
                        row.Cells["return_qty"].Style.BackColor = returnedQtyHighlight;
                        row.Cells["return_qty"].Style.ForeColor = Color.FromArgb(185, 28, 28);
                    }

                    if (remaining <= 0)
                    {
                        row.DefaultCellStyle.BackColor = fullyReturnedBg;
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
                        row.Cells["ReturnQty"].ReadOnly = true;
                        row.Cells["ReturnQty"].Style.BackColor = fullyReturnedBg;
                    }
                    else if (isExchange)
                    {
                        row.DefaultCellStyle.BackColor = exchangeBg;
                    }
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
            {
                e.Cancel = true;
                return;
            }

            DataGridViewRow row = grid.Rows[e.RowIndex];
            int qty = Convert.ToInt32(row.Cells["qty"].Value);
            int returned = Convert.ToInt32(row.Cells["return_qty"].Value);
            int remaining = grid.Columns.Contains("remaining_qty")
                ? Convert.ToInt32(row.Cells["remaining_qty"].Value)
                : Math.Max(0, qty - returned);
            if (remaining <= 0)
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

            if (isExchange && (exchangeSummary.Shortfall > 0 || !exchangeSummary.MeetsEqualOrMoreRule))
            {
                MessageBox.Show(
                    "Return process nahi hoga.\n\n" +
                    "Naye items ki value return value se kam hai.\n" +
                    "New items ≥ return value hona zaroori hai.\n\n" +
                    "Return: ₹ " + exchangeSummary.ReturnValue.ToString("0.00") + "\n" +
                    "New items: ₹ " + exchangeSummary.NewItemsValue.ToString("0.00") + "\n" +
                    "Short by: ₹ " + exchangeSummary.Shortfall.ToString("0.00"));
                RefreshProcessButtonState(exchangeSummary);
                return;
            }

            string settlementType = "";
            decimal settlementAmount = 0;
            if (isExchange && exchangeSummary.BalanceDue > 0)
            {
                settlementType = "collect";
                settlementAmount = exchangeSummary.BalanceDue;
            }
            else if (!isExchange && exchangeSummary.ReturnValue > 0)
            {
                settlementType = "refund";
                settlementAmount = exchangeSummary.ReturnValue;
            }

            string paymentMethod = GetSelectedPaymentMethod();
            if (settlementAmount > 0)
            {
                if (cmbPaymentMethod.SelectedIndex < 0 || string.IsNullOrWhiteSpace(paymentMethod))
                {
                    MessageBox.Show(
                        settlementType == "collect"
                            ? "Balance collect ke liye Payment Method choose karein (Cash / Online)."
                            : "Refund ke liye Payment Method choose karein (Cash / Online).");
                    cmbPaymentMethod.Visible = true;
                    cmbPaymentMethod.Focus();
                    return;
                }
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
                exchangeSummary.BalanceDue,
                paymentMethod,
                settlementType,
                settlementAmount);
            PendingReturnStore.Save(pending);

            isProcessingReturn = true;
            SetProcessEnabled(false);
            bool returnCompleted = false;
            pendingPrintLines.Clear();
            pendingExchangePrintLines.Clear();
            pendingTotalRefund = 0;
            pendingBalanceDue = 0;
            pendingIsExchange = isExchange;
            pendingPaymentMethod = paymentMethod;
            pendingSettlementType = settlementType;
            pendingSettlementAmount = settlementAmount;

            try
            {
                using (MySqlConnection con = DB.GetConnection())
                {
                    con.Open();
                    EnsureOrderDetailExchangeColumn(con);
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
                            decimal sellingPrice = 0;
                            decimal discPercent = 0;
                            string size = "";

                            using (MySqlCommand fetchCmd = new MySqlCommand(@"
                                SELECT
                                    od.qty,
                                    IFNULL(od.return_qty, 0) AS return_qty,
                                    od.gross_amount,
                                    od.discount_amount,
                                    IFNULL(od.discount_percent, 0) AS discount_percent,
                                    od.selling_price,
                                    od.taxable_amount,
                                    od.gst_amount,
                                    od.net_amount,
                                    i.item_code,
                                    IFNULL(i.size, '') AS size
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
                                sellingPrice = Convert.ToDecimal(detailReader["selling_price"]);
                                discPercent = Convert.ToDecimal(detailReader["discount_percent"]);
                                size = detailReader["size"]?.ToString() ?? "";
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
                                ItemCode = itemCode,
                                Size = size,
                                Qty = returnNow,
                                Price = sellingPrice,
                                DiscountPercent = discPercent,
                                Gross = Round2(gross - lineResult.NewGrossAmount),
                                Taxable = Round2(subtotalCurrent - lineResult.NewTaxableAmount),
                                Gst = Round2(tax - lineResult.NewGstAmount),
                                Net = refund
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
                                decimal.TryParse(exRow.Cells["Discount"].Value?.ToString(), out decimal discPercent);
                                discPercent = AutoDiscountHelper.ClampDiscount(discPercent);
                                decimal discountAmount = Round2(gross - (taxable + gstAmt));

                                string size = "";
                                try
                                {
                                    using MySqlCommand sizeCmd = new MySqlCommand(
                                        "SELECT IFNULL(size,'') FROM inv_items_master WHERE id=@id LIMIT 1",
                                        con, transaction);
                                    sizeCmd.Parameters.AddWithValue("@id", itemId);
                                    object? sz = sizeCmd.ExecuteScalar();
                                    size = sz?.ToString() ?? "";
                                }
                                catch { }

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
                                        taxable_amount, gst_amount, net_amount,
                                        is_exchange
                                    )
                                    VALUES
                                    (
                                        @oid, @iid, @qty, @price,
                                        @gross, @discPercent, @discAmt,
                                        @taxable, @gst, @net,
                                        1
                                    )", con, transaction);
                                insertCmd.Parameters.AddWithValue("@oid", orderId);
                                insertCmd.Parameters.AddWithValue("@iid", itemId);
                                insertCmd.Parameters.AddWithValue("@qty", qty);
                                insertCmd.Parameters.AddWithValue("@price", price);
                                insertCmd.Parameters.AddWithValue("@gross", Round2(gross));
                                insertCmd.Parameters.AddWithValue("@discPercent", discPercent);
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
                                    ItemCode = itemCode,
                                    Size = size,
                                    Qty = qty,
                                    Price = price,
                                    DiscountPercent = discPercent,
                                    Gross = Round2(gross),
                                    Taxable = Round2(taxable),
                                    Gst = Round2(gstAmt),
                                    Net = Round2(net)
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

                        if (settlementAmount > 0 &&
                            (settlementType == "collect" || settlementType == "refund"))
                        {
                            DB.EnsureReturnSettlementSchema(con);
                            using MySqlCommand settleCmd = new MySqlCommand(@"
                                INSERT INTO inv_return_settlements
                                (order_id, settlement_type, payment_method, amount, created_at)
                                VALUES
                                (@oid, @stype, @pmethod, @amount, NOW())", con, transaction);
                            settleCmd.Parameters.AddWithValue("@oid", orderId);
                            settleCmd.Parameters.AddWithValue("@stype", settlementType);
                            settleCmd.Parameters.AddWithValue("@pmethod", paymentMethod);
                            settleCmd.Parameters.AddWithValue("@amount", Round2(settlementAmount));
                            settleCmd.ExecuteNonQuery();
                        }

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
                            pendingBalanceDue,
                            paymentMethod,
                            settlementType,
                            settlementAmount);
                        FillPendingPrintLines(pending);
                        PendingReturnStore.Save(pending);

                        transaction.Commit();
                        returnCompleted = true;
                        ClosingCashStore.SyncTodaysSavedClosing();

                        if (isExchange)
                        {
                            string payLine = exchangeSummary.BalanceDue > 0
                                ? "\nCollect (" + paymentMethod + "): ₹ " + exchangeSummary.BalanceDue.ToString("0.00")
                                : "\nEven exchange — no money.";
                            if (exchangeSummary.BalanceDue > 0 &&
                                paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                            {
                                payLine += "\n(Counter cash badhega)";
                            }

                            MessageBox.Show(
                                "Exchange Completed on Same Bill\n\n" +
                                "Order ID: " + orderId + "\n" +
                                "Return value: ₹ " + exchangeSummary.ReturnValue.ToString("0.00") + "\n" +
                                "New items: ₹ " + exchangeSummary.NewItemsValue.ToString("0.00") +
                                payLine);
                        }
                        else
                        {
                            string refundLine =
                                "Total Refund (" + paymentMethod + "): ₹ " + totalRefund.ToString("0.00");
                            if (paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                                refundLine += "\n(Counter cash kam hoga)";

                            MessageBox.Show(
                                "Return Completed Successfully\n\n" + refundLine);
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
                        exchangeSummary.BalanceDue,
                        paymentMethod,
                        settlementType,
                        settlementAmount));
                }
                catch { }

                MessageBox.Show("Return failed: " + ex.Message);
                return;
            }
            finally
            {
                isProcessingReturn = false;
                RefreshProcessButtonState();
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

            // Fresh screen for next return (same as Reset).
            CompleteReturnUiReset();
        }

        private void CompleteReturnUiReset()
        {
            pendingPrintLines.Clear();
            pendingExchangePrintLines.Clear();
            pendingTotalRefund = 0;
            pendingBalanceDue = 0;
            pendingIsExchange = false;
            pendingPaymentMethod = "Cash";
            pendingSettlementType = "";
            pendingSettlementAmount = 0;
            txtOrderId.Text = "";
            ResetReturnForm();
            txtOrderId.Focus();
        }

        private PendingReturnCheckpoint BuildPendingReturnCheckpoint(
            PendingReturnStage stage,
            int orderId,
            bool isExchange,
            decimal returnValue,
            decimal exchangeValue,
            decimal totalRefund,
            decimal balanceDue,
            string paymentMethod = "Cash",
            string settlementType = "",
            decimal settlementAmount = 0)
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
                ExchangeValue = exchangeValue,
                PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                SettlementType = settlementType ?? "",
                SettlementAmount = Round2(settlementAmount)
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
                    Net = Convert.ToDecimal(exRow.Cells["Net"].Value),
                    DiscountPercent = decimal.TryParse(exRow.Cells["Discount"].Value?.ToString(), out decimal d) ? d : 0,
                    AutoDiscount = decimal.TryParse(exRow.Cells["AutoDiscount"].Value?.ToString(), out decimal a) ? a : 0,
                    ManualDiscount = decimal.TryParse(exRow.Cells["ManualDiscount"].Value?.ToString(), out decimal m) ? m : 0,
                    DiscountManual = IsExchangeManualDiscountRow(exRow)
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
                checkpoint.PrintReturnLines.Add(ToPendingPrintLine(line));
            }

            foreach (ReturnReceiptLine line in pendingExchangePrintLines)
            {
                checkpoint.PrintExchangeLines.Add(ToPendingPrintLine(line));
            }
        }

        private static PendingReturnPrintLine ToPendingPrintLine(ReturnReceiptLine line)
            => new()
            {
                ItemName = line.ItemName,
                ItemCode = line.ItemCode,
                Size = line.Size,
                Qty = line.Qty,
                Price = line.Price,
                DiscountPercent = line.DiscountPercent,
                Gross = line.Gross,
                Taxable = line.Taxable,
                Gst = line.Gst,
                Net = line.Net
            };

        private static ReturnReceiptLine FromPendingPrintLine(PendingReturnPrintLine line)
            => new()
            {
                ItemName = line.ItemName,
                ItemCode = line.ItemCode,
                Size = line.Size,
                Qty = line.Qty,
                Price = line.Price,
                DiscountPercent = line.DiscountPercent,
                Gross = line.Gross,
                Taxable = line.Taxable,
                Gst = line.Gst,
                Net = line.Net != 0 ? line.Net : line.Amount
            };

        private void ApplyPendingPrintLinesToMemory(PendingReturnCheckpoint pending)
        {
            pendingPrintLines.Clear();
            pendingExchangePrintLines.Clear();
            pendingIsExchange = pending.IsExchange;
            pendingTotalRefund = pending.TotalRefund;
            pendingBalanceDue = pending.BalanceDue;
            pendingPaymentMethod = string.IsNullOrWhiteSpace(pending.PaymentMethod) ? "Cash" : pending.PaymentMethod;
            pendingSettlementType = pending.SettlementType ?? "";
            pendingSettlementAmount = pending.SettlementAmount;
            currentCustomerName = pending.CustomerName ?? "";
            currentCustomerPhone = pending.CustomerPhone ?? "";
            currentOrderDate = pending.OrderDate;

            foreach (PendingReturnPrintLine line in pending.PrintReturnLines)
                pendingPrintLines.Add(FromPendingPrintLine(line));

            foreach (PendingReturnPrintLine line in pending.PrintExchangeLines)
                pendingExchangePrintLines.Add(FromPendingPrintLine(line));
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

                CompleteReturnUiReset();
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
                        ex.DiscountPercent.ToString("0.##"),
                        ex.Qty,
                        ex.GstPercent.ToString("0.##"),
                        ex.Net.ToString("0.00"),
                        ex.ItemId,
                        ex.Taxable.ToString("0.00"),
                        ex.GstAmt.ToString("0.00"),
                        ex.Gross.ToString("0.00"),
                        ex.AutoDiscount,
                        ex.ManualDiscount,
                        ex.DiscountManual ? 1 : 0);
                }
                RefreshExchangeCalculation();

                if (!string.IsNullOrWhiteSpace(pending.PaymentMethod) && cmbPaymentMethod.Items.Count > 0)
                {
                    int idx = cmbPaymentMethod.FindStringExact(pending.PaymentMethod);
                    if (idx >= 0)
                        cmbPaymentMethod.SelectedIndex = idx;
                }

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

            int pageWidth = e.PageSettings.PaperSize.Width;
            Font headerFont = new Font("Segoe UI", 12, FontStyle.Bold);
            Font normalFont = new Font("Segoe UI", 8);
            Font boldFont = new Font("Segoe UI", 8, FontStyle.Bold);
            Font totalFont = new Font("Segoe UI", 10, FontStyle.Bold);
            Font policyFont = new Font("Segoe UI", 7);
            Font policyHeaderFont = new Font("Segoe UI", 8, FontStyle.Bold);
            Font billTypeFont = new Font("Segoe UI", 10, FontStyle.Bold);

            float y = 5;
            int itemNumber = 1;
            int gatewayQuantity = 0;
            decimal taxableTotal = 0;
            decimal gstTotal = 0;
            decimal netTotalReturned = 0;
            decimal netTotalNew = 0;

            // ===== Header (same as Receipt) =====
            string title = StoreName;
            SizeF titleSize = g.MeasureString(title, headerFont);
            g.DrawString(title, headerFont, Brushes.Black, (pageWidth - titleSize.Width) / 2, y);
            y += 20;
            SizeF add1Size = g.MeasureString(StoreAddressLine1, normalFont);
            g.DrawString(StoreAddressLine1, normalFont, Brushes.Black, (pageWidth - add1Size.Width) / 2, y);
            y += 13;
            SizeF add2Size = g.MeasureString(StoreAddressLine2, normalFont);
            g.DrawString(StoreAddressLine2, normalFont, Brushes.Black, (pageWidth - add2Size.Width) / 2, y);
            y += 15;

            string billHeading = "RETURN BILL";
            SizeF billHeadingSize = g.MeasureString(billHeading, billTypeFont);
            g.DrawString(billHeading, billTypeFont, Brushes.Black, (pageWidth - billHeadingSize.Width) / 2, y);
            y += 18;

            string invoiceText = "Invoice: " + (txtOrderId.Text.Trim().Length > 0 ? txtOrderId.Text.Trim() : "?");
            g.DrawString(invoiceText, normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm"), normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Original Bill: " + currentOrderDate.ToString("dd-MM-yyyy HH:mm"), normalFont, Brushes.Black, 5, y);
            y += 15;

            string customer = string.IsNullOrWhiteSpace(currentCustomerName) ? "Walk-in" : currentCustomerName;
            string phone = string.IsNullOrWhiteSpace(currentCustomerPhone) ? "-" : currentCustomerPhone;
            g.DrawString("Customer: " + customer, normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Mobile: " + phone, normalFont, Brushes.Black, 5, y);
            y += 15;

            g.DrawString(new string('-', 48), normalFont, Brushes.Black, 5, y);
            y += 15;

            // ===== Returned items (Receipt-style lines) =====
            g.DrawString("RETURNED ITEMS", boldFont, Brushes.Black, 5, y);
            y += 15;

            foreach (ReturnReceiptLine line in pendingPrintLines)
            {
                y = DrawReceiptStyleItem(g, pageWidth, y, normalFont, boldFont, itemNumber++, line);
                gatewayQuantity += line.Qty;
                taxableTotal += line.Taxable;
                gstTotal += line.Gst;
                netTotalReturned += line.Net;
            }

            // ===== New exchange items =====
            if (pendingIsExchange && pendingExchangePrintLines.Count > 0)
            {
                g.DrawString("NEW ITEMS", boldFont, Brushes.Black, 5, y);
                y += 15;

                foreach (ReturnReceiptLine line in pendingExchangePrintLines)
                {
                    y = DrawReceiptStyleItem(g, pageWidth, y, normalFont, boldFont, itemNumber++, line);
                    gatewayQuantity += line.Qty;
                    taxableTotal += line.Taxable;
                    gstTotal += line.Gst;
                    netTotalNew += line.Net;
                }
            }

            g.DrawString("Gate check quantity: " + gatewayQuantity, totalFont, Brushes.Black, 5, y);
            y += 15;

            g.DrawString(new string('-', 48), normalFont, Brushes.Black, 5, y);
            y += 15;

            if (pendingIsExchange)
            {
                g.DrawString("RETURN VALUE: " + netTotalReturned.ToString("0.00"), totalFont, Brushes.Black, 5, y);
                y += 18;
                g.DrawString("NEW ITEMS: " + netTotalNew.ToString("0.00"), totalFont, Brushes.Black, 5, y);
                y += 18;
                g.DrawString("BALANCE DUE: " + pendingBalanceDue.ToString("0.00"), totalFont, Brushes.Black, 5, y);
                y += 18;
                if (pendingBalanceDue > 0)
                {
                    g.DrawString("Payment: " + pendingPaymentMethod, boldFont, Brushes.Black, 5, y);
                    y += 22;
                }
                else
                {
                    y += 4;
                }
            }
            else
            {
                g.DrawString("TOTAL REFUND: " + pendingTotalRefund.ToString("0.00"), totalFont, Brushes.Black, 5, y);
                y += 18;
                if (pendingTotalRefund > 0)
                {
                    g.DrawString("Refund via: " + pendingPaymentMethod, boldFont, Brushes.Black, 5, y);
                    y += 22;
                }
                else
                {
                    y += 4;
                }
            }

            g.DrawString("Taxable: " + Round2(taxableTotal).ToString("0.00"), normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("GST: " + Round2(gstTotal).ToString("0.00"), normalFont, Brushes.Black, 5, y);
            y += 15;
            decimal cgstTotal = Round2(gstTotal / 2m);
            decimal sgstTotal = Round2(gstTotal - cgstTotal);
            g.DrawString("CGST: " + cgstTotal.ToString("0.00") + "  SGST: " + sgstTotal.ToString("0.00"), normalFont, Brushes.Black, 5, y);
            y += 18;

            g.DrawString("Phone: " + StorePhone, normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Email: " + StoreEmail, normalFont, Brushes.Black, 5, y);
            y += 15;
            g.DrawString("Website: " + StoreWebsite, normalFont, Brushes.Black, 5, y);
            y += 22;

            g.DrawString(new string('-', 48), normalFont, Brushes.Black, 5, y);
            y += 14;
            g.DrawString("Return Policy", policyHeaderFont, Brushes.Black, 5, y);
            y += 13;
            g.DrawString("1. Return window: within 7 days from bill date.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("2. Original bill/invoice required.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("3. Item must be unused, unwashed, tags and box intact.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("4. Garments (altered/stitched) are non-returnable.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("5. Footwear with used/dirty sole non-returnable; box required.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("6. Toys opened/damaged seal/battery-used usually non-returnable,", policyFont, Brushes.Black, 5, y);
            y += 10;
            g.DrawString("   unless manufacturing defect.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("7. Socks/innerwear/accessories mostly non-returnable.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("8. No cash refund. Exchange only for same or higher value item.", policyFont, Brushes.Black, 5, y);
            y += 11;
            g.DrawString("9. Counter checks: barcode match, tag match, invoice match.", policyFont, Brushes.Black, 5, y);
            y += 16;

            if (int.TryParse(txtOrderId.Text.Trim(), out int orderIdForBarcode) && orderIdForBarcode > 0)
            {
                using Bitmap? barcodeImage = GenerateOrderBarcode(orderIdForBarcode.ToString());
                if (barcodeImage != null)
                {
                    int barcodeWidth = 180;
                    int barcodeHeight = 50;
                    float barcodeX = (pageWidth - barcodeWidth) / 2f;
                    g.DrawImage(barcodeImage, barcodeX, y, barcodeWidth, barcodeHeight);
                    y += barcodeHeight + 5;

                    Font textFont = new Font("Segoe UI", 9, FontStyle.Bold);
                    string orderText = orderIdForBarcode.ToString();
                    SizeF textSize = g.MeasureString(orderText, textFont);
                    g.DrawString(orderText, textFont, Brushes.Black, (pageWidth - textSize.Width) / 2, y);
                    y += textSize.Height + 5;
                }
            }

            string thankYou = "Thank you for shopping with us";
            SizeF thankSize = g.MeasureString(thankYou, normalFont);
            g.DrawString(thankYou, normalFont, Brushes.Black, (pageWidth - thankSize.Width) / 2, y);

            e.HasMorePages = false;
        }

        private float DrawReceiptStyleItem(
            Graphics g,
            int pageWidth,
            float y,
            Font normalFont,
            Font boldFont,
            int itemNumber,
            ReturnReceiptLine line)
        {
            string name = line.ItemName ?? "";
            string itemCode = line.ItemCode ?? "";
            string size = string.IsNullOrWhiteSpace(line.Size) ? "-" : line.Size;
            int qtyVal = line.Qty;
            decimal priceVal = line.Price;
            decimal grossVal = line.Gross;
            decimal subtotalVal = line.Taxable;
            decimal gstVal = line.Gst;
            decimal totalVal = line.Net;
            decimal discountPercentVal = line.DiscountPercent;
            if (discountPercentVal < 0) discountPercentVal = 0;

            decimal originalTotalInclTax = Round2(priceVal * qtyVal);
            decimal discountAmountInclTax = Round2(originalTotalInclTax - totalVal);
            if (discountAmountInclTax < 0) discountAmountInclTax = 0;

            g.DrawString($"Item {itemNumber}: {name}", boldFont, Brushes.Black, 5, y);
            y += 13;
            g.DrawString($"Code: {itemCode}  Size : {size}", normalFont, Brushes.Black, 4, y);
            y += 13;
            g.DrawString($"Price: {priceVal:0.00}  Qty: {qtyVal}  Gross: {grossVal:0.00}", normalFont, Brushes.Black, 4, y);
            y += 13;
            g.DrawString($"Discount : {discountPercentVal:0.##}% (-{discountAmountInclTax:0.00})", normalFont, Brushes.Black, 4, y);
            y += 13;
            g.DrawString($"Taxable: {subtotalVal:0.00}  GST: {gstVal:0.00}", normalFont, Brushes.Black, 4, y);
            y += 13;
            g.DrawString($"Net: {totalVal:0.00}", boldFont, Brushes.Black, 6, y);
            y += 15;
            g.DrawString("--------------------------------", normalFont, Brushes.Black, 5, y);
            y += 15;
            return y;
        }

        private Bitmap? GenerateOrderBarcode(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                var writer = new BarcodeWriter<Bitmap>
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = 180,
                        Height = 50,
                        Margin = 1
                    },
                    Renderer = new BitmapRenderer()
                };
                return writer.Write(text.Trim());
            }
            catch
            {
                return null;
            }
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

            pendingPrintLines.Clear();
            pendingExchangePrintLines.Clear();
            pendingTotalRefund = 0;
            pendingBalanceDue = 0;
            pendingIsExchange = false;
            pendingPaymentMethod = "Cash";
            pendingSettlementType = "";
            pendingSettlementAmount = 0;

            if (cmbPaymentMethod.Items.Count > 0)
                cmbPaymentMethod.SelectedIndex = 0;
            UpdateSettlementPaymentUi("", 0);
        }

        private int CalculateReturnPrintHeight()
        {
            // Receipt-style: ~110px per item block + header/footer/policy/barcode
            int itemCount = pendingPrintLines.Count + pendingExchangePrintLines.Count;
            if (itemCount <= 0) itemCount = 1;
            int baseHeight = 520;
            int perItem = 110;
            return baseHeight + (itemCount * perItem);
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
