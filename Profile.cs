using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public class Profile : UserControl
    {
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

        private int userId;
        private byte[]? photoData;

        public Profile()
        {
            InitializeUI();
            LoadProfile();
        }

        private void InitializeUI()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            AutoScroll = true;

            Panel form = new Panel
            {
                Width = 760,
                Height = 620,
                Top = 20,
                Left = 20,
                BackColor = Color.White
            };

            Label title = new Label
            {
                Text = "Profile",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(300, 40)
            };

            int y = 60;
            AddField(form, "Username", txtUsername, 0, y, readOnly: true);
            AddField(form, "Password", txtPassword, 360, y);
            y += 58;

            AddField(form, "Role", txtRole, 0, y, readOnly: true);
            AddField(form, "Full Name", txtFullName, 360, y);
            y += 58;

            AddField(form, "Phone", txtPhone, 0, y);
            AddField(form, "Status", txtStatus, 360, y, readOnly: true);
            y += 58;

            AddDateField(form, "Joining Date", dtJoiningDate, 0, y);
            AddField(form, "Address", txtAddress, 360, y);
            y += 58;

            AddField(form, "Aadhar", txtAadhar, 0, y, readOnly: true);
            y += 70;

            Label photoLabel = new Label
            {
                Text = "Photo",
                Location = new Point(0, y),
                Size = new Size(120, 24),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            picPhoto.Location = new Point(0, y + 28);
            picPhoto.Size = new Size(130, 130);
            picPhoto.BorderStyle = BorderStyle.FixedSingle;
            picPhoto.SizeMode = PictureBoxSizeMode.StretchImage;

            btnUpload.Text = "Upload Photo";
            btnUpload.Location = new Point(150, y + 28);
            btnUpload.Size = new Size(130, 34);
            btnUpload.Click += BtnUpload_Click;

            btnSave.Text = "Save Profile";
            btnSave.Location = new Point(0, y + 165);
            btnSave.Size = new Size(150, 38);
            btnSave.BackColor = Color.FromArgb(25, 118, 210);
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Click += BtnSave_Click;

            txtPhone.KeyPress += OnlyNumber_KeyPress;

            form.Controls.Add(title);
            form.Controls.Add(photoLabel);
            form.Controls.Add(picPhoto);
            form.Controls.Add(btnUpload);
            form.Controls.Add(btnSave);
            Controls.Add(form);
            AutoScrollMinSize = new Size(form.Right + 20, form.Bottom + 20);
        }

        private static void AddField(Panel parent, string labelText, TextBox textBox, int x, int y, bool readOnly = false)
        {
            Label label = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                Size = new Size(120, 24),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            textBox.Location = new Point(x, y + 26);
            textBox.Size = new Size(300, 28);
            textBox.ReadOnly = readOnly;

            if (readOnly)
            {
                textBox.BackColor = Color.FromArgb(240, 240, 240);
            }

            parent.Controls.Add(label);
            parent.Controls.Add(textBox);
        }

        private static void AddDateField(Panel parent, string labelText, DateTimePicker picker, int x, int y)
        {
            Label label = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                Size = new Size(120, 24),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            picker.Location = new Point(x, y + 26);
            picker.Size = new Size(300, 28);
            picker.Enabled = false;
            picker.Format = DateTimePickerFormat.Short;

            parent.Controls.Add(label);
            parent.Controls.Add(picker);
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
                txtStatus.Text = reader["status"]?.ToString() ?? "";
                txtAadhar.Text = reader["aadhar"]?.ToString() ?? "";
                txtAddress.Text = reader["address"]?.ToString() ?? "";

                if (reader["joining_date"] != DBNull.Value)
                {
                    dtJoiningDate.Value = Convert.ToDateTime(reader["joining_date"]);
                }

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
