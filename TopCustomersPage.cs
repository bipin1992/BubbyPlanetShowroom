using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class TopCustomersPage : UserControl
    {
        readonly ComboBox cmbRange = new ComboBox();
        readonly TextBox txtTop = new TextBox();
        readonly ComboBox cmbBand = new ComboBox();
        readonly ComboBox cmbCoupon = new ComboBox();
        readonly DataGridView dgvTop = new DataGridView();
        readonly DataGridView dgvAssigned = new DataGridView();
        readonly Label lblStatus = new Label();
        bool reloadBusy;

        public TopCustomersPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Build();
            Load += (_, _) => Reload();
        }

        public void Reload()
        {
            if (reloadBusy)
                return;

            reloadBusy = true;
            try
            {
                LoadTopCustomers();
                LoadCouponChoices();
                LoadAssignments();
            }
            finally
            {
                reloadBusy = false;
            }
        }

        void Build()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = Color.White
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

            var title = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(12, 0, 0, 0),
                Text = "Top customers — one coupon for a whole rank group"
            };

            var filterBar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 6, 12, 6) };
            cmbRange.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRange.Width = 160;
            cmbRange.Dock = DockStyle.Left;
            cmbRange.Items.Add("All time");
            cmbRange.Items.Add("This month");
            cmbRange.SelectedIndex = 0;
            cmbRange.SelectedIndexChanged += (_, _) =>
            {
                if (!reloadBusy)
                    LoadTopCustomers();
            };
            var lblTop = new Label
            {
                Text = "Top",
                Dock = DockStyle.Left,
                Width = 36,
                TextAlign = ContentAlignment.MiddleLeft
            };
            txtTop.Dock = DockStyle.Left;
            txtTop.Width = 70;
            txtTop.MaxLength = 4;
            txtTop.Text = "10";
            txtTop.KeyPress += TxtTop_KeyPress;
            txtTop.Leave += (_, _) => ApplyTopCount();
            txtTop.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;
                e.SuppressKeyPress = true;
                ApplyTopCount();
            };
            var btnRefresh = new Button { Text = "Refresh", Width = 90, Dock = DockStyle.Left };
            btnRefresh.Click += (_, _) => Reload();
            filterBar.Controls.Add(btnRefresh);
            filterBar.Controls.Add(txtTop);
            filterBar.Controls.Add(lblTop);
            filterBar.Controls.Add(cmbRange);

            StyleGrid(dgvTop);
            dgvTop.Columns.Add(Col("rank", "Rank", 8));
            dgvTop.Columns.Add(Col("customer_name", "Customer", 28));
            dgvTop.Columns.Add(Col("phone", "Mobile", 16));
            dgvTop.Columns.Add(Col("bill_count", "Bills", 10));
            dgvTop.Columns.Add(MoneyCol("total_purchase", "Purchase ₹", 16));
            dgvTop.Columns.Add(DateCol("last_purchase", "Last purchase"));
            dgvTop.MultiSelect = true;

            var assignBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(12, 6, 12, 6)
            };
            assignBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
            assignBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            assignBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            assignBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            assignBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            assignBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            cmbBand.Dock = DockStyle.Fill;
            cmbBand.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBand.SelectedIndexChanged += (_, _) => SelectBandRows();
            FillBands(0);
            cmbCoupon.Dock = DockStyle.Fill;
            cmbCoupon.DropDownStyle = ComboBoxStyle.DropDownList;
            var btnAssign = new Button { Text = "Assign these", Dock = DockStyle.Fill };
            var btnRemove = new Button { Text = "Remove", Dock = DockStyle.Fill };
            btnAssign.Click += (_, _) => AssignRankGroup();
            btnRemove.Click += (_, _) => RemoveSelected();
            assignBar.Controls.Add(new Label { Text = "Group", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            assignBar.Controls.Add(cmbBand, 1, 0);
            assignBar.Controls.Add(new Label { Text = "Coupon", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 2, 0);
            assignBar.Controls.Add(cmbCoupon, 3, 0);
            assignBar.Controls.Add(btnAssign, 4, 0);
            assignBar.Controls.Add(btnRemove, 5, 0);

            StyleGrid(dgvAssigned);
            dgvAssigned.Columns.Add(Col("id", "ID", 8));
            dgvAssigned.Columns.Add(Col("phone", "Mobile", 14));
            dgvAssigned.Columns.Add(Col("customer_name", "Customer", 22));
            dgvAssigned.Columns.Add(Col("coupon_code", "Coupon", 14));
            dgvAssigned.Columns.Add(Col("uses_per_phone", "Allowed", 10));
            dgvAssigned.Columns.Add(Col("used_count", "Used", 10));
            dgvAssigned.Columns.Add(Col("uses_left", "Left", 10));

            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Padding = new Padding(12, 0, 8, 0);
            lblStatus.ForeColor = Color.FromArgb(71, 85, 105);
            lblStatus.Text = "Type 10, 100, or 1000, then assign one coupon to each group of 10 ranks.";

            var topHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 4) };
            topHost.Controls.Add(dgvTop);
            var assignedHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 0) };
            assignedHost.Controls.Add(dgvAssigned);

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(filterBar, 0, 1);
            root.Controls.Add(topHost, 0, 2);
            root.Controls.Add(assignBar, 0, 3);
            root.Controls.Add(assignedHost, 0, 4);
            root.Controls.Add(lblStatus, 0, 5);
            Controls.Add(root);
        }

        void LoadTopCustomers()
        {
            try
            {
                bool thisMonth = cmbRange.SelectedIndex == 1;
                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureCouponSchema(conn);
                DB.EnsureOrderCouponColumns(conn);

                string sql = @"
SELECT
    TRIM(c.phone) AS phone,
    MAX(TRIM(CONCAT(IFNULL(c.first_name, ''), ' ', IFNULL(c.sur_name, '')))) AS customer_name,
    COUNT(DISTINCT o.id) AS bill_count,
    ROUND(SUM(" + CouponCalculations.SqlOrderBilledSale + @"), 2) AS total_purchase,
    MAX(o.date_added) AS last_purchase
FROM inv_orders o
INNER JOIN inv_customers c ON c.id = o.customer_id
WHERE TRIM(IFNULL(c.phone, '')) REGEXP '^[0-9]{10}$'";

                if (thisMonth)
                    sql += " AND o.date_added >= @from AND o.date_added < @to";

                sql += @"
GROUP BY TRIM(c.phone)
HAVING total_purchase > 0
ORDER BY total_purchase DESC, bill_count DESC
LIMIT " + ListSize();

                using var cmd = new MySqlCommand(sql, conn);
                if (thisMonth)
                {
                    (DateTime from, DateTime toExclusive) = CouponCustomerAccess.ThisMonth(DateTime.Today);
                    cmd.Parameters.Add("@from", MySqlDbType.DateTime).Value = from;
                    cmd.Parameters.Add("@to", MySqlDbType.DateTime).Value = toExclusive;
                }

                var table = new DataTable();
                using (var adapter = new MySqlDataAdapter(cmd))
                    adapter.Fill(table);

                if (!table.Columns.Contains("rank"))
                {
                    DataColumn rank = table.Columns.Add("rank", typeof(int));
                    rank.SetOrdinal(0);
                }

                FillBands(table.Rows.Count);

                for (int i = 0; i < table.Rows.Count; i++)
                    table.Rows[i]["rank"] = i + 1;

                dgvTop.DataSource = table;
                SelectBandRows();
                string range = thisMonth ? "this month" : "all time";
                lblStatus.Text = table.Rows.Count == 0
                    ? $"No customers for {range}."
                    : $"Top {table.Rows.Count} for {range}. Assign one coupon to the highlighted rank group.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Could not load top customers.";
                MessageBox.Show("Failed to load top customers.\n" + ex.Message);
            }
        }

        void LoadCouponChoices()
        {
            int previousId = (cmbCoupon.SelectedItem as CouponChoice)?.Id ?? 0;
            cmbCoupon.Items.Clear();
            try
            {
                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureCouponSchema(conn);
                using var cmd = new MySqlCommand(@"
SELECT id, coupon_code, title, uses_per_phone
FROM inv_coupons
ORDER BY is_active DESC, coupon_code", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var choice = new CouponChoice
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Code = reader["coupon_code"]?.ToString() ?? "",
                        Title = reader["title"]?.ToString() ?? "",
                        UsesPerPhone = Convert.ToInt32(reader["uses_per_phone"])
                    };
                    cmbCoupon.Items.Add(choice);
                    if (choice.Id == previousId)
                        cmbCoupon.SelectedItem = choice;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Could not load coupons.";
                MessageBox.Show("Failed to load coupons.\n" + ex.Message);
            }

            if (cmbCoupon.SelectedIndex < 0 && cmbCoupon.Items.Count > 0)
                cmbCoupon.SelectedIndex = 0;
        }

        void LoadAssignments()
        {
            try
            {
                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureCouponSchema(conn);

                using var cmd = new MySqlCommand(@"
SELECT
    p.id,
    p.phone,
    IFNULL(names.customer_name, '') AS customer_name,
    c.coupon_code,
    c.uses_per_phone,
    IFNULL(used.used_count, 0) AS used_count
FROM inv_coupon_phones p
INNER JOIN inv_coupons c ON c.id = p.coupon_id
LEFT JOIN (
    SELECT
        TRIM(phone) AS phone,
        MAX(TRIM(CONCAT(IFNULL(first_name, ''), ' ', IFNULL(sur_name, '')))) AS customer_name
    FROM inv_customers
    GROUP BY TRIM(phone)
) names ON names.phone = p.phone
LEFT JOIN (
    SELECT
        TRIM(cu.phone) AS phone,
        UPPER(TRIM(o.coupon_code)) AS coupon_code,
        COUNT(*) AS used_count
    FROM inv_orders o
    INNER JOIN inv_customers cu ON cu.id = o.customer_id
    WHERE " + CouponCustomerAccess.SqlOrderStillHasItems + @"
    GROUP BY TRIM(cu.phone), UPPER(TRIM(o.coupon_code))
) used ON used.phone = p.phone AND used.coupon_code = UPPER(TRIM(c.coupon_code))
ORDER BY c.coupon_code, p.phone", conn);

                var table = new DataTable();
                using (var adapter = new MySqlDataAdapter(cmd))
                    adapter.Fill(table);

                if (!table.Columns.Contains("uses_left"))
                    table.Columns.Add("uses_left", typeof(int));

                foreach (DataRow row in table.Rows)
                {
                    int allowed = CouponCustomerAccess.AllowedUses(Convert.ToInt32(row["uses_per_phone"]));
                    int used = Convert.ToInt32(row["used_count"]);
                    row["uses_per_phone"] = allowed;
                    row["uses_left"] = CouponCustomerAccess.UsesLeft(allowed, used);
                }

                dgvAssigned.DataSource = table;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Could not load assignments.";
                MessageBox.Show("Failed to load coupon assignments.\n" + ex.Message);
            }
        }

        int ListSize()
        {
            return int.TryParse(txtTop.Text.Trim(), out int typed)
                ? CouponCustomerAccess.NormalizeTopCount(typed)
                : 10;
        }

        void ApplyTopCount()
        {
            if (reloadBusy)
                return;

            int size = ListSize();
            txtTop.Text = size.ToString(CultureInfo.InvariantCulture);
            LoadTopCustomers();
        }

        void TxtTop_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        void FillBands(int loadedCount)
        {
            int previousFrom = (cmbBand.SelectedItem as CouponCustomerAccess.RankBand)?.From ?? 1;
            int bandCount = loadedCount > 0 ? Math.Min(ListSize(), loadedCount) : ListSize();
            cmbBand.Items.Clear();
            CouponCustomerAccess.RankBand? match = null;
            foreach (CouponCustomerAccess.RankBand band in CouponCustomerAccess.BandsFor(bandCount))
            {
                cmbBand.Items.Add(band);
                if (band.From == previousFrom)
                    match = band;
            }

            cmbBand.SelectedItem = match ?? (cmbBand.Items.Count > 0 ? cmbBand.Items[0] : null);
        }

        void SelectBandRows()
        {
            if (dgvTop.Rows.Count == 0 || cmbBand.SelectedItem is not CouponCustomerAccess.RankBand band)
                return;

            dgvTop.ClearSelection();
            foreach (DataGridViewRow row in dgvTop.Rows)
            {
                if (!int.TryParse(row.Cells["rank"]?.Value?.ToString(), out int rank))
                    continue;
                if (!CouponCustomerAccess.RankInBand(rank, band.From, band.To))
                    continue;
                row.Selected = true;
            }
        }

        void AssignRankGroup()
        {
            if (cmbCoupon.SelectedItem is not CouponChoice coupon || coupon.Id <= 0)
            {
                MessageBox.Show("Create a coupon in the Coupons tab, then assign it here.");
                return;
            }

            if (cmbBand.SelectedItem is not CouponCustomerAccess.RankBand band)
                return;

            var phones = new System.Collections.Generic.List<string>();
            foreach (DataGridViewRow row in dgvTop.Rows)
            {
                if (!int.TryParse(row.Cells["rank"]?.Value?.ToString(), out int rank))
                    continue;
                if (!CouponCustomerAccess.RankInBand(rank, band.From, band.To))
                    continue;
                string phone = CouponCustomerAccess.NormalizePhone(row.Cells["phone"]?.Value?.ToString());
                if (phone.Length == 10)
                    phones.Add(phone);
            }

            if (phones.Count == 0)
            {
                MessageBox.Show("No customers in " + band + ".");
                return;
            }

            if (MessageBox.Show(
                    "Assign " + coupon.Code + " to " + phones.Count + " numbers (" + band + ")?\n\n" +
                    "After this, that coupon works only for assigned mobiles.",
                    "Assign group",
                    MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureCouponSchema(conn);
                int added = 0;
                int already = 0;
                foreach (string phone in phones)
                {
                    using var cmd = new MySqlCommand(@"
INSERT IGNORE INTO inv_coupon_phones (coupon_id, phone)
VALUES (@couponId, @phone)", conn);
                    cmd.Parameters.AddWithValue("@couponId", coupon.Id);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    int wrote = cmd.ExecuteNonQuery();
                    if (wrote > 0)
                        added++;
                    else
                        already++;
                }

                using (var mark = new MySqlCommand("UPDATE inv_coupons SET for_special=1 WHERE id=@id", conn))
                {
                    mark.Parameters.AddWithValue("@id", coupon.Id);
                    mark.ExecuteNonQuery();
                }

                lblStatus.Text = coupon.Code + " assigned to " + added + " numbers in " + band +
                    (already > 0 ? ". " + already + " already had it." : ".");
                LoadAssignments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to assign coupon.\n" + ex.Message);
            }
        }

        void RemoveSelected()
        {
            if (dgvAssigned.CurrentRow == null)
                return;

            object idObj = dgvAssigned.CurrentRow.Cells["id"]?.Value ?? "";
            if (!int.TryParse(idObj.ToString(), out int id) || id <= 0)
                return;

            string phone = dgvAssigned.CurrentRow.Cells["phone"]?.Value?.ToString() ?? "";
            string code = dgvAssigned.CurrentRow.Cells["coupon_code"]?.Value?.ToString() ?? "";
            if (MessageBox.Show($"Remove {code} from {phone}?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureCouponSchema(conn);
                using var cmd = new MySqlCommand("DELETE FROM inv_coupon_phones WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                lblStatus.Text = $"Removed {code} from {phone}.";
                LoadAssignments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to remove assignment.\n" + ex.Message);
            }
        }

        static void StyleGrid(DataGridView grid)
        {
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoGenerateColumns = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Color.White;
            grid.RowHeadersVisible = false;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.GridColor = Color.Gainsboro;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersHeight = 32;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grid.DataError += (_, e) => e.ThrowException = false;
        }

        static DataGridViewTextBoxColumn Col(string name, string header, int weight)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                DataPropertyName = name,
                HeaderText = header,
                FillWeight = weight
            };
        }

        static DataGridViewTextBoxColumn MoneyCol(string name, string header, int weight)
        {
            DataGridViewTextBoxColumn column = Col(name, header, weight);
            column.DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" };
            return column;
        }

        static DataGridViewTextBoxColumn DateCol(string name, string header)
        {
            DataGridViewTextBoxColumn column = Col(name, header, 16);
            column.DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "dd-MMM-yyyy",
                FormatProvider = CultureInfo.InvariantCulture
            };
            return column;
        }

        sealed class CouponChoice
        {
            public int Id { get; init; }
            public string Code { get; init; } = "";
            public string Title { get; init; } = "";
            public int UsesPerPhone { get; init; }

            public override string ToString()
            {
                string title = string.IsNullOrWhiteSpace(Title) ? "" : " — " + Title;
                return Code + title + "  (" + CouponCustomerAccess.AllowedUses(UsesPerPhone) + " / phone)";
            }
        }
    }
}
