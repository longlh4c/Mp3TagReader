using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Mp3TagReader
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
            searchString = _txtSearchString.Text;
            string url = string.Empty;

            if (searchString == string.Empty)
            {
                MessageBox.Show("Nothing to search", "Warning");
                _txtSearchString.Focus();
                return;
            }

            else
            {
                searchString = searchString.Trim();

                if (_rbZing.Checked == true)
                {
                    url = @"http://mp3.zing.vn/tim-kiem/bai-hat.html?q=";
                    string[] words = searchString.Split(' ');
                    foreach (string word in words)
                    {
                        url += word + "+";
                    }

                    url = url.Substring(0, url.LastIndexOf('+'));

                    Process.Start(url);
                }

                else if (_rbSub.Checked == true)
                {
                    url = @"http://subscene.com/subtitles/title.aspx?q=";
                    string[] words = searchString.Split(' ');
                    foreach (string word in words)
                    {
                        url += word + "+";
                    }

                    url = url.Substring(0, url.LastIndexOf('+'));

                    url += @"&l=";

                    Process.Start(url);
                }

                else if (_rbLyric.Checked == true)
                {
                    url = @"http://search.azlyrics.com/search.php?q=";
                    string[] words = searchString.Split(' ');
                    foreach (string word in words)
                    {
                        url += word + "+";
                    }

                    url = url.Substring(0, url.LastIndexOf('+'));

                    Process.Start(url);
                }
            }
        }
    }
}