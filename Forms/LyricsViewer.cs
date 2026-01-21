using System;
using System.Windows.Forms;

namespace Mp3TagReader
{
    public partial class LyricsViewer : Form
    {
        private string artist = string.Empty;
        private string title = string.Empty;
        private string lyrics = string.Empty;

        public LyricsViewer(string Artist, string Title, string Lyrics)
        {
            this.artist = Artist;
            this.title = Title;
            this.lyrics = Lyrics;
            InitializeComponent();
        }

        private void LyricsViewer_Load(object sender, EventArgs e)
        {
            _lblArtist.Text = artist;
            _lblTitle.Text = title;
            _txtLyrics.Text = lyrics;
        }

        private void _btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}