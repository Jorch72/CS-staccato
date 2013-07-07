using System;
using System.IO;
using System.Collections.Concurrent;
using TagLib.Mpeg;
using System.Threading;

namespace staccato
{
    public static class MusicRunner
    {
        static MusicRunner()
        {
            var random = new Random();
            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3", SearchOption.AllDirectories);
            Playlist = new ConcurrentQueue<Song>();
            QueueSong(files[random.Next(files.Length)]);
            Timer = new Timer(Tick);
        }

        public static Song NowPlaying { get; private set; }
        public static ConcurrentQueue<Song> Playlist { get; set; }
        public static DateTime StartTime { get; private set; }
        private static Timer Timer { get; set; }

        public static void Start()
        {
            Tick(null);
        }

        private static void Tick(object discarded)
        {
            var song = PlayNextSong();
            StartTime = DateTime.Now;
            Timer.Change((int)(song.Duration + TimeSpan.FromSeconds(3)).TotalMilliseconds, Timeout.Infinite);
        }

        public static Song PlayNextSong()
        {
            Song song;
            while (!Playlist.TryDequeue(out song)) { }
            NowPlaying = song;
            return song;
        }

        public static void QueueSong(string mp3)
        {
            var file = new AudioFile(mp3);
            var duration = file.Properties.Duration;
            var song = new Song
            {
                Name = Path.GetFileNameWithoutExtension(mp3),
                Stream = "/" + Path.GetFileName(mp3),
                Download = "/download/" + Path.GetFileName(mp3),
                Duration = duration
            };
            Playlist.Enqueue(song);
        }
    }

    public class Song
    {
        public string Stream { get; set; }
        public string Name { get; set; }
        public string Download { get; set; }
        public TimeSpan Duration { get; set; }
    }
}

