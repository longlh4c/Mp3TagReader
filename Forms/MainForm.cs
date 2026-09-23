using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Mp3TagReader.Helpers;
using Mp3TagReader.Models;
using Mp3TagReader.Services;

namespace Mp3TagReader.Forms
{
    // Main window. The code is split by feature:
    //   MainForm.cs         setup, menu commands that open other windows, layout
    //   MainForm.Browse.cs  file list: folders, history, search, tree, file operations
    //   MainForm.Tags.cs    tag editor, cover art, tag tools
    //   MainForm.Player.cs  music player (bundled Libs\ffplay.exe)
    public partial class MainForm : Form
    {
        private readonly FileExplorer fe = new FileExplorer();
        private readonly MySortableBindingList<Mp3Info> listMp3Infos = new MySortableBindingList<Mp3Info>();

        private GroupBox playerGroupBox;
        private Label lblTotalTime;

        public MainForm()
        {
            InitializeComponent();
            InitializeModernLayout();
            this.FormClosing += MainForm_FormClosing;
            treeViewFolder.AfterSelect += treeViewFolder_AfterSelect;
            listMp3Infos.KeepOnTop = delegate(Mp3Info item) { return item is ParentFolderInfo; };
            InitializeSearch();
            InitializePlayer();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                fe.CreateTree(treeViewFolder);

                gridView.AutoGenerateColumns = false;
                DataGridViewTextBoxColumn columnFilePath = new DataGridViewTextBoxColumn();
                columnFilePath.DataPropertyName = "Path";
                columnFilePath.Name = "ColumnPath";
                columnFilePath.Visible = false;
                DataGridViewTextBoxColumn columnFileName = new DataGridViewTextBoxColumn();
                columnFileName.DataPropertyName = "Name";
                columnFileName.Name = "ColumnName";
                columnFileName.HeaderText = "File Name";
                columnFileName.Width = gridView.Width - 180;
                DataGridViewTextBoxColumn columnFileDate = new DataGridViewTextBoxColumn();
                columnFileDate.DataPropertyName = "CreationTime";
                columnFileDate.HeaderText = "Creation Time";
                columnFileDate.Width = 130;

                gridView.Columns.Add(columnFilePath);
                gridView.Columns.Add(columnFileName);
                gridView.Columns.Add(columnFileDate);

                reloadFolderList();
            }
            catch
            {
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisposePlayer();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void convertToMp3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = CurrentRowPath();
            if (path == null || !File.Exists(path))
            {
                MessageBox.Show("Please select a file to convert.", "Warning");
                return;
            }

            using (Mp3ConvertForm convertForm = new Mp3ConvertForm(path))
            {
                convertForm.ShowDialog(this);
            }

            // show the new mp3 file
            RefreshList();
        }

        private void downloadMP3FromYouTubeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string initialFolder = Directory.Exists(currentFolder) ? currentFolder : cbbFilePath.Text;
            using (YoutubeDownloadForm downloadForm = new YoutubeDownloadForm(initialFolder))
            {
                if (downloadForm.ShowDialog(this) == DialogResult.OK)
                {
                    ListFolder(downloadForm.OutputFolder);
                }
            }
        }

