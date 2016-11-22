var songListTable;
var emailPath = "./API/sendEmail.ashx";
var songPath = "./API/songUsage.ashx?location={location}";
var randomPath = "./API/pickRandom.ashx?location={location}";
var historyPath = './API/SongHistory.ashx?location={location}&songID={songID}';
var venuesPath = "./API/venues.ashx";
var venues = [];
var songs = [];
var randomSongs = [];
var plans = [];
var currentLocation = 'richland';
var activeTooltip = false;

//var tableRowTemplate = '<ul class="data-row"><li class="title" data-songid="{{songID}}">{{title}}</li><li class="artist" title="{{artist}}"><span class="artist-name">{{artist}}</span></li><li class="lastUsed">{{lastUsedFormatted}} ({{weeksSince}})</li><li class="weeksSince"><span class="wkSince">{{weeksSince}}</span></li><li class="useCount" data-sincedate="{{sincedate}}"><span class="label">{{useCount}}</span> <div class="meter"><span  style="width: {{usePercent}}%"></span></li><li class="voting"></li></ul>';
var tableRowTemplate = '<ul class="data-row"><li class="title" data-songid="{{songID}}"><span class="constraint-width tooltip" title="Loading...">{{title}} <span class="ccliNumber">{{ccliNumberFormatted}}</span></span><span class="tooltipster-icon mobile-only manual-tooltip"></span><span class="song-ctrl ctrl1"></span><span class="song-ctrl ctrl2"></span></li><li class="artist" title="{{artist}}"><span class="constraint-width overflow-ellipsis artist-name">{{artist}}</span></li><li class="lastUsed">{{lastUsedFormatted}} ({{weeksSince}})</li><li class="preferredSlot">{{preferredSlot}}<li class="useCount" data-sincedate="{{sincedate}}"><span class="label">{{useCount}}</span> <div class="meter"><span  style="width: {{usePercent}}%"></span></li><li class="voting"></li></ul>';
var tipRowTemplate = '<li class="tip-row"><span class="tip-date">{{ServiceDate}}</span><span class="tip-name">{{LeaderName}}</span><span class="tip-key">[{{Key}}]</span></li>';

var folderListTemplate = '<ul class="folders treeList {{isChild}}">{{{children}}}</ul>';
var folderTemplate = '<li class="folder treeElement {{hasChildren}}" data-venueid="{{ID}}" data-parentid="{{ParentID}}">{{Name}}</span>{{{subFolders}}}{{{ChildVenues}}}</li>';
var venueTemplate = '<li class="serviceVenue treeElement checkable" data-venueid="{{ID}}" data-parentid="{{ParentID}}">{{Name}}</span></li>';

