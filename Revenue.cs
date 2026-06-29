using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Revenue : UserControl
    {
        private readonly Panel topPanel = new Panel();
        private readonly FlowLayoutPanel summaryPanel = new FlowLayoutPanel();
        private readonly Panel chartPanel = new Panel();
        private readonly DataGridView grid = new DataGridView();
        private readonly ComboBox cmbType = new ComboBox();
        private readonly DateTimePicker dtpDate = new DateTimePicker();
        private readonly DateTimePicker dtpFrom = new DateTimePicker();
        private readonly DateTimePicker dtpTo = new DateTimePicker();
        private readonly Label statusLabel = new Label();
        private readonly Label graphTitle = new Label();
        private readonly Label tableTitle = new Label();

        private DataTable graphData = new DataTable();

        private readonly Color pageBack = Color.FromArgb(245, 247, 251);
        private readonly Color navy = Color.FromArgb(21, 32, 55);
        private readonly Color textMain = Color.FromArgb(28, 37, 65);
        private readonly Color textMuted = Color.FromArgb(104, 116, 140);
        private readonly Color salesBlue = Color.FromArgb(37, 99, 235);
        private readonly Color profitGreen = Color.FromArgb(16, 185, 129);
        private readonly Color costAmber = Color.FromArgb(245, 158, 11);

        public Revenue()
        {
            InitUI();
            cmbType.SelectedIndex = 3;
            LoadData();
        }

        private void InitUI()
        {
            Dock = DockStyle.Fill;
            BackColor = pageBack;
            Padding = new Padding(18);

            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 78;
            topPanel.BackColor = pageBack;
            topPanel.Padding = new Padding(0, 0, 0, 14);

            Label title = new Label
            {
                Text = "Revenue Dashboard",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = textMain,
                Location = new Point(0, 4)
            };

            Label subtitle = new Label
            {
                Text = "Sales, cost price and profit view",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = textMuted,
                Location = new Point(3, 44)
            };

            cmbType.Items.AddRange(new string[] { "Date Wise", "Date Range", "Today", "This Month", "This Year", "Till Date", "Category Wise" });
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbType.Location = new Point(Math.Max(0, Width - 190), 16);
            cmbType.Width = 168;
            cmbType.Font = new Font("Segoe UI", 10);
            cmbType.SelectedIndexChanged += (s, e) =>
            {
                dtpDate.Visible = cmbType.Text == "Date Wise";
                dtpFrom.Visible = cmbType.Text == "Date Range";
                dtpTo.Visible = cmbType.Text == "Date Range";
                PositionFilters();
                LoadData();
            };

            dtpDate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dtpDate.Format = DateTimePickerFormat.Short;
            dtpDate.Width = 126;
            dtpDate.Font = new Font("Segoe UI", 10);
            dtpDate.Visible = false;
            dtpDate.ValueChanged += (s, e) =>
            {
                if (cmbType.Text == "Date Wise")
                    LoadData();
            };

            dtpFrom.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dtpFrom.Format = DateTimePickerFormat.Custom;
            dtpFrom.CustomFormat = "'From' dd-MM-yy";
            dtpFrom.Width = 128;
            dtpFrom.Font = new Font("Segoe UI", 10);
            dtpFrom.Value = DateTime.Today.AddDays(-7);
            dtpFrom.Visible = false;
            dtpFrom.ValueChanged += (s, e) =>
            {
                if (cmbType.Text == "Date Range")
                    LoadData();
            };

            dtpTo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dtpTo.Format = DateTimePickerFormat.Custom;
            dtpTo.CustomFormat = "'To' dd-MM-yy";
            dtpTo.Width = 116;
            dtpTo.Font = new Font("Segoe UI", 10);
            dtpTo.Value = DateTime.Today;
            dtpTo.Visible = false;
            dtpTo.ValueChanged += (s, e) =>
            {
                if (cmbType.Text == "Date Range")
                    LoadData();
            };

            topPanel.Resize += (s, e) =>
            {
                PositionFilters();
            };

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(subtitle);
            topPanel.Controls.Add(dtpTo);
            topPanel.Controls.Add(dtpFrom);
            topPanel.Controls.Add(dtpDate);
            topPanel.Controls.Add(cmbType);

            summaryPanel.Dock = DockStyle.Top;
            summaryPanel.Height = 168;
            summaryPanel.BackColor = pageBack;
            summaryPanel.WrapContents = false;
            summaryPanel.AutoScroll = true;
            summaryPanel.Padding = new Padding(0, 2, 0, 14);

            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = pageBack
            };

            RoundedPanel tablePanel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Radius = 8,
                Padding = new Padding(16)
            };

            RoundedPanel graphWrapper = new RoundedPanel
            {
                Dock = DockStyle.Bottom,
                Height = 285,
                BackColor = Color.White,
                Radius = 8,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 0, 14)
            };

            graphTitle.Text = "Sales vs Profit";
            graphTitle.Dock = DockStyle.Top;
            graphTitle.Height = 28;
            graphTitle.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold);
            graphTitle.ForeColor = textMain;

            tableTitle.Text = "Period Breakdown";
            tableTitle.Dock = DockStyle.Top;
            tableTitle.Height = 32;
            tableTitle.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold);
            tableTitle.ForeColor = textMain;

            chartPanel.Dock = DockStyle.Fill;
            chartPanel.BackColor = Color.White;
            chartPanel.Paint += GraphPanel_Paint;
            graphWrapper.Controls.Add(chartPanel);
            graphWrapper.Controls.Add(graphTitle);

            statusLabel.Dock = DockStyle.Bottom;
            statusLabel.Height = 24;
            statusLabel.ForeColor = textMuted;
            statusLabel.Font = new Font("Segoe UI", 8.5f);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            ConfigureGrid();
            tablePanel.Controls.Add(grid);
            tablePanel.Controls.Add(statusLabel);
            tablePanel.Controls.Add(tableTitle);

            content.Controls.Add(tablePanel);
            content.Controls.Add(graphWrapper);

            Controls.Add(content);
            Controls.Add(summaryPanel);
            Controls.Add(topPanel);
        }

        private void PositionFilters()
        {
            int right = topPanel.Width;
            int y = 18;

            if (dtpDate.Visible)
            {
                dtpDate.Location = new Point(Math.Max(0, right - dtpDate.Width), y);
                right = dtpDate.Left - 10;
            }

            if (dtpTo.Visible)
            {
                dtpTo.Location = new Point(Math.Max(0, right - dtpTo.Width), y);
                right = dtpTo.Left - 8;
            }

            if (dtpFrom.Visible)
            {
                dtpFrom.Location = new Point(Math.Max(0, right - dtpFrom.Width), y);
                right = dtpFrom.Left - 10;
            }

            cmbType.Location = new Point(Math.Max(0, right - cmbType.Width), y);
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
            grid.RowTemplate.Height = 34;
            grid.GridColor = Color.FromArgb(226, 232, 240);

            grid.ColumnHeadersDefaultCellStyle.BackColor = navy;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            grid.DefaultCellStyle.ForeColor = textMain;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = textMain;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection con = DB.GetConnection())
                {
                    con.Open();
                    LoadSummary(con);
                    LoadGraphGrid(con);
                }
            }
            catch (Exception ex)
            {
                summaryPanel.Controls.Clear();
                graphData = new DataTable();
                grid.DataSource = null;
                statusLabel.Text = "Unable to load revenue data: " + ex.Message;
                chartPanel.Invalidate();
            }
        }

        private void LoadSummary(MySqlConnection con)
        {
            summaryPanel.Controls.Clear();

            AddSummaryCard("Today", GetMetrics(con, "DATE(o.date_added) = CURDATE()"), salesBlue);
            AddSummaryCard("Last 7 Days", GetMetrics(con, "DATE(o.date_added) >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)"), Color.FromArgb(99, 102, 241));
            AddSummaryCard("This Month", GetMetrics(con, "YEAR(o.date_added)=YEAR(CURDATE()) AND MONTH(o.date_added)=MONTH(CURDATE())"), costAmber);
            AddSummaryCard("This Year", GetMetrics(con, "YEAR(o.date_added)=YEAR(CURDATE())"), profitGreen);
            AddSummaryCard("All Time", GetMetrics(con, "1=1"), Color.FromArgb(71, 85, 105));
        }

        private void AddSummaryCard(string title, MetricSnapshot metric, Color accent)
        {
            summaryPanel.Controls.Add(CreateCard(title, metric, accent));
        }

        private void LoadGraphGrid(MySqlConnection con)
        {
            tableTitle.Text = cmbType.Text == "Category Wise" ? "Category Breakdown" : "Period Breakdown";
            graphTitle.Text = cmbType.Text == "Category Wise" ? "Category Sales vs Profit" : "Sales vs Profit";

            string query = BuildPeriodQuery();
            using (MySqlDataAdapter da = new MySqlDataAdapter(query, con))
            {
                graphData = new DataTable();
                da.Fill(graphData);
            }

            grid.DataSource = graphData;
            FormatGridColumns();

            decimal sales = SumColumn("Sales");
            decimal cost = SumColumn("Cost");
            decimal profit = SumColumn("Profit");
            decimal profitPercent = sales == 0 ? 0 : (profit / sales) * 100m;

            statusLabel.Text = $"Selected view: {GetSelectedViewText()}   |   Sales: Rs. {sales:N2}   Cost: Rs. {cost:N2}   Profit: Rs. {profit:N2}   Profit %: {profitPercent:N1}%";
            chartPanel.Refresh();
        }

        private string BuildPeriodQuery()
        {
            string periodSql;
            string sortSql;
            string condition;
            string groupSql;
            string orderSql;

            if (cmbType.Text == "Category Wise")
            {
                periodSql = "IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other')";
                sortSql = "IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other')";
                condition = "1=1";
                groupSql = "IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other')";
                orderSql = "Profit DESC";
            }
            else if (cmbType.Text == "Date Wise")
            {
                string selectedDate = dtpDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                periodSql = "DATE_FORMAT(o.date_added, '%h %p')";
                sortSql = "HOUR(o.date_added)";
                condition = $"DATE(o.date_added) = '{selectedDate}'";
                groupSql = "HOUR(o.date_added), DATE_FORMAT(o.date_added, '%h %p')";
                orderSql = "sort_key ASC";
            }
            else if (cmbType.Text == "Date Range")
            {
                DateTime from = dtpFrom.Value.Date;
                DateTime to = dtpTo.Value.Date;
                if (from > to)
                {
                    DateTime temp = from;
                    from = to;
                    to = temp;
                }

                string fromSql = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string toSql = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                periodSql = "DATE_FORMAT(o.date_added, '%d %b')";
                sortSql = "DATE(o.date_added)";
                condition = $"DATE(o.date_added) BETWEEN '{fromSql}' AND '{toSql}'";
                groupSql = "DATE(o.date_added), DATE_FORMAT(o.date_added, '%d %b')";
                orderSql = "sort_key ASC";
            }
            else if (cmbType.Text == "Today")
            {
                periodSql = "DATE_FORMAT(o.date_added, '%h %p')";
                sortSql = "HOUR(o.date_added)";
                condition = "DATE(o.date_added) = CURDATE()";
                groupSql = "HOUR(o.date_added), DATE_FORMAT(o.date_added, '%h %p')";
                orderSql = "sort_key ASC";
            }
            else if (cmbType.Text == "This Year")
            {
                periodSql = "DATE_FORMAT(o.date_added, '%b %Y')";
                sortSql = "DATE_FORMAT(o.date_added, '%Y-%m')";
                condition = "YEAR(o.date_added)=YEAR(CURDATE())";
                groupSql = "DATE_FORMAT(o.date_added, '%Y-%m'), DATE_FORMAT(o.date_added, '%b %Y')";
                orderSql = "sort_key ASC";
            }
            else if (cmbType.Text == "Till Date")
            {
                periodSql = "DATE_FORMAT(o.date_added, '%b %Y')";
                sortSql = "DATE_FORMAT(o.date_added, '%Y-%m')";
                condition = "1=1";
                groupSql = "DATE_FORMAT(o.date_added, '%Y-%m'), DATE_FORMAT(o.date_added, '%b %Y')";
                orderSql = "sort_key DESC";
            }
            else
            {
                periodSql = "DATE_FORMAT(o.date_added, '%d %b')";
                sortSql = "DATE(o.date_added)";
                condition = "YEAR(o.date_added)=YEAR(CURDATE()) AND MONTH(o.date_added)=MONTH(CURDATE())";
                groupSql = "DATE(o.date_added), DATE_FORMAT(o.date_added, '%d %b')";
                orderSql = "sort_key DESC";
            }

            return $@"
SELECT
    {periodSql} AS Period,
    COUNT(DISTINCT o.id) AS Orders,
    SUM(GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)) AS Qty,
    ROUND(IFNULL(SUM(IFNULL(d.net_amount,0)),0), 2) AS Sales,
    ROUND(IFNULL(SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)),0), 2) AS Cost,
    ROUND(
        IFNULL(SUM(IFNULL(d.net_amount,0)),0) -
        IFNULL(SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)),0),
        2
    ) AS Profit,
    ROUND(
        CASE
            WHEN IFNULL(SUM(IFNULL(d.net_amount,0)),0) = 0 THEN 0
            ELSE (
                (
                    IFNULL(SUM(IFNULL(d.net_amount,0)),0) -
                    IFNULL(SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)),0)
                ) / IFNULL(SUM(IFNULL(d.net_amount,0)),0)
            ) * 100
        END,
        2
    ) AS ProfitPercent,
    {sortSql} AS sort_key
