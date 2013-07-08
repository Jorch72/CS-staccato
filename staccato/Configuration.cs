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
            SkipThreshold = 0.51;
        }

        public string MusicPath { get; set; }
        public bool LogRequests { get; set; }
        public string EndPoint { get; set; }
        public int Port { get; set; }
        public double SkipThreshold { get; set; }
    }
}

