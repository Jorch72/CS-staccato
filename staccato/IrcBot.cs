using System;
using System.Linq;
using ChatSharp;
using ChatSharp.Events;
using System.IO;

namespace staccato
{
    public class IrcBot
    {
        public IrcClient Client { get; set; }
        public string[] PreviousSearchResults { get; set; }
        private bool ShowNowPlaying { get; set; }

        public IrcBot()
        {
            Client = new IrcClient(Program.Configuration.Irc.Server, new IrcUser(
                Program.Configuration.Irc.Nick, Program.Configuration.Irc.User, Program.Configuration.Irc.Password));
            ShowNowPlaying = true;
        }

        public void Start()
        {
            Client.ConnectionComplete += (sender, e) => Array.ForEach(Program.Configuration.Irc.Channels, Client.JoinChannel);
            Client.ChannelMessageRecieved += HandleChannelMessageRecieved;
            Client.PrivateMessageRecieved += HandlePrivateMessageRecieved;
            Client.ConnectAsync();
        }

        public void AnnounceSong(Song nowPlaying)
        {
            if (!Program.Configuration.Irc.AnnounceNowPlaying || !ShowNowPlaying)
                return;
            var message = string.Format("Now Playing: {0} ({1}:{2:00}) - {3}",
                nowPlaying.Name,
                nowPlaying.Duration.Minutes,
                nowPlaying.Duration.Seconds,
                GetSiteUrl());
            foreach (var channel in Client.Channels)
                channel.SendMessage(message);
            ShowNowPlaying = false;
        }

        public void AnnounceRequest(Song requestedSong)
        {
            if (!Program.Configuration.Irc.AnnounceNowPlaying)
                return;
            var message = string.Format("Requested: {0} ({1}:{2:00}) - {3}",
                requestedSong.Name,
                requestedSong.Duration.Minutes,
                requestedSong.Duration.Seconds,
                GetSiteUrl());
            foreach (var channel in Client.Channels)
                channel.SendMessage(message);
        }

        public void Announce(string announcement)
        {
            foreach (var channel in Client.Channels)
                channel.SendMessage("Notice: " + announcement);
        }

        public void HandlePrivateMessageRecieved(object sender, PrivateMessageEventArgs e)
        {
            try
            {
                if (Program.Configuration.Irc.TrustedMasks.Any(e.PrivateMessage.User.Match))
                {
                    if (e.PrivateMessage.Message.StartsWith("~"))
                    {
                        // Handle command
                        var command = e.PrivateMessage.Message.Substring(1);
                        var parameters = string.Empty;
                        if (command.Contains(" "))
                        {
                            parameters = command.Substring(command.IndexOf(' ') + 1);
                            command = command.Remove(command.IndexOf(' '));
                        }
                        switch (command)
                        {
                            case "announce":
                                if (string.IsNullOrEmpty(parameters))
                                    break;
                                Program.Announce(parameters);
                                break;
                        }
                    }
                }
            }
            catch { }
        }

