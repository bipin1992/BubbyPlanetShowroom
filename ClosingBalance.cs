using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class ClosingBalance : UserControl
    {
        private readonly TextBox txtClosingDate = new TextBox();
        private readonly TextBox txtOpeningBalance = new TextBox();
        private readonly TextBox txtCashSales = new TextBox();
        private readonly TextBox txtCounterCash = new TextBox();

        private readonly TextBox txtCashInAmount = new TextBox();
        private readonly TextBox txtCashInReason = new TextBox();
        private readonly TextBox txtCashInTotal = new TextBox();

        private readonly TextBox txtCashOutAmount = new TextBox();
        private readonly TextBox txtCashOutReason = new TextBox();
        private readonly TextBox txtCashOutTotal = new TextBox();

        private readonly TextBox txtOwnerCash = new TextBox();
        private readonly TextBox txtClosingBalance = new TextBox();
        private readonly TextBox txtExpectedOwnerCash = new TextBox();
        private readonly TextBox txtDifference = new TextBox();

        private readonly DataGridView cashInGrid = new DataGridView();
        private readonly DataGridView cashOutGrid = new DataGridView();
        private readonly DataGridView todayBillsGrid = new DataGridView();
        private readonly DataGridView grid = new DataGridView();
        private readonly DataTable cashInEntries = CreateEntryTable();
        private readonly DataTable cashOutEntries = CreateEntryTable();
        private readonly Label lblStatus = new Label();
        private readonly Label lblTodayBills = new Label();
        private List<ClosingCashBillLine> todayCashBillLines = new List<ClosingCashBillLine>();

        private readonly Color pageBack = Color.FromArgb(245, 247, 251);
        private readonly Color textMain = Color.FromArgb(28, 37, 65);
        private readonly Color textMuted = Color.FromArgb(104, 116, 140);
        private readonly Color navy = Color.FromArgb(21, 32, 55);
        private readonly Color green = Color.FromArgb(22, 163, 74);
        private readonly Color red = Color.FromArgb(220, 38, 38);
        private readonly Color panelSoft = Color.FromArgb(248, 250, 252);

        public ClosingBalance()
        {
            InitializeUI();
            EnsureSchema();
            ClearFields();
            LoadRecentClosings();
        }

        private static DataTable CreateEntryTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("Reason", typeof(string));
            return table;
        }

        private void InitializeUI()
        {
            Dock = DockStyle.Fill;
            BackColor = pageBack;
            Padding = new Padding(18);
            AutoScroll = true;

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = pageBack
            };

            Label title = new Label
            {
                Text = "Closing Balance",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = textMain,
                Location = new Point(0, 2)
            };

            Label subtitle = new Label
            {
                Text = "Counter pe total cash = aaj ka opening + cash sale. Online extra bills mein already kata hai — Cash-OUT mein dubara mat daalo. Owner aur drawer bharo — Difference 0 hona chahiye.",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = textMuted,
                Location = new Point(3, 42)
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            TableLayoutPanel settlementPanel = CreateSettlementPanel();
            settlementPanel.Dock = DockStyle.Top;
            settlementPanel.Height = 86;
            settlementPanel.Padding = new Padding(8, 6, 8, 0);

            Button btnAddCashIn = CreateButton("Add IN", green, 0, 0, 86);
            btnAddCashIn.Click += (s, e) => AddCashEntry(cashInEntries, txtCashInAmount, txtCashInReason, "Cash-IN");

            Button btnRemoveCashIn = CreateButton("Remove", Color.FromArgb(100, 116, 139), 0, 0, 88);
            btnRemoveCashIn.Click += (s, e) => RemoveSelectedEntry(cashInGrid, cashInEntries);

            Button btnAddCashOut = CreateButton("Add OUT", red, 0, 0, 86);
            btnAddCashOut.Click += (s, e) => AddCashEntry(cashOutEntries, txtCashOutAmount, txtCashOutReason, "Cash-OUT");

            Button btnRemoveCashOut = CreateButton("Remove", Color.FromArgb(100, 116, 139), 0, 0, 88);
            btnRemoveCashOut.Click += (s, e) => RemoveSelectedEntry(cashOutGrid, cashOutEntries);

            ConfigureEntryGrid(cashInGrid, cashInEntries);
            ConfigureEntryGrid(cashOutGrid, cashOutEntries);

            Panel cashInPanel = CreateMovementPanel("Cash-IN", txtCashInAmount, txtCashInReason, btnAddCashIn, cashInGrid, btnRemoveCashIn, green);
            Panel cashOutPanel = CreateMovementPanel("Cash-OUT", txtCashOutAmount, txtCashOutReason, btnAddCashOut, cashOutGrid, btnRemoveCashOut, red);
            Panel todayBillsPanel = CreateTodayBillsPanel();

            TableLayoutPanel listsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(4)
            };
            listsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            listsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            listsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            listsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listsLayout.Controls.Add(cashInPanel, 0, 0);
            listsLayout.Controls.Add(todayBillsPanel, 1, 0);
            listsLayout.Controls.Add(cashOutPanel, 2, 0);

            Panel formPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 470,
                BackColor = Color.White,
                Padding = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle
            };
            formPanel.Controls.Add(listsLayout);
            formPanel.Controls.Add(settlementPanel);

            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 28;
            lblStatus.ForeColor = textMuted;
            lblStatus.Font = new Font("Segoe UI", 9);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            ConfigureTextBoxes();
            ConfigureGrid();

            Panel gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label recentTitle = new Label
            {
                Text = "Recent Closing Records",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold),
                ForeColor = textMain
            };

            gridPanel.Controls.Add(grid);
            gridPanel.Controls.Add(lblStatus);
            gridPanel.Controls.Add(recentTitle);

            Controls.Add(gridPanel);
            Controls.Add(formPanel);
            Controls.Add(header);
        }

        private Button CreateButton(string text, Color backColor, int left, int top, int width)
        {
            Button button = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Location = new Point(left, top),
                Size = new Size(width, 34)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private Panel CreateMovementPanel(
            string titleText,
            TextBox amountBox,
            TextBox reasonBox,
            Button addButton,
            DataGridView entryGrid,
            Button removeButton,
            Color accentColor)
        {
            Panel section = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = panelSoft,
                Padding = new Padding(10),
                Margin = new Padding(6),
                BorderStyle = BorderStyle.FixedSingle
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = panelSoft,
                ColumnCount = 2,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label title = new Label
            {
                Text = titleText,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = textMain,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 2);

            Panel amountPanel = CreateFieldPanel("Amount", amountBox);
            Panel reasonPanel = CreateFieldPanel("Reason", reasonBox);
            layout.Controls.Add(amountPanel, 0, 1);
            layout.Controls.Add(reasonPanel, 1, 1);

            addButton.Dock = DockStyle.Fill;
            addButton.Margin = new Padding(6, 4, 4, 2);
            layout.Controls.Add(addButton, 0, 2);

            removeButton.Dock = DockStyle.Fill;
            removeButton.Margin = new Padding(4, 4, 6, 2);
            layout.Controls.Add(removeButton, 1, 2);

            entryGrid.Dock = DockStyle.Fill;
            entryGrid.Margin = new Padding(0, 6, 0, 0);
            layout.Controls.Add(entryGrid, 0, 3);
            layout.SetColumnSpan(entryGrid, 2);

            Panel accent = new Panel
            {
                BackColor = accentColor,
                Dock = DockStyle.Left,
                Width = 4
            };
            section.Controls.Add(layout);
            section.Controls.Add(accent);
            return section;
        }

        private TableLayoutPanel CreateSettlementPanel()
        {
            TableLayoutPanel settlement = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(0, 4, 0, 0)
            };
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            settlement.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            AddField(settlement, "Counter pe total cash", txtCounterCash, 0, 0);
            AddField(settlement, "Owner ko kitna diya", txtOwnerCash, 1, 0);
            AddField(settlement, "Drawer me kitna bacha", txtClosingBalance, 2, 0);
            AddField(settlement, "Difference", txtDifference, 3, 0);

            Button btnSave = CreateButton("Save Closing", green, 0, 0, 124);
            btnSave.Click += BtnSave_Click;
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnSave.Location = new Point(0, 23);
            btnSave.Size = new Size(124, 34);

            Panel savePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(6)
            };
            savePanel.Resize += (s, e) => { btnSave.Width = savePanel.Width; };
            savePanel.Controls.Add(btnSave);
            settlement.Controls.Add(savePanel, 4, 0);

            return settlement;
        }

        private Panel CreateTodayBillsPanel()
        {
            Panel section = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = panelSoft,
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(6, 2, 6, 2),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblTodayBills.Text = "Aaj ki cash bills";
            lblTodayBills.Dock = DockStyle.Top;
            lblTodayBills.Height = 36;
            lblTodayBills.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            lblTodayBills.ForeColor = textMain;
            lblTodayBills.TextAlign = ContentAlignment.MiddleLeft;
            lblTodayBills.AutoEllipsis = true;

            ConfigureTodayBillsGrid();
            todayBillsGrid.Dock = DockStyle.Fill;

            section.Controls.Add(todayBillsGrid);
            section.Controls.Add(lblTodayBills);
            return section;
        }

        private void ConfigureTodayBillsGrid()
        {
            todayBillsGrid.BorderStyle = BorderStyle.None;
            todayBillsGrid.BackgroundColor = Color.White;
            todayBillsGrid.RowHeadersVisible = false;
            todayBillsGrid.AllowUserToAddRows = false;
            todayBillsGrid.AllowUserToDeleteRows = false;
            todayBillsGrid.ReadOnly = true;
            todayBillsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            todayBillsGrid.MultiSelect = false;
            todayBillsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            todayBillsGrid.ScrollBars = ScrollBars.Vertical;
            todayBillsGrid.ColumnHeadersHeight = 30;
            todayBillsGrid.RowTemplate.Height = 28;
            todayBillsGrid.EnableHeadersVisualStyles = false;
            todayBillsGrid.ColumnHeadersDefaultCellStyle.BackColor = navy;
            todayBillsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            todayBillsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            todayBillsGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            todayBillsGrid.DefaultCellStyle.ForeColor = textMain;
            todayBillsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            todayBillsGrid.DefaultCellStyle.SelectionForeColor = textMain;
            todayBillsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            todayBillsGrid.Columns.Add("Bill", "Bill");
            todayBillsGrid.Columns.Add("Time", "Time");
            todayBillsGrid.Columns.Add("Type", "Type");
            todayBillsGrid.Columns.Add("Amount", "Amount");
            todayBillsGrid.Columns["Bill"].FillWeight = 22;
            todayBillsGrid.Columns["Time"].FillWeight = 18;
            todayBillsGrid.Columns["Type"].FillWeight = 32;
            todayBillsGrid.Columns["Amount"].FillWeight = 28;
            todayBillsGrid.Columns["Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            todayBillsGrid.Columns["Amount"].DefaultCellStyle.Format = "N2";
        }

        private Panel CreateFieldPanel(string labelText, TextBox textBox)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(6)
            };

            Label label = new Label
            {
                Text = labelText,
                Location = new Point(0, 0),
                Size = new Size(160, 19),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoEllipsis = true,
                Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
                ForeColor = textMuted
            };

            textBox.Location = new Point(0, 23);
            textBox.Width = 160;
            textBox.Height = 27;
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox.Font = new Font("Segoe UI", 10);
            textBox.BorderStyle = BorderStyle.FixedSingle;

            panel.Resize += (s, e) =>
            {
                label.Width = panel.Width;
                textBox.Width = panel.Width;
            };

            panel.Controls.Add(label);
            panel.Controls.Add(textBox);
            return panel;
        }

        private void AddField(TableLayoutPanel parent, string labelText, TextBox textBox, int column, int row)
        {
            parent.Controls.Add(CreateFieldPanel(labelText, textBox), column, row);
        }

        private Label CreateSectionLabel(string text, int left, int top, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(left, top),
                Size = new Size(width, 20),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = textMain
            };
        }

        private void AddField(Panel parent, string labelText, TextBox textBox, int left, int top, int width)
        {
            Label label = new Label
            {
                Text = labelText,
                Location = new Point(left, top),
                Size = new Size(width, 19),
                Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
                ForeColor = textMuted
            };

            textBox.Location = new Point(left, top + 22);
            textBox.Size = new Size(width, 27);
            textBox.Font = new Font("Segoe UI", 10);

            parent.Controls.Add(label);
            parent.Controls.Add(textBox);
        }

        private void ConfigureTextBoxes()
        {
            TextBox[] amountInputs = { txtCashInAmount, txtCashOutAmount, txtOwnerCash, txtClosingBalance };
            foreach (TextBox txt in amountInputs)
            {
                txt.KeyPress += OnlyDecimal_KeyPress;
            }

            txtOwnerCash.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            txtOwnerCash.BackColor = Color.FromArgb(254, 249, 195);
            txtOwnerCash.TextChanged += (s, e) => Recalculate();
            txtOwnerCash.KeyUp += (s, e) => Recalculate();

            txtClosingBalance.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            txtClosingBalance.BackColor = Color.FromArgb(254, 249, 195);
            txtClosingBalance.TextChanged += (s, e) => Recalculate();
            txtClosingBalance.KeyUp += (s, e) => Recalculate();

            txtDifference.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            txtCounterCash.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            txtExpectedOwnerCash.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);

            TextBox[] calculated =
            {
                txtClosingDate, txtOpeningBalance, txtCashSales,
                txtCashInTotal, txtCashOutTotal, txtCounterCash,
                txtExpectedOwnerCash, txtDifference
            };

            foreach (TextBox txt in calculated)
            {
                txt.ReadOnly = true;
                txt.BackColor = Color.FromArgb(248, 250, 252);
                txt.ForeColor = textMain;
            }

            txtCounterCash.BackColor = Color.FromArgb(219, 234, 254);
        }

        private void ConfigureEntryGrid(DataGridView entryGrid, DataTable source)
        {
            entryGrid.DataSource = source;
            entryGrid.BorderStyle = BorderStyle.FixedSingle;
            entryGrid.BackgroundColor = Color.White;
            entryGrid.RowHeadersVisible = false;
            entryGrid.AllowUserToAddRows = false;
            entryGrid.AllowUserToDeleteRows = false;
            entryGrid.ReadOnly = true;
            entryGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            entryGrid.MultiSelect = false;
            entryGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            entryGrid.ColumnHeadersHeight = 28;
            entryGrid.RowTemplate.Height = 32;
            entryGrid.EnableHeadersVisualStyles = false;
            entryGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(71, 85, 105);
            entryGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            entryGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            entryGrid.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f);
            entryGrid.DefaultCellStyle.ForeColor = textMain;
            entryGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            entryGrid.DefaultCellStyle.SelectionForeColor = textMain;
            entryGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            entryGrid.DataBindingComplete += (s, e) => ApplyEntryGridColumnLayout(entryGrid);
            ApplyEntryGridColumnLayout(entryGrid);
        }

        private void ApplyEntryGridColumnLayout(DataGridView entryGrid)
        {
            if (entryGrid.Columns.Contains("Amount"))
            {
                entryGrid.Columns["Amount"].DefaultCellStyle.Format = "0.00";
                entryGrid.Columns["Amount"].FillWeight = 18;
                entryGrid.Columns["Amount"].MinimumWidth = 80;
            }

            if (entryGrid.Columns.Contains("Reason"))
            {
                entryGrid.Columns["Reason"].FillWeight = 70;
                entryGrid.Columns["Reason"].MinimumWidth = 90;
            }
        }

        private void ConfigureGrid()
        {
            grid.Dock = DockStyle.Fill;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Color.White;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ScrollBars = ScrollBars.Both;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.RowTemplate.Height = 34;
            grid.GridColor = Color.FromArgb(226, 232, 240);
            grid.ColumnHeadersDefaultCellStyle.BackColor = navy;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            grid.DefaultCellStyle.ForeColor = textMain;
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = textMain;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.CellFormatting += Grid_CellFormatting;
            grid.DataBindingComplete += (s, e) => ApplyRecentGridColumns();
        }

        private void ApplyRecentGridColumns()
        {
            SetRecentColumn("Date", 11, 90, false);
            SetRecentColumn("Status", 10, 90, false);
            SetRecentColumn("Opening", 11, 90, true);
            SetRecentColumn("Cash Sale", 12, 100, true);
            SetRecentColumn("Cash In", 9, 80, true);
            SetRecentColumn("Cash Out", 9, 80, true);
            SetRecentColumn("Drawer", 11, 90, true);
            SetRecentColumn("Owner", 11, 90, true);
            SetRecentColumn("Difference", 11, 90, true);
            SetRecentColumn("User", 10, 80, false);
            SetRecentColumn("Saved At", 15, 130, false);
        }

        private void SetRecentColumn(string name, float fillWeight, int minWidth, bool money)
        {
            if (!grid.Columns.Contains(name))
                return;

            DataGridViewColumn col = grid.Columns[name];
            col.FillWeight = fillWeight;
            col.MinimumWidth = minWidth;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            col.SortMode = DataGridViewColumnSortMode.Automatic;
            if (money)
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.DefaultCellStyle.Format = "N2";
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            string name = grid.Columns[e.ColumnIndex].Name;
            if (name == "Status" && e.Value?.ToString() == "Shop Closed")
            {
                e.CellStyle.ForeColor = Color.FromArgb(217, 119, 6);
                e.CellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
                return;
            }

            if (name == "Difference" && e.Value != null && e.Value != DBNull.Value)
            {
                decimal amount = Convert.ToDecimal(e.Value);
                if (amount != 0)
                    e.CellStyle.ForeColor = red;
                else
                    e.CellStyle.ForeColor = green;
            }
        }

        private void EnsureSchema()
        {
            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureClosingBalanceSchema(conn);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Schema check failed: " + ex.Message;
            }
        }

        private void AddCashEntry(DataTable table, TextBox amountBox, TextBox reasonBox, string label)
        {
            decimal amount = ReadAmount(amountBox);
            if (amount <= 0)
            {
                MessageBox.Show(label + " amount 0 se bada hona chahiye.");
                amountBox.Focus();
                return;
            }

            string reason = reasonBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show(label + " reason required hai.");
                reasonBox.Focus();
                return;
            }

            if (string.Equals(label, "Cash-OUT", StringComparison.OrdinalIgnoreCase)
                && WouldDuplicateOnlineExtra(amount, reason))
            {
                MessageBox.Show("Yeh amount already aaj ki cash bills mein Online extra se kat chuka hai. Cash-OUT mein dubara mat daalo.");
                amountBox.Focus();
                return;
            }

            table.Rows.Add(amount, reason);
            amountBox.Text = "0";
            reasonBox.Clear();
            amountBox.Focus();
            Recalculate();
        }

        private void RemoveSelectedEntry(DataGridView entryGrid, DataTable source)
        {
            if (entryGrid.CurrentRow == null || entryGrid.CurrentRow.Index < 0)
                return;

            int index = entryGrid.CurrentRow.Index;
            if (index >= source.Rows.Count)
                return;

            source.Rows.RemoveAt(index);
            Recalculate();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveClosing(shopClosed: false);
        }

        private void BtnShopClosed_Click(object sender, EventArgs e)
        {
            if (!LoadAutoAmounts())
                return;

            decimal cashSales = ReadAmount(txtCashSales);
            if (cashSales != 0)
            {
                MessageBox.Show("Aaj cash sale/return hai. Shop Closed sirf tab mark karein jab shop band ho.");
                return;
            }

            if (cashInEntries.Rows.Count > 0 || cashOutEntries.Rows.Count > 0)
            {
                MessageBox.Show("Cash IN/OUT entries hain. Pehle unhe hatao ya normal closing save karo.");
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Aaj shop band mark karein?" + Environment.NewLine + Environment.NewLine +
                "Last closing ka counter aaj ke opening aur closing dono me same rahega. Owner cash 0 hoga.",
                "Shop Closed",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            txtOwnerCash.Text = "0";
            SetAmount(txtClosingBalance, ReadAmount(txtOpeningBalance));
            Recalculate();
            SaveClosing(shopClosed: true);
        }

        private void SaveClosing(bool shopClosed)
        {
            DateTime closingDate = DateTime.Today;
            txtClosingDate.Text = closingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (!ValidateInput())
                return;

            if (!LoadAutoAmounts())
                return;

            if (shopClosed)
            {
                if (ReadAmount(txtCashSales) != 0)
                {
                    MessageBox.Show("Aaj cash sale/return hai. Shop Closed save nahi ho sakta.");
                    return;
                }

                txtOwnerCash.Text = "0";
                SetAmount(txtClosingBalance, ReadAmount(txtOpeningBalance));
                Recalculate();
            }
            else if (!ConfirmMissingSalesDays(closingDate))
            {
                return;
            }
            else
            {
                Recalculate();
            }

            decimal openingBalance = ReadAmount(txtOpeningBalance);
            decimal cashSales = ReadAmount(txtCashSales);
            decimal cashIn = GetEntryTotal(cashInEntries);
            decimal cashOut = GetCountableCashOut();
            decimal ownerCash = ReadAmount(txtOwnerCash);
            decimal closingBalance = ReadAmount(txtClosingBalance);
            decimal counterCash = ReadAmount(txtCounterCash);
            decimal expectedOwnerCash = ReadAmount(txtExpectedOwnerCash);
            decimal difference = ClosingCashCalculations.ActualDifference(
                counterCash, closingBalance, ownerCash);
            string user = string.IsNullOrWhiteSpace(LoginForm.LoggedInUser) ? "Unknown" : LoginForm.LoggedInUser;
            string role = string.IsNullOrWhiteSpace(MainForm.CurrentRole) ? "Unknown" : MainForm.CurrentRole;
            string note = shopClosed ? "Shop Closed - balance carried forward" : "";

            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureClosingBalanceSchema(conn);

                using MySqlTransaction tx = conn.BeginTransaction();

                int filledClosedDays = FillClosedGapDays(conn, tx, closingDate, openingBalance, user, role);

                int closingId = SaveClosingSummary(
                    conn,
                    tx,
                    closingDate,
                    openingBalance,
                    cashSales,
                    cashIn,
                    cashOut,
                    ownerCash,
                    closingBalance,
                    counterCash,
                    expectedOwnerCash,
                    difference,
                    user,
                    role,
                    shopClosed,
                    note);

                ReplaceMovementEntries(conn, tx, closingId, closingDate, user, cashInEntries, "IN");
                ReplaceMovementEntries(conn, tx, closingId, closingDate, user, cashOutEntries, "OUT");

                tx.Commit();

                string extra = filledClosedDays > 0
                    ? Environment.NewLine + "Beech ke " + filledClosedDays.ToString(CultureInfo.InvariantCulture) +
                      " shop-closed din last closing se carry-forward ho gaye."
                    : "";

                MessageBox.Show(shopClosed
                    ? "Shop closed mark ho gaya. Last closing counter carry-forward ho gaya." + extra
                    : "Closing balance saved/updated." + extra);
                LoadAutoAmounts();
                LoadRecentClosings();
                Recalculate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to save closing balance: " + ex.Message);
            }
        }

        private int SaveClosingSummary(
            MySqlConnection conn,
            MySqlTransaction tx,
            DateTime closingDate,
            decimal openingBalance,
            decimal cashSales,
            decimal cashIn,
            decimal cashOut,
            decimal ownerCash,
            decimal closingBalance,
            decimal counterCash,
            decimal expectedOwnerCash,
            decimal difference,
            string user,
            string role,
            bool shopClosed = false,
            string note = "")
        {
            string query = @"
INSERT INTO daily_cash_closing
(
    closing_date, opening_balance, cash_sales, other_cash_in, cash_in_reason,
    other_cash_out, cash_out_reason, counter_left_for_tomorrow, cash_given_to_owner,
    total_cash_in_hand, total_cash_out, available_before_closing, expected_owner_cash, difference_amount,
    note, is_shop_closed, created_by_user, created_by_role
)
VALUES
(
    @closing_date, @opening_balance, @cash_sales, @cash_in, @cash_in_reason,
    @cash_out, @cash_out_reason, @closing_balance, @owner_cash,
    @total_cash_in_hand, @total_cash_out, @available_before_closing, @expected_owner_cash, @difference_amount,
    @note, @is_shop_closed, @created_by_user, @created_by_role
)
ON DUPLICATE KEY UPDATE
    opening_balance = VALUES(opening_balance),
    cash_sales = VALUES(cash_sales),
    other_cash_in = VALUES(other_cash_in),
    cash_in_reason = VALUES(cash_in_reason),
    shop_expense = 0,
    staff_advance = 0,
    vendor_payment = 0,
    bank_deposit = 0,
    refund_amount = 0,
    other_cash_out = VALUES(other_cash_out),
    cash_out_reason = VALUES(cash_out_reason),
    counter_left_for_tomorrow = VALUES(counter_left_for_tomorrow),
    cash_given_to_owner = VALUES(cash_given_to_owner),
    total_cash_in_hand = VALUES(total_cash_in_hand),
    total_cash_out = VALUES(total_cash_out),
    available_before_closing = VALUES(available_before_closing),
    expected_owner_cash = VALUES(expected_owner_cash),
    difference_amount = VALUES(difference_amount),
    note = VALUES(note),
    is_shop_closed = VALUES(is_shop_closed),
    created_by_user = VALUES(created_by_user),
    created_by_role = VALUES(created_by_role),
    created_at = CURRENT_TIMESTAMP;";

            using MySqlCommand cmd = new MySqlCommand(query, conn, tx);
            cmd.Parameters.AddWithValue("@closing_date", closingDate.Date);
            cmd.Parameters.AddWithValue("@opening_balance", openingBalance);
            cmd.Parameters.AddWithValue("@cash_sales", cashSales);
            cmd.Parameters.AddWithValue("@cash_in", cashIn);
            cmd.Parameters.AddWithValue("@cash_in_reason", shopClosed ? "Shop Closed" : BuildReasonSummary(cashInEntries));
            cmd.Parameters.AddWithValue("@cash_out", cashOut);
            cmd.Parameters.AddWithValue("@cash_out_reason", shopClosed ? "Shop Closed" : BuildReasonSummary(cashOutEntries));
            cmd.Parameters.AddWithValue("@closing_balance", closingBalance);
            cmd.Parameters.AddWithValue("@owner_cash", ownerCash);
            cmd.Parameters.AddWithValue("@total_cash_in_hand", openingBalance + cashSales + cashIn);
            cmd.Parameters.AddWithValue("@total_cash_out", cashOut);
            cmd.Parameters.AddWithValue("@available_before_closing", counterCash);
            cmd.Parameters.AddWithValue("@expected_owner_cash", expectedOwnerCash);
            cmd.Parameters.AddWithValue("@difference_amount", difference);
            cmd.Parameters.AddWithValue("@note", note ?? "");
            cmd.Parameters.AddWithValue("@is_shop_closed", shopClosed ? 1 : 0);
            cmd.Parameters.AddWithValue("@created_by_user", user);
            cmd.Parameters.AddWithValue("@created_by_role", role);

            cmd.ExecuteNonQuery();

            using MySqlCommand idCmd = new MySqlCommand(
                "SELECT id FROM daily_cash_closing WHERE closing_date = @closing_date LIMIT 1",
                conn,
                tx);
            idCmd.Parameters.AddWithValue("@closing_date", closingDate.Date);
            object result = idCmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new Exception("Closing id not found after save.");

            return Convert.ToInt32(result);
        }

        private void ReplaceMovementEntries(
            MySqlConnection conn,
            MySqlTransaction tx,
            int closingId,
            DateTime closingDate,
            string user,
            DataTable entries,
            string movementType)
        {
            using (MySqlCommand deleteCmd = new MySqlCommand(
                "DELETE FROM daily_cash_movements WHERE closing_id=@closing_id AND movement_type=@movement_type",
                conn,
                tx))
            {
                deleteCmd.Parameters.AddWithValue("@closing_id", closingId);
                deleteCmd.Parameters.AddWithValue("@movement_type", movementType);
                deleteCmd.ExecuteNonQuery();
            }

            string insertQuery = @"
INSERT INTO daily_cash_movements
(
    closing_id, movement_date, movement_type, amount, reason, created_by_user
)
VALUES
(
    @closing_id, @movement_date, @movement_type, @amount, @reason, @created_by_user
);";

            foreach (DataRow row in entries.Rows)
            {
                using MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn, tx);
                insertCmd.Parameters.AddWithValue("@closing_id", closingId);
                insertCmd.Parameters.AddWithValue("@movement_date", closingDate.Date);
                insertCmd.Parameters.AddWithValue("@movement_type", movementType);
                insertCmd.Parameters.AddWithValue("@amount", Convert.ToDecimal(row["Amount"]));
                insertCmd.Parameters.AddWithValue("@reason", row["Reason"].ToString());
                insertCmd.Parameters.AddWithValue("@created_by_user", user);
                insertCmd.ExecuteNonQuery();
            }
        }

        private bool ValidateInput()
        {
            if (!ValidateNonNegative(txtOwnerCash, "Owner cash"))
                return false;

            if (!ValidateNonNegative(txtClosingBalance, "Drawer me kitna bacha"))
                return false;

            return true;
        }

        private bool ValidateNonNegative(TextBox textBox, string label)
        {
            decimal amount = ReadAmount(textBox);
            if (amount >= 0)
                return true;

            MessageBox.Show(label + " negative nahi ho sakta.");
            textBox.Focus();
            return false;
        }

        private bool isRecalculating;

        private void Recalculate()
        {
            if (isRecalculating)
                return;

            isRecalculating = true;
            try
            {
                StripCashOutsThatDuplicateOnlineExtras();

                decimal openingBalance = ReadAmount(txtOpeningBalance);
                decimal cashSales = ReadAmount(txtCashSales);
                decimal cashIn = GetEntryTotal(cashInEntries);
                decimal cashOut = GetCountableCashOut();
                decimal ownerCash = ReadAmount(txtOwnerCash);
                decimal closingBalance = ReadAmount(txtClosingBalance);

                decimal counterCash = ClosingCashCalculations.CounterTotal(
                    openingBalance, cashSales, cashIn, cashOut);
                decimal expectedOwnerCash = counterCash - closingBalance;
                decimal actualDifference = ClosingCashCalculations.ActualDifference(
                    counterCash, closingBalance, ownerCash);
                decimal shownDifference = ClosingCashCalculations.ShownDifference(
                    counterCash, closingBalance, ownerCash);

                SetAmount(txtCashInTotal, cashIn);
                SetAmount(txtCashOutTotal, cashOut);
                SetAmount(txtCounterCash, counterCash);
                SetAmount(txtExpectedOwnerCash, expectedOwnerCash);
                SetAmount(txtDifference, shownDifference);

                txtDifference.ForeColor = shownDifference == 0 ? green : red;
                txtDifference.BackColor = shownDifference == 0
                    ? Color.FromArgb(220, 252, 231)
                    : Color.FromArgb(254, 226, 226);
            }
            finally
            {
                isRecalculating = false;
            }
        }

        private decimal GetEntryTotal(DataTable entries)
        {
            decimal total = 0;
            foreach (DataRow row in entries.Rows)
            {
                total += Convert.ToDecimal(row["Amount"]);
            }

            return total;
        }

        private List<decimal> GetTodayOnlineExtras()
        {
            return ClosingCashCalculations.OnlineExtraAmounts(todayCashBillLines);
        }

        private bool WouldDuplicateOnlineExtra(decimal amount, string reason)
        {
            List<decimal> remaining = GetTodayOnlineExtras();
            foreach (DataRow row in cashOutEntries.Rows)
            {
                ClosingCashCalculations.TryConsumeDuplicateOnlineCashOut(
                    Convert.ToDecimal(row["Amount"]),
                    row["Reason"]?.ToString() ?? "",
                    remaining);
            }

            return ClosingCashCalculations.TryConsumeDuplicateOnlineCashOut(amount, reason, remaining);
        }

        private void StripCashOutsThatDuplicateOnlineExtras()
        {
            List<decimal> remaining = GetTodayOnlineExtras();
            for (int i = cashOutEntries.Rows.Count - 1; i >= 0; i--)
            {
                decimal amount = Convert.ToDecimal(cashOutEntries.Rows[i]["Amount"]);
                string reason = cashOutEntries.Rows[i]["Reason"]?.ToString() ?? "";
                if (ClosingCashCalculations.TryConsumeDuplicateOnlineCashOut(amount, reason, remaining))
                    cashOutEntries.Rows.RemoveAt(i);
            }
        }

        private decimal GetCountableCashOut()
        {
            var amounts = new List<decimal>();
            var reasons = new List<string>();
            foreach (DataRow row in cashOutEntries.Rows)
            {
                amounts.Add(Convert.ToDecimal(row["Amount"]));
                reasons.Add(row["Reason"]?.ToString() ?? "");
            }

            return ClosingCashCalculations.CountableCashOut(amounts, reasons, GetTodayOnlineExtras());
        }

        private string BuildReasonSummary(DataTable entries)
        {
            StringBuilder summary = new StringBuilder();
            int index = 1;

            foreach (DataRow row in entries.Rows)
            {
                if (summary.Length > 0)
                    summary.Append(" | ");

                summary.Append(index.ToString(CultureInfo.InvariantCulture));
                summary.Append(". Rs.");
                summary.Append(Convert.ToDecimal(row["Amount"]).ToString("0.00", CultureInfo.InvariantCulture));
                summary.Append(" - ");
                summary.Append(row["Reason"].ToString());
                index++;
            }

            string text = summary.ToString();
            return text.Length <= 300 ? text : text.Substring(0, 300);
        }

        private void LoadRecentClosings()
        {
            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureClosingBalanceSchema(conn);

                string query = @"
SELECT
    DATE_FORMAT(closing_date, '%d-%m-%Y') AS Date,
    CASE WHEN IFNULL(is_shop_closed, 0) = 1 THEN 'Shop Closed' ELSE 'Open' END AS Status,
    opening_balance AS Opening,
    cash_sales AS `Cash Sale`,
    other_cash_in AS `Cash In`,
    other_cash_out AS `Cash Out`,
    counter_left_for_tomorrow AS Drawer,
    cash_given_to_owner AS Owner,
    difference_amount AS Difference,
    created_by_user AS `User`,
    DATE_FORMAT(created_at, '%d-%m-%Y %h:%i %p') AS `Saved At`
FROM daily_cash_closing
ORDER BY closing_date DESC, id DESC
LIMIT 100;";

                using MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                grid.DataSource = dt;
                lblStatus.Text = "Records loaded: " + dt.Rows.Count;
            }
            catch (Exception ex)
            {
                grid.DataSource = null;
                lblStatus.Text = "Unable to load closing records: " + ex.Message;
            }
        }

        private void ClearFields()
        {
            txtClosingDate.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            txtCashInAmount.Text = "0";
            txtCashInReason.Clear();
            txtCashOutAmount.Text = "0";
            txtCashOutReason.Clear();
            txtOwnerCash.Text = "0";
            txtClosingBalance.Text = "0";
            cashInEntries.Clear();
            cashOutEntries.Clear();
            LoadAutoAmounts();
            LoadExistingClosingForToday();
            Recalculate();
        }

        private void LoadExistingClosingForToday()
        {
            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureClosingBalanceSchema(conn);

                string query = @"
SELECT
    id,
    cash_sales,
    other_cash_in,
    cash_in_reason,
    other_cash_out,
    cash_out_reason,
    counter_left_for_tomorrow,
    cash_given_to_owner
FROM daily_cash_closing
WHERE closing_date = @closing_date
LIMIT 1;";

                using MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@closing_date", DateTime.Today.Date);

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return;

                int closingId = Convert.ToInt32(reader["id"]);
                decimal savedCashSales = Convert.ToDecimal(reader["cash_sales"], CultureInfo.InvariantCulture);
                decimal cashInTotal = Convert.ToDecimal(reader["other_cash_in"]);
                decimal cashOutTotal = Convert.ToDecimal(reader["other_cash_out"]);
                string cashInReason = reader["cash_in_reason"]?.ToString() ?? "";
                string cashOutReason = reader["cash_out_reason"]?.ToString() ?? "";
                decimal savedLeftover = Convert.ToDecimal(reader["counter_left_for_tomorrow"]);
                decimal savedOwner = Convert.ToDecimal(reader["cash_given_to_owner"]);
                reader.Close();

                decimal lateCash = ClosingCashCalculations.LateCashDelta(savedCashSales, ReadAmount(txtCashSales));
                SetAmount(txtClosingBalance, savedLeftover);
                SetAmount(txtOwnerCash, savedOwner);

                int inRows = LoadMovementEntries(conn, closingId, "IN", cashInEntries);
                int outRows = LoadMovementEntries(conn, closingId, "OUT", cashOutEntries);

                if (inRows == 0 && cashInTotal > 0)
                    cashInEntries.Rows.Add(cashInTotal, cashInReason);

                if (outRows == 0 && cashOutTotal > 0)
                    cashOutEntries.Rows.Add(cashOutTotal, cashOutReason);

                lblStatus.Text = lateCash == 0
                    ? "Today's saved closing loaded for update."
                    : lateCash > 0
                        ? "Save ke baad Rs." + lateCash.ToString("0.00", CultureInfo.InvariantCulture) +
                          " extra cash bill aayi. Difference me dikh raha hai."
                        : "Save ke baad Rs." + Math.Abs(lateCash).ToString("0.00", CultureInfo.InvariantCulture) +
                          " cash kam hui. Difference me dikh raha hai.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Unable to load today's saved closing: " + ex.Message;
            }
        }

        private int LoadMovementEntries(MySqlConnection conn, int closingId, string movementType, DataTable target)
        {
            string query = @"
SELECT amount, reason
FROM daily_cash_movements
WHERE closing_id = @closing_id
  AND movement_type = @movement_type
ORDER BY id ASC;";

            using MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@closing_id", closingId);
            cmd.Parameters.AddWithValue("@movement_type", movementType);

            int count = 0;
            using MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                target.Rows.Add(Convert.ToDecimal(reader["amount"]), reader["reason"].ToString());
                count++;
            }

            return count;
        }

        private void LoadTodayCashBills(DateTime day)
        {
            todayBillsGrid.Rows.Clear();
            todayCashBillLines = new List<ClosingCashBillLine>();
            try
            {
                List<ClosingCashBillLine> lines = ClosingCashStore.GetCashBillLines(day);
                todayCashBillLines = lines;
                decimal total = 0;
                foreach (ClosingCashBillLine line in lines)
                {
                    todayBillsGrid.Rows.Add(
                        "#" + line.OrderId.ToString(CultureInfo.InvariantCulture),
                        line.At.ToString("HH:mm", CultureInfo.InvariantCulture),
                        line.Kind,
                        line.Amount.ToString("0.00", CultureInfo.InvariantCulture));
                    total += line.Amount;
                }

                decimal opening = ReadAmount(txtOpeningBalance);
                string openingText = "Opening Rs." + opening.ToString("0.00", CultureInfo.InvariantCulture);
                string billsText = lines.Count == 0
                    ? "Aaj ki cash bills — koi bill nahi"
                    : "Aaj ki cash bills: " + lines.Count.ToString(CultureInfo.InvariantCulture) +
                      "  =  Rs." + total.ToString("0.00", CultureInfo.InvariantCulture);

                lblTodayBills.Text = openingText + "   ·   " + billsText;
            }
            catch (Exception ex)
            {
                lblTodayBills.Text = "Aaj ki cash bills load nahi hui: " + ex.Message;
            }
        }

        private bool LoadAutoAmounts()
        {
            try
            {
                DateTime today = DateTime.Today;
                decimal opening = 0;
                if (TryGetPreviousClosing(today, out DateTime lastDate, out decimal lastAmount))
                {
                    opening = lastAmount;
                    lblStatus.Text = BuildCarryForwardStatus(today, lastDate, lastAmount);
                }
                else
                {
                    lblStatus.Text = "Pehli closing. Opening 0.00 se start ho raha hai.";
                }

                txtOpeningBalance.Text = opening.ToString("0.00", CultureInfo.InvariantCulture);
                txtCashSales.Text = GetCashSalesFromDb(today).ToString("0.00", CultureInfo.InvariantCulture);
                LoadTodayCashBills(today);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load auto amounts: " + ex.Message);
                lblStatus.Text = "Unable to load auto amounts: " + ex.Message;
                return false;
            }
        }

        private string BuildCarryForwardStatus(DateTime today, DateTime lastDate, decimal lastAmount)
        {
            string lastText = lastDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            string amountText = lastAmount.ToString("0.00", CultureInfo.InvariantCulture);
            List<DateTime> missingDays = GetMissingClosingDates(lastDate, today);

            if (missingDays.Count == 0)
            {
                return "Opening Rs." + amountText + " last closing (" + lastText + ") se.";
            }

            List<string> closedDates = new List<string>();
            List<string> salesMissing = new List<string>();
            foreach (DateTime day in missingDays)
            {
                string dayText = day.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                decimal sales = GetCashSalesFromDb(day);
                if (sales == 0)
                    closedDates.Add(dayText);
                else
                    salesMissing.Add(dayText + " Rs." + sales.ToString("0.00", CultureInfo.InvariantCulture));
            }

            string status = "Opening Rs." + amountText + " last closing (" + lastText + ") se.";
            if (closedDates.Count > 0)
                status += " Shop closed/missing: " + string.Join(", ", closedDates) + ".";
            if (salesMissing.Count > 0)
                status += " Warning: in din ka closing missing hai aur cash sale bhi hai: " + string.Join(", ", salesMissing) + ".";

            return status;
        }

        private bool ConfirmMissingSalesDays(DateTime today)
        {
            if (!TryGetPreviousClosing(today, out DateTime lastDate, out _))
                return true;

            List<string> salesMissing = new List<string>();
            foreach (DateTime day in GetMissingClosingDates(lastDate, today))
            {
                decimal sales = GetCashSalesFromDb(day);
                if (sales == 0)
                    continue;

                salesMissing.Add(
                    day.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) +
                    "  Rs." +
                    sales.ToString("0.00", CultureInfo.InvariantCulture));
            }

            if (salesMissing.Count == 0)
                return true;

            DialogResult go = MessageBox.Show(
                "In din ka closing missing hai aur cash sale bhi hai:" + Environment.NewLine +
                string.Join(Environment.NewLine, salesMissing) + Environment.NewLine + Environment.NewLine +
                "Continue karoge to un din ki cash aaj ke opening me nahi aayegi.",
                "Missing closing",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return go == DialogResult.Yes;
        }

        private bool TryGetPreviousClosing(DateTime date, out DateTime lastDate, out decimal amount)
        {
            lastDate = DateTime.MinValue;
            amount = 0;

            using MySqlConnection conn = DB.GetConnection();
            conn.Open();
            DB.EnsureClosingBalanceSchema(conn);

            string query = @"
SELECT closing_date, counter_left_for_tomorrow
FROM daily_cash_closing
WHERE closing_date < @closing_date
ORDER BY closing_date DESC, id DESC
LIMIT 1;";

            using MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@closing_date", date.Date);
            using MySqlDataReader reader = cmd.ExecuteReader();
            if (!reader.Read())
                return false;

            lastDate = Convert.ToDateTime(reader["closing_date"]).Date;
            amount = Convert.ToDecimal(reader["counter_left_for_tomorrow"], CultureInfo.InvariantCulture);
            return true;
        }

        private static List<DateTime> GetMissingClosingDates(DateTime lastClosingDate, DateTime today)
        {
            List<DateTime> dates = new List<DateTime>();
            for (DateTime day = lastClosingDate.Date.AddDays(1); day < today.Date; day = day.AddDays(1))
                dates.Add(day);

            return dates;
        }

        private int FillClosedGapDays(
            MySqlConnection conn,
            MySqlTransaction tx,
            DateTime today,
            decimal carryAmount,
            string user,
            string role)
        {
            if (!TryGetPreviousClosing(today, out DateTime lastDate, out decimal lastAmount))
                return 0;

            if (carryAmount == 0)
                carryAmount = lastAmount;

            int filled = 0;
            foreach (DateTime day in GetMissingClosingDates(lastDate, today))
            {
                if (ClosingExists(conn, tx, day))
                    continue;

                if (GetCashSalesFromDb(day) != 0)
                    continue;

                SaveClosingSummary(
                    conn,
                    tx,
                    day,
                    carryAmount,
                    0,
                    0,
                    0,
                    0,
                    carryAmount,
                    carryAmount,
                    0,
                    0,
                    user,
                    role,
                    shopClosed: true,
                    note: "Shop Closed - balance carried forward");
                filled++;
            }

            return filled;
        }

        private static bool ClosingExists(MySqlConnection conn, MySqlTransaction tx, DateTime date)
        {
            using MySqlCommand cmd = new MySqlCommand(
                "SELECT 1 FROM daily_cash_closing WHERE closing_date = @closing_date LIMIT 1",
                conn,
                tx);
            cmd.Parameters.AddWithValue("@closing_date", date.Date);
            object result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value;
        }

        private decimal GetCashSalesFromDb(DateTime date)
        {
            return ClosingCashStore.GetCashSalesFromDb(date);
        }

        private decimal ReadAmount(TextBox textBox)
        {
            if (decimal.TryParse(textBox.Text.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal amount))
                return amount;

            return 0;
        }

        private void SetAmount(TextBox textBox, decimal amount)
        {
            textBox.Text = amount.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void OnlyDecimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (sender is not TextBox txt)
                return;

            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                e.Handled = true;

            if (e.KeyChar == '.' && txt.Text.Contains("."))
                e.Handled = true;
        }
    }
}
