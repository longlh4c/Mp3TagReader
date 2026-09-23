using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Mp3TagReader.Helpers;
using Mp3TagReader.Models;

namespace Mp3TagReader.Forms
{
    // File list: folder history, browsing, search, folder tree and file operations on the list
    public partial class MainForm
    {
        private static readonly string[] allPatterns = { "*" };
        private static readonly string[] audioPatterns = { "*.mp3", "*.wma", "*.flac", "*.m4a" };
        private static readonly string[] videoPatterns = { "*.mp4", "*.avi", "*.mpg", "*.flv", "*.wmv" };
        private const int MaxHistory = 10;

        // next to the exe, not in the working directory (which depends on how the app was started)
        private static readonly string folderListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "folderList.txt");

        // what the list shows, so it can be refreshed the same way
        private enum ListContent
        {
            None,
            AllFilesOfFolder, // List Files: every file of the folder and its subfolders
            FolderBrowse,     // double-click on a folder: "..", subfolders, files of that folder
            SearchResults,
            Other             // Untagged, dropped files, ...
        }
        private ListContent listContent = ListContent.None;

        // folder whose content the list shows (List Files / browsing)
        private string currentFolder = string.Empty;

        // folder chosen in the combobox or the tree; the search looks inside it
        private string searchRoot = string.Empty;

        // true while the code itself changes cbbFilePath, so cbbFilePath_TextChanged won't start a search
        private bool suppressFolderSearch = false;

        // tree node the context menu was opened on
        private TreeNode contextNode;

        // "Search ON": cbbFilePath holds the search text instead of a folder
        private bool IsSearchMode
        {
            get { return !_btnList.Enabled; }
        }

        #region Folder history (cbbFilePath + folderList.txt)

        private void reloadFolderList()
        {
            cbbFilePath.Items.Clear();
            if (File.Exists(folderListPath))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(folderListPath))
                    {
                        if (line.Trim().Length > 0 && cbbFilePath.Items.Count < MaxHistory)
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

        // Move the listed folder to the top of the history and save it to folderList.txt.
        // Only called for an explicit "List Files", never while browsing or searching.
        private void addToFolderHistory(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

            string normPath = Path.GetFullPath(folder);
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
                cbbFilePath.Items.Insert(0, folder);
                while (cbbFilePath.Items.Count > MaxHistory)
                {
                    cbbFilePath.Items.RemoveAt(cbbFilePath.Items.Count - 1);
                }
                cbbFilePath.SelectedIndex = 0;
            }
            finally
            {
                suppressFolderSearch = false;
            }

            try
            {
                List<string> lines = new List<string>();
                foreach (object item in cbbFilePath.Items)
                {
                    lines.Add(item.ToString());
                }
                File.WriteAllLines(folderListPath, lines.ToArray());
            }
            catch { }
        }

        // change the combobox text without starting a search
        private void SetPathTextSilently(string text)
        {
            suppressFolderSearch = true;
            try
            {
                cbbFilePath.Text = text;
            }
            finally
            {
                suppressFolderSearch = false;
            }
        }

        private void SetSearchRoot(string folder)
        {
            searchRoot = folder;
        }

