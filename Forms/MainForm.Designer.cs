namespace Mp3TagReader.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.txtAlbum = new System.Windows.Forms.TextBox();
            this.txtTitle = new System.Windows.Forms.TextBox();
            this.txtArtist = new System.Windows.Forms.TextBox();
            this.txtYear = new System.Windows.Forms.TextBox();
            this.txtComments = new System.Windows.Forms.TextBox();
            this.lblArtist = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblAlbum = new System.Windows.Forms.Label();
            this.lblYear = new System.Windows.Forms.Label();
            this.lblComments = new System.Windows.Forms.Label();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this._btnSave = new System.Windows.Forms.Button();
            this.gridView = new System.Windows.Forms.DataGridView();
            this.lblGenre = new System.Windows.Forms.Label();
            this.txtGenre = new System.Windows.Forms.TextBox();
            this.txtLyrics = new System.Windows.Forms.TextBox();
            this.lblResult = new System.Windows.Forms.Label();
            this._btnStop = new System.Windows.Forms.Button();
            this._lblCount = new System.Windows.Forms.Label();
            this.lblTrack = new System.Windows.Forms.Label();
            this.txtTrack = new System.Windows.Forms.TextBox();
            this._btnList = new System.Windows.Forms.Button();
            this._btnSearch = new System.Windows.Forms.Button();
            this._chkReplay = new System.Windows.Forms.CheckBox();
            this._btnUntagged = new System.Windows.Forms.Button();
            this._btnPlayAll = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fixTrackNumberToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.downloadMP3FromYouTubeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeChangerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._btnGetLyrics = new System.Windows.Forms.Button();
            this._btnInfo = new System.Windows.Forms.Button();
            this.timerSong = new System.Windows.Forms.Timer(this.components);
            this._btnCopy = new System.Windows.Forms.Button();
            this.timerNowPlayingText = new System.Windows.Forms.Timer(this.components);
            this._btnClearLyrics = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this._btnNext = new System.Windows.Forms.Button();
            this._btnPrev = new System.Windows.Forms.Button();
            this._chkShuffle = new System.Windows.Forms.CheckBox();

            this.imageListFolder = new System.Windows.Forms.ImageList(this.components);
            this.contextMenuFolder = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.treeViewFolder = new System.Windows.Forms.TreeView();
            this._btnDisableTimerSong = new System.Windows.Forms.Button();
            this.btnVolumeUp = new System.Windows.Forms.Button();
            this.btnVolumeDown = new System.Windows.Forms.Button();
            this.btnMute = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtBitrate = new System.Windows.Forms.TextBox();
            this.contextGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.convertToMp3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelTimerSong = new System.Windows.Forms.Label();
            this._btnOpenFolder = new System.Windows.Forms.Button();
            this.cbbFilePath = new System.Windows.Forms.ComboBox();
            this.picBxArtwork = new System.Windows.Forms.PictureBox();
            this.btnSetArtwork = new System.Windows.Forms.Button();
            this.btnSaveArtwork = new System.Windows.Forms.Button();
            this.btnUnsetArtwork = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.lblArtwork = new System.Windows.Forms.Label();
            this.btnReloadList = new System.Windows.Forms.Button();
            this.lblSelectedFilePath = new System.Windows.Forms.Label();
            this._chkAllFiles = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.gridView)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.contextMenuFolder.SuspendLayout();
            this.contextGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picBxArtwork)).BeginInit();
            this.SuspendLayout();
            // 
            // txtAlbum
            // 
            this.txtAlbum.Location = new System.Drawing.Point(550, 190);
            this.txtAlbum.Name = "txtAlbum";
            this.txtAlbum.Size = new System.Drawing.Size(194, 20);
            this.txtAlbum.TabIndex = 6;
            // 
            // txtTitle
            // 
            this.txtTitle.Location = new System.Drawing.Point(550, 164);
            this.txtTitle.Name = "txtTitle";
            this.txtTitle.Size = new System.Drawing.Size(194, 20);
            this.txtTitle.TabIndex = 5;
            // 
            // txtArtist
            // 
            this.txtArtist.Location = new System.Drawing.Point(550, 138);
            this.txtArtist.Name = "txtArtist";
            this.txtArtist.Size = new System.Drawing.Size(194, 20);
            this.txtArtist.TabIndex = 4;
            // 
            // txtYear
            // 
            this.txtYear.Location = new System.Drawing.Point(550, 242);
            this.txtYear.Name = "txtYear";
            this.txtYear.Size = new System.Drawing.Size(194, 20);
            this.txtYear.TabIndex = 8;
            // 
            // txtComments
            // 
            this.txtComments.Location = new System.Drawing.Point(550, 376);
            this.txtComments.Multiline = true;
            this.txtComments.Name = "txtComments";
            this.txtComments.Size = new System.Drawing.Size(194, 36);
            this.txtComments.TabIndex = 11;
            // 
            // lblArtist
            // 
            this.lblArtist.AutoSize = true;
            this.lblArtist.Location = new System.Drawing.Point(492, 141);
            this.lblArtist.Name = "lblArtist";
            this.lblArtist.Size = new System.Drawing.Size(30, 13);
            this.lblArtist.TabIndex = 6;
            this.lblArtist.Text = "Artist";
            this.lblArtist.Click += new System.EventHandler(this.lblArtist_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(492, 167);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(27, 13);
            this.lblTitle.TabIndex = 7;
            this.lblTitle.Text = "Title";
            this.lblTitle.Click += new System.EventHandler(this.lblTitle_Click);
            // 
            // lblAlbum
            // 
            this.lblAlbum.AutoSize = true;
            this.lblAlbum.Location = new System.Drawing.Point(492, 193);
            this.lblAlbum.Name = "lblAlbum";
            this.lblAlbum.Size = new System.Drawing.Size(36, 13);
            this.lblAlbum.TabIndex = 8;
            this.lblAlbum.Text = "Album";
            this.lblAlbum.Click += new System.EventHandler(this.lblAlbum_Click);
            // 
            // lblYear
            // 
            this.lblYear.AutoSize = true;
            this.lblYear.Location = new System.Drawing.Point(492, 245);
            this.lblYear.Name = "lblYear";
            this.lblYear.Size = new System.Drawing.Size(29, 13);
            this.lblYear.TabIndex = 9;
            this.lblYear.Text = "Year";
            this.lblYear.Click += new System.EventHandler(this.lblYear_Click);
            // 
            // lblComments
            // 
            this.lblComments.AutoSize = true;
            this.lblComments.Location = new System.Drawing.Point(492, 380);
            this.lblComments.Name = "lblComments";
            this.lblComments.Size = new System.Drawing.Size(56, 13);
            this.lblComments.TabIndex = 10;
            this.lblComments.Text = "Comments";
            this.lblComments.Click += new System.EventHandler(this.lblComments_Click);
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.SelectedPath = "E:\\Music";
            // 
            // _btnSave
            // 
            this._btnSave.Location = new System.Drawing.Point(495, 476);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(75, 45);
            this._btnSave.TabIndex = 12;
            this._btnSave.Text = "Save Tag";
            this._btnSave.UseVisualStyleBackColor = true;
            this._btnSave.Click += new System.EventHandler(this._btnSave_Click);
            // 
            // gridView
            // 
            this.gridView.AllowDrop = true;
            this.gridView.AllowUserToAddRows = false;
            this.gridView.AllowUserToResizeRows = false;
            this.gridView.BackgroundColor = System.Drawing.SystemColors.ControlDark;
            this.gridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnF2;
            this.gridView.GridColor = System.Drawing.SystemColors.ButtonFace;
            this.gridView.Location = new System.Drawing.Point(12, 33);
            this.gridView.Name = "gridView";
            this.gridView.ReadOnly = true;
            this.gridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.gridView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.gridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridView.Size = new System.Drawing.Size(472, 306);
            this.gridView.TabIndex = 0;
            this.gridView.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.gridView_CellBeginEdit);
            this.gridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridView_CellClick);
            this.gridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridView_CellDoubleClick);
            this.gridView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridView_CellEndEdit);
            this.gridView.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.gridView_DataBindingComplete);
            this.gridView.SelectionChanged += new System.EventHandler(this.gridView_SelectionChanged);
            this.gridView.DragDrop += new System.Windows.Forms.DragEventHandler(this.gridView_DragDrop);
            this.gridView.DragEnter += new System.Windows.Forms.DragEventHandler(this.gridView_DragEnter);
            this.gridView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.gridView_MouseDown);
            // 
            // lblGenre
            // 
            this.lblGenre.AutoSize = true;
            this.lblGenre.Location = new System.Drawing.Point(492, 271);
            this.lblGenre.Name = "lblGenre";
            this.lblGenre.Size = new System.Drawing.Size(36, 13);
            this.lblGenre.TabIndex = 18;
            this.lblGenre.Text = "Genre";
            this.lblGenre.Click += new System.EventHandler(this.lblGenre_Click);
            // 
            // txtGenre
            // 
            this.txtGenre.Location = new System.Drawing.Point(550, 268);
            this.txtGenre.Name = "txtGenre";
            this.txtGenre.Size = new System.Drawing.Size(194, 20);
            this.txtGenre.TabIndex = 9;
            // 
            // txtLyrics
            // 
            this.txtLyrics.Location = new System.Drawing.Point(550, 334);
            this.txtLyrics.Multiline = true;
            this.txtLyrics.Name = "txtLyrics";
            this.txtLyrics.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLyrics.Size = new System.Drawing.Size(164, 36);
            this.txtLyrics.TabIndex = 10;
            this.txtLyrics.DoubleClick += new System.EventHandler(this.txtLyrics_DoubleClick);
            // 
            // lblResult
            // 
            this.lblResult.AutoSize = true;
            this.lblResult.Location = new System.Drawing.Point(494, 455);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(0, 13);
            this.lblResult.TabIndex = 21;
            // 
            // _btnStop
            // 
            this._btnStop.Location = new System.Drawing.Point(413, 482);
            this._btnStop.Name = "_btnStop";
            this._btnStop.Size = new System.Drawing.Size(47, 39);
            this._btnStop.TabIndex = 24;
            this._btnStop.Text = "Stop";
            this._btnStop.UseVisualStyleBackColor = true;
            this._btnStop.Click += new System.EventHandler(this._btnStop_Click);
            // 
            // _lblCount
            // 
            this._lblCount.AutoSize = true;
            this._lblCount.Location = new System.Drawing.Point(12, 342);
            this._lblCount.Name = "_lblCount";
            this._lblCount.Size = new System.Drawing.Size(13, 13);
            this._lblCount.TabIndex = 25;
            this._lblCount.Text = "0";
            // 
            // lblTrack
            // 
            this.lblTrack.AutoSize = true;
            this.lblTrack.Location = new System.Drawing.Point(492, 219);
            this.lblTrack.Name = "lblTrack";
            this.lblTrack.Size = new System.Drawing.Size(35, 13);
            this.lblTrack.TabIndex = 27;
            this.lblTrack.Text = "Track";
            this.lblTrack.Click += new System.EventHandler(this.lblTrack_Click);
            // 
            // txtTrack
            // 
            this.txtTrack.Location = new System.Drawing.Point(550, 216);
            this.txtTrack.Name = "txtTrack";
            this.txtTrack.Size = new System.Drawing.Size(194, 20);
            this.txtTrack.TabIndex = 7;
            // 
            // _btnList
            // 
            this._btnList.Location = new System.Drawing.Point(616, 60);
            this._btnList.Name = "_btnList";
            this._btnList.Size = new System.Drawing.Size(62, 27);
            this._btnList.TabIndex = 3;
            this._btnList.Text = "List";
            this._btnList.UseVisualStyleBackColor = true;
            this._btnList.Click += new System.EventHandler(this._btnList_Click);
            // 
            // _btnSearch
            // 
            this._btnSearch.Location = new System.Drawing.Point(681, 93);
            this._btnSearch.Name = "_btnSearch";
            this._btnSearch.Size = new System.Drawing.Size(63, 39);
            this._btnSearch.TabIndex = 29;
            this._btnSearch.Text = "Search OFF";
            this._btnSearch.UseVisualStyleBackColor = true;
            this._btnSearch.Click += new System.EventHandler(this._btnSearch_Click);
            // 
            // _chkReplay
            // 
            this._chkReplay.AutoSize = true;
            this._chkReplay.Location = new System.Drawing.Point(328, 504);
            this._chkReplay.Name = "_chkReplay";
            this._chkReplay.Size = new System.Drawing.Size(59, 17);
            this._chkReplay.TabIndex = 30;
            this._chkReplay.Text = "Replay";
            this._chkReplay.UseVisualStyleBackColor = true;
            this._chkReplay.CheckedChanged += new System.EventHandler(this.chkReplay_CheckedChanged);
            // 
            // _btnUntagged
            // 
            this._btnUntagged.Location = new System.Drawing.Point(616, 93);
            this._btnUntagged.Name = "_btnUntagged";
            this._btnUntagged.Size = new System.Drawing.Size(62, 39);
            this._btnUntagged.TabIndex = 33;
            this._btnUntagged.Text = "Untagged Music";
            this._btnUntagged.UseVisualStyleBackColor = true;
            this._btnUntagged.Click += new System.EventHandler(this._btnUntagged_Click);
            // 
            // _btnPlayAll
            // 
            this._btnPlayAll.Location = new System.Drawing.Point(114, 483);
            this._btnPlayAll.Name = "_btnPlayAll";
            this._btnPlayAll.Size = new System.Drawing.Size(59, 39);
            this._btnPlayAll.TabIndex = 37;
            this._btnPlayAll.Text = "Play";
            this._btnPlayAll.UseVisualStyleBackColor = true;
            this._btnPlayAll.Click += new System.EventHandler(this._btnPlayList_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(946, 24);
            this.menuStrip1.TabIndex = 38;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuToolStripMenuItem
            // 
            this.menuToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fixTrackNumberToolStripMenuItem,
            this.downloadMP3FromYouTubeToolStripMenuItem,
            this.attributeChangerToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.menuToolStripMenuItem.Name = "menuToolStripMenuItem";
            this.menuToolStripMenuItem.Size = new System.Drawing.Size(50, 20);
            this.menuToolStripMenuItem.Text = "Menu";
            // 
            // fixTrackNumberToolStripMenuItem
            // 
            this.fixTrackNumberToolStripMenuItem.Name = "fixTrackNumberToolStripMenuItem";
            this.fixTrackNumberToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.fixTrackNumberToolStripMenuItem.Text = "Fix Track Number";
            this.fixTrackNumberToolStripMenuItem.Click += new System.EventHandler(this.fixTrackNumberToolStripMenuItem_Click);
            // 
            // downloadMP3FromYouTubeToolStripMenuItem
            // 
            this.downloadMP3FromYouTubeToolStripMenuItem.Name = "downloadMP3FromYouTubeToolStripMenuItem";
            this.downloadMP3FromYouTubeToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.downloadMP3FromYouTubeToolStripMenuItem.Text = "Download MP3 from YouTube...";
            this.downloadMP3FromYouTubeToolStripMenuItem.Click += new System.EventHandler(this.downloadMP3FromYouTubeToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // attributeChangerToolStripMenuItem
            // 
            this.attributeChangerToolStripMenuItem.Name = "attributeChangerToolStripMenuItem";
            this.attributeChangerToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.attributeChangerToolStripMenuItem.Text = "Attribute Changer...";
            this.attributeChangerToolStripMenuItem.Click += new System.EventHandler(this.attributeChangerToolStripMenuItem_Click);
            // 
            // _btnGetLyrics
            // 
            this._btnGetLyrics.Location = new System.Drawing.Point(495, 334);
            this._btnGetLyrics.Name = "_btnGetLyrics";
            this._btnGetLyrics.Size = new System.Drawing.Size(42, 36);
            this._btnGetLyrics.TabIndex = 40;
            this._btnGetLyrics.Text = "LyricsURL";
            this._btnGetLyrics.UseVisualStyleBackColor = true;
            this._btnGetLyrics.Click += new System.EventHandler(this._btnGetLyrics_Click);
            // 
            // _btnInfo
            // 
            this._btnInfo.Location = new System.Drawing.Point(598, 476);
            this._btnInfo.Name = "_btnInfo";
            this._btnInfo.Size = new System.Drawing.Size(75, 45);
            this._btnInfo.TabIndex = 41;
            this._btnInfo.Text = "Full info";
            this._btnInfo.UseVisualStyleBackColor = true;
            this._btnInfo.Click += new System.EventHandler(this._btnInfo_Click);
            // 
            // timerSong
            // 
            this.timerSong.Enabled = true;
            this.timerSong.Interval = 1000;
            this.timerSong.Tick += new System.EventHandler(this.timerSong_Tick);
            // 
            // _btnCopy
            // 
            this._btnCopy.Location = new System.Drawing.Point(550, 60);
            this._btnCopy.Name = "_btnCopy";
            this._btnCopy.Size = new System.Drawing.Size(62, 26);
            this._btnCopy.TabIndex = 42;
            this._btnCopy.Text = "Copy";
            this._btnCopy.UseVisualStyleBackColor = true;
            this._btnCopy.Click += new System.EventHandler(this._btnCopy_Click);
            // 
            // timerNowPlayingText
            // 
            this.timerNowPlayingText.Enabled = true;
            this.timerNowPlayingText.Interval = 200;
            this.timerNowPlayingText.Tick += new System.EventHandler(this.timerNowPlaying_Tick);
            // 
            // _btnClearLyrics
            // 
            this._btnClearLyrics.Location = new System.Drawing.Point(720, 334);
            this._btnClearLyrics.Name = "_btnClearLyrics";
            this._btnClearLyrics.Size = new System.Drawing.Size(24, 36);
            this._btnClearLyrics.TabIndex = 43;
            this._btnClearLyrics.Text = "X";
            this._btnClearLyrics.UseVisualStyleBackColor = true;
            this._btnClearLyrics.Click += new System.EventHandler(this._btnClearLyrics_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "mp3";
            this.openFileDialog.Filter = "All Files (*.*)|*.*";
            this.openFileDialog.InitialDirectory = "E:\\";
            this.openFileDialog.Multiselect = true;

            // _btnNext
            // 
            this._btnNext.Location = new System.Drawing.Point(179, 483);
            this._btnNext.Name = "_btnNext";
            this._btnNext.Size = new System.Drawing.Size(59, 39);
            this._btnNext.TabIndex = 45;
            this._btnNext.Text = "Next";
            this._btnNext.UseVisualStyleBackColor = true;
            this._btnNext.Click += new System.EventHandler(this._btnNext_Click);
            // 
            // _btnPrev
            // 
            this._btnPrev.Location = new System.Drawing.Point(49, 483);
            this._btnPrev.Name = "_btnPrev";
            this._btnPrev.Size = new System.Drawing.Size(59, 39);
            this._btnPrev.TabIndex = 46;
            this._btnPrev.Text = "Previous";
            this._btnPrev.UseVisualStyleBackColor = true;
            this._btnPrev.Click += new System.EventHandler(this._btnPrev_Click);
            // 
            // _chkShuffle
            // 
            this._chkShuffle.AutoSize = true;
            this._chkShuffle.Location = new System.Drawing.Point(328, 483);
            this._chkShuffle.Name = "_chkShuffle";
            this._chkShuffle.Size = new System.Drawing.Size(59, 17);
            this._chkShuffle.TabIndex = 47;
            this._chkShuffle.Text = "Shuffle";
            this._chkShuffle.UseVisualStyleBackColor = true;
            this._chkShuffle.CheckedChanged += new System.EventHandler(this._chkShuffle_CheckedChanged);

            // 
            // imageListFolder
            // 
            this.imageListFolder.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListFolder.ImageStream")));
            this.imageListFolder.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListFolder.Images.SetKeyName(0, "folder.png");
            this.imageListFolder.Images.SetKeyName(1, "folder.png");
            this.imageListFolder.Images.SetKeyName(2, "document.png");
            // 
            // contextMenuFolder
            // 
            this.contextMenuFolder.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.openFolderToolStripMenuItem});
            this.contextMenuFolder.Name = "contextMenuFolder";
            this.contextMenuFolder.Size = new System.Drawing.Size(140, 48);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.copyToolStripMenuItem.Text = "List Files";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // openFolderToolStripMenuItem
            // 
            this.openFolderToolStripMenuItem.Name = "openFolderToolStripMenuItem";
            this.openFolderToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.openFolderToolStripMenuItem.Text = "Open Folder";
            this.openFolderToolStripMenuItem.Click += new System.EventHandler(this.openFolderToolStripMenuItem_Click);
            // 
            // treeViewFolder
            // 
            this.treeViewFolder.ContextMenuStrip = this.contextMenuFolder;
            this.treeViewFolder.Dock = System.Windows.Forms.DockStyle.Right;
            this.treeViewFolder.ImageIndex = 0;
            this.treeViewFolder.ImageList = this.imageListFolder;
            this.treeViewFolder.Location = new System.Drawing.Point(750, 24);
            this.treeViewFolder.Name = "treeViewFolder";
            this.treeViewFolder.SelectedImageIndex = 0;
            this.treeViewFolder.Size = new System.Drawing.Size(196, 506);
            this.treeViewFolder.TabIndex = 50;
            this.treeViewFolder.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewFolder_BeforeExpand);
            this.treeViewFolder.MouseUp += new System.Windows.Forms.MouseEventHandler(this.treeViewFolder_MouseUp);
            // 
            // _btnDisableTimerSong
            // 
            this._btnDisableTimerSong.Location = new System.Drawing.Point(352, 5);
            this._btnDisableTimerSong.Name = "_btnDisableTimerSong";
            this._btnDisableTimerSong.Size = new System.Drawing.Size(132, 22);
            this._btnDisableTimerSong.TabIndex = 51;
            this._btnDisableTimerSong.Text = "Disable Auto Find Song";
            this._btnDisableTimerSong.UseVisualStyleBackColor = true;
            this._btnDisableTimerSong.Click += new System.EventHandler(this._btnDisableTimerSong_Click);
            // 
            // btnVolumeUp
            // 
            this.btnVolumeUp.Location = new System.Drawing.Point(235, 5);
            this.btnVolumeUp.Name = "btnVolumeUp";
            this.btnVolumeUp.Size = new System.Drawing.Size(59, 23);
            this.btnVolumeUp.TabIndex = 54;
            this.btnVolumeUp.Text = "Volume +";
            this.btnVolumeUp.UseVisualStyleBackColor = true;

            // 
            // btnVolumeDown
            // 
            this.btnVolumeDown.Location = new System.Drawing.Point(170, 5);
            this.btnVolumeDown.Name = "btnVolumeDown";
            this.btnVolumeDown.Size = new System.Drawing.Size(59, 23);
            this.btnVolumeDown.TabIndex = 55;
            this.btnVolumeDown.Text = "Volume -";
            this.btnVolumeDown.UseVisualStyleBackColor = true;

            // 
            // btnMute
            // 
            this.btnMute.Location = new System.Drawing.Point(299, 5);
            this.btnMute.Name = "btnMute";
            this.btnMute.Size = new System.Drawing.Size(47, 23);
            this.btnMute.TabIndex = 56;
            this.btnMute.Text = "Mute";
            this.btnMute.UseVisualStyleBackColor = true;

            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(492, 297);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 58;
            this.label1.Text = "Bitrate";
            // 
            // txtBitrate
            // 
            this.txtBitrate.Location = new System.Drawing.Point(550, 294);
            this.txtBitrate.Name = "txtBitrate";
            this.txtBitrate.ReadOnly = true;
            this.txtBitrate.Size = new System.Drawing.Size(194, 20);
            this.txtBitrate.TabIndex = 10;
            // 
            // contextGrid
            // 
            this.contextGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem3,
            this.removeToolStripMenuItem,
            this.removeAllToolStripMenuItem,
            this.renameToolStripMenuItem,
            this.convertToMp3ToolStripMenuItem});
            this.contextGrid.Name = "contextMenuFolder";
            this.contextGrid.Size = new System.Drawing.Size(206, 136);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(205, 22);
            this.toolStripMenuItem1.Text = "Open with Default Player";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(205, 22);
            this.toolStripMenuItem3.Text = "Open Folder";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.toolStripMenuItem3_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // removeAllToolStripMenuItem
            // 
            this.removeAllToolStripMenuItem.Name = "removeAllToolStripMenuItem";
            this.removeAllToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            this.removeAllToolStripMenuItem.Text = "Remove All";
            this.removeAllToolStripMenuItem.Click += new System.EventHandler(this.removeAllToolStripMenuItem_Click);
            // 
            // renameToolStripMenuItem
            // 
            this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            this.renameToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            this.renameToolStripMenuItem.Text = "Rename";
            this.renameToolStripMenuItem.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // convertToMp3ToolStripMenuItem
            // 
            this.convertToMp3ToolStripMenuItem.Name = "convertToMp3ToolStripMenuItem";
            this.convertToMp3ToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            this.convertToMp3ToolStripMenuItem.Text = "Convert to MP3";
            this.convertToMp3ToolStripMenuItem.Click += new System.EventHandler(this.convertToMp3ToolStripMenuItem_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(231, 416);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(151, 23);
            this.progressBar.TabIndex = 59;
            this.progressBar.Click += new System.EventHandler(this.progressBar_Click);
            // 
            // labelTimerSong
            // 
            this.labelTimerSong.AutoSize = true;
            this.labelTimerSong.Location = new System.Drawing.Point(388, 422);
            this.labelTimerSong.Name = "labelTimerSong";
            this.labelTimerSong.Size = new System.Drawing.Size(28, 13);
            this.labelTimerSong.TabIndex = 60;
            this.labelTimerSong.Text = "0:00";
            // 
            // _btnOpenFolder
            // 
            this._btnOpenFolder.Location = new System.Drawing.Point(550, 92);
            this._btnOpenFolder.Name = "_btnOpenFolder";
            this._btnOpenFolder.Size = new System.Drawing.Size(62, 39);
            this._btnOpenFolder.TabIndex = 61;
            this._btnOpenFolder.Text = "Open Folder";
            this._btnOpenFolder.UseVisualStyleBackColor = true;
            this._btnOpenFolder.Click += new System.EventHandler(this._btnOpenFolder_Click);
            // 
            // cbbFilePath
            // 
            this.cbbFilePath.FormattingEnabled = true;
            this.cbbFilePath.Location = new System.Drawing.Point(550, 35);
            this.cbbFilePath.Name = "cbbFilePath";
            this.cbbFilePath.Size = new System.Drawing.Size(194, 21);
            this.cbbFilePath.TabIndex = 62;
            this.cbbFilePath.SelectedIndexChanged += new System.EventHandler(this.cbbFilePath_SelectedIndexChanged);
            this.cbbFilePath.TextChanged += new System.EventHandler(this.cbbFilePath_TextChanged);
            // 
            // picBxArtwork
            // 
            this.picBxArtwork.BackColor = System.Drawing.SystemColors.ControlDark;
            this.picBxArtwork.Location = new System.Drawing.Point(78, 345);
            this.picBxArtwork.Name = "picBxArtwork";
            this.picBxArtwork.Size = new System.Drawing.Size(132, 132);
            this.picBxArtwork.TabIndex = 63;
            this.picBxArtwork.TabStop = false;
            this.picBxArtwork.Click += new System.EventHandler(this.picBxArtwork_Click);
            this.picBxArtwork.DragDrop += new System.Windows.Forms.DragEventHandler(this.picBxArtwork_DragDrop);
            this.picBxArtwork.DragEnter += new System.Windows.Forms.DragEventHandler(this.picBxArtwork_DragEnter);
            // 
            // btnSetArtwork
            // 
            this.btnSetArtwork.Location = new System.Drawing.Point(231, 347);
            this.btnSetArtwork.Name = "btnSetArtwork";
            this.btnSetArtwork.Size = new System.Drawing.Size(75, 23);
            this.btnSetArtwork.TabIndex = 64;
            this.btnSetArtwork.Text = "Set Artwork";
            this.btnSetArtwork.UseVisualStyleBackColor = true;
            this.btnSetArtwork.Click += new System.EventHandler(this.btnChangeCover_Click);
            // 
            // btnSaveArtwork
            // 
            this.btnSaveArtwork.Location = new System.Drawing.Point(231, 376);
            this.btnSaveArtwork.Name = "btnSaveArtwork";
            this.btnSaveArtwork.Size = new System.Drawing.Size(75, 23);
            this.btnSaveArtwork.TabIndex = 65;
            this.btnSaveArtwork.Text = "Save Artwork";
            this.btnSaveArtwork.UseVisualStyleBackColor = true;
            this.btnSaveArtwork.Click += new System.EventHandler(this.btnSaveCover_Click);
            // 
            // btnUnsetArtwork
            // 
            this.btnUnsetArtwork.Location = new System.Drawing.Point(312, 347);
            this.btnUnsetArtwork.Name = "btnUnsetArtwork";
            this.btnUnsetArtwork.Size = new System.Drawing.Size(88, 23);
            this.btnUnsetArtwork.TabIndex = 66;
            this.btnUnsetArtwork.Text = "Unset Artwork";
            this.btnUnsetArtwork.UseVisualStyleBackColor = true;
            this.btnUnsetArtwork.Click += new System.EventHandler(this.btnUnloadCover_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Location = new System.Drawing.Point(550, 7);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(96, 23);
            this.btnOpenFolder.TabIndex = 67;
            this.btnOpenFolder.Text = "Open Folder List";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // lblArtwork
            // 
            this.lblArtwork.AutoSize = true;
            this.lblArtwork.Location = new System.Drawing.Point(235, 455);
            this.lblArtwork.Name = "lblArtwork";
            this.lblArtwork.Size = new System.Drawing.Size(0, 13);
            this.lblArtwork.TabIndex = 68;
            // 
            // btnReloadList
            // 
            this.btnReloadList.Location = new System.Drawing.Point(652, 7);
            this.btnReloadList.Name = "btnReloadList";
            this.btnReloadList.Size = new System.Drawing.Size(92, 23);
            this.btnReloadList.TabIndex = 69;
            this.btnReloadList.Text = "Reload List";
            this.btnReloadList.UseVisualStyleBackColor = true;
            this.btnReloadList.Click += new System.EventHandler(this.btnReloadList_Click);
            // 
            // lblSelectedFilePath
            // 
            this.lblSelectedFilePath.AutoSize = true;
            this.lblSelectedFilePath.Location = new System.Drawing.Point(747, 7);
            this.lblSelectedFilePath.Name = "lblSelectedFilePath";
            this.lblSelectedFilePath.Size = new System.Drawing.Size(85, 13);
            this.lblSelectedFilePath.TabIndex = 70;
            this.lblSelectedFilePath.Text = "selectedFilePath";
            // 
            // _chkAllFiles
            // 
            this._chkAllFiles.AutoSize = true;
            this._chkAllFiles.Location = new System.Drawing.Point(685, 66);
            this._chkAllFiles.Name = "_chkAllFiles";
            this._chkAllFiles.Size = new System.Drawing.Size(61, 17);
            this._chkAllFiles.TabIndex = 71;
            this._chkAllFiles.Text = "All Files";
            this._chkAllFiles.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(946, 530);
            this.Controls.Add(this._chkAllFiles);
            this.Controls.Add(this.lblSelectedFilePath);
            this.Controls.Add(this.btnReloadList);
            this.Controls.Add(this.lblArtwork);
            this.Controls.Add(this.btnOpenFolder);
            this.Controls.Add(this.btnUnsetArtwork);
            this.Controls.Add(this.btnSaveArtwork);
            this.Controls.Add(this.btnSetArtwork);
            this.Controls.Add(this.picBxArtwork);
            this.Controls.Add(this.cbbFilePath);
            this.Controls.Add(this._btnOpenFolder);
            this.Controls.Add(this.labelTimerSong);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtBitrate);
            this.Controls.Add(this.btnMute);
            this.Controls.Add(this.btnVolumeDown);
            this.Controls.Add(this.btnVolumeUp);
            this.Controls.Add(this._btnDisableTimerSong);
            this.Controls.Add(this.treeViewFolder);

            this.Controls.Add(this._chkShuffle);
            this.Controls.Add(this._btnPrev);
            this.Controls.Add(this._btnNext);
            this.Controls.Add(this._btnClearLyrics);
            this.Controls.Add(this._btnCopy);
            this.Controls.Add(this._btnInfo);
            this.Controls.Add(this._btnGetLyrics);
            this.Controls.Add(this._btnPlayAll);
            this.Controls.Add(this._btnUntagged);
            this.Controls.Add(this._chkReplay);
            this.Controls.Add(this._btnSearch);
            this.Controls.Add(this._btnList);
            this.Controls.Add(this.lblTrack);
            this.Controls.Add(this.txtTrack);
            this.Controls.Add(this._lblCount);
            this.Controls.Add(this._btnStop);
            this.Controls.Add(this.lblResult);
            this.Controls.Add(this.txtLyrics);
            this.Controls.Add(this.lblGenre);
            this.Controls.Add(this.txtGenre);
            this.Controls.Add(this._btnSave);
            this.Controls.Add(this.lblComments);
            this.Controls.Add(this.lblYear);
            this.Controls.Add(this.lblAlbum);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblArtist);
            this.Controls.Add(this.txtComments);
            this.Controls.Add(this.txtYear);
            this.Controls.Add(this.txtArtist);
            this.Controls.Add(this.txtTitle);
            this.Controls.Add(this.txtAlbum);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.gridView);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(962, 569);
            this.MinimumSize = new System.Drawing.Size(962, 569);
            this.Name = "MainForm";
            this.Text = "Mp3TagReader";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gridView)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuFolder.ResumeLayout(false);
            this.contextGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picBxArtwork)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtAlbum;
        private System.Windows.Forms.TextBox txtTitle;
        private System.Windows.Forms.TextBox txtArtist;
        private System.Windows.Forms.TextBox txtYear;
        private System.Windows.Forms.TextBox txtComments;
        private System.Windows.Forms.Label lblArtist;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblAlbum;
        private System.Windows.Forms.Label lblYear;
        private System.Windows.Forms.Label lblComments;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Button _btnSave;
        private System.Windows.Forms.DataGridView gridView;
        private System.Windows.Forms.Label lblGenre;
        private System.Windows.Forms.TextBox txtGenre;
        private System.Windows.Forms.TextBox txtLyrics;
        private System.Windows.Forms.Label lblResult;
        private System.Windows.Forms.Button _btnStop;
        private System.Windows.Forms.Label _lblCount;
        private System.Windows.Forms.Label lblTrack;
        private System.Windows.Forms.TextBox txtTrack;
        private System.Windows.Forms.Button _btnList;
        private System.Windows.Forms.Button _btnSearch;
        private System.Windows.Forms.CheckBox _chkReplay;
        private System.Windows.Forms.Button _btnUntagged;
        private System.Windows.Forms.Button _btnPlayAll;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Button _btnGetLyrics;
        private System.Windows.Forms.Button _btnInfo;
        private System.Windows.Forms.Timer timerSong;
        private System.Windows.Forms.Button _btnCopy;
        private System.Windows.Forms.Timer timerNowPlayingText;
        private System.Windows.Forms.Button _btnClearLyrics;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button _btnNext;
        private System.Windows.Forms.Button _btnPrev;
        private System.Windows.Forms.CheckBox _chkShuffle;

        private System.Windows.Forms.ImageList imageListFolder;
        private System.Windows.Forms.ContextMenuStrip contextMenuFolder;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.TreeView treeViewFolder;
        private System.Windows.Forms.ToolStripMenuItem openFolderToolStripMenuItem;
        private System.Windows.Forms.Button _btnDisableTimerSong;
        private System.Windows.Forms.Button btnVolumeUp;
        private System.Windows.Forms.Button btnVolumeDown;
        private System.Windows.Forms.Button btnMute;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtBitrate;
        private System.Windows.Forms.ContextMenuStrip contextGrid;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelTimerSong;
        private System.Windows.Forms.Button _btnOpenFolder;
        private System.Windows.Forms.ToolStripMenuItem renameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fixTrackNumberToolStripMenuItem;
        private System.Windows.Forms.ComboBox cbbFilePath;
        private System.Windows.Forms.PictureBox picBxArtwork;
        private System.Windows.Forms.Button btnSetArtwork;
        private System.Windows.Forms.Button btnSaveArtwork;
        private System.Windows.Forms.Button btnUnsetArtwork;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Label lblArtwork;
        private System.Windows.Forms.Button btnReloadList;
        private System.Windows.Forms.Label lblSelectedFilePath;
        private System.Windows.Forms.CheckBox _chkAllFiles;
        private System.Windows.Forms.ToolStripMenuItem convertToMp3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem downloadMP3FromYouTubeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem attributeChangerToolStripMenuItem;
    }
}

