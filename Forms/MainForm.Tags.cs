using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Mp3TagReader.Helpers;
using Mp3TagReader.Models;

namespace Mp3TagReader.Forms
{
    // Tag editor (right panel): selection, reading / saving tags, cover art and the tag tools
    public partial class MainForm
    {
        private const string MultipleValues = "(multiple values)";

        // file shown in the tag editor (the selected row); kept separate from nowPlayingFile
        private string selectedFileName = string.Empty;

        // audio files the editor saves to: the selected audio rows
        private readonly List<string> listSelectedFiles = new List<string>();

        // file whose tags are currently shown in the editor (null when several files are shown)
        private string tagsShownFor = null;

        #region Selection

        private void gridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Header clicked, ignore
            artistFromEnd = true;
            titleFromStart = true;
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

            if (selectedPaths.Count == 0)
            {
                // empty list: nothing to save to any more (a selection being changed by code passes through here too,
                // so the editor is only cleared when the list itself is empty)
                if (gridView.Rows.Count == 0)
                {
                    listSelectedFiles.Clear();
                    selectedFileName = string.Empty;
                    tagsShownFor = null;
                    ClearTagFields();
                }
                return;
            }

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

        #endregion

        #region Show / save tags

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

        private void ShowTags(string path)
        {
            try
            {
                if (!Manipulator.IsAudioFile(path))
                {
                    // folder / video / other file: don't leave the previous song's tags on screen
                    ClearTagFields();
                    return;
                }

                TagLib.File file = TagLib.File.Create(path);
                txtArtist.Text = Manipulator.ArrayToString(file.Tag.Performers, ",");
                txtAlbum.Text = file.Tag.Album;
                txtTitle.Text = file.Tag.Title;
                txtTrack.Text = file.Tag.Track.ToString();
                txtYear.Text = file.Tag.Year.ToString();
                txtGenre.Text = Manipulator.ArrayToString(file.Tag.Genres, ",");
                txtBitrate.Text = file.Properties.AudioBitrate.ToString() + " kbps";
                txtLyrics.Text = file.Tag.Lyrics;
                txtComments.Text = file.Tag.Comment;
                SetArtworkImage(file.Tag.Pictures.Length >= 1 ? (byte[])file.Tag.Pictures[0].Data.Data : null);
            }
            catch (Exception e)
            {
                ClearTagFields();
                lblResult.Text = "Error: " + e.Message;
            }
        }

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
                    TagLib.File file = TagLib.File.Create(path);
                    string[] values = new string[]
                    {
                        Manipulator.ArrayToString(file.Tag.Performers, ","),
                        file.Tag.Album,
                        file.Tag.Title,
                        file.Tag.Track.ToString(),
                        file.Tag.Year.ToString(),
                        Manipulator.ArrayToString(file.Tag.Genres, ","),
                        file.Tag.Lyrics,
                        file.Tag.Comment
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

        // empty text = 0; anything else must be a positive number
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

        // with several files, a field still showing "(multiple values)" is left untouched
        private bool ShouldWrite(TextBox box, bool multi)
        {
            return !multi || box.Text != MultipleValues;
        }

        private void _btnSave_Click(object sender, EventArgs e)
        {
            if (listSelectedFiles.Count == 0)
            {
                MessageBox.Show("Please select an audio file first.", "Warning");
                return;
            }

            bool multi = listSelectedFiles.Count > 1;
            uint track = 0;
            uint year = 0;
            bool setTrack = ShouldWrite(txtTrack, multi);
            bool setYear = ShouldWrite(txtYear, multi);
            if (setTrack && !TryParseTagNumber(txtTrack, "Track", out track)) return;
            if (setYear && !TryParseTagNumber(txtYear, "Year", out year)) return;

            int saved = 0;
            List<string> errors = new List<string>();
            foreach (string path in listSelectedFiles)
            {
                try
                {
                    TagLib.File file = TagLib.File.Create(path);
                    if (ShouldWrite(txtArtist, multi)) file.Tag.Performers = Manipulator.StringToArray(txtArtist.Text, ',');
                    if (ShouldWrite(txtAlbum, multi)) file.Tag.Album = txtAlbum.Text;
                    if (ShouldWrite(txtTitle, multi)) file.Tag.Title = txtTitle.Text;
                    if (setTrack) file.Tag.Track = track;
                    if (setYear) file.Tag.Year = year;
                    if (ShouldWrite(txtGenre, multi)) file.Tag.Genres = Manipulator.StringToArray(txtGenre.Text, ',');
                    if (ShouldWrite(txtLyrics, multi)) file.Tag.Lyrics = txtLyrics.Text;
                    if (ShouldWrite(txtComments, multi)) file.Tag.Comment = txtComments.Text;
                    file.Save();
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

        private void _btnInfo_Click(object sender, EventArgs e)
        {
            string path = selectedFileName;
            if (listSelectedFiles.Count != 1 || !Manipulator.IsAudioFile(path))
            {
                MessageBox.Show("Please select one audio file.", "Warning");
                return;
            }

            FullInfo info = new FullInfo(path);
            info.FormClosed += delegate
            {
                // tags saved in Full Info: show them in the editor too
                if (info.Saved && selectedFileName == path)
                {
                    ShowTags(path);
                    tagsShownFor = path;
                }
            };
            info.Show();
        }

        #endregion

        #region Quick fill (click on a field label)

        // label click toggles: first click takes one part of "Title - Artist.mp3", the next click the other part
        private bool artistFromEnd = true;
        private bool titleFromStart = true;

        // "Song - Artist.mp3" -> "Song" / "Artist"; without "-" both parts are the whole name
        private static void SplitFileName(string path, out string beforeDash, out string afterDash)
        {
            string name = Path.GetFileNameWithoutExtension(path ?? string.Empty);
            int dash = name.LastIndexOf('-');
            if (dash > 0)
            {
                beforeDash = name.Substring(0, dash).Trim();
                afterDash = name.Substring(dash + 1).Trim();
            }
            else
            {
                beforeDash = name.Trim();
                afterDash = beforeDash;
            }
        }

        private void lblArtist_Click(object sender, EventArgs e)
        {
            string before, after;
            SplitFileName(selectedFileName, out before, out after);
            txtArtist.Text = artistFromEnd ? after : before;
            artistFromEnd = !artistFromEnd;
        }

        private void lblTitle_Click(object sender, EventArgs e)
        {
            string before, after;
            SplitFileName(selectedFileName, out before, out after);
            txtTitle.Text = titleFromStart ? before : after;
            titleFromStart = !titleFromStart;
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

        private void lblGenre_Click(object sender, EventArgs e)
        {
            txtGenre.Text = "EDM";
        }

        private void lblComments_Click(object sender, EventArgs e)
        {
            txtComments.Text = string.Empty;
        }

        // track number from the first two characters of the file name ("07 Song.mp3" -> 7), 0 when there is none
        private static int TrackFromFileName(string path)
        {
            string name = Path.GetFileName(path ?? string.Empty);
            int track;
            if (name.Length >= 2 && int.TryParse(name.Substring(0, 2), out track) && track > 0)
            {
                return track;
            }
            return 0;
        }

        // track from the file name, otherwise the next number
        private void lblTrack_Click(object sender, EventArgs e)
        {
            int track = TrackFromFileName(selectedFileName);
            if (track == 0)
            {
                int current;
                track = int.TryParse(txtTrack.Text, out current) && current > 0 ? current + 1 : 1;
            }
            txtTrack.Text = track.ToString();
        }

        // this year, then one year earlier per click
        private void lblYear_Click(object sender, EventArgs e)
        {
            int year;
            if (int.TryParse(txtYear.Text, out year) && year > 0)
            {
                txtYear.Text = (year - 1).ToString();
            }
            else
            {
                txtYear.Text = DateTime.Now.Year.ToString();
            }
        }

        #endregion

        #region Lyrics

        private void txtLyrics_DoubleClick(object sender, EventArgs e)
        {
            if (txtLyrics.Text != string.Empty)
            {
                LyricsViewer lv = new LyricsViewer(txtArtist.Text, txtTitle.Text, txtLyrics.Text);
                lv.Show();
            }
        }

        private void _btnClearLyrics_Click(object sender, EventArgs e)
        {
            txtLyrics.Clear();
        }

        // search the lyrics on the web (the old LyricWiki service no longer exists)
        private void _btnGetLyrics_Click(object sender, EventArgs e)
        {
            Searcher searchLyric = new Searcher((txtTitle.Text + " " + txtArtist.Text).Trim());
            searchLyric.Show(this);
        }

        #endregion

        #region Tag tools

        private static bool IsUntagged(TagLib.Tag tag)
        {
            if (tag == null) return true;
            if (string.IsNullOrEmpty(tag.Album) ||
                string.IsNullOrEmpty(Manipulator.ArrayToString(tag.Performers, ",")) ||
                string.IsNullOrEmpty(tag.Title))
            {
                return true;
            }

            // album filled with a download site name
            string album = tag.Album.ToLower();
            return album.Contains("zing") || album.Contains(".com") || album.Contains(".vn") || album.Contains(".org");
        }

        // keep only the audio files of the list whose tags are missing or junk
        private void _btnUntagged_Click(object sender, EventArgs e)
        {
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
            foreach (Mp3Info info in candidates)
            {
                try
                {
                    if (IsUntagged(TagLib.File.Create(info.Path, TagLib.ReadStyle.None).Tag))
                    {
                        listMp3Infos.Add(info);
                    }
                }
                catch (Exception)
                {
                    // corrupt / unsupported file: skip it and keep checking the others
                    unreadable++;
                }
            }
            listContent = ListContent.Other;
            ShowList(" files" + (unreadable > 0 ? " (" + unreadable + " unreadable)" : ""));
        }

        // set the track number of every listed file from the first two characters of its name
        private void fixTrackNumberToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int count = 0;
            foreach (DataGridViewRow row in gridView.Rows)
            {
                try
                {
                    string path = row.Cells["ColumnPath"].Value.ToString();
                    int track = TrackFromFileName(path);
                    if (track > 0 && File.Exists(path))
                    {
                        TagLib.File file = TagLib.File.Create(path);
                        file.Tag.Track = (uint)track;
                        file.Save();
                        count++; // only count files that were actually updated
                    }
                }
                catch (Exception) { }
            }

            MessageBox.Show(count + " Tracks Updated", "Information");
        }

        #endregion

        #region Cover art

        private byte[] binArtwork;
        private string imagePath = string.Empty;
        private bool unloadArtwork = false;
        private bool changeCoverFlag = false;

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

        private void picBxArtwork_Click(object sender, EventArgs e)
        {
            if (binArtwork == null) return;

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

        // show the new cover and remember its path; it is written to the files by btnSaveCover_Click
        private void definePicture(string path)
        {
            try
            {
                SetArtworkImage(File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                lblArtwork.Text = "Cannot load image: " + ex.Message;
                return;
            }

            imagePath = path;
            unloadArtwork = false;
            changeCoverFlag = true;
        }

        //http://stackoverflow.com/questions/13667378/embed-album-art-in-mp3-using-tag-lib-c-sharp
        private void btnChangeCover_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                definePicture(openFileDialog.FileName);
            }
        }

        private void btnUnloadCover_Click(object sender, EventArgs e)
        {
            imagePath = string.Empty;
            unloadArtwork = true;
            SetArtworkImage(null);
            changeCoverFlag = true;
        }

        private void picBxArtwork_DragEnter(object sender, DragEventArgs e)
        {
            AcceptFileDrop(e);
        }

        private void picBxArtwork_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;
            definePicture(files[0]);
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
            foreach (string path in listSelectedFiles)
            {
                try
                {
                    TagLib.File file = TagLib.File.Create(path);
                    file.Tag.Pictures = removeCover ? new TagLib.IPicture[0] : new TagLib.IPicture[] { pic };
                    file.Save();
                    saved++;
                }
                catch (Exception ex)
                {
                    errors.Add(Path.GetFileName(path) + ": " + ex.Message);
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

        #endregion
    }
}
