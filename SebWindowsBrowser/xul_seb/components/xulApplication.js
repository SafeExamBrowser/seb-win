const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ appinfo } = Cu.import("resource://gre/modules/Services.jsm").Services,
	{ OS } = Cu.import("resource://gre/modules/osfile.jsm");
	
Cu.import("resource://gre/modules/XPCOMUtils.jsm");
Cu.import("resource://gre/modules/Services.jsm");
Cu.import("resource://modules/seb.jsm");

var xulApplication = function () {};
var cmdline = null;
var logfile = null;
var logenc = null;
var filterLog = new RegExp(/(TelemetryEnvironment|ProfileAge\.jsm)/gm);
var consoleListener = {
	observe: function( msg ) {
		/*
		if (filterLog.test(msg.message)) {
			return;
		}
		*/ 
		let a = null;
		try { // general script error: report only exceptions
			msg.QueryInterface(Ci.nsIScriptError);
			//if (((msg.flags & Ci.nsIScriptError.errorFlag) == Ci.nsIScriptError.errorFlag) || ((msg.flags & Ci.nsIScriptError.exceptionFlag) == Ci.nsIScriptError.exceptionFlag)) {
			if ((msg.flags & Ci.nsIScriptError.exceptionFlag) == Ci.nsIScriptError.exceptionFlag) {	
				a = logenc.encode(msg.flags + " : " + msg.message + "\n");
				logfile.write(a);
			}
		}
		catch(e) { // seb debugging messages
			a = logenc.encode(msg.message + "\n");
			logfile.write(a);
		}
	},
	QueryInterface: function (iid) {
	if (!iid.equals(Ci.nsIConsoleListener) &&
		!iid.equals(Ci.nsISupports)) {
		throw Cr.NS_ERROR_NO_INTERFACE;
	}
	return this;
    }
};
xulApplication.prototype = {
	classDescription: "xulApplication",
	classID: Components.ID('{2971c315-b871-32cd-b33f-bfee4fcbf682}'),
	contractID: "@mozilla.org/commandlinehandler/profile-after-change;1?type=xulApplication",
	_xpcom_categories: [{
		category: "command-line-handler",
		entry: "m-xul-application"
	}],	
	QueryInterface: XPCOMUtils.generateQI([Ci.nsICommandLineHandler]),
	debug : 0,
	handle : function clh_handle(cmdLine) {	
		cmdline = cmdLine;
		try {	
			Cc["@mozilla.org/toolkit/crash-reporter;1"].getService(Ci.nsICrashReporter).submitReports = false;
			if (cmdLine.findFlag("silent",false) < 0) {
				let dpos = cmdLine.findFlag("debug",false);
				if (dpos > -1) {
					this.debug = cmdLine.getArgument(dpos+1); // maybe in windows only dpos -> split the whole string " "
					this._dump("debug " + this.debug);
					//this._dump(cmdline.getArgument("debug"));
				}
				//this.deletePrefs();
				this.initLog();
                                seb.initCmdLine(cmdLine);
                                cmdLine.preventDefault = false;
                        }
		}
		catch (e) { this._dump(e); }			
	},
	
	deletePrefs : function () {
		let prefsPath = OS.Path.join(OS.Constants.Path.profileDir,"prefs.js");
		this._dump("prefs.js: " + prefsPath);
		OS.File.remove(prefsPath,{ignoreAbsent:true}).then(
			function onSuccess() {
				dump("prefs.js deleted");
			},
			function onError() {
				dump("error");
			}
		); 
	},
	
	initLog : function(cmdLine) {
		logfileEnabled = this.getBool(this.getCmd("logfile"));
		this._dump("logfile enabled: " + logfileEnabled); 
		if (!logfileEnabled) { 
			this._dump("logfile disabled."); 
			return; 
		}
		logpath = this.getCmd("logpath");
		logpath = (typeof logpath == "string" && logpath != "") ? logpath : OS.Path.join(OS.Constants.Path.profileDir, appinfo.name + ".log");
		let promise = OS.File.open(logpath,{write:true, append:true});
		promise = promise.then(
			function onSuccess(file) {
				//dump("xulApplication: " + cmdline);
				
				logenc = new TextEncoder();
				logfile = file;
				let cs = Cc["@mozilla.org/consoleservice;1"].getService(Ci.nsIConsoleService);
				let carr = cs.getMessageArray();
				let d = new Date();
				let a = logenc.encode("\n**************************************\ninitialize logfile " + d.toLocaleString() + "\n**************************************\n");
				logfile.write(a);
				// write buffered console messages
				for (var i=0;i<carr.length;i++) {
					let msg = carr[i];
					let b = null;
					
					try { // general script error: report only exceptions
						msg.QueryInterface(Ci.nsIScriptError);
						//if (((msg.flags & Ci.nsIScriptError.errorFlag) == Ci.nsIScriptError.errorFlag) || ((msg.flags & Ci.nsIScriptError.exceptionFlag) == Ci.nsIScriptError.exceptionFlag)) {
						if ((msg.flags & Ci.nsIScriptError.exceptionFlag) == Ci.nsIScriptError.exceptionFlag) {	
							b = logenc.encode(msg.flag + " : " + msg.message + "\n");
							file.write(b);
						}
					}
					catch(e) { // seb debugging messages
						b = logenc.encode(msg.message + "\n");
						file.write(b);
					}
				}
				// register a console listener for writing all other messages to the logfile
				cs.registerListener(consoleListener);
			},
			function onError(file) {
				dump("error");
			}
		); 
	},
	getCmd : function (k) { // convert strings to data types
		let v = cmdline.handleFlagWithParam(k,false); // beware this will remove the key and the value from the commandline list!
		let t = (v === "" || v === null) ? null : v;
		if (t) {
			var num = parseFloat(t);
			// try to parseFloat
			if (isNaN(num)) { // not a number
				// try bool
				if (/^(true|false)$/i.test(t)) {
					return /^true$/i.test(t);
				}
				else {
					return t;
				}
			}
			else {
				return num;
			}
		}
		else {
			return t;
		}		
	},
	getBool : function (b) {
		var ret;
		switch (typeof(b)) {
			case "string" :
				ret = (b == "false" || b == "undefined") ? false : true;
				break;
			case "number" :
				ret = (b > 0) ? true : false;
				break;
			case "boolean" :
				ret = b;
				break;
			case "object" :
				ret = (b === null) ? false : true;
				break;
			case "undefined" :
				ret = false;
				break;
			default :
				ret = true;
		}
		return ret;
	},
	_dump : function(txt) {
		dump(txt+"\n");
	},
	helpInfo : "	-debug              set debug 1|0\n" +
             "		-config	<uri>       additional json object url,\n" +
             "		wrapping this description\n"
};
var NSGetFactory = XPCOMUtils.generateNSGetFactory([xulApplication]);
