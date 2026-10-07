using System;
using System.Collections.Generic;
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
        private readonly Panel pageScroll = new Panel();
        private readonly Panel pageInner = new Panel();
        private readonly Panel topPanel = new Panel();
        private readonly Panel titleBar = new Panel();
        private readonly FlowLayoutPanel summaryPanel = new FlowLayoutPanel();
        private readonly Panel chartPanel = new Panel();
        private readonly Panel mixChartPanel = new Panel();
        private readonly Panel categoryChartPanel = new Panel();
        private readonly DataGridView grid = new DataGridView();
        private readonly ComboBox cmbType = new ComboBox();
        private readonly DateTimePicker dtpDate = new DateTimePicker();
        private readonly DateTimePicker dtpFrom = new DateTimePicker();
        private readonly DateTimePicker dtpTo = new DateTimePicker();
        private readonly Label statusLabel = new Label();
        private readonly Label graphTitle = new Label();
        private readonly Label mixTitle = new Label();
        private readonly Label categoryTitle = new Label();
        private readonly Label tableTitle = new Label();

        private DataTable graphData = new DataTable();
        private DataTable categoryData = new DataTable();
        private readonly Color[] sliceColors =
        {
            Color.FromArgb(37, 99, 235),
            Color.FromArgb(16, 185, 129),
            Color.FromArgb(245, 158, 11),
            Color.FromArgb(139, 92, 246),
            Color.FromArgb(236, 72, 153),
            Color.FromArgb(6, 182, 212),
            Color.FromArgb(249, 115, 22),
            Color.FromArgb(100, 116, 139)
        };

        private readonly Color pageBack = Color.FromArgb(245, 247, 251);
        private readonly Color navy = Color.FromArgb(21, 32, 55);
        private readonly Color textMain = Color.FromArgb(28, 37, 65);
        private readonly Color textMuted = Color.FromArgb(104, 116, 140);
        private readonly Color salesBlue = Color.FromArgb(37, 99, 235);
        private readonly Color profitGreen = Color.FromArgb(16, 185, 129);
        private readonly Color costAmber = Color.FromArgb(245, 158, 11);

        private bool _filtersReady;

        public Revenue()
        {
            InitUI();
            _filtersReady = true;
            int monthIndex = cmbType.Items.IndexOf("This Month");
            if (monthIndex >= 0)
                cmbType.SelectedIndex = monthIndex;
            else
                LoadData();
        }

        private void InitUI()
        {
            Dock = DockStyle.Fill;
            BackColor = pageBack;
            Padding = new Padding(0);

            pageScroll.Dock = DockStyle.Fill;
            pageScroll.BackColor = pageBack;
            pageScroll.AutoScroll = true;
            pageScroll.AutoScrollMargin = new Size(8, 8);
            pageScroll.Resize += (s, e) => FitScrollContent();

            pageInner.BackColor = pageBack;
            pageInner.Location = Point.Empty;
            pageInner.Padding = new Padding(18);

            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 86;
            topPanel.BackColor = pageBack;
            topPanel.Padding = new Padding(0, 0, 0, 12);

            titleBar.Dock = DockStyle.Left;
            titleBar.Width = 420;
            titleBar.BackColor = pageBack;

            Label title = new Label
            {
                Text = "Revenue Dashboard",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = textMain,
                Location = new Point(0, 2)
            };

            Label subtitle = new Label
            {
                Text = "Sales · cost · profit  ·  pie charts follow the selected filter",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = textMuted,
                Location = new Point(2, 42)
            };

            titleBar.Controls.Add(title);
            titleBar.Controls.Add(subtitle);

            cmbType.Items.AddRange(new string[] { "Date Wise", "Date Range", "Today", "Last 7 Days", "This Month", "This Year", "Till Date", "Category Wise" });
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbType.Location = new Point(Math.Max(0, Width - 190), 18);
            cmbType.Width = 176;
            cmbType.Font = new Font("Segoe UI", 10);
            cmbType.SelectedIndexChanged += (s, e) =>
            {
                if (!_filtersReady)
                    return;

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
            dtpFrom.Width = 156;
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
            dtpTo.Width = 138;
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

            topPanel.Controls.Add(titleBar);
            topPanel.Controls.Add(dtpTo);
            topPanel.Controls.Add(dtpFrom);
            topPanel.Controls.Add(dtpDate);
            topPanel.Controls.Add(cmbType);

            summaryPanel.Dock = DockStyle.Top;
            summaryPanel.Height = 168;
            summaryPanel.BackColor = pageBack;
            summaryPanel.WrapContents = true;
            summaryPanel.AutoScroll = false;
            summaryPanel.Padding = new Padding(0, 2, 0, 12);
            summaryPanel.Resize += (_, _) => FitSummaryHeight();

            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = pageBack
            };

            RoundedPanel tablePanel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Radius = 10,
                Padding = new Padding(16),
                ClipToRoundRegion = false
            };

            Panel chartsRow = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 312,
                BackColor = pageBack,
                Padding = new Padding(0, 12, 0, 0)
            };

            TableLayoutPanel chartsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = pageBack
            };
            chartsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            chartsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29f));
            chartsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29f));

            RoundedPanel barCard = CreateChartCard();
            graphTitle.Text = "Sales vs Profit";
            graphTitle.Dock = DockStyle.Top;
            graphTitle.Height = 30;
            graphTitle.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold);
            graphTitle.ForeColor = textMain;
            chartPanel.Dock = DockStyle.Fill;
            chartPanel.BackColor = Color.White;
            chartPanel.Paint += GraphPanel_Paint;
            barCard.Controls.Add(chartPanel);
            barCard.Controls.Add(graphTitle);

            RoundedPanel mixCard = CreateChartCard();
            mixTitle.Text = "Cost vs Profit";
            mixTitle.Dock = DockStyle.Top;
            mixTitle.Height = 30;
            mixTitle.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold);
            mixTitle.ForeColor = textMain;
            mixChartPanel.Dock = DockStyle.Fill;
            mixChartPanel.BackColor = Color.White;
            mixChartPanel.Paint += MixChart_Paint;
            mixCard.Controls.Add(mixChartPanel);
            mixCard.Controls.Add(mixTitle);

            RoundedPanel categoryCard = CreateChartCard();
            categoryTitle.Text = "Category share";
            categoryTitle.Dock = DockStyle.Top;
            categoryTitle.Height = 30;
            categoryTitle.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold);
            categoryTitle.ForeColor = textMain;
            categoryChartPanel.Dock = DockStyle.Fill;
            categoryChartPanel.BackColor = Color.White;
            categoryChartPanel.Paint += CategoryChart_Paint;
            categoryCard.Controls.Add(categoryChartPanel);
            categoryCard.Controls.Add(categoryTitle);
            categoryCard.Margin = new Padding(0);

            chartsLayout.Controls.Add(barCard, 0, 0);
            chartsLayout.Controls.Add(mixCard, 1, 0);
            chartsLayout.Controls.Add(categoryCard, 2, 0);
            chartsRow.Controls.Add(chartsLayout);

            tableTitle.Text = "Period Breakdown";
            tableTitle.Dock = DockStyle.Top;
            tableTitle.Height = 32;
            tableTitle.Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold);
            tableTitle.ForeColor = textMain;

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
            content.Controls.Add(chartsRow);

            pageInner.Controls.Add(content);
            pageInner.Controls.Add(summaryPanel);
            pageInner.Controls.Add(topPanel);
            pageScroll.Controls.Add(pageInner);
            Controls.Add(pageScroll);
            FitScrollContent();
        }

        private void FitScrollContent()
        {
            int viewW = pageScroll.ClientSize.Width;
            int viewH = pageScroll.ClientSize.Height;
            int width = Math.Max(viewW, 1040);
            int height = Math.Max(viewH, 920);
            if (pageInner.Width != width || pageInner.Height != height)
                pageInner.Size = new Size(width, height);
        }

        private RoundedPanel CreateChartCard()
        {
            return new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Radius = 10,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 10, 0)
            };
        }

        private void PositionFilters()
        {
            int filtersWidth = cmbType.Width + 12;
            if (dtpDate.Visible)
                filtersWidth += dtpDate.Width + 10;
            if (dtpFrom.Visible)
                filtersWidth += dtpFrom.Width + 10;
            if (dtpTo.Visible)
                filtersWidth += dtpTo.Width + 8;

            titleBar.Width = Math.Max(180, Math.Min(420, topPanel.Width - filtersWidth - 16));

            int right = topPanel.Width;
            int y = 18;

            if (dtpDate.Visible)
            {
                dtpDate.Location = new Point(Math.Max(titleBar.Right + 8, right - dtpDate.Width), y);
                right = dtpDate.Left - 10;
            }

            if (dtpTo.Visible)
            {
                dtpTo.Location = new Point(Math.Max(titleBar.Right + 8, right - dtpTo.Width), y);
                right = dtpTo.Left - 8;
            }

            if (dtpFrom.Visible)
            {
                dtpFrom.Location = new Point(Math.Max(titleBar.Right + 8, right - dtpFrom.Width), y);
                right = dtpFrom.Left - 10;
            }

            cmbType.Location = new Point(Math.Max(titleBar.Right + 8, right - cmbType.Width), y);
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
            grid.ScrollBars = ScrollBars.Both;
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
                    DB.EnsureOrderCouponColumns(con);
                    LoadSummary(con);
                    LoadCategoryShare(con);
                    LoadGraphGrid(con);
                }
            }
            catch (Exception ex)
            {
                summaryPanel.Controls.Clear();
                graphData = new DataTable();
                categoryData = new DataTable();
                grid.DataSource = null;
                statusLabel.Text = "Unable to load revenue data: " + ex.Message;
                chartPanel.Invalidate();
                mixChartPanel.Invalidate();
                categoryChartPanel.Invalidate();
            }
        }

        private void LoadSummary(MySqlConnection con)
        {
            summaryPanel.Controls.Clear();

            AddSummaryCard(
                GetFilterCardTitle(),
                GetMetrics(con, GetViewCondition()),
                navy,
                selected: true,
                subtitle: GetFilterCardSubtitle());
            AddSummaryCard("Today", GetMetrics(con, "o.date_added >= @revToday AND o.date_added < @revTodayEnd"), salesBlue);
            AddSummaryCard("Last 7 Days", GetMetrics(con, "o.date_added >= @revLast7From AND o.date_added < @revTodayEnd"), Color.FromArgb(99, 102, 241));
            AddSummaryCard("This Month", GetMetrics(con, "o.date_added >= @revMonthStart AND o.date_added < @revMonthEndExclusive"), costAmber);
            AddSummaryCard("This Year", GetMetrics(con, "o.date_added >= @revYearStart AND o.date_added < @revYearEndExclusive"), profitGreen);
            AddSummaryCard("All Time", GetMetrics(con, "1=1"), Color.FromArgb(71, 85, 105));
            FitSummaryHeight();
        }

        private void AddSummaryCard(string title, MetricSnapshot metric, Color accent, bool selected = false, string subtitle = "")
        {
            summaryPanel.Controls.Add(CreateCard(title, metric, accent, selected, subtitle));
        }

        private string GetFilterCardTitle()
        {
            string filter = cmbType.Text;
            return string.IsNullOrWhiteSpace(filter) ? "This filter" : filter;
        }

        private string GetFilterCardSubtitle()
        {
            if (cmbType.Text == "Date Wise")
                return dtpDate.Value.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);

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

                return from.ToString("dd-MMM-yy", CultureInfo.InvariantCulture) +
                    " → " +
                    to.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);
            }

            return "";
        }

        private void FitSummaryHeight()
        {
            int count = summaryPanel.Controls.Count;
            if (count == 0)
            {
                summaryPanel.Height = 168;
                return;
            }

            int cardPitch = 238;
            int avail = Math.Max(1, summaryPanel.ClientSize.Width);
            int perRow = Math.Max(1, avail / cardPitch);
            int rows = (count + perRow - 1) / perRow;
            summaryPanel.Height = 14 + rows * 148;
        }

        private void LoadGraphGrid(MySqlConnection con)
        {
            tableTitle.Text = cmbType.Text == "Category Wise"
                ? "Category Breakdown"
                : "Bills in this filter";
            graphTitle.Text = cmbType.Text == "Category Wise" ? "Category sales vs profit" : "Sales vs Profit";
            mixTitle.Text = "Cost vs Profit";
            categoryTitle.Text = cmbType.Text == "Category Wise" ? "Payment share" : "Category share";

            string query = BuildPeriodQuery();
            using (MySqlCommand cmd = new MySqlCommand(query, con))
            {
                BindRevenueDates(cmd);
                using MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                graphData = new DataTable();
                da.Fill(graphData);
            }

            if (IsBillListView() && graphData.Columns.Contains("DiscountPercent"))
            {
                foreach (DataRow row in graphData.Rows)
                    row["DiscountPercent"] = SellingCalculations.FormatDiscountPercentLabel(row["DiscountPercent"]?.ToString());
            }

            grid.DataSource = graphData;
            try
            {
                FormatGridColumns();
            }
            catch
            {
                // Column chrome must not blank the revenue page.
            }

            decimal sales = SumColumn("Sales");
            decimal cost = SumColumn("Cost");
            decimal profit = SumColumn("Profit");
            decimal profitPercent = sales == 0 ? 0 : (profit / sales) * 100m;

            statusLabel.Text = $"Selected view: {GetSelectedViewText()}   |   Sales: Rs. {sales:N2}   Cost: Rs. {cost:N2}   Profit: Rs. {profit:N2}   Profit %: {profitPercent:N1}%";
            chartPanel.Refresh();
            mixChartPanel.Refresh();
            categoryChartPanel.Refresh();
            FitScrollContent();
        }

        private void LoadCategoryShare(MySqlConnection con)
        {
            string condition = GetViewCondition();
            bool paymentInstead = cmbType.Text == "Category Wise";
            string nameExpr = paymentInstead
                ? "IFNULL(NULLIF(TRIM(o.payment_method), ''), 'Cash')"
                : "IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other')";

            string query = $@"
SELECT
    {nameExpr} AS Period,
    ROUND(IFNULL(SUM(
        {CouponCalculations.SqlLineBilledNet}
    ),0), 2) AS Sales
FROM inv_orders o
INNER JOIN inv_order_details d ON d.order_id = o.id
INNER JOIN (
    SELECT order_id, SUM(IFNULL(net_amount,0)) AS items_net
    FROM inv_order_details
    GROUP BY order_id
) ord ON ord.order_id = o.id
LEFT JOIN inv_items_master i ON i.id = d.item_id
WHERE {condition}
GROUP BY {nameExpr}
HAVING Sales > 0
ORDER BY Sales DESC;";

            using (MySqlCommand cmd = new MySqlCommand(query, con))
            {
                BindRevenueDates(cmd);
                using MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                categoryData = new DataTable();
                da.Fill(categoryData);
            }
        }

        private string GetViewCondition()
        {
            if (cmbType.Text == "Category Wise" || cmbType.Text == "Till Date")
                return "1=1";

            if (cmbType.Text == "Date Wise")
                return "o.date_added >= @revDateWise AND o.date_added < @revDateWiseEnd";

            if (cmbType.Text == "Date Range")
                return "o.date_added >= @revRangeFrom AND o.date_added < @revRangeToExclusive";

            if (cmbType.Text == "Today")
                return "o.date_added >= @revToday AND o.date_added < @revTodayEnd";

            if (cmbType.Text == "Last 7 Days")
                return "o.date_added >= @revLast7From AND o.date_added < @revTodayEnd";

            if (cmbType.Text == "This Year")
                return "o.date_added >= @revYearStart AND o.date_added < @revYearEndExclusive";

            return "o.date_added >= @revMonthStart AND o.date_added < @revMonthEndExclusive";
        }

        private string BuildPeriodQuery()
        {
            string condition = GetViewCondition();
            if (IsBillListView())
                return BuildBillListQuery(condition);

            string periodSql;
            string sortSql;
            string orderSql;

            if (cmbType.Text == "Category Wise")
            {
                periodSql = "IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other')";
                sortSql = "IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other')";
                orderSql = "Profit DESC";
            }
            else if (cmbType.Text == "Date Range")
            {
                periodSql = "DATE_FORMAT(o.date_added, '%d %b')";
                sortSql = "DATE(o.date_added)";
                orderSql = "sort_key ASC";
            }
            else if (cmbType.Text == "Last 7 Days")
            {
                periodSql = "DATE_FORMAT(o.date_added, '%d %b')";
                sortSql = "DATE(o.date_added)";
                orderSql = "sort_key DESC";
            }
            else if (cmbType.Text == "This Year")
            {
                periodSql = "DATE_FORMAT(o.date_added, '%b %Y')";
                sortSql = "DATE_FORMAT(o.date_added, '%Y-%m')";
                orderSql = "sort_key ASC";
            }
            else if (cmbType.Text == "Till Date")
            {
                periodSql = "DATE_FORMAT(o.date_added, '%b %Y')";
                sortSql = "DATE_FORMAT(o.date_added, '%Y-%m')";
                orderSql = "sort_key DESC";
            }
            else
            {
                periodSql = "DATE_FORMAT(o.date_added, '%d %b')";
                sortSql = "DATE(o.date_added)";
                orderSql = "sort_key DESC";
            }

            if (cmbType.Text == "Category Wise")
            {
                return $@"
SELECT
    IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other') AS Period,
    COUNT(DISTINCT o.id) AS Orders,
    SUM(GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)) AS Qty,
    ROUND(IFNULL(SUM(
        {CouponCalculations.SqlLineBilledNet}
    ),0), 2) AS Sales,
    ROUND(IFNULL(SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)),0), 2) AS Cost,
    ROUND(
        IFNULL(SUM(
            {CouponCalculations.SqlLineBilledNet}
        ),0) -
        IFNULL(SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)),0),
        2
    ) AS Profit,
    ROUND(
        CASE
            WHEN IFNULL(SUM(
                {CouponCalculations.SqlLineBilledNet}
            ),0) = 0 THEN 0
            ELSE (
                (
                    IFNULL(SUM(
                        {CouponCalculations.SqlLineBilledNet}
                    ),0) -
                    IFNULL(SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)),0)
                ) / IFNULL(SUM(
                    {CouponCalculations.SqlLineBilledNet}
                ),0)
            ) * 100
        END,
        2
    ) AS ProfitPercent,
    IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other') AS sort_key
