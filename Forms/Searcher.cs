using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Mp3TagReader.Forms
{
    // searches the lyrics of a song on AZLyrics in the default browser
    public partial class Searcher : Form
    {
        public Searcher(string songName)
        {
            InitializeComponent();
            _txtSearchString.Text = songName ?? string.Empty;
        }

        private void _btnSearch_Click(object sender, EventArgs e)
        {
            string searchString = _txtSearchString.Text.Trim();
            if (searchString == string.Empty)
            {
                MessageBox.Show("Nothing to search", "Warning");
                _txtSearchString.Focus();
                return;
            }

            string query = Uri.EscapeDataString(searchString).Replace("%20", "+");
            try
            {
                Process.Start("https://search.azlyrics.com/search.php?q=" + query);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }
        }
    }
}
