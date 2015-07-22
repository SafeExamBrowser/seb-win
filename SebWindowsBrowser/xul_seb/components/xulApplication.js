const Cc = Components.classes;
const Ci = Components.interfaces;

Components.utils.import("resource://gre/modules/XPCOMUtils.jsm");
Components.utils.import("resource://gre/modules/Services.jsm");
Components.utils.import("resource://modules/xullib.jsm");
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
			// dump(xullib.XULLIB_WIN+"\n\r");
			// var w = Services.ww.openWindow(null,xullib.XULLIB_WIN,"xullibwin","chrome,extrachrome,menubar,toolbar,status,resizable,dialog=no",null);
			// dump(w+"\n\r");
			xullib.init(cmdLine);	
			cmdLine.preventDefault = false;	 // what about p2pdf or apps without a main window event loop? 
			/*	
			if (Services.appinfo.name === "Firefox") { // don't hook into default BrowserHandler!
				cmdLine.preventDefault = false;
			}
			else { // custom app
				cmdLine.preventDefault = true;
			}
			*/
		}
		catch (e) {
			dump(e+"\n");
			//this.err(e);
		}			
	},
	// CHANGEME: change the help info as appropriate, but
	// follow the guidelines in nsICommandLineHandler.idl
	// specifically, flag descriptions should start at
	// character 24, and lines should be wrapped at
	// 72 characters with embedded newlines,
	// and finally, the string should end with a newline
	helpInfo : "	-debug              set debug true|false\n" +
             "		-config	<uri>       additional json object url,\n" +
             "		wrapping this description\n"
};
var NSGetFactory = XPCOMUtils.generateNSGetFactory([xulApplication]);
