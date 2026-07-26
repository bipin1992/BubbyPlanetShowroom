using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Profile : UserControl
    {
        private static readonly Color HeaderBg = Color.FromArgb(30, 41, 59);
        private static readonly Color PageBg = Color.FromArgb(241, 245, 249);
        private static readonly Color Slate = Color.FromArgb(15, 23, 42);
        private static readonly Color Teal = Color.FromArgb(13, 148, 136);
        private static readonly Color Sky = Color.FromArgb(14, 165, 233);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color FieldBg = Color.FromArgb(248, 250, 252);
        private static readonly Color FieldBorder = Color.FromArgb(226, 232, 240);

        private readonly TextBox txtUsername = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly TextBox txtRole = new TextBox();
        private readonly TextBox txtFullName = new TextBox();
        private readonly TextBox txtPhone = new TextBox();
        private readonly DateTimePicker dtJoiningDate = new DateTimePicker();
        private readonly TextBox txtStatus = new TextBox();
        private readonly TextBox txtAadhar = new TextBox();
        private readonly TextBox txtAddress = new TextBox();
        private readonly PictureBox picPhoto = new PictureBox();
        private readonly Button btnUpload = new Button();
        private readonly Button btnSave = new Button();
        private readonly Button btnRefreshToday = new Button();
        private readonly Label lblTodayOrders = new Label();
        private readonly Label lblTodaySale = new Label();
        private readonly Label lblHeaderUser = new Label();
        private DataGridView dgvOrders = null!;

        private int userId;
        private byte[]? photoData;

        public Profile()
        {
            InitializeUI();
            LoadProfile();
            LoadTodayOrders();
            VisibleChanged += (_, _) =>
            {
                if (Visible)
                    LoadTodayOrders();
            };
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
                RowCount = 4,
                BackColor = PageBg,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));   // header
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 330f));  // profile card
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));  // stats
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // orders
            Controls.Add(main);

            main.Controls.Add(BuildPageHeader(), 0, 0);
            main.Controls.Add(BuildProfileCard(), 0, 1);
            main.Controls.Add(BuildStatsRow(), 0, 2);
            main.Controls.Add(BuildOrdersPanel(), 0, 3);
        }

        private Panel BuildPageHeader()
        {
            Panel header = CreateCard();
            header.Margin = new Padding(0, 0, 0, 10);
            header.Padding = new Padding(0);
            header.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using LinearGradientBrush brush = new LinearGradientBrush(
                    header.ClientRectangle, Teal, Sky, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brush, header.ClientRectangle);

                using Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "Profile", titleFont,
                    new Rectangle(18, 10, 220, 28), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                using Font hintFont = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(
                    e.Graphics,
                    "Manage your account  ·  view today's billing",
                    hintFont,
                    new Rectangle(18, 40, 420, 20),
                    Color.FromArgb(204, 251, 241),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };

            lblHeaderUser.Dock = DockStyle.Right;
            lblHeaderUser.Width = 260;
            lblHeaderUser.ForeColor = Color.White;
            lblHeaderUser.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            lblHeaderUser.TextAlign = ContentAlignment.MiddleRight;
            lblHeaderUser.Padding = new Padding(0, 0, 18, 0);
            lblHeaderUser.BackColor = Color.Transparent;
            header.Controls.Add(lblHeaderUser);
            return header;
        }

        private Panel BuildProfileCard()
        {
            Panel card = CreateCard();
            card.Margin = new Padding(0, 0, 0, 8);
            card.Padding = new Padding(0);

            Panel sectionBar = CreateSectionHeader("ACCOUNT", "Update personal details");
            Panel body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 10)
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));

            Panel col1 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Panel col2 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Panel col3 = BuildPhotoPanel();

            int y = 2;
            AddField(col1, "USERNAME", txtUsername, 0, y, readOnly: true);
            y += 52;
            AddField(col1, "ROLE", txtRole, 0, y, readOnly: true);
            y += 52;
            AddField(col1, "PHONE", txtPhone, 0, y);
            y += 52;
            AddDateField(col1, "JOINING DATE", dtJoiningDate, 0, y);

            y = 2;
            AddField(col2, "PASSWORD", txtPassword, 0, y);
            txtPassword.UseSystemPasswordChar = true;
            y += 52;
            AddField(col2, "FULL NAME", txtFullName, 0, y);
            y += 52;
            AddField(col2, "STATUS", txtStatus, 0, y, readOnly: true);
            y += 52;
            AddField(col2, "ADDRESS", txtAddress, 0, y);

            txtPhone.KeyPress += OnlyNumber_KeyPress;

            layout.Controls.Add(col1, 0, 0);
            layout.Controls.Add(col2, 1, 0);
            layout.Controls.Add(col3, 2, 0);
            body.Controls.Add(layout);

            card.Controls.Add(body);
            card.Controls.Add(sectionBar);
            return card;
        }

        private Panel BuildPhotoPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(8, 0, 0, 0)
            };
            panel.Paint += (_, e) =>
            {
                using Pen pen = new Pen(FieldBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            Label photoCap = new Label
            {
                Text = "PHOTO",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted,
                Location = new Point(12, 8),
                AutoSize = true
            };

            picPhoto.Location = new Point(12, 28);
            picPhoto.Size = new Size(108, 108);
            picPhoto.BackColor = Color.White;
            picPhoto.BorderStyle = BorderStyle.None;
            picPhoto.SizeMode = PictureBoxSizeMode.Zoom;
            picPhoto.Paint += (_, e) =>
            {
                using Pen pen = new Pen(FieldBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, picPhoto.Width - 1, picPhoto.Height - 1);
            };

            btnUpload.Text = "Upload Photo";
            btnUpload.Location = new Point(128, 28);
            btnUpload.Size = new Size(120, 32);
            StyleSecondaryButton(btnUpload);
            btnUpload.Click += BtnUpload_Click;

            btnSave.Text = "Save Profile";
            btnSave.Location = new Point(128, 68);
            btnSave.Size = new Size(120, 34);
            StylePrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;

            Label aadharCap = new Label
            {
                Text = "AADHAR",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted,
                Location = new Point(12, 146),
                AutoSize = true
            };
            txtAadhar.Location = new Point(12, 164);
            txtAadhar.Size = new Size(236, 28);
            StyleTextBox(txtAadhar, readOnly: true);

            panel.Controls.Add(photoCap);
            panel.Controls.Add(picPhoto);
            panel.Controls.Add(btnUpload);
            panel.Controls.Add(btnSave);
            panel.Controls.Add(aadharCap);
            panel.Controls.Add(txtAadhar);
            return panel;
        }

        private Panel BuildStatsRow()
        {
            Panel wrap = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBg,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(0)
            };

            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = PageBg
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));

            lblTodayOrders.Text = "0";
            lblTodaySale.Text = "₹0.00";

            Panel ordersStat = CreateStatCard("TODAY'S ORDERS", lblTodayOrders, Color.FromArgb(186, 230, 253), Sky);
            Panel saleStat = CreateStatCard("TODAY'S SALE", lblTodaySale, Color.FromArgb(167, 243, 208), Teal);

            Panel actionCard = CreateCard();
            actionCard.Margin = new Padding(0);
            actionCard.Padding = new Padding(14, 12, 14, 12);
            actionCard.BackColor = Color.White;

            Label actionCap = new Label
            {
                Text = "TODAY ONLY",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted,
                Dock = DockStyle.Top,
                Height = 18
            };
            Label actionHint = new Label
            {
                Text = "Bills under your login",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Slate,
                Dock = DockStyle.Top,
                Height = 24
            };

            btnRefreshToday.Text = "Refresh";
            btnRefreshToday.Dock = DockStyle.Right;
            btnRefreshToday.Width = 110;
            btnRefreshToday.Height = 34;
            StylePrimaryButton(btnRefreshToday);
            btnRefreshToday.BackColor = Teal;
            btnRefreshToday.Click += (_, _) => LoadTodayOrders();

            Panel btnHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            btnHost.Controls.Add(btnRefreshToday);

            actionCard.Controls.Add(btnHost);
            actionCard.Controls.Add(actionHint);
            actionCard.Controls.Add(actionCap);

            row.Controls.Add(ordersStat, 0, 0);
            row.Controls.Add(saleStat, 1, 0);
            row.Controls.Add(actionCard, 2, 0);
            wrap.Controls.Add(row);
            return wrap;
        }

        private Panel BuildOrdersPanel()
        {
            Panel ordersCard = CreateCard();
            ordersCard.Margin = new Padding(0);
            ordersCard.Padding = new Padding(1);

            dgvOrders = CreateOrdersGrid();
            dgvOrders.Columns.Add("OrderId", "Order ID");
            dgvOrders.Columns.Add("Date", "Date");
            dgvOrders.Columns.Add("Customer", "Customer");
            dgvOrders.Columns.Add("Mobile", "Mobile");
            dgvOrders.Columns.Add("Payment", "Payment");
            dgvOrders.Columns.Add("User", "User");
            dgvOrders.Columns.Add("Amount", "Amount");

            Panel ordersHeaderBar = CreateSectionHeader(
                "ORDERS",
                "Today's bills created by your login");

            ordersCard.Controls.Add(dgvOrders);
            ordersCard.Controls.Add(ordersHeaderBar);
            return ordersCard;
        }

        private static Panel CreateCard()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }

        private static Panel CreateSectionHeader(string title, string hint)
        {
            Panel bar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = HeaderBg,
                Padding = new Padding(12, 0, 12, 0)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Left,
                Width = 110,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblHint = new Label
            {
                Text = hint,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleRight
            };

            bar.Controls.Add(lblHint);
            bar.Controls.Add(lblTitle);
            return bar;
        }

        private static Panel CreateStatCard(string caption, Label valueLabel, Color bg, Color accent)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bg,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(14, 12, 14, 12)
            };
            card.Paint += (_, e) =>
            {
                using SolidBrush bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, 0, 0, 4, card.Height);
            };

            Label lblCap = new Label
            {
                Text = caption,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Muted,
                BackColor = Color.Transparent
            };

            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            valueLabel.ForeColor = Slate;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.BackColor = Color.Transparent;

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblCap);
            return card;
        }

        private static DataGridView CreateOrdersGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(226, 232, 240),
                RowTemplate = { Height = 34 },
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.25f);
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(204, 251, 241);
            grid.DefaultCellStyle.SelectionForeColor = Slate;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return grid;
        }

        private void AddField(Panel parent, string labelText, TextBox textBox, int x, int y, bool readOnly = false)
        {
            Label label = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted
            };

            textBox.Location = new Point(x, y + 18);
            textBox.Width = Math.Max(220, parent.ClientSize.Width - 16);
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            StyleTextBox(textBox, readOnly);

            parent.Controls.Add(label);
            parent.Controls.Add(textBox);

            parent.Resize += (_, _) =>
            {
                textBox.Width = Math.Max(180, parent.ClientSize.Width - 16);
            };
        }

        private void AddDateField(Panel parent, string labelText, DateTimePicker picker, int x, int y)
        {
            Label label = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted
            };

            picker.Location = new Point(x, y + 18);
            picker.Width = Math.Max(220, parent.ClientSize.Width - 16);
            picker.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            picker.Enabled = false;
            picker.Format = DateTimePickerFormat.Short;
            picker.Font = new Font("Segoe UI", 9.5f);
            picker.CalendarMonthBackground = FieldBg;

            parent.Controls.Add(label);
            parent.Controls.Add(picker);

            parent.Resize += (_, _) =>
            {
                picker.Width = Math.Max(180, parent.ClientSize.Width - 16);
            };
        }

        private static void StyleTextBox(TextBox textBox, bool readOnly)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 9.5f);
            textBox.Height = 28;
            textBox.ReadOnly = readOnly;
            textBox.BackColor = readOnly ? Color.FromArgb(241, 245, 249) : Color.White;
            textBox.ForeColor = Slate;
        }

        private static void StylePrimaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(37, 99, 235);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        private static void StyleSecondaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = FieldBorder;
            btn.FlatAppearance.BorderSize = 1;
            btn.BackColor = Color.White;
            btn.ForeColor = Slate;
            btn.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        private void LoadTodayOrders()
        {
            dgvOrders.Rows.Clear();
            lblTodayOrders.Text = "0";
            lblTodaySale.Text = "₹0.00";

            string user = LoginForm.LoggedInUser?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(user))
                return;

            try
            {
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();

                using MySqlCommand cmd = new MySqlCommand(@"
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
                    LEFT JOIN inv_customers c ON c.id = o.customer_id
                    WHERE o.created_by = @user
                      AND DATE(o.date_added) = CURDATE()
                    ORDER BY o.id DESC", conn);
                cmd.Parameters.AddWithValue("@user", user);

                decimal totalSale = 0;
                int orderCount = 0;

                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string customer =
                        ((reader["first_name"]?.ToString() ?? "") + " " +
                         (reader["sur_name"]?.ToString() ?? "")).Trim();
                    if (string.IsNullOrWhiteSpace(customer))
                        customer = "Walk-in Customer";

                    decimal amount = Convert.ToDecimal(reader["grand_total"]);
                    totalSale += amount;
                    orderCount++;

                    dgvOrders.Rows.Add(
                        reader["id"],
                        Convert.ToDateTime(reader["date_added"]).ToString("dd-MM-yyyy HH:mm"),
                        customer,
                        reader["phone"]?.ToString() ?? "",
                        reader["payment_method"]?.ToString() ?? "",
                        reader["created_by"]?.ToString() ?? "",
                        amount.ToString("0.00"));
                }

                lblTodayOrders.Text = orderCount.ToString();
                lblTodaySale.Text = "₹" + totalSale.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load today's orders: " + ex.Message);
            }
        }

        private void LoadProfile()
        {
            if (string.IsNullOrWhiteSpace(LoginForm.LoggedInUser))
            {
                MessageBox.Show("Please login first");
                return;
            }

            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();

                string q = @"SELECT id, username, password, role, full_name, phone,
                            joining_date, status, aadhar, address, photo
                            FROM inv_users
                            WHERE username=@u
                            LIMIT 1";

                using MySqlCommand cmd = new MySqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@u", LoginForm.LoggedInUser);

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    MessageBox.Show("Profile not found");
                    return;
                }

                userId = Convert.ToInt32(reader["id"]);
                txtUsername.Text = reader["username"]?.ToString() ?? "";
                txtPassword.Text = reader["password"]?.ToString() ?? "";
                txtRole.Text = reader["role"]?.ToString() ?? "";
                txtFullName.Text = reader["full_name"]?.ToString() ?? "";
                txtPhone.Text = reader["phone"]?.ToString() ?? "";

                string statusRaw = reader["status"]?.ToString() ?? "";
                txtStatus.Text = statusRaw == "1" ? "Active" : (statusRaw == "0" ? "Inactive" : statusRaw);

                txtAadhar.Text = reader["aadhar"]?.ToString() ?? "";
                txtAddress.Text = reader["address"]?.ToString() ?? "";
                lblHeaderUser.Text = txtFullName.Text.Trim().Length > 0
                    ? txtFullName.Text.Trim()
                    : txtUsername.Text;

                if (reader["joining_date"] != DBNull.Value)
                    dtJoiningDate.Value = Convert.ToDateTime(reader["joining_date"]);

                if (reader["photo"] != DBNull.Value)
                {
                    photoData = (byte[])reader["photo"];
                    using MemoryStream ms = new MemoryStream(photoData);
                    picPhoto.Image = Image.FromStream(ms);
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (userId == 0)
            {
                MessageBox.Show("Profile not loaded");
                return;
            }

            if (!ValidateInput()) return;

            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();

                string q = @"UPDATE inv_users SET
                            password=@p,
                            full_name=@n,
                            phone=@ph,
                            address=@ad,
                            photo=@img
                            WHERE id=@id";

                using MySqlCommand cmd = new MySqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@p", txtPassword.Text.Trim());
                cmd.Parameters.AddWithValue("@n", txtFullName.Text.Trim());
                cmd.Parameters.AddWithValue("@ph", txtPhone.Text.Trim());
                cmd.Parameters.AddWithValue("@ad", txtAddress.Text.Trim());
                cmd.Parameters.AddWithValue("@img", (object?)photoData ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", userId);

                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Profile updated");
            LoadProfile();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtPassword.Text)) return Show("Password required");
            if (string.IsNullOrWhiteSpace(txtFullName.Text)) return Show("Full name required");

            if (txtPhone.Text.Length != 10 || !txtPhone.Text.All(char.IsDigit))
                return Show("Invalid phone");

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
                return Show("Address required");

            return true;
        }

        private static bool Show(string msg)
        {
            MessageBox.Show(msg);
            return false;
        }

        private void BtnUpload_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.jpg;*.jpeg;*.png";

            if (ofd.ShowDialog() != DialogResult.OK) return;

            photoData = File.ReadAllBytes(ofd.FileName);
            picPhoto.Image = Image.FromFile(ofd.FileName);
        }

        private static void OnlyNumber_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }
    }
}
