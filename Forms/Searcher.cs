using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Mp3TagReader.Forms
{
    public partial class Searcher : Form
    {
        private string searchString = string.Empty;

        public Searcher(string songName)
        {
            InitializeComponent();
            if (songName != string.Empty)
            {
                _txtSearchString.Text = songName;
                searchString = songName;
                _rbLyric.Checked = true;
            }
        }

        private void _btnSearch_Click(object sender, EventArgs e)
        {
            searchString = _txtSearchString.Text.Trim();

            if (searchString == string.Empty)
            {
                MessageBox.Show("Nothing to search", "Warning");
                _txtSearchString.Focus();
                return;
            }

            string query = Uri.EscapeDataString(searchString).Replace("%20", "+");
            string url = string.Empty;

            if (_rbZing.Checked)
            {
                url = "http://mp3.zing.vn/tim-kiem/bai-hat.html?q=" + query;
            }
            else if (_rbSub.Checked)
            {
                url = "http://subscene.com/subtitles/title.aspx?q=" + query + "&l=";
            }
            else if (_rbLyric.Checked)
            {
                url = "http://search.azlyrics.com/search.php?q=" + query;
            }

            if (url != string.Empty)
            {
                Process.Start(url);
            }
        }
    }
}