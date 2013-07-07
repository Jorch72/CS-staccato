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
    
    self.getNowPlaying = function() {
        $.get('/nowplaying', function(data) {
            self.name(data.Name);
            self.download(data.Download);
            self.stream(data.Stream);
            changeStream(data.Stream);
        });
    };
    
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
        self.stream(data.Stream);
        self.name(data.Name);
        self.download(data.Download);
        changeStream(data.Stream);
    };
}

$(function() {
    var viewModel = new PageViewModel();
    viewModel.getNowPlaying();
    ko.applyBindings(viewModel);
});

function changeStream(source) {
    player = document.getElementById('player');
    player.src = source;
    player.load();
    player.play();
}