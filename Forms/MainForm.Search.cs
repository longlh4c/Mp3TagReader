using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Mp3TagReader.Helpers;
using Mp3TagReader.Models;

namespace Mp3TagReader.Forms
{
    // "Search ON": cbbFilePath becomes a search box. The names of the folders and files below the search root
    // (all levels) are searched while typing; the folder tree and the history list change the search root.
    public partial class MainForm
    {
        private const int SearchDelayMs = 300;

        // search text typed in cbbFilePath (kept when a folder is picked from the history list)
        private string searchText = string.Empty;

        // runs the search once typing pauses, instead of on every key
        private Timer searchTimer;

        // "Searching in: <folder>" under the list while in search mode
        private Label lblSearchRoot;

        // folders and files below the search root, read once and filtered in memory while typing
        private string indexRoot;
        private List<DirectoryInfo> indexFolders;
        private List<FileInfo> indexFiles;

        private void InitializeSearch()
        {
            searchTimer = new Timer();
            searchTimer.Interval = SearchDelayMs;
            searchTimer.Tick += delegate { RunSearch(); };

            lblSearchRoot = new Label();
            lblSearchRoot.AutoSize = false;
            lblSearchRoot.AutoEllipsis = true;
            lblSearchRoot.Bounds = new Rectangle(_lblCount.Right + 5, _lblCount.Top, gridView.Right - _lblCount.Right - 5, _lblCount.Height);
            lblSearchRoot.ForeColor = SystemColors.GrayText;
            lblSearchRoot.Visible = false;
            _lblCount.Parent.Controls.Add(lblSearchRoot);

            // "All Files" changes which files the search shows
            _chkAllFiles.CheckedChanged += delegate { if (searchMode) RunSearch(); };
        }

        private void _btnSearch_Click(object sender, EventArgs e)
        {
            if (!searchMode)
            {
                StartSearchMode();
            }
            else
            {
                StopSearchMode();
            }
        }

        private void StartSearchMode()
        {
            searchMode = true;
            _btnList.Enabled = false;
            _btnSearch.Text = "Search ON";
            if (string.IsNullOrEmpty(searchRoot)) SetSearchRoot(currentFolder);

            searchText = string.Empty;
            SetPathTextSilently(string.Empty);
            InvalidateSearchIndex(); // read the folder again: files may have changed since the last search
            UpdateSearchRootLabel();
            cbbFilePath.Focus();
            RunSearch(); // empty text: everything below the search root
        }

