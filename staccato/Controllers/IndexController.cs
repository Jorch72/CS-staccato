using System;
using System.Linq;
using WebSharp.MVC;
using System.IO;

namespace staccato
{
    public class IndexController : Controller
    {
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
                listeners = MusicRunner.Listeners
            });
        }

        public ActionResult Search(string query, int page)
        {
            query = query.Replace('+', ' ').ToUpper(); // TODO: WebSharp should probably handle this
            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3")
                .OrderBy(Path.GetFileNameWithoutExtension)
                .Where(f => Path.GetFileNameWithoutExtension(f).ToUpper().Contains(query)).ToArray();

            return Json(new {
                results = files
                    .Skip(10 * (page - 1)).Take(10).Select(f => 
                    new 
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        Download = "/download/" + Path.GetFileName(f),
                        Stream = Path.GetFileName(f),
                        CanRequest = !MusicRunner.MasterQueue.Any(s => s.Name.Equals(Path.GetFileNameWithoutExtension(f)))
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
            MusicRunner.QueueUserSong(song);
            return Json(new { success = true, queue = MusicRunner.MasterQueue.Take(8) });
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
    }
}