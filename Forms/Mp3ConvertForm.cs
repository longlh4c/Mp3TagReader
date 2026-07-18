using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TagLib;

namespace Mp3TagReader.Forms
{
    public partial class Mp3ConvertForm : Form
    {
        private string sourceFilePath;
        private double totalDurationSeconds = 0;
        private Process ffmpegProcess;
        private Thread conversionThread;

        public Mp3ConvertForm(string filePath)
        {
            InitializeComponent();
            sourceFilePath = filePath;
        }

        private void Mp3ConvertForm_Load(object sender, EventArgs e)
        {
            txtSource.Text = sourceFilePath;
            
            // Default output path: same folder, change extension to .mp3
            string defaultOutput = Path.ChangeExtension(sourceFilePath, ".mp3");
            
            // If the source is already .mp3, add suffix to avoid overwriting directly
            if (string.Compare(Path.GetExtension(sourceFilePath), ".mp3", StringComparison.OrdinalIgnoreCase) == 0)
            {
                string dir = Path.GetDirectoryName(sourceFilePath);
                string name = Path.GetFileNameWithoutExtension(sourceFilePath) + "_converted.mp3";
                defaultOutput = Path.Combine(dir, name);
            }
            
            txtOutput.Text = defaultOutput;

            // Populate Bitrates
            cbbBitrate.Items.Add("128 kbps");
            cbbBitrate.Items.Add("192 kbps");
            cbbBitrate.Items.Add("256 kbps");
            cbbBitrate.Items.Add("320 kbps");
            cbbBitrate.SelectedIndex = 0; // Default to 128 kbps

            // Get total duration using TagLib
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
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "MP3 Audio (*.mp3)|*.mp3";
                saveFileDialog.FileName = Path.GetFileName(txtOutput.Text);
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(txtOutput.Text);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutput.Text = saveFileDialog.FileName;
                }
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            string output = txtOutput.Text.Trim();
            if (string.IsNullOrEmpty(output))
            {
                MessageBox.Show("Please specify output path.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.Compare(sourceFilePath, output, StringComparison.OrdinalIgnoreCase) == 0)
            {
                MessageBox.Show("Source file and output file cannot be the same.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get selected bitrate value
            string selectedBitrate = "192";
            if (cbbBitrate.SelectedItem != null)
            {
                string text = cbbBitrate.SelectedItem.ToString();
                selectedBitrate = text.Replace(" kbps", "").Trim();
            }

            // Disable controls
            btnConvert.Enabled = false;
            btnBrowse.Enabled = false;
            cbbBitrate.Enabled = false;
            txtOutput.ReadOnly = true;
            btnCancel.Text = "Cancel";

            lblStatus.Text = "Starting conversion...";
            progressBar.Value = 0;

            // Start conversion in background thread
            conversionThread = new Thread(() => RunConversion(output, selectedBitrate));
            conversionThread.IsBackground = true;
            conversionThread.Start();
        }

        private void RunConversion(string outputFile, string bitrate)
        {
            try
            {
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", "ffmpeg.exe");
                if (!System.IO.File.Exists(ffmpegPath))
                {
                    // Check direct root Libs folder if base directory is different (e.g. in development)
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
                        ParseFfmpegProgress(line);
                    }
                }

                ffmpegProcess.WaitForExit();
                int exitCode = ffmpegProcess.ExitCode;

                this.BeginInvoke((MethodInvoker)delegate
                {
                    if (exitCode == 0)
                    {
                        progressBar.Value = 100;
                        lblStatus.Text = "Conversion completed successfully!";
                        btnCancel.Text = "Close";
                        MessageBox.Show("Conversion successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblStatus.Text = "Conversion failed.";
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

        private void ParseFfmpegProgress(string line)
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
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                progressBar.Value = pct;
                                lblStatus.Text = string.Format("Converting... {0}%", pct);
                            });
                        }
                    }
                }
                catch { }
            }
        }

        private void ResetUI()
        {
            btnConvert.Enabled = true;
            btnBrowse.Enabled = true;
            cbbBitrate.Enabled = true;
            txtOutput.ReadOnly = false;
            btnCancel.Text = "Close";
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (btnConvert.Enabled == false) // Conversion is active
            {
                if (MessageBox.Show("Abort conversion?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    AbortConversion();
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        private void AbortConversion()
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

        private void Mp3ConvertForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btnConvert.Enabled == false)
            {
                if (MessageBox.Show("Abort conversion?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    AbortConversion();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
