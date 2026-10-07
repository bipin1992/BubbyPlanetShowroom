using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Users : UserControl
    {
        private static readonly Color PageBg = Color.FromArgb(241, 245, 249);
        private static readonly Color Slate = Color.FromArgb(15, 23, 42);
        private static readonly Color HeaderBg = Color.FromArgb(30, 41, 59);
        private static readonly Color Teal = Color.FromArgb(13, 148, 136);
        private static readonly Color Sky = Color.FromArgb(14, 165, 233);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);
        private static readonly Color Danger = Color.FromArgb(220, 38, 38);
        private static readonly Font RoleFont = new Font("Segoe UI", 9f, FontStyle.Bold);

        private readonly DataGridView grid = new DataGridView();
        private readonly TextBox txtUsername = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly ComboBox cmbRole = new ComboBox();
        private readonly TextBox txtName = new TextBox();
        private readonly TextBox txtPhone = new TextBox();
        private readonly TextBox txtSalary = new TextBox();
        private readonly TextBox txtAadhar = new TextBox();
        private readonly TextBox txtAddress = new TextBox();
        private readonly DateTimePicker dtJoin = new DateTimePicker();
        private readonly TextBox txtSearch = new TextBox();
        private readonly Button btnAdd = new Button();
        private readonly Button btnUpdate = new Button();
        private readonly Button btnDelete = new Button();
        private readonly Button btnClear = new Button();

        private Label lblCount = null!;
        private Label lblHint = null!;
        private DataTable? usersTable;
        private int selectedId;

        public Users()
        {
            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            Dock = DockStyle.Fill;
            BackColor = PageBg;
            Padding = new Padding(12);

            TableLayoutPanel main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = PageBg
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 188f));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(main);

            main.Controls.Add(BuildHeader(), 0, 0);
            main.Controls.Add(BuildFormCard(), 0, 1);
            main.Controls.Add(BuildGridCard(), 0, 2);
        }

        private Panel BuildHeader()
        {
            Panel header = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
            header.Paint += (_, e) =>
            {
                Rectangle bounds = header.ClientRectangle;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using LinearGradientBrush brush = new LinearGradientBrush(bounds, Teal, Sky, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brush, bounds);
                using Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "Users", titleFont,
                    new Rectangle(16, 10, 360, 28), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                using Font hintFont = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(
                    e.Graphics,
                    "Staff accounts  ·  role, contact and joining details",
                    hintFont,
                    new Rectangle(16, 38, 640, 20),
                    Color.FromArgb(204, 251, 241),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
            return header;
        }

        private Panel BuildFormCard()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(14, 10, 14, 8)
            };

            txtUsername.MaxLength = 40;
            txtPassword.MaxLength = 40;
            txtPassword.UseSystemPasswordChar = true;
            txtName.MaxLength = 80;
            txtPhone.MaxLength = 10;
            txtAadhar.MaxLength = 12;
            txtSalary.MaxLength = 12;
            txtAddress.MaxLength = 200;

            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.Items.AddRange(new object[] { "Master Admin", "Admin", "Cashier", "Staff" });

            dtJoin.Format = DateTimePickerFormat.Custom;
            dtJoin.CustomFormat = "dd-MM-yyyy";

            txtPhone.KeyPress += OnlyNumber_KeyPress;
            txtAadhar.KeyPress += OnlyNumber_KeyPress;
            txtSalary.KeyPress += OnlyDecimal_KeyPress;

            TableLayoutPanel fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                BackColor = Color.White
            };
            for (int i = 0; i < 5; i++)
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            fields.Controls.Add(Field("USERNAME", txtUsername), 0, 0);
            fields.Controls.Add(Field("PASSWORD", txtPassword), 1, 0);
            fields.Controls.Add(Field("ROLE", cmbRole), 2, 0);
            fields.Controls.Add(Field("FULL NAME", txtName), 3, 0);
            fields.Controls.Add(Field("PHONE", txtPhone), 4, 0);
            fields.Controls.Add(Field("SALARY", txtSalary), 0, 1);
            fields.Controls.Add(Field("JOINING DATE", dtJoin), 1, 1);
            fields.Controls.Add(Field("AADHAR", txtAadhar), 2, 1);
            Panel address = Field("ADDRESS", txtAddress);
            fields.Controls.Add(address, 3, 1);
            fields.SetColumnSpan(address, 2);

            Panel actions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.White
            };
            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 480,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(0, 4, 0, 0)
            };
            buttons.Controls.Add(StyleButton(btnAdd, "Add", PrimaryBlue, Color.White, false));
            buttons.Controls.Add(StyleButton(btnUpdate, "Update", Teal, Color.White, false));
            buttons.Controls.Add(StyleButton(btnDelete, "Delete", Danger, Color.White, false));
            buttons.Controls.Add(StyleButton(btnClear, "Clear", Color.White, Slate, true));
            actions.Controls.Add(buttons);

            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClear.Click += (_, _) => ClearFields();

            card.Controls.Add(fields);
            card.Controls.Add(actions);
            return card;
        }

        private Panel BuildGridCard()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(1)
            };

            Panel bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = HeaderBg,
                Padding = new Padding(12, 0, 8, 0)
            };
            Label staffLabel = new Label
            {
                Text = "STAFF",
                Dock = DockStyle.Left,
                Width = 64,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblCount = new Label
            {
                Text = "0 users",
                Dock = DockStyle.Left,
                Width = 90,
                ForeColor = Color.FromArgb(186, 230, 253),
                Font = new Font("Segoe UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblHint = new Label
            {
                Text = "Select a row to edit",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Font = new Font("Segoe UI", 9.5f);
            txtSearch.PlaceholderText = "Search name, phone, role";
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.TextChanged += (_, _) => ApplySearch();
            Panel searchWrap = new Panel
            {
                Dock = DockStyle.Right,
                Width = 240,
                BackColor = HeaderBg,
                Padding = new Padding(0, 6, 4, 6)
            };
            searchWrap.Controls.Add(txtSearch);

            // Fill is added first so it docks last. Left controls are added
            // from the inside outward. Search is added last and docks first.
            bar.Controls.Add(lblHint);
            bar.Controls.Add(lblCount);
            bar.Controls.Add(staffLabel);
            bar.Controls.Add(searchWrap);

            StyleGrid();
            grid.CellClick += Grid_CellClick;
            grid.DataError += (_, e) => { e.ThrowException = false; };
            grid.CellFormatting += Grid_CellFormatting;

            card.Controls.Add(grid);
            card.Controls.Add(bar);
            return card;
        }

        private void StyleGrid()
        {
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(226, 232, 240);
            grid.ScrollBars = ScrollBars.Vertical;
            grid.RowTemplate.Height = 32;
            grid.ColumnHeadersHeight = 34;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            grid.DefaultCellStyle.ForeColor = Slate;
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        }

        private void StyleGridColumns()
        {
            HideColumn("id");
            HideColumn("password");
            HideColumn("status");
            HideColumn("photo");

            SetColumn("full_name", "Full Name", 18);
            SetColumn("username", "Username", 12);
            SetColumn("role", "Role", 12);
            SetColumn("phone", "Phone", 12);
            SetColumn("salary", "Salary", 10);
            SetColumn("joining_date", "Joined", 11);
            SetColumn("aadhar", "Aadhar", 13);
            SetColumn("address", "Address", 20);

            if (grid.Columns.Contains("salary"))
                grid.Columns["salary"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (grid.Columns.Contains("joining_date"))
                grid.Columns["joining_date"].DefaultCellStyle.Format = "dd-MM-yyyy";

            string[] order = { "full_name", "username", "role", "phone", "salary", "joining_date", "aadhar", "address" };
            for (int i = 0; i < order.Length; i++)
            {
                if (grid.Columns.Contains(order[i]))
                    grid.Columns[order[i]].DisplayIndex = i;
            }
        }

        private void HideColumn(string name)
        {
            if (grid.Columns.Contains(name))
                grid.Columns[name].Visible = false;
        }

        private void SetColumn(string name, string header, float weight)
        {
            if (!grid.Columns.Contains(name))
                return;
            grid.Columns[name].HeaderText = header;
            grid.Columns[name].FillWeight = weight;
            grid.Columns[name].MinimumWidth = 70;
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            string name = grid.Columns[e.ColumnIndex].Name;
            if (name == "salary" && e.Value != null && e.Value != DBNull.Value
                && decimal.TryParse(e.Value.ToString(), out decimal salary))
            {
                e.Value = "₹" + salary.ToString("N2");
                e.FormattingApplied = true;
            }
            else if (name == "role" && e.Value != null)
            {
                string role = e.Value.ToString() ?? "";
                e.CellStyle.Font = RoleFont;
                e.CellStyle.ForeColor = role switch
                {
                    "Master Admin" => Color.FromArgb(109, 40, 217),
                    "Admin" => Color.FromArgb(29, 78, 216),
                    "Cashier" => Color.FromArgb(15, 118, 110),
                    _ => Color.FromArgb(71, 85, 105)
                };
            }
        }

        private static Panel Field(string caption, Control input)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 12, 4),
                BackColor = Color.White
            };
            input.Font = new Font("Segoe UI", 9.5f);
            input.Dock = DockStyle.Top;
            input.Height = 28;
            panel.Controls.Add(input);
            panel.Controls.Add(new Label
            {
                Text = caption,
                Dock = DockStyle.Top,
                Height = 16,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted
            });
            return panel;
        }

        private static Button StyleButton(Button button, string text, Color back, Color fore, bool bordered)
        {
            button.Text = text;
            button.Size = new Size(104, 30);
            button.Margin = new Padding(0, 0, 8, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = back;
            button.ForeColor = fore;
            button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderSize = bordered ? 1 : 0;
            if (bordered)
                button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            return button;
        }

        private void LoadData()
        {
            using MySqlConnection conn = DB.GetConnection();
            conn.Open();
            MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM inv_users WHERE status=1 ORDER BY id DESC", conn);
            DataTable dt = new DataTable();
            da.Fill(dt);
            usersTable = dt;
            grid.DataSource = usersTable;
            StyleGridColumns();
            ApplySearch();
        }

        private void ApplySearch()
        {
            if (usersTable == null)
                return;

            string text = txtSearch.Text.Trim();
            if (text.Length == 0)
            {
                usersTable.DefaultView.RowFilter = "";
            }
            else
            {
                string esc = text.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
                usersTable.DefaultView.RowFilter =
                    "username LIKE '%" + esc + "%' OR full_name LIKE '%" + esc + "%' OR phone LIKE '%" + esc
                    + "%' OR role LIKE '%" + esc + "%' OR address LIKE '%" + esc + "%'";
            }

            int count = usersTable.DefaultView.Count;
            lblCount.Text = count.ToString("N0") + (count == 1 ? " user" : " users");
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            if (IsAadharExists(txtAadhar.Text))
            {
                MessageBox.Show("This Aadhar is already used.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using MySqlConnection conn = DB.GetConnection();
            conn.Open();

            string q = @"INSERT INTO inv_users
                (username,password,role,full_name,phone,salary,joining_date,aadhar,address,status)
                VALUES(@u,@p,@r,@n,@ph,@s,@j,@a,@ad,1)";

            MySqlCommand cmd = new MySqlCommand(q, conn);
            BindUser(cmd);
            cmd.ExecuteNonQuery();

            MessageBox.Show("User added.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
            ClearFields();
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (selectedId == 0)
            {
                MessageBox.Show("Select a user first.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateInput()) return;

            if (IsAadharExists(txtAadhar.Text, selectedId))
            {
                MessageBox.Show("This Aadhar is already used.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using MySqlConnection conn = DB.GetConnection();
            conn.Open();

            string q = @"UPDATE inv_users SET
                username=@u,password=@p,role=@r,full_name=@n,
                phone=@ph,salary=@s,joining_date=@j,
                aadhar=@a,address=@ad
                WHERE id=@id";

            MySqlCommand cmd = new MySqlCommand(q, conn);
            BindUser(cmd);
            cmd.Parameters.AddWithValue("@id", selectedId);
            cmd.ExecuteNonQuery();

            MessageBox.Show("User updated.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
            ClearFields();
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (selectedId == 0)
            {
                MessageBox.Show("Select a user first.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Remove this user?", "Users", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            using MySqlConnection conn = DB.GetConnection();
            conn.Open();

            MySqlCommand cmd = new MySqlCommand("UPDATE inv_users SET status=0 WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", selectedId);
            cmd.ExecuteNonQuery();

            MessageBox.Show("User removed.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
            ClearFields();
        }

        private void BindUser(MySqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@u", txtUsername.Text.Trim());
            cmd.Parameters.AddWithValue("@p", txtPassword.Text);
            cmd.Parameters.AddWithValue("@r", cmbRole.Text);
            cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
            cmd.Parameters.AddWithValue("@ph", txtPhone.Text.Trim());
            cmd.Parameters.AddWithValue("@s", txtSalary.Text.Trim());
            cmd.Parameters.AddWithValue("@j", dtJoin.Value.Date);
            cmd.Parameters.AddWithValue("@a", txtAadhar.Text.Trim());
            cmd.Parameters.AddWithValue("@ad", txtAddress.Text.Trim());
        }

        private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = grid.Rows[e.RowIndex];
            if (row.Cells["id"].Value == null || row.Cells["id"].Value == DBNull.Value)
                return;

            selectedId = Convert.ToInt32(row.Cells["id"].Value);

            txtUsername.Text = row.Cells["username"]?.Value?.ToString() ?? "";
            txtPassword.Text = row.Cells["password"]?.Value?.ToString() ?? "";
            SelectRole(row.Cells["role"]?.Value?.ToString() ?? "");
            txtName.Text = row.Cells["full_name"]?.Value?.ToString() ?? "";
            txtPhone.Text = row.Cells["phone"]?.Value?.ToString() ?? "";
            txtSalary.Text = row.Cells["salary"]?.Value?.ToString() ?? "";
            txtAadhar.Text = row.Cells["aadhar"]?.Value?.ToString() ?? "";
            txtAddress.Text = row.Cells["address"]?.Value?.ToString() ?? "";

            if (row.Cells["joining_date"].Value != null && row.Cells["joining_date"].Value != DBNull.Value)
                dtJoin.Value = Convert.ToDateTime(row.Cells["joining_date"].Value);

            string name = txtName.Text.Trim();
            lblHint.Text = name.Length == 0 ? "Editing selected user" : "Editing  " + name;
        }

        private void SelectRole(string role)
        {
            int index = cmbRole.FindStringExact(role);
            if (index < 0 && role.Length > 0)
            {
                cmbRole.Items.Add(role);
                index = cmbRole.Items.Count - 1;
            }
            cmbRole.SelectedIndex = index;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text)) return Show("Username required");
            if (string.IsNullOrWhiteSpace(txtPassword.Text)) return Show("Password required");
            if (cmbRole.SelectedIndex == -1) return Show("Select role");
            if (string.IsNullOrWhiteSpace(txtName.Text)) return Show("Name required");

            if (txtPhone.Text.Length != 10 || !txtPhone.Text.All(char.IsDigit))
                return Show("Invalid phone");

            if (txtAadhar.Text.Length != 12 || !txtAadhar.Text.All(char.IsDigit))
                return Show("Invalid Aadhar");

            if (!decimal.TryParse(txtSalary.Text, out decimal sal) || sal < 0)
                return Show("Invalid salary");

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
                return Show("Address required");

            return true;
        }

        private static bool Show(string msg)
        {
            MessageBox.Show(msg, "Users", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void ClearFields()
        {
            txtUsername.Clear();
            txtPassword.Clear();
            cmbRole.SelectedIndex = -1;
            txtName.Clear();
            txtPhone.Clear();
            txtSalary.Clear();
            txtAadhar.Clear();
            txtAddress.Clear();
            dtJoin.Value = DateTime.Today;
            selectedId = 0;
            lblHint.Text = "Select a row to edit";
            grid.ClearSelection();
        }

        private static bool IsAadharExists(string aadhar, int excludeId = 0)
        {
            using MySqlConnection conn = DB.GetConnection();
            conn.Open();

            MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM inv_users WHERE aadhar=@a AND id!=@id", conn);
            cmd.Parameters.AddWithValue("@a", aadhar);
            cmd.Parameters.AddWithValue("@id", excludeId);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void OnlyNumber_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private static void OnlyDecimal_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                e.Handled = true;

            if (e.KeyChar == '.' && tb.Text.Contains('.'))
                e.Handled = true;
        }
    }
}
