using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Mp3TagReader.Forms
{
    public partial class YoutubeDownloadForm : Form
    {
        private string outputFolder;
        private Thread downloadThread;
        private bool hasDownloadedSuccessfully = false;
        private readonly Services.YoutubeDownloadService _youtubeDownloadService = new Services.YoutubeDownloadService();

        public string OutputFolder
        {
            get { return outputFolder; }
        }

        public YoutubeDownloadForm(string initialFolder)
        {
            InitializeComponent();
            outputFolder = initialFolder;
        }

        private void YoutubeDownloadForm_Load(object sender, EventArgs e)
        {
            txtOutput.Text = outputFolder;

            // Populate Bitrates
            cbbBitrate.Items.Add("128 kbps");
            cbbBitrate.Items.Add("192 kbps");
            cbbBitrate.Items.Add("256 kbps");
            cbbBitrate.Items.Add("320 kbps");
            cbbBitrate.SelectedIndex = 0; // Default to 128 kbps
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.SelectedPath = txtOutput.Text;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutput.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            string url = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a YouTube URL.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            outputFolder = txtOutput.Text.Trim();
            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
            {
                MessageBox.Show("Please specify a valid output folder.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Disable controls
            btnDownload.Enabled = false;
            btnBrowse.Enabled = false;
            cbbBitrate.Enabled = false;
            chkEmbedArtwork.Enabled = false;
            chkEmbedMetadata.Enabled = false;
            txtUrl.ReadOnly = true;
            txtOutput.ReadOnly = true;
            btnCancel.Text = "Cancel";

            lblStatus.Text = "Checking dependencies...";
            progressBar.Value = 0;

            // Check if yt-dlp.exe is available
            string ytDlpPath = _youtubeDownloadService.GetYtDlpPath();

            if (!System.IO.File.Exists(ytDlpPath))
            {
                string libsDir = Path.GetDirectoryName(ytDlpPath);
                if (!Directory.Exists(libsDir))
                {
                    Directory.CreateDirectory(libsDir);
                }

                if (MessageBox.Show("yt-dlp.exe is required but not found. Would you like to download it automatically (~20MB)?", 
                    "Download Dependency", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DownloadYtDlp(ytDlpPath);
                }
                else
                {
                    ResetUI();
                    lblStatus.Text = "Cancelled. yt-dlp.exe is required.";
                }
            }
            else
            {
                StartDownloadProcess();
            }
        }

        private void DownloadYtDlp(string targetPath)
        {
            _youtubeDownloadService.DownloadDependency(
                targetPath,
                (s, ev) =>
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        progressBar.Value = ev.ProgressPercentage;
                        lblStatus.Text = string.Format("Downloading yt-dlp.exe: {0}% ({1} KB / {2} KB)",
                            ev.ProgressPercentage, ev.BytesReceived / 1024, ev.TotalBytesToReceive / 1024);
                    });
                },
                (s, ev) =>
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (ev.Error != null)
                        {
                            MessageBox.Show("Failed to download yt-dlp.exe:\n" + ev.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            ResetUI();
                        }
                        else if (ev.Cancelled)
                        {
                            lblStatus.Text = "Download cancelled.";
                            ResetUI();
                        }
                        else
                        {
                            lblStatus.Text = "yt-dlp.exe downloaded successfully! Starting YouTube download...";
                            progressBar.Value = 0;
                            StartDownloadProcess();
                        }
                    });
                }
            );
        }

        private void StartDownloadProcess()
        {
            hasDownloadedSuccessfully = false;
            downloadThread = new Thread(() => RunYoutubeDownload());
            downloadThread.IsBackground = true;
            downloadThread.Start();
        }

        private void RunYoutubeDownload()
        {
            try
            {
                this.BeginInvoke((MethodInvoker)delegate { lblStatus.Text = "Fetching video details..."; });

                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "ffmpeg.exe");
                if (!System.IO.File.Exists(ffmpegPath))
                {
                    ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "Libs", "ffmpeg.exe");
                }

                if (!System.IO.File.Exists(ffmpegPath))
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("ffmpeg.exe not found in Libs folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ResetUI();
                    });
                    return;
                }

                string ytDlpPath = _youtubeDownloadService.GetYtDlpPath();
                string bitrate = "192";
                this.Invoke((MethodInvoker)delegate
                {
                    if (cbbBitrate.SelectedItem != null)
                    {
                        bitrate = cbbBitrate.SelectedItem.ToString().Replace(" kbps", "").Trim();
                    }
                });

                string url = "";
                bool embedArtwork = true;
                bool embedMetadata = true;
                this.Invoke((MethodInvoker)delegate
                {
                    url = txtUrl.Text.Trim();
                    embedArtwork = chkEmbedArtwork.Checked;
                    embedMetadata = chkEmbedMetadata.Checked;
                });

                string outputTemplate = Path.Combine(outputFolder, "%(title)s.%(ext)s");
                string ffmpegDir = Path.GetDirectoryName(ffmpegPath);

                // Check if target file already exists
                string targetFile = _youtubeDownloadService.GetTargetFilename(ytDlpPath, ffmpegDir, outputTemplate, url);
                if (!string.IsNullOrEmpty(targetFile))
                {
                    string fileName = Path.GetFileName(targetFile);
                    string finalMp3Path = Path.Combine(outputFolder, Path.ChangeExtension(fileName, ".mp3"));

                    bool overwrite = true;
                    if (System.IO.File.Exists(finalMp3Path))
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            var result = MessageBox.Show(this,
                                "The file already exists:\n" + Path.GetFileName(finalMp3Path) + "\n\nDo you want to overwrite it?",
                                "Confirm Overwrite",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.No)
                            {
                                overwrite = false;
                            }
                        });
                    }

                    if (!overwrite)
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            lblStatus.Text = "Download cancelled (file exists).";
                            ResetUI();
                        });
                        return;
                    }
                }

                _youtubeDownloadService.DownloadAudio(
                    url,
                    outputFolder,
                    bitrate,
                    embedArtwork,
                    embedMetadata,
                    (pct, status) =>
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (pct >= 0) progressBar.Value = pct;
                            lblStatus.Text = status;
                        });
                    },
                    (success, message) =>
                    {
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (success)
                            {
                                lblStatus.Text = message;
                                hasDownloadedSuccessfully = true;
                                MessageBox.Show("Download successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                ResetUI();
                                txtUrl.Text = string.Empty;
                                progressBar.Value = 0;
                            }
                            else
                            {
                                lblStatus.Text = message;
                                ResetUI();
                            }
                        });
                    }
                );
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "Error: " + ex.Message;
                    ResetUI();
                });
            }
        }

        private void ResetUI()
        {
            btnDownload.Enabled = true;
            btnBrowse.Enabled = true;
            cbbBitrate.Enabled = true;
            chkEmbedArtwork.Enabled = true;
            chkEmbedMetadata.Enabled = true;
            txtUrl.ReadOnly = false;
            txtOutput.ReadOnly = false;
            btnCancel.Text = "Close";
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (hasDownloadedSuccessfully)
            {
                this.DialogResult = DialogResult.OK;
            }
            this.Close();
        }

        private void AbortOperation()
        {
            _youtubeDownloadService.Abort();
        }

        private void YoutubeDownloadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool isActive = (btnDownload.Enabled == false && !hasDownloadedSuccessfully);
            if (isActive)
            {
                if (MessageBox.Show("Abort download?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    AbortOperation();
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                if (hasDownloadedSuccessfully)
                {
                    this.DialogResult = DialogResult.OK;
                }
            }
        }
    }
}
