using System;

namespace staccato
{
    public class Configuration
    {
        public class IrcConfiguration
        {
            public IrcConfiguration()
            {
                Enabled = AnnounceNowPlaying = false;
                Channels = new string[0];
                TrustedMasks = new string[0];
            }

            public bool Enabled { get; set; }
            public string Server { get; set; }
            public string Nick { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string[] Channels { get; set; }
            public bool AnnounceNowPlaying { get; set; }
            public string[] TrustedMasks { get; set; }
        }

        public Configuration()
        {
            LogRequests = false;
            EndPoint = "0.0.0.0";
            Port = 8888;
            SkipThreshold = 0.51;
            MaximumRequestsPerUser = 3;
            RequestLimitResetMinutes = 60;
            MinimumMinutesBetweenUploads = 60;
            Domain = "localhost";
            Irc = new IrcConfiguration();
        }

        public string MusicPath { get; set; }
        public bool LogRequests { get; set; }
        public string EndPoint { get; set; }
        public int Port { get; set; }
        public double SkipThreshold { get; set; }
        public int MaximumRequestsPerUser { get; set; }
        public int RequestLimitResetMinutes { get; set; }
        public int MinimumMinutesBetweenUploads { get; set; }
        public string Domain { get; set; }
        public IrcConfiguration Irc { get; set; }
    }
}

