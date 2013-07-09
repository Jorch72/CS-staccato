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

            Console.WriteLine("Type 'quit' to exit, or 'help' for help.");
            string command = null;
            while (command != "quit")
            {
                command = Console.ReadLine();
                HandleCommand(command);
            }
        }

        public static void HandleCommand(string command)
        {
            var name = command;
            var parameters = string.Empty;
            if (name.Contains(" "))
            {
                parameters = name.Substring(name.IndexOf(' ') + 1);
                name = name.Remove(name.IndexOf(' '));
            }
            switch (name.ToUpper())
            {
                case "ANNOUNCE":
                    if (parameters.Length == 0)
                        Console.WriteLine("Use 'announce <text goes here>' to make an announcement.");
                    else
                        MusicRunner.Announcement = parameters;
                    break;
                case "UNANNOUNCE":
                    MusicRunner.Announcement = null;
                    break;
                case "QUIT":
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }
}
