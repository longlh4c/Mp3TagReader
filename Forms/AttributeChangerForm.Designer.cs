using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mp3TagReader.Forms
{
    partial class AttributeChangerForm
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

        private void InitializeComponent()
        {
            this.chkReadOnly = new System.Windows.Forms.CheckBox();
            this.chkHidden = new System.Windows.Forms.CheckBox();
            this.chkArchive = new System.Windows.Forms.CheckBox();
            this.chkSystem = new System.Windows.Forms.CheckBox();
            this.chkCompress = new System.Windows.Forms.CheckBox();
            this.chkIndex = new System.Windows.Forms.CheckBox();
            this.chkModifyStamps = new System.Windows.Forms.CheckBox();
            
            this.tabTimestamps = new System.Windows.Forms.TabControl();
            this.tpSystem = new System.Windows.Forms.TabPage();
            
            this.lblCreated = new System.Windows.Forms.Label();
            this.dtpCreatedDate = new System.Windows.Forms.DateTimePicker();
            this.dtpCreatedTime = new System.Windows.Forms.DateTimePicker();
            
            this.lblModified = new System.Windows.Forms.Label();
            this.dtpModifiedDate = new System.Windows.Forms.DateTimePicker();
            this.dtpModifiedTime = new System.Windows.Forms.DateTimePicker();
            
            this.lblAccessed = new System.Windows.Forms.Label();
            this.dtpAccessedDate = new System.Windows.Forms.DateTimePicker();
            this.dtpAccessedTime = new System.Windows.Forms.DateTimePicker();
            
            this.lblFootnote = new System.Windows.Forms.Label();
            
            this.lblFilePath = new System.Windows.Forms.Label();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            
            this.tabTimestamps.SuspendLayout();
            this.tpSystem.SuspendLayout();
            this.SuspendLayout();
            
            // File Path Row
            this.lblFilePath.Location = new Point(12, 15);
            this.lblFilePath.Size = new Size(60, 20);
            this.lblFilePath.Text = "File Path:";
            
            this.txtFilePath.Location = new Point(80, 12);
            this.txtFilePath.Size = new Size(330, 20);
            this.txtFilePath.ReadOnly = true;
            
            this.btnBrowse.Location = new Point(420, 10);
            this.btnBrowse.Size = new Size(65, 23);
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.Click += new EventHandler(this.btnBrowse_Click);
            
            // Checkboxes top row
            this.chkReadOnly.Location = new Point(20, 50);
            this.chkReadOnly.Text = "Read-only";
            this.chkReadOnly.Size = new Size(130, 20);
            
            this.chkHidden.Location = new Point(180, 50);
            this.chkHidden.Text = "Hidden";
            this.chkHidden.Size = new Size(130, 20);
            
            this.chkArchive.Location = new Point(340, 50);
            this.chkArchive.Text = "Archive";
            this.chkArchive.Size = new Size(130, 20);
            
            // Checkboxes second row
            this.chkSystem.Location = new Point(20, 80);
            this.chkSystem.Text = "System";
            this.chkSystem.Size = new Size(130, 20);
            
            this.chkCompress.Location = new Point(180, 80);
            this.chkCompress.Text = "Compress";
            this.chkCompress.Size = new Size(130, 20);
            this.chkCompress.Enabled = false; // System attributes typically managed by OS
            
            this.chkIndex.Location = new Point(340, 80);
            this.chkIndex.Text = "Index";
            this.chkIndex.Size = new Size(130, 20);
            this.chkIndex.Enabled = false; // System attributes typically managed by OS
            
            // Divider line
            Label lblDivider = new Label();
            lblDivider.BorderStyle = BorderStyle.Fixed3D;
            lblDivider.Location = new Point(12, 115);
            lblDivider.Size = new Size(476, 2);
            this.Controls.Add(lblDivider);

            // Modify stamps checkbox
            this.chkModifyStamps.Location = new Point(15, 130);
            this.chkModifyStamps.Text = "Modify date and time stamps";
            this.chkModifyStamps.Size = new Size(250, 20);
            this.chkModifyStamps.CheckedChanged += (s, e) => {
                bool enabled = chkModifyStamps.Checked;
                dtpCreatedDate.Enabled = enabled;
                dtpCreatedTime.Enabled = enabled;
                dtpModifiedDate.Enabled = enabled;
                dtpModifiedTime.Enabled = enabled;
                dtpAccessedDate.Enabled = enabled;
                dtpAccessedTime.Enabled = enabled;
            };

            // TabControl
            this.tabTimestamps.Location = new Point(15, 160);
            this.tabTimestamps.Size = new Size(470, 160);
            this.tabTimestamps.Controls.Add(this.tpSystem);
            
            // TabPage (System)
            this.tpSystem.Text = "System";
            this.tpSystem.UseVisualStyleBackColor = true;
            this.tpSystem.Padding = new Padding(10);
            
            // Created Row
            this.lblCreated.Location = new Point(15, 20);
            this.lblCreated.Size = new Size(80, 20);
            this.lblCreated.Text = "Created";
            
            this.dtpCreatedDate.Format = DateTimePickerFormat.Short;
            this.dtpCreatedDate.Location = new Point(100, 17);
            this.dtpCreatedDate.Size = new Size(150, 20);
            
            this.dtpCreatedTime.Format = DateTimePickerFormat.Time;
            this.dtpCreatedTime.ShowUpDown = true;
            this.dtpCreatedTime.Location = new Point(260, 17);
            this.dtpCreatedTime.Size = new Size(150, 20);
            
            // Modified Row
            this.lblModified.Location = new Point(15, 50);
            this.lblModified.Size = new Size(80, 20);
            this.lblModified.Text = "Modified";
            
            this.dtpModifiedDate.Format = DateTimePickerFormat.Short;
            this.dtpModifiedDate.Location = new Point(100, 47);
            this.dtpModifiedDate.Size = new Size(150, 20);
            
            this.dtpModifiedTime.Format = DateTimePickerFormat.Time;
            this.dtpModifiedTime.ShowUpDown = true;
            this.dtpModifiedTime.Location = new Point(260, 47);
            this.dtpModifiedTime.Size = new Size(150, 20);
            
            // Accessed Row
            this.lblAccessed.Location = new Point(15, 80);
            this.lblAccessed.Size = new Size(80, 20);
            this.lblAccessed.Text = "Accessed";
            
            this.dtpAccessedDate.Format = DateTimePickerFormat.Short;
            this.dtpAccessedDate.Location = new Point(100, 77);
            this.dtpAccessedDate.Size = new Size(150, 20);
            
            this.dtpAccessedTime.Format = DateTimePickerFormat.Time;
            this.dtpAccessedTime.ShowUpDown = true;
            this.dtpAccessedTime.Location = new Point(260, 77);
            this.dtpAccessedTime.Size = new Size(150, 20);

            // Footnote description
            this.lblFootnote.Location = new Point(15, 115);
            this.lblFootnote.Size = new Size(440, 20);
            this.lblFootnote.ForeColor = Color.Gray;
            this.lblFootnote.Text = "Configure Creation, Last Write, and Last Access Timestamps.";

            // Add fields to TabPage tpSystem
            this.tpSystem.Controls.Add(this.lblCreated);
            this.tpSystem.Controls.Add(this.dtpCreatedDate);
            this.tpSystem.Controls.Add(this.dtpCreatedTime);
            
            this.tpSystem.Controls.Add(this.lblModified);
            this.tpSystem.Controls.Add(this.dtpModifiedDate);
            this.tpSystem.Controls.Add(this.dtpModifiedTime);
            
            this.tpSystem.Controls.Add(this.lblAccessed);
            this.tpSystem.Controls.Add(this.dtpAccessedDate);
            this.tpSystem.Controls.Add(this.dtpAccessedTime);
            
            this.tpSystem.Controls.Add(this.lblFootnote);

            // Add Apply & Cancel Buttons
            this.btnApply.Location = new Point(310, 335);
            this.btnApply.Size = new Size(80, 30);
            this.btnApply.Text = "Apply";
            this.btnApply.Click += new EventHandler(this.btnApply_Click);

            this.btnCancel.Location = new Point(400, 335);
            this.btnCancel.Size = new Size(80, 30);
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);

            // Form Layout settings
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(500, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AttributeChangerForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Attribute Changer";
            this.Load += new EventHandler(this.AttributeChangerForm_Load);
            
            this.Controls.Add(this.lblFilePath);
            this.Controls.Add(this.txtFilePath);
            this.Controls.Add(this.btnBrowse);
            
            this.Controls.Add(this.chkReadOnly);
            this.Controls.Add(this.chkHidden);
            this.Controls.Add(this.chkArchive);
            this.Controls.Add(this.chkSystem);
            this.Controls.Add(this.chkCompress);
            this.Controls.Add(this.chkIndex);
            
            this.Controls.Add(this.chkModifyStamps);
            this.Controls.Add(this.tabTimestamps);
            
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnCancel);
            
            this.tabTimestamps.ResumeLayout(false);
            this.tpSystem.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.CheckBox chkReadOnly;
        private System.Windows.Forms.CheckBox chkHidden;
        private System.Windows.Forms.CheckBox chkArchive;
        private System.Windows.Forms.CheckBox chkSystem;
        private System.Windows.Forms.CheckBox chkCompress;
        private System.Windows.Forms.CheckBox chkIndex;
        private System.Windows.Forms.CheckBox chkModifyStamps;
        
        private System.Windows.Forms.TabControl tabTimestamps;
        private System.Windows.Forms.TabPage tpSystem;
        
        private System.Windows.Forms.Label lblCreated;
        private System.Windows.Forms.DateTimePicker dtpCreatedDate;
        private System.Windows.Forms.DateTimePicker dtpCreatedTime;
        
        private System.Windows.Forms.Label lblModified;
        private System.Windows.Forms.DateTimePicker dtpModifiedDate;
        private System.Windows.Forms.DateTimePicker dtpModifiedTime;
        
        private System.Windows.Forms.Label lblAccessed;
        private System.Windows.Forms.DateTimePicker dtpAccessedDate;
        private System.Windows.Forms.DateTimePicker dtpAccessedTime;
        
        private System.Windows.Forms.Label lblFootnote;
        
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Button btnBrowse;
        
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
    }
}
