using System;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using TagLib.Mpeg;
using System.Threading;
using System.Collections.Generic;
using System.Net;

namespace staccato
{
    public static class MusicRunner
    {
        static MusicRunner()
        {
            Random = new Random();
            AutoQueue = new ConcurrentQueue<Song>();
            UserQueue = new ConcurrentQueue<Song>();
            ActiveListeners = new List<Tuple<string, DateTime>>();
            SkipRequests = new List<string>();
            UserRequests = new Dictionary<string, List<DateTime>>();
            UploadTimes = new Dictionary<string, DateTime>();
            Listeners = 0;
            Timer = new Timer(Tick);
            for (int i = 0; i < 10; i++)
                QueueRandomSong();
        }

        public static Song NowPlaying { get; private set; }
        public static ConcurrentQueue<Song> UserQueue { get; set; }
        public static ConcurrentQueue<Song> AutoQueue { get; set; }
        public static DateTime StartTime { get; private set; }
        public static int Listeners { get; private set; }
        public static string Announcement { get; set; }
        public static List<string> SkipRequests { get; set; }
        private static Timer Timer { get; set; }
        private static Random Random { get; set; }
        private static List<Tuple<string, DateTime>> ActiveListeners { get; set; }
        private static Dictionary<string, List<DateTime>> UserRequests { get; set; }
        private static Dictionary<string, DateTime> UploadTimes { get; set; }

        public static Song[] MasterQueue
        {
            get
            {
                return UserQueue.ToArray().Concat(AutoQueue.ToArray()).ToArray();
            }
        }

        public static int SkipRequestsIssued
        {
            get
            {
                return SkipRequests.Count;
            }
        }

        public static int SkipRequestsRequired
        {
            get
            {
                return (int)Math.Ceiling(Listeners * Program.Configuration.SkipThreshold);
            }
        }

        public static void Start()
        {
            Tick(null);
        }

        private static void Tick(object discarded)
        {
            var song = PlayNextSong();
            SkipRequests.Clear();
            StartTime = DateTime.Now;
            Console.WriteLine("Now playing: {0} ({1}:{2})", song.Name, song.Duration.Minutes, song.Duration.Seconds.ToString("00"));
            if (Program.Configuration.Irc.Enabled)
                Program.IrcBot.AnnounceSong(song);
            UpdateListeners();
            Timer.Change(Timeout.Infinite, Timeout.Infinite);
            Timer = new Timer(Tick, null, (int)(song.Duration).TotalMilliseconds, Timeout.Infinite);
        }

        static void UpdateListeners()
        {
            foreach (var listener in ActiveListeners.ToArray())
            {
                if (listener.Item2.AddMinutes(1) < DateTime.Now)
                    ActiveListeners.Remove(listener);
            }
            Listeners = ActiveListeners.Count;
        }

        public static void UpdateListener(string user)
        {
            var listener = ActiveListeners.SingleOrDefault(l => l.Item1.Equals(user));
            ActiveListeners.Remove(listener);
            listener = new Tuple<string, DateTime>(user, DateTime.Now);
            ActiveListeners.Add(listener);
            Listeners = ActiveListeners.Count;
        }

        public static bool RequestSkip(string user)
        {
            if (((StartTime + NowPlaying.Duration) - DateTime.Now).TotalSeconds < 10)
                return false;
            if (SkipRequests.Contains(user))
                return false;
            SkipRequests.Add(user);
            if (SkipRequestsIssued >= SkipRequestsRequired)
            {
                Console.WriteLine("Skipping {0}", NowPlaying.Name);
                Tick(null);
                return true;
            }
            return false;
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

        /// <summary>
        /// Returns true if the user can request another song.
        /// </summary>
        public static bool QueueUserSong(string mp3, string user)
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
            if (MasterQueue.Any(s => s.Name == song.Name))
                return true;
            if (!UserRequests.ContainsKey(user))
            {
                UserRequests[user] = new List<DateTime>();
                UserRequests[user].Add(DateTime.Now);
            }
            else
            {
                foreach (var item in UserRequests[user].ToArray())
                {
                    if (item.AddMinutes(Program.Configuration.RequestLimitResetMinutes) < DateTime.Now)
                        UserRequests[user].Remove(item);
                }
                if (UserRequests[user].Count >= Program.Configuration.MaximumRequestsPerUser)
                    return false;
                UserRequests[user].Add(DateTime.Now);
            }
            UserQueue.Enqueue(song);
            return UserRequests[user].Count < Program.Configuration.MaximumRequestsPerUser;
        }

        public static void QueueRandomSong()
        {
            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3", SearchOption.AllDirectories)
                .Where(r => !MasterQueue.Any(s => s.Name == Path.GetFileNameWithoutExtension(r))).ToArray();
            QueueSong(files[Random.Next(files.Length)]);
        }

        public static void QueueRandomUserSong(string user)
        {
            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3", SearchOption.AllDirectories);
            QueueUserSong(files[Random.Next(files.Length)], user);
        }

        public static bool CanUserRequest(string user)
        {
            if (!UserRequests.ContainsKey(user))
                return true;
            foreach (var item in UserRequests[user].ToArray())
            {
                if (item.AddMinutes(Program.Configuration.RequestLimitResetMinutes) < DateTime.Now)
                    UserRequests[user].Remove(item);
            }
            return UserRequests[user].Count < Program.Configuration.MaximumRequestsPerUser;
        }

        public static int MinutesUntilNextUpload(string user)
        {
            if (!UploadTimes.ContainsKey(user))
                return 0;
            var minutes = (UploadTimes[user].AddMinutes(Program.Configuration.MinimumMinutesBetweenUploads) - DateTime.Now).TotalMinutes;
            if (minutes <= 0)
                return 0;
            return (int)minutes;
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

