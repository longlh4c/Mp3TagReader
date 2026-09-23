namespace Mp3TagReader.Forms
{
    partial class Searcher
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Searcher));
            this._btnSearch = new System.Windows.Forms.Button();
            this._txtSearchString = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // _btnSearch
            // 
            this._btnSearch.Location = new System.Drawing.Point(274, 12);
            this._btnSearch.Name = "_btnSearch";
            this._btnSearch.Size = new System.Drawing.Size(75, 23);
            this._btnSearch.TabIndex = 1;
            this._btnSearch.Text = "Search";
            this._btnSearch.UseVisualStyleBackColor = true;
            this._btnSearch.Click += new System.EventHandler(this._btnSearch_Click);
            // 
            // _txtSearchString
            // 
            this._txtSearchString.Location = new System.Drawing.Point(12, 14);
            this._txtSearchString.Name = "_txtSearchString";
            this._txtSearchString.Size = new System.Drawing.Size(256, 20);
            this._txtSearchString.TabIndex = 0;
            // 
            // Searcher
            // 
            this.AcceptButton = this._btnSearch;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(361, 47);
            this.Controls.Add(this._txtSearchString);
            this.Controls.Add(this._btnSearch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Searcher";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Search Lyrics (AZLyrics)";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.Button _btnSearch;
        private System.Windows.Forms.TextBox _txtSearchString;
    }
}