$(document).ready(function(e){

	/*
	The list sorter won't reindex the list when I swap out the data.  The next release is supposed to include a reindex function, then we can do this.
	uncomment out the line below and change the date input from hidden to text type
	$('input.datepicker').Zebra_DatePicker({'default_position': 'below', 'first_day_of_week': 0, 'format':'n/j/Y', 'onSelect':getSongList});
	*/
	$('input.datepicker').Zebra_DatePicker({'default_position': 'below', 'first_day_of_week': 0, 'format':'n/j/Y', 'onSelect':getSongList});

	$('.location-select').val(getLocation());
	if ($('.js-nav-item[data-nav="' + window.location.hash.replace('#', '') + '"]').length > 0) {
		selectTab(window.location.hash.replace('#', ''));
	} else {
		getSongList();
	}


	$(".drop-list").children().clone().appendTo('#data-titles');
	$(".drop-list").find('.sort').addClass('sorter').removeClass('sort');

	$('.nav-icon').click(function(e) {
		e.preventDefault();
		$('.nav').toggleClass('open');
		$('body').toggleClass('nav-open');
	})

	$('.drop-list').on('click', '.sorter', function(e){
		var $sortVersion = $('.sort[data-sort="' + $(this).data('sort') + '"]');
		$('#songList .sort').find('a').removeClass('sort-up').removeClass('sort-down');

		$('.drop-list .sorter').not(this).removeClass('asc').removeClass('desc');
		var sortOrder = 'asc';
		if ($(this).hasClass('asc')) {
			sortOrder = 'desc';
			$(this).addClass('desc').removeClass('asc');
			$sortVersion.find('a').addClass('sort-up').removeClass('sort-down');
		} else {
			$(this).addClass('asc').removeClass('desc');
			$sortVersion.find('a').removeClass('sort-up').addClass('sort-down');
		}
		songListTable.sort($(this).data('sort'), { order: sortOrder }); 
	})

	$('#venuesList').on('click', '.hasChildren', function(e){
		e.stopPropagation();
		$(this).toggleClass('open');
	});


	$('#venuesList').on('click', '.checkable', function(e){
		e.stopPropagation();
		$(this).toggleClass('checked');
	});


	$('.location-select').change(function(e) {
		if (currentLocation === $(this).val()) {
			return;
		}

		setLocation($(this).val());
		/*randomSongs = [];
		songs = [];

		getRandomSongList();
		getSongList();*/
		window.location.reload();
	})

	$('.mobile-tooltip .button-close').click(function(e) {
		e.preventDefault();
		$(this).closest('.mobile-tooltip').removeClass('active');
		if (activeTooltip) {
			$(activeTooltip).tooltipster('hide');
			activeTooltip = false;
		}
	});


	$('#songList').on('click', '.sort', function(e){
		$('.sorter').removeClass('active');
		var $sorterVersion = $('.sorter[data-sort="' + $(this).data('sort') + '"]');
		$('#songList .sort').not(this).find('a').removeClass('sort-up').removeClass('sort-down');
		if ($(this).find('a').hasClass('sort-up')) {
			$(this).find('a').removeClass('sort-up').addClass('sort-down');
			$sorterVersion.addClass('asc').removeClass('desc');
		} else {
			$(this).find('a').addClass('sort-up').removeClass('sort-down');
			$sorterVersion.addClass('desc').removeClass('asc');
		} 
		$sorterVersion.addClass('active');

	})

	$('#songList').on('click', '.manual-tooltip', function(e) {
		$(this).siblings('.tooltip').first().tooltipster('show');
		//$('.mobile-tooltip-content').html($(this).siblings('.tooltip').first().tooltipster('content'));
		//$('.mobile-tooltip').addClass('active');
	});

	$('#show-feedback').click(function(e) {
		e.preventDefault();
		showModal();
	})

	$('#submit-feedback').click(function(e) {
		e.preventDefault();
		sendFeedback();
	})

	$('.modal-close').click(function(e) {
		e.preventDefault();
		closeModal();
	});

	$('#rerandomize').click(function(e) {
		e.preventDefault();
		
		getRandomSongList();
	});

	$('.js-nav-item a').click(function(e) {
		//e.preventDefault();
		if ($(this).closest('.js-nav-item').hasClass('selected')) {return;}
		/*$(this).closest('.nav').find('.js-nav-item').removeClass('selected');
		$(this).closest('.js-nav-item').addClass('selected');

		$('.content-container').hide();
		$('#' + $(this).closest('.js-nav-item').data('nav')).show();

		$('.nav').removeClass('open');
		$('body').removeClass('nav-open');


		if (randomSongs.length == 0) {
			getRandomSongList();
		}

		if (songs.length == 0) {
			getSongList();
		}*/

		selectTab($(this).closest('.js-nav-item').data('nav'));
	})
})

function selectTab(tabName) {

	$('.js-nav-item').removeClass('selected');
	$('.js-nav-item[data-nav="' + tabName + '"]').addClass('selected');

	$('.content-container').hide();
	$('#tab-' + tabName).show();

	$('.nav').removeClass('open');
	$('body').removeClass('nav-open');


	if (randomSongs.length == 0) {
		getRandomSongList();
	}

	if (songs.length == 0) {
		getSongList();
	}

	if (venues.length == 0) {
		getVenues();
	}
	window.location.hash = tabName;
}


function setLocation(locationVal) {
	currentLocation = locationVal ? locationVal : 'richland';
	if (currentLocation == '') {
		currentLocation = 'richland';
	}
	currentLocation = currentLocation.replace('#', '');
	SetCookie('bwpLocation', currentLocation, 365);
}

function getLocation() {
	currentLocation = GetCookie('bwpLocation');
	if (currentLocation === '' || currentLocation === null || currentLocation === undefined) {
		currentLocation = 'richland';
	}
	//window.location.hash = currentLocation;
	return currentLocation;
}

function getVenues() {
	var instance = this;
	$('#venuesList').html('<p class="loading"><span>Loading venues </span><span class="loader-image"></span></p>')


	var queryString = venuesPath;
	queryString += '?nocache=' + (new Date().getTime());

	$.getJSON(queryString, function(data) {
		venues = data; 
		processVenues();

	});
}

