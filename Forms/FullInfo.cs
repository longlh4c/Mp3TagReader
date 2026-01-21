using System;
using System.Windows.Forms;

namespace Mp3TagReader
{
    public partial class FullInfo : Form
    {
        private string filePath = string.Empty;

        public FullInfo(string path)
        {
            filePath = path;
            InitializeComponent();
        }

        private void FullInfo_Load(object sender, EventArgs e)
        {
            try
            {
                TagLib.File mp3 = TagLib.File.Create(filePath);
                txtArtist.Text = Manipulator.ArrayToString(mp3.Tag.Performers, ",");
                txtAlbum.Text = mp3.Tag.Album;
                txtTitle.Text = mp3.Tag.Title;
                txtTrack.Text = mp3.Tag.Track.ToString();
                txtYear.Text = mp3.Tag.Year.ToString();
                txtGenre.Text = Manipulator.ArrayToString(mp3.Tag.Genres, ",");
                txtLyrics.Text = mp3.Tag.Lyrics;
                txtComments.Text = mp3.Tag.Comment;
                txtAlbumArtist.Text = Manipulator.ArrayToString(mp3.Tag.AlbumArtists, ",");
                txtComposer.Text = Manipulator.ArrayToString(mp3.Tag.Composers, ",");
                txtCopyright.Text = mp3.Tag.Copyright;
                txtBPM.Text = mp3.Tag.BeatsPerMinute.ToString();
                txtDisc.Text = mp3.Tag.Disc.ToString();
                txtDiscCount.Text = mp3.Tag.DiscCount.ToString();
                txtAudioBitrate.Text = mp3.Properties.AudioBitrate.ToString();
                txtAudioChannels.Text = mp3.Properties.AudioChannels.ToString();
                txtAudioSampleRate.Text = mp3.Properties.AudioSampleRate.ToString();
                txtDescription.Text = mp3.Properties.Description;
                txtDuration.Text = mp3.Properties.Duration.Minutes + ":" + mp3.Properties.Duration.Seconds;
            }
            catch (Exception) { }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                TagLib.File mp3 = TagLib.File.Create(filePath);
                mp3.Tag.Performers = new string[] { txtArtist.Text };
                mp3.Tag.Album = txtAlbum.Text;
                mp3.Tag.Title = txtTitle.Text;
                mp3.Tag.Track = Convert.ToUInt32(txtTrack.Text);
                mp3.Tag.Year = Convert.ToUInt32(txtYear.Text);
                mp3.Tag.Genres = new string[] { txtGenre.Text };
                mp3.Tag.Lyrics = txtLyrics.Text;
                mp3.Tag.Comment = txtComments.Text;
                mp3.Tag.AlbumArtists = new string[] { txtAlbumArtist.Text };
                mp3.Tag.Composers = new string[] { txtComposer.Text };
                mp3.Tag.Copyright = txtCopyright.Text;
                mp3.Tag.BeatsPerMinute = Convert.ToUInt32(txtBPM.Text);
                mp3.Tag.Disc = Convert.ToUInt32(txtDisc.Text);
                mp3.Tag.DiscCount = Convert.ToUInt32(txtDiscCount.Text);
                mp3.Save();
                lblResult.Text = "Saved";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void txtLyrics_DoubleClick(object sender, EventArgs e)
        {
            if (txtLyrics.Text != string.Empty)
            {
                LyricsViewer lv = new LyricsViewer(txtArtist.Text, txtTitle.Text, txtLyrics.Text);
                lv.Show();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}