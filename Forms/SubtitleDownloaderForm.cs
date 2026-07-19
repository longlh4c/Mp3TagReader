using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Mp3TagReader.Services;
using Mp3TagReader.Models;

namespace Mp3TagReader.Forms
{
    public partial class SubtitleDownloaderForm : Form
    {
        private string targetFilePath = string.Empty;
        private string targetDirectory = string.Empty;
        private List<SubdlSearchResult> searchResults = new List<SubdlSearchResult>();
        private readonly SubtitleService _subtitleService = new SubtitleService();

        public SubtitleDownloaderForm()
        {
            InitializeComponent();
            EnsureTls12();
        }

        public SubtitleDownloaderForm(string filePath) : this()
        {
            targetFilePath = filePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                targetDirectory = Path.GetDirectoryName(filePath);
                txtSearchQuery.Text = Path.GetFileNameWithoutExtension(filePath);
            }
        }

        private void EnsureTls12()
        {
            try
            {
                // Force TLS 1.2 for modern web API compatibility (OpenSubtitles / SubDB APIs)
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            }
            catch { }
        }

        private void SubtitleDownloaderForm_Load(object sender, EventArgs e)
        {
            // Populate Language Combobox
            cbbLanguage.Items.Add("English");
            cbbLanguage.Items.Add("Vietnamese");
            cbbLanguage.SelectedIndex = 0; // Default to English

            ApplyLightTheme();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Video Files (*.mp4;*.mkv;*.avi;*.wmv;*.mov)|*.mp4;*.mkv;*.avi;*.wmv;*.mov|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    targetFilePath = ofd.FileName;
                    targetDirectory = Path.GetDirectoryName(targetFilePath);
                    txtSearchQuery.Text = CleanMovieFilename(Path.GetFileNameWithoutExtension(targetFilePath));
                }
            }
        }

        private string CleanMovieFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return string.Empty;

            // Replace dots, underscores, hyphens with spaces
            string cleaned = filename.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ');

            // Find a 4-digit year (1900-2099)
            var match = Regex.Match(cleaned, @"\b(19\d{2}|20\d{2})\b");
            if (match.Success)
            {
                int index = match.Index + match.Length;
                cleaned = cleaned.Substring(0, index).Trim();
            }
            else
            {
                // Strip common tags if year is not present
                string[] tags = new string[] { "1080p", "720p", "4k", "2160p", "bluray", "webrip", "hdrip", "dvdrip", "brrip", "x264", "x265", "h264", "hevc" };
                foreach (var tag in tags)
                {
                    int tagIdx = cleaned.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
                    if (tagIdx > 0)
                    {
                        cleaned = cleaned.Substring(0, tagIdx).Trim();
                    }
                }
            }

            // Replace multiple spaces with a single space
            cleaned = Regex.Replace(cleaned, @"\s+", " ");

            return cleaned.Trim();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string query = txtSearchQuery.Text.Trim();
            if (string.IsNullOrEmpty(query) && (string.IsNullOrEmpty(targetFilePath) || !File.Exists(targetFilePath)))
            {
                MessageBox.Show("Please enter a movie title or select a movie file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedLang = cbbLanguage.SelectedItem.ToString();
            lblStatus.Text = "Searching subtitles...";
            lstResults.Items.Clear();
            searchResults.Clear();
            btnDownload.Enabled = false;

            Thread thread = new Thread(() => PerformCombinedSearch(query, targetFilePath, selectedLang));
            thread.IsBackground = true;
            thread.Start();
        }

        private void PerformCombinedSearch(string keywordQuery, string videoPath, string selectedLang)
        {
            try
            {
                var combined = _subtitleService.SearchSubtitles(keywordQuery, videoPath, selectedLang);

                // Update UI
                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (combined.Count == 0)
                    {
                        lblStatus.Text = "No subtitles found for either search.";
                    }
                    else
                    {
                        searchResults.AddRange(combined);
                        foreach (var item in searchResults)
                        {
                            lstResults.Items.Add(item.Name);
                        }
                        lblStatus.Text = "Found " + searchResults.Count + " result(s) (Auto-Match items on top).";
                        if (lstResults.Items.Count > 0) lstResults.SelectedIndex = 0;
                    }
                });
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "Search failed: " + ex.Message;
                });
            }
        }

        private void lstResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnDownload.Enabled = lstResults.SelectedIndex >= 0;
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            int index = lstResults.SelectedIndex;
            if (index < 0 || index >= searchResults.Count) return;

            var item = searchResults[index];
            
            // Choose save location
            string defaultName = string.IsNullOrEmpty(targetFilePath) ? "subtitle.srt" : Path.ChangeExtension(targetFilePath, ".srt");
            
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Subrip Subtitles (*.srt)|*.srt";
                sfd.FileName = Path.GetFileName(defaultName);
                if (!string.IsNullOrEmpty(targetDirectory)) sfd.InitialDirectory = targetDirectory;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    lblStatus.Text = "Downloading subtitle file...";
                    Thread thread = new Thread(() => DownloadSubtitle(item, sfd.FileName));
                    thread.IsBackground = true;
                    thread.Start();
                }
            }
        }

        private void DownloadSubtitle(SubdlSearchResult result, string savePath)
        {
            try
            {
                bool isOriginalFallback = _subtitleService.DownloadSubtitle(result, savePath);

                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (isOriginalFallback)
                    {
                        lblStatus.Text = "Downloaded successfully (English source fallback).";
                        MessageBox.Show("Selected language translation was not generated yet on Subtitle Cat.\nOriginal English subtitle downloaded instead and saved to:\n" + savePath, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        lblStatus.Text = "Subtitle downloaded successfully!";
                        MessageBox.Show("Subtitle downloaded and saved to:\n" + savePath, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                });
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "Download failed: " + ex.Message;
                });
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ApplyLightTheme()
        {
            Color lightBg = SystemColors.Control;
            Color textPrimary = SystemColors.ControlText;
            Color textSecondary = SystemColors.GrayText;

            this.BackColor = lightBg;
            this.ForeColor = textPrimary;

            txtSearchQuery.BackColor = Color.White;
            txtSearchQuery.ForeColor = textPrimary;
            txtSearchQuery.BorderStyle = BorderStyle.Fixed3D;

            lstResults.BackColor = Color.White;
            lstResults.ForeColor = textPrimary;
            lstResults.BorderStyle = BorderStyle.Fixed3D;

            lblSearch.ForeColor = textPrimary;
            lblStatus.ForeColor = textPrimary;
            lblLanguage.ForeColor = textPrimary;

            cbbLanguage.BackColor = Color.White;
            cbbLanguage.ForeColor = textPrimary;
            cbbLanguage.FlatStyle = FlatStyle.Standard;

            Action<Button> styleBtn = (btn) =>
            {
                btn.UseVisualStyleBackColor = true;
            };

            styleBtn(btnBrowse);
            styleBtn(btnSearch);
            styleBtn(btnDownload);
            styleBtn(btnCancel);
        }
    }
}