        // back to the folder list: the combobox shows the searched folder again and its files are listed
        private void StopSearchMode()
        {
            searchTimer.Stop();
            searchMode = false;
            _btnList.Enabled = true;
            _btnSearch.Text = "Search OFF";
            UpdateSearchRootLabel();

            string folder = !string.IsNullOrEmpty(searchRoot) ? searchRoot : currentFolder;
            if (Directory.Exists(folder))
            {
                try
                {
                    ListAllFilesOfFolder(folder, CurrentRowPath());
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            SetPathTextSilently(currentFolder);
        }

        private void cbbFilePath_TextChanged(object sender, EventArgs e)
        {
            if (!searchMode || suppressFolderSearch) return;

            string text = cbbFilePath.Text;
            if (IsHistoryEntry(text))
            {
                // a folder picked from the history list: it becomes the search root and the search text stays.
                // The combobox is still updating its text here, so put the search text back afterwards.
                SetSearchRoot(text);
                BeginInvoke((MethodInvoker)delegate
                {
                    SetPathTextSilently(searchText);
                    cbbFilePath.SelectionStart = searchText.Length;
                    RunSearch();
                });
                return;
            }

            searchText = text;
            searchTimer.Stop();
            searchTimer.Start();
        }

        private bool IsHistoryEntry(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (object item in cbbFilePath.Items)
            {
                if (string.Equals(item.ToString(), text, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void UpdateSearchRootLabel()
        {
            if (lblSearchRoot == null) return;
            string root = !string.IsNullOrEmpty(searchRoot) ? searchRoot : currentFolder;
            lblSearchRoot.Text = "Searching in: " + root;
            lblSearchRoot.Visible = searchMode;
        }

        private void InvalidateSearchIndex()
        {
            indexRoot = null;
            indexFolders = null;
            indexFiles = null;
        }

        // read every folder and file below root once; later searches in the same root only filter these lists
        private void BuildSearchIndex(string root)
        {
            if (indexRoot != null && string.Equals(indexRoot, root, StringComparison.OrdinalIgnoreCase)) return;

            Cursor previous = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                List<DirectoryInfo> folders = new List<DirectoryInfo>();
                List<FileInfo> files = new List<FileInfo>();
                Stack<DirectoryInfo> pending = new Stack<DirectoryInfo>();
                pending.Push(new DirectoryInfo(root));
                while (pending.Count > 0)
                {
                    DirectoryInfo dir = pending.Pop();
                    files.AddRange(getFilesSafe(dir, "*", SearchOption.TopDirectoryOnly));
                    foreach (DirectoryInfo sub in getDirectoriesSafe(dir))
                    {
                        folders.Add(sub);
                        pending.Push(sub);
                    }
                }

                indexRoot = root;
                indexFolders = folders;
                indexFiles = files;
            }
            finally
            {
                Cursor.Current = previous;
            }
        }

        // Folders first, then files, both sorted by name.
        // Empty search text: the direct subfolders of the search root and every file below it.
        // Otherwise: the folders whose name contains the text (not the folders inside a match) and the files whose
        // name contains it. Files are audio / video, or every file with "All Files".
        private void RunSearch()
        {
            searchTimer.Stop();
            ClearMp3List();
            listContent = ListContent.SearchResults;
            UpdateSearchRootLabel();

            string root = !string.IsNullOrEmpty(searchRoot) ? searchRoot : currentFolder;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                gridView.DataSource = listMp3Infos;
                _lblCount.Text = "Select a folder first";
                return;
            }

            try
            {
                root = Path.GetFullPath(root).TrimEnd('\\');
                BuildSearchIndex(root);
                string text = searchText.Trim();

                List<DirectoryInfo> folders = new List<DirectoryInfo>();
                if (text.Length == 0)
                {
                    foreach (DirectoryInfo dir in indexFolders)
                    {
                        if (dir.Parent != null && string.Equals(dir.Parent.FullName.TrimEnd('\\'), root, StringComparison.OrdinalIgnoreCase))
                        {
                            folders.Add(dir);
                        }
                    }
                }
                else
                {
                    // shallower folders first, so a match hides the matches inside it
                    List<DirectoryInfo> byDepth = new List<DirectoryInfo>(indexFolders);
                    byDepth.Sort(delegate(DirectoryInfo a, DirectoryInfo b) { return a.FullName.Length.CompareTo(b.FullName.Length); });
                    foreach (DirectoryInfo dir in byDepth)
                    {
                        if (dir.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        bool insideMatch = false;
                        foreach (DirectoryInfo match in folders)
                        {
                            if (dir.FullName.StartsWith(match.FullName + "\\", StringComparison.OrdinalIgnoreCase))
                            {
                                insideMatch = true;
                                break;
                            }
                        }
                        if (!insideMatch) folders.Add(dir);
                    }
                }
                folders.Sort(delegate(DirectoryInfo a, DirectoryInfo b) { return CompareNames(a.Name, b.Name); });

                bool allFiles = _chkAllFiles.Checked;
                List<FileInfo> files = new List<FileInfo>();
                foreach (FileInfo fi in indexFiles)
                {
                    if (!allFiles && !Manipulator.IsAudioFile(fi.Name) && !Manipulator.IsVideoFile(fi.Name)) continue;
                    if (text.Length > 0 && fi.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    files.Add(fi);
                }
                files.Sort(delegate(FileInfo a, FileInfo b) { return CompareNames(a.Name, b.Name); });

                foreach (DirectoryInfo dir in folders)
                {
                    listMp3Infos.Add(new FolderInfo(dir.FullName, dir.Name));
                }
                foreach (FileInfo fi in files)
                {
                    listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, fi.CreationTime));
                }
                ShowList(" results"); // also "0 results"
            }
            catch (Exception ex)
            {
                gridView.DataSource = listMp3Infos;
                _lblCount.Text = "Search error: " + ex.Message;
            }
        }
    }
}
