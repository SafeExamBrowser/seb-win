const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components;
Cu.import("resource://gre/modules/XPCOMUtils.jsm");
Cu.import("resource://gre/modules/Services.jsm");
Cu.import("resource://modules/seb.jsm");

var xulApplication = function () {};

xulApplication.prototype = {
	classDescription: "xulApplication",
	classID: Components.ID('{2971c315-b871-32cd-b33f-bfee4fcbf682}'),
	contractID: "@mozilla.org/commandlinehandler/profile-after-change;1?type=xulApplication",
	_xpcom_categories: [{
		category: "command-line-handler",
		entry: "m-xul-application"
	}],	
	QueryInterface: XPCOMUtils.generateQI([Ci.nsICommandLineHandler]),
	handle : function clh_handle(cmdLine) {		
		try {	
			seb.initCmdLine(cmdLine);	
			cmdLine.preventDefault = false;	 // what about p2pdf or apps without a main window event loop? 
		}
		catch (e) { dump(e+"\n"); }			
	},
	helpInfo : "	-debug              set debug 1|0\n" +
             "		-config	<uri>       additional json object url,\n" +
             "		wrapping this description\n"
};
var NSGetFactory = XPCOMUtils.generateNSGetFactory([xulApplication]);
