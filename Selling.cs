using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Selling : UserControl
    {
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
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(241, 245, 249);

            TableLayoutPanel main = new TableLayoutPanel();
            main.Dock = DockStyle.Fill;
            main.RowCount = 3;
            main.ColumnCount = 1;

            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

            Controls.Add(main);

            // Top Panel
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;

            cmbFilter = new ComboBox();
            cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilter.Items.AddRange(new string[]
            {
    "Today",
    "Weekly",
    "Monthly",
    "Yearly",
    "Custom"
            });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.Location = new Point(20, 60);
            cmbFilter.Width = 120;

            topPanel.Controls.Add(cmbFilter);

            cmbUser = new ComboBox();
            cmbUser.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbUser.Location = new Point(150, 55);
            cmbUser.Width = 150;

            topPanel.Controls.Add(cmbUser);

            dtFrom = new DateTimePicker();
            dtFrom.Format = DateTimePickerFormat.Short;
            dtFrom.Location = new Point(320, 60);

            topPanel.Controls.Add(dtFrom);

            dtTo = new DateTimePicker();
            dtTo.Format = DateTimePickerFormat.Short;
            dtTo.Location = new Point(500, 60);

            topPanel.Controls.Add(dtTo);

            btnSearch = new Button();
            btnSearch.Text = "Search";
            btnSearch.Location = new Point(680, 58);
            btnSearch.Width = 100;

            btnSearch.Click += (s, e) =>
            {
                LoadSummary();
                LoadOrders();
            };

            topPanel.Controls.Add(btnSearch);

            lblTodaySale = new Label();
            lblTodaySale.Text = "Today's Sale : ₹0";
            lblTodaySale.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTodaySale.Location = new Point(20, 20);
            lblTodaySale.AutoSize = true;

            lblTodayOrders = new Label();
            lblTodayOrders.Text = "Today's Orders : 0";
            lblTodayOrders.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTodayOrders.Location = new Point(350, 20);
            lblTodayOrders.AutoSize = true;

            topPanel.Controls.Add(lblTodaySale);
            topPanel.Controls.Add(lblTodayOrders);

            // Orders Grid
            dgvOrders = new DataGridView();
            dgvOrders.Dock = DockStyle.Fill;
            dgvOrders.AllowUserToAddRows = false;
            dgvOrders.ReadOnly = true;
            dgvOrders.RowHeadersVisible = false;
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvOrders.Columns.Add("OrderId", "Order ID");
            dgvOrders.Columns.Add("Date", "Date");
            dgvOrders.Columns.Add("Customer", "Customer");
            dgvOrders.Columns.Add("Mobile", "Mobile");
            dgvOrders.Columns.Add("Payment", "Payment");
            dgvOrders.Columns.Add("User", "User");
            dgvOrders.Columns.Add("Amount", "Amount");

            dgvOrders.SelectionChanged += DgvOrders_SelectionChanged;

            // Details Grid
            dgvDetails = new DataGridView();
            dgvDetails.Dock = DockStyle.Fill;
            dgvDetails.AllowUserToAddRows = false;
            dgvDetails.ReadOnly = true;
            dgvDetails.RowHeadersVisible = false;
            dgvDetails.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDetails.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvDetails.Columns.Add("Item", "Item");
            dgvDetails.Columns.Add("Size", "Size");
            dgvDetails.Columns.Add("Qty", "Qty");
            dgvDetails.Columns.Add("Price", "Price");
            dgvDetails.Columns.Add("Discount", "Discount");
            dgvDetails.Columns.Add("Total", "Total");

            main.Controls.Add(topPanel, 0, 0);
            main.Controls.Add(dgvOrders, 0, 1);
            main.Controls.Add(dgvDetails, 0, 2);
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
                        d.price,
                        IFNULL(d.discount_percent,0)
                            discount_percent,
                        d.total
                    FROM inv_order_details d
                    INNER JOIN inv_items_master i
                        ON i.id=d.item_id
                    WHERE d.order_id=@orderId";

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
                            reader["item_name"],
                            reader["size"],
                            reader["qty"],
                            reader["price"],
                            reader["discount_percent"] + "%",
                            reader["total"]
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