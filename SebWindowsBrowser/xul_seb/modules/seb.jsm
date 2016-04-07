/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is the browser component of seb.
 *
 * The Initial Developer of the Original Code is Stefan Schneider <schneider@hrz.uni-marburg.de>.
 * Portions created by the Initial Developer are Copyright (C) 2005
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *   Stefan Schneider <schneider@hrz.uni-marburg.de>
 *   
 * ***** END LICENSE BLOCK ***** */

/* ***** GLOBAL seb SINGLETON *****

* *************************************/ 

/* 	for javascript module import
	see: https://developer.mozilla.org/en/Components.utils.import 
*/
this.EXPORTED_SYMBOLS = ["seb"];

/* Modules */
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ appinfo, io, locale, prefs, prompt } = Cu.import("resource://gre/modules/Services.jsm").Services,
	{ FileUtils } = Cu.import("resource://gre/modules/FileUtils.jsm",{}),
	{ OS } = Cu.import("resource://gre/modules/osfile.jsm");

Cu.import("resource://gre/modules/XPCOMUtils.jsm");

/* Services */

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"su","resource://modules/SebUtils.jsm","SebUtils");
XPCOMUtils.defineLazyModuleGetter(this,"sg","resource://modules/SebConfig.jsm","SebConfig");
XPCOMUtils.defineLazyModuleGetter(this,"sl","resource://modules/SebLog.jsm","SebLog");
XPCOMUtils.defineLazyModuleGetter(this,"sw","resource://modules/SebWin.jsm","SebWin");
XPCOMUtils.defineLazyModuleGetter(this,"sb","resource://modules/SebBrowser.jsm","SebBrowser");
XPCOMUtils.defineLazyModuleGetter(this,"sn","resource://modules/SebNet.jsm","SebNet");
XPCOMUtils.defineLazyModuleGetter(this,"sh","resource://modules/SebHost.jsm","SebHost");
XPCOMUtils.defineLazyModuleGetter(this,"ss","resource://modules/SebServer.jsm","SebServer");
XPCOMUtils.defineLazyModuleGetter(this,"sc","resource://modules/SebScreenshot.jsm","SebScreenshot");

/* ModuleGlobals */
let	base = null,
	overrideProfile = true;