function getSongList() {
	var instance = this;
	$('#songBody').html('<p class="loading"><span>Loading songs </span><span class="loader-image"></span></p>')

	var thisSongPath = songPath.replace('{location}', getLocation());

	var queryString = '&';

	if ($('#startdatepicker').val() != '') {
		queryString += 'startdate=' + $('#startdatepicker').val() + '&';
	}
	queryString += 'nocache=' + (new Date().getTime());

	$.getJSON(thisSongPath + queryString, function(data) {
		songs = data; 
		processData();

	});
}


function getRandomSongList() {
	if ($('.button--randomize').hasClass('processing')) {return;}
	var now = Date.now();
	var minWait = 0;


	$('.button--randomize').addClass('processing');

	var instance = this;
		var thisRandomPath = randomPath.replace('{location}', getLocation());

	var stopCaching = '&nocache=' + (new Date().getTime());

	//$('#randomBody').empty();
	//$('#randomBody').html('<p class="loading"><span>Selecting songs </span><span class="loader-image"></span></p>')
	$.getJSON(thisRandomPath + stopCaching, function(data) {
		randomSongs = data; 
		if ((Date.now() - now) < minWait) {
			setTimeout(processRandomData, (minWait - (Date.now() - now)));
		} else {
			processRandomData();	
		}

	});
}

function processData() {
	var now = Date.now();
	var maxUse = 1;
	songs.forEach(function(song) {
		song.lastUsed = new Date(song.lastUsed);
		song.lastUsedFormatted = (song.lastUsed.getMonth() + 1) + "/" + song.lastUsed.getDate() + "/" + song.lastUsed.getFullYear();
		var startDate = new Date(song.startDate);
		var sincedate = (startDate.getMonth() + 1) + "/" + startDate.getDate() + "/" + startDate.getFullYear();
		song.ccliNumberFormatted = (song.ccliNumber !== '') ? '(' + song.ccliNumber + ')' : '';
		$('.dataStartDate').text(sincedate);
		$('#startdatepicker').val(sincedate);
		$('#lastUpdatedDate').text(song.lastUpdated);
		$('.footer_last-updated').show();
		
		song.sincedate = sincedate;
		if (song.dates.length > 0) {
			maxUse = Math.max(maxUse, song.useCount);
		}
	});
	$('#songBody').empty();
	songs.forEach(function(song) {
		song.usePercent = Math.ceil((song.useCount/maxUse) * 100);
		$('#songBody').append(Mustache.render(tableRowTemplate, song));
	});


	var options = {
		valueNames: [ 'title', 'artist', 'lastUsed', 'weeksSince', 'preferredSlot', 'useCount']
	};
	songListTable = null;
	var thissongListTable = new List('tab-songs', options);
	songListTable = thissongListTable;
	$( ".wkSince:contains('999')" ).css( "visibility", "hidden" );
	var $lastUsedSort = $('.sort[data-sort="lastUsed"]');
	$lastUsedSort.click();
	$lastUsedSort.click();
	$('.tooltip').tooltipster({
		theme: 'bwp-theme',
		position: 'right',
		contentAsHTML: true,
		onlyOne: true,
		animation: 'fade',
		speed: 200,
		delay: 200,
		icon: '',
		iconDesktop: true,
		iconTouch: false,
		updateAnimation: false,
		touchDevices: true,
		content: 'Loading...',

		functionBefore: function(origin, continueTooltip) {

			// we'll make this function asynchronous and allow the tooltip to go ahead and show the loading notification while fetching our data
			continueTooltip();
			activeTooltip = origin;
			
			// next, we want to check if our data has already been cached
			if (origin.data('ajax') !== 'cached') {
				var tipURL = historyPath.replace('{location}', getLocation()).replace('{songID}', $(origin).closest('.title').data('songid'));

				$.getJSON(tipURL, function(data) {
					var tipContent = '<ul>';
					data.forEach(function(tip) {
						if (tip.LeaderName == '') {tip.LeaderName = 'Unavailable';}
						tipContent += Mustache.render(tipRowTemplate, tip);
					});
					tipContent += '</ul>';
					var $tmp = $(tipContent);
					$tmp.find('.tip-key').each(function() {
						if ($(this).text() === '[]') {
							$(this).addClass('hide');
						}
					});
					tipContent = $tmp[0].outerHTML;
					origin.tooltipster('content', tipContent).data('ajax', 'cached');
					$('.mobile-tooltip-content').html(tipContent);
					$('.mobile-tooltip').addClass('active');
				});

			} else {
				$('.mobile-tooltip-content').html($(activeTooltip).tooltipster('content'));
				$('.mobile-tooltip').addClass('active');
			}
		},
		functionAfter: function() {
			$('.mobile-tooltip').removeClass('active');
			$('.mobile-tooltip-content').html('');
			activeTooltip = false;
		}


	});
	//songListTable.sort('lastUsed', { order: 'desc' }); 


	$('#songBody .data-row li').each(function() {
		if ($(this).text() === '') {
			$(this).html('&nbsp;');
		}
		var sincedate = '';
		if ($(this).data('sincedate')) {
			sincedate = 'data-sincedate="Times Played (since ' + $(this).data('sincedate') + ')"';
		}
		$(this).append('<span class="data-label" ' + sincedate + '></label>')
	});


	$('.search-title').on('keyup', function() {
		var searchString = $(this).val();
		songListTable.search(searchString, ['title', 'artist']);
	});


	$('.clear-input').click(function(e) {
		e.preventDefault();
		$('.search-title').val('');
		songListTable.search('', ['title', 'artist']);

	});

	$('.button--filter').click(function(e) {
		e.preventDefault();
		$('.button--filter').removeClass('selected');
		$(this).addClass('selected');

		var searchString = $(this).data('filterslot');
		if (searchString === '') {
			songListTable.filter(); // Remove all filters
		} else {
			songListTable.filter(function(item) {
			   if (item.values().preferredSlot*1 === searchString*1) {
				   return true;
			   } else {
				   return false;
			   }
			}); 

		}

	})

}



