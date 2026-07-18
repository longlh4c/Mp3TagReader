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
        private Process ytDlpProcess;
        private Thread downloadThread;
        private WebClient webClient;
        private bool isDownloadingYtDlp = false;
        private bool hasDownloadedSuccessfully = false;

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
            string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "yt-dlp.exe");
            if (!System.IO.File.Exists(ytDlpPath))
            {
                ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "Libs", "yt-dlp.exe");
            }

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
                    isDownloadingYtDlp = true;
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
            try
            {
                webClient = new WebClient();
                // Enable TLS 1.2
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                webClient.DownloadProgressChanged += (s, ev) =>
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        progressBar.Value = ev.ProgressPercentage;
                        lblStatus.Text = string.Format("Downloading yt-dlp.exe: {0}% ({1} KB / {2} KB)",
                            ev.ProgressPercentage, ev.BytesReceived / 1024, ev.TotalBytesToReceive / 1024);
                    });
                };

                webClient.DownloadFileCompleted += (s, ev) =>
                {
                    isDownloadingYtDlp = false;
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
                };

                webClient.DownloadFileAsync(new Uri("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"), targetPath);
            }
            catch (Exception ex)
            {
                isDownloadingYtDlp = false;
                MessageBox.Show("Error starting download of yt-dlp.exe:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUI();
            }
        }

        private void StartDownloadProcess()
        {
            hasDownloadedSuccessfully = false;
            downloadThread = new Thread(() => RunYoutubeDownload());
            downloadThread.IsBackground = true;
            downloadThread.Start();
        }

        private string GetTargetFilename(string ytDlpPath, string ffmpegDir, string outputTemplate, string url)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = string.Format("--encoding utf-8 --no-warnings --ffmpeg-location \"{0}\" -x --audio-format mp3 -o \"{1}\" --get-filename --no-playlist \"{2}\"",
                        ffmpegDir, outputTemplate, url),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in lines)
                        {
                            string trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed)) continue;
                            if (trimmed.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase)) continue;
                            if (trimmed.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase)) continue;
                            if (trimmed.Contains("[youtube]") || trimmed.Contains("[download]")) continue;
                            return trimmed;
                        }
                    }
                }
            }
            catch { }
            return null;
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

                string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "yt-dlp.exe");
                if (!System.IO.File.Exists(ytDlpPath))
                {
                    ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "Libs", "yt-dlp.exe");
                }

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
                string targetFile = GetTargetFilename(ytDlpPath, ffmpegDir, outputTemplate, url);
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

                string embedArg = "";
                if (embedArtwork) embedArg += " --embed-thumbnail";
                if (embedMetadata) embedArg += " --embed-metadata";

                // yt-dlp options:
                // -x: extract audio
                // --audio-format mp3: convert to MP3
                // --audio-quality bitrate: select quality
                // --ffmpeg-location ffmpegDir: directory containing ffmpeg and ffprobe binaries
                // -o outputTemplate: target file path pattern
                // --no-playlist: download only the video
                // --embed-thumbnail: embed video thumbnail as artwork
                // --embed-metadata: embed video metadata as ID3 tags
                string arguments = string.Format("--encoding utf-8 --no-warnings --ffmpeg-location \"{0}\" -x --audio-format mp3 --audio-quality {1}{2} -o \"{3}\" --no-playlist \"{4}\"",
                    ffmpegDir, bitrate, embedArg, outputTemplate, url);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";

                ytDlpProcess = new Process { StartInfo = startInfo };
                ytDlpProcess.Start();

                // Read output to parse progress
                using (StreamReader reader = ytDlpProcess.StandardOutput)
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ParseYtDlpProgress(line);
                    }
                }

                ytDlpProcess.WaitForExit();
                int exitCode = ytDlpProcess.ExitCode;

                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (exitCode == 0)
                    {
                        lblStatus.Text = "Download and conversion completed successfully!";
                        hasDownloadedSuccessfully = true;
                        MessageBox.Show("Download successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ResetUI();
                        txtUrl.Text = string.Empty;
                        progressBar.Value = 0;
                    }
                    else
                    {
                        lblStatus.Text = "Download/Conversion failed.";
                        ResetUI();
                    }
                });
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

        private void ParseYtDlpProgress(string line)
        {
            // yt-dlp output example:
            // [download]  12.3% of ~10.42MiB at  1.23MiB/s ETA 00:08
            // [ExtractAudio] Destination: ...
            if (line.Contains("[download]") && line.Contains("%"))
            {
                try
                {
                    // Find the percentage substring
                    int pctIdx = line.IndexOf('%');
                    if (pctIdx > 0)
                    {
                        int startIdx = line.LastIndexOf(' ', pctIdx);
                        if (startIdx >= 0)
                        {
                            string pctStr = line.Substring(startIdx + 1, pctIdx - startIdx - 1).Trim();
                            double pct;
                            if (double.TryParse(pctStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out pct))
                            {
                                int val = (int)pct;
                                if (val >= 0 && val <= 100)
                                {
                                    this.BeginInvoke((MethodInvoker)delegate
                                    {
                                        progressBar.Value = val;
                                        // Try to find speed and ETA details to show in status
                                        int speedIdx = line.IndexOf("at ");
                                        string statusText = "Downloading... " + val + "%";
                                        if (speedIdx > 0)
                                        {
                                            statusText += " (" + line.Substring(speedIdx + 3).Trim() + ")";
                                        }
                                        lblStatus.Text = statusText;
                                    });
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            else if (line.Contains("[ExtractAudio]") || line.Contains("[Merger]") || line.Contains("Converting"))
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "Extracting audio and converting to MP3...";
                });
            }
            else if (line.Contains("[EmbedThumbnail]") || line.Contains("Adding thumbnail"))
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "Embedding video thumbnail as MP3 artwork...";
                });
            }
            else if (line.Contains("[Metadata]") || line.Contains("Adding metadata"))
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "Embedding video metadata tags (Title, Artist, Year, etc.)...";
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
            try
            {
                if (isDownloadingYtDlp && webClient != null && webClient.IsBusy)
                {
                    webClient.CancelAsync();
                }

                if (ytDlpProcess != null && !ytDlpProcess.HasExited)
                {
                    ytDlpProcess.Kill();
                }
            }
            catch { }
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
