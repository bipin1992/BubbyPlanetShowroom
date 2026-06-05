//using System;
//using System.Data;
//using System.Drawing;
//using System.Windows.Forms;
//using MySql.Data.MySqlClient;

//namespace BubbyPlanetShowroom
//{
//    public class Stock : UserControl
//    {
//        DataGridView dgv = new DataGridView();

//        TextBox txtSearch = new TextBox();

//        string connectionString = "server=localhost;user=root;password=;database=showroom_db";

//        DataTable dt = new DataTable(); // 🔥 global DataTable

//        public Stock()
//        {
//            SetupUI();
//            LoadStock();
//        }

//        void SetupUI()
//        {
//            // 🔍 Search Box
//            txtSearch.Dock = DockStyle.Top;
//            txtSearch.PlaceholderText = "Search Item Code or Name...";
//            txtSearch.Height = 30;
//            txtSearch.TextChanged += TxtSearch_TextChanged;

//            // 📊 DataGridView
//            dgv.Dock = DockStyle.Fill;
//            dgv.ReadOnly = true;
//            dgv.AllowUserToAddRows = false;
//            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

//            // 🔥 FULL ROW SELECT
//            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
//            dgv.MultiSelect = false;
//            dgv.ReadOnly = true;

//            // 🔥 REMOVE LEFT EMPTY COLUMN
//            dgv.RowHeadersVisible = false;

//            // 🔥 ALTERNATE ROW COLOR (ZEBRA)
//            dgv.DefaultCellStyle.BackColor = Color.White;
//            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

//            // 🔥 SELECTION COLOR
//            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
//            dgv.DefaultCellStyle.SelectionForeColor = Color.White;

//            // 🔥 HEADER STYLE
//            dgv.EnableHeadersVisualStyles = false;
//            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkRed;
//            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
//            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

//            // 🔥 ALIGNMENT
//            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
//            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

//            // 🔥 ROW HEIGHT
//            dgv.RowTemplate.Height = 30;

//            // 🔥 HEADER FIX HEIGHT (SCROLL SMOOTH)
//            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
//            dgv.ColumnHeadersHeight = 35;

//            // 🔥 SCROLL
//            dgv.ScrollBars = ScrollBars.Both;

//            this.Controls.Add(dgv);
//            this.Controls.Add(txtSearch);

//            dgv.CellClick += (s, e) =>
//            {
//                if (e.RowIndex >= 0)
//                    dgv.Rows[e.RowIndex].Selected = true;
//            };
//        }

//        void LoadStock()
//        {
//            using (MySqlConnection con = new MySqlConnection(connectionString))
//            {
//                con.Open();

//                MySqlDataAdapter da = new MySqlDataAdapter(
//                    "SELECT item_code AS 'Item Code', item_name AS 'Item Name', quantity AS 'Quantity' FROM inv_stock ORDER BY id DESC",
//                    con);

//                dt.Clear(); // 🔥 important
//                da.Fill(dt);

//                dgv.DataSource = dt;
//            }
//        }

//        // 🔥 FILTER LOGIC
//        void TxtSearch_TextChanged(object sender, EventArgs e)
//        {
//            string searchText = txtSearch.Text.Trim().Replace("'", "''");

//            if (string.IsNullOrEmpty(searchText))
//            {
//                // 🔄 RESET → full data show
//                dgv.DataSource = dt;
//            }
//            else
//            {
//                DataView dv = dt.DefaultView;

//                // 🔍 search both Item Code & Item Name
//                dv.RowFilter = $"[Item Code] LIKE '%{searchText}%' OR [Item Name] LIKE '%{searchText}%'";

//                dgv.DataSource = dv;
//            }
//        }

//        // 🔄 external refresh (agar future me use karo)
//        public void RefreshStock()
//        {
//            LoadStock();
//        }
//    }
//}