        public void HandleChannelMessageRecieved(object sender, PrivateMessageEventArgs e)
        {
            ShowNowPlaying = true; // We only show now playing if we weren't the last one to speak. This reduces clutter in the channel.
            try
            {
                if (e.PrivateMessage.Message.StartsWith("~"))
                {
                    // Handle command
                    var command = e.PrivateMessage.Message.Substring(1);
                    var parameters = string.Empty;
                    if (command.Contains(" "))
                    {
                        parameters = command.Substring(command.IndexOf(' ') + 1);
                        command = command.Remove(command.IndexOf(' '));
                    }
                    switch (command)
                    {
                        case "help":
                            if (!string.IsNullOrEmpty(parameters))
                                break;
                            Client.SendMessage("Help: " + GetSiteUrl() + "/ircHelp", e.PrivateMessage.Source);
                            break;
                        case "np":
                            if (!string.IsNullOrEmpty(parameters))
                                break;
                            Client.SendMessage(string.Format("Now Playing: \"{0} ({1}:{2:00})\" for {3} listener{4} - {5}",
                                MusicRunner.NowPlaying.Name, 
                                MusicRunner.NowPlaying.Duration.Minutes,
                                MusicRunner.NowPlaying.Duration.Seconds,
                                MusicRunner.Listeners,
                                MusicRunner.Listeners != 1 ? "s" : "",
                                GetSiteUrl()),
                                e.PrivateMessage.Source);
                            break;
                        case "q":
                            if (!string.IsNullOrEmpty(parameters))
                                break;
                            Client.SendMessage("Queue: " + string.Join("; ", MusicRunner.MasterQueue.Take(3).Select(s => s.Name)), e.PrivateMessage.Source);
                            break;
                        case "r":
                        case "sr":
                            if (string.IsNullOrEmpty(parameters))
                                break;
                            int index;
                            if (int.TryParse(parameters, out index))
                            {
                                index--;
                                if (index >= PreviousSearchResults.Length)
                                {
                                    Client.SendMessage("Song not found.", e.PrivateMessage.Source);
                                    break;
                                }
                                if (MusicRunner.MasterQueue.Any(s => s.Name.Equals(Path.GetFileNameWithoutExtension(PreviousSearchResults[index]))))
                                    Client.SendMessage("That song is already in the queue.", e.PrivateMessage.Source);
                                else if (!MusicRunner.CanUserRequest(e.PrivateMessage.User.Hostname))
                                    Client.SendMessage("You need to wait a while before you can request again.", e.PrivateMessage.Source);
                                else
                                {
                                    Song song;
                                    MusicRunner.QueueUserSong(Path.Combine(Program.Configuration.MusicPath, PreviousSearchResults[index]),
                                        e.PrivateMessage.User.Hostname, out song);
                                    Client.SendMessage(Path.GetFileNameWithoutExtension(PreviousSearchResults[index]) + 
                                        " has been added to the queue.", e.PrivateMessage.Source);
                                }
                            }
                            else
                            {
                                var file = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3")
                                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).ToUpper().Contains(parameters.ToUpper()));
                                if (file == null)
                                    Client.SendMessage("Song not found.", e.PrivateMessage.Source);
                                else
                                {
                                    if (MusicRunner.MasterQueue.Any(s => s.Name.Equals(Path.GetFileNameWithoutExtension(file))))
                                        Client.SendMessage("That song is already in the queue.", e.PrivateMessage.Source);
                                    else if (!MusicRunner.CanUserRequest(e.PrivateMessage.User.Hostname))
                                        Client.SendMessage("You need to wait a while before you can request again.", e.PrivateMessage.Source);
                                    else
                                    {
                                        Song song;
                                        MusicRunner.QueueUserSong(Path.Combine(Program.Configuration.MusicPath, file),
                                            e.PrivateMessage.User.Hostname, out song);
                                        Client.SendMessage(Path.GetFileNameWithoutExtension(file) + 
                                            " has been added to the queue.", e.PrivateMessage.Source);
                                    }
                                }
                            }
                            break;
                        case "s":
                            if (string.IsNullOrEmpty(parameters))
                                break;
                            var files = Directory.GetFiles(Program.Configuration.MusicPath, "*.mp3")
                                .OrderBy(Path.GetFileNameWithoutExtension)
                                .Where(f => Path.GetFileNameWithoutExtension(f).ToUpper().Contains(parameters.ToUpper())).ToArray();
                            string result;
                            if (!files.Any())
                                result = "No results.";
                            else
                            {
                                result = string.Format("{0} result{1}: ", files.Length, files.Length != 1 ? "s" : "");
                                for (int i = 0; i < files.Length && i < 3; i++)
                                    result += string.Format("[{0}] {1}; ", i + 1, Path.GetFileNameWithoutExtension(files[i]));
                                result = result.Remove(result.Length - 2);
                            }
                            PreviousSearchResults = files.Take(3).ToArray();
                            Client.SendMessage(result, e.PrivateMessage.Source);
                            break;
                        case "skip":
                            if (MusicRunner.SkipRequests.Contains(e.PrivateMessage.User.Hostname))
                            {
                                Client.SendMessage(string.Format("You've already voted to skip this song. You need {0} more vote{1} to skip the song.",
                                    MusicRunner.SkipRequestsRequired - MusicRunner.SkipRequestsIssued,
                                    MusicRunner.SkipRequestsRequired - MusicRunner.SkipRequestsIssued != 1 ? "s" : ""), e.PrivateMessage.Source);
                                break;
                            }
                            if (((MusicRunner.StartTime + MusicRunner.NowPlaying.Duration) - DateTime.Now).TotalSeconds < 10)
                            {
                                Client.SendMessage("The song is nearly over. Hang tight.", e.PrivateMessage.Source);
                                break;
                            }
                            if (!MusicRunner.RequestSkip(e.PrivateMessage.User.Hostname))
                                Client.SendMessage(string.Format("Your vote has been noted. You need {0} more vote{1} to skip the song.",
                                    MusicRunner.SkipRequestsRequired - MusicRunner.SkipRequestsIssued,
                                    MusicRunner.SkipRequestsRequired - MusicRunner.SkipRequestsIssued != 1 ? "s" : ""), e.PrivateMessage.Source);
                            break;
                    }
                }
            }
            catch
            {
                // This is just here in case something goes wrong
                // We're dealing with user input here, after all
            }
        }

        private string GetSiteUrl()
        {
            if (Program.Configuration.Port != 80)
                return string.Format("http://{0}:{1}", Program.Configuration.Domain, Program.Configuration.Port);
            return "http://" + Program.Configuration.Domain;
        }
    }
}