        private void attributeChangerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = CurrentRowPath();
            using (AttributeChangerForm changer = new AttributeChangerForm(path != null && File.Exists(path) ? path : string.Empty))
            {
                if (changer.ShowDialog(this) == DialogResult.OK)
                {
                    // show the modified creation dates / timestamps
                    RefreshList();
                }
            }
        }

        private void subtitleDownloaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // the form searches by the file name, so only pass a file (not a folder or "..")
            string path = CurrentRowPath();
            using (SubtitleDownloaderForm downloader = new SubtitleDownloaderForm(path != null && File.Exists(path) ? path : string.Empty))
            {
                downloader.ShowDialog(this);
            }
        }

        private void InitializeModernLayout()
        {
            this.SuspendLayout();

            // Clear size constraints first so we can resize
            this.MaximumSize = new Size(0, 0);
            this.MinimumSize = new Size(0, 0);

            // Configure MainForm Size to accommodate split panels with full button texts and extra height
            this.ClientSize = new Size(1150, 570);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Mp3TagReader";

            // Now lock it to the new size
            this.MinimumSize = this.Size;
            this.MaximumSize = this.Size;

            // Reset Dock styles so Bounds are respected
            treeViewFolder.Dock = DockStyle.None;
            gridView.Dock = DockStyle.None;

            // Create Left Panel (Explorer & Files Grid)
            Panel leftPanel = new Panel();
            leftPanel.Bounds = new Rectangle(0, 24, 680, 546);
            leftPanel.BackColor = SystemColors.Control;
            
            // Create Right Panel (Tag Editor & Player)
            Panel rightPanel = new Panel();
            rightPanel.Bounds = new Rectangle(680, 24, 470, 546);
            rightPanel.BackColor = SystemColors.Control;

            // Add panels to Form
            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);

            // Move Left Panel controls
            leftPanel.Controls.Add(cbbFilePath);
            leftPanel.Controls.Add(_btnList);
            leftPanel.Controls.Add(_btnSearch);
            leftPanel.Controls.Add(_btnUntagged);
            leftPanel.Controls.Add(_chkAllFiles);
            leftPanel.Controls.Add(treeViewFolder);
            leftPanel.Controls.Add(gridView);
            leftPanel.Controls.Add(_lblCount);
            leftPanel.Controls.Add(lblResult); // Add lblResult (selected count) to left panel

            // Reposition Left Panel controls
            cbbFilePath.Bounds = new Rectangle(10, 10, 200, 21);
            
            _btnList.Bounds = new Rectangle(330, 9, 100, 23);
            _btnList.Text = "List Files";
            
            _btnSearch.Bounds = new Rectangle(440, 9, 110, 23);
            _btnSearch.Text = "Search OFF";
            
            _btnUntagged.Bounds = new Rectangle(560, 9, 110, 23);
            _btnUntagged.Text = "Untagged";
            
            treeViewFolder.Bounds = new Rectangle(10, 42, 160, 460);
            gridView.Bounds = new Rectangle(175, 42, 495, 460);
            _chkAllFiles.Bounds = new Rectangle(10, 512, 160, 20);
            
            // Put total count on the left of the table
            _lblCount.Bounds = new Rectangle(175, 512, 150, 20);

            // Move Right Panel controls
            rightPanel.Controls.Add(picBxArtwork);
            rightPanel.Controls.Add(btnSetArtwork);
            rightPanel.Controls.Add(btnUnsetArtwork);
            rightPanel.Controls.Add(btnSaveArtwork);
            rightPanel.Controls.Add(lblArtwork);
            
            rightPanel.Controls.Add(lblTitle);
            rightPanel.Controls.Add(txtTitle);
            rightPanel.Controls.Add(lblArtist);
            rightPanel.Controls.Add(txtArtist);
            rightPanel.Controls.Add(lblAlbum);
            rightPanel.Controls.Add(txtAlbum);
            rightPanel.Controls.Add(lblGenre);
            rightPanel.Controls.Add(txtGenre);
            rightPanel.Controls.Add(lblYear);
            rightPanel.Controls.Add(txtYear);
            rightPanel.Controls.Add(lblTrack);
            rightPanel.Controls.Add(txtTrack);
            rightPanel.Controls.Add(label1);
            rightPanel.Controls.Add(txtBitrate);
            rightPanel.Controls.Add(lblComments);
            rightPanel.Controls.Add(txtComments);
            rightPanel.Controls.Add(_btnGetLyrics); // Add _btnGetLyrics back
            rightPanel.Controls.Add(_btnClearLyrics); // Add _btnClearLyrics back
            rightPanel.Controls.Add(txtLyrics);

            rightPanel.Controls.Add(_btnSave);
            rightPanel.Controls.Add(_btnInfo);
            rightPanel.Controls.Add(lblResult);

            playerGroupBox = new GroupBox();
            playerGroupBox.Text = "Music Player";
            rightPanel.Controls.Add(playerGroupBox);

            playerGroupBox.Controls.Add(progressBar);
            playerGroupBox.Controls.Add(labelTimerSong);
            playerGroupBox.Controls.Add(_btnPrev);
            playerGroupBox.Controls.Add(_btnPlayAll);
            playerGroupBox.Controls.Add(_btnNext);
            playerGroupBox.Controls.Add(_btnStop);

            playerGroupBox.Controls.Add(_chkShuffle);
            playerGroupBox.Controls.Add(_chkReplay);
            playerGroupBox.Controls.Add(_btnDisableTimerSong);

            // Reposition Right Panel controls (wider inspector spacing)
            picBxArtwork.Bounds = new Rectangle(15, 10, 100, 100);
            picBxArtwork.SizeMode = PictureBoxSizeMode.Zoom;
            
            btnSetArtwork.Bounds = new Rectangle(125, 10, 100, 26);
            btnUnsetArtwork.Bounds = new Rectangle(125, 42, 100, 26);
            btnSaveArtwork.Bounds = new Rectangle(125, 74, 100, 26);
            lblArtwork.Bounds = new Rectangle(235, 10, 220, 90);
            lblArtwork.AutoSize = false;
            
            lblTitle.Bounds = new Rectangle(15, 122, 50, 15);
            txtTitle.Bounds = new Rectangle(75, 119, 380, 21);
            
            lblArtist.Bounds = new Rectangle(15, 149, 50, 15);
            txtArtist.Bounds = new Rectangle(75, 146, 380, 21);
            
            lblAlbum.Bounds = new Rectangle(15, 176, 50, 15);
            txtAlbum.Bounds = new Rectangle(75, 173, 380, 21);
            
            lblGenre.Bounds = new Rectangle(15, 203, 50, 15);
            txtGenre.Bounds = new Rectangle(75, 200, 150, 21);
            lblYear.Bounds = new Rectangle(245, 203, 50, 15);
            txtYear.Bounds = new Rectangle(305, 200, 150, 21);
            
            lblTrack.Bounds = new Rectangle(15, 230, 50, 15);
            txtTrack.Bounds = new Rectangle(75, 227, 150, 21);
            label1.Bounds = new Rectangle(245, 230, 50, 15);
            txtBitrate.Bounds = new Rectangle(305, 227, 150, 21);
            
            lblComments.Bounds = new Rectangle(15, 257, 50, 15);
            txtComments.Bounds = new Rectangle(75, 254, 380, 21);
            
            // Lyrics buttons row and lyrics text area
            _btnGetLyrics.Bounds = new Rectangle(15, 282, 100, 23);
            _btnGetLyrics.Text = "LyricsURL";
            _btnClearLyrics.Bounds = new Rectangle(120, 282, 30, 23);
            _btnClearLyrics.Text = "X";
            
            // Put Full info button on the far right of the lyrics row (red circle area)
            _btnInfo.Bounds = new Rectangle(305, 282, 150, 23);
            _btnInfo.Text = "Full info";
            
            txtLyrics.Bounds = new Rectangle(15, 310, 440, 50);
            txtLyrics.Multiline = true;
            txtLyrics.ScrollBars = ScrollBars.Vertical;
            
            // Save Tag button directly below lyrics area: shrunk to half (220px) and centered (X=125) relative to 470px width
            _btnSave.Bounds = new Rectangle(125, 365, 220, 32);
            _btnSave.Text = "Save Tag";

            // Put selected count to the left of the Save Tag button
            lblResult.AutoSize = false;
            lblResult.Bounds = new Rectangle(15, 365, 105, 32);
            lblResult.TextAlign = ContentAlignment.MiddleLeft;
            
            // Player GroupBox container
            playerGroupBox.Bounds = new Rectangle(15, 405, 440, 125);

            // Playback controls row at top of GroupBox:
            // Layout: [⏮ Prev] [▶ Play] [⏭ Next] [⏹ Stop]   [⧁ Shuffle][↺ Replay]
            _btnPrev.Bounds = new Rectangle(75, 20, 42, 34);
            _btnPrev.Text = "\u23EE";
            _btnPlayAll.Bounds = new Rectangle(121, 16, 50, 40);
            _btnPlayAll.Text = "\u25B6";
            _btnNext.Bounds = new Rectangle(175, 20, 42, 34);
            _btnNext.Text = "\u23ED";
            _btnStop.Bounds = new Rectangle(221, 20, 42, 34);
            _btnStop.Text = "\u23F9";

            // Shuffle and Replay flush to right edge of GroupBox (inner width ~436px)
            _chkShuffle.Appearance = Appearance.Button;
            _chkShuffle.TextAlign = ContentAlignment.MiddleCenter;
            _chkShuffle.Bounds = new Rectangle(328, 20, 42, 34);
            _chkShuffle.Text = "\u29C1";

            _chkReplay.Appearance = Appearance.Button;
            _chkReplay.TextAlign = ContentAlignment.MiddleCenter;
            _chkReplay.Bounds = new Rectangle(374, 20, 42, 34);
            _chkReplay.Text = "\u21BA";
            
            // Progress Bar seeker below controls
            progressBar.Bounds = new Rectangle(75, 66, 280, 10);
            labelTimerSong.Bounds = new Rectangle(10, 64, 62, 15);
            labelTimerSong.TextAlign = ContentAlignment.MiddleLeft;

            lblTotalTime = new Label();
            lblTotalTime.Name = "lblTotalTime";
            lblTotalTime.Bounds = new Rectangle(360, 64, 60, 15);
            lblTotalTime.TextAlign = ContentAlignment.MiddleRight;
            lblTotalTime.Text = "0:00";
            if (!playerGroupBox.Controls.ContainsKey("lblTotalTime"))
            {
                playerGroupBox.Controls.Add(lblTotalTime);
            }
            
            // Bottom row: Find Playing Song button
            _btnDisableTimerSong.Bounds = new Rectangle(10, 92, 420, 23);
            _btnDisableTimerSong.Text = "\u27A4 Find Playing Song";

            // Ensure Menu bar remains on top
            menuStrip1.BringToFront();

            this.ResumeLayout(true);
            this.PerformLayout();
        }
    }
}
