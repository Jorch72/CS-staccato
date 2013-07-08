using System;
using System.Linq;
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
            Random = new Random();
            AutoQueue = new ConcurrentQueue<Song>();
            UserQueue = new ConcurrentQueue<Song>();
            Timer = new Timer(Tick);
            for (int i = 0; i < 10; i++)
                QueueRandomSong();
        }

        public static Song NowPlaying { get; private set; }
        public static ConcurrentQueue<Song> UserQueue { get; set; }
        public static ConcurrentQueue<Song> AutoQueue { get; set; }
        public static DateTime StartTime { get; private set; }
        private static Timer Timer { get; set; }
        private static Random Random { get; set; }

        public static Song[] MasterQueue
        {
            get
            {
                return UserQueue.ToArray().Concat(AutoQueue.ToArray()).ToArray();
            }
        }

        public static void Start()
        {
            Tick(null);
        }

        private static void Tick(object discarded)
        {
            var song = PlayNextSong();
            StartTime = DateTime.Now.AddSeconds(3);
            Console.WriteLine("Now playing: {0} ({1}:{2})", song.Name, song.Duration.Minutes, song.Duration.Seconds);
            Timer = new Timer(Tick, null, (int)(song.Duration).TotalMilliseconds, Timeout.Infinite);
        }

        public static Song PlayNextSong()
        {
            Song song;
            if (UserQueue.Any())
                while (!UserQueue.TryDequeue(out song)) { }
            else
            {
                while (!AutoQueue.TryDequeue(out song)) { }
                QueueRandomSong();
            }
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
                Duration = duration,
                UserAdded = false
            };
            AutoQueue.Enqueue(song);
        }

        public static void QueueUserSong(string mp3)
        {
            var file = new AudioFile(mp3);
            var duration = file.Properties.Duration;
            var song = new Song
            {
                Name = Path.GetFileNameWithoutExtension(mp3),
                Stream = "/" + Path.GetFileName(mp3),
                Download = "/download/" + Path.GetFileName(mp3),
                Duration = duration,
                UserAdded = true
            };
            UserQueue.Enqueue(song);
        }

        public static void QueueRandomSong()
        {
            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3", SearchOption.AllDirectories);
            QueueSong(files[Random.Next(files.Length)]);
        }

        public static void QueueRandomUserSong()
        {
            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3", SearchOption.AllDirectories);
            QueueUserSong(files[Random.Next(files.Length)]);
        }
    }

    public class Song
    {
        public string Stream { get; set; }
        public string Name { get; set; }
        public string Download { get; set; }
        public TimeSpan Duration { get; set; }
        public bool UserAdded { get; set; }
    }
}

