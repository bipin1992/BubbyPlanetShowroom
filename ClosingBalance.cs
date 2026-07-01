using System;
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
        private readonly DataGridView grid = new DataGridView();
        private readonly DataTable cashInEntries = CreateEntryTable();
        private readonly DataTable cashOutEntries = CreateEntryTable();
        private readonly Label lblStatus = new Label();

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
                Text = "Daily counter cash-in, cash-out, owner handover and tomorrow opening balance",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = textMuted,
                Location = new Point(3, 42)
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            Panel formPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 520,
                BackColor = Color.White,
                Padding = new Padding(14),
                BorderStyle = BorderStyle.FixedSingle
            };

            TableLayoutPanel formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 4,
                RowCount = 3
            };
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            AddField(formLayout, "Closing Date", txtClosingDate, 0, 0);
            AddField(formLayout, "Opening Balance Auto", txtOpeningBalance, 1, 0);
            AddField(formLayout, "Cash Sale From DB", txtCashSales, 2, 0);
            AddField(formLayout, "Counter Cash", txtCounterCash, 3, 0);

            Button btnAddCashIn = CreateButton("Add IN", green, 0, 0, 86);
            btnAddCashIn.Click += (s, e) => AddCashEntry(cashInEntries, txtCashInAmount, txtCashInReason, "Cash-IN");

            Button btnRemoveCashIn = CreateButton("Remove IN", Color.FromArgb(100, 116, 139), 0, 0, 96);
            btnRemoveCashIn.Click += (s, e) => RemoveSelectedEntry(cashInGrid, cashInEntries);

            Button btnAddCashOut = CreateButton("Add OUT", red, 0, 0, 86);
            btnAddCashOut.Click += (s, e) => AddCashEntry(cashOutEntries, txtCashOutAmount, txtCashOutReason, "Cash-OUT");

            Button btnRemoveCashOut = CreateButton("Remove OUT", Color.FromArgb(100, 116, 139), 0, 0, 104);
            btnRemoveCashOut.Click += (s, e) => RemoveSelectedEntry(cashOutGrid, cashOutEntries);

            ConfigureEntryGrid(cashInGrid, cashInEntries);
            ConfigureEntryGrid(cashOutGrid, cashOutEntries);

            Panel cashInPanel = CreateMovementPanel("Cash-IN Entries", txtCashInAmount, txtCashInReason, btnAddCashIn, cashInGrid, btnRemoveCashIn, green);
            Panel cashOutPanel = CreateMovementPanel("Cash-OUT Entries", txtCashOutAmount, txtCashOutReason, btnAddCashOut, cashOutGrid, btnRemoveCashOut, red);
            formLayout.Controls.Add(cashInPanel, 0, 1);
            formLayout.SetColumnSpan(cashInPanel, 2);
            formLayout.Controls.Add(cashOutPanel, 2, 1);
            formLayout.SetColumnSpan(cashOutPanel, 2);

            TableLayoutPanel settlementPanel = CreateSettlementPanel();
            formLayout.Controls.Add(settlementPanel, 0, 2);
            formLayout.SetColumnSpan(settlementPanel, 4);
            formPanel.Controls.Add(formLayout);

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
                ColumnCount = 3,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            Label title = new Label
            {
                Text = titleText,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = textMain,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 3);

            Panel amountPanel = CreateFieldPanel("Amount", amountBox);
            Panel reasonPanel = CreateFieldPanel("Reason", reasonBox);
            layout.Controls.Add(amountPanel, 0, 1);
            layout.Controls.Add(reasonPanel, 1, 1);

            addButton.Dock = DockStyle.Fill;
            addButton.Margin = new Padding(6, 18, 0, 5);
            layout.Controls.Add(addButton, 2, 1);

            entryGrid.Dock = DockStyle.Fill;
            entryGrid.Margin = new Padding(0, 10, 0, 4);
            layout.Controls.Add(entryGrid, 0, 2);
            layout.SetColumnSpan(entryGrid, 3);

            Panel footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = panelSoft
            };

            removeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            removeButton.Location = new Point(footer.Width - removeButton.Width, 4);
            removeButton.Margin = new Padding(0);
            footer.Resize += (s, e) =>
            {
                removeButton.Location = new Point(Math.Max(0, footer.Width - removeButton.Width), 4);
            };
            footer.Controls.Add(removeButton);

            Panel accent = new Panel
            {
                BackColor = accentColor,
                Dock = DockStyle.Left,
                Width = 4
            };
            footer.Controls.Add(accent);

            layout.Controls.Add(footer, 0, 3);
            layout.SetColumnSpan(footer, 3);

            section.Controls.Add(layout);
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
                Padding = new Padding(0, 8, 0, 0)
            };
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            settlement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            settlement.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            AddField(settlement, "Owner Ko Cash Diya", txtOwnerCash, 0, 0);
            AddField(settlement, "Closing Balance", txtClosingBalance, 1, 0);
            AddField(settlement, "Expected Owner Cash", txtExpectedOwnerCash, 2, 0);
            AddField(settlement, "Difference", txtDifference, 3, 0);

            Button btnSave = CreateButton("Save Closing", green, 0, 0, 124);
            btnSave.Click += BtnSave_Click;

            Button btnClear = CreateButton("Clear", Color.FromArgb(71, 85, 105), 0, 0, 78);
            btnClear.Click += (s, e) => ClearFields();

            Button btnRefresh = CreateButton("Refresh", Color.FromArgb(37, 99, 235), 0, 0, 92);
            btnRefresh.Click += (s, e) =>
            {
                LoadAutoAmounts();
                Recalculate();
            };

            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(6, 19, 0, 0),
                Margin = new Padding(6, 0, 0, 0),
                BackColor = Color.White
            };
            actions.Controls.Add(btnSave);
            actions.Controls.Add(btnClear);
            actions.Controls.Add(btnRefresh);
            settlement.Controls.Add(actions, 4, 0);

            return settlement;
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

            txtOwnerCash.TextChanged += (s, e) => Recalculate();
            txtClosingBalance.TextChanged += (s, e) => Recalculate();

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
                entryGrid.Columns["Reason"].FillWeight = 82;
                entryGrid.Columns["Reason"].MinimumWidth = 260;
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
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeight = 38;
            grid.RowTemplate.Height = 32;
            grid.GridColor = Color.FromArgb(226, 232, 240);
            grid.ColumnHeadersDefaultCellStyle.BackColor = navy;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            grid.DefaultCellStyle.ForeColor = textMain;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = textMain;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
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
            DateTime closingDate = DateTime.Today;
            txtClosingDate.Text = closingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (!ValidateInput())
                return;

            if (!LoadAutoAmounts())
                return;

            decimal openingBalance = ReadAmount(txtOpeningBalance);
            decimal cashSales = ReadAmount(txtCashSales);
            decimal cashIn = GetEntryTotal(cashInEntries);
            decimal cashOut = GetEntryTotal(cashOutEntries);
            decimal ownerCash = ReadAmount(txtOwnerCash);
            decimal closingBalance = ReadAmount(txtClosingBalance);
            decimal counterCash = ReadAmount(txtCounterCash);
            decimal expectedOwnerCash = ReadAmount(txtExpectedOwnerCash);
            decimal difference = ReadAmount(txtDifference);
            string user = string.IsNullOrWhiteSpace(LoginForm.LoggedInUser) ? "Unknown" : LoginForm.LoggedInUser;
            string role = string.IsNullOrWhiteSpace(MainForm.CurrentRole) ? "Unknown" : MainForm.CurrentRole;

            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureClosingBalanceSchema(conn);

                using MySqlTransaction tx = conn.BeginTransaction();

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
                    role);

                ReplaceMovementEntries(conn, tx, closingId, closingDate, user, cashInEntries, "IN");
                ReplaceMovementEntries(conn, tx, closingId, closingDate, user, cashOutEntries, "OUT");

                tx.Commit();

                MessageBox.Show("Closing balance saved/updated.");
                ClearFields();
                LoadRecentClosings();
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
            string role)
        {
            string query = @"
INSERT INTO daily_cash_closing
(
    closing_date, opening_balance, cash_sales, other_cash_in, cash_in_reason,
    other_cash_out, cash_out_reason, counter_left_for_tomorrow, cash_given_to_owner,
    total_cash_in_hand, total_cash_out, available_before_closing, expected_owner_cash, difference_amount,
    note, created_by_user, created_by_role
)
VALUES
(
    @closing_date, @opening_balance, @cash_sales, @cash_in, @cash_in_reason,
    @cash_out, @cash_out_reason, @closing_balance, @owner_cash,
    @total_cash_in_hand, @total_cash_out, @available_before_closing, @expected_owner_cash, @difference_amount,
    @note, @created_by_user, @created_by_role
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
    created_by_user = VALUES(created_by_user),
    created_by_role = VALUES(created_by_role),
    created_at = CURRENT_TIMESTAMP;";

            using MySqlCommand cmd = new MySqlCommand(query, conn, tx);
            cmd.Parameters.AddWithValue("@closing_date", closingDate.Date);
            cmd.Parameters.AddWithValue("@opening_balance", openingBalance);
            cmd.Parameters.AddWithValue("@cash_sales", cashSales);
            cmd.Parameters.AddWithValue("@cash_in", cashIn);
            cmd.Parameters.AddWithValue("@cash_in_reason", BuildReasonSummary(cashInEntries));
            cmd.Parameters.AddWithValue("@cash_out", cashOut);
            cmd.Parameters.AddWithValue("@cash_out_reason", BuildReasonSummary(cashOutEntries));
            cmd.Parameters.AddWithValue("@closing_balance", closingBalance);
            cmd.Parameters.AddWithValue("@owner_cash", ownerCash);
            cmd.Parameters.AddWithValue("@total_cash_in_hand", openingBalance + cashSales + cashIn);
            cmd.Parameters.AddWithValue("@total_cash_out", cashOut);
            cmd.Parameters.AddWithValue("@available_before_closing", counterCash);
            cmd.Parameters.AddWithValue("@expected_owner_cash", expectedOwnerCash);
            cmd.Parameters.AddWithValue("@difference_amount", difference);
            cmd.Parameters.AddWithValue("@note", "");
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

            if (!ValidateNonNegative(txtClosingBalance, "Closing balance"))
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

        private void Recalculate()
        {
            decimal openingBalance = ReadAmount(txtOpeningBalance);
            decimal cashSales = ReadAmount(txtCashSales);
            decimal cashIn = GetEntryTotal(cashInEntries);
            decimal cashOut = GetEntryTotal(cashOutEntries);
            decimal ownerCash = ReadAmount(txtOwnerCash);
            decimal closingBalance = ReadAmount(txtClosingBalance);

            decimal counterCash = openingBalance + cashSales + cashIn - cashOut;
            decimal expectedOwnerCash = counterCash - closingBalance;
            decimal difference = expectedOwnerCash - ownerCash;

            SetAmount(txtCashInTotal, cashIn);
            SetAmount(txtCashOutTotal, cashOut);
            SetAmount(txtCounterCash, counterCash);
            SetAmount(txtExpectedOwnerCash, expectedOwnerCash);
            SetAmount(txtDifference, difference);

            txtDifference.ForeColor = difference == 0 ? green : red;
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
    opening_balance AS Opening,
    cash_sales AS CashSale,
    other_cash_in AS CashInTotal,
    other_cash_out AS CashOutTotal,
    counter_left_for_tomorrow AS ClosingBalance,
    cash_given_to_owner AS OwnerCash,
    difference_amount AS Difference,
    created_by_user AS EnteredBy,
    DATE_FORMAT(created_at, '%d-%m-%Y %h:%i %p') AS EnteredAt
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
                decimal cashInTotal = Convert.ToDecimal(reader["other_cash_in"]);
                decimal cashOutTotal = Convert.ToDecimal(reader["other_cash_out"]);
                string cashInReason = reader["cash_in_reason"]?.ToString() ?? "";
                string cashOutReason = reader["cash_out_reason"]?.ToString() ?? "";

                txtClosingBalance.Text = Convert.ToDecimal(reader["counter_left_for_tomorrow"]).ToString("0.00", CultureInfo.InvariantCulture);
                txtOwnerCash.Text = Convert.ToDecimal(reader["cash_given_to_owner"]).ToString("0.00", CultureInfo.InvariantCulture);
                reader.Close();

                int inRows = LoadMovementEntries(conn, closingId, "IN", cashInEntries);
                int outRows = LoadMovementEntries(conn, closingId, "OUT", cashOutEntries);

                if (inRows == 0 && cashInTotal > 0)
                    cashInEntries.Rows.Add(cashInTotal, cashInReason);

                if (outRows == 0 && cashOutTotal > 0)
                    cashOutEntries.Rows.Add(cashOutTotal, cashOutReason);

                lblStatus.Text = "Today's saved closing loaded for update.";
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

        private bool LoadAutoAmounts()
        {
            try
            {
                DateTime today = DateTime.Today;
                txtOpeningBalance.Text = GetPreviousClosingBalance(today).ToString("0.00", CultureInfo.InvariantCulture);
                txtCashSales.Text = GetCashSalesFromDb(today).ToString("0.00", CultureInfo.InvariantCulture);
                lblStatus.Text = "Auto amounts loaded for " + today.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load auto amounts: " + ex.Message);
                lblStatus.Text = "Unable to load auto amounts: " + ex.Message;
                return false;
            }
        }

        private decimal GetPreviousClosingBalance(DateTime date)
        {
            using MySqlConnection conn = DB.GetConnection();
            conn.Open();
            DB.EnsureClosingBalanceSchema(conn);

            string query = @"
SELECT counter_left_for_tomorrow
FROM daily_cash_closing
WHERE closing_date < @closing_date
ORDER BY closing_date DESC, id DESC
LIMIT 1;";

            using MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@closing_date", date.Date);
            object result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToDecimal(result, CultureInfo.InvariantCulture);
        }

        private decimal GetCashSalesFromDb(DateTime date)
        {
            using MySqlConnection conn = DB.GetConnection();
            conn.Open();

            DB.EnsureColumnExists(conn, "inv_orders", "payment_method", "VARCHAR(40) NOT NULL DEFAULT 'Cash'");

            string query = @"
SELECT IFNULL(SUM(grand_total), 0)
FROM inv_orders
WHERE DATE(date_added) = @sale_date
  AND LOWER(TRIM(IFNULL(payment_method, 'Cash'))) = 'cash';";

            using MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@sale_date", date.Date);
            object result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToDecimal(result, CultureInfo.InvariantCulture);
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
