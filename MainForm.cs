
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    public class MainForm : Form
    {
        private static readonly Color HeaderDeep = Color.FromArgb(15, 118, 110);
        private static readonly Color HeaderMid = Color.FromArgb(13, 148, 136);
        private static readonly Color HeaderSky = Color.FromArgb(14, 165, 233);
        private static readonly Color Lime = Color.FromArgb(132, 204, 22);
        private static readonly Color Coral = Color.FromArgb(244, 63, 94);
        private static readonly Color LoginGreen = Color.FromArgb(34, 197, 94);

        public static string CurrentRole { get; private set; } = "";

        Panel header = new Panel();
        Panel leftMenu = new Panel();
        Panel content = new Panel();
        Panel userChip = new Panel();

        Label lblUser = new Label();
        Button btnLogin = new Button();
        Button btnLogout = new Button();

        Button activeButton = null;

        Dictionary<string, Button> menuButtons = new Dictionary<string, Button>();
        private Receipt? receiptPage;
        private Return? returnPage;
        private InternetConnectivityMonitor? internetMonitor;

        public MainForm()
        {
            this.Text = "Bubbyplanet Showroom Management";
            this.WindowState = FormWindowState.Maximized;

            InitializeLayout();
            CreateMenuButtons();
            AddMenuTitle();
            HideAllMenus();

            this.Shown += (s, e) =>
            {
                internetMonitor ??= new InternetConnectivityMonitor(this);
                internetMonitor.Start();
            };

            this.FormClosed += (s, e) =>
            {
                internetMonitor?.Dispose();
                internetMonitor = null;
            };

            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeLayout()
        {
            // ===== HEADER =====
            header.Dock = DockStyle.Top;
            header.Height = 64;
            header.BackColor = HeaderDeep;
            header.Padding = new Padding(16, 0, 12, 0);
            header.Paint += Header_Paint;

            // Right actions
            Panel actions = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 12, 0, 12)
            };

            StyleHeaderButton(btnLogin, "Login", LoginGreen, 96);
            btnLogin.Click += BtnLogin_Click;

            StyleHeaderButton(btnLogout, "Logout", Coral, 96);
            btnLogout.Visible = false;
            btnLogout.Click += BtnLogout_Click;

            userChip.Size = new Size(150, 36);
            userChip.BackColor = Color.FromArgb(40, 255, 255, 255);
            userChip.Visible = false;
            userChip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using SolidBrush fill = new SolidBrush(Color.FromArgb(55, 255, 255, 255));
                using Pen border = new Pen(Color.FromArgb(90, 255, 255, 255));
                Rectangle r = new Rectangle(0, 0, userChip.Width - 1, userChip.Height - 1);
                e.Graphics.FillRectangle(fill, r);
                e.Graphics.DrawRectangle(border, r);
            };

            lblUser.Text = "";
            lblUser.ForeColor = Color.White;
            lblUser.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblUser.Dock = DockStyle.Fill;
            lblUser.TextAlign = ContentAlignment.MiddleCenter;
            lblUser.BackColor = Color.Transparent;
            userChip.Controls.Add(lblUser);

            actions.Resize += (_, _) => LayoutHeaderActions(actions);
            actions.Controls.Add(userChip);
            actions.Controls.Add(btnLogout);
            actions.Controls.Add(btnLogin);

            header.Controls.Add(actions);
            header.Resize += (_, _) => LayoutHeaderActions(actions);

            // ===== LEFT MENU =====
            leftMenu.Dock = DockStyle.Left;
            leftMenu.Width = 212;
            leftMenu.BackColor = Color.FromArgb(241, 245, 249); // soft slate (not black)
            leftMenu.Padding = new Padding(10, 10, 10, 10);
            leftMenu.Paint += LeftMenu_Paint;
            leftMenu.AutoScroll = true;

            // ===== CONTENT =====
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.FromArgb(248, 250, 252);

            this.Controls.Add(content);
            this.Controls.Add(leftMenu);
            this.Controls.Add(header);
        }

        private void LeftMenu_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Soft light wash behind colorful tabs
            using (LinearGradientBrush wash = new LinearGradientBrush(
                leftMenu.ClientRectangle,
                Color.FromArgb(248, 250, 252),
                Color.FromArgb(226, 232, 240),
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(wash, leftMenu.ClientRectangle);
            }

            // Right edge accent (sky → lime)
            using (LinearGradientBrush edge = new LinearGradientBrush(
                new Rectangle(leftMenu.Width - 3, 0, 3, leftMenu.Height),
                BrandNameLabel.BubbyColor, Lime, LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(edge, leftMenu.Width - 3, 0, 3, leftMenu.Height);
            }
        }

        private void Header_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = header.ClientRectangle;

            using (LinearGradientBrush brush = new LinearGradientBrush(
                r, HeaderDeep, HeaderSky, LinearGradientMode.Horizontal))
            {
                ColorBlend blend = new ColorBlend
                {
                    Colors = new[] { HeaderDeep, HeaderMid, HeaderSky },
                    Positions = new[] { 0f, 0.55f, 1f }
                };
                brush.InterpolationColors = blend;
                e.Graphics.FillRectangle(brush, r);
            }

            // Soft highlight orb
            using (SolidBrush orb = new SolidBrush(Color.FromArgb(35, Color.White)))
                e.Graphics.FillEllipse(orb, header.Width - 160, -40, 200, 120);

            // Brand accent line (sky → lime)
            using (LinearGradientBrush line = new LinearGradientBrush(
                new Rectangle(0, header.Height - 4, header.Width, 4),
                BrandNameLabel.BubbyColor, Lime, LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(line, 0, header.Height - 4, header.Width, 4);
            }

            // Two-tone brand + subtitle — keep clear of right action buttons
            using Font brandFont = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Point);
            BrandNameLabel.DrawBrand(e.Graphics, brandFont, new Point(16, 10), "  Showroom", Color.White);

            using Font hintFont = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            TextRenderer.DrawText(
                e.Graphics,
                "Kids POS  ·  Inventory  ·  Returns",
                hintFont,
                new Rectangle(18, 40, 360, 18),
                Color.FromArgb(204, 251, 241),
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        private static void StyleHeaderButton(Button btn, string text, Color back, int width)
        {
            btn.Text = text;
            btn.Width = width;
            btn.Height = 36;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = back;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        private void LayoutHeaderActions(Panel actions)
        {
            int right = actions.ClientSize.Width - 4;
            int top = Math.Max(0, (actions.ClientSize.Height - 36) / 2);

            void Place(Control c, int width)
            {
                if (!c.Visible)
                    return;
                c.Size = new Size(width, 36);
                c.Location = new Point(right - width, top);
                right -= width + 10;
            }

            Place(btnLogin, 96);
            Place(btnLogout, 96);
            Place(userChip, 150);
        }

        private void AddMenuTitle()
        {
            // Added last so Dock.Top places it above all menu buttons
            Panel menuTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.Transparent
            };
            menuTitle.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                TextRenderer.DrawText(
                    e.Graphics,
                    "MAIN MENU",
                    new Font("Segoe UI", 8f, FontStyle.Bold),
                    new Rectangle(8, 8, menuTitle.Width - 16, 20),
                    Color.FromArgb(71, 85, 105),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                using Pen line = new Pen(Color.FromArgb(203, 213, 225));
                e.Graphics.DrawLine(line, 8, menuTitle.Height - 6, menuTitle.Width - 8, menuTitle.Height - 6);

                using SolidBrush dot = new SolidBrush(Lime);
                e.Graphics.FillEllipse(dot, 8, menuTitle.Height - 9, 5, 5);
            };
            leftMenu.Controls.Add(menuTitle);
        }

        private void CreateMenuButtons()
        {
            AddMenuButton("Add Item");
            AddMenuButton("Master");
            AddMenuButton("IN");
            //AddMenuButton("OUT");
            AddMenuButton("Stock");
            AddMenuButton("Label");
            AddMenuButton("Receipt");
            AddMenuButton("Return");
            AddMenuButton("Profile");
            AddMenuButton("Users");
            AddMenuButton("Revenue");
            AddMenuButton("Discount");
            AddMenuButton("Selling");
            AddMenuButton("Closing Balance");
        }

        private static readonly Color MenuIdle = Color.FromArgb(30, 41, 59);
        private static readonly Color MenuHover = Color.FromArgb(51, 65, 85);
        private static readonly Color MenuActive = Color.FromArgb(13, 148, 136);

        private void AddMenuButton(string text)
        {
            // Host gives visible gap between docked tabs (Margin ignored on Dock)
            Panel host = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 2)
            };

            Button btn = new Button();
            btn.Text = ""; // custom-drawn label
            btn.Tag = text;
            btn.Dock = DockStyle.Fill;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.FromArgb(226, 232, 240);
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            btn.Paint += MenuButton_Paint;
            btn.MouseEnter += (_, _) => btn.Invalidate();
            btn.MouseLeave += (_, _) => btn.Invalidate();

            btn.Click += (s, e) =>
            {
                if (activeButton != null)
                    SetMenuButtonActive(activeButton, false);

                activeButton = (Button)s;
                SetMenuButtonActive(activeButton, true);

                content.Controls.Clear();

                string key = activeButton.Tag?.ToString() ?? text;
                UserControl uc = key switch
                {
                    "Add Item" => new AddItem(),
                    "Master" => new Master(CurrentRole),
                    "IN" => new Inward(),
                    "Stock" => new Stock(),
                    "Label" => new LabelPrint(),
                    "Receipt" => receiptPage ??= new Receipt(),
                    "Return" => returnPage ??= new Return(),
                    "Profile" => new Profile(),
                    "Users" => new Users(),
                    "Revenue" => new Revenue(),
                    "Discount" => new DiscountManager(CurrentRole),
                    "Selling" => new Selling(),
                    "Closing Balance" => new ClosingBalance(),
                    _ => null
                };

                if (uc != null)
                {
                    uc.Dock = DockStyle.Fill;
                    content.Controls.Add(uc);
                }
            };

            host.Controls.Add(btn);
            leftMenu.Controls.Add(host);
            menuButtons[text] = btn;
        }

        private void SetMenuButtonActive(Button btn, bool active)
        {
            btn.Invalidate();
            if (btn.Parent != null)
                btn.Parent.Invalidate();
        }

        private void MenuButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            string key = btn.Tag?.ToString() ?? "";
            bool active = ReferenceEquals(btn, activeButton);
            bool hover = btn.ClientRectangle.Contains(btn.PointToClient(Control.MousePosition));

            Color logoColor = GetMenuAccent(key);
            // Tab bg follows logo color; active a bit richer, hover slightly brighter
            Color fill = active
                ? Blend(logoColor, Color.White, 0.12f)
                : (hover ? Blend(logoColor, Color.White, 0.20f) : logoColor);

            Rectangle bounds = new Rectangle(1, 1, btn.Width - 3, btn.Height - 3);

            using (GraphicsPath path = RoundedRect(bounds, 10))
            {
                using (SolidBrush brush = new SolidBrush(fill))
                    e.Graphics.FillPath(brush, path);

                Color borderColor = active ? Darken(logoColor, 0.25f) : Blend(logoColor, Color.Black, 0.12f);
                using Pen border = new Pen(borderColor, active ? 1.8f : 1f);
                e.Graphics.DrawPath(border, path);
            }

            // Active accent bar (darker shade of same logo color)
            if (active)
            {
                using SolidBrush accent = new SolidBrush(Darken(logoColor, 0.35f));
                e.Graphics.FillRectangle(accent, 5, 8, 3, btn.Height - 16);
            }

            // Icon chip: white translucent so logo color bg shows through theme
            Rectangle chip = new Rectangle(active ? 14 : 10, (btn.Height - 26) / 2, 26, 26);
            using (SolidBrush chipBrush = new SolidBrush(Color.FromArgb(active ? 230 : 210, 255, 255, 255)))
                e.Graphics.FillEllipse(chipBrush, chip);

            Color iconColor = Darken(logoColor, 0.45f);
            DrawMenuIcon(e.Graphics, key, chip, iconColor);

            // Dark text on light logo-colored bg for readability
            Rectangle textBounds = new Rectangle(chip.Right + 10, 0, btn.Width - chip.Right - 16, btn.Height);
            TextRenderer.DrawText(
                e.Graphics, key,
                btn.Font,
                textBounds,
                Color.FromArgb(15, 23, 42),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        private static Color Blend(Color a, Color b, float amountB)
        {
            amountB = Math.Clamp(amountB, 0f, 1f);
            float amountA = 1f - amountB;
            return Color.FromArgb(
                (int)(a.R * amountA + b.R * amountB),
                (int)(a.G * amountA + b.G * amountB),
                (int)(a.B * amountA + b.B * amountB));
        }

        private static Color Darken(Color c, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);
            return Color.FromArgb(
                (int)(c.R * (1f - amount)),
                (int)(c.G * (1f - amount)),
                (int)(c.B * (1f - amount)));
        }

        private static Color GetMenuAccent(string key) => key switch
        {
            "Add Item" => Color.FromArgb(125, 211, 252),
            "Master" => Color.FromArgb(167, 243, 208),
            "IN" => Color.FromArgb(253, 224, 71),
            "Stock" => Color.FromArgb(253, 186, 116),
            "Label" => Color.FromArgb(196, 181, 253),
            "Receipt" => Color.FromArgb(110, 231, 183),
            "Return" => Color.FromArgb(252, 165, 165),
            "Profile" => Color.FromArgb(165, 243, 252),
            "Users" => Color.FromArgb(186, 230, 253),
            "Revenue" => Color.FromArgb(190, 242, 100),
            "Discount" => Color.FromArgb(253, 186, 116),
            "Selling" => Color.FromArgb(147, 197, 253),
            "Closing Balance" => Color.FromArgb(251, 113, 133),
            _ => Color.FromArgb(148, 163, 184)
        };

        private static void DrawMenuIcon(Graphics g, string key, Rectangle chip, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen pen = new Pen(color, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            using SolidBrush brush = new SolidBrush(color);

            int cx = chip.X + chip.Width / 2;
            int cy = chip.Y + chip.Height / 2;
            int s = 7; // scale from center

            switch (key)
            {
                case "Add Item": // plus
                    g.DrawLine(pen, cx - s, cy, cx + s, cy);
                    g.DrawLine(pen, cx, cy - s, cx, cy + s);
                    break;

                case "Master": // 2x2 grid
                    g.DrawRectangle(pen, cx - s, cy - s, s - 1, s - 1);
                    g.DrawRectangle(pen, cx + 1, cy - s, s - 1, s - 1);
                    g.DrawRectangle(pen, cx - s, cy + 1, s - 1, s - 1);
                    g.DrawRectangle(pen, cx + 1, cy + 1, s - 1, s - 1);
                    break;

                case "IN": // arrow down into tray
                    g.DrawLine(pen, cx, cy - s, cx, cy + 2);
                    g.DrawLine(pen, cx - 4, cy - 1, cx, cy + 3);
                    g.DrawLine(pen, cx + 4, cy - 1, cx, cy + 3);
                    g.DrawLine(pen, cx - s, cy + s, cx + s, cy + s);
                    break;

                case "Stock": // stacked boxes
                    g.DrawRectangle(pen, cx - s, cy - 2, s * 2, s + 1);
                    g.DrawRectangle(pen, cx - s + 2, cy - s, s * 2 - 4, s);
                    break;

                case "Label": // price tag
                    Point[] tag =
                    {
                        new Point(cx - s, cy),
                        new Point(cx - 2, cy - s),
                        new Point(cx + s, cy - s),
                        new Point(cx + s, cy + s),
                        new Point(cx - 2, cy + s)
                    };
                    g.DrawPolygon(pen, tag);
                    g.FillEllipse(brush, cx + 2, cy - 2, 3, 3);
                    break;

                case "Receipt": // document
                    g.DrawRectangle(pen, cx - s + 1, cy - s, s * 2 - 2, s * 2);
                    g.DrawLine(pen, cx - 3, cy - 3, cx + 3, cy - 3);
                    g.DrawLine(pen, cx - 3, cy, cx + 3, cy);
                    g.DrawLine(pen, cx - 3, cy + 3, cx + 1, cy + 3);
                    break;

                case "Return": // U-turn arrow
                    g.DrawArc(pen, cx - s, cy - s + 1, s * 2, s * 2 - 2, 200, 220);
                    g.DrawLine(pen, cx - s + 1, cy - 2, cx - s + 1, cy - s);
                    g.DrawLine(pen, cx - s + 1, cy - s, cx - 2, cy - s + 3);
                    break;

                case "Profile": // person
                    g.DrawEllipse(pen, cx - 3, cy - s, 6, 6);
                    g.DrawArc(pen, cx - s, cy - 1, s * 2, s + 4, 200, 140);
                    break;

                case "Users": // two people
                    g.DrawEllipse(pen, cx - 5, cy - s, 5, 5);
                    g.DrawArc(pen, cx - s, cy - 1, s + 2, s + 3, 200, 140);
                    g.DrawEllipse(pen, cx + 1, cy - s + 1, 5, 5);
                    g.DrawArc(pen, cx - 1, cy, s + 2, s + 3, 200, 140);
                    break;

                case "Revenue": // bar chart
                    g.DrawLine(pen, cx - s, cy + s, cx + s, cy + s);
                    g.FillRectangle(brush, cx - s, cy, 3, s);
                    g.FillRectangle(brush, cx - 1, cy - 4, 3, s + 4);
                    g.FillRectangle(brush, cx + 4, cy - s + 2, 3, s * 2 - 2);
                    break;

                case "Discount": // %
                    TextRenderer.DrawText(
                        g, "%",
                        new Font("Segoe UI", 10f, FontStyle.Bold),
                        chip, color,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    break;

                case "Selling": // cart
                    g.DrawLine(pen, cx - s, cy - 4, cx - s + 3, cy - 4);
                    g.DrawLine(pen, cx - s + 3, cy - 4, cx - 2, cy + 3);
                    g.DrawLine(pen, cx - 2, cy + 3, cx + s - 1, cy + 3);
                    g.DrawLine(pen, cx + s - 1, cy + 3, cx + s, cy - 2);
                    g.DrawLine(pen, cx - 2, cy - 2, cx + s, cy - 2);
                    g.FillEllipse(brush, cx - 1, cy + s - 1, 3, 3);
                    g.FillEllipse(brush, cx + 3, cy + s - 1, 3, 3);
                    break;

                case "Closing Balance": // wallet / rupee card
                    g.DrawRectangle(pen, cx - s, cy - 4, s * 2, s + 5);
                    g.DrawLine(pen, cx - s, cy, cx + s, cy);
                    TextRenderer.DrawText(
                        g, "₹",
                        new Font("Segoe UI", 8f, FontStyle.Bold),
                        new Rectangle(chip.X, chip.Y + 2, chip.Width, chip.Height - 2),
                        color,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    break;

                default:
                    g.DrawEllipse(pen, cx - 4, cy - 4, 8, 8);
                    break;
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void HideAllMenus()
        {
            foreach (var btn in menuButtons.Values)
            {
                btn.Visible = false;
                if (btn.Parent != null)
                    btn.Parent.Visible = false;
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            LoginForm login = new LoginForm();

            if (login.ShowDialog() == DialogResult.OK)
            {
                lblUser.Text = login.SelectedRole;
                CurrentRole = login.SelectedRole;

                ApplyRoleAccess(login.SelectedRole);

                btnLogin.Visible = false;
                btnLogout.Visible = true;
                userChip.Visible = true;
                if (btnLogout.Parent is Panel actions)
                    LayoutHeaderActions(actions);
            }
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            HideAllMenus();
            content.Controls.Clear();

            lblUser.Text = "";
            CurrentRole = "";
            btnLogin.Visible = true;
            btnLogout.Visible = false;
            userChip.Visible = false;
            if (btnLogin.Parent is Panel actions)
                LayoutHeaderActions(actions);
        }

        private void ApplyRoleAccess(string role)
        {
            foreach (var pair in menuButtons)
            {
                Button btn = pair.Value;
                string key = pair.Key;
                bool show = false;

                if (key == "Profile")
                    show = true;
                else if (role == "Master Admin")
                    show = true;
                else if (role == "Admin")
                    show = key is "Add Item" or "Master" or "IN" or "Stock" or "Label" or "Receipt" or "Return" or "Closing Balance";
                else if (role == "Cashier")
                    show = key is "Receipt" or "Return" or "IN" or "Stock" or "Label" or "Closing Balance";

                btn.Visible = show;
                if (btn.Parent != null)
                    btn.Parent.Visible = show;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                BackupProgressForm.RunBackupWithUi(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Database backup failed.\n\n" + ex.Message,
                    "Backup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
