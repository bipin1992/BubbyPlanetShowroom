//using System;
//using System.Data;
//using System.Drawing;
//using System.Windows.Forms;
//using MySql.Data.MySqlClient;

//namespace BubbyPlanetShowroom
//{
//    public class Revenue : UserControl
//    {
//        string conn = "server=localhost;user=root;password=;database=showroom_db;";

//        Panel topPanel = new Panel();
//        FlowLayoutPanel summaryPanel = new FlowLayoutPanel();
//        SplitContainer split = new SplitContainer();

//        Panel graphPanel = new Panel();
//        DataGridView grid = new DataGridView();

//        ComboBox cmbType = new ComboBox();
//        ComboBox cmbCustomer = new ComboBox();
//        Button btnLoad = new Button();

//        DataTable graphData = new DataTable();
//        string currentLabelFormat = "dd";

//        public Revenue()
//        {
//            InitUI();
//            LoadCustomers();

//            cmbType.SelectedItem = "Monthly";
//            LoadData();
//        }

//        private void InitUI()
//        {
//            this.Dock = DockStyle.Fill;

//            // 🔷 TOP PANEL
//            topPanel.Dock = DockStyle.Top;
//            topPanel.Height = 60;
//            topPanel.BackColor = Color.FromArgb(30, 41, 59);

//            cmbType.Items.AddRange(new string[] { "Daily", "Monthly", "Yearly", "Till Date" });
//            cmbType.Location = new Point(20, 15);
//            cmbType.Width = 120;

//            cmbCustomer.Location = new Point(160, 15);
//            cmbCustomer.Width = 150;

//            btnLoad.Text = "Load";
//            btnLoad.Location = new Point(330, 15);
//            btnLoad.Click += (s, e) => LoadData();

//            topPanel.Controls.Add(cmbType);
//            topPanel.Controls.Add(cmbCustomer);
//            topPanel.Controls.Add(btnLoad);

//            // 🔷 SUMMARY
//            summaryPanel.Dock = DockStyle.Top;
//            summaryPanel.Height = 100;
//            summaryPanel.Padding = new Padding(10);

//            // 🔷 SPLIT CONTAINER
//            split.Dock = DockStyle.Fill;
//            split.Orientation = Orientation.Vertical;
//            split.SplitterDistance = 400;

//            // LEFT → GRID
//            grid.Dock = DockStyle.Fill;
//            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
//            grid.BorderStyle = BorderStyle.None;
//            grid.BackgroundColor = Color.White;
//            grid.RowHeadersVisible = false;
//            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

//            split.Panel1.Controls.Add(grid);

//            // RIGHT → GRAPH
//            graphPanel.Dock = DockStyle.Fill;
//            graphPanel.BackColor = Color.White;
//            graphPanel.Paint += GraphPanel_Paint;

//            split.Panel2.Controls.Add(graphPanel);

//            // ADD CONTROLS
//            this.Controls.Add(split);
//            this.Controls.Add(summaryPanel);
//            this.Controls.Add(topPanel);

//            // 🔥 FIX ORDER
//            this.Controls.SetChildIndex(split, 0);
//            this.Controls.SetChildIndex(summaryPanel, 1);
//            this.Controls.SetChildIndex(topPanel, 2);
//        }

//        private void LoadCustomers()
//        {
//            using (MySqlConnection con = new MySqlConnection(conn))
//            {
//                con.Open();

//                MySqlDataAdapter da = new MySqlDataAdapter(
//                    "SELECT id, CONCAT(first_name,' ',sur_name) name FROM customers", con);

//                DataTable dt = new DataTable();
//                da.Fill(dt);

//                DataRow r = dt.NewRow();
//                r["id"] = 0;
//                r["name"] = "All";
//                dt.Rows.InsertAt(r, 0);

//                cmbCustomer.DataSource = dt;
//                cmbCustomer.DisplayMember = "name";
//                cmbCustomer.ValueMember = "id";
//            }
//        }

//        private void LoadData()
//        {
//            using (MySqlConnection con = new MySqlConnection(conn))
//            {
//                con.Open();

//                string filter = "";
//                string groupFormat = "";
//                string labelFormat = "";

