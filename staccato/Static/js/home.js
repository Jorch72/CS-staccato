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
            $(this).attr('disabled', 'disabled');
            self.queue(data.queue);
        });
    };
    
    self.refreshQueue = function() {
        $.get('/nowplaying', function(data) {
            self.queue(data.queue);
            setTimeout(self.refreshQueue, 15000);
        });
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

    viewModel.getNowPlaying();
    setTimeout(viewModel.refreshQueue, 15000);
    ko.applyBindings(viewModel);
});

function changeStream(source, start) {
    player = document.getElementById('player');
    player.src = source;
    player.load();
    if (start)
        player.play();
}