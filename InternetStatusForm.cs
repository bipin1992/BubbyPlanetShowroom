using System;
using System.Drawing;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    public sealed class InternetStatusForm : Form
    {
        private bool canClose;

        public InternetStatusForm()
        {
            Text = "Internet Disconnected";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            TopMost = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Width = 520;
            Height = 180;

            var title = new Label
            {
                Text = "Internet connection is not available",
                Dock = DockStyle.Top,
                Height = 48,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Padding = new Padding(10, 10, 10, 0)
            };

            var message = new Label
            {
                Text = "Please connect to the internet.\nThis window will close automatically once internet is back.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10)
            };

            Controls.Add(message);
            Controls.Add(title);
        }

        public void AllowClose()
        {
            canClose = true;
            ControlBox = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!canClose)
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }
    }
}

