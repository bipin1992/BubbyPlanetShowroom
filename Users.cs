using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Drawing;

namespace BubbyPlanetShowroom
{
    public class Users : UserControl
    {
        DataGridView grid = new DataGridView();

        TextBox txtUsername = new TextBox();
        TextBox txtPassword = new TextBox();
        ComboBox cmbRole = new ComboBox();

        TextBox txtName = new TextBox();
        TextBox txtPhone = new TextBox();
        TextBox txtSalary = new TextBox();
        TextBox txtAadhar = new TextBox();
        TextBox txtAddress = new TextBox();

        DateTimePicker dtJoin = new DateTimePicker();

        Button btnAdd = new Button();
        Button btnUpdate = new Button();
        Button btnDelete = new Button();

        //PictureBox pic = new PictureBox();
        //Button btnUpload = new Button();

        //byte[] imageData = null;
        int selectedId = 0;

        public Users()
        {
            InitializeUI();
            LoadData();
        }

        void InitializeUI()
        {
            this.Dock = DockStyle.Fill;

            Panel top = new Panel { Dock = DockStyle.Top, Height = 150 };

            txtUsername.PlaceholderText = "Username";
            txtUsername.Left = 10;

            txtPassword.PlaceholderText = "Password";
            txtPassword.Left = 120;

            cmbRole.Left = 230;
            cmbRole.Items.AddRange(new string[] { "Admin", "Cashier", "Staff" });

            txtName.PlaceholderText = "Full Name";
            txtName.Left = 340;

            txtPhone.PlaceholderText = "Phone";
            txtPhone.Left = 480;

            txtSalary.PlaceholderText = "Salary";
            txtSalary.Left = 620;

            txtAadhar.PlaceholderText = "Aadhar";
            txtAadhar.Left = 760;

            dtJoin.Left = 900;

            txtAddress.PlaceholderText = "Address";
            txtAddress.Left = 10;
            txtAddress.Top = 70;
            txtAddress.Width = 400;

            btnAdd.Text = "Add";
            btnAdd.Left = 430;
            btnAdd.Top = 70;
            btnAdd.Click += BtnAdd_Click;

            btnUpdate.Text = "Update";
            btnUpdate.Left = 510;
            btnUpdate.Top = 70;
            btnUpdate.Click += BtnUpdate_Click;

            btnDelete.Text = "Delete";
            btnDelete.Left = 600;
            btnDelete.Top = 70;
            btnDelete.Click += BtnDelete_Click;

            //pic.Left = 1150;
            //pic.Top = 10;
            //pic.Width = 100;
            //pic.Height = 100;
            //pic.BorderStyle = BorderStyle.FixedSingle;
            //pic.SizeMode = PictureBoxSizeMode.StretchImage;

            //btnUpload.Text = "Upload";
            //btnUpload.Left = 1150;
            //btnUpload.Top = 120;
            //btnUpload.Click += BtnUpload_Click;

            txtPhone.KeyPress += OnlyNumber_KeyPress;
            txtAadhar.KeyPress += OnlyNumber_KeyPress;
            txtSalary.KeyPress += OnlyDecimal_KeyPress;

            top.Controls.AddRange(new Control[]
            {
                txtUsername, txtPassword, cmbRole,
                txtName, txtPhone, txtSalary, txtAadhar, dtJoin,
                txtAddress, btnAdd, btnUpdate, btnDelete
                //, pic, btnUpload
            });

            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.CellClick += Grid_CellClick;

            this.Controls.Add(grid);
            this.Controls.Add(top);
        }

        void LoadData()
        {
            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();
                MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM inv_users WHERE status=1 ORDER BY id DESC", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                grid.DataSource = dt;
            }
        }

        void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            if (IsAadharExists(txtAadhar.Text))
            {
                MessageBox.Show("Aadhar exists ❌");
                return;
            }

            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();

                string q = @"INSERT INTO inv_users
                (username,password,role,full_name,phone,salary,joining_date,aadhar,address,status)
                VALUES(@u,@p,@r,@n,@ph,@s,@j,@a,@ad,1)";

                MySqlCommand cmd = new MySqlCommand(q, conn);