function processRandomData() {
	var now = Date.now();
	var maxUse = 1;
	randomSongs.forEach(function(song) {
		song.lastUsed = new Date(song.lastUsed);
		song.lastUsedFormatted = (song.lastUsed.getMonth() + 1) + "/" + song.lastUsed.getDate() + "/" + song.lastUsed.getFullYear();
		var startDate = new Date(song.startDate);
		var sincedate = (startDate.getMonth() + 1) + "/" + startDate.getDate() + "/" + startDate.getFullYear();
		
		song.sincedate = sincedate;
		if (song.dates.length > 0) {
			maxUse = Math.max(maxUse, song.useCount);
		}
	});

	$('#randomBody').empty();
	randomSongs.forEach(function(song) {
		song.usePercent = Math.ceil((song.useCount/maxUse) * 100);
		$('#randomBody').append(Mustache.render(tableRowTemplate, song));
	});


	$( ".wkSince:contains('999')" ).css( "visibility", "hidden" );


	$('#randomBody .data-row li').each(function() {
		if ($(this).text() === '') {
			$(this).html('&nbsp;');
		}
		var sincedate = '';
		if ($(this).data('sincedate')) {
			sincedate = 'data-sincedate="Times Played (since ' + $(this).data('sincedate') + ')"';
		}
		$(this).append('<span class="data-label" ' + sincedate + '></label>')
	});

		$('.button--randomize').removeClass('processing');

}

function processVenues() {
	var foldersObj = {};
	foldersObj.children = '';
	foldersObj.isChild = '';

	venues.forEach(function(venue) {
		foldersObj.children += getVenueDisplay( venue);
	});

	$('#venuesList').empty();
	var venuesString = Mustache.render(folderListTemplate, foldersObj);
	$('#venuesList').append(venuesString);
}

function getVenueDisplay(folder) {
	folder.subFolders = '';
	folder.hasChildren = 'noChildren';

	var venuesObj = {};
	venuesObj.children = '';
	if(folder.ChildFolders.length > 0) {
		venuesObj.isChild = 'childList';
		folder.hasChildren = 'hasChildren';
		folder.ChildFolders.forEach(function(venueChild) {
			venuesObj.children += getVenueDisplay(venueChild);
		});
	}
	if(folder.Venues.length > 0) {
		venuesObj.isChild = 'childList';
		folder.hasChildren = 'hasChildren';
		folder.Venues.forEach(function(venueChild) {
			venuesObj.children += Mustache.render(venueTemplate, venueChild);
		});
	}
	if (venuesObj.children !== '') {folder.subFolders = Mustache.render(folderListTemplate, venuesObj);}

 	
	return Mustache.render(folderTemplate, folder);
}

function sendFeedback() {
	var feedback = $('#feedback-form-content').val();
	if (feedback === '') {
		showCustomMessage('Hey! You didn&rsquo;t put in any feedback!', 'hide');
		return;
	}
	var data = new FormData();
	data.append("label", $('#feedback-form-name').val());
	data.append("message", feedback);

	showMessage("progress");

	$.ajax({
		type: "POST",
		url: emailPath,
		contentType: false,
		processData: false,
		data: data,
		success: function (result) {
			showMessage("complete", 'close');
		},
		error: function () {
		  showMessage("error");
		}
	});		
}

