namespace Mp3TagReader
{
    partial class ArtworkViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ArtworkViewer));
            this.picBxArt = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picBxArt)).BeginInit();
            this.SuspendLayout();
            // 
            // picBxArt
            // 
            this.picBxArt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picBxArt.Location = new System.Drawing.Point(0, 0);
            this.picBxArt.Name = "picBxArt";
            this.picBxArt.Size = new System.Drawing.Size(504, 502);
            this.picBxArt.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picBxArt.TabIndex = 0;
            this.picBxArt.TabStop = false;
            this.picBxArt.Click += new System.EventHandler(this.picBxArt_Click);
            // 
            // ArtworkViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(504, 502);
            this.Controls.Add(this.picBxArt);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ArtworkViewer";
            this.Text = "ArtworkViewer";
            this.Load += new System.EventHandler(this.ArtworkViewer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picBxArt)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picBxArt;
    }
}