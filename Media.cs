using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using WpfMediaPlayer = System.Windows.Media.MediaPlayer;

namespace BubbyPlanetShowroom
{
    public class Media : UserControl
    {
        private static readonly Color HeaderBg = Color.FromArgb(30, 41, 59);
        private static readonly Color PageBg = Color.FromArgb(241, 245, 249);
        private static readonly Color Slate = Color.FromArgb(15, 23, 42);
        private static readonly Color Teal = Color.FromArgb(13, 148, 136);
        private static readonly Color Sky = Color.FromArgb(14, 165, 233);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color FieldBorder = Color.FromArgb(226, 232, 240);
        private static readonly Color Coral = Color.FromArgb(244, 63, 94);

        private readonly TextBox txtFolder = new TextBox();
        private readonly NumericUpDown numInterval = new NumericUpDown();
        private readonly CheckBox chkLoop = new CheckBox();
        private readonly Button btnBrowse = new Button();
        private readonly Button btnRefresh = new Button();
        private readonly Button btnPlay = new Button();
        private readonly Button btnPause = new Button();
        private readonly Button btnStop = new Button();
        private readonly Button btnReset = new Button();
        private readonly Button btnSelectAll = new Button();
        private readonly Button btnSelectNone = new Button();
        private readonly Label lblStatus = new Label();
        private readonly TextBox lblNowPlaying = new TextBox();
        private readonly Label lblFileCount = new Label();
        private readonly DataGridView dgvFiles = new DataGridView();
        private readonly Panel audioVisual = new Panel();
        private readonly Label lblAudioTitle = new Label();
        private TableLayoutPanel? bodyLayout;

        private readonly ElementHost playerHost = new ElementHost();
        private readonly WpfMediaPlayer player = new WpfMediaPlayer();
        private readonly System.Windows.Media.VideoDrawing videoDrawing = new System.Windows.Media.VideoDrawing();
        private readonly System.Windows.Controls.Border videoSurface = new System.Windows.Controls.Border();
        private readonly System.Windows.Forms.Timer gapTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer playWatchTimer = new System.Windows.Forms.Timer();

        private MediaPlayerSettings settings = new MediaPlayerSettings();
        private string? currentPath;
        private bool isPaused;
        private bool suppressSave;
        private int failStreak;
        private bool waitingForGap;
        private bool finishingCurrent;
        private int gapSecondsLeft;

        public Media()
        {
            InitializePlayer();
            InitializeUI();
            LoadSettingsAndFiles();
        }

        public void Shutdown()
        {
            StopPlayback(clearCurrent: true);
            try
            {
                player.Stop();
                player.Close();
            }
            catch
            {
                // Ignore shutdown errors.
            }
        }

        private void InitializePlayer()
        {
            // MediaPlayer is independent of the visual tree, so switching tabs
            // does not stop audio/video. VideoDrawing is only the on-screen view.
            videoDrawing.Player = player;
            videoDrawing.Rect = new System.Windows.Rect(0, 0, 1280, 720);
            videoSurface.Background = new System.Windows.Media.DrawingBrush(videoDrawing)
            {
                Stretch = System.Windows.Media.Stretch.Uniform,
                AlignmentX = System.Windows.Media.AlignmentX.Center,
                AlignmentY = System.Windows.Media.AlignmentY.Center
            };

            player.MediaEnded += (_, _) => SafeBeginInvoke(OnCurrentMediaFinished);
            player.MediaOpened += (_, _) => SafeBeginInvoke(OnMediaOpened);
            player.MediaFailed += (_, e) => SafeBeginInvoke(() => OnMediaFailed(e.ErrorException));
            player.Volume = 1.0;

            playerHost.Dock = DockStyle.Fill;
            playerHost.Child = videoSurface;
            playerHost.BackColor = Color.Black;

            playWatchTimer.Interval = 250;
            playWatchTimer.Tick += (_, _) => CheckPlaybackEnded();

            gapTimer.Interval = 1000;
            gapTimer.Tick += (_, _) => OnGapSecondElapsed();
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
                BackColor = PageBg,
                Margin = new Padding(0)
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 118f));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(main);