function showCustomMessage(message, autoclose) {
	
	$('#feedback-form .message .custom').html(message);
	showMessage('custom', autoclose);
}

function showMessage(msgClass, autoclose) {
	hideMessage();
	$('#feedback-form .message').addClass(msgClass);

	if (autoclose && autoclose === 'close') {
		setTimeout(closeModal, 3000);
	}

	if (autoclose && autoclose === 'hide') {
		setTimeout(hideMessage, 3000);
	}
}

function showModal() {
	hideMessage();
	$('body').addClass('show-modal');
	$('.l-modal-container').fadeIn(250);
}

function closeModal() {
	$('.l-modal-container').fadeOut(100, function() {
		$('body').removeClass('show-modal');
		hideMessage();	
	});
}

function hideMessage() {
	$('#feedback-form .message').removeClass().addClass("message");
}

//------------------------------------------------
//
// ul-select
// https://github.com/zgreen/ul-select
//

$.fn.ulSelect = function(){
	var ul = $(this);

	if (!ul.hasClass('zg-ul-select')) {
		ul.addClass('zg-ul-select');
	}
	// SVG arrow
	var arrow ='<span id="ul-arrow" class="arrow"></span>'; // '<svg id="ul-arrow" xmlns="http://www.w3.org/2000/svg" version="1.1" width="32" height="32" viewBox="0 0 32 32"><line stroke-width="1" x1="" y1="" x2="" y2="" stroke="#449FDB" opacity=""/><path d="M4.131 8.962c-0.434-0.429-1.134-0.429-1.566 0-0.432 0.427-0.432 1.122 0 1.55l12.653 12.528c0.434 0.429 1.133 0.429 1.566 0l12.653-12.528c0.432-0.429 0.434-1.122 0-1.55s-1.136-0.429-1.566-0.002l-11.87 11.426-11.869-11.424z" fill="#111"/></svg>';
	$('li:first-of-type', this).addClass('active').append(arrow);

	$(this).on('click', 'li .arrow', function(event){
		event.stopPropagation();
		var $thisLI = $(this).closest('li');
		// Remove div#selected if it exists
		if ($('#selected--zg-ul-select').length) {
			$('#selected--zg-ul-select').remove();
		}

		ul.before('<div id="selected--zg-ul-select">');

		$('li #ul-arrow', ul).remove();
		ul.toggleClass('active');

	});

	$(this).on('click', 'li', function(event){
		if (!ul.hasClass('active')) {return;}
		var $thisLI = $(this);
		var selected = $('#selected--zg-ul-select');

		// Remove active class from any <li> that has it...
		ul.children().removeClass('active');
		// And add the class to the <li> that gets clicked
		$thisLI.toggleClass('active');

		var selectedText = $thisLI.text();
		if (ul.hasClass('active')) {
		  selected.text(selectedText).addClass('active').append(arrow);
		  $('ul.zg-ul-select').removeClass('active');
		  $('#selected--zg-ul-select').removeClass('active').text('');
		  $('#ul-arrow').remove();
		  $('ul.zg-ul-select li.active').append(arrow);
		}
		else {
		  selected.text('').removeClass('active'); 
		  $('li.active', ul).append(arrow);

		}
	});

	// Close the faux select menu when clicking outside it 
	$(document).on('click', function(event){
		if($('ul.zg-ul-select').length) {
			if(!$('ul.zg-ul-select').has(event.target).length == 0) {
				return;
			} else {
				$('ul.zg-ul-select').removeClass('active');
				$('#selected--zg-ul-select').removeClass('active').text('');
				$('#ul-arrow').remove();
				$('ul.zg-ul-select li.active').append(arrow);
			} 
		}
	});
}

function SetCookie(cookieName, cookieValue, nDays) {
	var today = new Date();
	var expire = new Date();
	if (nDays == null || nDays == 0) {nDays=1;}
	expire.setTime(today.getTime() + 3600000 * 24 * nDays);
	document.cookie = cookieName + "=" + escape(cookieValue) + ";expires=" + expire.toGMTString();
}

function GetCookie(cookie_name) {
	var results = document.cookie.match (cookie_name + '=(.*?)(;|$)');

	if (results) {
		return (unescape (results[1] ));
	} else{
		return null;
	}
}

// Run
$('.drop-list').ulSelect();