FROM inv_orders o
INNER JOIN inv_order_details d ON d.order_id = o.id
INNER JOIN (
    SELECT order_id, SUM(IFNULL(net_amount,0)) AS items_net
    FROM inv_order_details
    GROUP BY order_id
) ord ON ord.order_id = o.id
LEFT JOIN inv_items_master i ON i.id = d.item_id
WHERE {condition}
GROUP BY IFNULL(NULLIF(TRIM(i.main_category), ''), 'Other')
HAVING Sales > 0 OR Cost > 0
ORDER BY Profit DESC;";
            }

            return $@"
SELECT
    Period,
    COUNT(*) AS Orders,
    SUM(Qty) AS Qty,
    ROUND(SUM(Sales), 2) AS Sales,
    ROUND(SUM(Cost), 2) AS Cost,
    ROUND(SUM(Sales) - SUM(Cost), 2) AS Profit,
    ROUND(
        CASE
            WHEN SUM(Sales) = 0 THEN 0
            ELSE ((SUM(Sales) - SUM(Cost)) / SUM(Sales)) * 100
        END,
        2
    ) AS ProfitPercent,
    sort_key
FROM (
    SELECT
        {periodSql} AS Period,
        {sortSql} AS sort_key,
        GREATEST(0, ord.items_net - {CouponCalculations.SqlCouponOutsideLines}) AS Sales,
        ord.Cost AS Cost,
        ord.Qty AS Qty
    FROM inv_orders o
    LEFT JOIN (
        SELECT
            d.order_id,
            SUM(IFNULL(d.net_amount,0)) AS items_net,
            SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)) AS Cost,
            SUM(GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)) AS Qty
        FROM inv_order_details d
        LEFT JOIN inv_items_master i ON i.id = d.item_id
        GROUP BY d.order_id
    ) ord ON ord.order_id = o.id
    WHERE {condition}
) billed
GROUP BY Period, sort_key
ORDER BY {orderSql};";
        }

        private bool IsBillListView()
        {
            return cmbType.Text != "Category Wise";
        }

        private void BindRevenueDates(MySqlCommand cmd)
        {
            DateTime today = DateTime.Today;
            DateTime rangeFrom = dtpFrom.Value.Date;
            DateTime rangeTo = dtpTo.Value.Date;
            if (rangeFrom > rangeTo)
            {
                DateTime swap = rangeFrom;
                rangeFrom = rangeTo;
                rangeTo = swap;
            }

            cmd.Parameters.AddWithValue("@revToday", today);
            cmd.Parameters.AddWithValue("@revTodayEnd", today.AddDays(1));
            cmd.Parameters.AddWithValue("@revLast7From", today.AddDays(-7));
            cmd.Parameters.AddWithValue("@revMonthStart", new DateTime(today.Year, today.Month, 1));
            cmd.Parameters.AddWithValue("@revMonthEndExclusive", new DateTime(today.Year, today.Month, 1).AddMonths(1));
            cmd.Parameters.AddWithValue("@revYearStart", new DateTime(today.Year, 1, 1));
            cmd.Parameters.AddWithValue("@revYearEndExclusive", new DateTime(today.Year + 1, 1, 1));
            cmd.Parameters.AddWithValue("@revDateWise", dtpDate.Value.Date);
            cmd.Parameters.AddWithValue("@revDateWiseEnd", dtpDate.Value.Date.AddDays(1));
            cmd.Parameters.AddWithValue("@revRangeFrom", rangeFrom);
            cmd.Parameters.AddWithValue("@revRangeToExclusive", rangeTo.AddDays(1));
        }

        private string BuildBillListQuery(string condition)
        {
            return $@"
SELECT
    CONCAT('#', o.id, '  ', DATE_FORMAT(o.date_added, '%d-%m-%Y %H:%i')) AS Period,
    IFNULL(NULLIF(TRIM(o.created_by), ''), '—') AS CreatedBy,
    ROUND(IFNULL(ord.line_discount,0) + {CouponCalculations.SqlCouponOutsideLines}, 2) AS Discount,
    IFNULL(ord.discount_pcts,'') AS DiscountPercent,
    CASE
        WHEN IFNULL(NULLIF(TRIM(o.coupon_code), ''), '') = '' THEN
            CASE WHEN IFNULL(o.coupon_discount,0) > 0 THEN CONCAT('₹', FORMAT(o.coupon_discount, 2)) ELSE '—' END
        ELSE CONCAT(o.coupon_code,
            CASE WHEN IFNULL(o.coupon_discount,0) > 0 THEN CONCAT('  ₹', FORMAT(o.coupon_discount, 2)) ELSE '' END)
    END AS Coupon,
    1 AS Orders,
    IFNULL(ord.Qty, 0) AS Qty,
    ROUND(GREATEST(0, IFNULL(ord.items_net,0) - {CouponCalculations.SqlCouponOutsideLines}), 2) AS Sales,
    ROUND(IFNULL(ord.Cost, 0), 2) AS Cost,
    ROUND(GREATEST(0, IFNULL(ord.items_net,0) - {CouponCalculations.SqlCouponOutsideLines}) - IFNULL(ord.Cost, 0), 2) AS Profit,
    ROUND(
        CASE
            WHEN GREATEST(0, IFNULL(ord.items_net,0) - {CouponCalculations.SqlCouponOutsideLines}) = 0 THEN 0
            ELSE (
                (GREATEST(0, IFNULL(ord.items_net,0) - {CouponCalculations.SqlCouponOutsideLines}) - IFNULL(ord.Cost, 0))
                / GREATEST(0, IFNULL(ord.items_net,0) - {CouponCalculations.SqlCouponOutsideLines})
            ) * 100
        END,
        2
    ) AS ProfitPercent,
    o.id AS sort_key
FROM inv_orders o
LEFT JOIN (
    SELECT
        d.order_id,
        SUM(IFNULL(d.net_amount,0)) AS items_net,
        SUM(IFNULL(d.discount_amount,0)) AS line_discount,
        GROUP_CONCAT(ROUND(IFNULL(d.discount_percent,0), 1) SEPARATOR ',') AS discount_pcts,
        SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)) AS Cost,
        SUM(GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)) AS Qty
    FROM inv_order_details d
    LEFT JOIN inv_items_master i ON i.id = d.item_id
    GROUP BY d.order_id
) ord ON ord.order_id = o.id
WHERE {condition}
ORDER BY o.date_added ASC, o.id ASC;";
        }

        /// <summary>
        /// Billed sales after coupon. Matches Selling Total Sale and cash collected.
        /// </summary>
        private MetricSnapshot GetMetrics(MySqlConnection con, string condition)
        {
            string query = $@"
SELECT
    IFNULL(SUM(GREATEST(0, ord.items_net - {CouponCalculations.SqlCouponOutsideLines})),0) AS Sales,
    IFNULL(SUM(ord.Cost),0) AS Cost,
    COUNT(o.id) AS Orders,
    IFNULL(SUM(ord.Qty),0) AS Qty
FROM inv_orders o
LEFT JOIN (
    SELECT
        d.order_id,
        SUM(IFNULL(d.net_amount,0)) AS items_net,
        SUM(IFNULL(i.cost_price,0) * GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)) AS Cost,
        SUM(GREATEST(IFNULL(d.qty,0) - IFNULL(d.return_qty,0), 0)) AS Qty
    FROM inv_order_details d
    LEFT JOIN inv_items_master i ON i.id = d.item_id
    GROUP BY d.order_id
) ord ON ord.order_id = o.id
WHERE {condition};";

            using (MySqlCommand cmd = new MySqlCommand(query, con))
            {
                BindRevenueDates(cmd);
                using MySqlDataReader reader = cmd.ExecuteReader();
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

            bool billList = IsBillListView();
            SetHeader("Period", cmbType.Text == "Category Wise" ? "Category" : (billList ? "Bill" : "Period"));
            SetHeader("CreatedBy", "Billed by");
            SetHeader("Coupon", "Coupon");
            SetHeader("Orders", "Bills");
            SetHeader("Qty", "Qty Sold");
            SetMoneyColumn("Discount", "Discount");
            if (grid.Columns.Contains("DiscountPercent"))
            {
                grid.Columns["DiscountPercent"].HeaderText = "Disc %";
                grid.Columns["DiscountPercent"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            SetMoneyColumn("Sales", "Sell Amount");
            SetMoneyColumn("Cost", "Cost Amount");
            SetMoneyColumn("Profit", "Profit");

            if (grid.Columns.Contains("ProfitPercent"))
            {
                grid.Columns["ProfitPercent"].HeaderText = "Profit %";
                grid.Columns["ProfitPercent"].DefaultCellStyle.Format = "N2";
                grid.Columns["ProfitPercent"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            DataGridViewAutoSizeColumnsMode previousMode = grid.AutoSizeColumnsMode;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            try
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col == null)
                        continue;

                    if (col.Name != "Period" && col.Name != "CreatedBy" && col.Name != "Coupon")
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                    if (!col.Visible)
                        continue;

                    col.FillWeight = col.Name switch
                    {
                        "Period" => 22f,
                        "CreatedBy" => 14f,
                        "Discount" => 11f,
                        "DiscountPercent" => 12f,
                        "Coupon" => 12f,
                        _ => 10f
                    };
                    int minWidth = col.Name switch
                    {
                        "Period" => billList ? 140 : 100,
                        "CreatedBy" => 90,
                        "Discount" => 80,
                        "DiscountPercent" => 110,
                        "Coupon" => 80,
                        _ => 72
                    };
                    if (minWidth < 2)
                        minWidth = 2;
                    if (col.Width < minWidth)
                        col.Width = minWidth;
                    col.MinimumWidth = minWidth;
                }
            }
            finally
            {
                grid.AutoSizeColumnsMode = previousMode;
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

        private Panel CreateCard(string title, MetricSnapshot metric, Color accent, bool selected = false, string subtitle = "")
        {
            RoundedPanel p = new RoundedPanel
            {
                Width = 224,
                Height = 138,
                BackColor = selected ? Color.FromArgb(239, 246, 255) : Color.White,
                Radius = 8,
                Margin = new Padding(0, 0, 14, 10),
                Padding = new Padding(14),
                ClipToRoundRegion = false,
                BorderColor = selected ? navy : Color.FromArgb(226, 232, 240)
            };

            Panel accentLine = new Panel
            {
                BackColor = accent,
                Dock = DockStyle.Left,
                Width = selected ? 5 : 4
            };

            Label titleLabel = new Label
            {
                Text = title,
                AutoSize = false,
                Width = 180,
                Height = 22,
                Location = new Point(18, 12),
                Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold),
                ForeColor = selected ? navy : textMuted
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
                Text = "Profit: Rs. " + FormatAmount(metric.Profit) +
                    (metric.Sales == 0 ? "" : "  (" + ((metric.Profit / metric.Sales) * 100m).ToString("0.0") + "%)"),
                AutoSize = false,
                Width = 188,
                Height = 18,
                Location = new Point(18, 92),
                Font = new Font("Segoe UI Semibold", 8.6f, FontStyle.Bold),
                ForeColor = metric.Profit >= 0 ? profitGreen : Color.FromArgb(220, 38, 38)
            };

            string qtyText = $"{metric.Orders} bills  |  {metric.Qty} pcs";
            if (selected && !string.IsNullOrWhiteSpace(subtitle))
                qtyText = subtitle + "  ·  " + metric.Orders + " bills";

            Label qtyLabel = new Label
            {
                Text = qtyText,
                AutoSize = false,
                Width = 188,
                Height = 18,
                Location = new Point(18, 114),
                Font = new Font("Segoe UI", 8),
                ForeColor = selected ? navy : textMuted
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

        private void MixChart_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            decimal cost = SumColumn("Cost");
            decimal profit = SumColumn("Profit");
            var slices = new List<PieSlice>();
            if (cost > 0)
                slices.Add(new PieSlice("Cost", cost, costAmber));
            if (profit > 0)
                slices.Add(new PieSlice("Profit", profit, profitGreen));
            else if (profit < 0)
                slices.Add(new PieSlice("Loss", Math.Abs(profit), Color.FromArgb(220, 38, 38)));

            DrawDonut(g, mixChartPanel.ClientRectangle, slices, "Mix");
        }

        private void CategoryChart_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            DrawDonut(g, categoryChartPanel.ClientRectangle, BuildNamedSlices(categoryData), "Share");
        }

        private List<PieSlice> BuildNamedSlices(DataTable table)
        {
            var slices = new List<PieSlice>();
            if (table == null || table.Rows.Count == 0)
                return slices;

            decimal other = 0;
            int index = 0;
            foreach (DataRow row in table.Rows)
            {
                decimal sales = 0;
                if (table.Columns.Contains("Sales") && row["Sales"] != DBNull.Value)
                    sales = Convert.ToDecimal(row["Sales"]);
                if (sales <= 0)
                    continue;

                if (index < sliceColors.Length - 1)
                {
                    string name = row["Period"]?.ToString() ?? "Other";
                    if (name.Length > 14)
                        name = name.Substring(0, 14);
                    slices.Add(new PieSlice(name, sales, sliceColors[index]));
                    index++;
                }
                else
                {
                    other += sales;
                }
            }

            if (other > 0)
                slices.Add(new PieSlice("Other", other, sliceColors[sliceColors.Length - 1]));

            return slices;
        }

        private void DrawDonut(Graphics g, Rectangle area, List<PieSlice> slices, string emptyTitle)
        {
            if (area.Width < 80 || area.Height < 80)
                return;

            decimal total = 0;
            foreach (PieSlice slice in slices)
                total += slice.Value;

            if (slices.Count == 0 || total <= 0)
            {
                DrawCenteredText(g, "No " + emptyTitle.ToLower() + " data", area, textMuted);
                return;
            }

            int legendWidth = 118;
            int pieBox = Math.Min(area.Width - legendWidth - 8, area.Height - 8);
            pieBox = Math.Max(90, pieBox);
            int pieX = 8;
            int pieY = Math.Max(4, (area.Height - pieBox) / 2);
            Rectangle pie = new Rectangle(pieX, pieY, pieBox, pieBox);

            float start = -90f;
            foreach (PieSlice slice in slices)
            {
                float sweep = (float)((double)(slice.Value / total) * 360d);
                if (sweep <= 0)
                    continue;
                using (SolidBrush brush = new SolidBrush(slice.Color))
                {
                    g.FillPie(brush, pie, start, Math.Max(0.5f, sweep));
                }
                start += sweep;
            }

            int hole = (int)(pieBox * 0.52);
            Rectangle holeRect = new Rectangle(
                pie.X + (pieBox - hole) / 2,
                pie.Y + (pieBox - hole) / 2,
                hole,
                hole);
            using (SolidBrush holeBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(holeBrush, holeRect);
            }

            using (Font centerFont = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold))
            using (SolidBrush centerBrush = new SolidBrush(textMain))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(FormatAmount(total), centerFont, centerBrush, holeRect, format);
            }

            int legendX = pie.Right + 10;
            int legendY = Math.Max(8, pie.Y + 4);
            using (Font legendFont = new Font("Segoe UI", 8f))
            using (SolidBrush textBrush = new SolidBrush(textMuted))
            using (SolidBrush valueBrush = new SolidBrush(textMain))
            {
                foreach (PieSlice slice in slices)
                {
                    using (SolidBrush swatch = new SolidBrush(slice.Color))
                    {
                        g.FillEllipse(swatch, legendX, legendY + 3, 9, 9);
                    }

                    decimal pct = total == 0 ? 0 : (slice.Value / total) * 100m;
                    g.DrawString(slice.Label, legendFont, valueBrush, legendX + 14, legendY);
                    g.DrawString(pct.ToString("0.0") + "%  " + FormatAmount(slice.Value), legendFont, textBrush, legendX + 14, legendY + 13);
                    legendY += 32;
                    if (legendY > area.Bottom - 28)
                        break;
                }
            }
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
            int plotW = area.Width - left - right;
            int plotH = area.Height - top - bottom;
            if (plotW <= 0 || plotH <= 0)
                return;

            Rectangle plot = new Rectangle(left, top, plotW, plotH);

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

        private class PieSlice
        {
            public PieSlice(string label, decimal value, Color color)
            {
                Label = label;
                Value = value;
                Color = color;
            }

            public string Label { get; }
            public decimal Value { get; }
            public Color Color { get; }
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
            public bool ClipToRoundRegion { get; set; } = true;
            public Color BorderColor { get; set; } = Color.FromArgb(226, 232, 240);

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 4 || Height < 4)
                    return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath path = CreatePath(ClientRectangle, Radius))
                using (Pen pen = new Pen(BorderColor))
                {
                    if (ClipToRoundRegion)
                        Region = new Region(path);
                    Rectangle borderRect = ClientRectangle;
                    borderRect.Width = Math.Max(1, borderRect.Width - 1);
                    borderRect.Height = Math.Max(1, borderRect.Height - 1);
                    using (GraphicsPath borderPath = CreatePath(borderRect, Radius))
                    {
                        e.Graphics.DrawPath(pen, borderPath);
                    }
                }
            }

            private static GraphicsPath CreatePath(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    path.AddRectangle(new Rectangle(rect.X, rect.Y, Math.Max(1, rect.Width), Math.Max(1, rect.Height)));
                    return path;
                }

                int diameter = Math.Min(Math.Max(1, radius * 2), Math.Min(rect.Width, rect.Height));
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
