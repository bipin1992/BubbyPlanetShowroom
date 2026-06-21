
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    public class MainForm : Form
    {
        public static string CurrentRole { get; private set; } = "";

        Panel header = new Panel();
        Panel leftMenu = new Panel();
        Panel content = new Panel();

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
            this.Text = "BubbyPlanet Showroom Management";
            this.WindowState = FormWindowState.Maximized;

            InitializeLayout();
            CreateMenuButtons();
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
            header.Height = 38;
            header.BackColor = Color.FromArgb(30, 41, 59);

            Label title = new Label();
            title.Text = "BUBBYPLANET SHOWROOM MANAGEMENT";
            title.ForeColor = Color.White;
            title.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            title.Dock = DockStyle.Left;
            title.Width = 450;
            title.AutoSize = true;
            //title.Padding = new Padding(40, 0, 0, 0);
            title.TextAlign = ContentAlignment.MiddleLeft;

            lblUser.ForeColor = Color.White;
            lblUser.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblUser.AutoSize = false;
            lblUser.Dock = DockStyle.Right;
            lblUser.Width = 150;
            lblUser.TextAlign = ContentAlignment.MiddleRight;

            btnLogin.Text = "Login";
            btnLogin.Width = 80;
            btnLogin.Dock = DockStyle.Right;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.BackColor = Color.FromArgb(34, 197, 94);
            btnLogin.ForeColor = Color.White;
            btnLogin.Click += BtnLogin_Click;

            btnLogout.Text = "Logout";
            btnLogout.Width = 80;
            btnLogout.Dock = DockStyle.Right;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.BackColor = Color.FromArgb(239, 68, 68);
            btnLogout.ForeColor = Color.White;
            btnLogout.Visible = false;
            btnLogout.Click += BtnLogout_Click;

            header.Controls.Add(btnLogout);
            header.Controls.Add(btnLogin);
            header.Controls.Add(lblUser);
            header.Controls.Add(title);

            // ===== LEFT MENU =====
            leftMenu.Dock = DockStyle.Left;
            leftMenu.Width = 180;
            leftMenu.BackColor = Color.LightGray;

            // ===== CONTENT =====
            content.Dock = DockStyle.Fill;

            this.Controls.Add(content);
            this.Controls.Add(leftMenu);
            this.Controls.Add(header);
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
            AddMenuButton("Users");
            AddMenuButton("Revenue");
            AddMenuButton("Discount");
            AddMenuButton("Selling");
        }

        private void AddMenuButton(string text)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Dock = DockStyle.Top;
            btn.Height = 55;

            btn.Click += (s, e) =>
            {
                // ✅ ONLY ADD THIS BLOCK (no other change)
                if (activeButton != null)
                {
                    activeButton.BackColor = Color.LightGray;
                    activeButton.ForeColor = Color.Black;
                }

                activeButton = (Button)s;
                activeButton.BackColor = Color.FromArgb(59, 130, 246);
                activeButton.ForeColor = Color.White;

                content.Controls.Clear();

                UserControl uc = text switch
                {
                    "Add Item" => new AddItem(),
                    "Master" => new Master(CurrentRole),
                    "IN" => new Inward(),
                    //"OUT" => new Outward(),
                    "Stock" => new Stock(),
                    "Label" => new LabelPrint(),
                    "Receipt" => receiptPage ??= new Receipt(),
                    "Return" => returnPage ??= new Return(),
                    "Users" => new Users(),
                    "Revenue" => new Revenue(), // ✅ NEW
                    "Discount" => new DiscountManager(CurrentRole),
                    "Selling" => new Selling(),
                    _ => null
                };

                if (uc != null)
                {
                    uc.Dock = DockStyle.Fill;
                    content.Controls.Add(uc);
                }
            };

            leftMenu.Controls.Add(btn);
            menuButtons[text] = btn;
        }

        private void HideAllMenus()
        {
            foreach (var btn in menuButtons.Values)
            {
                btn.Visible = false;
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            LoginForm login = new LoginForm();

            if (login.ShowDialog() == DialogResult.OK)
            {
                lblUser.Text = "👤 " + login.SelectedRole;
                CurrentRole = login.SelectedRole;

                ApplyRoleAccess(login.SelectedRole);

                btnLogin.Visible = false;
                btnLogout.Visible = true;
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
        }

        private void ApplyRoleAccess(string role)
        {
            foreach (Control ctrl in leftMenu.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.Visible = false;

                    if (role == "Master Admin")
                    {
                        btn.Visible = true;
                    }

                    if (role == "Admin")
                    {
                        if ((btn.Text == "Add Item") || (btn.Text == "Master") || (btn.Text == "IN") || (btn.Text == "Stock") || (btn.Text == "Label") || (btn.Text == "Receipt") || (btn.Text == "Return"))
                        {
                            btn.Visible = true;
                        }
                    }

                    if (role == "Cashier")
                    {
                        if (btn.Text == "Receipt" || btn.Text == "Return" || (btn.Text == "IN") || (btn.Text == "Stock") || (btn.Text == "Label"))
                        {
                            btn.Visible = true;
                        }
                    }
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                DBBackup.CreateBackup();
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
