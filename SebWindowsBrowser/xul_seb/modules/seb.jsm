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
	arsKeys : {},

	toString : function() {
		return appinfo.name;
	},

	quitObserver : {
		observe	: function(subject, topic, data) {
			if (topic == "xpcom-shutdown") {
				if (base.config["removeBrowserProfile"]) {
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
				prefs.savePrefFile(null);
			}
			catch (e) { sl.err(e); }
		}
		else { sl.err("could not find: " + prefFile.path); }
	},

	initAfterConfig : function() {
		base.initLocale();
		base.initAdditionalResources();
		base.getArsLinksAndKeys();
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
		let loc = "en-US";
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
		
		let paramLoc = base.config["browserLanguage"];
		sl.debug("browserLanguage:" + paramLoc);
		if (paramLoc != null && paramLoc != "") {
			loc = paramLoc;
		}
		let cmdLoc = su.getCmd("language");
		sl.debug("cmd language:" + cmdLoc);
		if (cmdLoc != null && cmdLoc != "") {
			loc = cmdLoc;
		}
		sl.debug("locale: " + loc);
		sg.setPref("general.useragent.locale",loc);
		sg.setPref("intl.accept_languages",loc);
	},

	initMain : function(win) {
		sl.debug("initMain");
		base.url = su.getUrl();
		base.allowQuit = su.getConfig("allowQuit","boolean",false);
		base.quitURL =su.getConfig("quitURL","string","");
		base.initArsKeys(win);
		sb.setEmbeddedCerts();
		base.setQuitHandler(win);
		sh.setMessageSocketHandler(win);
		sh.createScreenKeyboardController(win);
		sh.createFullscreenController(win);
		ss.setSebserverSocketHandler(win);
		base.locs = win.document.getElementById("locale");
		base.consts = win.document.getElementById("const");
		sw.setToolbar(win);
		sw.setSize(win);
		//sw.showContent(win); still required?
		sb.loadPage(win,base.url);
	},

	initSecondary : function(win) {
		sl.debug("initSecondary");
		base.initArsKeys(win);
		sw.setToolbar(win);
		sw.setSize(win);
		sh.createScreenKeyboardController(win);
		sh.createFullscreenController(win);
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
			//sl.debug(JSON.stringify(base.ars[k]));
			if (base.ars[k]["linkURL"] && base.ars[k]["linkURL"] != "") {
				sb.linkURLS[base.ars[k]["linkURL"]] = k;
			}
			if (base.ars[k]["keycode"] && base.ars[k]["keycode"] != "") {
				let m = (base.ars[k]["modifiers"] && base.ars[k]["modifiers"] != "") ? base.ars[k]["modifiers"] : "";
				base.arsKeys[k] = {keycode : base.ars[k]["keycode"], modifiers : m}
			}
			if (base.ars[k]["key"] && base.ars[k]["key"] != "") {
				let m = (base.ars[k]["modifiers"] && base.ars[k]["modifiers"] != "") ? base.ars[k]["modifiers"] : "";
				base.arsKeys[k] = {key : base.ars[k]["key"], modifiers : m}
			}
		}
		//sl.debug(JSON.stringify(sb.linkURLS));
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
			elKey.setAttribute("oncommand",'seb.loadAR(window, this.id)');
			keySet.appendChild(elKey);
		}
		keySet.parentNode.appendChild(keySet);
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
			// sebserver and other handler?
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

	sizeModeChange : function (e) {
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
			sb.reload(win);
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
		sg.initCustomConfig(config);
		sw.resetWindows();
		base.mainWin.document.location.reload(true);
	},

	loadAR: function(win, id) {
		sl.debug("try to load additional ressource:" + id);
		let ar = base.ars[id];
		let url = ar["url"];
		// check if embedded ressource is triggered
		if (!url || url == "") {
			sh.sendAdditionalRessourceTriggered(id);
			return true;
		}
		
		let filter = ar["refererFilter"];
		let reset = ar["resetSession"];
		let confirm = ar["confirm"];
		let confirmText = (ar["confirmText"] && ar["confirmText"] != "") ? ar["confirmText"] : su.getLocStr("seb.load.warning");
		if (!url || url == "") {
			sl.debug("no url to load!");
			return;
		}

		// first check referrer
		if (filter && filter != "") {
			let w = (win) ? win : sw.getRecentWin();
			let loadReferrer = w.content.document.location.href;
			if (loadReferrer.indexOf(filter) < 0) {
				sl.debug("loading \"" + url + "\" is only allowed if string in referrer: \"" + filter + "\"");
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
		sb.loadPage(base.mainWin,url);
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

			sw.closeAllWin();
			sl.debug("quit");
		}
	}
}
