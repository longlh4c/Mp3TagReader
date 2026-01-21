namespace Mp3TagReader
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
            this._rbZing = new System.Windows.Forms.RadioButton();
            this._rbSub = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._rbLyric = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _btnSearch
            // 
            this._btnSearch.Location = new System.Drawing.Point(211, 79);
            this._btnSearch.Name = "_btnSearch";
            this._btnSearch.Size = new System.Drawing.Size(75, 46);
            this._btnSearch.TabIndex = 1;
            this._btnSearch.Text = "Search";
            this._btnSearch.UseVisualStyleBackColor = true;
            this._btnSearch.Click += new System.EventHandler(this._btnSearch_Click);
            // 
            // _txtSearchString
            // 
            this._txtSearchString.Location = new System.Drawing.Point(30, 28);
            this._txtSearchString.Name = "_txtSearchString";
            this._txtSearchString.Size = new System.Drawing.Size(256, 20);
            this._txtSearchString.TabIndex = 0;
            // 
            // _rbZing
            // 
            this._rbZing.AutoSize = true;
            this._rbZing.Checked = true;
            this._rbZing.Location = new System.Drawing.Point(6, 20);
            this._rbZing.Name = "_rbZing";
            this._rbZing.Size = new System.Drawing.Size(107, 17);
            this._rbZing.TabIndex = 2;
            this._rbZing.TabStop = true;
            this._rbZing.Text = "Search Mp3.Zing";
            this._rbZing.UseVisualStyleBackColor = true;
            // 
            // _rbSub
            // 
            this._rbSub.AutoSize = true;
            this._rbSub.Location = new System.Drawing.Point(6, 43);
            this._rbSub.Name = "_rbSub";
            this._rbSub.Size = new System.Drawing.Size(110, 17);
            this._rbSub.TabIndex = 3;
            this._rbSub.Text = "Search Subscene";
            this._rbSub.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this._rbLyric);
            this.groupBox1.Controls.Add(this._rbZing);
            this.groupBox1.Controls.Add(this._rbSub);
            this.groupBox1.Location = new System.Drawing.Point(30, 65);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(166, 97);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Search Option";
            // 
            // _rbLyric
            // 
            this._rbLyric.AutoSize = true;
            this._rbLyric.Location = new System.Drawing.Point(6, 66);
            this._rbLyric.Name = "_rbLyric";
            this._rbLyric.Size = new System.Drawing.Size(103, 17);
            this._rbLyric.TabIndex = 4;
            this._rbLyric.Text = "Search AZLyrics";
            this._rbLyric.UseVisualStyleBackColor = true;
            // 
            // Searcher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(311, 177);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this._txtSearchString);
            this.Controls.Add(this._btnSearch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Searcher";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Search";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _btnSearch;
        private System.Windows.Forms.TextBox _txtSearchString;
        private System.Windows.Forms.RadioButton _rbZing;
        private System.Windows.Forms.RadioButton _rbSub;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton _rbLyric;
    }
}