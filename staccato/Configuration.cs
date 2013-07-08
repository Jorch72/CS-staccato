using System;

namespace staccato
{
    public class Configuration
    {
        public Configuration()
        {
            LogRequests = false;
            EndPoint = "0.0.0.0";
            Port = 8888;
        }

        public string MusicPath { get; set; }
        public bool LogRequests { get; set; }
        public string EndPoint { get; set; }
        public int Port { get; set; }
    }
}

