using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// On app close: start backup, keep popup open for a fixed 1 minute, then close.
    /// </summary>
    internal sealed class BackupProgressForm : Form
    {
        private readonly Label lblStatus = new Label();
        private readonly Label lblTimer = new Label();
        private readonly ProgressBar progress = new ProgressBar();
        private readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
        private DateTime startedAtUtc;
        private bool closingFromLogic;
        private const int ForceWaitSeconds = 60;

        private BackupProgressForm()
        {
            Text = "Database Backup";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(400, 140);
            BackColor = Color.White;

            lblStatus.Text = "Backing up database to Google Drive…\nPlease wait 1 minute. App will close after that.";
            lblStatus.Location = new Point(16, 12);
            lblStatus.Size = new Size(368, 48);
            lblStatus.Font = new Font("Segoe UI", 9.5f);
            lblStatus.ForeColor = Color.FromArgb(30, 41, 59);

            progress.Location = new Point(16, 68);
            progress.Size = new Size(368, 20);
            progress.Style = ProgressBarStyle.Marquee;
            progress.MarqueeAnimationSpeed = 25;

            lblTimer.Location = new Point(16, 98);
            lblTimer.Size = new Size(368, 24);
            lblTimer.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblTimer.ForeColor = Color.FromArgb(15, 118, 110);
            lblTimer.Text = $"Time left: {ForceWaitSeconds}s";

            Controls.Add(lblStatus);
            Controls.Add(progress);
            Controls.Add(lblTimer);

            uiTimer.Interval = 200;
            uiTimer.Tick += UiTimer_Tick;
        }

        public static void RunBackupWithUi(IWin32Window? owner)
        {
            using BackupProgressForm form = new BackupProgressForm();
            if (owner != null)
                form.ShowDialog(owner);
            else
                form.ShowDialog();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            startedAtUtc = DateTime.UtcNow;
            uiTimer.Start();

            // Backup in background — popup stays full 1 minute either way
            _ = Task.Run(() =>
            {
                try { DBBackup.CreateBackup(); }
                catch { /* ignore — timer still runs */ }
            });
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            double elapsed = (DateTime.UtcNow - startedAtUtc).TotalSeconds;
            double left = Math.Max(0, ForceWaitSeconds - elapsed);
            lblTimer.Text = $"Time left: {left:0}s  (forced 1 minute wait)";

            if (elapsed >= ForceWaitSeconds)
                Finish();
        }

        private void Finish()
        {
            if (closingFromLogic || IsDisposed)
                return;

            closingFromLogic = true;
            uiTimer.Stop();

            progress.Style = ProgressBarStyle.Continuous;
            progress.Value = 100;
            lblStatus.Text = "1 minute done.\nClosing application…";
            lblTimer.Text = "Done";

            DialogResult = DialogResult.OK;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!closingFromLogic && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }
            uiTimer.Stop();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                uiTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
