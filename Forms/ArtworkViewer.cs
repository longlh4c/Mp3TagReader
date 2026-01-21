using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mp3TagReader
{
    public partial class ArtworkViewer : Form
    {
        public ArtworkViewer()
        {
            InitializeComponent();
        }

        private Image image;

        public ArtworkViewer(Image img)
        {
            InitializeComponent();
            image = img;
        }

        private void ArtworkViewer_Load(object sender, EventArgs e)
        {
            picBxArt.Image = image;
        }

        private void picBxArt_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}