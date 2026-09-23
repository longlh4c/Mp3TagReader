using System;
using System.Collections.Generic;

namespace Mp3TagReader.Services
{
    public enum RepeatMode
    {
        Off,
        All,
        One
    }

    // The songs of the list and the order they are played in (shuffled or not).
    // Decides which song comes next; knows nothing about the player or the UI.
    public class Playlist
    {
        private readonly Random random = new Random();
        private List<string> songs = new List<string>();
        private List<int> order = new List<int>(); // indexes into songs, in play order
        private int position = -1;                 // current position in order
        private bool shuffle;

        public int Count
        {
            get { return songs.Count; }
        }

        // song at the current position, null when the playlist is empty
        public string Current
        {
            get { return position >= 0 && position < order.Count ? songs[order[position]] : null; }
        }

        // the songs in play order
        public IList<string> PlayOrder
        {
            get
            {
                List<string> result = new List<string>();
                foreach (int index in order) result.Add(songs[index]);
                return result.AsReadOnly();
            }
        }

        // Replace the songs; startIndex (into songs) becomes the current song. With shuffle the start song
        // is played first and the others follow in random order.
        public void Load(IList<string> newSongs, int startIndex, bool shuffleOn)
        {
            songs = new List<string>(newSongs);
            shuffle = shuffleOn;
            if (songs.Count == 0)
            {
                order = new List<int>();
                position = -1;
                return;
            }
            position = BuildOrder(Math.Max(0, Math.Min(startIndex, songs.Count - 1)));
        }

        // re-order the songs after the current one; the current song stays current
        public void SetShuffle(bool shuffleOn)
        {
            if (shuffle == shuffleOn) return;
            shuffle = shuffleOn;
            if (Current != null)
            {
                position = BuildOrder(order[position]);
            }
        }

        // previous / next song, wrapping around at both ends
        public string Skip(int step)
        {
            if (order.Count == 0) return null;
            int count = order.Count;
            position = ((position + step) % count + count) % count;
            return Current;
        }

        // Move on after the current song ended (or could not be played). Returns the song to play, or null to stop.
        public string MoveAfterEnd(RepeatMode repeat, bool failed)
        {
            if (order.Count == 0) return null;

            if (repeat == RepeatMode.One && !failed)
            {
                return Current;
            }

            if (position + 1 < order.Count)
            {
                position++;
                return Current;
            }

            if (repeat == RepeatMode.Off)
            {
                return null;
            }

            // repeat: start over, with a new random order when shuffling
            position = shuffle ? BuildOrder(random.Next(songs.Count)) : 0;
            return Current;
        }

        // keep the playlist valid when a file of it is renamed
        public void Rename(string oldPath, string newPath)
        {
            for (int i = 0; i < songs.Count; i++)
            {
                if (string.Equals(songs[i], oldPath, StringComparison.OrdinalIgnoreCase))
                {
                    songs[i] = newPath;
                }
            }
        }

        // Returns the position of startIndex in the new order.
        private int BuildOrder(int startIndex)
        {
            order = new List<int>();
            for (int i = 0; i < songs.Count; i++) order.Add(i);
            if (!shuffle) return startIndex;

            order.Remove(startIndex);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int t = order[i];
                order[i] = order[j];
                order[j] = t;
            }
            order.Insert(0, startIndex);
            return 0;
        }
    }
}
