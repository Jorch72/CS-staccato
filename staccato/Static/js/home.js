var seek = 0;
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
            setTimeout(self.refreshQueue, 5000);
        });
    };
    setTimeout(self.refreshQueue, 5000);
    
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
    document.body.addEventListener('keypress', function(e) {
        if (document.activeElement.tagName == 'INPUT')
            return;
        if (e.keyCode == 32) { // space
           viewModel.togglePlayback();
        }
    });

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