using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Mp3TagReader.Services
{
    // Plays audio with the bundled Libs\ffplay.exe (the app does not depend on Windows Media Player).
    // ffplay cannot be paused without its window, so Pause stops the process and Resume restarts it
    // at the saved position. The position comes from the clock ffplay prints with -stats.
    public class FfplayTrackPlayer : ITrackPlayer
    {
        private readonly string ffplayPath;
        private readonly object sync = new object();

        private Process process;
        private string currentPath;
        private double startOffset;   // position the running process was started at
        private double clock = -1;    // last position reported by ffplay, -1 until the first report
        private string lastError;
        private double pausedAt;
        private bool paused;

        public event EventHandler<TrackEndedEventArgs> TrackEnded;

        public FfplayTrackPlayer(string ffplayPath)
        {
            this.ffplayPath = ffplayPath;
        }

        // Libs\ffplay.exe next to the application, or null when it is missing
        public static string FindFfplay()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "ffplay.exe");
            if (File.Exists(path)) return path;
            path = Path.Combine(Directory.GetCurrentDirectory(), "Libs", "ffplay.exe");
            if (File.Exists(path)) return path;
            return null;
        }

        public string Name
        {
            get { return "ffplay"; }
        }

        public double Position
        {
            get
            {
                lock (sync)
                {
                    if (paused) return pausedAt;
                    if (process == null) return 0;
                    return clock >= 0 ? clock : startOffset;
                }
            }
            set
            {
                if (currentPath == null) return;
                double target = Math.Max(0, value);
                if (paused)
                {
                    lock (sync) pausedAt = target;
                }
                else
                {
                    KillProcess();
                    StartProcess(target);
                }
            }
        }

        public void Play(string path, double startSeconds)
        {
            KillProcess();
            lock (sync)
            {
                currentPath = path;
                paused = false;
            }
            StartProcess(Math.Max(0, startSeconds));
        }

        public void Pause()
        {
            lock (sync)
            {
                if (paused || process == null) return;
                pausedAt = clock >= 0 ? clock : startOffset;
                paused = true;
            }
            KillProcess();
        }

        public void Resume()
        {
            double from;
            lock (sync)
            {
                if (!paused || currentPath == null) return;
                paused = false;
                from = pausedAt;
            }
            StartProcess(from);
        }

        public void Stop()
        {
            lock (sync)
            {
                paused = false;
                currentPath = null;
            }
            KillProcess();
        }

        private void StartProcess(double start)
        {
            ProcessStartInfo psi = new ProcessStartInfo(ffplayPath,
                string.Format(CultureInfo.InvariantCulture,
                    "-nodisp -autoexit -hide_banner -loglevel error -stats -ss {0:0.###} \"{1}\"", start, currentPath));
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;

            Process p = new Process();
            p.StartInfo = psi;
            p.EnableRaisingEvents = true;
            p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { OnStderr(p, e.Data); };
            p.Exited += delegate { OnExited(p); };

            lock (sync)
            {
                process = p;
                startOffset = start;
                clock = -1;
                lastError = null;
            }
            p.Start();
            p.BeginErrorReadLine();
        }

        // stats lines look like "   12.34 M-A:  0.000 fd=   0 aq=    0KB ..."; anything else is an error message
        private void OnStderr(Process p, string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            string trimmed = line.Trim();
            if (trimmed.Length == 0) return;

            lock (sync)
            {
                if (p != process) return;
                if (trimmed.Contains("M-A:") || trimmed.Contains("A-V:") || trimmed.Contains("M-V:"))
                {
                    int space = trimmed.IndexOf(' ');
                    double value;
                    if (space > 0 && double.TryParse(trimmed.Substring(0, space), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= 0)
                    {
                        clock = value;
                    }
                }
                else
                {
                    lastError = trimmed;
                }
            }
        }

        private void OnExited(Process p)
        {
            bool failed;
            string error;
            lock (sync)
            {
                // killed on purpose (pause / stop / seek / next song): not an end of track
                if (p != process) return;
                process = null;
                int exitCode = 0;
                try { exitCode = p.ExitCode; } catch { }
                // ffplay exits with 0 even for files it cannot open; a failure is an exit that never reported a position
                failed = clock < 0 && (exitCode != 0 || lastError != null);
                error = lastError;
            }

            try { p.Dispose(); } catch { }

            EventHandler<TrackEndedEventArgs> handler = TrackEnded;
            if (handler != null)
            {
                handler(this, new TrackEndedEventArgs(failed, error));
            }
        }

        private void KillProcess()
        {
            Process p;
            lock (sync)
            {
                p = process;
                process = null;
            }
            if (p == null) return;
            try
            {
                if (!p.HasExited)
                {
                    p.Kill();
                    p.WaitForExit(2000);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
