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
var fileData = null;
var partLength = null;

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
    self.uploadError = ko.observable('');
    self.fileName = ko.observable('');
    self.uploadReady = ko.observable(false);
    self.uploading = ko.observable(false);
    self.uploadSuccess = ko.observable(false);
    
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
            setTimeout(self.refreshQueue, updateDelay);
        });
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
    
    self.uploadSong = function() {
        if (!self.uploadReady())
            return;
        self.uploading(true);
        $.post('/canUpload', {
            filename: self.fileName()
        }, function(data) {
            if (!data.success) {
                self.uploadReady(false);
                self.uploading(false);
                self.fileName('');
                fileData = null;
                self.uploadError(data.reason);
            } else {
                // Upload song
                fileData = fileData.substring(fileData.indexOf(',') + 1); // Seek to start of base64 data
                $.post('/startUpload', {
                    filename: self.fileName(),
                    length64: fileData.length
                }, function(r) {
                    if (!r.success) {
                        self.uploadReady(false);
                        self.uploading(false);
                        self.fileName('');
                        fileData = null;
                        self.uploadError(r.reason);
                    } else {
                        partLength = r.partLength;
                        var chunk = fileData.substring(0, partLength);
                        fileData = fileData.substring(partLength);
                        $.post('/uploadPart', {
                            part: chunk
                        }, self.recursiveUpload);
                    }
                });
            }
        });
    };
    
    self.recursiveUpload = function(data) {
        if (!data.success) {
            self.uploadReady(false);
            self.uploading(false);
            self.fileName('');
            fileData = null;
            self.uploadError(data.reason);
            return;
        }
        if (data.complete) {
            self.uploadReady(false);
            self.uploading(false);
            self.fileName('');
            self.uploadSuccess('Upload complete!');
            fileData = null;
            return;
        }
        var chunk = fileData.substring(0, partLength);
        fileData = fileData.substring(partLength);
        console.log('Sending ' + chunk.length + ' byte chunk, ' + fileData.length + ' bytes remain.');
        $.post('/uploadPart', {
            part: chunk
        }, self.recursiveUpload);
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
        if (!viewModel.groupPlay() && e.keyCode == 114) { // r
            viewModel.getNowPlaying();
        }
    });
    
    // File upload event handlers
    var upload = document.getElementById('upload-target');
    upload.addEventListener('dragenter', function(e) {
        noopHandler(e);
        $(e.target).addClass('file-hovering');
    }, false);
    upload.addEventListener('dragexit', function(e) {
        noopHandler(e);
        $(e.target).removeClass('file-hovering');
    }, false);
    upload.addEventListener('dragover', noopHandler, false);
    upload.addEventListener('drop', function(e) {
        noopHandler(e);
        $(e.target).removeClass('file-hovering');
        if (viewModel.uploading())
            return;
        viewModel.uploadSuccess(false);
        if (event.dataTransfer.files.length != 1) {
            viewModel.uploadError('Please upload exactly one mp3 file.');
        }
        else if (!event.dataTransfer.files[0].name.endsWith('.mp3')) {
            viewModel.uploadError('Please upload only mp3 files.');
        } else {
            viewModel.uploadError('');
            viewModel.fileName(event.dataTransfer.files[0].name);
            var reader = new FileReader();
            reader.onloadend = function(result) {
                viewModel.uploadReady(true);
                fileData = result.target.result;
            };
            reader.readAsDataURL(event.dataTransfer.files[0]);
        }
    }, false);
    
    // Get now playing and bind everything up
    viewModel.getNowPlaying();
    ko.applyBindings(viewModel);
});

function noopHandler(e) {
    e.stopPropagation();
    e.preventDefault();
}

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