                cmd.Parameters.AddWithValue("@u", txtUsername.Text);
                cmd.Parameters.AddWithValue("@p", txtPassword.Text);
                cmd.Parameters.AddWithValue("@r", cmbRole.Text);
                cmd.Parameters.AddWithValue("@n", txtName.Text);
                cmd.Parameters.AddWithValue("@ph", txtPhone.Text);
                cmd.Parameters.AddWithValue("@s", txtSalary.Text);
                cmd.Parameters.AddWithValue("@j", dtJoin.Value);
                cmd.Parameters.AddWithValue("@a", txtAadhar.Text);
                cmd.Parameters.AddWithValue("@ad", txtAddress.Text);
                //cmd.Parameters.AddWithValue("@img", (object)imageData ?? DBNull.Value);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Added ✅");
                LoadData();
                ClearFields();
            }
        }

        void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedId == 0)
            {
                MessageBox.Show("Select user first");
                return;
            }

            if (!ValidateInput()) return;

            if (IsAadharExists(txtAadhar.Text, selectedId))
            {
                MessageBox.Show("Aadhar exists ❌");
                return;
            }

            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();

                string q = @"UPDATE inv_users SET
                username=@u,password=@p,role=@r,full_name=@n,
                phone=@ph,salary=@s,joining_date=@j,
                aadhar=@a,address=@ad
                WHERE id=@id";

                MySqlCommand cmd = new MySqlCommand(q, conn);

                cmd.Parameters.AddWithValue("@u", txtUsername.Text);
                cmd.Parameters.AddWithValue("@p", txtPassword.Text);
                cmd.Parameters.AddWithValue("@r", cmbRole.Text);
                cmd.Parameters.AddWithValue("@n", txtName.Text);
                cmd.Parameters.AddWithValue("@ph", txtPhone.Text);
                cmd.Parameters.AddWithValue("@s", txtSalary.Text);
                cmd.Parameters.AddWithValue("@j", dtJoin.Value);
                cmd.Parameters.AddWithValue("@a", txtAadhar.Text);
                cmd.Parameters.AddWithValue("@ad", txtAddress.Text);
                //cmd.Parameters.AddWithValue("@img", (object)imageData ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", selectedId);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Updated ✅");
                LoadData();
                ClearFields();
            }
        }

        void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedId == 0)
            {
                MessageBox.Show("Select user first");
                return;
            }

            if (MessageBox.Show("Delete user?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();

                MySqlCommand cmd = new MySqlCommand("UPDATE inv_users SET status=0 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", selectedId);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Deleted ✅");
                LoadData();
                ClearFields();
            }
        }

        void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = grid.Rows[e.RowIndex];

            selectedId = Convert.ToInt32(row.Cells["id"].Value);

            txtUsername.Text = row.Cells["username"]?.Value?.ToString() ?? "";
            txtPassword.Text = row.Cells["password"]?.Value?.ToString() ?? "";
            cmbRole.Text = row.Cells["role"]?.Value?.ToString() ?? "";

            txtName.Text = row.Cells["full_name"]?.Value?.ToString() ?? "";
            txtPhone.Text = row.Cells["phone"]?.Value?.ToString() ?? "";
            txtSalary.Text = row.Cells["salary"]?.Value?.ToString() ?? "";
            txtAadhar.Text = row.Cells["aadhar"]?.Value?.ToString() ?? "";
            txtAddress.Text = row.Cells["address"]?.Value?.ToString() ?? "";

            //try
            //{
            //    if (row.Cells["photo"].Value != DBNull.Value)
            //    {
            //        byte[] img = (byte[])row.Cells["photo"].Value;
            //        using (var ms = new System.IO.MemoryStream(img))
            //        {
            //            pic.Image = Image.FromStream(ms);
            //            imageData = img;
            //        }
            //    }
            //    else
            //    {
            //        pic.Image = null;
            //        imageData = null;
            //    }
            //}
            //catch
            //{
            //    pic.Image = null;
            //    imageData = null;
            //}

            if (row.Cells["joining_date"].Value != DBNull.Value)
            {
                dtJoin.Value = Convert.ToDateTime(row.Cells["joining_date"].Value);
            }
        }

        bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text)) return Show("Username required");
            if (string.IsNullOrWhiteSpace(txtPassword.Text)) return Show("Password required");
            if (cmbRole.SelectedIndex == -1) return Show("Select role");
            if (string.IsNullOrWhiteSpace(txtName.Text)) return Show("Name required");

            if (txtPhone.Text.Length != 10 || !txtPhone.Text.All(char.IsDigit))
                return Show("Invalid phone");

            if (txtAadhar.Text.Length != 12 || !txtAadhar.Text.All(char.IsDigit))
                return Show("Invalid Aadhar");

            decimal sal;
            if (!decimal.TryParse(txtSalary.Text, out sal) || sal < 0)
                return Show("Invalid salary");

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
                return Show("Address required");

            return true;
        }

        bool Show(string msg)
        {
            MessageBox.Show(msg);
            return false;
        }

        void ClearFields()
        {
            txtUsername.Clear();
            txtPassword.Clear();
            cmbRole.SelectedIndex = -1;
            txtName.Clear();
            txtPhone.Clear();
            txtSalary.Clear();
            txtAadhar.Clear();
            txtAddress.Clear();
            //pic.Image = null;
            //imageData = null;
            selectedId = 0;
        }

        bool IsAadharExists(string aadhar, int excludeId = 0)
        {
            using (MySqlConnection conn = DB.GetConnection())
            {
                conn.Open();

                string q = "SELECT COUNT(*) FROM inv_users WHERE aadhar=@a AND id!=@id";

                MySqlCommand cmd = new MySqlCommand(q, conn);
                cmd.Parameters.AddWithValue("@a", aadhar);
                cmd.Parameters.AddWithValue("@id", excludeId);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        void OnlyNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        void OnlyDecimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox tb = sender as TextBox;

            if (!char.IsControl(e.KeyChar) &&
                !char.IsDigit(e.KeyChar) &&
                e.KeyChar != '.')
                e.Handled = true;

            if (e.KeyChar == '.' && tb.Text.Contains("."))
                e.Handled = true;
        }

        //void BtnUpload_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialog ofd = new OpenFileDialog();
        //    ofd.Filter = "Images|*.jpg;*.png;*.jpeg";

        //    if (ofd.ShowDialog() == DialogResult.OK)
        //    {
        //        pic.Image = Image.FromFile(ofd.FileName);
        //        imageData = System.IO.File.ReadAllBytes(ofd.FileName);
        //    }
        //}
    }
}
