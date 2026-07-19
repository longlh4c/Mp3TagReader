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
        private static int selectedRowIndex = 0;
        private static List<string> listSelectedFiles = new List<string>();
        private MySortableBindingList<Mp3Info> listMp3Infos = new MySortableBindingList<Mp3Info>();
        String[] allPatterns = { "*" };
        String[] audioPatterns = { "*.mp3", "*.wma", "*.flac", "*.m4a" };
        String[] videoPatterns = { "*.mp4", "*.avi", "*.mpg", "*.flv", "*.wmv" };

        private FileInfo[] allFiles = null;
        private FileInfo[] mp3Files = null;
        private FileInfo[] wmaFiles = null;

        private String folderListPath = "folderList.txt";

        private WMPLib.IWMPPlaylist pl;
        private WMPLib.IWMPPlaylistArray plItems;
        private WMPLib.WindowsMediaPlayer windowsMediaPlayer = new WMPLib.WindowsMediaPlayer();

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
                    _btnPlayAll.Text = "\u23F8"; // ⏸ icon only
                    timerNowPlayingText.Enabled = true;
                    break;
                case PlaybackState.Paused:
                    _btnPlayAll.Text = "\u25B6"; // ▶ icon only
                    timerNowPlayingText.Enabled = false;
                    break;
                case PlaybackState.Stopped:
                    _btnPlayAll.Text = "\u25B6"; // ▶ icon only
                    timerNowPlayingText.Enabled = false;
                    break;
            }
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
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                windowsMediaPlayer.controls.stop();
                windowsMediaPlayer.close();
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
                //windowsMediaPlayer.settings.setMode("shuffle", true);

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
            }
            catch (Exception) { }
        }

        private void RebuildList()
        {
            try
            {
                gridView.DataSource = listMp3Infos;
                _lblCount.Text = gridView.RowCount.ToString() + " files";
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

        private void listFiles(String[] searchPattern, String filePath, String searchString)
        {
            // Update directory path history in cbbFilePath
            if (!string.IsNullOrEmpty(filePath) && Directory.Exists(filePath))
            {
                string normPath = Path.GetFullPath(filePath);
                
                // Temporarily disable index changed event if any, or just modify items
                int existingIdx = -1;
                for (int idx = 0; idx < cbbFilePath.Items.Count; idx++)
                {
                    if (Path.GetFullPath(cbbFilePath.Items[idx].ToString()).Equals(normPath, StringComparison.OrdinalIgnoreCase))
                    {
                        existingIdx = idx;
                        break;
                    }
                }

                if (existingIdx != 0)
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
            }

            DirectoryInfo di = new DirectoryInfo(filePath);
            foreach (String s in searchPattern)
            {
                allFiles = di.GetFiles(s, SearchOption.AllDirectories);
                foreach (FileInfo fi in allFiles)
                {
                    if (!searchString.Equals(""))
                    {
                        if (fi.Name.ToLower().Contains(searchString.ToLower()))
                            listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, System.IO.File.GetCreationTime(fi.FullName)));
                    }
                    else
                        listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, System.IO.File.GetCreationTime(fi.FullName)));
                }
            }
        }

        private void listAllFiles(String filePath)
        {
            listFiles(allPatterns, filePath, "");
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
            DirectoryInfo[] folders = di.GetDirectories();
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

        private void ListFiles()
        {
            string _temp = string.Empty;
            try
            {
                ClearMp3List();
                _temp = filePath;
                filePath = cbbFilePath.Text;

                listAudioFiles(filePath);
                listVideoFiles(filePath);

                RebuildList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                filePath = _temp;
                cbbFilePath.Text = filePath;
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

                listAllFiles(filePath);

                RebuildList();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                filePath = _temp;
                cbbFilePath.Text = filePath;
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
            picBxArtwork.Image = null;
            binArtwork = null;
        }

        private void ShowTags(string _path)
        {
            try
            {
                if (!isFolder() && !isVideo())
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
                        binArtwork = (byte[])(selectedMp3.Tag.Pictures[0].Data.Data);
                        picBxArtwork.Image = Image.FromStream(new MemoryStream(binArtwork)).GetThumbnailImage(picBxArtwork.Height, picBxArtwork.Height, null, IntPtr.Zero);
                    }
                    else
                    {
                        binArtwork = null;
                        picBxArtwork.Image = null;
                    }
                }
            }
            catch (Exception e)
            {
                ClearTagFields();
                lblResult.Text = "Error: " + e.Message;
            }
        }

        private void ShowMultiTags(List<string> fileNames)
        {
            string temp = string.Empty;
            string artistTag = string.Empty;
            string titleTag = string.Empty;
            string albumTag = string.Empty;
            string trackTag = string.Empty;
            string yearTag = string.Empty;
            string genreTag = string.Empty;
            string lyricTag = string.Empty;
            string commentTag = string.Empty;

            TagLib.File tempFile = null;
            try
            {
                tempFile = TagLib.File.Create(fileNames[0]);
                artistTag = Manipulator.ArrayToString(tempFile.Tag.Performers, ",");
                titleTag = tempFile.Tag.Title;
                albumTag = tempFile.Tag.Album;
                trackTag = tempFile.Tag.Track.ToString();
                yearTag = tempFile.Tag.Year.ToString();
                genreTag = Manipulator.ArrayToString(tempFile.Tag.Genres, ",");
                lyricTag = tempFile.Tag.Lyrics;
                commentTag = tempFile.Tag.Comment;
            }
            catch (Exception) { }

            foreach (string filePath in fileNames)
            {
                try
                {
                    TagLib.File tag = TagLib.File.Create(filePath);
                    temp = Manipulator.ArrayToString(tag.Tag.Performers, ",");
                    if (temp != artistTag)
                    {
                        txtArtist.Text = "(multiple values)";
                    }
                    else
                    {
                        txtArtist.Text = temp;
                    }
                    temp = tag.Tag.Album;
                    if (temp != albumTag)
                    {
                        txtAlbum.Text = "(multiple values)";
                    }
                    else
                    {
                        txtAlbum.Text = temp;
                    }
                    temp = tag.Tag.Title;
                    if (temp != titleTag)
                    {
                        txtTitle.Text = "(multiple values)";
                    }
                    else
                    {
                        txtTitle.Text = temp;
                    }
                    temp = tag.Tag.Track.ToString();
                    if (temp != trackTag)
                    {
                        txtTrack.Text = "(multiple values)";
                    }
                    else
                    {
                        txtTrack.Text = temp;
                    }
                    temp = tag.Tag.Year.ToString();
                    if (temp != yearTag)
                    {
                        txtYear.Text = "(multiple values)";
                    }
                    else
                    {
                        txtYear.Text = temp;
                    }
                    temp = Manipulator.ArrayToString(tag.Tag.Genres, ",");
                    if (temp != genreTag)
                    {
                        txtGenre.Text = "(multiple values)";
                    }
                    else
                    {
                        txtGenre.Text = temp;
                    }
                    temp = tag.Tag.Lyrics;
                    if (temp != lyricTag)
                    {
                        txtLyrics.Text = "(multiple values)";
                    }
                    else
                    {
                        txtLyrics.Text = temp;
                    }
                    temp = tag.Tag.Comment;
                    if (temp != commentTag)
                    {
                        txtComments.Text = "(multiple values)";
                    }
                    else
                    {
                        txtComments.Text = temp;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        private void _btnSave_Click(object sender, EventArgs e)
        {
            if (gridView.SelectedRows.Count == 1)
            {
                try
                {
                    TagLib.File mp3 = TagLib.File.Create(selectedFileName);
                    mp3.Tag.Performers = new string[] { txtArtist.Text };
                    mp3.Tag.Album = txtAlbum.Text;
                    mp3.Tag.Title = txtTitle.Text;
                    mp3.Tag.Track = Convert.ToUInt32(txtTrack.Text);
                    mp3.Tag.Year = Convert.ToUInt32(txtYear.Text);
                    mp3.Tag.Genres = new string[] { txtGenre.Text };
                    mp3.Tag.Lyrics = txtLyrics.Text;
                    mp3.Tag.Comment = txtComments.Text;
                    mp3.Save();
                    lblResult.Text = "Saved";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                foreach (string filePath in listSelectedFiles)
                {
                    TagLib.File mp3 = TagLib.File.Create(filePath);
                    if (txtArtist.Text != "(multiple values)")
                    {
                        mp3.Tag.Performers = new string[] { txtArtist.Text };
                    }
                    if (txtAlbum.Text != "(multiple values)")
                    {
                        mp3.Tag.Album = txtAlbum.Text;
                    }
                    if (txtTitle.Text != "(multiple values)")
                    {
                        mp3.Tag.Title = txtTitle.Text;
                    }
                    if (txtTrack.Text != "(multiple values)")
                    {
                        mp3.Tag.Track = Convert.ToUInt32(txtTrack.Text);
                    }
                    if (txtYear.Text != "(multiple values)")
                    {
                        mp3.Tag.Year = Convert.ToUInt32(txtYear.Text);
                    }
                    if (txtGenre.Text != "(multiple values)")
                    {
                        mp3.Tag.Genres = new string[] { txtGenre.Text };
                    }
                    if (txtLyrics.Text != "(multiple values)")
                    {
                        mp3.Tag.Lyrics = txtLyrics.Text;
                    }
                    if (txtComments.Text != "(multiple values)")
                    {
                        mp3.Tag.Comment = txtComments.Text;
                    }
                    mp3.Save();
                    lblResult.Text = "Saved";
                }
            }
        }

        private void gridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Header clicked, ignore
            try
            {
                artistFlag = 0;
                titleFlag = 0;
                lblArtwork.Text = string.Empty;
                lblResult.Text = gridView.SelectedRows.Count.ToString() + " selected";
                if (gridView.SelectedRows.Count == 1)
                {
                    selectedRowIndex = gridView.CurrentRow.Index;
                    DisplaySelectedFileInfo();

                    // add to list for setting artwork later
                    listSelectedFiles.Clear();
                    listSelectedFiles.Add(selectedFileName);
                }
                else
                {
                    DisplayMultiSelectedFilesInfo();
                }
                changeCoverFlag = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DisplaySelectedFileInfo()
        {
            if (gridView.CurrentRow == null) return;
            if (gridView.CurrentRow.Cells["ColumnPath"].Value == null) return;

            if (gridView.CurrentRow.Index != selectedRowIndex || gridView.CurrentRow.Index == 0)
            {
                selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
                lblResult.Text = "";
                ShowTags(selectedFileName);
            }
            else { }

        }

        private void DisplayMultiSelectedFilesInfo()
        {
            if (gridView.CurrentRow == null) return;
            if (gridView.CurrentRow.Cells["ColumnPath"].Value == null) return;
            string selectedPath = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            if (isFolder() || isVideo())
            {
                //
            }
            else
            {
                listSelectedFiles.Clear();
                foreach (DataGridViewRow row in gridView.SelectedRows)
                {
                    if (row.Cells["ColumnPath"].Value != null)
                    {
                        listSelectedFiles.Add(row.Cells["ColumnPath"].Value.ToString());
                    }
                }
                ShowMultiTags(listSelectedFiles);
            }
        }

        private void gridView_SelectionChanged(object sender, EventArgs e)
        {
            DisplaySelectedFileInfo();
        }

        //http://forum.codecall.net/csharp-tutorials/20420-tutorial-playing-mp3-files-c.html#post199646
        //http://www.codeproject.com/Articles/14709/Playing-MP3s-using-MCI
        private void PlaySelectedMp3Files()
        {
            try
            {
                if (gridView.CurrentRow == null) return;
                if (gridView.CurrentRow.Cells["ColumnPath"].Value == null) return;
                selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
                if (Manipulator.IsVideoFile(selectedFileName) && _btnList.Enabled == false)
                {
                    System.Diagnostics.Process.Start(selectedFileName);
                }
                else
                {
                    //mciSendString("open \"" + selectedFileName + "\" type mpegvideo alias MediaFile", null, 0, IntPtr.Zero);
                    //mciSendString("play MediaFile" + playMode, null, 0, IntPtr.Zero);

                    if (windowsMediaPlayer.playState == WMPLib.WMPPlayState.wmppsPlaying)
                    {
                        if (replayState == 1) // repeat playlist
                        {
                            windowsMediaPlayer.settings.setMode("loop", true);
                        }
                        else if (_chkShuffle.Checked == true)
                        {
                            windowsMediaPlayer.settings.setMode("shuffle", true);
                        }
                        else
                        {
                            windowsMediaPlayer.settings.setMode("loop", false);
                            windowsMediaPlayer.settings.setMode("shuffle", false);
                        }
                    }
                    else
                    {
                        string myPlaylist = "MyPlayList";

                        plItems = windowsMediaPlayer.playlistCollection.getByName(myPlaylist);

                        pl = windowsMediaPlayer.playlistCollection.newPlaylist(myPlaylist);

                        int index = getSongIndex(selectedFileName);

                        // Build a FULL playlist with all songs so Prev/Next wrap correctly
                        string fi = string.Empty;
                        int wmpStartIndex = 0; // WMP playlist index of the selected song
                        int audioCount = 0;
                        for (int i = 0; i <= gridView.Rows.Count - 1; i++)
                        {
                            fi = gridView.Rows[i].Cells["ColumnPath"].Value.ToString();
                            if (Manipulator.IsAudioFile(fi))
                            {
                                WMPLib.IWMPMedia m1 = windowsMediaPlayer.newMedia(fi);
                                pl.appendItem(m1);
                                if (i < index) wmpStartIndex++; // count audio-only items before selected
                                audioCount++;
                            }
                        }

                        if (replayState == 1) // repeat playlist
                        {
                            windowsMediaPlayer.settings.setMode("loop", true);
                        }

                        if (_chkShuffle.Checked == true)
                        {
                            windowsMediaPlayer.settings.setMode("shuffle", true);
                        }

                        windowsMediaPlayer.currentPlaylist = pl;

                        // Determine start position: random if shuffle, else selected song
                        int startIndex = wmpStartIndex;
                        if (_chkShuffle.Checked && pl.count > 0)
                            startIndex = new System.Random().Next(0, pl.count);

                        if (startIndex < pl.count)
                            windowsMediaPlayer.controls.currentItem = pl.get_Item(startIndex);
                        windowsMediaPlayer.controls.play();
                        UpdatePlaybackUI(PlaybackState.Playing);
 
                        // If shuffle is on, update gridView selection to the randomly chosen song
                        if (_chkShuffle.Checked)
                            new System.Threading.Timer(_ => this.Invoke((Action)SelectCurrentPlayingRow), null, 350, System.Threading.Timeout.Infinite);
 
                        this.Text = "Now Playing - " + Path.GetFileNameWithoutExtension(selectedFileName) + " | ";
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void _btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isFolder())
                {
                    //mciSendString("close MediaFile", null, 0, IntPtr.Zero);
                }

                windowsMediaPlayer.controls.stop();
                windowsMediaPlayer.close();
                UpdatePlaybackUI(PlaybackState.Stopped);

                this.Text = "Mp3TagReader";

                timerNowPlayingText.Enabled = false;


                // progress bar
                setProgressBar(0);
            }
            catch
            {
                MessageBox.Show("No song in list", "Warning");
                return;
            }
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
            DirectoryInfo[] folders = folder.GetDirectories();

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

        private void chkReplay_CheckedChanged(object sender, EventArgs e)
        {
            // Cycle: 0 (Off) -> 1 (Repeat Playlist) -> 2 (Repeat One)
            replayState = (replayState + 1) % 3;
            
            if (replayState == 0)
            {
                _chkReplay.Checked = false;
                _chkReplay.Text = "\u21BA"; // ↺ default icon
                windowsMediaPlayer.settings.setMode("loop", false);
            }
            else if (replayState == 1)
            {
                _chkReplay.Checked = true;
                _chkReplay.Text = "\u21BA\u1D3A"; // ↺ᴬ (All)
                windowsMediaPlayer.settings.setMode("loop", true);
            }
            else if (replayState == 2)
            {
                _chkReplay.Checked = true;
                _chkReplay.Text = "\u21BA\u00B9"; // ↺¹ (One)
                // Disable native playlist loop, we will manually loop the single item in timerSong_Tick
                windowsMediaPlayer.settings.setMode("loop", false);
            }
        }

        private void _btnUntagged_Click(object sender, EventArgs e)
        {
            TagLib.File audio;
            ClearMp3List();
            try
            {
                if (allFiles == null)
                {
                    MessageBox.Show("Please load files first", "Warning");
                    return;
                }
                foreach (FileInfo fi in allFiles)
                {
                    audio = TagLib.File.Create(fi.FullName, ReadStyle.None);
                    if (audio.Tag == null)
                    {
                        listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, System.IO.File.GetCreationTime(fi.FullName)));
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
                        listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, System.IO.File.GetCreationTime(fi.FullName)));
                    }
                }
                gridView.DataSource = listMp3Infos;
                _lblCount.Text = gridView.RowCount.ToString() + " files";
            }
            catch (CorruptFileException ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("No song in list", "Warning");
                return;
            }
        }

        private void _btnPlayList_Click(object sender, EventArgs e)
        {
            if (listMp3Infos.Count == 0)
            {
                MessageBox.Show("No song in list", "Warning");
                return;
            }

            if (currentPlaybackState == PlaybackState.Stopped)
            {
                PlaySelectedMp3Files();
            }
            else if (currentPlaybackState == PlaybackState.Playing)
            {
                windowsMediaPlayer.controls.pause();
                UpdatePlaybackUI(PlaybackState.Paused);
            }
            else if (currentPlaybackState == PlaybackState.Paused)
            {
                windowsMediaPlayer.controls.play();
                UpdatePlaybackUI(PlaybackState.Playing);
            }
        }

        private void gridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Header double-clicked, ignore
            // open folder
            try
            {
                selectedFileName = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
                if (isFolder())
                {
                    ClearMp3List();
                    string searchString = cbbFilePath.Text;

                    listFolders(selectedFileName);
                    listAudioFiles(selectedFileName);
                    listVideoFiles(selectedFileName);

                    gridView.DataSource = listMp3Infos;
                    _lblCount.Text = gridView.RowCount.ToString() + " results";
                }
            }
            catch (Exception) { }
        }

        private bool isFolder()
        {
            string selectedPath = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            return Manipulator.IsFolder(selectedPath);
        }

        private bool isVideo()
        {
            string selectedPath = gridView.CurrentRow.Cells["ColumnPath"].Value.ToString();
            return Manipulator.IsVideoFile(selectedPath);
        }

        private string folderOf(string filePath) // return folder of the selected file
        {
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
            Thread oThread = new Thread(new ThreadStart(getLyrics));
            oThread.Start();
        }

        private void getLyrics()
        {
            try
            {
                LyricWiki wiki = new LyricWiki();
                LyricsResult result = new LyricsResult();
                string artist = txtArtist.Text;
                string title = txtTitle.Text;
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
                    MessageBox.Show("Error fetching lyrics: " + ex.Message, "Error");
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
                if (windowsMediaPlayer.playState == WMPLib.WMPPlayState.wmppsStopped)
                {
                    _btnStop_Click(null, null);
                    progressBar.Value = (int)0;
                }
                else
                {
                    if (currentPlaybackState == PlaybackState.Playing)
                    {
                        // Custom Repeat One handler: if track is near end, loop it back to start
                        if (replayState == 2 && windowsMediaPlayer.currentMedia != null)
                        {
                            double duration = windowsMediaPlayer.currentMedia.duration;
                            double position = windowsMediaPlayer.controls.currentPosition;
                            if (duration > 0 && duration - position <= 1.0)
                            {
                                windowsMediaPlayer.controls.currentPosition = 0;
                            }
                        }

                        string currentSong = windowsMediaPlayer.controls.currentItem.sourceURL;
                        if (currentSong != selectedFileName) // next song
                        {
                            selectedFileName = currentSong;
                            this.Text = "Now Playing - " + Path.GetFileNameWithoutExtension(currentSong) + " | ";
                        }

                        setProgressBar(1);
                    }
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
            if (windowsMediaPlayer.playState == WMPLib.WMPPlayState.wmppsStopped)
            {
                MessageBox.Show("Not Playing", "Warning");
                return;
            }

            try
            {
                windowsMediaPlayer.controls.previous();
                // Wait briefly then select the now-playing row in gridView
                new System.Threading.Timer(_ => this.Invoke((Action)SelectCurrentPlayingRow), null, 350, System.Threading.Timeout.Infinite);
            }
            catch { }
        }

        private void _btnNext_Click(object sender, EventArgs e)
        {
            if (windowsMediaPlayer.playState == WMPLib.WMPPlayState.wmppsStopped)
            {
                MessageBox.Show("Not Playing", "Warning");
                return;
            }

            try
            {
                windowsMediaPlayer.controls.next();
                // Wait briefly then select the now-playing row in gridView
                new System.Threading.Timer(_ => this.Invoke((Action)SelectCurrentPlayingRow), null, 350, System.Threading.Timeout.Infinite);
            }
            catch { }
        }

        /// <summary>Selects the currently playing song row in the gridView.</summary>
        private void SelectCurrentPlayingRow()
        {
            try
            {
                if (windowsMediaPlayer.currentMedia == null) return;
                string currentSong = windowsMediaPlayer.controls.currentItem.sourceURL;
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

                    // Update state so DisplaySelectedFileInfo won't skip ShowTags
                    selectedRowIndex = foundIndex;
                    selectedFileName = currentSong;

                    // Reload artwork + metadata for the now-playing song
                    ShowTags(currentSong);
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
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files[0].EndsWith("jpg") || files[0].EndsWith("png"))
            {
                imagePath = files[0];
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
                    listMp3Infos.Add(new Mp3Info(file, System.IO.Path.GetFileName(file), System.IO.File.GetCreationTime(file)));
                }
                gridView.DataSource = listMp3Infos;
                _lblCount.Text = gridView.RowCount.ToString() + " files";
            }
        }



        private void _chkShuffle_CheckedChanged(object sender, EventArgs e)
        {
            if (_chkShuffle.Checked == true)
            {
                windowsMediaPlayer.settings.setMode("shuffle", true);

            }
            else
            {
                windowsMediaPlayer.settings.setMode("shuffle", false);
            }
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
                if (windowsMediaPlayer.currentMedia == null) return;
                string currentSong = windowsMediaPlayer.controls.currentItem.sourceURL;
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

        private void button1_Click(object sender, EventArgs e)
        {
            TagLib.File audio;
            ClearMp3List();
            try
            {
                if (mp3Files == null || wmaFiles == null)
                {
                    MessageBox.Show("Please load files first", "Warning");
                    return;
                }
                foreach (FileInfo fi in mp3Files)
                {
                    audio = TagLib.File.Create(fi.FullName, ReadStyle.None);
                    if (audio.Tag.Album != null)
                    {
                        if (audio.Tag.Album.ToLower().Contains("/") || Manipulator.ArrayToString(audio.Tag.Performers, ",").ToLower().Contains("/") || Manipulator.ArrayToString(audio.Tag.Performers, ",").ToLower().Contains("/") || (audio.Tag.Title != null && audio.Tag.Title.ToLower().Contains("/")))
                        {
                            listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, System.IO.File.GetCreationTime(fi.FullName)));
                        }
                    }
                }
                foreach (FileInfo fi in wmaFiles)
                {
                    audio = TagLib.File.Create(fi.FullName, ReadStyle.None);
                    if (audio.Tag.Album != null)
                    {
                        if (audio.Tag.Album.ToLower().Contains("/") || Manipulator.ArrayToString(audio.Tag.Performers, ",").ToLower().Contains("/") || Manipulator.ArrayToString(audio.Tag.Performers, ",").ToLower().Contains("/") || (audio.Tag.Title != null && audio.Tag.Title.ToLower().Contains("/")))
                        {
                            listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, System.IO.File.GetCreationTime(fi.FullName)));
                        }
                    }
                }
                gridView.DataSource = listMp3Infos;
                _lblCount.Text = gridView.RowCount.ToString() + " files";
            }
            catch (CorruptFileException ex)
            {
                MessageBox.Show(ex.ToString());
                return;
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
                int rowIndex = gridView.HitTest(e.X, e.Y).RowIndex;

                if (rowIndex == -1)
                {
                    return;
                }

                // clear other selected rows
                gridView.ClearSelection();

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
                double duration = windowsMediaPlayer.currentMedia.duration;
                double position = windowsMediaPlayer.controls.currentPosition;

                progressBar.Minimum = 0;
                progressBar.Maximum = (int)duration;
                progressBar.Value = (int)position;

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
            // set the progress bar's value based on mouse position
            progressBar.Value = (((MouseEventArgs)e).X) * progressBar.Maximum / progressBar.Width;

            // set the media's position
            windowsMediaPlayer.controls.currentPosition = progressBar.Value;
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
            beforeRenamed = gridView.CurrentCell.Value.ToString();
            renamePath = selectedFileName;
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
            string afterRenamed = gridView.CurrentCell.Value.ToString();
            if (afterRenamed != beforeRenamed)
            {
                if (MessageBox.Show("Modify file name?", "Info", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    System.IO.File.Move(renamePath, Path.Combine(Path.GetDirectoryName(renamePath), afterRenamed));
                    gridView.ReadOnly = true;
                    gridView.RefreshEdit();

                    gridView.CurrentRow.Cells["ColumnPath"].Value = Path.Combine(Path.GetDirectoryName(renamePath), afterRenamed); // prevent file not found exception after renamed
                }
                else
                {
                    gridView.CurrentCell.Value = beforeRenamed;
                }
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
                        if (trackNo != 0)
                        {
                            TagLib.File mp3 = TagLib.File.Create(path);
                            mp3.Tag.Track = Convert.ToUInt32(trackNo);
                            mp3.Save();
                        }

                        count++;
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
                    cbbFilePath.Text = downloadForm.OutputFolder;
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



        private void cbbFilePath_TextChanged(object sender, EventArgs e)
        {
            if (_btnList.Enabled == false)
            {
                string searchString = cbbFilePath.Text;
                ClearMp3List();
                int count = 0;
                try
                {
                    count = mp3Files.Length;
                }
                catch
                {
                    count = 0;
                }
                if (searchString.Length == 0 && count < 2000)
                {
                    listAudioFiles(selectedFilePath);
                    listVideoFiles(selectedFilePath);
                    listFolders(selectedFilePath);
                }
                else
                {
                    // folder search results
                    listFolders(selectedFilePath, searchString);
                    // audio search results
                    listFiles(audioPatterns, filePath, searchString);
                    // video search results
                    listFiles(videoPatterns, filePath, searchString);
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
                Image image = Image.FromStream(new MemoryStream(binArtwork)).GetThumbnailImage(500, 500, null, IntPtr.Zero);
                ArtworkViewer av = new ArtworkViewer(image);
                av.ShowDialog();
            }
        }

        private List<TagLib.File> ls = new List<TagLib.File>();
        private String imagePath = "1";
        private Boolean unloadArtwork = false;
        private Boolean changeCoverFlag = false;

        //http://stackoverflow.com/questions/13667378/embed-album-art-in-mp3-using-tag-lib-c-sharp

        private void btnChangeCover_Click(object sender, EventArgs e)
        {
            unloadArtwork = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                imagePath = openFileDialog.FileName;

                // define picture
                definePicture(imagePath);
            }
            else { }
        }

        private void btnSaveCover_Click(object sender, EventArgs e)
        {
            try
            {
                if (gridView.SelectedRows.Count >= 1)
                {
                    if (changeCoverFlag == true)
                    {
                        // define picture
                        TagLib.Id3v2.AttachedPictureFrame pic = new TagLib.Id3v2.AttachedPictureFrame();
                        if (!imagePath.Equals(string.Empty))
                        {
                            pic.TextEncoding = TagLib.StringType.Latin1;
                            pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                            pic.Type = TagLib.PictureType.FrontCover;
                            pic.Data = TagLib.ByteVector.FromPath(imagePath);
                        }

                        foreach (String selectedFile in listSelectedFiles)
                        {
                            TagLib.File mp3 = TagLib.File.Create(selectedFile);

                            // save or unload picture to file
                            if (!unloadArtwork && !imagePath.Equals(string.Empty))
                            {
                                mp3.Tag.Pictures = new TagLib.IPicture[1] { pic };
                            }
                            else
                            {
                                mp3.Tag.Pictures = null;
                            }

                            mp3.Save();
                            changeCoverFlag = false; // void the next Save attempt
                        }
                    }
                    else
                    {
                        lblArtwork.Text = "No cover to change";
                    }
                }

                if (picBxArtwork.Image != null)
                {
                    lblArtwork.Text = listSelectedFiles.Count + " artwork(s) set successfully";
                }
                else
                {
                    binArtwork = null;
                    lblArtwork.Text = listSelectedFiles.Count + " artwork(s) unset successfully";
                }
            }
            catch (Exception) { }
        }

        private void btnUnloadCover_Click(object sender, EventArgs e)
        {
            imagePath = string.Empty;
            unloadArtwork = true;
            picBxArtwork.Image = null;
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
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // define picture
            definePicture(files[0]);
        }

        private void definePicture(String filePath)
        {
            TagLib.Id3v2.AttachedPictureFrame pic = new TagLib.Id3v2.AttachedPictureFrame();
            pic.TextEncoding = TagLib.StringType.Latin1;
            pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
            pic.Type = TagLib.PictureType.FrontCover;
            pic.Data = TagLib.ByteVector.FromPath(filePath);

            binArtwork = (byte[])(pic.Data.Data);
            picBxArtwork.Image = Image.FromStream(new MemoryStream(binArtwork)).GetThumbnailImage(picBxArtwork.Height, picBxArtwork.Height, null, IntPtr.Zero);

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
            
            // Put total count on the left, and selected count on the far right of the table
            _lblCount.Bounds = new Rectangle(175, 512, 150, 20);
            lblResult.Bounds = new Rectangle(520, 512, 150, 20);
            lblResult.TextAlign = ContentAlignment.MiddleRight;

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