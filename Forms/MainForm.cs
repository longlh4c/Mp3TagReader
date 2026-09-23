using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Mp3TagReader.com.wikia.lyrics;
using TagLib;
using Mp3TagReader.Services;
using Mp3TagReader.Models;
using Mp3TagReader.Helpers;

namespace Mp3TagReader.Forms
{
    public partial class MainForm : Form
    {
        private FileExplorer fe = new FileExplorer();
        private static string filePath = string.Empty;
        private static string selectedFileName = string.Empty;
        private static string temp = string.Empty;
        private static List<string> listSelectedFiles = new List<string>();
        private MySortableBindingList<Mp3Info> listMp3Infos = new MySortableBindingList<Mp3Info>();
        String[] allPatterns = { "*" };
        String[] audioPatterns = { "*.mp3", "*.wma", "*.flac", "*.m4a" };
        String[] videoPatterns = { "*.mp4", "*.avi", "*.mpg", "*.flv", "*.wmv" };

        private String folderListPath = "folderList.txt";

        // true while the code itself changes cbbFilePath, so cbbFilePath_TextChanged won't start a search
        private bool suppressFolderSearch = false;

        // plays the songs with the bundled Libs\ffplay.exe; null when ffplay.exe is missing
        private ITrackPlayer player = null;

        // song currently loaded in the player; kept separate from selectedFileName (the file shown in the tag editor)
        private string nowPlayingFile = string.Empty;
        private double nowPlayingDuration = 0;

        // playlist managed by the app: the audio files of the list and the order they are played in (shuffled or not)
        private List<string> playlist = new List<string>();
        private List<int> playOrder = new List<int>();
        private int orderPos = -1;
        private int consecutiveFailures = 0;
        private readonly Random random = new Random();

        // 0: Off, 1: Repeat Playlist (Loop), 2: Repeat One
        private int replayState = 0;
        private GroupBox playerGroupBox;
        private Label lblTotalTime;

        public enum PlaybackState
        {
            Stopped,
            Playing,
            Paused
        }
        private PlaybackState currentPlaybackState = PlaybackState.Stopped;

        private void UpdatePlaybackUI(PlaybackState newState)
        {
            currentPlaybackState = newState;
            switch (newState)
            {
                case PlaybackState.Playing:
                    _btnPlayAll.Text = "⏸"; // ⏸ icon only
                    timerNowPlayingText.Enabled = true;
                    break;
                case PlaybackState.Paused:
                    _btnPlayAll.Text = "▶"; // ▶ icon only
                    timerNowPlayingText.Enabled = false;
                    break;
                case PlaybackState.Stopped:
                    _btnPlayAll.Text = "▶"; // ▶ icon only
                    timerNowPlayingText.Enabled = false;
                    break;
            }
            UpdatePlayerButtons();
        }

        // Play needs a selected audio file (or a song already loaded); Prev / Next / Stop / seek need a loaded song
        private void UpdatePlayerButtons()
        {
            bool hasPlayer = player != null;
            bool active = currentPlaybackState != PlaybackState.Stopped;
            bool audioSelected = false;
            try
            {
                foreach (DataGridViewRow row in gridView.SelectedRows)
                {
                    object value = row.Cells["ColumnPath"].Value;
                    if (value != null && Manipulator.IsAudioFile(value.ToString()))
                    {
                        audioSelected = true;
                        break;
                    }
                }
            }
            catch { } // columns not created yet

            _btnPlayAll.Enabled = hasPlayer && (active || audioSelected);
            _btnPrev.Enabled = hasPlayer && active;
            _btnNext.Enabled = hasPlayer && active;
            _btnStop.Enabled = hasPlayer && active;
            progressBar.Enabled = hasPlayer && active;
            _btnDisableTimerSong.Enabled = hasPlayer && active;
        }

        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        // Volume control
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;

        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
        IntPtr wParam, IntPtr lParam);

        /// </summary>

        public MainForm()
        {
            InitializeComponent();
            InitializeModernLayout();
            this.FormClosing += MainForm_FormClosing;
            treeViewFolder.AfterSelect += treeViewFolder_AfterSelect;
            listMp3Infos.KeepOnTop = delegate(Mp3Info item) { return item is ParentFolderInfo; };

            player = CreatePlayer();
            if (player == null)
            {
                playerGroupBox.Enabled = false;
                playerGroupBox.Text = "Music Player (Libs\\ffplay.exe not found)";
            }
            else
            {
                player.TrackEnded += player_TrackEnded;
            }
            UpdatePlayerButtons();
        }

        // songs are always played with the bundled Libs\ffplay.exe
        private static ITrackPlayer CreatePlayer()
        {
            string ffplay = FfplayTrackPlayer.FindFfplay();
            return ffplay != null ? new FfplayTrackPlayer(ffplay) : null;
        }

