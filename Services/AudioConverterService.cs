using System;
using System.Diagnostics;
using System.IO;

namespace Mp3TagReader.Services
{
    public class AudioConverterService
    {
        private Process ffmpegProcess;
        private double totalDurationSeconds = 0;

        public delegate void ProgressHandler(int percentage, string status);
        public delegate void CompletionHandler(int exitCode, string message);

        public void ConvertToMp3(string sourceFilePath, string outputFile, string bitrate, ProgressHandler onProgress, CompletionHandler onComplete)
        {
            try
            {
                // Get duration using TagLib
                try
                {
                    using (var file = TagLib.File.Create(sourceFilePath))
                    {
                        totalDurationSeconds = file.Properties.Duration.TotalSeconds;
                    }
                }
                catch
                {
                    totalDurationSeconds = 0;
                }

                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "ffmpeg.exe");
                if (!File.Exists(ffmpegPath))
                {
                    ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "Libs", "ffmpeg.exe");
                }

                if (!File.Exists(ffmpegPath))
                {
                    onComplete(-1, "ffmpeg.exe not found in Libs folder.");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = string.Format("-y -i \"{0}\" -codec:a libmp3lame -b:a {1}k \"{2}\"", sourceFilePath, bitrate, outputFile),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true // ffmpeg logs progress to stderr
                };

                ffmpegProcess = new Process { StartInfo = startInfo };
                ffmpegProcess.Start();

                using (StreamReader reader = ffmpegProcess.StandardError)
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ParseFfmpegProgress(line, onProgress);
                    }
                }

                ffmpegProcess.WaitForExit();
                int exitCode = ffmpegProcess.ExitCode;

                if (exitCode == 0)
                {
                    onComplete(0, "Conversion completed successfully!");
                }
                else
                {
                    onComplete(exitCode, "Conversion failed.");
                }
            }
            catch (Exception ex)
            {
                onComplete(-2, ex.Message);
            }
        }

        private void ParseFfmpegProgress(string line, ProgressHandler onProgress)
        {
            if (totalDurationSeconds <= 0) return;

            // Look for "time=00:00:17.51"
            int timeIdx = line.IndexOf("time=");
            if (timeIdx >= 0)
            {
                try
                {
                    string timeStr = line.Substring(timeIdx + 5).Split(' ')[0].Trim();
                    TimeSpan elapsed;
                    if (TimeSpan.TryParse(timeStr, out elapsed))
                    {
                        double elapsedSeconds = elapsed.TotalSeconds;
                        int pct = (int)((elapsedSeconds / totalDurationSeconds) * 100);
                        if (pct >= 0 && pct <= 100)
                        {
                            onProgress(pct, string.Format("Converting... {0}%", pct));
                        }
                    }
                }
                catch { }
            }
        }

        public void Abort()
        {
            try
            {
                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill();
                }
            }
            catch { }
        }
    }
}