using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Stock : UserControl
    {
        DataGridView dgv = new DataGridView();
        TextBox txtSearch = new TextBox();
        Button btnNext = new Button();
        Button btnPrev = new Button();
        Label lblPage = new Label();

        DataTable dt = new DataTable();

        int pageSize = 100;
        int currentPage = 0;
        string currentSearch = "";

        public Stock()
        {
            SetupUI();
            LoadStockAsync();
        }

        void SetupUI()
        {
            txtSearch.Dock = DockStyle.Top;
            txtSearch.PlaceholderText = "Search Item Code or Name...";
            txtSearch.Height = 30;
            txtSearch.TextChanged += TxtSearch_TextChanged;

            dgv.Dock = DockStyle.Fill;
            dgv.ReadOnly = false;
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.CellEndEdit += Dgv_CellEndEdit;

            // Buttons Panel
            Panel panel = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            btnPrev.Text = "◀ Previous";
            btnNext.Text = "Next ▶";
            lblPage.AutoSize = true;

            btnPrev.Left = 10;
            btnNext.Left = 120;
            lblPage.Left = 250;
            lblPage.Top = 10;

            btnPrev.Click += (s, e) =>
            {
                if (currentPage > 0)
                {
                    currentPage--;
                    LoadStockAsync();
                }
            };

            btnNext.Click += (s, e) =>
            {
                currentPage++;
                LoadStockAsync();
            };

            panel.Controls.Add(btnPrev);
            panel.Controls.Add(btnNext);
            panel.Controls.Add(lblPage);

            this.Controls.Add(dgv);
            this.Controls.Add(panel);
            this.Controls.Add(txtSearch);
        }

        async void LoadStockAsync()
        {
            await Task.Run(() => LoadStock());
        }

        void LoadStock()
        {
            using (MySqlConnection con = DB.GetConnection())
            {
                con.Open();

                // Show all items from master; if item is not yet in stock, default quantity to 0.
                string query = @"SELECT i.item_code AS 'Item Code',
                                        i.item_name AS 'Item Name',
                                        COALESCE(s.quantity, 0) AS 'Quantity'
                                 FROM inv_items_master i
                                 LEFT JOIN inv_stock s ON s.item_code = i.item_code
                                 WHERE i.item_code LIKE @search OR i.item_name LIKE @search
                                 ORDER BY i.id DESC
                                 LIMIT @offset, @limit";

                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@search", "%" + currentSearch + "%");
                cmd.Parameters.AddWithValue("@offset", currentPage * pageSize);
                cmd.Parameters.AddWithValue("@limit", pageSize);

                MySqlDataAdapter da = new MySqlDataAdapter(cmd);

                DataTable temp = new DataTable();
                da.Fill(temp);

                // UI thread update
                Invoke(new Action(() =>
                {
                    dgv.DataSource = temp;
                    ConfigureGridColumns();
                    lblPage.Text = $"Page: {currentPage + 1}";
                }));
            }
        }

        void ConfigureGridColumns()
        {
            if (dgv.Columns.Count == 0)
                return;

            if (dgv.Columns.Contains("Item Code"))
                dgv.Columns["Item Code"].ReadOnly = true;

            if (dgv.Columns.Contains("Item Name"))
                dgv.Columns["Item Name"].ReadOnly = true;

            if (dgv.Columns.Contains("Quantity"))
                dgv.Columns["Quantity"].ReadOnly = false;
        }

        void Dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (dgv.Columns[e.ColumnIndex].Name != "Quantity")
                return;

            DataGridViewRow row = dgv.Rows[e.RowIndex];
            string itemCode = row.Cells["Item Code"]?.Value?.ToString() ?? "";
            string qtyText = row.Cells["Quantity"]?.Value?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(itemCode))
                return;

                if (!int.TryParse(qtyText, out int qty) || qty < 0)
                {
                    MessageBox.Show("Enter valid quantity (0 or greater).");
                    LoadStockAsync();
                    return;
                }

            try
            {
                using (MySqlConnection con = DB.GetConnection())
                {
                    con.Open();
                    string itemName = row.Cells["Item Name"]?.Value?.ToString() ?? "";

                    // Upsert so items that don't yet exist in inv_stock can still be stocked here.
                    MySqlCommand cmd = new MySqlCommand(@"
INSERT INTO inv_stock (item_code, item_name, quantity, last_updated)
VALUES (@code, @name, @qty, NOW())
ON DUPLICATE KEY UPDATE
    item_name = VALUES(item_name),
    quantity = VALUES(quantity),
    last_updated = NOW()", con);
                    cmd.Parameters.AddWithValue("@qty", qty);
                    cmd.Parameters.AddWithValue("@code", itemCode);
                    cmd.Parameters.AddWithValue("@name", itemName);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Quantity update failed: " + ex.Message);
                LoadStockAsync();
            }
        }

        void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            currentSearch = txtSearch.Text.Trim();
            currentPage = 0;
            LoadStockAsync();
        }

        public void RefreshStock()
        {
            currentPage = 0;
            LoadStockAsync();
        }
    }
}
