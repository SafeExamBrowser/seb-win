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
	{ appinfo, io, locale, prefs, prompt, scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services,
	{ FileUtils } = Cu.import("resource://gre/modules/FileUtils.jsm",{}),
	{ OS } = Cu.import("resource://gre/modules/osfile.jsm");

Cu.import("resource://gre/modules/XPCOMUtils.jsm");

/* Services */

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");
scriptloader.loadSubScript("resource://globals/const.js");

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
	DEBUG_CMD : 0,
	DEBUG : false,
	INFO : false,
	cmdline : null,
	defaultConfig : null,
	locale : "en-US",
	config : null,
	url : "",
	mainWin : null,
	profile: {},
	locs : null,
	consts : null,
	ars : {},
	allowQuit : false,
	quitURL : "",
	quitIgnorePassword : false,
	quitIgnoreWarning : false,
	hostForceQuit : false,
	hostQuitHandler : null,
	reconfState : RECONF_NO,
	reconfWin : null,
	lastWin : null,
	reconfWinStart : false,
	arsKeys : {},
	isLocked : false,
    lockMode : MODE_RECONNECT,
    privateClipboard : {},

	toString : function() {
		return appinfo.name;
	},

	quitObserver : {
		observe	: function(subject, topic, data) {
			if (topic == "xpcom-shutdown") {
				if (base.reconfWinStart) {
					sl.debug("quitObserver skipped");
					return;
				}
				if (base.config["removeBrowserProfile"]) {
					base.removeBrowserProfileFiles(true);
				}
				else {
					base.removeBrowserProfileFiles();
				}
				if (typeof base.hostQuitHandler === 'function') {
					sl.debug("apply hostQuitHandler");
					base.hostQuitHandler.apply(sh,[]);
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
		base.DEBUG_CMD = su.getNumber(su.getCmd("debug"));
		base.DEBUG = su.getBool(base.DEBUG_CMD); // true > 0
		base.INFO = (base.DEBUG && (base.DEBUG_CMD == INFO_LEVEL)) ? true : false;
		sl.init(base);
		base.initProfile();
		sw.init(base);
		sb.init(base);
		ss.init(base);
		sc.init(base);
		base.initDebug();
		sg.initConfig(base.initAfterConfig);
		sl.out("initCmdLine finished");
	},

	initDebug : function() {
		let prefFile = (base.DEBUG) ? FileUtils.getFile("CurProcD",["debug_prefs.js"], null) : FileUtils.getFile("CurProcD",["debug_reset_prefs.js"], null);
		if (prefFile.exists()) {
			sl.debug("found " + prefFile.path);
			try {
				prefs.readUserPrefs(prefFile);
				prefs.readUserPrefs(null); // tricky: for current prefs file use profile prefs, so my original prefs will never be overridden ;-)
				//prefs.savePrefFile(null);
			}
			catch (e) { sl.err(e); }
		}
		else { sl.err("could not find: " + prefFile.path); }
	},

	initAfterConfig : function() {
		base.initLocale();
		base.initAdditionalResources();
		base.getArsLinksAndKeys();
        base.initPrivateClipboard();
		sn.init(base); // needs config on init for compiled RegEx
		sn.initProxies();
		sh.init(base);
		sb.initSecurity();
		sb.initSpellChecker();
	},

	initProfile : function() {
		sl.out("initProfile");
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
		/*
		try {
			let osLoc = locale.getAppLocale();
			sl.debug("getAppLocale: " + osLoc);
			if (osLoc != "") {
				loc = osLoc;
			}
		}
		catch(e) {
			sl.err("locale.getAppLocale() error: " + e);
		}
		*/
		let paramLoc = base.config["browserLanguage"];
		sl.debug("browserLanguage:" + paramLoc);
		if (paramLoc != null && paramLoc != "") {
			base.locale = paramLoc;
		}
		let cmdLoc = su.getCmd("language");
		sl.debug("cmd language:" + cmdLoc);
		if (cmdLoc != null && cmdLoc != "") {
			base.locale = cmdLoc;
		}
		sl.debug("locale: " + base.locale);
		sg.setPref("general.useragent.locale",base.locale);
		sg.setPref("intl.accept_languages",base.locale);
	},

	initMain : function(win) {
		sl.debug("initMain");
		base.url = su.getUrl();
		base.allowQuit = su.getConfig("allowQuit","boolean",false);
		base.quitURL = su.getConfig("quitURL","string","").replace(/\/$/,"");
        base.clearClipboardUrl = su.getConfig("clearClipboardUrl","string","").replace(/\/$/,"");
        base.clearClipboardUrlRegex = new RegExp(sn.getRegex(base.clearClipboardUrl));
		base.initArsKeys(win);
		sb.setEmbeddedCerts();
		base.setQuitHandler(win);
		sh.setMessageSocketHandler(win);
		sh.createScreenKeyboardController(win);
		sh.createFullscreenController(win);
		ss.setSebserverSocketHandler(win);
		sb.createSpellCheckController(win);
        sb.createClipboardController(win);
		base.locs = win.document.getElementById("locale");
		base.consts = win.document.getElementById("const");
		sw.setMainNavigation(win);
		sw.setToolbar(win);
		sw.setSize(win);
		sb.loadPage(win,base.url);
		//sw.showContent(win); still required?
		/*
		if (!base.reconfWinStart) {
			sb.loadPage(win,base.url);
		}
		*/ 
	},

	initSecondary : function(win) {
		sl.debug("initSecondary");
		base.initArsKeys(win);
		sw.setPopupNavigation(win);
		sw.setToolbar(win);
		sw.setSize(win);
		sh.createScreenKeyboardController(win);
		sh.createFullscreenController(win);
		sb.createSpellCheckController(win);
        sb.createClipboardController(win);
	},

	initAdditionalResources : function (obj) {
		//var ar = {};
		if (obj === undefined) { // initial load
			sl.debug("initAdditionalResources");
			obj = su.getConfig("additionalResources","object",null);
			if (obj !== undefined && obj !== null) {
				base.initAdditionalResources(obj);
				return;
			}

		}
		else { // object param
			for (var i=0;i<obj.length;i++) { // ars array
				var ar = obj[i];
				var data = {};
				var sub = null;
				for (var key in ar) { // plain object structure without hierarchy
					if (key !== "additionalResources") {
						data[key] = ar[key];
					}
					else {

						if (ar[key] !== undefined && ar[key] !== null) {
							base.initAdditionalResources(ar[key]);
						}
					}
				}
				base.ars[data["identifier"]] = data;
			}
		}
	},

	getArsLinksAndKeys : function () {
		sl.debug("getArsLinksAndKeys");
		for (let k in base.ars) {
			let ar = base.ars[k];
			// prepare all regex and queries in ar objects for better performance
			ar["linkURLExist"] = (ar["linkURL"] && ar["linkURL"] != "");
			ar["refererFilterExist"] = (ar["refererFilter"] && ar["refererFilter"] != "");
			ar["linkOrReferer"] = (ar["linkURLExist"] || ar["refererFilterExist"]);
			ar["linkAndReferer"] = (ar["linkURLExist"] && ar["refererFilterExist"]);
			ar["linkOnly"] = (ar["linkURLExist"] && !ar["refererFilterExist"]);
			ar["refererOnly"] = (ar["refererFilterExist"] && !ar["linkURLExist"]);
			ar["isLink"] = (ar["url"] && ar["url"] != "");
			ar["checkTrigger"] = function () { return false; };
			
			if (ar["linkURLExist"]) {
				ar["linkURLRegex"] = su.globToRegex(ar["linkURL"]);
			}
			if (ar["refererFilter"] && ar["refererFilter"] != "") {
				ar["refererFilterRegex"] = su.globToRegex(ar["refererFilter"]);
			}
			if (ar["linkOrReferer"]) {
				ar["checkTrigger"] = function(url,referer) {
					if (this.linkOnly) {
						return this.linkURLRegex.test(url);
					}
					if (this.refererOnly) {
						
						return this.refererFilterRegex.test(referer);
					}
					return (this.linkURLRegex.test(url) && this.refererFilterRegex.test(referer));
				}
			} 
			if (ar["keycode"] && ar["keycode"] != "") {
				let m = (ar["modifiers"] && ar["modifiers"] != "") ? ar["modifiers"] : "";
				base.arsKeys[k] = {keycode : ar["keycode"], modifiers : m}
			}
			if (ar["key"] && ar["key"] != "") {
				let m = (ar["modifiers"] && ar["modifiers"] != "") ? ar["modifiers"] : "";
				base.arsKeys[k] = {key : ar["key"], modifiers : m}
			}
		}
	},

	initArsKeys : function (win) {
		let keySet = win.document.getElementById("sebKeySet");
		for (k in base.arsKeys) {
			let elKey = win.document.createElement("key");
			elKey.setAttribute("id", k);
			if (base.arsKeys[k].keycode) {
				elKey.setAttribute("keycode", base.arsKeys[k].keycode);
				elKey.setAttribute("modifiers", base.arsKeys[k].modifiers);
			}
			if (base.arsKeys[k].key) {
				elKey.setAttribute("key", base.arsKeys[k].key);
				elKey.setAttribute("modifiers", base.arsKeys[k].modifiers);
			}
			elKey.setAttribute("oncommand",'seb.loadAR(window, this.id, true)');
			keySet.appendChild(elKey);
		}
		keySet.parentNode.appendChild(keySet);
	},
	
    initPrivateClipboard : function() {
        base.privateClipboard['text'] = "";
        base.privateClipboard['ranges'] = [];
    },
    
	/* handler */
	setQuitHandler : function(win) {
		sl.debug("setQuitHandler");
		win.addEventListener("close", base.quit, true); // controlled shutdown for main window
		base.quitObserver.register();
	},

	removeQuitHandler : function(win) {
		sl.debug("removeQuitHandler");
		win.removeEventListener("close", base.quit); // controlled shutdown for main window
		base.quitObserver.unregister();
	},

	/* events */
	onload : function (win) {
		sl.debug("onload");
		sw.addWin(win);
		sb.setBrowserHandler(win);
		if (sw.getWinType(win) == "main") {
			if (base.reconfWinStart) { // new main window is loaded 
				sl.debug("new main window is loaded");
				sl.debug("send message to host for closing the old main window...");
				base.removeQuitHandler(base.mainWin);
				sh.sendReconfigureSuccess();
				base.reconfWinStart = false;
			}
			base.mainWin = win;
			base.initMain(win);
		}
		else {
			base.initSecondary(win);
		}
	},

	onunload : function(win) {
		sl.debug("onunload");
		if (sw.isDeprecatedMain(win)) {
			sl.debug("deprecated win");
			return;
		}
		if (sw.getWinType(win) == "main") {
			sl.debug("close message socket");
			//base.removeBrowserProfileFiles();
			sh.closeMessageSocket();
		}
		else {
			sl.debug("remove secondary win");
			sw.removeWin(win);
		}
	},

	onclose : function (win) {
		sl.debug("onclose");
		if (sw.getWinType(win) == "main") { return; }
		sl.debug("onclose secondary win");
	},

	sizeModeChange : function (e) {
		sl.debug("sizemodechange: " + e);
		e.preventDefault();
		e.stopPropagation();
		return;
	},

	reload: function(win) {
		sl.debug("try reload...");
		win = (win === null) ? sw.getRecentWin() : win;
		if (win == base.mainWin) {
			if (su.getConfig("browserWindowAllowReload","boolean",true)) {
				if (su.getConfig("showReloadWarning","boolean",true)) {
					//var prompts = Cc["@mozilla.org/embedcomp/prompt-service;1"].getService(Ci.nsIPromptService);
					var result = prompt.confirm(null, su.getLocStr("seb.reload.warning.title"), su.getLocStr("seb.reload.warning"));
					if (result) {
						sb.reload(win);	
					}
				} else {
					sb.reload(win);
				}
			}
		} 
		else {
		    if (su.getConfig("newBrowserWindowAllowReload","boolean",true)) {
			if (su.getConfig("newBrowserWindowShowReloadWarning","boolean",true)) {
			    var result = prompt.confirm(null, su.getLocStr("seb.reload.warning.title"), su.getLocStr("seb.reload.warning"));
			    if (result) {
				sb.reload(win);
			    }
			} 
			else {
			    sb.reload(win);
			}
		    }
		}
	},

	reconfigure: function(config) {
		sl.debug("reconfigure");
		sl.info("reconfigure config: " + config);
		if (base.reconfState == RECONF_ABORTED) {
			sl.debug("aborted!");
			return;
		}

		if (!su.isBase64(config)) {
			var msg = "no base64 config recieved: aborted!";
			sl.debug(msg);
			base.reconfState == RECONF_NO;
			//sb.dialogHandler(msg);
			return;
		}
		if (base.reconfState == RECONF_START) { // started by link and dialog
			sb.resetReconf();
		}
		base.reset();
		sg.initCustomConfig(config);
		sw.resetWindows();
		base.reconfWinStart = true;
		var ww = Cc["@mozilla.org/embedcomp/window-watcher;1"].getService(Ci.nsIWindowWatcher);
		var win = ww.openWindow(null, SEB_URL,"reconfWin", SEB_FEATURES, null);
		//sw.openWin(su.getUrl());
		//base.mainWin.document.location.reload(true);
	},

	reset: function() {
		base.url = "";
		//mainWin : null, oder doch?
		//profile: {},
		//locs : null,
		//consts : null,
		base.ars = {};
		base.allowQuit = false;
		base.quitURL = "";
		base.quitIgnorePassword = false;
		base.quitIgnoreWarning = false;
		base.hostForceQuit = false;
		try {
			sg.setPref("general.useragent.override",su.userAgent);
		}
		catch(e) {
			sl.err(e);
		}
		//hostQuitHandler : null,
		//reconfState : RECONF_NO,
		//base.reconfWin = null;
		base.lastWin = null;
		//reconfWinStart : false,
		base.arsKeys = {};
	},

	removeBrowserProfileFiles : function (all) {
		try {
			let alltxt = (all) ? "all " : "";
			let profileFiles = su.getConfig("removeBrowserProfileFiles","object",[]);
			//sl.debug("profileFilesType: " + typeof profileFiles);
			for (var i=0;i<base.profile.dirs.length;i++) { // don't delete data folder
				sl.debug("try to remove files " + alltxt + "from profile folder: " + base.profile.dirs[i].path);
				let entries = base.profile.dirs[i].directoryEntries;
				if (all) {
					sl.debug("removeBrowserProfileFiles all");
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
				else {
					sl.debug("removeBrowserProfileFiles from array");
					while(entries.hasMoreElements()) {
						let entry = entries.getNext();
						entry.QueryInterface(Ci.nsIFile);
						if (profileFiles.includes(entry.leafName)) {
							try {
								sl.debug("remove: " + entry.path);
								entry.remove(true);
							}
							catch(e) { sl.err(e); }
						}
					}
				}
			}
		}
		catch (e) {
			dump(e);
		}
	},
	
	loadAR: function(win, id, byKey=false) {
		sl.debug("try to load additional ressource:" + id);
		let ar = base.ars[id];
		let url = ar["url"];
		// check if embedded ressource is triggered
		if (!url || url == "") {
			sh.sendAdditionalRessourceTriggered(id);
			return true;
		}
		
		//let filter = ar["refererFilter"];
		let reset = ar["resetSession"];
		let confirm = ar["confirm"];
		let confirmText = (ar["confirmText"] && ar["confirmText"] != "") ? ar["confirmText"] : su.getLocStr("seb.load.warning");
		if (!url || url == "") {
			sl.debug("no url to load!");
			return;
		}

		// check referrer only if loaded byKey
		
        if (byKey) {
            let w = (win) ? win : sw.getRecentWin();
            let loadReferrer = w.content.document.location.href;
            if (ar["refererFilterRegex"] && ar["refererFilterRegex"] != "" && !ar["refererFilterRegex"].test(loadReferrer)) {
				sl.debug("loading \"" + url + "\" is only allowed if string in referrer: \"" + ar['refererFilterRegex'] + "\"");
				return false;
            }
        }
		
		
		// check confirmation
		if (confirm) {
			var result = prompt.confirm(null, su.getLocStr("seb.load.warning.title"), confirmText);
			if (!result) {
				sl.debug("loadURL aborted by user");
				return;
			}
		}
		if (reset) {
			sb.clearSession();
		}
		if (ar["newWindow"] === false) { // explicit exists and false, null ignored
			sb.loadPage(base.mainWin,url);
		}
		else { // default even if no newWindow parameter exists
			sw.openDistinctWin(url);
		}
	},
	
	initLockScreen : function(win) {
		sl.debug("initLockScreen");
		sl.out("Lock Screen!");
        let sebLocked = win.document.getElementById("sebLocked");
        let sebLockedUserSwitch = win.document.getElementById("sebLockedUserSwitch");
        let sebReconnect = win.document.getElementById("sebReconnect");
        switch (base.lockMode) {
            case MODE_LOCKED :
                sebLocked.classList.remove("hidden");
                sebLockedUserSwitch.classList.add("hidden");
                sebReconnect.classList.add("hidden");
                break;
            case MODE_USERSWITCH :
                sebLocked.classList.add("hidden");
                sebLockedUserSwitch.classList.remove("hidden");
                sebReconnect.classList.add("hidden");
                let unlockKeySet = win.document.getElementById("unlockKeySet");
                unlockKeySet.setAttribute("disabled",false);
                try {
                    win.document.getElementById("sebUserSwitchPasswordInput").value = "";
                    win.document.getElementById("sebLockUnlockMessageUserSwitch").value = " ";
                }
                catch(e) {}
                break;
            case MODE_RECONNECT :
                sebLocked.classList.add("hidden");
                sebLockedUserSwitch.classList.add("hidden");
                sebReconnect.classList.remove("hidden");
                try {
                    win.document.getElementById("sebLockPasswordInput").value = "";
                    win.document.getElementById("sebLockUnlockMessage").value = " ";
                }
                catch(e) {}
                break;
        }
	},
	
	lockScreenReload : function(win) {
		sl.debug("lockScreenReload");
		if (base.mainWin) {
			base.mainWin.document.getElementById("seb.lockscreen").reload();
		}
	},
	
	lock : function(lockMode=MODE_RECONNECT) {
		sl.debug("seb lock " + lockMode);
        base.lockMode = lockMode;
        if (base.isLocked) {
            sl.debug("seb already locked...");
            return;
        }
        
		for (var i=0;i<sw.wins.length;i++) {
			try {
                let lockBrowser = sw.wins[i].document.getElementById("seb.lockscreen");
                let imageBox = sw.wins[i].document.getElementById("imageBox");
                if (lockBrowser.getAttribute('src') !== LOCK_URL) {
                    lockBrowser.setAttribute("src",LOCK_URL);
                }
                else {
                    lockBrowser.reload();
                }
                sw.showLock(sw.wins[i]);
                if (imageBox) {
                    imageBox.classList.add("hidden2");
                }
			}
			catch(e) {
				sl.err(e);
				return;
			}
		}
		base.isLocked = true;
        switch (lockMode) {
            case MODE_RECONNECT :
                base.setUnconnectedMessage();
                sh.reconnect();
                break;
            case MODE_LOCKED :
                ss.sendLock();
                break;
        }
	},
    
	setUnconnectedMessage : function() {
		sl.debug("setUnconnectedMessage");
		for (var i=0;i<sw.wins.length;i++) {
			try {
				let unconnectedBox = sw.wins[i].document.getElementById("unconnectedBox");
				unconnectedBox.classList.remove("hidden");
			}
			catch(e) {
				sl.err(e);
			}
		}
	},
	
	deleteUnconnectedMessage : function() {
		sl.debug("deleteUnconnectedMessage");
		for (var i=0;i<sw.wins.length;i++) {
			try {
				let unconnectedBox = sw.wins[i].document.getElementById("unconnectedBox");
				unconnectedBox.classList.add("hidden");
			}
			catch(e) {
				sl.err(e);
			}
		}
	},
	
	setReconnectScreen : function() {
		sl.debug("setReconnectScreen");
		for (var i=0;i<sw.wins.length;i++) {
            try {
                let lockBrowser = sw.wins[i].document.getElementById("seb.lockscreen");
                let unlockKeySet = lockBrowser.contentDocument.getElementById("unlockKeySet");
                unlockKeySet.setAttribute("disabled",true);
				let reconnectVbox = lockBrowser.contentDocument.getElementById("sebReconnectVbox");
				let unlockVbox = lockBrowser.contentDocument.getElementById("sebUnlockVbox");
				reconnectVbox.classList.remove("hidden");
				unlockVbox.classList.add("hidden");
			}
			catch(e) {
				sl.err(e);
			}
		}
	},
	
	setUnlockScreen : function() {
		sl.debug("setUnlockScreen");
		for (var i=0;i<sw.wins.length;i++) {
		try {
                let lockBrowser = sw.wins[i].document.getElementById("seb.lockscreen");
                let unlockKeySet = lockBrowser.contentDocument.getElementById("unlockKeySet");
                unlockKeySet.setAttribute("disabled",false);
				let reconnectVbox = lockBrowser.contentDocument.getElementById("sebReconnectVbox");
				let unlockVbox = lockBrowser.contentDocument.getElementById("sebUnlockVbox");
				reconnectVbox.classList.add("hidden");
				unlockVbox.classList.remove("hidden");
			}
			catch(e) {
				sl.err(e);
			}
		}
	},
	
	unlock : function(win) {
		sl.debug("try unlock...");
        let passwd = su.getConfig("hashedQuitPassword","string","");
        let password = null;
        let unlockMessage = null;
		if (passwd === "") {
			password.value = "";
			unlockMessage.value = su.getLocStr("seb.unlock.failed.no.password");
			return;
		}
        switch (base.lockMode) {
            case MODE_USERSWITCH :
                password = win.document.getElementById("sebUserSwitchPasswordInput");
                unlockMessage = win.document.getElementById("sebLockUnlockMessageUserSwitch");
                break;
            case MODE_RECONNECT :
                password = win.document.getElementById("sebLockPasswordInput");
                unlockMessage = win.document.getElementById("sebLockUnlockMessage");
                break;
        }
		unlockMessage.value = "";
		let pwd = password.value;
		if (pwd === "") {
			unlockMessage.value = su.getLocStr("seb.unlock.failed.empty.password");
			return;
		}
		let check = su.getHash(pwd);
		if (check.toLowerCase() != passwd.toLowerCase()) {
			unlockMessage.value = su.getLocStr("seb.unlock.failed.wrong.password");
			return;
		}
		else {
			base.unlockAll();
		}
	},
	
	unlockAll : function(lockMode=MODE_RECONNECT) {
        sl.debug("unlockAll");
        base.lockMode = lockMode;
        if (!base.isLocked) {
            return;
        }
		for (var i=0;i<sw.wins.length;i++) {
			sw.showContent(sw.wins[i]);
			let imageBox = sw.wins[i].document.getElementById("imageBox");
			if (imageBox) {
				imageBox.classList.remove("hidden2");
			}
		}
		base.isLocked = false;
        switch (lockMode) {
            case MODE_RECONNECT :
                if (sh.messageServer) {
                    base.deleteUnconnectedMessage();
                }
                break;
            case MODE_LOCKED :
                ss.sendUnlock();
                break;
        }
	},
	
	quit: function(e) {
		sl.debug("try to quit...");
		if (e) {
			if (sw.isDeprecatedMain(e.target)) {
				sl.debug("don't use quitHandler on reconf transaction");
				return;
			}
		}
		var w = base.mainWin;

		if (base.hostForceQuit) {
			sl.debug("host force quit");
			if (e != null) {
				return true;
			}
			else {
				try {
					sb.clearSession();
				}
				catch(e) {
					sl.err(e);
				}
				try {
					sg.setPref("general.useragent.override",su.userAgent);
				}
				catch(e) {
					sl.err(e);
				}
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

		if (!base.allowQuit) { // on quitURL the global variable "allowQuit" is set to true
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

			sw.closeAllWin();
			sl.debug("quit");
		}
	},
    
    clearClipboard : function() {
        sl.debug("clearClipboard");
        base.privateClipboard.ranges = [];
        base.privateClipboard.text = "";
        sh.sendClearClipboard();
    }
}
