if (!String.prototype.endsWith) {
    Object.defineProperty(String.prototype, 'endsWith', {
        enumerable: false,
        configurable: false,
        writable: false,
        value: function (searchString, position) {
            position = position || this.length;
            position = position - searchString.length;
            return this.lastIndexOf(searchString) === position;
        }
    });
}

var seek = 0;
var updateDelay = 10000;
var adjustSync = true;

function PageViewModel () {
    var self = this;
    self.name = ko.observable('');
    self.stream = ko.observable('');
    self.download = ko.observable('');
    self.searchTerms = ko.observable('');
    self.page = ko.observable(1);
    self.pages = ko.observableArray([ 1 ]);
    self.totalPages = ko.observable(1);
    self.searchResults = ko.observableArray([]);
    self.hasSearched = ko.observable(false);
    self.queue = ko.observableArray([]);
    self.groupPlay = ko.observable(true);
    self.skipsRequested = ko.observable(0);
    self.skipsRequired = ko.observable(1);
    self.skipRequested = ko.observable(false);
    self.listeners = ko.observable(1);
    self.announcement = ko.observable('');
    
    self.getNowPlaying = function() {
        $.get('/nowplaying', function(data) {
            adjustSync = true;
            self.groupPlay(true);
            self.name(data.song.Name);
            self.download(data.song.Download);
            self.stream(data.song.Stream);
            self.queue(data.queue);
            seek = data.seek;
            changeStream(data.song.Stream, true);
            self.skipRequested(false);
            self.listeners(data.listeners);
        });
    };
    
    self.prepareNext = function() {
        if (!self.groupPlay())
            return;
        $.get('/nowplaying', function(data) {
            adjustSync = true;
            self.groupPlay(true);
            self.name(data.song.Name);
            self.download(data.song.Download);
            self.stream(data.song.Stream);
            self.queue(data.queue);
            seek = data.seek;
            changeStream(data.song.Stream, false);
            self.skipRequested(false);
            self.listeners(data.listeners);
        });
    }
    
    self.search = function() {
        self.page(1);
        self.doSearch();
    };
    
    self.doSearch = function() {
        $.post('/search', {
            query: self.searchTerms(),
            page: self.page()
        }, function(data) {
            self.searchResults(data.results);
            var pages = [];
            for (var i = 1; i <= data.totalPages; i++)
                pages.push(i);
            self.pages(pages);
            self.totalPages(data.totalPages);
            self.hasSearched(true);
        });
    };
    
    self.nextPage = function() {
        if (self.page() == self.totalPages())
            return;
        self.page(self.page() + 1);
        self.doSearch();
    };
    
    self.previousPage = function() {
        if (self.page() == 1)
            return;
        self.page(self.page() - 1);
        self.doSearch();
    };
    
    self.specificPage = function(data, event) {
        self.page(data);
        self.doSearch();
    };
    
    self.changeSong = function(data, event) {
        seek = 0;
        self.stream(data.Stream);
        self.name(data.Name);
        self.download(data.Download);
        self.groupPlay(false);
        adjustSync = false;
        changeStream(data.Stream, true);
    };
    
    self.requestSong = function(data, event) {
        $.post('/requestSong', {
            song: data.Name
        }, function(data) {
            $(event.target).attr('disabled', 'disabled');
            self.queue(data.queue);
            if (!data.canRequest) {
                $('#searchTable a.btn-info').attr('disabled', 'disabled');
            }
        });
    };
    
    self.refreshQueue = function() {
        $.get('/nowplaying', function(data) {
            self.queue(data.queue);
            self.skipsRequested(data.skipsRequested);
            self.skipsRequired(data.skipsRequired);
            self.announcement(data.announcement);
            self.listeners(data.listeners);
            if (self.groupPlay() && self.name() != data.song.Name) {
                // Song skipped
                adjustSync = true;
                self.name(data.song.Name);
                self.download(data.song.Download);
                self.stream(data.song.Stream);
                seek = data.seek;
                changeStream(data.song.Stream, true);
                self.skipRequested(false);
            }
        }).always(function() { setTimeout(self.refreshQueue, updateDelay); });
    };
    setTimeout(self.refreshQueue, updateDelay);
    
    self.requestSkip = function() {
        self.skipRequested(true);
        $.post('/requestSkip', { }, function(data) {
            if (data.success) {
                adjustSync = true;
                self.name(data.song.Name);
                self.download(data.song.Download);
                self.stream(data.song.Stream);
                self.queue(data.queue);
                seek = data.seek;
                self.skipRequested(false);
                changeStream(data.song.Stream, true);
            }
            self.skipsRequested(data.skipsRequested);
            self.skipsRequired(data.skipsRequired);
        });
    };
    
    self.togglePlayback = function() {
        var player = document.getElementById('player');
        if (player.paused) {
            player.play();
        } else {
            player.pause();
        }
        self.groupPlay(false);
    };
}

$(function() {
    var viewModel = new PageViewModel();

    // Register audio player events
    var player = document.getElementById('player');
    player.addEventListener('loadedmetadata', function() {
        if (adjustSync) {
            if (seek < 0) {
                setTimeout(function() {
                    player.currentTime = seek;
                    player.play();
                }, -seek * 1000);
            }
            else {
                player.currentTime = seek;
            }
        }
    });
    
    player.addEventListener('ended', function() {
        viewModel.prepareNext();
    });
    
    player.addEventListener('volumechange', function() {
        console.log(player.volume);
        createCookie('volume', player.volume);
    });
    
    // Load desired volume
    var desiredVolume = readCookie('volume');
    if (desiredVolume != null) {
        player.volume = desiredVolume;
    }
    
    // Register for keyboard shortcuts
    document.body.addEventListener('keypress', function(e) {
        if (document.activeElement.tagName == 'INPUT')
            return;
        console.log(e.keyCode);
        if (e.keyCode == 32) { // space
           viewModel.togglePlayback();
        }
        if (e.keyCode == 114) { // r
            viewModel.getNowPlaying();
        }
    });
    
    // Get now playing and bind everything up
    viewModel.getNowPlaying();
    ko.applyBindings(viewModel);
});

function changeStream(source, start) {
    player = document.getElementById('player');
    player.src = source;
    player.load();
    if (start)
        player.play();
}

function createCookie(name,value,days) {
    if (days) {
        var date = new Date();
        date.setTime(date.getTime()+(days*24*60*60*1000));
        var expires = "; expires="+date.toGMTString();
    }
    else var expires = "";
    document.cookie = name+"="+value+expires+"; path=/";
}

function readCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for(var i=0;i < ca.length;i++) {
        var c = ca[i];
        while (c.charAt(0)==' ') c = c.substring(1,c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length,c.length);
    }
    return null;
}

function eraseCookie(name) {
    createCookie(name,"",-1);
}