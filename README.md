# Staccato

An online jukebox for sharing music with your friends. It has the following fancy features:

* Jukebox play, where everyone listening hears the same song synced up with everyone else
  * More jukebox features allow users to request songs, vote to skip songs, and download the mp3 of the playing song
* Solo play, where you can ditch the group and listen to some songs alone
* Straightforward library management - just keep your mp3 files on the server
* IRC bot to help you operate a channel jukebox
  * Search for, request, or vote to skip any song without leaving the channel

![Staccato](http://i.imgur.com/WyTb5aS.png)

## Setting Up

Staccato has only been tested on Linux, but it's likely to work on Windows and Mac. You'll have to compile it yourself,
though. The ideal staccato setup is on a Linux server somewhere you can use to listen to music with your friends and such.
Make sure you have Mono and Git installed and do the following to compile:

    git clone --recursive git://github.com/SirCmpwn/staccato.git
    cd staccato
    xbuild /p:Configuration=RELEASE

The output will be produced in `staccato/bin/Release`. You need every file here in that folder to run staccato, but you can
put them wherever you want. You'll also need the `Static` and `Views` folders. Your best bet is to just copy the entire
Release folder to wherever you need it.

Once you've compiled it, run `mono staccato.exe` to generate an empty config file. It'll look like this:

    {
      "MusicPath": null,
      "LogRequests": false,
      "EndPoint": "0.0.0.0",
      "Port": 8888,
      "SkipThreshold": 0.51,
      "MaximumRequestsPerUser": 3,
      "RequestLimitResetMinutes": 60,
      "MinimumMinutesBetweenUploads": 60,
      "Domain": "localhost",
      "Irc": {
        "Enabled": false,
        "Server": null,
        "Nick": null,
        "User": null,
        "Password": null,
        "Channels": [],
        "AnnounceNowPlaying": false,
        "TrustedMasks": []
      }
    }

Each of these settings is described here:

* **MusicPath**: Set this to the directory your mp3 files are in. I use `/home/sircmpwn/Music/`.
* **LogRequests**: Debugging feature
* **EndPoint**: Unless you know what this means, leave it as `0.0.0.0`
* **Port**: Unless you know what this means, use port 80. Read the note below if you use port 80.
* **SkipThreshold**: The amount of votes required to skip a song. `0` allows a single vote to skip, `1` requires all users
  to vote before it'll skip.
* **MaximumRequestsPerUser** and **RequestLimitResetMinutes**: Limits how often users can request a song. The default shown
  here allows for up to 3 requests every 60 minutes.
* **Domain**: If you put your site on a domain, change this to that domain. You could also use your public IP. This just
  affects links that are shown in IRC.
* **Irc**: Disabled by default. See "IRC" below if you want to set up the IRC bot.

**Note**: On Linux and Mac, you have to be root to bind to ports less than 1024. *Do not run staccato as root because it
would be incredibly stupid.* Instead, you have two choices:

1. Bind to a high port and use iptables to forward traffic. The command you'd use looks something like
   `iptables -t nat -A PREROUTING -p tcp --dport 80 -j REDIRECT --to-port 8080`
2. Add the net bind capability to the staccato executable. The command looks something like
   `setcap 'cap_net_bind_service=+ep' staccato.exe`. You might have to give that permission to `/usr/bin/mono` instead.

With either solution, run the commands as root.

### IRC

Staccato includes an IRC bot that you can use to announce songs in a channel, recieve requests, etc. You can fill out the
IRC section of the configuration file to use it. An example:

     "Irc": {
       "Enabled": true,
       "Server": "irc.freenode.net",
       "Nick": "staccato",
       "User": "staccato",
       "Password": null,
       "Channels": [
         "#yourchannel"
       ],
       "AnnounceNowPlaying": true,
       "TrustedMasks": [
         "*!*@your.host.name"
       ]
     }

Make sure you set `Enabled` to true, or it won't work at all. Say `~help` when the bot enters the channel to get a link
to read about all the commands it has. There's one command it doesn't document, though - `~announce <text>`. This command
is done via PM, and only works for trusted masks. It announces `<text>` both in the channel and on the site.

If you have any questions about setting up staccato, please join `#staccato` on irc.freenode.net.

## Contributing

Pull requests are welcome on this software. Feel free to submit them, and make sure you adhere to the coding styles we're
already using.

## Technical Information

Staccato runs on [WebSharp](https://github.com/SirCmpwn/WebSharp), which is a .NET web software framework. It's pretty
nice, and you can get a good idea of how to use it by browsing the staccato source code. For the IRC bot, it uses
[ChatSharp](https://github.com/SirCmpwn/ChatSharp), which is another cool library I've made to support the IRC protocol.
Additionally, it uses [taglib-sharp](https://github.com/mono/taglib-sharp) to pull information out of mp3 files.
