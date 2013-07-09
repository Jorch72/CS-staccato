using System;
using System.Linq;
using WebSharp.MVC;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace staccato
{
    public class IndexController : Controller
    {
        public const int UploadedPartLength = 16384; // 16K per chunk

        public ActionResult Index()
        {
            MusicRunner.UpdateListener(Request.RemoteEndPoint);
            return View();
        }

        public ActionResult NowPlaying()
        {
            MusicRunner.UpdateListener(Request.RemoteEndPoint);
            return Json(new
            {
                song = MusicRunner.NowPlaying,
                seek = (DateTime.Now - MusicRunner.StartTime).TotalSeconds,
                queue = MusicRunner.MasterQueue.Take(8),
                skipsRequested = MusicRunner.SkipRequestsIssued,
                skipsRequired = MusicRunner.SkipRequestsRequired,
                listeners = MusicRunner.Listeners,
                announcement = MusicRunner.Announcement
            });
        }

        public ActionResult Search(string query, int page)
        {
            query = query.Replace('+', ' ').ToUpper(); // TODO: WebSharp should probably handle this
            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3")
                .OrderBy(Path.GetFileNameWithoutExtension)
                .Where(f => Path.GetFileNameWithoutExtension(f).ToUpper().Contains(query)).ToArray();
            bool canRequest = MusicRunner.CanUserRequest(Request.RemoteEndPoint);

            return Json(new {
                results = files
                    .Skip(10 * (page - 1)).Take(10).Select(f => 
                    new 
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        Download = "/download/" + Path.GetFileName(f),
                        Stream = Path.GetFileName(f),
                        CanRequest = canRequest && !MusicRunner.MasterQueue.Any(s => s.Name.Equals(Path.GetFileNameWithoutExtension(f)))
                    }).ToArray(),
                totalPages = files.Count() / 10
            });
        }

        public ActionResult RequestSong(string song)
        {
            if (song.Contains(Path.DirectorySeparatorChar))
                return Json(new { success = false });
            song = song.Replace('+', ' ');
            song = Path.Combine(Program.Configuration.MusicPath, song + ".mp3");
            if (!File.Exists(song))
                return Json(new { success = false });
            return Json(new 
            {
                success = MusicRunner.QueueUserSong(song, Request.RemoteEndPoint),
                canRequest = MusicRunner.CanUserRequest(Request.RemoteEndPoint), 
                queue = MusicRunner.MasterQueue.Take(8)
            });
        }

        public ActionResult RequestSkip()
        {
            return Json(new
            {
                success = MusicRunner.RequestSkip(Request.RemoteEndPoint),
                song = MusicRunner.NowPlaying,
                queue = MusicRunner.MasterQueue.Take(8),
                seek = (DateTime.Now - MusicRunner.StartTime).TotalSeconds,
                skipsRequested = MusicRunner.SkipRequestsIssued,
                skipsRequired = MusicRunner.SkipRequestsRequired
            });
        }

        public static Regex SongNameRegex = new Regex("^.{3,50} - .{3,50}\\.mp3$", RegexOptions.Compiled);
        public ActionResult CanUpload(string filename)
        {
            filename = filename.Replace('+', ' ');
            if (Uploads.ContainsKey(Request.RemoteEndPoint.Address.ToString()))
                return Json(new { success = false, reason = "One upload at a time, please." });
            if (filename.Contains(Path.DirectorySeparatorChar))
                return Json(new { success = false, reason = "Invalid filename." });
            if (!SongNameRegex.IsMatch(filename))
                return Json(new { success = false, reason = "File name is not in \"Artist Name - Song Title.mp3\" format." });
            if (File.Exists(Path.Combine(Program.Configuration.MusicPath, filename)))
                return Json(new { success = false, reason = "This song is already in our library." });
            var minutes = MusicRunner.MinutesUntilNextUpload(Request.RemoteEndPoint);
            if (minutes > 0)
                return Json(new { success = false, reason = string.Format("You need to wait another {0} minutes before you can upload again.", minutes) });

            return Json(new { success = true });
        }

        public ActionResult StartUpload(string filename, long length64)
        {
            filename = filename.Replace('+', ' ');
            if (Uploads.ContainsKey(Request.RemoteEndPoint.Address.ToString()))
                return Json(new { success = false, reason = "One upload at a time, please." });
            if (length64 > 104857600) // 100 MB
                return Json(new { success = false, reason = "File too large." });
            if (filename.Contains(Path.DirectorySeparatorChar))
                return Json(new { success = false, error = "Invalid filename." });
            if (!SongNameRegex.IsMatch(filename))
                return Json(new { success = false, error = "File name is not in \"Artist Name - Song Title.mp3\" format." });
            if (File.Exists(Path.Combine(Program.Configuration.MusicPath, filename)))
                return Json(new { success = false, error = "This song is already in our library." });
            var minutes = MusicRunner.MinutesUntilNextUpload(Request.RemoteEndPoint);
            if (minutes < 0)
                return Json(new { success = false, error = string.Format("You need to wait another {0} minutes before you can upload again.", minutes) });
            Uploads.Add(Request.RemoteEndPoint.Address.ToString(), new UploadInProgress
            {
                PartialData = string.Empty,
                Filename = filename,
                FinalLength = length64
            });
            return Json(new { success = true, partLength = UploadedPartLength });
        }

        public ActionResult UploadPart(string part)
        {
            if (!Uploads.ContainsKey(Request.RemoteEndPoint.Address.ToString()))
                return Json(new { success = false, reason = "You don't have any active uploads!" });
            var upload = Uploads[Request.RemoteEndPoint.Address.ToString()];
            if (part.Length > UploadedPartLength)
            {
                Uploads.Remove(Request.RemoteEndPoint.Address.ToString());
                return Json(new { success = false, reason = "Chunk too large." });
            }
            upload.PartialData += part;
            Console.WriteLine("Recieved {0} byte long chunk, awaiting {1} more bytes", part.Length, upload.FinalLength - upload.PartialData.Length);
            if (upload.PartialData.Length >= upload.FinalLength)
            {
                // Song is done uploading
                Console.WriteLine("Song uploaded: " + upload.Filename);
                Uploads.Remove(Request.RemoteEndPoint.Address.ToString());
                return Json(new { success = true, complete = true });
            }
            return Json(new { success = true, complete = false });
        }

        private static Dictionary<string, UploadInProgress> Uploads = new Dictionary<string, UploadInProgress>();
        private class UploadInProgress
        {
            public string PartialData { get; set; }
            public string Filename { get; set; }
            public long FinalLength { get; set; }
        }
    }
}