            main.Controls.Add(BuildPageHeader(), 0, 0);
            main.Controls.Add(BuildControlsCard(), 0, 1);
            main.Controls.Add(BuildBody(), 0, 2);
        }

        private Panel BuildPageHeader()
        {
            Panel header = CreateCard();
            header.Margin = new Padding(0, 0, 0, 10);
            header.Paint += (_, e) =>
            {
                Rectangle bounds = header.ClientRectangle;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using LinearGradientBrush brush = new LinearGradientBrush(
                    bounds, Teal, Sky, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brush, bounds);

                using Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "Media", titleFont,
                    new Rectangle(18, 10, 220, 28), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                using Font hintFont = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(
                    e.Graphics,
                    "Select files  ·  each file plays fully  ·  then interval (seconds)  ·  then next",
                    hintFont,
                    new Rectangle(18, 40, 620, 20),
                    Color.FromArgb(204, 251, 241),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
            return header;
        }

        private Panel BuildControlsCard()
        {
            Panel card = CreateCard();
            card.Margin = new Padding(0, 0, 0, 8);
            card.Padding = new Padding(14, 10, 14, 10);

            Label folderCap = SmallCaption("FOLDER");
            folderCap.Location = new Point(0, 2);

            txtFolder.Location = new Point(0, 20);
            txtFolder.Width = 520;
            txtFolder.Height = 28;
            txtFolder.ReadOnly = true;
            txtFolder.BorderStyle = BorderStyle.FixedSingle;
            txtFolder.Font = new Font("Segoe UI", 9.5f);
            txtFolder.BackColor = Color.FromArgb(248, 250, 252);
            txtFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            btnBrowse.Text = "Browse";
            btnBrowse.Size = new Size(92, 28);
            StyleSecondaryButton(btnBrowse);
            btnBrowse.Click += (_, _) => BrowseFolder();
            btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            btnRefresh.Text = "Refresh";
            btnRefresh.Size = new Size(92, 28);
            StyleSecondaryButton(btnRefresh);
            btnRefresh.Click += (_, _) => LoadFiles(keepSelection: true);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            Label intervalCap = SmallCaption("INTERVAL (SECONDS) — STARTS AFTER FILE ENDS");
            intervalCap.Location = new Point(0, 56);

            numInterval.Location = new Point(0, 74);
            numInterval.Size = new Size(80, 28);
            numInterval.Minimum = 0;
            numInterval.Maximum = 3600;
            numInterval.DecimalPlaces = 0;
            numInterval.Increment = 1;
            numInterval.Value = 11;
            numInterval.Font = new Font("Segoe UI", 9.5f);
            numInterval.ValueChanged += (_, _) =>
            {
                if (suppressSave) return;
                settings.IntervalSeconds = (int)numInterval.Value;
                SaveSettings();
            };

            chkLoop.Text = "After last file + interval, first selected plays";
            chkLoop.AutoSize = true;
            chkLoop.Location = new Point(96, 78);
            chkLoop.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            chkLoop.ForeColor = Slate;
            chkLoop.Checked = true;
            chkLoop.Enabled = false;

            btnPlay.Text = "Play";
            btnPlay.Size = new Size(88, 32);
            StylePrimaryButton(btnPlay, Teal);
            btnPlay.Click += (_, _) => StartPlaylist();

            btnPause.Text = "Pause";
            btnPause.Size = new Size(88, 32);
            StyleSecondaryButton(btnPause);
            btnPause.Click += (_, _) => TogglePause();

            btnStop.Text = "Stop";
            btnStop.Size = new Size(88, 32);
            StyleSecondaryButton(btnStop);
            btnStop.Click += (_, _) => StopPlayback(clearCurrent: true);

            btnReset.Text = "Reset";
            btnReset.Size = new Size(88, 32);
            StylePrimaryButton(btnReset, Coral);
            btnReset.Click += (_, _) => ResetAll();

            lblStatus.AutoSize = false;
            lblStatus.Height = 22;
            lblStatus.Font = new Font("Segoe UI", 8.5f);
            lblStatus.ForeColor = Muted;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Text = "Choose a folder, tick files, then Play.";

            card.Controls.Add(folderCap);
            card.Controls.Add(txtFolder);
            card.Controls.Add(btnBrowse);
            card.Controls.Add(btnRefresh);
            card.Controls.Add(intervalCap);
            card.Controls.Add(numInterval);
            card.Controls.Add(chkLoop);
            card.Controls.Add(btnPlay);
            card.Controls.Add(btnPause);
            card.Controls.Add(btnStop);
            card.Controls.Add(btnReset);
            card.Controls.Add(lblStatus);

            card.Resize += (_, _) => LayoutControls(card);
            LayoutControls(card);
            return card;
        }

        private void LayoutControls(Panel card)
        {
            int right = card.ClientSize.Width - card.Padding.Right;
            int topFolder = 20;
            btnRefresh.Location = new Point(right - btnRefresh.Width, topFolder);
            btnBrowse.Location = new Point(btnRefresh.Left - 8 - btnBrowse.Width, topFolder);
            txtFolder.Width = Math.Max(160, btnBrowse.Left - 12);

            int topBtns = 72;
            btnReset.Location = new Point(right - btnReset.Width, topBtns);
            btnStop.Location = new Point(btnReset.Left - 8 - btnStop.Width, topBtns);
            btnPause.Location = new Point(btnStop.Left - 8 - btnPause.Width, topBtns);
            btnPlay.Location = new Point(btnPause.Left - 8 - btnPlay.Width, topBtns);

            lblStatus.Location = new Point(210, 78);
            lblStatus.Width = Math.Max(80, btnPlay.Left - 222);
        }

        private Control BuildBody()
        {
            bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = PageBg,
                Margin = new Padding(0)
            };
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86f));

            bodyLayout.Controls.Add(BuildFileListCard(), 0, 0);
            bodyLayout.Controls.Add(BuildPlayerCard(), 0, 1);
            return bodyLayout;
        }

        private void SetPlayerHeight(bool video)
        {
            if (bodyLayout == null || bodyLayout.RowStyles.Count < 2)
                return;
            bodyLayout.RowStyles[1] = new RowStyle(SizeType.Absolute, video ? 200f : 86f);
        }

        private Panel BuildFileListCard()
        {
            Panel card = CreateCard();
            card.Margin = new Padding(0, 0, 0, 8);
            card.Padding = new Padding(1);

            ConfigureFileGrid();

            Panel header = CreateSectionHeader("FILES", "");
            lblFileCount.Dock = DockStyle.Fill;
            lblFileCount.Font = new Font("Segoe UI", 8.5f);
            lblFileCount.ForeColor = Color.FromArgb(148, 163, 184);
            lblFileCount.TextAlign = ContentAlignment.MiddleRight;
            lblFileCount.Text = "0 files";
            header.Controls.Add(lblFileCount);

            Panel actions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 6)
            };
            btnSelectAll.Text = "Select all";
            btnSelectAll.Size = new Size(100, 30);
            StyleSecondaryButton(btnSelectAll);
            btnSelectAll.Click += (_, _) => SetAllSelected(true);

            btnSelectNone.Text = "Select none";
            btnSelectNone.Size = new Size(110, 30);
            StyleSecondaryButton(btnSelectNone);
            btnSelectNone.Click += (_, _) => SetAllSelected(false);

            actions.Controls.Add(btnSelectAll);
            actions.Controls.Add(btnSelectNone);
            actions.Resize += (_, _) =>
            {
                btnSelectAll.Location = new Point(10, 6);
                btnSelectNone.Location = new Point(btnSelectAll.Right + 8, 6);
            };

            card.Controls.Add(dgvFiles);
            card.Controls.Add(actions);
            card.Controls.Add(header);
            return card;
        }

        private Panel BuildPlayerCard()
        {
            Panel card = CreateCard();
            card.Margin = new Padding(0);
            card.Padding = new Padding(1);
            card.BackColor = Color.FromArgb(15, 23, 42);

            audioVisual.Dock = DockStyle.Left;
            audioVisual.Width = 52;
            audioVisual.Visible = true;
            audioVisual.BackColor = Color.FromArgb(15, 23, 42);
            audioVisual.Paint += AudioVisual_Paint;

            lblAudioTitle.Visible = false;

            lblNowPlaying.Dock = DockStyle.Fill;
            lblNowPlaying.ForeColor = Color.White;
            lblNowPlaying.BackColor = Color.FromArgb(15, 23, 42);
            lblNowPlaying.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblNowPlaying.BorderStyle = BorderStyle.None;
            lblNowPlaying.Multiline = true;
            lblNowPlaying.ReadOnly = true;
            lblNowPlaying.TabStop = false;
            lblNowPlaying.ScrollBars = ScrollBars.None;
            lblNowPlaying.Padding = new Padding(8, 10, 10, 8);
            lblNowPlaying.Text = "Nothing playing";

            playerHost.Dock = DockStyle.Fill;
            playerHost.Visible = false;

            card.Controls.Add(lblNowPlaying);
            card.Controls.Add(playerHost);
            card.Controls.Add(audioVisual);
            return card;
        }

        private void ConfigureFileGrid()
        {
            dgvFiles.Dock = DockStyle.Fill;
            dgvFiles.AllowUserToAddRows = false;
            dgvFiles.AllowUserToResizeRows = false;
            dgvFiles.RowHeadersVisible = false;
            dgvFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFiles.MultiSelect = false;
            dgvFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvFiles.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvFiles.BackgroundColor = Color.White;
            dgvFiles.BorderStyle = BorderStyle.None;
            dgvFiles.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvFiles.GridColor = FieldBorder;
            dgvFiles.RowTemplate.Height = 34;
            dgvFiles.ColumnHeadersHeight = 38;
            dgvFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvFiles.EnableHeadersVisualStyles = false;
            dgvFiles.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
            dgvFiles.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvFiles.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvFiles.DefaultCellStyle.Font = new Font("Segoe UI", 9.25f);
            dgvFiles.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            dgvFiles.DefaultCellStyle.SelectionBackColor = Color.FromArgb(204, 251, 241);
            dgvFiles.DefaultCellStyle.SelectionForeColor = Slate;
            dgvFiles.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            dgvFiles.EditMode = DataGridViewEditMode.EditOnEnter;
            dgvFiles.Enabled = true;
            dgvFiles.ReadOnly = false;

            var chkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "",
                Width = 36,
                FillWeight = 6,
                TrueValue = true,
                FalseValue = false,
                ThreeState = false,
                ReadOnly = false
            };
            dgvFiles.Columns.Add(chkColumn);
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FileName",
                HeaderText = "File",
                FillWeight = 78,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True,
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(6, 6, 6, 6)
                }
            });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Kind",
                HeaderText = "Type",
                FillWeight = 8,
                ReadOnly = true
            });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Size",
                HeaderText = "Size",
                FillWeight = 8,
                ReadOnly = true
            });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FullPath",
                Visible = false
            });

            dgvFiles.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (dgvFiles.IsCurrentCellDirty && dgvFiles.CurrentCell is DataGridViewCheckBoxCell)
                    dgvFiles.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvFiles.CellContentClick += (_, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 0)
                    return;
                dgvFiles.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvFiles.CellValueChanged += (_, e) =>
            {
                if (e.ColumnIndex != 0)
                    return;
                PersistSelection();
                RefreshPlaylistHint();
            };
        }

        private void LoadSettingsAndFiles()
        {
            settings = MediaSettingsStore.Load();
            suppressSave = true;
            txtFolder.Text = settings.FolderPath;
            numInterval.Value = Math.Clamp(settings.IntervalSeconds, 0, 3600);
            chkLoop.Checked = settings.Loop;
            settings.SelectedFiles.Clear();
            suppressSave = false;
            LoadFiles(keepSelection: false);
        }

        private void BrowseFolder()
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog
            {
                Description = "Select the folder with audio and video files",
                UseDescriptionForTitle = true,
                SelectedPath = Directory.Exists(txtFolder.Text) ? txtFolder.Text : MediaSettingsStore.DefaultFolderPath
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            settings.FolderPath = dlg.SelectedPath;
            txtFolder.Text = dlg.SelectedPath;
            SaveSettings();
            LoadFiles(keepSelection: false);
        }

        private void LoadFiles(bool keepSelection)
        {
            HashSet<string> previouslySelected = new(StringComparer.OrdinalIgnoreCase);
            if (keepSelection)
            {
                foreach (string path in GetSelectedPaths())
                    previouslySelected.Add(Path.GetFileName(path));
            }

            dgvFiles.Rows.Clear();
            List<MediaFileEntry> files = MediaLibrary.ScanFolder(txtFolder.Text);
            foreach (MediaFileEntry file in files)
            {
                int row = dgvFiles.Rows.Add(
                    previouslySelected.Contains(file.FileName),
                    file.FileName,
                    file.Kind,
                    MediaLibrary.FormatSize(file.SizeBytes),
                    file.FullPath);
                dgvFiles.Rows[row].Tag = file.FullPath;
            }

            lblFileCount.Text = files.Count == 1 ? "1 file" : $"{files.Count} files";
            if (files.Count == 0)
                lblStatus.Text = "No audio/video files in this folder. Copy files here or Browse.";
            else
                lblStatus.Text = $"{files.Count} media file(s). Tick the ones to play.";

            PersistSelection();
            HighlightCurrentRow();
        }

        private void StartPlaylist()
        {
            List<string> selected = GetSelectedPaths();
            if (selected.Count == 0)
            {
                MessageBox.Show(
                    "Tick at least one audio or video file.",
                    "Media",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            gapTimer.Stop();
            playWatchTimer.Stop();
            waitingForGap = false;
            finishingCurrent = false;
            isPaused = false;
            failStreak = 0;
            currentPath = null;
            PlayPath(selected[0]);
        }

        private void TogglePause()
        {
            if (currentPath == null && !waitingForGap)
                return;

            try
            {
                if (waitingForGap)
                {
                    isPaused = !isPaused;
                    btnPause.Text = isPaused ? "Resume" : "Pause";
                    lblStatus.Text = isPaused
                        ? "Interval paused."
                        : $"File finished. Next in {gapSecondsLeft} sec…";
                    return;
                }

                if (isPaused)
                {
                    player.Play();
                    playWatchTimer.Start();
                    isPaused = false;
                    btnPause.Text = "Pause";
                    SetStatusPlaying();
                }
                else
                {
                    player.Pause();
                    playWatchTimer.Stop();
                    isPaused = true;
                    btnPause.Text = "Resume";
                    lblStatus.Text = "Paused.";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Could not pause/resume: " + ex.Message;
            }
        }

        private void StopPlayback(bool clearCurrent)
        {
            gapTimer.Stop();
            playWatchTimer.Stop();
            waitingForGap = false;
            finishingCurrent = false;
            gapSecondsLeft = 0;
            isPaused = false;
            btnPause.Text = "Pause";
            try
            {
                player.Stop();
                player.Close();
            }
            catch
            {
                // Ignore stop errors.
            }

            SetPlayerHeight(false);
            audioVisual.Visible = true;
            playerHost.Visible = false;
            lblNowPlaying.Visible = true;
            if (clearCurrent)
            {
                currentPath = null;
                lblNowPlaying.Text = "Nothing playing";
                lblStatus.Text = "Stopped.";
                HighlightCurrentRow();
            }
        }

        private void ResetAll()
        {
            StopPlayback(clearCurrent: true);
            SetAllSelected(false);
            settings.IntervalSeconds = 11;
            settings.Loop = true;
            suppressSave = true;
            numInterval.Value = 11;
            chkLoop.Checked = true;
            suppressSave = false;
            SaveSettings();
            lblStatus.Text = "Reset. Folder kept — tick files and press Play.";
        }

        private void CheckPlaybackEnded()
        {
            if (waitingForGap || isPaused || finishingCurrent || currentPath == null)
                return;

            if (!player.NaturalDuration.HasTimeSpan)
                return;

            TimeSpan duration = player.NaturalDuration.TimeSpan;
            if (duration.TotalMilliseconds <= 0)
                return;

            if (player.Position + TimeSpan.FromMilliseconds(300) >= duration)
                OnCurrentMediaFinished();
        }

        private void OnCurrentMediaFinished()
        {
            if (finishingCurrent || waitingForGap)
                return;
            if (isPaused)
                return;

            finishingCurrent = true;
            playWatchTimer.Stop();
            try
            {
                player.Pause();
                player.Stop();
            }
            catch
            {
                // File already ended.
            }

            int seconds = (int)numInterval.Value;
            if (seconds <= 0)
            {
                finishingCurrent = false;
                PlayNext();
                return;
            }

            waitingForGap = true;
            gapSecondsLeft = seconds;
            lblNowPlaying.Text = "Next soon  ·  " + Path.GetFileName(currentPath ?? "");
            SetPlayerHeight(false);
            audioVisual.Visible = true;
            playerHost.Visible = false;
            lblNowPlaying.Visible = true;
            lblStatus.Text = $"File finished. Next in {gapSecondsLeft} sec…";
            RefreshPlaylistHint();
            gapTimer.Start();
        }

        private void OnGapSecondElapsed()
        {
            if (isPaused)
                return;

            gapSecondsLeft--;
            if (gapSecondsLeft > 0)
            {
                RefreshPlaylistHint();
                return;
            }

            gapTimer.Stop();
            waitingForGap = false;
            finishingCurrent = false;
            PlayNext();
        }

        private void PlayNext()
        {
            List<string> queue = GetSelectedPaths();
            string? next = MediaLibrary.NextPath(queue, currentPath, loop: true);
            if (next == null)
            {
                StopPlayback(clearCurrent: true);
                lblStatus.Text = "Playlist finished.";
                return;
            }

            PlayPath(next);
        }

        private void OnMediaOpened()
        {
            failStreak = 0;
            if (player.NaturalVideoWidth > 0 && player.NaturalVideoHeight > 0)
                videoDrawing.Rect = new System.Windows.Rect(0, 0, player.NaturalVideoWidth, player.NaturalVideoHeight);
        }

        private void PlayPath(string path)
        {
            if (!File.Exists(path))
            {
                lblStatus.Text = "File missing, skipping: " + Path.GetFileName(path);
                currentPath = path;
                SkipAfterFailure();
                return;
            }

            try
            {
                gapTimer.Stop();
                playWatchTimer.Stop();
                waitingForGap = false;
                finishingCurrent = false;
                isPaused = false;
                btnPause.Text = "Pause";
                currentPath = path;

                player.Stop();
                player.Close();
                player.Volume = 1.0;
                player.Open(new Uri(path));
                player.Play();
                playWatchTimer.Start();

                bool audio = MediaLibrary.IsAudio(path);
                SetPlayerHeight(!audio);
                audioVisual.Visible = audio;
                playerHost.Visible = !audio;
                lblNowPlaying.Visible = audio;
                if (audio)
                    audioVisual.BringToFront();
                else
                    playerHost.BringToFront();
                lblAudioTitle.Text = Path.GetFileName(path);
                lblNowPlaying.Text = Path.GetFileName(path);
                SetStatusPlaying();
                HighlightCurrentRow();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Could not play " + Path.GetFileName(path) + ": " + ex.Message;
                currentPath = path;
                SkipAfterFailure();
            }
        }

        private void OnMediaFailed(Exception? error)
        {
            string name = currentPath == null ? "file" : Path.GetFileName(currentPath);
            lblStatus.Text = $"Could not play {name}" + (error == null ? "." : $": {error.Message}");
            SkipAfterFailure();
        }

        private void SkipAfterFailure()
        {
            failStreak++;
            if (failStreak >= Math.Max(1, GetSelectedPaths().Count))
            {
                StopPlayback(clearCurrent: true);
                lblStatus.Text = "Stopped — none of the selected files could play.";
                return;
            }

            PlayNext();
        }

        private void SetStatusPlaying()
        {
            int selected = GetSelectedPaths().Count;
            lblStatus.Text = $"Playing until file ends  ·  then {numInterval.Value}s  ·  {selected} selected";
        }

        private void RefreshPlaylistHint()
        {
            if (waitingForGap)
            {
                string? next = MediaLibrary.NextPath(GetSelectedPaths(), currentPath, loop: true);
                if (next == null)
                    lblStatus.Text = $"File finished. No file selected — will stop in {gapSecondsLeft} sec…";
                else
                    lblStatus.Text = $"File finished. Next in {gapSecondsLeft} sec…  ({Path.GetFileName(next)})";
                return;
            }

            if (currentPath != null && !isPaused)
                SetStatusPlaying();
        }

        private List<string> GetSelectedPaths()
        {
            var list = new List<string>();
            foreach (DataGridViewRow row in dgvFiles.Rows)
            {
                if (row.IsNewRow) continue;
                bool ticked = row.Cells["Selected"].Value is true;
                if (!ticked) continue;
                string? path = row.Cells["FullPath"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(path))
                    list.Add(path);
            }
            return list;
        }

        private void SetAllSelected(bool selected)
        {
            foreach (DataGridViewRow row in dgvFiles.Rows)
            {
                if (row.IsNewRow) continue;
                row.Cells["Selected"].Value = selected;
            }
            PersistSelection();
            RefreshPlaylistHint();
        }

        private void HighlightCurrentRow()
        {
            foreach (DataGridViewRow row in dgvFiles.Rows)
            {
                if (row.IsNewRow) continue;
                string? path = row.Cells["FullPath"].Value?.ToString();
                bool current = currentPath != null &&
                    string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase);
                row.DefaultCellStyle.BackColor = current
                    ? Color.FromArgb(204, 251, 241)
                    : (row.Index % 2 == 1 ? Color.FromArgb(248, 250, 252) : Color.White);
                row.DefaultCellStyle.Font = new Font("Segoe UI", 9.25f, current ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        private void PersistSelection()
        {
            if (suppressSave) return;
            settings.SelectedFiles.Clear();
            foreach (string path in GetSelectedPaths())
                settings.SelectedFiles.Add(Path.GetFileName(path));
            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                settings.FolderPath = txtFolder.Text;
                settings.IntervalSeconds = (int)numInterval.Value;
                settings.Loop = chkLoop.Checked;
                MediaSettingsStore.Save(settings);
            }
            catch
            {
                // Settings are optional; playback still works.
            }
        }

        private void SafeBeginInvoke(Action action)
        {
            if (IsDisposed)
                return;

            Control target = this;
            if (!IsHandleCreated)
            {
                Form? form = FindForm();
                if (form != null && form.IsHandleCreated && !form.IsDisposed)
                    target = form;
                else
                {
                    action();
                    return;
                }
            }

            if (target.InvokeRequired)
            {
                try { target.BeginInvoke(action); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

            action();
        }

        private void AudioVisual_Paint(object? sender, PaintEventArgs e)
        {
            if (audioVisual.Width < 8 || audioVisual.Height < 8)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = audioVisual.Width / 2;
            int cy = audioVisual.Height / 2;
            using SolidBrush ring = new SolidBrush(Color.FromArgb(50, 45, 212, 191));
            e.Graphics.FillEllipse(ring, cx - 18, cy - 18, 36, 36);
            using SolidBrush inner = new SolidBrush(Color.FromArgb(80, 13, 148, 136));
            e.Graphics.FillEllipse(inner, cx - 13, cy - 13, 26, 26);

            using Pen pen = new Pen(Color.FromArgb(165, 243, 252), 2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            Point[] play =
            {
                new Point(cx - 5, cy - 6),
                new Point(cx - 5, cy + 6),
                new Point(cx + 7, cy)
            };
            using SolidBrush playBrush = new SolidBrush(Color.FromArgb(165, 243, 252));
            e.Graphics.FillPolygon(playBrush, play);
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
                Width = 80,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            bar.Controls.Add(lblTitle);
            return bar;
        }

        private static Label SmallCaption(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Muted
            };
        }

        private static void StylePrimaryButton(Button btn, Color back)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = back;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shutdown();
                gapTimer.Dispose();
                playWatchTimer.Dispose();
                playerHost.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
