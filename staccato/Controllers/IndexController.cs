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
            return View();
        }

        public ActionResult NowPlaying()
        {
            return Json(MusicRunner.NowPlaying);
        }

        public ActionResult Search(string query, int page)
        {
            query = query.Replace('+', ' ').ToUpper(); // TODO: WebSharp should probably handle this
            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3", SearchOption.AllDirectories)
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
                    }).ToArray(),
                totalPages = files.Count() / 10
            });
        }

        public ActionResult Playlist()
        {
            return null;
        }

        public ActionResult Queue(string song)
        {
            return null;
        }
    }
}