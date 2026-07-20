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
        private Thread conversionThread;
        private readonly Services.AudioConverterService _audioConverterService = new Services.AudioConverterService();

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
            _audioConverterService.ConvertToMp3(
                sourceFilePath,
                outputFile,
                bitrate,
                (pct, status) =>
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        progressBar.Value = pct;
                        lblStatus.Text = status;
                    });
                },
                (exitCode, message) =>
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (exitCode == 0)
                        {
                            progressBar.Value = 100;
                            lblStatus.Text = message;
                            btnCancel.Text = "Close";
                            MessageBox.Show("Conversion successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            lblStatus.Text = "Conversion failed: " + message;
                            ResetUI();
                        }
                    });
                }
            );
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
            _audioConverterService.Abort();
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
