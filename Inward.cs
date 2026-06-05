//using System;
//using System.Data;
//using System.Windows.Forms;
//using Microsoft.VisualBasic;
//using MySql.Data.MySqlClient;

//namespace BubbyPlanetShowroom
//{
//    public class Inward : UserControl
//    {
//        TextBox txtBarcode = new TextBox();
//        DataGridView dgv = new DataGridView();

//        string connectionString = "server=localhost;user=root;password=;database=showroom_db";

//        public Inward()
//        {
//            SetupUI();
//        }

//        void SetupUI()
//        {
//            txtBarcode.Dock = DockStyle.Top;
//            txtBarcode.Height = 40;
//            txtBarcode.PlaceholderText = "Scan Barcode Here...";
//            txtBarcode.KeyDown += TxtBarcode_KeyDown;

//            dgv.Dock = DockStyle.Fill;
//            dgv.ReadOnly = true;
//            dgv.AllowUserToAddRows = false;
//            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

//            this.Controls.Add(dgv);
//            this.Controls.Add(txtBarcode);
//        }

//        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
//        {
//            if (e.KeyCode == Keys.Enter)
//            {
//                ProcessScan();
//            }
//        }

//        void ProcessScan()
//        {
//            string barcode = txtBarcode.Text.Trim();

//            if (string.IsNullOrEmpty(barcode))
//                return;

//            using (MySqlConnection con = new MySqlConnection(connectionString))
//            {
//                con.Open();

//                // 🔎 1️⃣ Get item from MASTER
//                MySqlCommand masterCmd = new MySqlCommand(
//                    "SELECT item_code, item_name FROM inv_items_master WHERE item_code=@item_code",
//                    con);

//                masterCmd.Parameters.AddWithValue("@item_code", barcode);

//                MySqlDataReader dr = masterCmd.ExecuteReader();

//                if (!dr.Read())
//                {
//                    dr.Close();
//                    MessageBox.Show("Item not found in Master!");
//                    return;
//                }

//                string code = dr["item_code"].ToString();
//                string name = dr["item_name"].ToString();
//                dr.Close();

//                // 🔎 2️⃣ Check stock table
//                MySqlCommand stockCheck = new MySqlCommand(
//                    "SELECT quantity FROM inv_stock WHERE item_code=@code",
//                    con);

//                stockCheck.Parameters.AddWithValue("@code", code);

//                object result = stockCheck.ExecuteScalar();

//                int newQty = 1;

//                if (result != null)
//                {
//                    int currentQty = Convert.ToInt32(result);
//                    newQty = currentQty + 1;

//                    // 🔄 Update quantity
//                    MySqlCommand update = new MySqlCommand(
//                        "UPDATE inv_stock SET quantity=@qty WHERE item_code=@code",
//                        con);

//                    update.Parameters.AddWithValue("@qty", newQty);
//                    update.Parameters.AddWithValue("@code", code);
//                    update.ExecuteNonQuery();
//                }
//                else
//                {
//                    // 🆕 Insert first time
//                    MySqlCommand insert = new MySqlCommand(
//                        "INSERT INTO inv_stock (item_code, item_name, quantity) VALUES (@code,@name,1)",
//                        con);

//                    insert.Parameters.AddWithValue("@code", code);
//                    insert.Parameters.AddWithValue("@name", name);
//                    insert.ExecuteNonQuery();
//                }

//                // 🖥 Show in IN Grid
//                DataTable dt = new DataTable();
//                dt.Columns.Add("Item Code");
//                dt.Columns.Add("Item Name");
//                dt.Columns.Add("Quantity");

//                dt.Rows.Add(code, name, newQty);
//                dgv.DataSource = dt;

//                // 🔥 Refresh Stock Tab
//                //MainForm.OnStockUpdated();
//            }

//            txtBarcode.Clear();
//            txtBarcode.Focus();
//        }
//    }
//}


