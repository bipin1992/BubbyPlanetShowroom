using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class DiscountManager : UserControl
    {
        readonly string currentRole;

        DataGridView dgvRules;
        Button btnNew;
        Button btnUpsert;
        Button btnDelete;
        Button btnRefresh;
        Label lblTitle;
        Label lblStatus;

        TextBox txtRuleName;
        ComboBox cmbCategory;
        ComboBox cmbSubCategory;
        ComboBox cmbGender;
        TextBox txtItemCode;
        NumericUpDown nudAgeMonths;
        NumericUpDown nudDiscountPercent;
        CheckBox chkActive;
        CheckBox chkStaffOnly;

        int editingRuleId = 0;

        DataTable? rulesTable;

        public DiscountManager(string role = "")
        {
            currentRole = (role ?? "").Trim();
            InitializeUI();
            this.Load += (s, e) => Reload();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;

            lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 42,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(12, 0, 0, 0),
                Text = "Discount Rules (Months-old items) — Master Admin only"
            };
            this.Controls.Add(lblTitle);

            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(12, 6, 12, 6) };
            this.Controls.Add(topBar);

            btnNew = new Button { Text = "New", Width = 90, Dock = DockStyle.Left };
            btnUpsert = new Button { Text = "Add Rule", Width = 110, Dock = DockStyle.Left };
            btnDelete = new Button { Text = "Delete", Width = 90, Dock = DockStyle.Left };
            btnRefresh = new Button { Text = "Refresh", Width = 90, Dock = DockStyle.Left };

            btnNew.Click += (s, e) => ResetForm();
            btnUpsert.Click += (s, e) => UpsertRule();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnRefresh.Click += (s, e) => Reload();

            topBar.Controls.Add(btnRefresh);
            topBar.Controls.Add(btnDelete);
            topBar.Controls.Add(btnUpsert);
            topBar.Controls.Add(btnNew);

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                ForeColor = Color.FromArgb(71, 85, 105),
                Text = ""
            };
            this.Controls.Add(lblStatus);

            TableLayoutPanel formPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 170,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.White,
                ColumnCount = 4,
                RowCount = 5
            };
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // rule name
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // category/sub
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // gender/minAge
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F)); // itemcode/discount
            formPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F)); // active
            this.Controls.Add(formPanel);

            txtRuleName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "e.g. 3 months old Toys 10%" };
            cmbCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSubCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGender = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            txtItemCode = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Optional: exact item_code" };
            nudAgeMonths = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 120, DecimalPlaces = 0, Value = 0 };
            nudDiscountPercent = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, DecimalPlaces = 2, Increment = 0.5m, Value = 0 };
            chkActive = new CheckBox { Dock = DockStyle.Bottom, Text = "Active", Checked = true, Height = 20 };
            chkStaffOnly = new CheckBox { Dock = DockStyle.Bottom, Text = "Staff only", Checked = false, Height = 20 };

            // Bind once (Reload() refreshes the list items but shouldn't multiply event handlers)
            cmbCategory.SelectedIndexChanged += (s, e) =>
            {
                cmbSubCategory.Items.Clear();
                LoadSubCategories();
            };

            formPanel.Controls.Add(new Label { Text = "Rule Name", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            formPanel.Controls.Add(txtRuleName, 1, 0);
            formPanel.SetColumnSpan(txtRuleName, 3);

            formPanel.Controls.Add(new Label { Text = "Category", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            formPanel.Controls.Add(cmbCategory, 1, 1);
            formPanel.Controls.Add(new Label { Text = "Sub Category", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 2, 1);
            formPanel.Controls.Add(cmbSubCategory, 3, 1);

            formPanel.Controls.Add(new Label { Text = "Gender", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
            formPanel.Controls.Add(cmbGender, 1, 2);
            formPanel.Controls.Add(new Label { Text = "Min Age (months)", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 2, 2);
            formPanel.Controls.Add(nudAgeMonths, 3, 2);

            formPanel.Controls.Add(new Label { Text = "Item Code", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 3);
            formPanel.Controls.Add(txtItemCode, 1, 3);
            formPanel.Controls.Add(new Label { Text = "Discount %", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 2, 3);
            formPanel.Controls.Add(nudDiscountPercent, 3, 3);

            formPanel.Controls.Add(chkActive, 1, 4);
            formPanel.Controls.Add(chkStaffOnly, 3, 4);

            dgvRules = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.Gainsboro,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 30,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };

            dgvRules.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvRules.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRules.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            dgvRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", DataPropertyName = "id", HeaderText = "ID", ReadOnly = true, FillWeight = 8 });
            dgvRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "rule_name", DataPropertyName = "rule_name", HeaderText = "Rule Name", FillWeight = 22 });
            dgvRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "main_category", DataPropertyName = "main_category", HeaderText = "Category", FillWeight = 14 });
            dgvRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "sub_category", DataPropertyName = "sub_category", HeaderText = "Sub", FillWeight = 12 });
            dgvRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "gender", DataPropertyName = "gender", HeaderText = "Gender", FillWeight = 10 });
            dgvRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "item_code", DataPropertyName = "item_code", HeaderText = "Item Code", FillWeight = 16 });
            dgvRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "min_age_months", DataPropertyName = "min_age_months", HeaderText = "Months", FillWeight = 10 });
            dgvRules.Columns.Add(new DataGridViewTextBoxColumn { Name = "discount_percent", DataPropertyName = "discount_percent", HeaderText = "%", FillWeight = 8, DefaultCellStyle = new DataGridViewCellStyle { Format = "0.##" } });
            dgvRules.Columns.Add(new DataGridViewCheckBoxColumn { Name = "staff_only", DataPropertyName = "staff_only", HeaderText = "Staff", FillWeight = 8 });
            dgvRules.Columns.Add(new DataGridViewCheckBoxColumn { Name = "is_active", DataPropertyName = "is_active", HeaderText = "Active", FillWeight = 8 });

            dgvRules.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRules.MultiSelect = false;
            dgvRules.ReadOnly = false;
            foreach (DataGridViewColumn c in dgvRules.Columns)
                c.ReadOnly = true;
            dgvRules.Columns["is_active"].ReadOnly = false;
            dgvRules.CellDoubleClick += (s, e) => LoadSelectedIntoForm();
            dgvRules.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0)
                    return;
                if (dgvRules.Columns[e.ColumnIndex].Name == "is_active")
                {
                    // Commit checkbox edit first, so the Value reflects the user's click.
                    dgvRules.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    dgvRules.EndEdit();
                    ToggleActiveFromGrid(e.RowIndex);
                }
            };

            this.Controls.Add(dgvRules);

            bool isMaster = string.Equals(currentRole, "Master Admin", StringComparison.OrdinalIgnoreCase);
            if (!isMaster)
            {
                lblTitle.Text = "Discount Rules (access denied: Master Admin only)";
                dgvRules.Enabled = false;
                btnNew.Enabled = false;
                btnUpsert.Enabled = false;
                btnDelete.Enabled = false;
                formPanel.Enabled = false;
            }
        }

        private void Reload()
        {
            try
            {
                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureAgeDiscountSchema(conn);

                // Ensure grid is visible after reload
                dgvRules.Visible = true;
                dgvRules.BringToFront();

                // Rebuild categories each time (so new categories appear too)
                cmbCategory.Items.Clear();
                cmbSubCategory.Items.Clear();
                cmbGender.Items.Clear();
                LoadCategories(conn);
                LoadSubCategories();
                LoadGenders(conn);

                using var da = new MySqlDataAdapter(
                    "SELECT id, rule_name, main_category, sub_category, gender, item_code, min_age_months, discount_percent, staff_only, is_active FROM inv_age_discount_rules ORDER BY is_active DESC, staff_only DESC, min_age_months DESC, rule_name",
                    conn
                );

                rulesTable = new DataTable();
                da.Fill(rulesTable);

                if (!rulesTable.Columns.Contains("is_active"))
                    rulesTable.Columns.Add("is_active", typeof(int));
                if (!rulesTable.Columns.Contains("staff_only"))
                    rulesTable.Columns.Add("staff_only", typeof(int));

                dgvRules.DataSource = rulesTable;
                lblStatus.Text = $"Loaded rules: {rulesTable.Rows.Count}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Load failed.";
                MessageBox.Show("Failed to load discount rules.\n" + ex.Message);
            }
        }

        private void LoadCategories(MySqlConnection conn)
        {
            if (cmbCategory.Items.Count > 0)
                return;

            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("All");

            try
            {
                using var cmd = new MySqlCommand("SELECT DISTINCT main_category FROM inv_items_master WHERE IFNULL(main_category,'')<>'' ORDER BY main_category", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string cat = reader[0]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(cat))
                        cmbCategory.Items.Add(cat.Trim());
                }
            }
            catch
            {
                // categories optional
            }

            cmbCategory.SelectedIndex = 0;
        }

        private void LoadSubCategories()
        {
            cmbSubCategory.Items.Clear();
            cmbSubCategory.Items.Add("All");

            try
            {
                string category = (cmbCategory.SelectedItem?.ToString() ?? "All").Trim();
                using var conn = DB.GetConnection();
                conn.Open();

                string sql = "SELECT DISTINCT sub_category FROM inv_items_master WHERE IFNULL(sub_category,'')<>''";
                if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
                    sql += " AND main_category=@cat";
                sql += " ORDER BY sub_category";

                using var cmd = new MySqlCommand(sql, conn);
                if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
                    cmd.Parameters.AddWithValue("@cat", category);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string sub = reader[0]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(sub))
                        cmbSubCategory.Items.Add(sub.Trim());
                }
            }
            catch { }

            cmbSubCategory.SelectedIndex = 0;
        }

        private void LoadGenders(MySqlConnection conn)
        {
            cmbGender.Items.Clear();
            cmbGender.Items.Add("All");

            try
            {
                using var cmd = new MySqlCommand("SELECT DISTINCT gender FROM inv_items_master WHERE IFNULL(gender,'')<>'' ORDER BY gender", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string g = reader[0]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(g))
                        cmbGender.Items.Add(g.Trim());
                }
            }
            catch { }

            cmbGender.SelectedIndex = 0;
        }

        private void ResetForm()
        {
            editingRuleId = 0;
            txtRuleName.Text = "";
            if (cmbCategory.Items.Count == 0)
                cmbCategory.Items.Add("All");
            cmbCategory.SelectedIndex = 0;
            if (cmbSubCategory.Items.Count == 0)
                cmbSubCategory.Items.Add("All");
            cmbSubCategory.SelectedIndex = 0;
            if (cmbGender.Items.Count == 0)
                cmbGender.Items.Add("All");
            cmbGender.SelectedIndex = 0;
            txtItemCode.Text = "";
            nudAgeMonths.Value = 0;
            nudDiscountPercent.Value = 0;
            chkActive.Checked = true;
            chkStaffOnly.Checked = false;
            btnUpsert.Text = "Add Rule";
            if (rulesTable == null)
                lblStatus.Text = "New rule.";
            else
                lblStatus.Text = $"Loaded rules: {rulesTable.Rows.Count}";
        }

        private void LoadSelectedIntoForm()
        {
            if (dgvRules.CurrentRow == null)
                return;

            object idObj = dgvRules.CurrentRow.Cells["id"]?.Value ?? "";
            if (!int.TryParse(idObj.ToString(), out int id) || id <= 0)
                return;

            editingRuleId = id;
            txtRuleName.Text = dgvRules.CurrentRow.Cells["rule_name"]?.Value?.ToString() ?? "";

            string cat = dgvRules.CurrentRow.Cells["main_category"]?.Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(cat))
            {
                cmbCategory.SelectedIndex = 0;
            }
            else
            {
                int idx = cmbCategory.FindStringExact(cat);
                cmbCategory.SelectedIndex = idx >= 0 ? idx : 0;
            }

            string sub = dgvRules.CurrentRow.Cells["sub_category"]?.Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(sub))
            {
                cmbSubCategory.SelectedIndex = 0;
            }
            else
            {
                int sidx = cmbSubCategory.FindStringExact(sub);
                cmbSubCategory.SelectedIndex = sidx >= 0 ? sidx : 0;
            }

            string gen = dgvRules.CurrentRow.Cells["gender"]?.Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(gen))
            {
                cmbGender.SelectedIndex = 0;
            }
            else
            {
                int gidx = cmbGender.FindStringExact(gen);
                cmbGender.SelectedIndex = gidx >= 0 ? gidx : 0;
            }

            txtItemCode.Text = dgvRules.CurrentRow.Cells["item_code"]?.Value?.ToString() ?? "";

            if (int.TryParse(dgvRules.CurrentRow.Cells["min_age_months"]?.Value?.ToString(), out int m))
                nudAgeMonths.Value = Math.Min(nudAgeMonths.Maximum, Math.Max(nudAgeMonths.Minimum, m));

            if (decimal.TryParse(dgvRules.CurrentRow.Cells["discount_percent"]?.Value?.ToString(), out decimal d))
                nudDiscountPercent.Value = Math.Min(nudDiscountPercent.Maximum, Math.Max(nudDiscountPercent.Minimum, d));

            bool active = false;
            object aObj = dgvRules.CurrentRow.Cells["is_active"]?.Value ?? 0;
            if (aObj is bool b)
                active = b;
            else
                active = aObj.ToString() == "1";
            chkActive.Checked = active;

            bool staffOnly = false;
            object staffObj = dgvRules.CurrentRow.Cells["staff_only"]?.Value ?? 0;
            if (staffObj is bool sb)
                staffOnly = sb;
            else
                staffOnly = staffObj.ToString() == "1";
            chkStaffOnly.Checked = staffOnly;

            btnUpsert.Text = "Update Rule";
            lblStatus.Text = $"Editing rule #{editingRuleId} (double-click row to edit).";
        }

        private void UpsertRule()
        {
            string ruleName = (txtRuleName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(ruleName))
            {
                MessageBox.Show("Rule name required.");
                txtRuleName.Focus();
                return;
            }

            string category = (cmbCategory.SelectedItem?.ToString() ?? "All").Trim();
            if (string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
                category = "";

            string subCategory = (cmbSubCategory.SelectedItem?.ToString() ?? "All").Trim();
            if (string.Equals(subCategory, "All", StringComparison.OrdinalIgnoreCase))
                subCategory = "";

            string gender = (cmbGender.SelectedItem?.ToString() ?? "All").Trim();
            if (string.Equals(gender, "All", StringComparison.OrdinalIgnoreCase))
                gender = "";

            string itemCode = (txtItemCode.Text ?? "").Trim();
            int ageMonths = (int)nudAgeMonths.Value;
            decimal discountPercent = nudDiscountPercent.Value;
            int isActive = chkActive.Checked ? 1 : 0;
            int staffOnly = chkStaffOnly.Checked ? 1 : 0;

            try
            {
                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureAgeDiscountSchema(conn);

                if (editingRuleId <= 0)
                {
                    using var cmd = new MySqlCommand(@"
INSERT INTO inv_age_discount_rules
(rule_name, main_category, sub_category, gender, item_code, min_age_months, discount_percent, staff_only, is_active)
VALUES
(@name, @cat, @sub, @gender, @code, @age, @disc, @staffOnly, @active);", conn);

                    cmd.Parameters.AddWithValue("@name", ruleName);
                    cmd.Parameters.AddWithValue("@cat", string.IsNullOrWhiteSpace(category) ? DBNull.Value : category);
                    cmd.Parameters.AddWithValue("@sub", string.IsNullOrWhiteSpace(subCategory) ? DBNull.Value : subCategory);
                    cmd.Parameters.AddWithValue("@gender", string.IsNullOrWhiteSpace(gender) ? DBNull.Value : gender);
                    cmd.Parameters.AddWithValue("@code", string.IsNullOrWhiteSpace(itemCode) ? DBNull.Value : itemCode);
                    cmd.Parameters.AddWithValue("@age", ageMonths);
                    cmd.Parameters.AddWithValue("@disc", discountPercent);
                    cmd.Parameters.AddWithValue("@staffOnly", staffOnly);
                    cmd.Parameters.AddWithValue("@active", isActive);
                    cmd.ExecuteNonQuery();

                    lblStatus.Text = "Rule added.";
                }
                else
                {
                    using var cmd = new MySqlCommand(@"
UPDATE inv_age_discount_rules SET
    rule_name=@name,
    main_category=@cat,
    sub_category=@sub,
    gender=@gender,
    item_code=@code,
    min_age_months=@age,
    discount_percent=@disc,
    staff_only=@staffOnly,
    is_active=@active
WHERE id=@id;", conn);

                    cmd.Parameters.AddWithValue("@id", editingRuleId);
                    cmd.Parameters.AddWithValue("@name", ruleName);
                    cmd.Parameters.AddWithValue("@cat", string.IsNullOrWhiteSpace(category) ? DBNull.Value : category);
                    cmd.Parameters.AddWithValue("@sub", string.IsNullOrWhiteSpace(subCategory) ? DBNull.Value : subCategory);
                    cmd.Parameters.AddWithValue("@gender", string.IsNullOrWhiteSpace(gender) ? DBNull.Value : gender);
                    cmd.Parameters.AddWithValue("@code", string.IsNullOrWhiteSpace(itemCode) ? DBNull.Value : itemCode);
                    cmd.Parameters.AddWithValue("@age", ageMonths);
                    cmd.Parameters.AddWithValue("@disc", discountPercent);
                    cmd.Parameters.AddWithValue("@staffOnly", staffOnly);
                    cmd.Parameters.AddWithValue("@active", isActive);
                    cmd.ExecuteNonQuery();

                    lblStatus.Text = $"Rule #{editingRuleId} updated.";
                }

                Reload();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save rule.\n" + ex.Message);
            }
        }

        private void DeleteSelected()
        {
            if (dgvRules.CurrentRow == null)
                return;

            object idObj = dgvRules.CurrentRow.Cells["id"]?.Value ?? "";
            if (!int.TryParse(idObj.ToString(), out int id) || id <= 0)
            {
                dgvRules.Rows.Remove(dgvRules.CurrentRow);
                return;
            }

            if (MessageBox.Show("Delete selected rule?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureAgeDiscountSchema(conn);

                using var cmd = new MySqlCommand("DELETE FROM inv_age_discount_rules WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                Reload();
                if (editingRuleId == id)
                    ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete rule.\n" + ex.Message);
            }
        }

        private void ToggleActiveFromGrid(int rowIndex)
        {
            try
            {
                if (dgvRules.Rows.Count <= rowIndex)
                    return;

                object idObj = dgvRules.Rows[rowIndex].Cells["id"]?.Value ?? "";
                if (!int.TryParse(idObj.ToString(), out int id) || id <= 0)
                    return;

                object activeObj = dgvRules.Rows[rowIndex].Cells["is_active"]?.Value ?? 0;
                int activeInt;
                if (activeObj is bool b)
                    activeInt = b ? 1 : 0;
                else
                    activeInt = activeObj.ToString() == "1" ? 1 : 0;

                using var conn = DB.GetConnection();
                conn.Open();
                DB.EnsureAgeDiscountSchema(conn);

                using var cmd = new MySqlCommand("UPDATE inv_age_discount_rules SET is_active=@a WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@a", activeInt);
                cmd.ExecuteNonQuery();

                lblStatus.Text = $"Rule #{id} active = {activeInt}";
                Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update Active flag.\n" + ex.Message);
            }
        }
    }
}