        private void cbbFilePath_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetSearchRoot(cbbFilePath.Text);
        }

        #endregion

        #region File system helpers

        // GetFiles(..., AllDirectories) aborts on the first folder without access; walk the tree manually instead
        private static List<FileInfo> getFilesSafe(DirectoryInfo di, string searchPattern, SearchOption option)
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

        private static DirectoryInfo[] getDirectoriesSafe(DirectoryInfo di)
        {
            try
            {
                return di.GetDirectories();
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            return new DirectoryInfo[0];
        }

        private static int CompareNames(string a, string b)
        {
            return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
        }

        #endregion

        #region List

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

        // bind the list to the grid and show the number of entries ("the .." row is not one of them)
        private void ShowList(string suffix)
        {
            gridView.DataSource = listMp3Infos;
            int count = 0;
            foreach (Mp3Info item in listMp3Infos)
            {
                if (!(item is ParentFolderInfo)) count++;
            }
            _lblCount.Text = count + suffix;
        }

        // Content of one folder: "..", its subfolders, then the files directly inside it (audio + video, or every
        // file when allFiles). Files of the subfolders are not included: double-click a subfolder to browse into it.
        private void listFolderContent(string folder, bool allFiles)
        {
            DirectoryInfo di = new DirectoryInfo(folder);

            // ".." row to go up (not at the root of a drive)
            if (di.Parent != null)
            {
                listMp3Infos.Add(new ParentFolderInfo(di.Parent.FullName));
            }

            List<DirectoryInfo> folders = new List<DirectoryInfo>(getDirectoriesSafe(di));
            folders.Sort(delegate(DirectoryInfo a, DirectoryInfo b) { return CompareNames(a.Name, b.Name); });
            foreach (DirectoryInfo sub in folders)
            {
                listMp3Infos.Add(new FolderInfo(sub.FullName, sub.Name));
            }

            List<string> patterns = new List<string>();
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
            foreach (string pattern in patterns)
            {
                foreach (FileInfo fi in getFilesSafe(di, pattern, SearchOption.TopDirectoryOnly))
                {
                    if (seen.Add(fi.FullName)) files.Add(fi);
                }
            }
            files.Sort(delegate(FileInfo a, FileInfo b) { return CompareNames(a.Name, b.Name); });
            foreach (FileInfo fi in files)
            {
                listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, fi.CreationTime));
            }
        }

        private static string CheckFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException("Folder not found: " + folder);
            }
            return Path.GetFullPath(folder);
        }

        // the list now shows folder: outside search mode the combobox follows it, so "List Files" lists it again
        private void SetCurrentFolder(string folder, ListContent content)
        {
            currentFolder = folder;
            listContent = content;
            if (!IsSearchMode)
            {
                SetPathTextSilently(folder);
                SetSearchRoot(folder);
            }
        }

        // List Files: every audio / video file (or every file with "All Files") of the folder and all its subfolders
        private void ListAllFilesOfFolder(string folder, string selectPath)
        {
            folder = CheckFolder(folder);

            ClearMp3List();
            if (_chkAllFiles.Checked)
            {
                addMatchingFiles(allPatterns, folder, "");
            }
            else
            {
                addMatchingFiles(audioPatterns, folder, "");
                addMatchingFiles(videoPatterns, folder, "");
            }
            ShowList(" files");
            SetCurrentFolder(folder, ListContent.AllFilesOfFolder);

            if (selectPath != null) SelectRowByPath(selectPath);
        }

        // Double-click on a folder: "..", its subfolders, then the files directly inside it.
        // selectPath is the row to select afterwards (or null).
        private void BrowseFolder(string folder, string selectPath)
        {
            folder = CheckFolder(folder);

            ClearMp3List();
            listFolderContent(folder, _chkAllFiles.Checked);
            ShowList(" files");
            SetCurrentFolder(folder, ListContent.FolderBrowse);

            if (selectPath != null) SelectRowByPath(selectPath);
        }

        // "List Files" on a folder: list it and put it on top of the history
        private void ListFolder(string folder)
        {
            try
            {
                ListAllFilesOfFolder(folder, null);
                addToFolderHistory(folder);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                if (!IsSearchMode) SetPathTextSilently(currentFolder);
            }
        }

        // "List Files": the folder typed or chosen in the combobox
        private void ListFiles()
        {
            ListFolder(cbbFilePath.Text.Trim());
        }

        private void _btnList_Click(object sender, EventArgs e)
        {
            ListFiles();
        }

        // reload what the list shows (after a file was converted, changed, ...), keeping the selected row
        private void RefreshList()
        {
            string keep = CurrentRowPath();
            try
            {
                switch (listContent)
                {
                    case ListContent.SearchResults:
                        RunSearch();
                        if (keep != null) SelectRowByPath(keep);
                        break;
                    case ListContent.FolderBrowse:
                        BrowseFolder(currentFolder, keep);
                        break;
                    case ListContent.AllFilesOfFolder:
                        ListAllFilesOfFolder(currentFolder, keep);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // remove the selected entries from the list (the files stay on disk)
            List<Mp3Info> remove = new List<Mp3Info>();
            foreach (DataGridViewRow row in gridView.SelectedRows)
            {
                Mp3Info item = row.DataBoundItem as Mp3Info;
                if (item != null && !(item is ParentFolderInfo)) remove.Add(item);
            }
            foreach (Mp3Info item in remove)
            {
                listMp3Infos.Remove(item);
            }
            ShowList(" files");
        }

        private void removeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearMp3List();
            listContent = ListContent.None;
            _lblCount.Text = "0 files";
        }

        private void gridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                gridView.Rows[0].Selected = true;
            }
            catch { }
        }

        #endregion

        #region Grid rows

        private string CurrentRowPath()
        {
            if (gridView.CurrentRow == null || gridView.CurrentRow.Index < 0) return null;
            object value = gridView.CurrentRow.Cells["ColumnPath"].Value;
            return value != null ? value.ToString() : null;
        }

        // select the row of a file / folder (not the ".." row) and scroll to it
        private bool SelectRowByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            foreach (DataGridViewRow row in gridView.Rows)
            {
                object value = row.Cells["ColumnPath"].Value;
                if (!(row.DataBoundItem is ParentFolderInfo) && value != null &&
                    string.Equals(value.ToString(), path, StringComparison.OrdinalIgnoreCase))
                {
                    gridView.ClearSelection();
                    gridView.CurrentCell = row.Cells["ColumnName"];
                    row.Selected = true;
                    gridView.FirstDisplayedScrollingRowIndex = row.Index;
                    return true;
                }
            }
            return false;
        }

        private void gridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Header double-clicked, ignore
            string path = CurrentRowPath();
            if (path == null) return;

            // open folder or play song
            try
            {
                if (Directory.Exists(path))
                {
                    // after "..", select the folder we just left
                    bool goingUp = gridView.CurrentRow.DataBoundItem is ParentFolderInfo;
                    BrowseFolder(path, goingUp ? currentFolder : null);
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

        private void gridView_MouseDown(object sender, MouseEventArgs e)
        {
            // Show menu only if the right mouse button is clicked.
            if (e.Button != MouseButtons.Right) return;

            DataGridView.HitTestInfo hit = gridView.HitTest(e.X, e.Y);
            if (hit.RowIndex == -1) return;

            // make the clicked row the only selected and the current row: the context menu actions work on it
            gridView.ClearSelection();
            if (hit.ColumnIndex >= 0 && gridView.Columns[hit.ColumnIndex].Visible)
            {
                gridView.CurrentCell = gridView.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
            }
            gridView.Rows[hit.RowIndex].Selected = true;
            contextGrid.Show(gridView, new Point(e.X, e.Y));
        }

        private static void AcceptFileDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void gridView_DragEnter(object sender, DragEventArgs e)
        {
            AcceptFileDrop(e);
        }

        // an image dropped on the list becomes the new cover; other files / folders are added to the list
        private void gridView_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            if (Manipulator.IsImageFile(files[0]))
            {
                definePicture(files[0]);
                return;
            }

            gridView.DataSource = null;
            foreach (string file in files)
            {
                if (Directory.Exists(file))
                    listMp3Infos.Add(new FolderInfo(file, Path.GetFileName(file)));
                else
                    listMp3Infos.Add(new Mp3Info(file, Path.GetFileName(file), File.GetCreationTime(file)));
            }
            listContent = ListContent.Other;
            ShowList(" files");
        }

        #endregion

        #region Search

        private void _btnSearch_Click(object sender, EventArgs e)
        {
            cbbFilePath.Focus();
            if (!IsSearchMode)
            {
                _btnList.Enabled = false;
                _btnSearch.Text = "Search ON";
                cbbFilePath.Text = ""; // starts the search: lists the whole folder
            }
            else
            {
                _btnList.Enabled = true;
                _btnSearch.Text = "Search OFF";
            }
        }

        private void cbbFilePath_TextChanged(object sender, EventArgs e)
        {
            if (IsSearchMode && !suppressFolderSearch)
            {
                RunSearch();
            }
        }

        // Search the text of cbbFilePath in the names of the folders and audio / video files below the search root
        // (all levels). An empty text lists every audio / video file and the subfolders.
        private void RunSearch()
        {
            string searchString = cbbFilePath.Text;
            ClearMp3List();
            listContent = ListContent.SearchResults;

            string root = !string.IsNullOrEmpty(searchRoot) ? searchRoot : currentFolder;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                _lblCount.Text = "Select a folder first";
                return;
            }

            try
            {
                if (searchString.Length == 0)
                {
                    addMatchingFiles(audioPatterns, root, "");
                    addMatchingFiles(videoPatterns, root, "");
                    addMatchingFolders(new DirectoryInfo(root), "");
                }
                else
                {
                    addMatchingFolders(new DirectoryInfo(root), searchString);
                    addMatchingFiles(audioPatterns, root, searchString);
                    addMatchingFiles(videoPatterns, root, searchString);
                }
            }
            catch (Exception ex)
            {
                _lblCount.Text = "Search error: " + ex.Message;
            }

            if (listMp3Infos.Count > 0)
            {
                ShowList(" results");
            }
        }

        // files below folder (all levels) whose name contains searchString (every file when it is empty)
        private void addMatchingFiles(string[] patterns, string folder, string searchString)
        {
            DirectoryInfo di = new DirectoryInfo(folder);
            foreach (string pattern in patterns)
            {
                foreach (FileInfo fi in getFilesSafe(di, pattern, SearchOption.AllDirectories))
                {
                    if (searchString.Length == 0 || fi.Name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        listMp3Infos.Add(new Mp3Info(fi.FullName, fi.Name, fi.CreationTime));
                    }
                }
            }
        }

        // With an empty searchString: the direct subfolders. Otherwise the folders at any level whose name contains
        // searchString (the subfolders of a match are not searched).
        private void addMatchingFolders(DirectoryInfo dir, string searchString)
        {
            foreach (DirectoryInfo sub in getDirectoriesSafe(dir))
            {
                if (searchString.Length == 0 || sub.Name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    listMp3Infos.Add(new FolderInfo(sub.FullName, sub.Name));
                }
                else
                {
                    addMatchingFolders(sub, searchString);
                }
            }
        }

        #endregion

        #region Folder tree

        // tree path -> folder path ("Desktop\x" -> the desktop folder; "C:\\Users" -> "C:\Users")
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
                fullPath = path;
            }

            try
            {
                return Path.GetFullPath(fullPath);
            }
            catch
            {
                return fullPath;
            }
        }

        // selecting a folder in the tree makes it the folder for "List Files" (and the search root)
        private void treeViewFolder_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string path = GetPhysicalPath(e.Node);
            if (!Directory.Exists(path)) return; // file node

            if (!IsSearchMode)
            {
                SetPathTextSilently(path);
            }
            SetSearchRoot(path);
        }

        private void treeViewFolder_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count > 0 && e.Node.Nodes[0].Text == "")
            {
                fe.EnumerateDirectory(e.Node);
            }
        }

        private void treeViewFolder_MouseUp(object sender, MouseEventArgs e)
        {
            // Show menu only if the right mouse button is clicked.
            if (e.Button != MouseButtons.Right) return;

            Point p = new Point(e.X, e.Y);
            TreeNode node = treeViewFolder.GetNodeAt(p);
            if (node != null)
            {
                treeViewFolder.SelectedNode = node;
                contextNode = node;
                contextMenuFolder.Show(treeViewFolder, p);
            }
        }

        // tree context menu: list this folder
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = GetPhysicalPath(contextNode);
            if (!Directory.Exists(path)) return;
            try
            {
                SetSearchRoot(path);
                ListFolder(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // tree context menu: open in Explorer
        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenInExplorer(GetPhysicalPath(contextNode));
        }

        #endregion

        #region File operations

        // a folder is opened, a file is shown selected in its folder
        private static void OpenInExplorer(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Process.Start(path);
                else if (File.Exists(path))
                    Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }
        }

        private static void OpenWithDefaultApp(string path)
        {
            try
            {
                Process.Start(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }
        }

        // grid context menu: open the file with its default application
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string path = CurrentRowPath();
            if (path != null) OpenWithDefaultApp(path);
        }

        // grid context menu: open folder
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            OpenInExplorer(CurrentRowPath());
        }

        #endregion

        #region Rename

        private string beforeRenamed = "";
        private string renamePath = "";

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gridView.ReadOnly = false;
        }

        private void gridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // only the "File Name" column can be edited, not the ".." row
            DataGridViewRow row = gridView.Rows[e.RowIndex];
            object pathValue = row.Cells["ColumnPath"].Value;
            object nameValue = row.Cells[e.ColumnIndex].Value;
            if (gridView.Columns[e.ColumnIndex].DataPropertyName != "Name" || pathValue == null || nameValue == null ||
                row.DataBoundItem is ParentFolderInfo)
            {
                e.Cancel = true;
                return;
            }
            beforeRenamed = nameValue.ToString();
            renamePath = pathValue.ToString();
        }

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

            if (MessageBox.Show("Modify file name?", "Info", MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                cell.Value = beforeRenamed;
                return;
            }

            string newPath = Path.Combine(Path.GetDirectoryName(renamePath), afterRenamed);
            try
            {
                if (Directory.Exists(renamePath))
                    Directory.Move(renamePath, newPath);
                else
                    File.Move(renamePath, newPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot rename: " + ex.Message, "Error");
                cell.Value = beforeRenamed;
                return;
            }

            gridView.Rows[e.RowIndex].Cells["ColumnPath"].Value = newPath; // prevent file not found exception after renamed
            playlist.Rename(renamePath, newPath);
            if (string.Equals(selectedFileName, renamePath, StringComparison.OrdinalIgnoreCase))
            {
                selectedFileName = newPath;
                tagsShownFor = newPath;
                listSelectedFiles.Remove(renamePath);
                if (Manipulator.IsAudioFile(newPath)) listSelectedFiles.Add(newPath);
            }
        }

        #endregion
    }
}