using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Inward : UserControl
    {
        TextBox txtBarcode = new TextBox();
        DataGridView dgv = new DataGridView();

        DataTable dt = new DataTable();

        bool isGridLoading = false;

        public Inward()
        {
            SetupUI();
        }

        void SetupUI()
        {
            txtBarcode.Dock = DockStyle.Top;
            txtBarcode.Height = 40;
            txtBarcode.PlaceholderText = "Scan Barcode Here...";
            txtBarcode.KeyDown += TxtBarcode_KeyDown;

            dgv.Dock = DockStyle.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            this.Controls.Add(dgv);
            this.Controls.Add(txtBarcode);

            // ✅ DataTable setup
            dt.Columns.Add("Item Code");
            dt.Columns.Add("Item Name");
            dt.Columns.Add("Quantity");

            dgv.DataSource = dt;

            // Only quantity editable
            dgv.Columns["Item Code"].ReadOnly = true;
            dgv.Columns["Item Name"].ReadOnly = true;

            // EVENTS (IMPORTANT)
            dgv.KeyDown += Dgv_KeyDown;
            dgv.CellValueChanged += Dgv_CellValueChanged;
            dgv.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;
        }

        // 🔥 ENTER → COMMIT EDIT
        private void Dgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                dgv.EndEdit();
                e.SuppressKeyPress = true;
            }
        }

        // 🔥 FORCE COMMIT
        private void Dgv_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgv.IsCurrentCellDirty)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ProcessScan();
                e.SuppressKeyPress = true;
            }
        }

        // 🔍 SCAN PROCESS
        void ProcessScan()
        {
            string barcode = txtBarcode.Text.Trim();

            if (string.IsNullOrEmpty(barcode))
                return;

            using (MySqlConnection con = DB.GetConnection())
            {
                con.Open();

                // 🔎 MASTER CHECK
                MySqlCommand cmd = new MySqlCommand(
                    "SELECT item_code, item_name FROM inv_items_master WHERE item_code=@code", con);

                cmd.Parameters.AddWithValue("@code", barcode);

                MySqlDataReader dr = cmd.ExecuteReader();

                if (!dr.Read())
                {
                    dr.Close();
                    MessageBox.Show("❌ Item not found in Master!");
                    txtBarcode.Clear();
                    return;
                }

                string code = dr["item_code"].ToString();
                string name = dr["item_name"].ToString();
                dr.Close();

                // 🔎 STOCK CHECK (IMPORTANT)
                int dbQty = 0;

                MySqlCommand stockCmd = new MySqlCommand(
                    "SELECT quantity FROM inv_stock WHERE item_code=@code", con);

                stockCmd.Parameters.AddWithValue("@code", code);

                object result = stockCmd.ExecuteScalar();

                if (result != null)
                {
                    dbQty = Convert.ToInt32(result);
                }

                AddOrUpdateGrid(code, name, dbQty);
            }

            txtBarcode.Clear();
            txtBarcode.Focus();
        }

        // 🧠 GRID LOGIC
        void AddOrUpdateGrid(string code, string name, int dbQty)
        {
            isGridLoading = true; // 🔥 START BLOCK

            foreach (DataRow row in dt.Rows)
            {
                if (row["Item Code"].ToString() == code)
                {
                    isGridLoading = false;
                    return;
                }
            }

            dt.Rows.Add(code, name, dbQty);

            isGridLoading = false; // 🔥 END BLOCK
        }

        // ✏️ GRID EDIT → DB UPDATE
        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (isGridLoading) return; // 🔥 GAME CHANGER

            if (e.RowIndex < 0) return;

            if (dgv.Columns[e.ColumnIndex].Name != "Quantity")
                return;

            string code = dgv.Rows[e.RowIndex].Cells["Item Code"].Value.ToString();
            string name = dgv.Rows[e.RowIndex].Cells["Item Name"].Value.ToString();

            var val = dgv.Rows[e.RowIndex].Cells["Quantity"].Value;

            if (!int.TryParse(val?.ToString(), out int qty))
            {
                dgv.Rows[e.RowIndex].Cells["Quantity"].Value = 0;
                return;
            }

            if (qty < 0)
            {
                dgv.Rows[e.RowIndex].Cells["Quantity"].Value = 0;
                return;
            }

            UpdateDatabase(code, name, qty);
        }

        // 💾 DB UPDATE / INSERT
        void UpdateDatabase(string code, string name, int qty)
        {
            using (MySqlConnection con = DB.GetConnection())
            {
                con.Open();

                // Ensure stock date columns exist for age-based discount logic
                DB.EnsureAgeDiscountSchema(con);

                MySqlCommand check = new MySqlCommand(
                    "SELECT COUNT(*) FROM inv_stock WHERE item_code=@code", con);

                check.Parameters.AddWithValue("@code", code);

                int count = Convert.ToInt32(check.ExecuteScalar());

                if (count > 0)
                {
                    MySqlCommand update = new MySqlCommand(
                        "UPDATE inv_stock SET quantity=@qty, last_updated=NOW() WHERE item_code=@code", con);

                    update.Parameters.AddWithValue("@qty", qty);
                    update.Parameters.AddWithValue("@code", code);
                    update.ExecuteNonQuery();
                }
                else
                {
                    MySqlCommand insert = new MySqlCommand(
                        "INSERT INTO inv_stock (item_code, item_name, quantity, date_added, last_updated) VALUES (@code,@name,@qty,NOW(),NOW())", con);

                    insert.Parameters.AddWithValue("@code", code);
                    insert.Parameters.AddWithValue("@name", name);
                    insert.Parameters.AddWithValue("@qty", qty);
                    insert.ExecuteNonQuery();
                }
            }
        }
    }
}
