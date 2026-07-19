using System;
using System.IO;
using System.Windows.Forms;

namespace Mp3TagReader.Forms
{
    public partial class AttributeChangerForm : Form
    {
        private string targetFilePath = string.Empty;

        public AttributeChangerForm()
        {
            InitializeComponent();
        }

        public AttributeChangerForm(string filePath) : this()
        {
            targetFilePath = filePath;
        }

        private void AttributeChangerForm_Load(object sender, EventArgs e)
        {
            txtFilePath.Text = targetFilePath;
            LoadAttributes();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    targetFilePath = ofd.FileName;
                    txtFilePath.Text = targetFilePath;
                    LoadAttributes();
                }
            }
        }

        private void LoadAttributes()
        {
            if (string.IsNullOrEmpty(targetFilePath) || !File.Exists(targetFilePath))
            {
                ResetFields();
                return;
            }

            try
            {
                FileInfo fi = new FileInfo(targetFilePath);
                
                // Read-only, Hidden, System, Archive
                chkReadOnly.Checked = (fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                chkHidden.Checked = (fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                chkSystem.Checked = (fi.Attributes & FileAttributes.System) == FileAttributes.System;
                chkArchive.Checked = (fi.Attributes & FileAttributes.Archive) == FileAttributes.Archive;

                // Compress, Index (Read-only representation or system queries)
                chkCompress.Checked = (fi.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed;
                chkIndex.Checked = (fi.Attributes & FileAttributes.NotContentIndexed) != FileAttributes.NotContentIndexed;

                // Load Timestamps
                dtpCreatedDate.Value = fi.CreationTime;
                dtpCreatedTime.Value = fi.CreationTime;

                dtpModifiedDate.Value = fi.LastWriteTime;
                dtpModifiedTime.Value = fi.LastWriteTime;

                dtpAccessedDate.Value = fi.LastAccessTime;
                dtpAccessedTime.Value = fi.LastAccessTime;

                chkModifyStamps.Checked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading attributes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetFields()
        {
            chkReadOnly.Checked = false;
            chkHidden.Checked = false;
            chkSystem.Checked = false;
            chkArchive.Checked = false;
            chkCompress.Checked = false;
            chkIndex.Checked = false;
            chkModifyStamps.Checked = false;

            dtpCreatedDate.Value = DateTime.Now;
            dtpCreatedTime.Value = DateTime.Now;
            dtpModifiedDate.Value = DateTime.Now;
            dtpModifiedTime.Value = DateTime.Now;
            dtpAccessedDate.Value = DateTime.Now;
            dtpAccessedTime.Value = DateTime.Now;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(targetFilePath) || !File.Exists(targetFilePath))
            {
                MessageBox.Show("Please select a valid file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                FileInfo fi = new FileInfo(targetFilePath);

                // Update FileAttributes
                FileAttributes attrs = fi.Attributes;

                // Read-only
                if (chkReadOnly.Checked) attrs |= FileAttributes.ReadOnly;
                else attrs &= ~FileAttributes.ReadOnly;

                // Hidden
                if (chkHidden.Checked) attrs |= FileAttributes.Hidden;
                else attrs &= ~FileAttributes.Hidden;

                // System
                if (chkSystem.Checked) attrs |= FileAttributes.System;
                else attrs &= ~FileAttributes.System;

                // Archive
                if (chkArchive.Checked) attrs |= FileAttributes.Archive;
                else attrs &= ~FileAttributes.Archive;

                File.SetAttributes(targetFilePath, attrs);

                // Modify timestamps if checked
                if (chkModifyStamps.Checked)
                {
                    DateTime created = dtpCreatedDate.Value.Date + dtpCreatedTime.Value.TimeOfDay;
                    DateTime modified = dtpModifiedDate.Value.Date + dtpModifiedTime.Value.TimeOfDay;
                    DateTime accessed = dtpAccessedDate.Value.Date + dtpAccessedTime.Value.TimeOfDay;

                    File.SetCreationTime(targetFilePath, created);
                    File.SetLastWriteTime(targetFilePath, modified);
                    File.SetLastAccessTime(targetFilePath, accessed);
                }

                MessageBox.Show("Attributes changed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to apply attributes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
