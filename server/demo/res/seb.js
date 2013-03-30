// add an eventlistener to the form submit event 
$(document).ready(function() { $('form#frmQuestion').submit( function() { return screenshot(); })} );

// the eventhandler
function screenshot() {
	// the screenshot component needs some data to save the question snapshot		
	var params = getUrlVars();
	document.getElementById("test_id").value = params.test_id;
	document.getElementById("user_id").value = params.user_id;
	var file = { 
		'path'		:	[params.test_id,params.user_id],
		'filename'	:	SEQUENCE_ID + "_" + QUESTION_ID + "_" + Date.now()
	};
	if (seb_ScreenShot instanceof Function) {		
		seb_ScreenShot(window,file);
		return true;
	}
    else {
		return true;
	}
	return true;
}

function getUrlVars() {
    if (!window.location.search) {
        return({});   // return empty object
    }
    var parms = {};
    var temp;
    var items = window.location.search.slice(1).split("&");   // remove leading ? and split
    for (var i = 0; i < items.length; i++) {
        temp = items[i].split("=");
        if (temp[0]) {
            if (temp.length < 2) {
                temp.push("");
            }
            parms[decodeURIComponent(temp[0])] = decodeURIComponent(temp[1]);        
        }
    }
    return(parms);
}