//                if (cmbType.Text == "Daily")
//                {
//                    filter = "DATE(o.date_added)=CURDATE()";
//                    groupFormat = "DATE(o.date_added)";
//                    labelFormat = "dd";
//                }
//                else if (cmbType.Text == "Monthly")
//                {
//                    filter = "YEAR(o.date_added)=YEAR(CURDATE())";
//                    groupFormat = "DATE(o.date_added)";
//                    labelFormat = "dd";
//                }
//                else if (cmbType.Text == "Yearly")
//                {
//                    filter = "YEAR(o.date_added)=YEAR(CURDATE())";
//                    groupFormat = "MONTH(o.date_added)";
//                    labelFormat = "MM";
//                }
//                else
//                {
//                    filter = "1=1";
//                    groupFormat = "DATE(o.date_added)";
//                    labelFormat = "dd-MM";
//                }

//                if (Convert.ToInt32(cmbCustomer.SelectedValue) != 0)
//                    filter += $" AND o.customer_id={cmbCustomer.SelectedValue}";

//                decimal revenue = GetValue(con, filter);

//                // 🔥 CARDS
//                summaryPanel.Controls.Clear();

//                summaryPanel.Controls.Add(CreateCard("Total", "₹ " + revenue.ToString("N0"), Color.Green));
//                summaryPanel.Controls.Add(CreateCard("Today", "₹ " + GetValue(con, "DATE(o.date_added)=CURDATE()"), Color.Blue));
//                summaryPanel.Controls.Add(CreateCard("This Month", "₹ " + GetValue(con, "MONTH(o.date_added)=MONTH(CURDATE()) AND YEAR(o.date_added)=YEAR(CURDATE())"), Color.Orange));
//                summaryPanel.Controls.Add(CreateCard("This Year", "₹ " + GetValue(con, "YEAR(o.date_added)=YEAR(CURDATE())"), Color.Gray));

//                // 📊 GRAPH DATA
//                MySqlDataAdapter da = new MySqlDataAdapter($@"
//                SELECT {groupFormat} AS grp, SUM(o.grand_total) total
//                FROM orders o
//                WHERE {filter}
//                GROUP BY grp ORDER BY grp", con);

//                graphData = new DataTable();
//                da.Fill(graphData);

//                currentLabelFormat = labelFormat;

//                grid.DataSource = graphData;

//                graphPanel.Invalidate();
//            }
//        }

//        private decimal GetValue(MySqlConnection con, string condition)
//        {
//            object val = new MySqlCommand(
//                $"SELECT SUM(o.grand_total) FROM orders o WHERE {condition}", con
//            ).ExecuteScalar();

//            return val == DBNull.Value ? 0 : Convert.ToDecimal(val);
//        }

//        private Panel CreateCard(string title, string value, Color color)
//        {
//            Panel p = new Panel();
//            p.Width = 200;
//            p.Height = 80;
//            p.BackColor = color;
//            p.Margin = new Padding(10);

//            Label t = new Label();
//            t.Text = title;
//            t.ForeColor = Color.White;
//            t.Location = new Point(10, 10);

//            Label v = new Label();
//            v.Text = value;
//            v.ForeColor = Color.White;
//            v.Font = new Font("Segoe UI", 14, FontStyle.Bold);
//            v.Location = new Point(10, 35);

//            p.Controls.Add(t);
//            p.Controls.Add(v);

//            return p;
//        }

//        private void GraphPanel_Paint(object sender, PaintEventArgs e)
//        {
//            Graphics g = e.Graphics;

//            int width = graphPanel.Width;
//            int height = graphPanel.Height;

//            if (graphData == null || graphData.Rows.Count == 0)
//            {
//                g.DrawString("No Data", new Font("Segoe UI", 12), Brushes.Gray, 100, 100);
//                return;
//            }

//            int margin = 60;
//            int bottom = 50;

//            int totalBars = graphData.Rows.Count;
//            int barWidth = Math.Max(20, (width - margin) / (totalBars * 2));
//            int gap = barWidth / 2;

//            decimal max = 0;
//            foreach (DataRow r in graphData.Rows)
//                if (Convert.ToDecimal(r["total"]) > max)
//                    max = Convert.ToDecimal(r["total"]);

//            if (max == 0) return;

//            int x = margin;

//            foreach (DataRow r in graphData.Rows)
//            {
//                decimal val = Convert.ToDecimal(r["total"]);
//                int h = (int)((val / max) * (height - 80));

//                Rectangle rect = new Rectangle(x, height - h - bottom, barWidth, h);

//                g.FillRectangle(Brushes.SteelBlue, rect);

