using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public partial class LoginForm : Form
    {
        // Kids-retail palette: teal + sky + coral (not purple / dark-mode)
        private static readonly Color TealDeep = Color.FromArgb(13, 148, 136);
        private static readonly Color Teal = Color.FromArgb(20, 184, 166);
        private static readonly Color Sky = Color.FromArgb(56, 189, 248);
        private static readonly Color Coral = Color.FromArgb(251, 113, 133);
        private static readonly Color CoralHover = Color.FromArgb(244, 63, 94);
        private static readonly Color Ink = Color.FromArgb(15, 23, 42);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color FieldBg = Color.FromArgb(248, 250, 252);
        private static readonly Color FieldFocus = Color.FromArgb(240, 253, 250);

        public static string LoggedInUser = "";
        public string SelectedRole { get; private set; } = "";

        private Panel brandPanel = new Panel();
        private Panel formPanel = new Panel();
        private Panel cardPanel = new Panel();
        private Label labelTagline = new Label();
        private Label labelBranch = new Label();
        private Label labelSubtitle = new Label();
        private Label label1 = new Label();
        private Label label2 = new Label();
        private Label label3 = new Label();
        private TextBox txtUsername = new TextBox();
        private TextBox txtPassword = new TextBox();
        private ComboBox cmbRole = new ComboBox();
        private Button btnLogin = new Button();
        private CheckBox chkShowPassword = new CheckBox();
        private Panel usernameWrap = new Panel();
        private Panel passwordWrap = new Panel();
        private Panel roleWrap = new Panel();

        public LoginForm()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AcceptButton = btnLogin;

            Load += LoginForm_Load;
            btnLogin.MouseEnter += (_, _) => btnLogin.BackColor = CoralHover;
            btnLogin.MouseLeave += (_, _) => btnLogin.BackColor = Coral;
        }

        private void LoginForm_Load(object? sender, EventArgs e)
        {
            // Keep Login Type blank by default (no Master Admin pre-selected)
            cmbRole.SelectedIndex = -1;
            cmbRole.SelectedItem = null;

            CenterCard();
            MakeRounded(btnLogin, 10);
            txtUsername.Focus();
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string role = cmbRole.SelectedItem?.ToString() ?? "";

            //string username = "Bipin";
            //string password = "1234";
            //string role = "Master Admin";

            if (username == "" || password == "" || role == "")
            {
                MessageBox.Show("Please fill all fields (including Login Type).", "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (MySqlConnection conn = DB.GetConnection())
            {
                try
                {
                    conn.Open();

                    string query = "SELECT COUNT(*) FROM inv_users WHERE username=@u AND password=@p AND role=@r";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);
                    cmd.Parameters.AddWithValue("@r", role);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        LoggedInUser = username;
                        SelectedRole = role;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid username, password, or role.", "Login failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        ShakeForm();
                        txtPassword.SelectAll();
                        txtPassword.Focus();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database error: " + ex.Message, "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Bubbyplanet · Sign In";
            ClientSize = new Size(860, 520);
            BackColor = Color.FromArgb(224, 242, 254);
            Font = new Font("Segoe UI", 9.5f);
            DoubleBuffered = true;

            // ===== Left brand panel (brand text painted in top padding — never clipped) =====
            brandPanel.Dock = DockStyle.Left;
            brandPanel.Width = 360;
            brandPanel.Padding = new Padding(40, 120, 40, 36); // top space for two-tone brand
            SetDoubleBuffered(brandPanel);
            brandPanel.Paint += BrandPanel_Paint;

            labelTagline.Text = "Kids Showroom · POS & Inventory";
            labelTagline.Font = new Font("Segoe UI", 11f);
            labelTagline.ForeColor = Color.FromArgb(230, 255, 255);
            labelTagline.AutoSize = false;
            labelTagline.Dock = DockStyle.Top;
            labelTagline.Height = 28;
            labelTagline.BackColor = Color.Transparent;

            Panel chips = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 28, 0, 0)
            };
            chips.Controls.Add(MakeChip("Billing", Coral, 0, 8));
            chips.Controls.Add(MakeChip("Stock", Sky, 100, 8));
            chips.Controls.Add(MakeChip("Returns", Color.FromArgb(250, 204, 21), 200, 8));
            chips.Controls.Add(MakeChip("Reports", Color.FromArgb(167, 243, 208), 0, 52));
            chips.Controls.Add(MakeChip("Labels", Color.FromArgb(253, 186, 116), 110, 52));

            Label labelWelcome = new Label
            {
                Text = "Bright tools for a happy store.\nSign in to manage sales, stock & returns.",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            labelBranch.Text = "Daudnagar Branch  ·  Aurangabad, Bihar";
            labelBranch.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            labelBranch.ForeColor = Color.White;
            labelBranch.AutoSize = false;
            labelBranch.Dock = DockStyle.Bottom;
            labelBranch.Height = 28;
            labelBranch.BackColor = Color.Transparent;
            labelBranch.TextAlign = ContentAlignment.BottomLeft;

            brandPanel.Controls.Add(labelBranch);
            brandPanel.Controls.Add(chips);
            brandPanel.Controls.Add(labelWelcome);
            brandPanel.Controls.Add(labelTagline);

            // ===== Right side =====
            formPanel.Dock = DockStyle.Fill;
            formPanel.BackColor = Color.FromArgb(241, 245, 249);
            formPanel.Padding = new Padding(40, 36, 40, 36);

            cardPanel.Size = new Size(380, 430);
            cardPanel.Location = new Point(40, 40);
            cardPanel.BackColor = Color.White;
            cardPanel.Padding = new Padding(0);
            cardPanel.Anchor = AnchorStyles.None;
            SetDoubleBuffered(cardPanel);
            formPanel.Resize += (_, _) => CenterCard();

            // Accent strip as a real control (avoid Paint over labels → overlap)
            Panel accentStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 5,
                BackColor = Teal
            };
            accentStrip.Paint += (_, e) =>
            {
                using LinearGradientBrush line = new LinearGradientBrush(
                    accentStrip.ClientRectangle, Teal, Coral, LinearGradientMode.Horizontal);
                ColorBlend blend = new ColorBlend
                {
                    Colors = new[] { Teal, Sky, Coral },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                line.InterpolationColors = blend;
                e.Graphics.FillRectangle(line, accentStrip.ClientRectangle);
            };

            Panel cardBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(36, 24, 36, 28)
            };

            // Paint title once — Label was ghosting/overlapping
            Panel titlePanel = new Panel
            {
                Location = new Point(36, 18),
                Size = new Size(308, 42),
                BackColor = Color.White
            };
            SetDoubleBuffered(titlePanel);
            titlePanel.Paint += (_, e) =>
            {
                e.Graphics.Clear(Color.White);
                using Font titleFont = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Point);
                TextRenderer.DrawText(
                    e.Graphics,
                    "Welcome back",
                    titleFont,
                    titlePanel.ClientRectangle,
                    Ink,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            };

            labelSubtitle.Text = "Sign in to continue to your workspace";
            labelSubtitle.Font = new Font("Segoe UI", 9.5f);
            labelSubtitle.ForeColor = Muted;
            labelSubtitle.AutoSize = false;
            labelSubtitle.Location = new Point(36, 60);
            labelSubtitle.Size = new Size(308, 24);
            labelSubtitle.BackColor = Color.White;

            int x = 36;
            int w = 308;
            int y = 100;

            label1 = MakeFieldLabel("USERNAME", x, y);
            y += 22;
            usernameWrap = MakeFieldWrap(x, y, w, Teal);
            StyleTextBox(txtUsername, usernameWrap);
            txtUsername.PlaceholderText = "Enter username";
            txtUsername.TabIndex = 0;
            y += 56;

            label2 = MakeFieldLabel("PASSWORD", x, y);
            y += 22;
            passwordWrap = MakeFieldWrap(x, y, w, Sky);
            StyleTextBox(txtPassword, passwordWrap);
            txtPassword.PlaceholderText = "Enter password";
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TabIndex = 1;

            chkShowPassword.Text = "Show password";
            chkShowPassword.Font = new Font("Segoe UI", 9f);
            chkShowPassword.ForeColor = Muted;
            chkShowPassword.AutoSize = true;
            chkShowPassword.Location = new Point(x, y + 42);
            chkShowPassword.TabIndex = 2;
            chkShowPassword.Cursor = Cursors.Hand;
            chkShowPassword.BackColor = Color.White;
            chkShowPassword.CheckedChanged += chkShowPassword_CheckedChanged;
            y += 74;

            label3 = MakeFieldLabel("LOGIN TYPE", x, y);
            y += 22;
            roleWrap = MakeFieldWrap(x, y, w, Coral);
            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.Items.AddRange(new object[] { "Master Admin", "Admin", "Cashier", "Staff" });
            cmbRole.SelectedIndex = -1; // blank until user selects
            cmbRole.Dock = DockStyle.Fill;
            cmbRole.Font = new Font("Segoe UI", 10.5f);
            cmbRole.FlatStyle = FlatStyle.Flat;
            cmbRole.TabIndex = 3;
            roleWrap.Controls.Add(cmbRole);
            y += 56;

            btnLogin.Text = "Sign In";
            btnLogin.Location = new Point(x, y);
            btnLogin.Size = new Size(w, 46);
            btnLogin.BackColor = Coral;
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.TabIndex = 4;
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;

            cardBody.Controls.Add(titlePanel);
            cardBody.Controls.Add(labelSubtitle);
            cardBody.Controls.Add(label1);
            cardBody.Controls.Add(usernameWrap);
            cardBody.Controls.Add(label2);
            cardBody.Controls.Add(passwordWrap);
            cardBody.Controls.Add(chkShowPassword);
            cardBody.Controls.Add(label3);
            cardBody.Controls.Add(roleWrap);
            cardBody.Controls.Add(btnLogin);

            cardPanel.Controls.Add(cardBody);
            cardPanel.Controls.Add(accentStrip);

            formPanel.Controls.Add(cardPanel);
            Controls.Add(formPanel);
            Controls.Add(brandPanel);

            ResumeLayout(false);
        }

        private void CenterCard()
        {
            cardPanel.Left = Math.Max(24, (formPanel.ClientSize.Width - cardPanel.Width) / 2);
            cardPanel.Top = Math.Max(24, (formPanel.ClientSize.Height - cardPanel.Height) / 2);
        }

        private void BrandPanel_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = brandPanel.ClientRectangle;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                r,
                TealDeep,
                Color.FromArgb(14, 165, 233),
                LinearGradientMode.Vertical))
            {
                ColorBlend blend = new ColorBlend
                {
                    Colors = new[] { TealDeep, Teal, Color.FromArgb(14, 165, 233) },
                    Positions = new[] { 0f, 0.55f, 1f }
                };
                brush.InterpolationColors = blend;
                e.Graphics.FillRectangle(brush, r);
            }

            using (SolidBrush orb = new SolidBrush(Color.FromArgb(40, Color.White)))
            {
                e.Graphics.FillEllipse(orb, 220, -40, 200, 200);
                e.Graphics.FillEllipse(orb, -60, 280, 180, 180);
            }

            using (Pen accent = new Pen(Coral, 5))
                e.Graphics.DrawLine(accent, brandPanel.Width - 3, 50, brandPanel.Width - 3, brandPanel.Height - 50);

            // Two-tone brand mark (Bubby=blue, planet=green) — full "y" visible
            using Font brandFont = new Font("Segoe UI", 28f, FontStyle.Bold, GraphicsUnit.Point);
            BrandNameLabel.DrawBrand(e.Graphics, brandFont, new Point(40, 48));
        }

        private static Label MakeChip(string text, Color bg, int left, int top)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Ink,
                BackColor = bg,
                AutoSize = false,
                Size = new Size(90, 28),
                Location = new Point(left, top),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private Panel MakeFieldWrap(int x, int y, int width, Color accent)
        {
            Panel wrap = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, 40),
                BackColor = FieldBg,
                Padding = new Padding(10, 6, 10, 6)
            };

            wrap.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool focused = wrap.ContainsFocus;
                Color border = focused ? accent : Color.FromArgb(203, 213, 225);
                using Pen pen = new Pen(border, focused ? 2f : 1f);
                Rectangle rect = new Rectangle(1, 1, wrap.Width - 3, wrap.Height - 3);
                e.Graphics.DrawRectangle(pen, rect);
                if (focused)
                {
                    using Pen glow = new Pen(Color.FromArgb(60, accent), 3f);
                    e.Graphics.DrawRectangle(glow, rect);
                }
            };

            return wrap;
        }

        private static Label MakeFieldLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Muted,
                AutoSize = false,
                Location = new Point(x, y),
                Size = new Size(200, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };
        }

        private static void SetDoubleBuffered(Control control)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }

        private void StyleTextBox(TextBox box, Panel wrap)
        {
            box.BorderStyle = BorderStyle.None;
            box.Font = new Font("Segoe UI", 11f);
            box.BackColor = FieldBg;
            box.ForeColor = Ink;
            box.Dock = DockStyle.Fill;
            box.Enter += (_, _) =>
            {
                box.BackColor = FieldFocus;
                wrap.BackColor = FieldFocus;
                wrap.Invalidate();
            };
            box.Leave += (_, _) =>
            {
                box.BackColor = FieldBg;
                wrap.BackColor = FieldBg;
                wrap.Invalidate();
            };
            wrap.Controls.Add(box);
        }

        private void chkShowPassword_CheckedChanged(object? sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }

        private static void MakeRounded(Control ctrl, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            Rectangle r = new Rectangle(0, 0, ctrl.Width, ctrl.Height);
            int d = radius * 2;

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            ctrl.Region = new Region(path);
        }

        private void ShakeForm()
        {
            Point original = Location;

            for (int i = 0; i < 8; i++)
            {
                Location = new Point(original.X - 5, original.Y);
                System.Threading.Thread.Sleep(20);
                Location = new Point(original.X + 5, original.Y);
                System.Threading.Thread.Sleep(20);
            }

            Location = original;
        }
    }
}
