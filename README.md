# Staccato

An online music player for sharing music with your friends.

![Staccato](http://i.imgur.com/WyTb5aS.png)

## Setting Up

If you'd like to run your own Staccato instance, it's pretty simple. You'll have to compile it yourself, though. Run these
commands:

    git clone --recursive git://github.com/SirCmpwn/staccato.git
    cd staccato
    xbuild
    cd staccato/bin/Debug/
    mono Staccato.exe

A configuration file will be generated in `config.json`, which you should fill with your configuration preferences. Then,
just run `mono Staccato.exe` to launch Staccato. You'll want to drop this on a server or something similar if you need to
access it remotely. Otherwise, just navigate to http://localhost to use it.

Currently only Linux is supported, but it'll probably work on Windows.

## Music Library

You should point Staccato to a folder where your music is stored. Staccato only supports mp3 files. The reccommended format
is `Artist - Title.mp3` for file naming. You can search these file names from the Staccato UI.

## WebSharp

Staccato runs on [WebSharp](https://github.com/SirCmpwn/WebSharp), which is an awesome library for straightforward website
creation, without IIS or Apache or ASP.NET or any of that junk. Browse the Staccato source code for an idea of how it can
be used.