FROM inv_orders o
INNER JOIN inv_order_details d ON d.order_id = o.id
LEFT JOIN inv_items_master i ON i.id = d.item_id
WHERE {condition}
GROUP BY {groupSql}
HAVING Sales > 0 OR Cost > 0
ORDER BY {orderSql};";
        }

        private MetricSnapshot GetMetrics(MySqlConnection con, string condition)
        {
            string query = $@"
SELECT
    IFNULL(SUM(IFNULL(d.net_amount,0)),0) AS Sales,
    IFNULL(SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)),0) AS Cost,
    COUNT(DISTINCT o.id) AS Orders,
    IFNULL(SUM(GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)),0) AS Qty
FROM inv_orders o
INNER JOIN inv_order_details d ON d.order_id = o.id
LEFT JOIN inv_items_master i ON i.id = d.item_id
WHERE {condition};";

            using (MySqlCommand cmd = new MySqlCommand(query, con))
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                    return new MetricSnapshot();

                decimal sales = Convert.ToDecimal(reader["Sales"]);
                decimal cost = Convert.ToDecimal(reader["Cost"]);

                return new MetricSnapshot
                {
                    Sales = sales,
                    Cost = cost,
                    Profit = sales - cost,
                    Orders = Convert.ToInt32(reader["Orders"]),
                    Qty = Convert.ToInt32(reader["Qty"])
                };
            }
        }

        private decimal SumColumn(string columnName)
        {
            if (graphData == null || !graphData.Columns.Contains(columnName))
                return 0;

            decimal total = 0;
            foreach (DataRow row in graphData.Rows)
            {
                if (row[columnName] != DBNull.Value)
                    total += Convert.ToDecimal(row[columnName]);
            }

            return total;
        }

        private void FormatGridColumns()
        {
            if (grid.Columns.Count == 0)
                return;

            if (grid.Columns.Contains("sort_key"))
                grid.Columns["sort_key"].Visible = false;

            SetHeader("Period", cmbType.Text == "Category Wise" ? "Category" : "Period");
            SetHeader("Orders", "Bills");
            SetHeader("Qty", "Qty Sold");
            SetMoneyColumn("Sales", "Sell Amount");
            SetMoneyColumn("Cost", "Cost Amount");
            SetMoneyColumn("Profit", "Profit");

            if (grid.Columns.Contains("ProfitPercent"))
            {
                grid.Columns["ProfitPercent"].HeaderText = "Profit %";
                grid.Columns["ProfitPercent"].DefaultCellStyle.Format = "N2";
                grid.Columns["ProfitPercent"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Name != "Period")
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void SetHeader(string columnName, string headerText)
        {
            if (grid.Columns.Contains(columnName))
                grid.Columns[columnName].HeaderText = headerText;
        }

        private void SetMoneyColumn(string columnName, string headerText)
        {
            if (!grid.Columns.Contains(columnName))
                return;

            grid.Columns[columnName].HeaderText = headerText;
            grid.Columns[columnName].DefaultCellStyle.Format = "N2";
            grid.Columns[columnName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            if (columnName == "Profit")
                grid.Columns[columnName].DefaultCellStyle.ForeColor = profitGreen;
        }

        private Panel CreateCard(string title, MetricSnapshot metric, Color accent)
        {
            RoundedPanel p = new RoundedPanel
            {
                Width = 224,
                Height = 138,
                BackColor = Color.White,
                Radius = 8,
                Margin = new Padding(0, 0, 14, 0),
                Padding = new Padding(14)
            };

            Panel accentLine = new Panel
            {
                BackColor = accent,
                Dock = DockStyle.Left,
                Width = 4
            };

            Label titleLabel = new Label
            {
                Text = title,
                AutoSize = false,
                Width = 180,
                Height = 22,
                Location = new Point(18, 12),
                Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold),
                ForeColor = textMuted
            };

            Label salesLabel = new Label
            {
                Text = "Rs. " + FormatAmount(metric.Sales),
                AutoSize = false,
                Width = 188,
                Height = 34,
                Location = new Point(18, 34),
                Font = new Font("Segoe UI Semibold", 18, FontStyle.Bold),
                ForeColor = textMain
            };

            Label costLabel = new Label
            {
                Text = "Cost: Rs. " + FormatAmount(metric.Cost),
                AutoSize = false,
                Width = 188,
                Height = 18,
                Location = new Point(18, 72),
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = textMuted
            };

            Label profitLabel = new Label
            {
                Text = "Profit: Rs. " + FormatAmount(metric.Profit),
                AutoSize = false,
                Width = 188,
                Height = 18,
                Location = new Point(18, 92),
                Font = new Font("Segoe UI Semibold", 8.6f, FontStyle.Bold),
                ForeColor = metric.Profit >= 0 ? profitGreen : Color.FromArgb(220, 38, 38)
            };

            Label qtyLabel = new Label
            {
                Text = $"{metric.Orders} bills  |  {metric.Qty} pcs",
                AutoSize = false,
                Width = 188,
                Height = 18,
                Location = new Point(18, 114),
                Font = new Font("Segoe UI", 8),
                ForeColor = textMuted
            };

            p.Controls.Add(qtyLabel);
            p.Controls.Add(profitLabel);
            p.Controls.Add(costLabel);
            p.Controls.Add(salesLabel);
            p.Controls.Add(titleLabel);
            p.Controls.Add(accentLine);
            return p;
        }

        private string FormatAmount(decimal amount)
        {
            decimal abs = Math.Abs(amount);
            string sign = amount < 0 ? "-" : "";

            if (abs >= 10000000)
                return sign + (abs / 10000000).ToString("0.##") + "Cr";

            if (abs >= 100000)
                return sign + (abs / 100000).ToString("0.##") + "L";

            if (abs >= 1000)
                return sign + (abs / 1000).ToString("0.#") + "K";

            return sign + abs.ToString("0");
        }

        private string GetSelectedViewText()
        {
            if (cmbType.Text == "Date Wise")
                return "Date Wise - " + dtpDate.Value.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);

            if (cmbType.Text == "Date Range")
            {
                DateTime from = dtpFrom.Value.Date;
                DateTime to = dtpTo.Value.Date;
                if (from > to)
                {
                    DateTime temp = from;
                    from = to;
                    to = temp;
                }

                return "Date Range - " +
                    from.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) +
                    " to " +
                    to.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
            }

            return cmbType.Text;
        }

        private void GraphPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            Rectangle area = chartPanel.ClientRectangle;
            if (area.Width < 120 || area.Height < 120)
                return;

            if (graphData == null || graphData.Rows.Count == 0)
            {
                DrawCenteredText(g, "No revenue data for this view", area, textMuted);
                return;
            }

            int left = 58;
            int right = 24;
            int top = 28;
            int bottom = 54;
            Rectangle plot = new Rectangle(left, top, area.Width - left - right, area.Height - top - bottom);

            decimal max = 0;
            foreach (DataRow r in graphData.Rows)
            {
                max = Math.Max(max, Math.Abs(ReadDecimal(r, "Sales")));
                max = Math.Max(max, Math.Abs(ReadDecimal(r, "Profit")));
            }

            if (max <= 0)
            {
                DrawCenteredText(g, "No positive sales to chart", area, textMuted);
                return;
            }

            using (Pen gridPen = new Pen(Color.FromArgb(226, 232, 240)))
            using (Pen axisPen = new Pen(Color.FromArgb(148, 163, 184)))
            using (Brush labelBrush = new SolidBrush(textMuted))
            using (Font small = new Font("Segoe UI", 8))
            using (Brush salesBrush = new SolidBrush(salesBlue))
            using (Brush profitBrush = new SolidBrush(profitGreen))
            using (Brush lossBrush = new SolidBrush(Color.FromArgb(220, 38, 38)))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = plot.Bottom - (plot.Height * i / 4);
                    g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    decimal tick = max * i / 4;
                    g.DrawString(FormatAmount(tick), small, labelBrush, 4, y - 8);
                }

                g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

                int count = graphData.Rows.Count;
                int slot = Math.Max(44, plot.Width / Math.Max(count, 1));
                int barWidth = Math.Min(24, Math.Max(8, slot / 5));
                int x = plot.Left + Math.Max(4, (slot - (barWidth * 2 + 5)) / 2);

                foreach (DataRow r in graphData.Rows)
                {
                    decimal sales = ReadDecimal(r, "Sales");
                    decimal profit = ReadDecimal(r, "Profit");
                    int salesHeight = (int)(plot.Height * (double)(sales / max));
                    int profitHeight = (int)(plot.Height * (double)(Math.Abs(profit) / max));

                    Rectangle salesRect = new Rectangle(x, plot.Bottom - Math.Max(2, salesHeight), barWidth, Math.Max(2, salesHeight));
                    Rectangle profitRect = new Rectangle(x + barWidth + 5, plot.Bottom - Math.Max(2, profitHeight), barWidth, Math.Max(2, profitHeight));

                    g.FillRectangle(salesBrush, salesRect);
                    g.FillRectangle(profit >= 0 ? profitBrush : lossBrush, profitRect);

                    string label = r["Period"].ToString() ?? "";
                    if (label.Length > 8)
                        label = label.Substring(0, 8);

                    g.DrawString(label, small, labelBrush, x - 4, plot.Bottom + 10);
                    x += slot;
                }

                DrawLegend(g, area.Right - 190, 4, salesBrush, "Sell", small);
                DrawLegend(g, area.Right - 105, 4, profitBrush, "Profit", small);
            }
        }

        private decimal ReadDecimal(DataRow row, string column)
        {
            if (!graphData.Columns.Contains(column) || row[column] == DBNull.Value)
                return 0;

            return Convert.ToDecimal(row[column]);
        }

        private void DrawLegend(Graphics g, int x, int y, Brush brush, string label, Font font)
        {
            g.FillRectangle(brush, x, y + 4, 12, 8);
            using (Brush labelBrush = new SolidBrush(textMuted))
            {
                g.DrawString(label, font, labelBrush, x + 18, y);
            }
        }

        private void DrawCenteredText(Graphics g, string text, Rectangle area, Color color)
        {
            using (Brush brush = new SolidBrush(color))
            using (Font font = new Font("Segoe UI", 10))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(text, font, brush, area, format);
            }
        }

        private class MetricSnapshot
        {
            public decimal Sales { get; set; }
            public decimal Cost { get; set; }
            public decimal Profit { get; set; }
            public int Orders { get; set; }
            public int Qty { get; set; }
        }

        private class RoundedPanel : Panel
        {
            public int Radius { get; set; } = 8;

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath path = CreatePath(ClientRectangle, Radius))
                using (Pen pen = new Pen(Color.FromArgb(226, 232, 240)))
                {
                    Region = new Region(path);
                    Rectangle borderRect = ClientRectangle;
                    borderRect.Width -= 1;
                    borderRect.Height -= 1;
                    using (GraphicsPath borderPath = CreatePath(borderRect, Radius))
                    {
                        e.Graphics.DrawPath(pen, borderPath);
                    }
                }
            }

            private static GraphicsPath CreatePath(Rectangle rect, int radius)
            {
                int diameter = Math.Max(1, radius * 2);
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}