this.seb =  {
	DEBUG : false,
	cmdline : null,
	config : null,
	url : "",
	mainWin : null,
	profile: {},
	locs : null,	
	consts : null,
	allowQuit : false,
	quitURL : "",
	quitIgnorePassword : false,
	quitIgnoreWarning : false,
	hostForceQuit : false,
	toString : function() {
		return appinfo.name;
	},
	
	quitObserver : {
		observe	: function(subject, topic, data) {
			if (topic == "xpcom-shutdown") {
				if (base.config["removeProfile"]) {
					sl.debug("removeProfile");
					for (var i=0;i<base.profile.dirs.length;i++) { // don't delete data folder
						sl.debug("try to remove everything from profile folder: " + base.profile.dirs[i].path);
						let entries = base.profile.dirs[i].directoryEntries;
						while(entries.hasMoreElements()) {  
							let entry = entries.getNext();  
							entry.QueryInterface(Ci.nsIFile);
							try {
								sl.debug("remove: " + entry.path);
								entry.remove(true);
							}
							catch(e) { sl.err(e); }
						}
					}
				}
			}	  
		},
		get observerService() {  
			return Cc["@mozilla.org/observer-service;1"].getService(Ci.nsIObserverService);  
		},
		register: function() {  
			this.observerService.addObserver(this, "xpcom-shutdown", false);  
			sl.debug("quitObserver registered");
		},  
		unregister: function()  {  
			this.observerService.removeObserver(this, "xpcom-shutdown");  
		}  
	},
	
	initCmdLine : function(cl) {
		base = this;
		base.cmdline = cl;
		su.init(base);
		sg.init(base);
		base.DEBUG = su.getBool(su.getCmd("debug"));
		sl.init(base); 
		base.initProfile();
		sw.init(base);
		sb.init(base);
		ss.init(base);
		sc.init(base);
		base.initDebug();
		sg.initConfig(base.initAfterConfig);
	},
	
	initDebug : function() {
		let prefFile = (base.DEBUG) ? FileUtils.getFile("CurProcD",["debug_prefs.js"], null) : FileUtils.getFile("CurProcD",["debug_reset_prefs.js"], null);
		if (prefFile.exists()) {
			sl.debug("found " + prefFile.path);
			try { 
				prefs.readUserPrefs(prefFile);
				prefs.readUserPrefs(null); // tricky: for current prefs file use profile prefs, so my original prefs will never be overridden ;-)
				prefs.savePrefFile(null);
			}
			catch (e) { sl.err(e); }
		}
		else { sl.err("could not find: " + prefFile.path); }
	},
	
	initAfterConfig : function() {
		base.initLocale();
		sn.init(base); // needs config on init for compiled RegEx
		sn.initProxies();
		sh.init(base);
		sb.initSecurity();
	},
	
	initProfile : function() {
		try {
			base.profile["dirs"] = [];
			let profilePath = OS.Constants.Path.profileDir;
			let profileDir = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsILocalFile);
			let defaultProfile = FileUtils.getDir("CurProcD",["defaults","profile"],null); // see http://mxr.mozilla.org/mozilla-central/source/xpcom/io/nsAppDirectoryServiceDefs.h
			profileDir.initWithPath(profilePath);
			sl.debug("push profile: " + profilePath);
			base.profile["dirs"].push(profileDir);
			// push AppData Local profile directory
			if (appinfo.OS == "WINNT") {
				let localProfilePath = profilePath.replace(/AppData\\Roaming/,"AppData\\Local"); // WIN7 o.k, XP?
				if (localProfilePath) {
					let localProfileDir = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsILocalFile);
					localProfileDir.initWithPath(localProfilePath);
					if (localProfileDir.exists()) {
						sl.debug("push local profile: " + localProfilePath);
						base.profile["dirs"].push(localProfileDir);
					}
				}
			}
			if (defaultProfile.exists()) {
				let entries = defaultProfile.directoryEntries; 
				base.profile["customFiles"] = [];
				while(entries.hasMoreElements()) {  
					let entry = entries.getNext();  
					entry.QueryInterface(Components.interfaces.nsIFile);
					// don't copy .svn
					if (/^\..*/.test(entry.leafName)) { // no hidden files like .svn .DS_Store
						continue;
					}
					var cf = base.profile.dirs[0].clone();
					cf.append(entry.leafName);
					base.profile.customFiles.push(cf);
					if (cf.exists() && overrideProfile) {
						try {
							cf.remove(true);
							sl.debug("delete existing " + cf.path);
						}
						catch(e) {};
					}
					if (!cf.exists()) {
						entry.copyTo(base.profile.dirs[0],entry.leafName);
						sl.debug("copy " + entry.leafName + " to " + base.profile.dirs[0].path);
					}														
				}
			}
			else {
				sl.debug("no default profile: " + defaultProfile.path);
			}		
		}
		catch (e) { sl.err(e); return false; }
	},
	
	initLocale : function() {
		let loc = "en-US";
		let osLoc = locale.getLocaleComponentForUserAgent();
		if (osLoc != "") {
			loc = osLoc;
		}
		let paramLoc = base.config["browserLanguage"];
		if (paramLoc != null && paramLoc != "") {
			loc = paramLoc;
		}
		let cmdLoc = su.getCmd("language");
		if (cmdLoc != null && cmdLoc != "") {
			loc = cmdLoc;
		}
		sl.debug("locale: " + loc);
		prefs.setCharPref("general.useragent.locale",loc);
	},
	
	initMain : function(win) {
		sl.debug("initMain");
		base.url = su.getUrl();
		base.allowQuit = su.getConfig("allowQuit","boolean",false);
		base.quitURL =su.getConfig("quitURL","string","");
		sb.setEmbeddedCerts();
		base.setQuitHandler(win);
		sh.setMessageSocketHandler(win);
		ss.setSebserverSocketHandler(win);
		base.locs = win.document.getElementById("locale");	
		base.consts = win.document.getElementById("const");
		sw.setToolbar(win);			
		sw.setSize(win);
		//sw.showContent(win); still required?
		sb.loadPage(win,base.url);
	},
	
	initSecondary : function(win) {
		sw.setToolbar(win);
		sw.setSize(win);
	},
	
	/* handler */
	setQuitHandler : function(win) {
		sl.debug("setQuitHandler");
		win.addEventListener( "close", base.quit, true); // controlled shutdown for main window
		base.quitObserver.register();
	},
	
	/* events */
	onload : function (win) {
		sl.debug("onload");
		sw.addWin(win);
		sb.setBrowserHandler(win);
		sn.httpRequestObserver.register();
		sn.httpResponseObserver.register();
		if (sw.getWinType(win) == "main") {
			base.mainWin = win;
			base.initMain(win);
		}
		else {
			base.initSecondary(win);
		}
	},
	
	onunload : function(win) {
		sl.debug("onunload");
		if (sw.getWinType(win) == "main") {
			sh.closeMessageSocket();
		}
		else {
			sw.removeWin(win);
		}
	},
	
	onclose : function (win) {
		sl.debug("onclose");
		if (sw.getWinType(win) == "main") { return; }
		sl.debug("onclose secondary win");
	},
	
	sizeModeChange(e) {
		sl.debug("sizemodechange: " + e);
		e.preventDefault();
		e.stopPropagation();
		return;
	},
	
	reload: function(win) {
		sl.debug("try reload...");
		win = (win === null) ? sw.getRecentWin() : win;
		if (su.getConfig("showReloadWarning","boolean",true)) {
			//var prompts = Cc["@mozilla.org/embedcomp/prompt-service;1"].getService(Ci.nsIPromptService);
			var result = prompt.confirm(null, su.getLocStr("seb.reload.warning.title"), su.getLocStr("seb.reload.warning"));
			if (result) {
				sb.reload(win);
			}
		}
		else {
			br.reload(win);
		}
	},
	
	quit: function(e) {
		sl.debug("try to quit...");
		var w = base.mainWin;
		
		if (base.hostForceQuit) {
			sl.debug("host force quit");
			if (e != null) {
				return true;
			}
			else {
				sw.closeAllWin();
				sl.debug("quit"); 
				return;
			}
		}
		else {
			if (e != null) {
				e.preventDefault();
				e.stopPropagation();
			}				
		}
		
		if (sh.messageServer) {
			sl.debug("quit should be handled by host");
			var msg = (e) ? "seb.beforeclose.manual" : "seb.beforeclose.quiturl";
			sh.sendMessage(msg);
			return;
		}
		
		if (!base.allowQuit) { // on shutdown url the global variable "shutdownEnabled" is set to true
			sl.out("no way! seb is locked :-)");
		}
		else {
			if (e) { // close window event: user action
				// first look for warning
				let passwd = su.getConfig("hashedQuitPassword","string","");
				
				if (!base.quitIgnoreWarning && !passwd) {
					var result = prompt.confirm(null, su.getLocStr("seb.quit.warning.title"), su.getLocStr("seb.quit.warning"));
					if (!result) {
						return;
					}
				}							
				
				if (passwd && !base.quitIgnorePassword) {				
					var password = {value: ""}; // default the password to pass
					var check = {value: true}; // default the checkbox to true
					var result = prompt.promptPassword(null, su.getLocStr("seb.password.title"), su.getLocStr("seb.password.text"), password, null, check);
					if (!result) {
						return;
					}
					var check = su.getHash(password.value);
					sl.debug(passwd + ":" + check);
					if (check.toLowerCase() != passwd.toLowerCase()) {
						sl.debug("wrong password");
						//prompt.alert(mainWin, getLocStr("seb.title"), getLocStr("seb.url.blocked"));
						prompt.alert(base.mainWin, su.getLocStr("seb.password.title"), su.getLocStr("seb.password.wrong"));
						return;
					}
				}
			}
			else { // shutdown link: browser takes shutdown control
				if (!base.quitIgnoreWarning) {
					var result = prompt.confirm(null, su.getLocStr("seb.quit.warning.title"), su.getLocStr("seb.quit.warning"));
					if (!result) {
						return;
					}
				}
				
				let passwd = su.getConfig("hashedQuitPassword","string","");
				
				if (passwd && !base.quitIgnorePassword) {				
					var password = {value: ""}; // default the password to pass
					var check = {value: true}; // default the checkbox to true
					var result = prompt.promptPassword(null, su.getLocStr("seb.password.title"), su.getLocStr("seb.password.text"), password, null, check);
					if (!result) {
						return;
					}
					var check = su.getHash(password.value);
					sl.debug(passwd + ":" + check);
					if (check.toLowerCase() != passwd.toLowerCase()) {
						//prompt.alert(mainWin, getLocStr("seb.title"), getLocStr("seb.url.blocked"));
						prompt.alert(base.mainWin, su.getLocStr("seb.password.title"), su.getLocStr("seb.password.wrong"));
						return;
					}
				}
			}
			
			/*
			if (sebBinaryClient) {
				for (var s in sebBinaryClient.streams) {
					x.debug("close stream " + s);
					sebBinaryClient.streams[s]._socket.close();
				}
			}
			*/
			sw.closeAllWin();
			sl.debug("quit"); 
		}		
	}
}
