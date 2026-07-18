namespace Mp3TagReader.Forms
{
    partial class YoutubeDownloadForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblUrl = new System.Windows.Forms.Label();
            this.txtUrl = new System.Windows.Forms.TextBox();
            this.lblBitrate = new System.Windows.Forms.Label();
            this.cbbBitrate = new System.Windows.Forms.ComboBox();
            this.lblOutput = new System.Windows.Forms.Label();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnDownload = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkEmbedArtwork = new System.Windows.Forms.CheckBox();
            this.chkEmbedMetadata = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lblUrl
            // 
            this.lblUrl.AutoSize = true;
            this.lblUrl.Location = new System.Drawing.Point(20, 22);
            this.lblUrl.Name = "lblUrl";
            this.lblUrl.Size = new System.Drawing.Size(75, 13);
            this.lblUrl.TabIndex = 0;
            this.lblUrl.Text = "YouTube URL:";
            // 
            // txtUrl
            // 
            this.txtUrl.Location = new System.Drawing.Point(110, 19);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(350, 20);
            this.txtUrl.TabIndex = 1;
            // 
            // lblBitrate
            // 
            this.lblBitrate.AutoSize = true;
            this.lblBitrate.Location = new System.Drawing.Point(20, 62);
            this.lblBitrate.Name = "lblBitrate";
            this.lblBitrate.Size = new System.Drawing.Size(73, 13);
            this.lblBitrate.TabIndex = 2;
            this.lblBitrate.Text = "Audio Quality:";
            // 
            // cbbBitrate
            // 
            this.cbbBitrate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbBitrate.FormattingEnabled = true;
            this.cbbBitrate.Location = new System.Drawing.Point(110, 59);
            this.cbbBitrate.Name = "cbbBitrate";
            this.cbbBitrate.Size = new System.Drawing.Size(150, 21);
            this.cbbBitrate.TabIndex = 3;
            // 
            // lblOutput
            // 
            this.lblOutput.AutoSize = true;
            this.lblOutput.Location = new System.Drawing.Point(20, 102);
            this.lblOutput.Name = "lblOutput";
            this.lblOutput.Size = new System.Drawing.Size(74, 13);
            this.lblOutput.TabIndex = 4;
            this.lblOutput.Text = "Output Folder:";
            // 
            // txtOutput
            // 
            this.txtOutput.Location = new System.Drawing.Point(110, 99);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.Size = new System.Drawing.Size(269, 20);
            this.txtOutput.TabIndex = 5;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(385, 97);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 6;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnDownload
            // 
            this.btnDownload.Location = new System.Drawing.Point(294, 250);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(80, 30);
            this.btnDownload.TabIndex = 7;
            this.btnDownload.Text = "Download";
            this.btnDownload.UseVisualStyleBackColor = true;
            this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(23, 185);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(437, 23);
            this.progressBar.TabIndex = 8;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(20, 217);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(38, 13);
            this.lblStatus.TabIndex = 9;
            this.lblStatus.Text = "Ready";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(380, 250);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Close";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // chkEmbedArtwork
            // 
            this.chkEmbedArtwork.AutoSize = true;
            this.chkEmbedArtwork.Checked = true;
            this.chkEmbedArtwork.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEmbedArtwork.Location = new System.Drawing.Point(110, 132);
            this.chkEmbedArtwork.Name = "chkEmbedArtwork";
            this.chkEmbedArtwork.Size = new System.Drawing.Size(235, 17);
            this.chkEmbedArtwork.TabIndex = 11;
            this.chkEmbedArtwork.Text = "Embed video thumbnail as MP3 artwork";
            this.chkEmbedArtwork.UseVisualStyleBackColor = true;
            // 
            // chkEmbedMetadata
            // 
            this.chkEmbedMetadata.AutoSize = true;
            this.chkEmbedMetadata.Checked = true;
            this.chkEmbedMetadata.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEmbedMetadata.Location = new System.Drawing.Point(110, 155);
            this.chkEmbedMetadata.Name = "chkEmbedMetadata";
            this.chkEmbedMetadata.Size = new System.Drawing.Size(250, 17);
            this.chkEmbedMetadata.TabIndex = 12;
            this.chkEmbedMetadata.Text = "Embed video metadata (Title, Artist, Year, etc.)";
            this.chkEmbedMetadata.UseVisualStyleBackColor = true;
            // 
            // YoutubeDownloadForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 300);
            this.Controls.Add(this.chkEmbedMetadata);
            this.Controls.Add(this.chkEmbedArtwork);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.lblOutput);
            this.Controls.Add(this.cbbBitrate);
            this.Controls.Add(this.lblBitrate);
            this.Controls.Add(this.txtUrl);
            this.Controls.Add(this.lblUrl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "YoutubeDownloadForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Download MP3 from YouTube";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.YoutubeDownloadForm_FormClosing);
            this.Load += new System.EventHandler(this.YoutubeDownloadForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblUrl;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.Label lblBitrate;
        private System.Windows.Forms.ComboBox cbbBitrate;
        private System.Windows.Forms.Label lblOutput;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkEmbedArtwork;
        private System.Windows.Forms.CheckBox chkEmbedMetadata;
    }
}
