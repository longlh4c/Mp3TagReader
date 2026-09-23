using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Mp3TagReader.Helpers;
using Mp3TagReader.Services;

namespace Mp3TagReader.Forms
{
    // Music player: plays the audio files of the list with the bundled Libs\ffplay.exe
    public partial class MainForm
    {
        public enum PlaybackState
        {
            Stopped,
            Playing,
            Paused
        }

        // null when Libs\ffplay.exe is missing
        private ITrackPlayer player = null;
        private readonly Playlist playlist = new Playlist();
        private PlaybackState currentPlaybackState = PlaybackState.Stopped;
        private RepeatMode repeatMode = RepeatMode.Off;

        // song currently loaded in the player; kept separate from selectedFileName (the file shown in the tag editor)
        private string nowPlayingFile = string.Empty;
        private double nowPlayingDuration = 0;

        // files that failed one after the other; playback stops when the whole list failed
        private int consecutiveFailures = 0;

        private void InitializePlayer()
        {
            string ffplay = FfplayTrackPlayer.FindFfplay();
            if (ffplay != null)
            {
                player = new FfplayTrackPlayer(ffplay);
                player.TrackEnded += player_TrackEnded;
            }
            else
            {
                playerGroupBox.Enabled = false;
                playerGroupBox.Text = "Music Player (Libs\\ffplay.exe not found)";
            }
            UpdatePlayerButtons();
        }

        private void DisposePlayer()
        {
            if (player == null) return;
            try
            {
                // also ends a running ffplay process
                player.Dispose();
            }
            catch { }
        }