        private bool EnsurePlayerAvailable()
        {
            if (player != null) return true;
            MessageBox.Show("Libs\\ffplay.exe was not found next to Mp3TagReader.exe, so songs cannot be played.",
                "Music Player", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        // raised on a worker thread: continue on the UI thread
        private void player_TrackEnded(object sender, TrackEndedEventArgs e)
        {
            try
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)delegate { OnTrackEnded(e); });
                }
            }
            catch (InvalidOperationException) { }
        }

        private void OnTrackEnded(TrackEndedEventArgs e)
        {
            if (currentPlaybackState != PlaybackState.Playing || playOrder.Count == 0) return;

            try
            {
                if (e.Failed)
                {
                    consecutiveFailures++;
                    lblResult.Text = "Cannot play " + Path.GetFileName(nowPlayingFile) + (e.Error != null ? ": " + e.Error : "");
                    if (consecutiveFailures >= playOrder.Count)
                    {
                        StopPlayback(); // nothing in the list can be played
                        return;
                    }
                }
                else
                {
                    consecutiveFailures = 0;
                    if (replayState == 2) // repeat one
                    {
                        PlayAt(orderPos);
                        return;
                    }
                }

                if (orderPos + 1 < playOrder.Count)
                {
                    PlayAt(orderPos + 1);
                }
                else if (replayState != 0) // repeat playlist
                {
                    if (_chkShuffle.Checked)
                    {
                        BuildPlayOrder(playOrder[random.Next(playOrder.Count)]);
                        PlayAt(0);
                    }
                    else
                    {
                        PlayAt(0);
                    }
                }
                else
                {
                    StopPlayback();
                }
            }
            catch (Exception ex)
            {
                StopPlayback();
                lblResult.Text = "Player error: " + ex.Message;
            }
        }

        // playOrder = indexes into playlist; with shuffle the start song comes first and the others follow in random order.
        // Returns the position of startIndex in playOrder.
        private int BuildPlayOrder(int startIndex)
        {
            playOrder = new List<int>();
            for (int i = 0; i < playlist.Count; i++) playOrder.Add(i);
            if (!_chkShuffle.Checked) return startIndex;

            playOrder.Remove(startIndex);
            for (int i = playOrder.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int t = playOrder[i];
                playOrder[i] = playOrder[j];
                playOrder[j] = t;
            }
            playOrder.Insert(0, startIndex);
            return 0;
        }

        private void PlayAt(int position)
        {
            orderPos = position;
            string path = playlist[playOrder[position]];
            nowPlayingFile = path;
            nowPlayingDuration = GetDuration(path);
            player.Play(path, 0);
            UpdatePlaybackUI(PlaybackState.Playing);
            this.Text = "Now Playing - " + Path.GetFileNameWithoutExtension(path) + " | ";
            setProgressBar(1);
        }

        private static double GetDuration(string path)
        {
            try
            {
                return TagLib.File.Create(path).Properties.Duration.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        private void StopPlayback()
        {
            try
            {
                if (player != null) player.Stop();
            }
            catch { }
            nowPlayingFile = string.Empty;
            nowPlayingDuration = 0;
            UpdatePlaybackUI(PlaybackState.Stopped);
            this.Text = "Mp3TagReader";
            setProgressBar(0);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (player == null) return;
            try
            {
                // also ends a running ffplay process
                player.Dispose();
            }
            catch { }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                loadTree();
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

                // load combobox from file
                reloadFolderList(folderListPath);

                // special: for autoplay
                //ListFiles();

                //PlaySelectedMp3Files();
            }
            catch
            {
            }
        }

        private void reloadFolderList(String folder)
        {
            cbbFilePath.Items.Clear();
            if (System.IO.File.Exists(folder))
            {
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(folder);
                    foreach (String line in lines)
                    {
                        if (cbbFilePath.Items.Count < 10)
                        {
                            cbbFilePath.Items.Add(line);
                        }
                    }
                }
                catch { }
            }

            if (cbbFilePath.Items.Count > 0)
            {
                cbbFilePath.SelectedIndex = 0;
            }
        }

        private void loadTree()
        {
            fe.CreateTree(this.treeViewFolder);
        }

        private void ClearMp3List()
        {
            try
            {
                gridView.DataSource = null;
                listMp3Infos.Clear();
                tagsShownFor = null; // re-read tags after a reload, the files may have changed
            }
            catch (Exception) { }
        }

        private void RebuildList()
        {
            try
            {
                gridView.DataSource = listMp3Infos;
                int count = 0;
                foreach (Mp3Info item in listMp3Infos)
                {
                    if (!(item is ParentFolderInfo)) count++; // the ".." row is not an entry of the folder
                }
                _lblCount.Text = count + " files";
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
        }

        private FileInfo[] getFileInfo(String filePath, String fileFormat)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(filePath);
                return di.GetFiles(fileFormat, SearchOption.AllDirectories);
            }
            catch
            {
                return null;
            }
        }

        // Move the listed folder to the top of the history in cbbFilePath and save it to folderList.txt.
        // Only called for an explicit "List Files", never while browsing or searching.
        private void addToFolderHistory(String filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !Directory.Exists(filePath)) return;

            string normPath = Path.GetFullPath(filePath);

            int existingIdx = -1;
            for (int idx = 0; idx < cbbFilePath.Items.Count; idx++)
            {
                try
                {
                    if (Path.GetFullPath(cbbFilePath.Items[idx].ToString()).Equals(normPath, StringComparison.OrdinalIgnoreCase))
                    {
                        existingIdx = idx;
                        break;
                    }
                }
                catch { } // invalid line in folderList.txt
            }

            if (existingIdx == 0) return;

            suppressFolderSearch = true;
            try
            {
                if (existingIdx > 0)
                {
                    cbbFilePath.Items.RemoveAt(existingIdx);
                }

                cbbFilePath.Items.Insert(0, filePath);

                // Limit to 10 items
                while (cbbFilePath.Items.Count > 10)
                {
                    cbbFilePath.Items.RemoveAt(cbbFilePath.Items.Count - 1);
                }

                cbbFilePath.SelectedIndex = 0;
            }
            finally
            {
                suppressFolderSearch = false;
            }

            // Save history to folderList.txt
            try
            {
                List<string> lines = new List<string>();
                foreach (var item in cbbFilePath.Items)
                {
                    lines.Add(item.ToString());
                }
                System.IO.File.WriteAllLines(folderListPath, lines.ToArray());
            }
            catch { }
        }

        // GetFiles(..., AllDirectories) aborts on the first folder without access; walk the tree manually instead
        private List<FileInfo> getFilesSafe(DirectoryInfo di, String searchPattern, SearchOption option)
        {
            List<FileInfo> result = new List<FileInfo>();
            try
            {
                result.AddRange(di.GetFiles(searchPattern, SearchOption.TopDirectoryOnly));
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            if (option == SearchOption.AllDirectories)
            {
                foreach (DirectoryInfo sub in getDirectoriesSafe(di))
                {
                    result.AddRange(getFilesSafe(sub, searchPattern, option));
                }
            }
            return result;
        }

        private DirectoryInfo[] getDirectoriesSafe(DirectoryInfo di)
        {
            try
            {
                return di.GetDirectories();
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            return new DirectoryInfo[0];
        }

        private void listFiles(String[] searchPattern, String filePath, String searchString)
        {
            listFiles(searchPattern, filePath, searchString, SearchOption.AllDirectories);
        }

        private void listFiles(String[] searchPattern, String filePath, String searchString, SearchOption option)
        {
            DirectoryInfo di = new DirectoryInfo(filePath);
            if (!di.Exists)
            {
                throw new DirectoryNotFoundException("Folder not found: " + filePath);
            }

            foreach (String s in searchPattern)
            {
                foreach (FileInfo fi in getFilesSafe(di, s, option))
                {
                    if (!searchString.Equals(""))
                    {
                        if (fi.Name.ToLower().Contains(searchString.ToLower()))
                            listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, fi.CreationTime));
                    }
                    else
                        listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, fi.CreationTime));
                }
            }
        }

        private void listVideoFiles(String filePath)
        {
            listFiles(videoPatterns, filePath, "");
        }

        private void listAudioFiles(String filePath)
        {
            listFiles(audioPatterns, filePath, "");
        }

        private void listFolders(String filePath)
        {
            listFolders(filePath, "");
        }

        private void listFolders(String filePath, String searchString)
        {
            DirectoryInfo di = new DirectoryInfo(filePath);
            DirectoryInfo[] folders = getDirectoriesSafe(di);
            foreach (DirectoryInfo folder in folders)
            {
                if (searchString.Equals(""))
                    listMp3Infos.Add(new FolderInfo(folder.FullName, folder.Name));
                else
                {
                    if (folder.Name.ToLower().Contains(searchString.ToLower()))
                        listMp3Infos.Add(new FolderInfo(folder.FullName, folder.Name));

                    // continue to search children folders
                    else
                        loopThroughFolders(folder, searchString);
                }
            }
        }

        // folder shown by the last listFolderContent call
        private string browsedFolder = string.Empty;

        // Content of one folder: its subfolders first, then the files directly inside it (audio + video, or every
        // file when allFiles). Files of the subfolders are not included: double-click a subfolder to browse into it.
        private void listFolderContent(String folder, bool allFiles)
        {
            DirectoryInfo di = new DirectoryInfo(folder);
            if (!di.Exists)
            {
                throw new DirectoryNotFoundException("Folder not found: " + folder);
            }
            browsedFolder = di.FullName;

            // ".." row to go up (not at the root of a drive)
            if (di.Parent != null)
            {
                listMp3Infos.Add(new ParentFolderInfo(di.Parent.FullName));
            }

            List<DirectoryInfo> folders = new List<DirectoryInfo>(getDirectoriesSafe(di));
            folders.Sort(delegate(DirectoryInfo a, DirectoryInfo b) { return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase); });
            foreach (DirectoryInfo sub in folders)
            {
                listMp3Infos.Add(new FolderInfo(sub.FullName, sub.Name));
            }

            List<String> patterns = new List<String>();
            if (allFiles)
            {
                patterns.AddRange(allPatterns);
            }
            else
            {
                patterns.AddRange(audioPatterns);
                patterns.AddRange(videoPatterns);
            }

            List<FileInfo> files = new List<FileInfo>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (String pattern in patterns)
            {
                foreach (FileInfo fi in getFilesSafe(di, pattern, SearchOption.TopDirectoryOnly))
                {
                    if (seen.Add(fi.FullName)) files.Add(fi);
                }
            }
            files.Sort(delegate(FileInfo a, FileInfo b) { return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase); });
            foreach (FileInfo fi in files)
            {
                listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, fi.CreationTime));
            }
        }

        private void ListFiles()
        {
            string _temp = string.Empty;
            try
            {
                ClearMp3List();
                _temp = filePath;
                filePath = cbbFilePath.Text;
                addToFolderHistory(filePath);

                listFolderContent(filePath, false);

                RebuildList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                filePath = _temp;
                suppressFolderSearch = true;
                cbbFilePath.Text = filePath;
                suppressFolderSearch = false;
            }
        }

        private void ListAllFiles()
        {
            string _temp = string.Empty;
            try
            {
                ClearMp3List();
                _temp = filePath;
                filePath = cbbFilePath.Text;
                addToFolderHistory(filePath);

                listFolderContent(filePath, true);

                RebuildList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                filePath = _temp;
                suppressFolderSearch = true;
                cbbFilePath.Text = filePath;
                suppressFolderSearch = false;
            }
        }



        private Byte[] binArtwork;
        private TagLib.File selectedMp3;

        private void ClearTagFields()
        {
            txtArtist.Text = string.Empty;
            txtAlbum.Text = string.Empty;
            txtTitle.Text = string.Empty;
            txtTrack.Text = string.Empty;
            txtYear.Text = string.Empty;
            txtGenre.Text = string.Empty;
            txtBitrate.Text = string.Empty;
            txtLyrics.Text = string.Empty;
            txtComments.Text = string.Empty;
            SetArtworkImage(null);
        }

        private void SetArtworkImage(byte[] data)
        {
            Image old = picBxArtwork.Image;
            binArtwork = data;
            if (data == null)
            {
                picBxArtwork.Image = null;
            }
            else
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (Image full = Image.FromStream(ms))
                {
                    picBxArtwork.Image = full.GetThumbnailImage(picBxArtwork.Height, picBxArtwork.Height, null, IntPtr.Zero);
                }
            }
            if (old != null)
            {
                old.Dispose();
            }
        }

        private void ShowTags(string _path)
        {
            try
            {
                if (!Manipulator.IsAudioFile(_path))
                {
                    // folder / video / other file: don't leave the previous song's tags on screen
                    ClearTagFields();
                }
                else
                {
                    selectedMp3 = TagLib.File.Create(_path);
                    txtArtist.Text = Manipulator.ArrayToString(selectedMp3.Tag.Performers, ",");
                    txtAlbum.Text = selectedMp3.Tag.Album;
                    txtTitle.Text = selectedMp3.Tag.Title;
                    txtTrack.Text = selectedMp3.Tag.Track.ToString();
                    txtYear.Text = selectedMp3.Tag.Year.ToString();
                    txtGenre.Text = Manipulator.ArrayToString(selectedMp3.Tag.Genres, ",");
                    txtBitrate.Text = selectedMp3.Properties.AudioBitrate.ToString() + " kbps";
                    txtLyrics.Text = selectedMp3.Tag.Lyrics;
                    txtComments.Text = selectedMp3.Tag.Comment;

                    if (selectedMp3.Tag.Pictures.Length >= 1)
                    {
                        SetArtworkImage((byte[])(selectedMp3.Tag.Pictures[0].Data.Data));
                    }
                    else
                    {
                        SetArtworkImage(null);
                    }
                }
            }
            catch (Exception e)
            {
                ClearTagFields();
                lblResult.Text = "Error: " + e.Message;
            }
        }

        private const string MultipleValues = "(multiple values)";

        // file whose tags are currently shown in the editor (null when several files are shown)
        private string tagsShownFor = null;

        private void ShowMultiTags(List<string> fileNames)
        {
            TextBox[] boxes = { txtArtist, txtAlbum, txtTitle, txtTrack, txtYear, txtGenre, txtLyrics, txtComments };
            string[] fieldValues = new string[boxes.Length];
            bool[] differs = new bool[boxes.Length];
            bool first = true;
            int unreadable = 0;

            foreach (string path in fileNames)
            {
                try
                {
                    TagLib.File tag = TagLib.File.Create(path);
                    string[] values = new string[]
                    {
                        Manipulator.ArrayToString(tag.Tag.Performers, ","),
                        tag.Tag.Album,
                        tag.Tag.Title,
                        tag.Tag.Track.ToString(),
                        tag.Tag.Year.ToString(),
                        Manipulator.ArrayToString(tag.Tag.Genres, ","),
                        tag.Tag.Lyrics,
                        tag.Tag.Comment
                    };

                    for (int i = 0; i < values.Length; i++)
                    {
                        string value = values[i] ?? string.Empty;
                        if (first)
                        {
                            fieldValues[i] = value;
                        }
                        else if (value != fieldValues[i])
                        {
                            // once a field differs it stays "(multiple values)", whatever the next files contain
                            differs[i] = true;
                        }
                    }
                    first = false;
                }
                catch (Exception)
                {
                    unreadable++;
                }
            }

            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].Text = differs[i] ? MultipleValues : (fieldValues[i] ?? string.Empty);
            }
            txtBitrate.Text = string.Empty;
            SetArtworkImage(null);

            if (unreadable > 0)
            {
                lblResult.Text = fileNames.Count + " selected, " + unreadable + " unreadable";
            }
        }

        private bool TryParseTagNumber(TextBox box, string fieldName, out uint value)
        {
            value = 0;
            string text = box.Text.Trim();
            if (text.Length == 0) return true;
            if (uint.TryParse(text, out value)) return true;

            MessageBox.Show(fieldName + " must be a positive number.", "Warning");
            box.Focus();
            return false;
        }

        private void _btnSave_Click(object sender, EventArgs e)
        {
            if (listSelectedFiles.Count == 0)
            {
                MessageBox.Show("Please select an audio file first.", "Warning");
                return;
            }

            // with several files, a field still showing "(multiple values)" is left untouched
            bool multi = listSelectedFiles.Count > 1;
            uint track = 0;
            uint year = 0;
            bool setTrack = !(multi && txtTrack.Text == MultipleValues);
            bool setYear = !(multi && txtYear.Text == MultipleValues);
            if (setTrack && !TryParseTagNumber(txtTrack, "Track", out track)) return;
            if (setYear && !TryParseTagNumber(txtYear, "Year", out year)) return;

            int saved = 0;
            List<string> errors = new List<string>();
            foreach (string path in listSelectedFiles)
            {
                try
                {
                    TagLib.File mp3 = TagLib.File.Create(path);
                    if (!multi || txtArtist.Text != MultipleValues)
                    {
                        mp3.Tag.Performers = Manipulator.StringToArray(txtArtist.Text, ',');
                    }
                    if (!multi || txtAlbum.Text != MultipleValues)
                    {
                        mp3.Tag.Album = txtAlbum.Text;
                    }
                    if (!multi || txtTitle.Text != MultipleValues)
                    {
                        mp3.Tag.Title = txtTitle.Text;
                    }
                    if (setTrack)
                    {
                        mp3.Tag.Track = track;
                    }
                    if (setYear)
                    {
                        mp3.Tag.Year = year;
                    }
                    if (!multi || txtGenre.Text != MultipleValues)
                    {
                        mp3.Tag.Genres = Manipulator.StringToArray(txtGenre.Text, ',');
                    }
                    if (!multi || txtLyrics.Text != MultipleValues)
                    {
                        mp3.Tag.Lyrics = txtLyrics.Text;
                    }
                    if (!multi || txtComments.Text != MultipleValues)
                    {
                        mp3.Tag.Comment = txtComments.Text;
                    }
                    mp3.Save();
                    saved++;
                }
                catch (Exception ex)
                {
                    errors.Add(Path.GetFileName(path) + ": " + ex.Message);
                }
            }

            lblResult.Text = multi ? saved + " saved" : (saved == 1 ? "Saved" : "Not saved");
            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, errors.ToArray()), "Error");
            }
        }

        private void gridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Header clicked, ignore
            artistFlag = 0;
            titleFlag = 0;
            lblArtwork.Text = string.Empty;
            changeCoverFlag = false;
        }

        private void gridView_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateSelectionInfo();
            }
            catch (Exception ex)
            {
                lblResult.Text = "Error: " + ex.Message;
            }
            UpdatePlayerButtons();
        }

        // Keep selectedFileName, listSelectedFiles and the tag editor in sync with the selected rows,
        // whether the selection changed by mouse, keyboard, sorting or code.
        private void UpdateSelectionInfo()
        {
            List<string> selectedPaths = new List<string>();
            foreach (DataGridViewRow row in gridView.SelectedRows)
            {
                if (row.Cells["ColumnPath"].Value != null)
                {
                    selectedPaths.Add(row.Cells["ColumnPath"].Value.ToString());
                }
            }
            if (selectedPaths.Count == 0) return;

            lblResult.Text = selectedPaths.Count + " selected";
            listSelectedFiles.Clear();

            if (selectedPaths.Count == 1)
            {
                string path = selectedPaths[0];
                selectedFileName = path;
                if (path != tagsShownFor)
                {
                    ShowTags(path);
                    tagsShownFor = path;
                }
                if (Manipulator.IsAudioFile(path))
                {
                    listSelectedFiles.Add(path);
                }
            }
            else
            {
                foreach (string path in selectedPaths)
                {
                    if (Manipulator.IsAudioFile(path))
                    {
                        listSelectedFiles.Add(path);
                    }
                }
                tagsShownFor = null;
                if (listSelectedFiles.Count > 0)
                {
                    ShowMultiTags(listSelectedFiles);
                }
                else
                {
                    ClearTagFields();
                }
            }
        }

        private void PlaySelectedMp3Files(bool openNonAudioExternally)
        {
            try
            {
                if (gridView.CurrentRow == null) return;
                if (gridView.CurrentRow.Cells["ColumnPath"].Value == null) return;
                selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
                if (openNonAudioExternally && !Manipulator.IsAudioFile(selectedFileName))
                {
                    // videos and other files are not part of the audio playlist: open them with the default app
                    System.Diagnostics.Process.Start(selectedFileName);
                    return;
                }

                if (!EnsurePlayerAvailable()) return;

                // double-click on the song that is already playing: keep playing
                if (currentPlaybackState == PlaybackState.Playing &&
                    string.Equals(nowPlayingFile, selectedFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                int index = getSongIndex(selectedFileName);

                // Build a FULL playlist with all songs so Prev/Next wrap correctly
                List<string> songs = new List<string>();
                int startIndex = 0; // playlist index of the selected song (or the next audio file after it)
                for (int i = 0; i <= gridView.Rows.Count - 1; i++)
                {
                    if (gridView.Rows[i].Cells["ColumnPath"].Value == null) continue;
                    string fi = gridView.Rows[i].Cells["ColumnPath"].Value.ToString();
                    if (Manipulator.IsAudioFile(fi))
                    {
                        songs.Add(fi);
                        if (i < index) startIndex++; // count audio-only items before selected
                    }
                }

                if (songs.Count == 0)
                {
                    MessageBox.Show("No audio file in list", "Warning");
                    return;
                }

                playlist = songs;
                consecutiveFailures = 0;

                // always start with the chosen song; shuffle only affects the order of the following songs
                if (startIndex >= playlist.Count) startIndex = 0;
                PlayAt(BuildPlayOrder(startIndex));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void _btnStop_Click(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void _btnList_Click(object sender, EventArgs e)
        {
            if (_chkAllFiles.Checked)
                ListAllFiles();
            else
                ListFiles();
        }

        private void _btnSearch_Click(object sender, EventArgs e)
        {
            if (_btnList.Enabled == true)
            {
                _btnList.Enabled = false;
                _btnUntagged.Enabled = true;
                _btnSearch.Text = "Search ON";
                cbbFilePath.Focus();
                cbbFilePath.Text = "";
            }
            else
            {
                _btnList.Enabled = true;
                _btnUntagged.Enabled = true;
                _btnSearch.Text = "Search OFF";
                cbbFilePath.Focus();
            }
        }

        private void loopThroughFolders(DirectoryInfo folder, string searchString)
        {
            DirectoryInfo[] folders = getDirectoriesSafe(folder);

            if (folders.Length > 0)
            {
                foreach (DirectoryInfo folderTemp in folders)
                {
                    if (folderTemp.Name.ToLower().Contains(searchString.ToLower()))
                    {
                        listMp3Infos.Add(new FolderInfo(folderTemp.FullName, folderTemp.Name));
                    }
                    else
                    {
                        loopThroughFolders(folderTemp, searchString);
                    }
                }
            }
        }

        private void txtLyrics_DoubleClick(object sender, EventArgs e)
        {
            if (txtLyrics.Text != string.Empty)
            {
                LyricsViewer lv = new LyricsViewer(txtArtist.Text, txtTitle.Text, txtLyrics.Text);
                lv.Show();
            }
        }

        private bool updatingReplayButton = false;

        private void chkReplay_CheckedChanged(object sender, EventArgs e)
        {
            // setting _chkReplay.Checked below raises this event again; ignore those nested calls
            if (updatingReplayButton) return;

            // Cycle: 0 (Off) -> 1 (Repeat Playlist) -> 2 (Repeat One)
            replayState = (replayState + 1) % 3;
            updatingReplayButton = true;
            try
            {
                if (replayState == 0)
                {
                    _chkReplay.Checked = false;
                    _chkReplay.Text = "\u21BA"; // ↺ default icon
                }
                else if (replayState == 1)
                {
                    _chkReplay.Checked = true;
                    _chkReplay.Text = "\u21BA\u1D3A"; // ↺ᴬ (All)
                }
                else if (replayState == 2)
                {
                    _chkReplay.Checked = true;
                    _chkReplay.Text = "\u21BA\u00B9"; // ↺¹ (One)
                }
            }
            finally
            {
                updatingReplayButton = false;
            }
        }

        private void _btnUntagged_Click(object sender, EventArgs e)
        {
            TagLib.File audio;

            // check the audio files of the current list
            List<Mp3Info> candidates = new List<Mp3Info>();
            foreach (Mp3Info info in listMp3Infos)
            {
                if (!(info is FolderInfo) && Manipulator.IsAudioFile(info.Path))
                {
                    candidates.Add(info);
                }
            }

            if (candidates.Count == 0)
            {
                MessageBox.Show("Please load files first", "Warning");
                return;
            }

            ClearMp3List();
            int unreadable = 0;
            foreach (Mp3Info fi in candidates)
            {
                try
                {
                    audio = TagLib.File.Create(fi.Path, ReadStyle.None);
                    if (audio.Tag == null)
                    {
                        listMp3Infos.Add(fi);
                        continue;
                    }

                    bool isUntagged = false;

                    if (audio.Tag.Album != null)
                    {
                        string albumLower = audio.Tag.Album.ToLower();
                        if (albumLower.Contains("zing") || albumLower.Contains(".com") || albumLower.Contains(".vn") || albumLower.Contains(".org"))
                        {
                            isUntagged = true;
                        }
                    }

                    if (string.IsNullOrEmpty(audio.Tag.Album) || 
                        string.IsNullOrEmpty(Manipulator.ArrayToString(audio.Tag.Performers, ",")) || 
                        string.IsNullOrEmpty(audio.Tag.Title))
                    {
                        isUntagged = true;
                    }

                    if (isUntagged)
                    {
                        listMp3Infos.Add(fi);
                    }
                }
                catch (Exception)
                {
                    // corrupt / unsupported file: skip it and keep checking the others
                    unreadable++;
                }
            }
            gridView.DataSource = listMp3Infos;
            _lblCount.Text = gridView.RowCount.ToString() + " files" + (unreadable > 0 ? " (" + unreadable + " unreadable)" : "");
        }

        private void _btnPlayList_Click(object sender, EventArgs e)
        {
            if (!EnsurePlayerAvailable()) return;

            try
            {
                if (currentPlaybackState == PlaybackState.Stopped)
                {
                    if (listMp3Infos.Count == 0)
                    {
                        MessageBox.Show("No song in list", "Warning");
                        return;
                    }
                    PlaySelectedMp3Files(false);
                }
                else if (currentPlaybackState == PlaybackState.Playing)
                {
                    player.Pause();
                    UpdatePlaybackUI(PlaybackState.Paused);
                }
                else if (currentPlaybackState == PlaybackState.Paused)
                {
                    player.Resume();
                    UpdatePlaybackUI(PlaybackState.Playing);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void gridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Header double-clicked, ignore
            // open folder or play song
            try
            {
                selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
                if (isFolder())
                {
                    // browse into the clicked folder: its subfolders, then the files directly inside it
                    string folder = selectedFileName;
                    bool goingUp = gridView.CurrentRow.DataBoundItem is ParentFolderInfo;
                    string cameFrom = browsedFolder;
                    ClearMp3List();

                    listFolderContent(folder, _chkAllFiles.Checked);

                    RebuildList();

                    // after "..", select the folder we just left
                    if (goingUp)
                    {
                        foreach (DataGridViewRow row in gridView.Rows)
                        {
                            if (!(row.DataBoundItem is ParentFolderInfo) && row.Cells["ColumnPath"].Value != null &&
                                string.Equals(row.Cells["ColumnPath"].Value.ToString(), cameFrom, StringComparison.OrdinalIgnoreCase))
                            {
                                gridView.CurrentCell = row.Cells["ColumnName"];
                                gridView.FirstDisplayedScrollingRowIndex = row.Index;
                                break;
                            }
                        }
                    }

                    // outside search mode, show the folder being browsed so "List Files" refreshes it;
                    // in search mode the combobox holds the search text and is left alone
                    if (_btnList.Enabled)
                    {
                        filePath = folder;
                        suppressFolderSearch = true;
                        try
                        {
                            cbbFilePath.Text = folder;
                        }
                        finally
                        {
                            suppressFolderSearch = false;
                        }
                        updateSelectedFilePath(folder);
                    }
                }
                else
                {
                    PlaySelectedMp3Files(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }
        }

        private bool isFolder()
        {
            if (gridView.CurrentRow == null || gridView.CurrentRow.Index < 0) return false;
            if (gridView.CurrentRow.Cells["ColumnPath"].Value == null) return false;
            string selectedPath = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            return Manipulator.IsFolder(selectedPath);
        }

        private bool isVideo()
        {
            if (gridView.CurrentRow == null || gridView.CurrentRow.Index < 0) return false;
            if (gridView.CurrentRow.Cells["ColumnPath"].Value == null) return false;
            string selectedPath = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            return Manipulator.IsVideoFile(selectedPath);
        }

        private string folderOf(string filePath) // return folder of the selected file
        {
            if (gridView.CurrentRow == null || gridView.CurrentRow.Index < 0) return string.Empty;
            if (gridView.CurrentRow.Cells["ColumnPath"].Value == null) return string.Empty;
            selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            return Path.GetDirectoryName(selectedFileName);
        }

        private void lblAlbum_Click(object sender, EventArgs e)
        {
            if (txtAlbum.Text == string.Empty || txtAlbum.Text.Contains("zing") || txtAlbum.Text.Contains("."))
            {
                txtAlbum.Text = "Unknown";
            }
            else
            {
                txtAlbum.Text = txtTitle.Text + " (Single)";
            }
        }

        private string fileNameOf()
        {
            string fileName = string.Empty;
            fileName = Path.GetFileName(selectedFileName);
            return fileName;
        }

        private string getArtistFromFileName(string fileName)
        {
            string artistName = string.Empty;
            int index = 0;
            index = fileName.LastIndexOf(".");
            if (index > 0)
            {
                artistName = fileName.Substring(0, index);
            }
            index = artistName.LastIndexOf("-");
            if (index > 0)
            {
                artistName = artistName.Substring(index + 1).Trim();
            }
            return artistName;
        }

        private string getTitleFromFileName(string fileName)
        {
            string title = string.Empty;
            int index = 0;
            index = fileName.LastIndexOf(".");
            if (index > 0)
            {
                title = fileName.Substring(0, index);
            }
            index = title.LastIndexOf("-");
            if (index > 0)
            {
                title = title.Substring(0, index).Trim();
            }
            return title;
        }

        private void lblComments_Click(object sender, EventArgs e)
        {
            txtComments.Text = string.Empty;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }



        private static int artistFlag = 0;
        private static int titleFlag = 0;
        private void lblArtist_Click(object sender, EventArgs e)
        {
            if (artistFlag == 0)
            {
                string artistName = string.Empty;
                string fileName = fileNameOf();
                artistName = getArtistFromFileName(fileName);
                txtArtist.Text = artistName;
                artistFlag = 1;
            }
            else
            {
                string artistName = string.Empty;
                string fileName = fileNameOf();
                artistName = getTitleFromFileName(fileName);
                txtArtist.Text = artistName;
                artistFlag = 0;
            }
        }

        private void lblTitle_Click(object sender, EventArgs e)
        {
            if (titleFlag == 0)
            {
                string title = string.Empty;
                string fileName = fileNameOf();
                title = getTitleFromFileName(fileName);
                txtTitle.Text = title;
                titleFlag = 1;
            }
            else
            {
                string title = string.Empty;
                string fileName = fileNameOf();
                title = getArtistFromFileName(fileName);
                txtTitle.Text = title;
                titleFlag = 0;
            }
        }

        // http://samuelhaddad.com/2009/03/22/c-net-and-lyricwiki-to-lookup-lyrics/
        private void _btnGetLyrics_Click(object sender, EventArgs e)
        {
            // read the text boxes here on the UI thread; the worker thread must not touch controls
            string artist = txtArtist.Text;
            string title = txtTitle.Text;
            Thread oThread = new Thread(delegate() { getLyrics(artist, title); });
            oThread.IsBackground = true;
            oThread.Start();
        }

        private void getLyrics(string artist, string title)
        {
            try
            {
                LyricWiki wiki = new LyricWiki();
                LyricsResult result = new LyricsResult();
                if (wiki.checkSongExists(artist, title))
                {
                    result = wiki.getSong(artist, title);
                    Process.Start(result.url);
                }
                else
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("Lyrics not found", "Info");
                        Searcher searchLyric = new Searcher(title + " " + artist);
                        searchLyric.Show(this);
                    });
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    // the LyricWiki service is offline: fall back to the web search
                    MessageBox.Show("Error fetching lyrics: " + ex.Message, "Error");
                    Searcher searchLyric = new Searcher(title + " " + artist);
                    searchLyric.Show(this);
                });
            }
        }

        private void _btnInfo_Click(object sender, EventArgs e)
        {
            FullInfo info = new FullInfo(selectedFileName);
            info.Show();
        }

        private void timerSong_Tick(object sender, EventArgs e)
        {
            try
            {
                // song changes / end of playlist are handled in OnTrackEnded; here only the progress is refreshed
                if (player != null && currentPlaybackState == PlaybackState.Playing)
                {
                    setProgressBar(1);
                }

                // notification after copied to clipboard, display "copied" then change back to number of files
                if (_lblCount.Text != "Copied")
                {
                    temp = _lblCount.Text;
                }
                else
                {
                    _lblCount.Text = temp;
                }
            }
            catch { }
        }

        private int getSongIndex(string path)
        {
            int index = 0;
            try
            {
                foreach (DataGridViewRow row in gridView.Rows)
                {
                    if (row.Cells["ColumnPath"].Value != null && row.Cells["ColumnPath"].Value.ToString() == path)
                    {
                        index = row.Index;
                        break;
                    }
                }
            }
            catch
            {
                MessageBox.Show("No song in list", "Warning");
                index = 0;
            }
            return index;
        }

        private void CopyFiles(List<string> ListToCopy)
        {
            if (ListToCopy.Count != 0)
            {
                System.Collections.Specialized.StringCollection FileCollection = new System.Collections.Specialized.StringCollection();

                foreach (string FileToCopy in ListToCopy)
                {
                    FileCollection.Add(FileToCopy);
                }

                if (FileCollection != null)
                {
                    Clipboard.SetFileDropList(FileCollection);
                }
            }
        }

        private void _btnCopy_Click(object sender, EventArgs e)
        {
            if (gridView.SelectedRows.Count == 1)
            {
                selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
                listSelectedFiles.Clear();
                listSelectedFiles.Add(selectedFileName);
            }

            if (listSelectedFiles.Count != 0)
            {
                CopyFiles(listSelectedFiles);
                _lblCount.Text = "Copied";
            }
            else
            {
                MessageBox.Show("Nothing to copy", "Warning");
            }
        }





        private void timerNowPlaying_Tick(object sender, EventArgs e)
        {
            if (this.Text != "Mp3TagReader")
            {
                string sScrollText = this.Text;
                sScrollText = sScrollText.Substring(1,
                    sScrollText.Length - 1) + sScrollText.Substring(0, 1);
                this.Text = sScrollText;
            }
        }

        private void gridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                gridView.Rows[0].Selected = true;
            }
            catch { }
        }

        private void _btnClearLyrics_Click(object sender, EventArgs e)
        {
            try
            {
                txtLyrics.Clear();
            }
            catch { }
        }



        private void _btnPrev_Click(object sender, EventArgs e)
        {
            SkipTrack(-1);
        }

        private void _btnNext_Click(object sender, EventArgs e)
        {
            SkipTrack(1);
        }

        // previous / next song in the play order, wrapping around at both ends
        private void SkipTrack(int step)
        {
            if (player == null || currentPlaybackState == PlaybackState.Stopped || playOrder.Count == 0)
            {
                MessageBox.Show("Not Playing", "Warning");
                return;
            }

            try
            {
                int count = playOrder.Count;
                PlayAt(((orderPos + step) % count + count) % count);
                SelectCurrentPlayingRow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>Selects the currently playing song row in the gridView.</summary>
        private void SelectCurrentPlayingRow()
        {
            try
            {
                string currentSong = nowPlayingFile;
                if (string.IsNullOrEmpty(currentSong)) return;

                int foundIndex = -1;
                foreach (DataGridViewRow row in gridView.Rows)
                {
                    if (row.Cells["ColumnPath"].Value != null &&
                        row.Cells["ColumnPath"].Value.ToString().Equals(currentSong, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIndex = row.Index;
                        break;
                    }
                }

                if (foundIndex != -1)
                {
                    gridView.ClearSelection();
                    gridView.Rows[foundIndex].Selected = true;
                    DataGridViewColumn firstVisibleCol = null;
                    foreach (DataGridViewColumn col in gridView.Columns)
                    {
                        if (col.Visible) { firstVisibleCol = col; break; }
                    }
                    if (firstVisibleCol != null)
                        gridView.CurrentCell = gridView.Rows[foundIndex].Cells[firstVisibleCol.Index];
                    gridView.FirstDisplayedScrollingRowIndex = foundIndex;
                    // the selection change above reloads the tag editor (UpdateSelectionInfo)
                }
            }
            catch { }
        }

        private void gridView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void gridView_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            if (Manipulator.IsImageFile(files[0]))
            {
                definePicture(files[0]);
            }
            else
            {
                try
                {
                    gridView.DataSource = null;
                }
                catch (Exception) { }
                foreach (string file in files)
                {
                    if (Directory.Exists(file))
                        listMp3Infos.Add(new FolderInfo(file, System.IO.Path.GetFileName(file)));
                    else
                        listMp3Infos.Add(new Mp3Info(file, System.IO.Path.GetFileName(file), System.IO.File.GetCreationTime(file)));
                }
                gridView.DataSource = listMp3Infos;
                _lblCount.Text = gridView.RowCount.ToString() + " files";
            }
        }



        private void _chkShuffle_CheckedChanged(object sender, EventArgs e)
        {
            // re-order the rest of the playlist; the current song keeps playing
            if (currentPlaybackState == PlaybackState.Stopped || orderPos < 0 || orderPos >= playOrder.Count) return;
            orderPos = BuildPlayOrder(playOrder[orderPos]);
        }

        private TreeNode m_OldSelectNode;

        private void treeViewFolder_MouseUp(object sender, MouseEventArgs e)
        {
            // Show menu only if the right mouse button is clicked.
            if (e.Button == MouseButtons.Right)
            {
                // Point where the mouse is clicked.
                Point p = new Point(e.X, e.Y);

                // Get the node that the user has clicked.
                TreeNode node = treeViewFolder.GetNodeAt(p);
                if (node != null)
                {
                    // Select the node the user has clicked.
                    treeViewFolder.SelectedNode = node;
                    m_OldSelectNode = node;

                    contextMenuFolder.Show(treeViewFolder, p);
                }
            }
        }

        // selecting a folder in the tree makes it the folder for "List Files" (and the search root)
        private void treeViewFolder_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string path = GetPhysicalPath(e.Node);
            try
            {
                path = Path.GetFullPath(path); // node paths below a drive look like "C:\\Users"
            }
            catch { return; }
            if (!Directory.Exists(path)) return; // file node

            if (_btnList.Enabled)
            {
                suppressFolderSearch = true;
                try
                {
                    cbbFilePath.Text = path;
                }
                finally
                {
                    suppressFolderSearch = false;
                }
            }
            updateSelectedFilePath(path);
        }

        private string GetPhysicalPath(TreeNode node)
        {
            if (node == null) return string.Empty;
            string fullPath = node.FullPath;
            string[] nameList = fullPath.Split('\\');
            if (nameList.Length > 0 && nameList[0] == "Desktop")
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                for (int i = 1; i < nameList.Length; i++)
                {
                    path = Path.Combine(path, nameList[i]);
                }
                return path;
            }
            return fullPath;
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                cbbFilePath.Text = GetPhysicalPath(m_OldSelectNode);
                updateSelectedFilePath(cbbFilePath.Text);
                ListFiles();
            }
            catch (Exception) { }
        }

        private void treeViewFolder_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes[0].Text == "")
            {
                TreeNode node = fe.EnumerateDirectory(e.Node);
            }
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string physicalPath = GetPhysicalPath(m_OldSelectNode);
            if (Path.HasExtension(physicalPath))
            {
                Process.Start(Path.GetDirectoryName(physicalPath));
            }
            else
            {
                Process.Start(physicalPath);
            }
        }

        private void _btnDisableTimerSong_Click(object sender, EventArgs e)
        {
            try
            {
                string currentSong = nowPlayingFile;
                if (string.IsNullOrEmpty(currentSong)) return;

                int foundIndex = -1;
                foreach (DataGridViewRow row in gridView.Rows)
                {
                    if (row.Cells["ColumnPath"].Value != null && row.Cells["ColumnPath"].Value.ToString().Equals(currentSong, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIndex = row.Index;
                        break;
                    }
                }

                if (foundIndex != -1)
                {
                    gridView.ClearSelection();
                    gridView.Rows[foundIndex].Selected = true;

                    // Find first visible column to set as current cell
                    DataGridViewColumn firstVisibleCol = null;
                    foreach (DataGridViewColumn col in gridView.Columns)
                    {
                        if (col.Visible)
                        {
                            firstVisibleCol = col;
                            break;
                        }
                    }

                    if (firstVisibleCol != null)
                    {
                        gridView.CurrentCell = gridView.Rows[foundIndex].Cells[firstVisibleCol.Index];
                    }

                    // Scroll the row into view
                    gridView.FirstDisplayedScrollingRowIndex = foundIndex;

                    selectedFileName = currentSong;
                    ShowTags(selectedFileName);
                }
                else
                {
                    MessageBox.Show("Currently playing song is not in the current list.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error finding song: " + ex.Message);
            }
        }

        private void lblTrack_Click(object sender, EventArgs e)
        {
            int trackNo = 0;
            try
            {
                trackNo = Convert.ToInt32(Path.GetFileName(selectedFileName).Substring(0, 2));
                txtTrack.Text = trackNo.ToString();
            }
            catch (Exception) { }

            if (trackNo == 0)
            {
                if (txtTrack.Text == "0" || txtTrack.Text == string.Empty)
                {
                    txtTrack.Text = "1";
                }
                else
                {
                    txtTrack.Text = (Convert.ToInt32(txtTrack.Text) + 1).ToString();
                }
            }
        }

        private void lblYear_Click(object sender, EventArgs e)
        {
            if (txtYear.Text == "0" || txtYear.Text == string.Empty)
            {
                txtYear.Text = DateTime.Now.Year.ToString();
            }
            else
            {
                txtYear.Text = (Convert.ToInt32(txtYear.Text) - 1).ToString();
            }
        }






        // right click at grid
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            System.Diagnostics.Process.Start(selectedFileName);
        }

        private void gridView_MouseDown(object sender, MouseEventArgs e)
        {
            // Show menu only if the right mouse button is clicked.
            if (e.Button == MouseButtons.Right)
            {
                // get the row index in selected point
                DataGridView.HitTestInfo hit = gridView.HitTest(e.X, e.Y);
                int rowIndex = hit.RowIndex;

                if (rowIndex == -1)
                {
                    return;
                }

                // clear other selected rows
                gridView.ClearSelection();

                // make the clicked row the current row too: the context menu actions work on CurrentRow
                if (hit.ColumnIndex >= 0 && gridView.Columns[hit.ColumnIndex].Visible)
                {
                    gridView.CurrentCell = gridView.Rows[rowIndex].Cells[hit.ColumnIndex];
                }
                gridView.Rows[rowIndex].Selected = true;
                Point p = new Point(e.X, e.Y);
                contextGrid.Show(gridView, p);
            }
        }

        // open folder right click
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            try
            {
                // no need
                //selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
                if (isFolder())
                {
                    Process.Start(selectedFileName);
                }
                else
                {
                    Process.Start("explorer.exe", @"/select, " + selectedFileName);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("No song in list", "Warning");
                cbbFilePath.Text = filePath;
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    int i = gridView.CurrentRow.Index;
            //    listMp3Infos.RemoveAt(i);
            //    RebuildList();
            //}
            //catch (NullReferenceException ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //    return;
            //}
        }

        private void setProgressBar(int now)
        {
            if (now == 1)
            {
                // progress bar
                double duration = nowPlayingDuration;
                double position = player != null ? player.Position : 0;

                progressBar.Minimum = 0;
                progressBar.Maximum = Math.Max(0, (int)duration);
                progressBar.Value = Math.Max(0, Math.Min((int)position, progressBar.Maximum));

                // time playing
                double t = Math.Floor(position);
                double s = Math.Floor(duration);
                int minT = (Int32)(t / 60);
                int secT = (Int32)(t % 60);
                int minS = (Int32)(s / 60);
                int secS = (Int32)(s % 60);
                
                // Format: "1:23" on the left, "5:55" on the right
                labelTimerSong.Text = minT + ":" + String.Format("{0:00}", secT);
                if (lblTotalTime != null)
                {
                    lblTotalTime.Text = minS + ":" + String.Format("{0:00}", secS);
                }
            }
            else if (now == 0)
            {
                progressBar.Value = 0;
                labelTimerSong.Text = "0:00";
                if (lblTotalTime != null)
                {
                    lblTotalTime.Text = "0:00";
                }
            }
        }

        private void progressBar_Click(object sender, EventArgs e)
        {
            if (player == null || currentPlaybackState == PlaybackState.Stopped) return;

            // set the progress bar's value based on mouse position
            int value = (((MouseEventArgs)e).X) * progressBar.Maximum / progressBar.Width;
            progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(value, progressBar.Maximum));

            // set the media's position
            player.Position = progressBar.Value;
        }

        private void _btnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                if (isFolder())
                    Process.Start(selectedFileName);
                else
                    Process.Start("explorer.exe", @"/select, " + selectedFileName);
            }
            catch (Exception)
            {
                MessageBox.Show("No song in list", "Warning");
                cbbFilePath.Text = filePath;
            }
        }

        // before rename
        private string beforeRenamed = "";

        private void gridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // only the "File Name" column can be edited, and only for the row being edited
            object pathValue = gridView.Rows[e.RowIndex].Cells["ColumnPath"].Value;
            object nameValue = gridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            if (gridView.Columns[e.ColumnIndex].DataPropertyName != "Name" || pathValue == null || nameValue == null ||
                gridView.Rows[e.RowIndex].DataBoundItem is ParentFolderInfo) // ".." cannot be renamed
            {
                e.Cancel = true;
                return;
            }
            beforeRenamed = nameValue.ToString();
            renamePath = pathValue.ToString();
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (gridView.ReadOnly == true)
            {
                gridView.ReadOnly = false;
            }
        }

        private void convertToMp3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (gridView.CurrentRow == null) return;
            if (gridView.CurrentRow.Cells["ColumnPath"].Value == null) return;
            string selectedPath = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();

            using (Mp3ConvertForm convertForm = new Mp3ConvertForm(selectedPath))
            {
                convertForm.ShowDialog(this);
            }

            // Refresh list after convert dialog closes
            ListFiles();
        }

        // after rename
        private string renamePath = "";

        private void gridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Header edit end, ignore
            DataGridViewCell cell = gridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string afterRenamed = cell.Value == null ? string.Empty : cell.Value.ToString().Trim();
            gridView.ReadOnly = true;

            if (afterRenamed == beforeRenamed) return;

            if (afterRenamed.Length == 0 || afterRenamed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("Invalid file name.", "Warning");
                cell.Value = beforeRenamed;
                return;
            }

            if (MessageBox.Show("Modify file name?", "Info", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string newPath = Path.Combine(Path.GetDirectoryName(renamePath), afterRenamed);
                try
                {
                    if (Directory.Exists(renamePath))
                        Directory.Move(renamePath, newPath);
                    else
                        System.IO.File.Move(renamePath, newPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot rename: " + ex.Message, "Error");
                    cell.Value = beforeRenamed;
                    return;
                }

                gridView.Rows[e.RowIndex].Cells["ColumnPath"].Value = newPath; // prevent file not found exception after renamed
                if (string.Equals(selectedFileName, renamePath, StringComparison.OrdinalIgnoreCase))
                {
                    selectedFileName = newPath;
                    tagsShownFor = newPath;
                    listSelectedFiles.Remove(renamePath);
                    if (Manipulator.IsAudioFile(newPath)) listSelectedFiles.Add(newPath);
                }
            }
            else
            {
                cell.Value = beforeRenamed;
            }
        }

        private void removeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearMp3List();
        }

        private void fixTrackNumberToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int count = 0;
            int trackNo = 0;
            String path = string.Empty;
            try
            {
                foreach (DataGridViewRow row in gridView.Rows)
                {
                    try
                    {
                        path = row.Cells["ColumnPath"].Value.ToString();
                        trackNo = Convert.ToInt32(Path.GetFileName(path).Substring(0, 2));
                        if (trackNo > 0)
                        {
                            TagLib.File mp3 = TagLib.File.Create(path);
                            mp3.Tag.Track = Convert.ToUInt32(trackNo);
                            mp3.Save();
                            count++; // only count files that were actually updated
                        }
                    }
                    catch (Exception) { }
                }

                MessageBox.Show(count + " Tracks Updated", "Information");
            }
            catch
            {
            }
        }



        private void downloadMP3FromYouTubeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (YoutubeDownloadForm downloadForm = new YoutubeDownloadForm(cbbFilePath.Text))
            {
                if (downloadForm.ShowDialog(this) == DialogResult.OK)
                {
                    suppressFolderSearch = true;
                    cbbFilePath.Text = downloadForm.OutputFolder;
                    suppressFolderSearch = false;
                    ListFiles();
                }
            }
        }

        private void attributeChangerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string initialFile = string.Empty;
            if (gridView.CurrentRow != null && gridView.CurrentRow.Cells["ColumnPath"].Value != null)
            {
                initialFile = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            }

            using (AttributeChangerForm changer = new AttributeChangerForm(initialFile))
            {
                if (changer.ShowDialog(this) == DialogResult.OK)
                {
                    // Refresh the grid to show any modified creation dates/timestamps
                    ListFiles();
                }
            }
        }

        private void subtitleDownloaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string initialFile = string.Empty;
            if (gridView.CurrentRow != null && gridView.CurrentRow.Cells["ColumnPath"].Value != null)
            {
                initialFile = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            }

            using (SubtitleDownloaderForm downloader = new SubtitleDownloaderForm(initialFile))
            {
                downloader.ShowDialog(this);
            }
        }



        private void cbbFilePath_TextChanged(object sender, EventArgs e)
        {
            if (_btnList.Enabled == false && !suppressFolderSearch)
            {
                string searchString = cbbFilePath.Text;
                ClearMp3List();

                // search inside the folder chosen in the combobox / tree, or the last listed folder
                string searchRoot = !string.IsNullOrEmpty(selectedFilePath) ? selectedFilePath : filePath;
                if (string.IsNullOrEmpty(searchRoot) || !Directory.Exists(searchRoot))
                {
                    _lblCount.Text = "Select a folder first";
                    return;
                }

                try
                {
                    if (searchString.Length == 0)
                    {
                        listAudioFiles(searchRoot);
                        listVideoFiles(searchRoot);
                        listFolders(searchRoot);
                    }
                    else
                    {
                        // folder search results
                        listFolders(searchRoot, searchString);
                        // audio search results
                        listFiles(audioPatterns, searchRoot, searchString);
                        // video search results
                        listFiles(videoPatterns, searchRoot, searchString);
                    }
                }
                catch (Exception ex)
                {
                    _lblCount.Text = "Search error: " + ex.Message;
                }

                // check to prevent null exception
                if (listMp3Infos.Count > 0)
                {
                    gridView.DataSource = listMp3Infos;
                    _lblCount.Text = gridView.RowCount.ToString() + " results";
                }
            }
        }

        private void picBxArtwork_Click(object sender, EventArgs e)
        {
            if (binArtwork != null)
            {
                Image image;
                using (MemoryStream ms = new MemoryStream(binArtwork))
                using (Image full = Image.FromStream(ms))
                {
                    image = full.GetThumbnailImage(500, 500, null, IntPtr.Zero);
                }
                using (ArtworkViewer av = new ArtworkViewer(image))
                {
                    av.ShowDialog();
                }
                image.Dispose();
            }
        }

        private String imagePath = string.Empty;
        private Boolean unloadArtwork = false;
        private Boolean changeCoverFlag = false;

        //http://stackoverflow.com/questions/13667378/embed-album-art-in-mp3-using-tag-lib-c-sharp

        private void btnChangeCover_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // define picture
                definePicture(openFileDialog.FileName);
            }
        }

        private void btnSaveCover_Click(object sender, EventArgs e)
        {
            if (listSelectedFiles.Count == 0)
            {
                lblArtwork.Text = "Please select audio file(s) first";
                return;
            }

            if (!changeCoverFlag)
            {
                lblArtwork.Text = "No cover to change";
                return;
            }

            bool removeCover = unloadArtwork || string.IsNullOrEmpty(imagePath);
            TagLib.Id3v2.AttachedPictureFrame pic = null;
            if (!removeCover)
            {
                try
                {
                    // define picture
                    pic = new TagLib.Id3v2.AttachedPictureFrame();
                    pic.TextEncoding = TagLib.StringType.Latin1;
                    pic.MimeType = Manipulator.GetImageMimeType(imagePath);
                    pic.Type = TagLib.PictureType.FrontCover;
                    pic.Data = TagLib.ByteVector.FromPath(imagePath);
                }
                catch (Exception ex)
                {
                    lblArtwork.Text = "Cannot read image: " + ex.Message;
                    return;
                }
            }

            int saved = 0;
            List<string> errors = new List<string>();
            foreach (String selectedFile in listSelectedFiles)
            {
                try
                {
                    TagLib.File mp3 = TagLib.File.Create(selectedFile);

                    // save or unload picture to file
                    if (!removeCover)
                    {
                        mp3.Tag.Pictures = new TagLib.IPicture[1] { pic };
                    }
                    else
                    {
                        mp3.Tag.Pictures = new TagLib.IPicture[0];
                    }

                    mp3.Save();
                    saved++;
                }
                catch (Exception ex)
                {
                    errors.Add(Path.GetFileName(selectedFile) + ": " + ex.Message);
                }
            }
            changeCoverFlag = false; // void the next Save attempt

            lblArtwork.Text = saved + " artwork(s) " + (removeCover ? "unset" : "set") + " successfully";
            if (errors.Count > 0)
            {
                lblArtwork.Text += ", " + errors.Count + " failed";
                MessageBox.Show(string.Join(Environment.NewLine, errors.ToArray()), "Error");
            }
        }

        private void btnUnloadCover_Click(object sender, EventArgs e)
        {
            imagePath = string.Empty;
            unloadArtwork = true;
            SetArtworkImage(null);
            changeCoverFlag = true;
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(folderListPath))
            {
                System.Diagnostics.Process.Start(folderListPath);
            }
        }

        private void btnReloadList_Click(object sender, EventArgs e)
        {
            reloadFolderList(folderListPath);
        }

        private String selectedFilePath = "";

        private void updateSelectedFilePath(String value)
        {
            selectedFilePath = value;
            lblSelectedFilePath.Text = value;
        }

        private void cbbFilePath_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateSelectedFilePath(cbbFilePath.Text);
        }

        private void picBxArtwork_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void picBxArtwork_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            // define picture
            definePicture(files[0]);
        }

        // show the new cover and remember its path; it is written to the files by btnSaveCover_Click
        private void definePicture(String filePath)
        {
            try
            {
                SetArtworkImage(System.IO.File.ReadAllBytes(filePath));
            }
            catch (Exception ex)
            {
                lblArtwork.Text = "Cannot load image: " + ex.Message;
                return;
            }

            imagePath = filePath;
            unloadArtwork = false;
            changeCoverFlag = true;
        }

        private void lblGenre_Click(object sender, EventArgs e)
        {
            txtGenre.Text = "EDM";
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

            // Hide unused controls to prevent them from floating/overlapping
            _btnCopy.Visible = false;
            lblSelectedFilePath.Visible = false;
            _btnOpenFolder.Visible = false;

            btnVolumeDown.Visible = false;
            btnVolumeUp.Visible = false;
            btnMute.Visible = false;

            btnReloadList.Visible = false;
            btnOpenFolder.Visible = false;

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
            leftPanel.Controls.Add(btnReloadList);
            leftPanel.Controls.Add(btnOpenFolder);
            leftPanel.Controls.Add(_btnList);
            leftPanel.Controls.Add(_btnSearch);
            leftPanel.Controls.Add(_btnUntagged);
            leftPanel.Controls.Add(_chkAllFiles);
            leftPanel.Controls.Add(treeViewFolder);
            leftPanel.Controls.Add(gridView);
            leftPanel.Controls.Add(_lblCount);
            leftPanel.Controls.Add(lblResult); // Add lblResult (selected count) to left panel

            // Reposition Left Panel controls: Expand cbbFilePath, hide Reload/Open, shift others left
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
