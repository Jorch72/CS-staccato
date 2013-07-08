using System;
using WebSharp;
using WebSharp.Routing;
using System.Net;
using WebSharp.Handlers;
using System.IO;
using WebSharp.MVC;
using Newtonsoft.Json;

namespace staccato
{
    public class Program
    {
        public static Configuration Configuration;

        public static void Main(string[] args)
        {
            Configuration = new Configuration();
            if (!File.Exists("config.json"))
            {
                File.WriteAllText("config.json", JsonConvert.SerializeObject(Configuration, Formatting.Indented));
                Console.WriteLine("Empty config.json file created. Populate it and restart.");
                return;
            }
            JsonConvert.PopulateObject(File.ReadAllText("config.json"), Configuration);
            File.WriteAllText("config.json", JsonConvert.SerializeObject(Configuration, Formatting.Indented));

            var httpd = new HttpServer();
            var router = new HttpRouter();
            httpd.LogRequests = Configuration.LogRequests;
            httpd.Request = router.Route;

            var staticContent = new StaticContentHandler(Configuration.MusicPath);
            router.AddRoute(new StaticContentRoute(staticContent));
            var staticResources = new StaticContentHandler(Path.Combine(".", "Static"));
            router.AddRoute(new StaticContentRoute(staticResources));

            var mvc = new MvcRouter();
            mvc.RegisterController(new IndexController());
            mvc.AddRoute("Default", "{action}", new { controller = "Index", action = "Index" });
            router.AddRoute(mvc);

            router.AddRoute(new RegexRoute("/download/(?<path>.*)", (context, request, response) =>
            {
                response.AddHeader("Content-Disposition", string.Format("attachment; filename=\"{0}\"", Path.GetFileName(context["path"])));
                staticContent.Serve(context["path"], request, response);
            }));

            MusicRunner.Start();
            httpd.Start(new IPEndPoint(IPAddress.Parse(Configuration.EndPoint), Configuration.Port));

            Console.WriteLine("Press 'q' to exit.");
            ConsoleKeyInfo cki;
            do cki = Console.ReadKey(true);
            while (cki.KeyChar != 'q');
        }
    }
}
