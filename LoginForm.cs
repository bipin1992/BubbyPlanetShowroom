using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public partial class LoginForm : Form
    {
        public static string LoggedInUser = "";
        public string SelectedRole { get; private set; }

        public LoginForm()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.AcceptButton = btnLogin; // ENTER key login
            this.Load += LoginForm_Load;

            btnLogin.MouseEnter += BtnLogin_MouseEnter;
            btnLogin.MouseLeave += BtnLogin_MouseLeave;
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            txtUsername.Focus();
            MakeButtonRounded(btnLogin);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string role = cmbRole.SelectedItem?.ToString();

            //string username = "Bipin";
            //string password = "1234";
            //string role = "Master Admin";

            if (username == "" || password == "" || role == "")
            {
                MessageBox.Show("Please fill all fields");
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
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid Login!");
                        ShakeForm();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DB Error: " + ex.Message);
                }
            }
        }

        private void InitializeComponent()
        {
            labelTitle = new Label();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            cmbRole = new ComboBox();
            btnLogin = new Button();
            chkShowPassword = new CheckBox();
            SuspendLayout();
            // 
            // labelTitle
            // 
            labelTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            labelTitle.Location = new Point(24, 9);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(285, 42);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "Member Login";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(38, 67);
            label1.Name = "label1";
            label1.Size = new Size(89, 23);
            label1.TabIndex = 1;
            label1.Text = "Username";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            label2.Location = new Point(38, 107);
            label2.Name = "label2";
            label2.Size = new Size(85, 23);
            label2.TabIndex = 2;
            label2.Text = "Password";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            label3.Location = new Point(38, 147);
            label3.Name = "label3";
            label3.Size = new Size(98, 23);
            label3.TabIndex = 3;
            label3.Text = "Login Type";
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(149, 63);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(138, 27);
            txtUsername.TabIndex = 4;
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(149, 103);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(138, 27);
            txtPassword.TabIndex = 5;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // cmbRole
            // 
            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.Items.AddRange(new object[] { "Master Admin", "Admin", "Cashier", "Staff" });
            cmbRole.Location = new Point(149, 143);
            cmbRole.Name = "cmbRole";
            cmbRole.Size = new Size(138, 28);
            cmbRole.TabIndex = 6;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(25, 118, 210);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(29, 193);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(258, 38);
            btnLogin.TabIndex = 7;
            btnLogin.Text = "LOGIN";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            // 
            // chkShowPassword
            // 
            chkShowPassword.AutoSize = true;
            chkShowPassword.Location = new Point(293, 106);
            chkShowPassword.Name = "chkShowPassword";
            chkShowPassword.Size = new Size(67, 24);
            chkShowPassword.TabIndex = 8;
            chkShowPassword.Text = "Show";
            chkShowPassword.CheckedChanged += chkShowPassword_CheckedChanged;
            // 
            // LoginForm
            // 
            BackColor = Color.Gainsboro;
            ClientSize = new Size(355, 260);
            Controls.Add(labelTitle);
            Controls.Add(label1);
            Controls.Add(label2);
            Controls.Add(label3);
            Controls.Add(txtUsername);
            Controls.Add(txtPassword);
            Controls.Add(cmbRole);
            Controls.Add(btnLogin);
            Controls.Add(chkShowPassword);
            Name = "LoginForm";
            Text = "Showroom Login";
            ResumeLayout(false);
            PerformLayout();
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }

        private void BtnLogin_MouseEnter(object sender, EventArgs e)
        {
            btnLogin.BackColor = Color.FromArgb(13, 71, 161);
        }

        private void BtnLogin_MouseLeave(object sender, EventArgs e)
        {
            btnLogin.BackColor = Color.FromArgb(25, 118, 210);
        }

        private void MakeButtonRounded(Button btn)
        {
            GraphicsPath path = new GraphicsPath();
            int radius = 20;

            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(btn.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(btn.Width - radius, btn.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, btn.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();

            btn.Region = new Region(path);
        }

        private void ShakeForm()
        {
            var original = this.Location;

            for (int i = 0; i < 8; i++)
            {
                this.Location = new Point(original.X - 5, original.Y);
                System.Threading.Thread.Sleep(20);
                this.Location = new Point(original.X + 5, original.Y);
                System.Threading.Thread.Sleep(20);
            }

            this.Location = original;
        }

        private Label labelTitle;
        private Label label1;
        private Label label2;
        private Label label3;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private ComboBox cmbRole;
        private Button btnLogin;
        private CheckBox chkShowPassword;
    }
}