//                string label = "";
//                try
//                {
//                    if (currentLabelFormat == "MM")
//                        label = System.Globalization.CultureInfo.CurrentCulture
//                            .DateTimeFormat.GetAbbreviatedMonthName(Convert.ToInt32(r["grp"]));
//                    else
//                        label = Convert.ToDateTime(r["grp"]).ToString(currentLabelFormat);
//                }
//                catch { }

//                g.DrawString(label, new Font("Segoe UI", 8), Brushes.Black, x, height - 20);

//                x += barWidth + gap;
//            }
//        }
//    }
//}















































using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Revenue : UserControl
    {
        Panel topPanel = new Panel();
        FlowLayoutPanel summaryPanel = new FlowLayoutPanel();
        Panel graphPanel = new Panel();
        DataGridView grid = new DataGridView();

        ComboBox cmbType = new ComboBox();

        DataTable graphData = new DataTable();

        public Revenue()
        {
            InitUI();
            cmbType.SelectedIndex = 0;
            LoadData(true);
        }

        private void InitUI()
        {
            this.Dock = DockStyle.Fill;

            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 60;
            topPanel.BackColor = Color.FromArgb(30, 41, 59);

            cmbType.Items.AddRange(new string[] { "Daily", "Monthly", "Yearly", "Till Date" });
            cmbType.Location = new Point(20, 15);
            cmbType.Width = 120;
            cmbType.SelectedIndexChanged += (s, e) => LoadData(false);

            topPanel.Controls.Add(cmbType);

            summaryPanel.Dock = DockStyle.Top;
            summaryPanel.Height = 100;

            Panel content = new Panel { Dock = DockStyle.Fill };

            Panel left = new Panel { Width = 300, Dock = DockStyle.Left };
            Panel right = new Panel { Dock = DockStyle.Fill };
            Panel bottomGraph = new Panel { Height = 260, Dock = DockStyle.Bottom };

            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            left.Controls.Add(grid);

            graphPanel.Dock = DockStyle.Fill;
            graphPanel.Paint += GraphPanel_Paint;
            bottomGraph.Controls.Add(graphPanel);

            right.Controls.Add(bottomGraph);

            content.Controls.Add(right);
            content.Controls.Add(left);

            this.Controls.Add(content);
            this.Controls.Add(summaryPanel);
            this.Controls.Add(topPanel);
        }

        private void LoadData(bool loadSummary = false)
        {
            using (MySqlConnection con = DB.GetConnection())
            {
                con.Open();

                if (loadSummary)
                    LoadSummary(con);

                LoadGraphGrid(con);
            }
        }

        // 🔥 CARDS (CORRECT)
        private void LoadSummary(MySqlConnection con)
        {
            summaryPanel.Controls.Clear();

            // 🟢 TODAY
            decimal today = GetValue(con,
                "DATE(date_added) = (SELECT DATE(MAX(date_added)) FROM inv_orders)");

            // 🟠 WEEK (last 7 days)
            decimal week = GetValue(con,
                "DATE(date_added) >= DATE_SUB((SELECT MAX(date_added) FROM inv_orders), INTERVAL 7 DAY)");

            // 🔵 MONTH (current month of latest data)
            decimal month = GetValue(con,
                "YEAR(date_added)=YEAR((SELECT MAX(date_added) FROM inv_orders)) " +
                "AND MONTH(date_added)=MONTH((SELECT MAX(date_added) FROM inv_orders))");

            // 🔷 YEAR (current year of latest data)
            decimal year = GetValue(con,
                "YEAR(date_added)=YEAR((SELECT MAX(date_added) FROM inv_orders))");

            // ⚫ TOTAL
            decimal total = GetValue(con, "1=1");

            summaryPanel.Controls.Add(CreateCard("Today", "₹ " + FormatAmount(today), Color.Green));
            summaryPanel.Controls.Add(CreateCard("This Week", "₹ " + FormatAmount(week), Color.Blue));
            summaryPanel.Controls.Add(CreateCard("This Month", "₹ " + FormatAmount(month), Color.Orange));
            summaryPanel.Controls.Add(CreateCard("This Year", "₹ " + FormatAmount(year), Color.Purple));
            summaryPanel.Controls.Add(CreateCard("Total", "₹ " + FormatAmount(total), Color.Gray));
        }

        // 🔥 MAIN LOGIC
        private void LoadGraphGrid(MySqlConnection con)
        {
            string query = "";

            // 🟢 DAILY
            if (cmbType.Text == "Daily")
            {
                query = @"
                SELECT DATE(date_added) grp, SUM(grand_total) total
                FROM inv_orders
                WHERE DATE(date_added) = (SELECT DATE(MAX(date_added)) FROM inv_orders)
                HAVING SUM(grand_total) > 0";

            }

            // 🟠 MONTHLY (12 MONTHS)
            else if (cmbType.Text == "Monthly")
            {
                query = @"
                        SELECT 
                            CASE m.month_num
                                WHEN 1 THEN 'Jan'
                                WHEN 2 THEN 'Feb'
                                WHEN 3 THEN 'Mar'
                                WHEN 4 THEN 'Apr'
                                WHEN 5 THEN 'May'
                                WHEN 6 THEN 'Jun'
                                WHEN 7 THEN 'Jul'
                                WHEN 8 THEN 'Aug'
                                WHEN 9 THEN 'Sep'
                                WHEN 10 THEN 'Oct'
                                WHEN 11 THEN 'Nov'
                                WHEN 12 THEN 'Dec'
                            END AS grp,
                            SUM(o.grand_total) AS total
                        FROM 
                        (
                            SELECT 1 AS month_num UNION SELECT 2 UNION SELECT 3 UNION SELECT 4
                            UNION SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8
                            UNION SELECT 9 UNION SELECT 10 UNION SELECT 11 UNION SELECT 12
                        ) m
                        LEFT JOIN inv_orders o 
                            ON MONTH(o.date_added) = m.month_num 
                            AND YEAR(o.date_added) = YEAR(CURDATE())
                        GROUP BY m.month_num
                        HAVING total > 0
                        ORDER BY m.month_num DESC";
            }

            // 🔵 YEARLY
            else if (cmbType.Text == "Yearly")
            {
                query = @"
                        SELECT YEAR(date_added) grp, SUM(grand_total) total
                        FROM inv_orders
                        GROUP BY YEAR(date_added)
                        HAVING total > 0
                        ORDER BY grp DESC";
            }

            // ⚫ TILL DATE
            else
            {
                query = @"
                SELECT 'All' grp, SUM(grand_total) total
                FROM inv_orders
                HAVING total > 0";
            }

            MySqlDataAdapter da = new MySqlDataAdapter(query, con);
            graphData = new DataTable();
            da.Fill(graphData);

            grid.DataSource = graphData;

            graphPanel.Refresh();
        }

        private decimal GetValue(MySqlConnection con, string condition)
        {
            object val = new MySqlCommand(
                $"SELECT IFNULL(SUM(grand_total),0) FROM inv_orders WHERE {condition}", con
            ).ExecuteScalar();

            return Convert.ToDecimal(val);
        }

        private string FormatAmount(decimal amount)
        {
            if (amount >= 10000000) // 1 Cr
                return (amount / 10000000).ToString("0.#") + "Cr";

            if (amount >= 100000) // 1 Lakh
                return (amount / 100000).ToString("0.#") + "L";

            if (amount >= 1000) // 1 Thousand
                return (amount / 1000).ToString("0.#") + "K";

            return amount.ToString("0");
        }

        private Panel CreateCard(string t, string v, Color c)
        {
            Panel p = new Panel { Width = 180, Height = 80, BackColor = c };

            Label l1 = new Label { Text = t, ForeColor = Color.White, Location = new Point(10, 10) };
            Label l2 = new Label
            {
                Text = v,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 35)
            };

            p.Controls.Add(l1);
            p.Controls.Add(l2);

            return p;
        }

        private void GraphPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (graphData == null || graphData.Rows.Count == 0) return;

            int height = graphPanel.Height;

            decimal max = 0;
            foreach (DataRow r in graphData.Rows)
            {
                decimal v = Convert.ToDecimal(r["total"]);
                if (v > max) max = v;
            }

            int x = 50;

            foreach (DataRow r in graphData.Rows)
            {
                decimal val = Convert.ToDecimal(r["total"]);
                int h = (int)((val / max) * (height - 100));
                if (h < 5) h = 5;

                g.FillRectangle(Brushes.SteelBlue, x, height - h - 40, 30, h);

                g.DrawString(val.ToString("N0"),
                    new Font("Segoe UI", 8),
                    Brushes.Black,
                    x, height - h - 55);

                g.DrawString(r["grp"].ToString(),
                    new Font("Segoe UI", 7),
                    Brushes.Black,
                    x, height - 20);

                x += 45;
            }
        }
    }
}
