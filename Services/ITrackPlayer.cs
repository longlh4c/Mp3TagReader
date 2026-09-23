using System;

namespace Mp3TagReader.Services
{
    public class TrackEndedEventArgs : EventArgs
    {
        public TrackEndedEventArgs(bool failed, string error)
        {
            Failed = failed;
            Error = error;
        }

        // true when the file could not be played (it did not simply reach its end)
        public bool Failed { get; private set; }
        public string Error { get; private set; }
    }

    // Plays one file at a time; the playlist (next / previous / shuffle / repeat) is managed by MainForm.
    public interface ITrackPlayer : IDisposable
    {
        string Name { get; }

        // current position in seconds; setting it seeks
        double Position { get; set; }

        void Play(string path, double startSeconds);
        void Pause();
        void Resume();
        void Stop();

        // raised when the current file ends or fails; may be raised on a worker thread
        event EventHandler<TrackEndedEventArgs> TrackEnded;
    }
}
