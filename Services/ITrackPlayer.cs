using System;

namespace Mp3TagReader.Services
{
    public class TrackEndedEventArgs : EventArgs
    {
        public TrackEndedEventArgs(string path, bool failed, string error)
        {
            Path = path;
            Failed = failed;
            Error = error;
        }

        // the file that ended; lets the receiver ignore a late event for a song it has already left
        public string Path { get; private set; }

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