        private bool EnsurePlayerAvailable()
        {
            if (player != null) return true;
            MessageBox.Show("Libs\\ffplay.exe was not found next to Mp3TagReader.exe, so songs cannot be played.",
                "Music Player", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        #region Playback state and buttons

        private void UpdatePlaybackUI(PlaybackState newState)
        {
            currentPlaybackState = newState;
            _btnPlayAll.Text = newState == PlaybackState.Playing ? "⏸" : "▶"; // ⏸ / ▶
            timerNowPlayingText.Enabled = newState == PlaybackState.Playing;
            UpdatePlayerButtons();
        }

        // Play needs a selected audio file (or a song already loaded); Prev / Next / Stop / seek need a loaded song
        private void UpdatePlayerButtons()
        {
            bool hasPlayer = player != null;
            bool active = currentPlaybackState != PlaybackState.Stopped;
            bool audioSelected = false;
            try
            {
                foreach (DataGridViewRow row in gridView.SelectedRows)
                {
                    object value = row.Cells["ColumnPath"].Value;
                    if (value != null && Manipulator.IsAudioFile(value.ToString()))
                    {
                        audioSelected = true;
                        break;
                    }
                }
            }
            catch { } // columns not created yet

            _btnPlayAll.Enabled = hasPlayer && (active || audioSelected);
            _btnPrev.Enabled = hasPlayer && active;
            _btnNext.Enabled = hasPlayer && active;
            _btnStop.Enabled = hasPlayer && active;
            progressBar.Enabled = hasPlayer && active;
            _btnDisableTimerSong.Enabled = hasPlayer && active;
        }

        #endregion

        #region Play / pause / stop

        // Play the current row with the audio files of the list as playlist. Double-click passes
        // openNonAudioExternally: a video or other file is opened with its default application instead.
        private void PlaySelectedMp3Files(bool openNonAudioExternally)
        {
            string path = CurrentRowPath();
            if (path == null) return;

            if (openNonAudioExternally && !Manipulator.IsAudioFile(path))
            {
                OpenWithDefaultApp(path);
                return;
            }

            if (!EnsurePlayerAvailable()) return;

            // double-click on the song that is already playing: keep playing
            if (currentPlaybackState == PlaybackState.Playing &&
                string.Equals(nowPlayingFile, path, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // all audio files of the list, so Prev / Next move through it; start at the selected song
            // (or the next audio file after the selected row)
            List<string> songs = new List<string>();
            int startIndex = 0;
            int selectedRow = gridView.CurrentRow.Index;
            foreach (DataGridViewRow row in gridView.Rows)
            {
                object value = row.Cells["ColumnPath"].Value;
                if (value != null && Manipulator.IsAudioFile(value.ToString()))
                {
                    songs.Add(value.ToString());
                    if (row.Index < selectedRow) startIndex++;
                }
            }

            if (songs.Count == 0)
            {
                MessageBox.Show("No audio file in list", "Warning");
                return;
            }

            try
            {
                consecutiveFailures = 0;
                playlist.Load(songs, startIndex < songs.Count ? startIndex : 0, _chkShuffle.Checked);
                PlayCurrent();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void PlayCurrent()
        {
            string path = playlist.Current;
            nowPlayingFile = path;
            nowPlayingDuration = GetDuration(path);
            player.Play(path, 0);
            UpdatePlaybackUI(PlaybackState.Playing);
            this.Text = "Now Playing - " + Path.GetFileNameWithoutExtension(path) + " | ";
            UpdateProgress();
        }

        private static double GetDuration(string path)
        {
            try
            {
                return TagLib.File.Create(path).Properties.Duration.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        private void StopPlayback()
        {
            try
            {
                if (player != null) player.Stop();
            }
            catch { }
            nowPlayingFile = string.Empty;
            nowPlayingDuration = 0;
            UpdatePlaybackUI(PlaybackState.Stopped);
            this.Text = "Mp3TagReader";
            ResetProgress();
        }

        private void _btnPlayList_Click(object sender, EventArgs e)
        {
            if (!EnsurePlayerAvailable()) return;

            try
            {
                switch (currentPlaybackState)
                {
                    case PlaybackState.Stopped:
                        if (listMp3Infos.Count == 0)
                        {
                            MessageBox.Show("No song in list", "Warning");
                            return;
                        }
                        PlaySelectedMp3Files(false);
                        break;
                    case PlaybackState.Playing:
                        player.Pause();
                        UpdatePlaybackUI(PlaybackState.Paused);
                        break;
                    case PlaybackState.Paused:
                        player.Resume();
                        UpdatePlaybackUI(PlaybackState.Playing);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void _btnStop_Click(object sender, EventArgs e)
        {
            StopPlayback();
        }

        // raised on a worker thread: continue on the UI thread
        private void player_TrackEnded(object sender, TrackEndedEventArgs e)
        {
            try
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)delegate { OnTrackEnded(e); });
                }
            }
            catch (InvalidOperationException) { }
        }

        private void OnTrackEnded(TrackEndedEventArgs e)
        {
            if (currentPlaybackState == PlaybackState.Stopped || playlist.Count == 0) return;

            // a late event for a song we already left (Next was clicked just as it ended)
            if (!string.Equals(e.Path, nowPlayingFile, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                if (e.Failed)
                {
                    consecutiveFailures++;
                    lblResult.Text = "Cannot play " + Path.GetFileName(nowPlayingFile) + (e.Error != null ? ": " + e.Error : "");
                    if (consecutiveFailures >= playlist.Count)
                    {
                        StopPlayback(); // nothing in the list can be played
                        return;
                    }
                }
                else
                {
                    consecutiveFailures = 0;
                }

                if (playlist.MoveAfterEnd(repeatMode, e.Failed) != null)
                {
                    PlayCurrent();
                }
                else
                {
                    StopPlayback();
                }
            }
            catch (Exception ex)
            {
                StopPlayback();
                lblResult.Text = "Player error: " + ex.Message;
            }
        }

        #endregion

        #region Previous / next / shuffle / repeat

        private void _btnPrev_Click(object sender, EventArgs e)
        {
            SkipTrack(-1);
        }

        private void _btnNext_Click(object sender, EventArgs e)
        {
            SkipTrack(1);
        }

        // previous / next song in the play order, wrapping around at both ends
        private void SkipTrack(int step)
        {
            if (player == null || currentPlaybackState == PlaybackState.Stopped || playlist.Count == 0)
            {
                MessageBox.Show("Not Playing", "Warning");
                return;
            }

            try
            {
                playlist.Skip(step);
                PlayCurrent();
                SelectRowByPath(nowPlayingFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void _chkShuffle_CheckedChanged(object sender, EventArgs e)
        {
            // re-orders the songs after the current one; the current song keeps playing
            playlist.SetShuffle(_chkShuffle.Checked);
        }

        private bool updatingReplayButton = false;

        // cycles Off -> Repeat playlist -> Repeat one
        private void chkReplay_CheckedChanged(object sender, EventArgs e)
        {
            // setting _chkReplay.Checked below raises this event again; ignore those nested calls
            if (updatingReplayButton) return;

            repeatMode = repeatMode == RepeatMode.Off ? RepeatMode.All
                       : repeatMode == RepeatMode.All ? RepeatMode.One
                       : RepeatMode.Off;

            updatingReplayButton = true;
            try
            {
                _chkReplay.Checked = repeatMode != RepeatMode.Off;
                _chkReplay.Text = repeatMode == RepeatMode.All ? "↺ᴺ"   // ↺ᴺ (all)
                                : repeatMode == RepeatMode.One ? "↺¹"   // ↺¹ (one)
                                : "↺";                                       // ↺ (off)
            }
            finally
            {
                updatingReplayButton = false;
            }
        }

        // "Find Playing Song": select the playing song in the list
        private void _btnDisableTimerSong_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(nowPlayingFile)) return;
            if (!SelectRowByPath(nowPlayingFile))
            {
                MessageBox.Show("Currently playing song is not in the current list.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region Progress

        private static string FormatTime(double seconds)
        {
            int total = (int)Math.Floor(Math.Max(0, seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        private void UpdateProgress()
        {
            double duration = nowPlayingDuration;
            double position = player != null ? player.Position : 0;

            progressBar.Minimum = 0;
            progressBar.Maximum = Math.Max(0, (int)duration);
            progressBar.Value = Math.Max(0, Math.Min((int)position, progressBar.Maximum));

            // "1:23" on the left, "5:55" on the right
            labelTimerSong.Text = FormatTime(position);
            if (lblTotalTime != null)
            {
                lblTotalTime.Text = FormatTime(duration);
            }
        }

        private void ResetProgress()
        {
            progressBar.Value = 0;
            labelTimerSong.Text = "0:00";
            if (lblTotalTime != null)
            {
                lblTotalTime.Text = "0:00";
            }
        }

        // seek to the clicked position (also while paused)
        private void progressBar_Click(object sender, EventArgs e)
        {
            if (player == null || currentPlaybackState == PlaybackState.Stopped) return;

            int value = ((MouseEventArgs)e).X * progressBar.Maximum / progressBar.Width;
            player.Position = Math.Max(progressBar.Minimum, Math.Min(value, progressBar.Maximum));
            UpdateProgress();
        }

        private void timerSong_Tick(object sender, EventArgs e)
        {
            try
            {
                // song changes / end of playlist are handled in OnTrackEnded; here only the progress is refreshed
                if (player != null && currentPlaybackState == PlaybackState.Playing)
                {
                    UpdateProgress();
                }
            }
            catch { }
        }

        // scrolls the "Now Playing - ..." window title
        private void timerNowPlaying_Tick(object sender, EventArgs e)
        {
            if (this.Text != "Mp3TagReader" && this.Text.Length > 1)
            {
                this.Text = this.Text.Substring(1) + this.Text.Substring(0, 1);
            }
        }

        #endregion
    }
}
