using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace Mp3TagReader.Services
{
    public class YoutubeDownloadService
    {
        private Process ytDlpProcess;
        private WebClient webClient;
        private bool isDownloadingYtDlp = false;

        public delegate void ProgressHandler(int percentage, string status);
        public delegate void CompletionHandler(bool success, string message);

        public bool IsDownloadingDependency
        {
            get { return isDownloadingYtDlp; }
        }

        public string GetYtDlpPath()
        {
            string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "yt-dlp.exe");
            if (!File.Exists(ytDlpPath))
            {
                ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "Libs", "yt-dlp.exe");
            }
            return ytDlpPath;
        }

        public void DownloadDependency(string targetPath, DownloadProgressChangedEventHandler onProgress, AsyncCompletedEventHandler onComplete)
        {
            isDownloadingYtDlp = true;
            webClient = new WebClient();
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // Ensure TLS 1.2
            webClient.DownloadProgressChanged += onProgress;
            webClient.DownloadFileCompleted += (sender, e) =>
            {
                isDownloadingYtDlp = false;
                onComplete(sender, e);
            };
            webClient.DownloadFileAsync(new Uri("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"), targetPath);
        }

        public string GetTargetFilename(string ytDlpPath, string ffmpegDir, string outputTemplate, string url)
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
                    StandardOutputEncoding = Encoding.UTF8
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

        public void DownloadAudio(string url, string outputFolder, string bitrate, bool embedArtwork, bool embedMetadata, ProgressHandler onProgress, CompletionHandler onComplete)
        {
            try
            {
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "ffmpeg.exe");
                if (!File.Exists(ffmpegPath))
                {
                    ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "Libs", "ffmpeg.exe");
                }

                if (!File.Exists(ffmpegPath))
                {
                    onComplete(false, "ffmpeg.exe not found in Libs folder.");
                    return;
                }

                string ytDlpPath = GetYtDlpPath();
                string outputTemplate = Path.Combine(outputFolder, "%(title)s.%(ext)s");
                string ffmpegDir = Path.GetDirectoryName(ffmpegPath);

                string embedArg = "";
                if (embedArtwork) embedArg += " --embed-thumbnail";
                if (embedMetadata) embedArg += " --embed-metadata";

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

                using (StreamReader reader = ytDlpProcess.StandardOutput)
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ParseYtDlpProgress(line, onProgress);
                    }
                }

                ytDlpProcess.WaitForExit();
                int exitCode = ytDlpProcess.ExitCode;

                if (exitCode == 0)
                {
                    onComplete(true, "Download and conversion completed successfully!");
                }
                else
                {
                    onComplete(false, "Download/Conversion failed.");
                }
            }
            catch (Exception ex)
            {
                onComplete(false, ex.Message);
            }
        }

        private void ParseYtDlpProgress(string line, ProgressHandler onProgress)
        {
            if (line.Contains("[download]") && line.Contains("%"))
            {
                try
                {
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
                                    int speedIdx = line.IndexOf("at ");
                                    string speedDetails = string.Empty;
                                    if (speedIdx > 0)
                                    {
                                        speedDetails = " (" + line.Substring(speedIdx + 3).Trim() + ")";
                                    }
                                    onProgress(val, "Downloading... " + val + "%" + speedDetails);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            else if (line.Contains("[ExtractAudio]") || line.Contains("[Merger]") || line.Contains("Converting"))
            {
                onProgress(-1, "Extracting audio and converting to MP3...");
            }
            else if (line.Contains("[EmbedThumbnail]") || line.Contains("Adding thumbnail"))
            {
                onProgress(-1, "Embedding video thumbnail as MP3 artwork...");
            }
            else if (line.Contains("[Metadata]") || line.Contains("Adding metadata"))
            {
                onProgress(-1, "Embedding video metadata tags (Title, Artist, Year, etc.)...");
            }
        }

        public void Abort()
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
    }
}
