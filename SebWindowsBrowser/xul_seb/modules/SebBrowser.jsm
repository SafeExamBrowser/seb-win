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

this.EXPORTED_SYMBOLS = ["SebBrowser"];

/* Modules */
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ prompt, scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services;
Cu.import("resource://gre/modules/XPCOMUtils.jsm");

/* Services */
let	wpl = Ci.nsIWebProgressListener,
	wnav = Ci.nsIWebNavigation,
	ovs = Cc["@mozilla.org/security/certoverride;1"].getService(Ci.nsICertOverrideService);
	

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"su","resource://modules/SebUtils.jsm","SebUtils");
XPCOMUtils.defineLazyModuleGetter(this,"sl","resource://modules/SebLog.jsm","SebLog");
XPCOMUtils.defineLazyModuleGetter(this,"sw","resource://modules/SebWin.jsm","SebWin");
XPCOMUtils.defineLazyModuleGetter(this,"sn","resource://modules/SebNet.jsm","SebNet");
XPCOMUtils.defineLazyModuleGetter(this,"sh","resource://modules/SebHost.jsm","SebHost");
XPCOMUtils.defineLazyModuleGetter(this,"sc","resource://modules/SebScreenshot.jsm","SebScreenshot");

/* ModuleGlobals */
let 	base = null,
	seb = null,
	certdb = null,
	nav = null,
	loadFlag = null,
	startDocumentFlags = wpl.STATE_IS_REQUEST | wpl.STATE_IS_DOCUMENT | wpl.STATE_START,
	stopDocumentFlags = wpl.STATE_IS_WINDOW | wpl.STATE_STOP,
	startNetworkFlags = wpl.STATE_IS_NETWORK | wpl.STATE_IS_DOCUMENT | wpl.STATE_START,
	stopNetworkFlags = wpl.STATE_IS_NETWORK | wpl.STATE_IS_WINDOW | wpl.STATE_STOP,
	wplFlag = { //nsIWebProgressListener state transition flags
	    STATE_START: wpl.STATE_START,
	    STATE_STOP: wpl.STATE_STOP,
	    STATE_REDIRECTING: wpl.STATE_REDIRECTING,
	    STATE_TRANSFERRING: wpl.STATE_TRANSFERRING,
	    STATE_NEGOTIATING: wpl.STATE_NEGOTIATING,
	    STATE_IS_REQUEST: wpl.STATE_IS_REQUEST,
	    STATE_IS_DOCUMENT: wpl.STATE_IS_DOCUMENT,
	    STATE_IS_NETWORK: wpl.STATE_IS_NETWORK,
	    STATE_RESTORING: wpl.STATE_RESTORING,
	    LOCATION_CHANGE_SAME_DOCUMENT: wpl.LOCATION_CHANGE_SAME_DOCUMENT,
	    LOCATION_CHANGE_ERROR_PAGE: wpl.LOCATION_CHANGE_ERROR_PAGE,
	},
	isStarted = false;
	mimeTypesRegs = {
		flash : new RegExp(/^application\/x-shockwave-flash/),
		pdf : new RegExp(/^application\/(x-)?pdf/)
	};
	
const	nsIX509CertDB = Ci.nsIX509CertDB,
	nsX509CertDB = "@mozilla.org/security/x509certdb;1",
	DOCUMENT_BLOCKER = "about:document-onload-blocker";

function nsBrowserStatusHandler() {};
nsBrowserStatusHandler.prototype = {
	onStateChange : function(aWebProgress, aRequest, aStateFlags, aStatus) {},
	onStatusChange : function(aWebProgress, aRequest, aStatus, aMessage) {},
	onProgressChange : function(aWebProgress, aRequest, aCurSelfProgress,
							  aMaxSelfProgress, aCurTotalProgress, aMaxTotalProgress) {
	},
	onSecurityChange : function(aWebProgress, aRequest, state) {
	},
	onLocationChange : function(aWebProgress, aRequest, aLocation) {
	},
	QueryInterface : function(aIID) {
			if (aIID.equals(Ci.nsIWebProgressListener) ||
			aIID.equals(Ci.nsISupportsWeakReference) ||
			aIID.equals(Ci.nsIXULBrowserWindow) ||
			aIID.equals(Ci.nsISupports)) {
			return this;
		}
		throw Cr.NS_NOINTERFACE;
	},
	setJSStatus : function(status) {},  
	setJSDefaultStatus : function(status) {},
	setOverLink : function(link) {}
}

let base = null;

this.SebBrowser = {
	init : function(obj) {
		base = this;
		seb = obj;
		certdb = Cc[nsX509CertDB].getService(nsIX509CertDB);
		sl.out("SebBrowser initialized: " + seb);
	},
	
	/*
	browserListener : {
		QueryInterface: XPCOMUtils.generateQI(["nsIWebProgressListener", "nsISupportsWeakReference"]),
		
		onStateChange: function(aWebProgress, aRequest, aFlag, aStatus) {
		// If you use myListener for more than one tab/window, use
		// aWebProgress.DOMWindow to obtain the tab/window which triggers the state change
		if (aFlag) {
			    for (var f in wplFlag) {
				if (wplFlag[f] & aFlag) {
				    Cu.reportError('onStateChange -> wplFlag present = ' + f);
				}
			    }
			}
		},

		onLocationChange: function(aProgress, aRequest, aURI, aFlag) {
		// This fires when the location bar changes; that is load event is confirmed
		// or when the user switches tabs. If you use myListener for more than one tab/window,
		// use aProgress.DOMWindow to obtain the tab/window which triggered the change.
		if (aFlag) {
			for (var f in wplFlag) {
				if (wplFlag[f] & aFlag) {
				    Cu.reportError('onLocationChange -> wplFlag present = ' + f);
				}
			    }
			}
		},

		// For definitions of the remaining functions see related documentation
		onProgressChange: function(aWebProgress, aRequest, curSelf, maxSelf, curTot, maxTot) {},
		onStatusChange: function(aWebProgress, aRequest, aStatus, aMessage) {
			if (aStatus) {
			    Cu.reportError('onStatusChange -> aStatus = ' + aStatus);
			}
			if (aStatus) {
			    Cu.reportError('onStatusChange -> aMessage = ' + aMessage);
			}
		},
		onSecurityChange: function(aWebProgress, aRequest, aState) {}
	},
	*/
	
	stateListener : function(aWebProgress, aRequest, aStateFlags, aStatus) {
		/*
		if ((aStateFlags & startNetworkFlags) == startNetworkFlags) { // start document network event
			sl.debug("DOCUMENT NETWORK START: " + aRequest.name);
			let win = sw.getChromeWin(aWebProgress.DOMWindow);
			base.startLoading(win);
		}
		*/
		/*
		if ((aStateFlags & stopNetworkFlags) == stopNetworkFlags) { // stop network event 
			sl.debug("status: " + aStatus);
		}
		*/
		/*
		if (aStateFlags) {
			for (var f in wplFlag) {
				if (wplFlag[f] & aStateFlags) {
				    sl.debug('onStateChange -> wplFlag present = ' + f);
				}
			}
		}
		*/
		
		if ((aStateFlags & startDocumentFlags) == startDocumentFlags) { // start document request event
			isStarted = true;
			sl.debug("DOCUMENT REQUEST START: " + aRequest.name + " status: " + aStatus);
			let win = sw.getChromeWin(aWebProgress.DOMWindow);
			base.startLoading(win);
			if (seb.quitURL === aRequest.name) {
				aRequest.cancel(aStatus);
				base.stopLoading();
				var tmpQuit = seb.allowQuit; // store default shutdownEnabled
				var tmpIgnorePassword = seb.quitIgnorePassword; // store default quitIgnorePassword
				seb.allowQuit = true; // set to true
				seb.quitIgnorePassword = true;
				seb.quit();									
				seb.allowQuit = tmpQuit; // set default shutdownEnabled
				seb.quitIgnorePassword = tmpIgnorePassword; // set default shutdownIgnorePassword
				return;
			}
			if (!sn.isValidUrl(aRequest.name)) {
				aRequest.cancel(aStatus);
				base.stopLoading();
				prompt.alert(seb.mainWin, su.getLocStr("seb.title"), su.getLocStr("seb.url.blocked"));
				return 1; // 0?
			}
			// PDF Handling
			// don't trigger if pdf is part of the query string: infinite loop
			// don't trigger from pdfViewer itself: infinite loop
			if (su.getConfig("sebPdfJsEnabled","boolean", true) && /^[^\?]+\.pdf$/i.test(aRequest.name) && !sw.winTypesReg.pdfViewer.test(aRequest.name)) {
				sl.debug("pdf start request");
				aRequest.cancel(aStatus);
				sw.openPdfViewer(aRequest.name);
				base.stopLoading();
				return;
			} 
		}
		if ((aStateFlags & stopDocumentFlags) == stopDocumentFlags) { // stop document request event
			sl.debug("DOCUMENT REQUEST STOP: " + aRequest.name + " - status: " + aStatus); 
			let win = sw.getChromeWin(aWebProgress.DOMWindow);
			if (aStatus > 0) { // error: experimental!!!
				sl.debug("Error document loading: " + aStatus);
				base.stopLoading();
				try {
					try {
						aRequest.QueryInterface(Ci.nsIHttpChannel); // no pdf mimetype handling
						let mimeType = aRequest.getResponseHeader("Content-Type");
						if (mimeTypesRegs.pdf.test(mimeType) && !/\.pdf$/i.test(aRequest.name) && su.getConfig("sebPdfJsEnabled","boolean", true)) { // pdf file requests should already catched by SebBrowser
							sl.debug("request already aborted by httpResponseObserver, no error page!");
							isStarted = false;
							return 0;
						}	
					}
					catch (e) { // there is no getResponseHeader function
						switch (e.name) {
							case "NS_ERROR_NOT_AVAILABLE" :
								sl.debug("handled: NS_ERROR_NOT_AVAILABLE");
								break;
							default: 
								sl.debug("not handled: " + e);
						}
					}
					aRequest.cancel(aStatus);
					win.setTimeout(function() {
						if (!isStarted) { // no new start request until now (capturing double clicks on links: experimental)
							let flags = wnav.LOAD_FLAGS_BYPASS_HISTORY; // does not work??? why???
							//win.content.document.location.assign("chrome://seb/content/error.xhtml?req=" + btoa(aRequest.name));
							win.XulLibBrowser.webNavigation.loadURI("chrome://seb/content/error.xhtml?req=" + btoa(aRequest.name), flags, null, null, null);
							isStarted = false;
						}
					}, 100);
					return 0;
				}
				catch(e) {
					sl.debug(e);
				}
				finally {
					aRequest.cancel(aStatus);
					isStarted = false;
					return 1;
				}
			}
			
			base.stopLoading();
			isStarted = false;
			var w = aWebProgress.DOMWindow.wrappedJSObject;
			if (win === seb.mainWin && su.getConfig("sebScreenshot","boolean",false)) {
				sc.createScreenshotController(w);
			}
			if (win === seb.mainWin && su.getConfig("enableBrowserWindowToolbar","boolean",false)) {
				base.refreshNavigation();
			}
			if (su.getConfig("browserScreenKeyboard","boolean",false)) {
				sh.createScreenKeyboardController(win);
			}
		}
	},
	
	locationListener : function(aProgress, aRequest, aURI, aFlag) {},
	
	progressListener : function(aWebProgress, aRequest, curSelf, maxSelf, curTot, maxTot) {},
	
	statusListener : function(aWebProgress, aRequest, aStatus, aMessage) {
		if (aStatus) {
			sl.debug("status: " + aStatus + " : " + aMessage);
		}	
	},
	
	securityListener : function(aWebProgress, aRequest, aState) {},
	
	getBrowser : function (win) {
		try { return win.document.getElementById("seb.browser"); }
		catch(e) { sl.err(e) }
	},
	
	setBrowserHandler : function setBrowserHandler(win) { // Event handler for both wintypes
		sl.debug("setBrowserHandler");
		try {
			win.XULBrowserWindow.onStateChange = base.stateListener;
			win.XULBrowserWindow.onLocationChange = base.locationListener;
			win.XULBrowserWindow.onProgressChange = base.progressListener;
			win.XULBrowserWindow.onStatusChange = base.statusListener;
			win.XULBrowserWindow.onSecurityChange = base.securityListener;
		}
		catch(e) {
			sl.err("Error in setBrowserListener: " + e);
		}
	},
	
	initBrowser : function (win) {
		if (!win) {
			sl.err("wrong arguments for initBrowser(win)");
			return false;
		}
		var br = base.getBrowser(win);
		
		if (!br) {
			sl.debug("no seb.browser in ChromeWindow!");
			return false;
		}	
		win.XulLibBrowser = br; // extend window property to avoid multiple getBrowser() calls
		win.XULBrowserWindow = new nsBrowserStatusHandler();
		// hook up UI through progress listener
		var interfaceRequestor = win.XulLibBrowser.docShell.QueryInterface(Ci.nsIInterfaceRequestor);
		var webProgress = interfaceRequestor.getInterface(Ci.nsIWebProgress);
		webProgress.addProgressListener(win.XULBrowserWindow, Ci.nsIWebProgress.NOTIFY_ALL);
		nav = win.XulLibBrowser.webNavigation;
		sl.debug("initBrowser");
	},
	
	addCert : function(cert) {
		try {
			let flags = ovs.ERROR_UNTRUSTED | ovs.ERROR_MISMATCH | ovs.ERROR_TIME;
			let x509 = certdb.constructX509FromBase64(cert.certificateData);
			//certlist.addCert(x509); // maybe needed for type 1 Identity Certs
			let host = cert.name;
			let port = 443;
			let fullhost = cert.name.split(":");
			if (fullhost.length==2) {
				host = fullhost[0];
				port = parseInt(fullhost[1]);
			}
			ovs.rememberValidityOverride(host,port,x509,flags,true);
			sl.debug("add cert: " + host + ":" + port + "\n" + cert.certificateData);
		}
		catch (e) { sl.err(e); s}
	},
	
	setEmbeddedCerts : function() {
		let certs = seb.config["embeddedCertificates"];
		if ( typeof certs != "object") { return; }
		sl.debug("setEmbeddedCerts");
		for (var i=0;i<certs.length;i++) {
			base.addCert(certs[i]);
		}
	},
	
	loadPage : function (win,url,flag) {	// only use for real http requests
		sl.debug("try to load: " + url);
		//win.XulLibBrowser.webNavigation.loadURI(url, wnav.LOAD_FLAGS_BYPASS_HISTORY, null, null, null);
		//win.XulLibBrowser.setAttribute("src", url);
		win.content.document.location.href = url;
		/*
		if (loadFlag === null) {
			if (su.getConfig("mainBrowserBypassCache","boolean",false)) {
				loadFlag |= wnav.LOAD_FLAGS_BYPASS_CACHE;
			}
			if (!su.getConfig("allowBrowsingBackForward","boolean",false)) {
				loadFlag |= wnav.LOAD_FLAGS_BYPASS_HISTORY;
			}
		}
		
		let f = (flag === "undefined") ? loadFlag : flag; 
		if (!win.XulLibBrowser) {
			sl.err("no seb.browser in ChromeWindow!");
			return false;
		}
		if (typeof flag == "undefined") {
    			flag = loadFlag;
		}
		*/ 
		//win.XulLibBrowser.webNavigation.loadURI(url, wnav.LOAD_FLAGS_REPLACE_HISTORY, null, null, null); 
	},
	
	reload : function (win) {
		sl.debug("try reload...");
		if (!win.XulLibBrowser) {
			sl.err("no xullib.browser in ChromeWindow!");
			return false;
		}
		win.XulLibBrowser.webNavigation.reload(wnav.LOAD_FLAGS_BYPASS_CACHE);
	},
	
	restart : function() {
		if (!su.getConfig("mainBrowserRestart","boolean",true)) {
			sl.debug("restart not allowed.");
			return;
		}
		sl.debug("restart...");
		sw.removeSecondaryWins();
		let url = su.getUrl();
		//sw.showLoading(seb.mainWin);
		base.loadPage(seb.mainWin,url);
	},
	
	hostRestartUrl : function () {
		let url = su.getConfig("restartExamURL","string","");
		if (url == "") {
			sl.err("no restart url from host");
			return;
		}
		sl.debug("host restart url: " + url);
		sw.removeSecondaryWins();
		//sw.showLoading(seb.mainWin);
		base.loadPage(seb.mainWin,url);
	},
	
	load : function() {
		//mainBrowserLoad
		//mainBrowserLoadReferrer
		sl.debug("try to load from command...");
		let loadUrl = su.getConfig("mainBrowserLoad","string","");
		let loadReferrerInString = su.getConfig("mainBrowserLoadReferrerInString","string","");
		if (loadUrl == "") {
			sl.debug("no mainBrowserLoad defined.");
			return;
		}
		let doc = seb.mainWin.content.document;
		let loadReferrer = doc.location.href;
		if (loadReferrerInString != "") {
			if (loadReferrer.indexOf(loadReferrerInString) > -1) {
				base.loadPage(seb.mainWin,loadUrl);
			}
			else {
					
				sl.debug("loading \"" + loadUrl + "\" is only allowed if string in referrer: \"" + loadReferrerInString + "\"");
				return false;
			}
		}
		else {
			sl.debug("load from command " + loadUrl);
			base.loadPage(seb.mainWin,loadUrl);
		}
	},
	
	startLoading : function(win) {
		try {
			win = (win) ? win : seb.mainWin;
			win.document.getElementById('loadingBox').className = "visible";
		}
		catch (e) {
			sl.debug("error startLoading: " + e);
		}
	},
	
	stopLoading : function() { // stop loading on all windows 
		try {
			for (var i=0;i<sw.wins.length;i++) {
				sw.wins[i].document.getElementById('loadingBox').className = "hidden";
			}	
		}
		catch (e) {
			sl.debug("error stopLoading: " + e);
		}
	}, 
	
	back : function() {
		if (!su.getConfig("allowBrowsingBackForward","boolean",false)) {
			sl.debug("navigation: back not allowed")
			return; 
		}
		if (nav.canGoBack) {
			sl.debug("navigation: back");	
			nav.goBack();
		}
	},
	
	forward : function() {
		if (!su.getConfig("allowBrowsingBackForward","boolean",false)) { 
			sl.debug("navigation: forward not allowed");	
			return; 
		}
		if (nav.canGoForward) {
			sl.debug("navigation: forward");	
			nav.goForward();
		}		
	},
	
	refreshNavigation : function() {
		sl.debug("refreshNavigation");
		if (su.getConfig("allowBrowsingBackForward","boolean",false)) { // should be visible
			var back = seb.mainWin.document.getElementById("btnBack");
			var forward = seb.mainWin.document.getElementById("btnForward");
			if (nav.canGoBack) {
				sl.debug("canGoBack");
				back.className = "tbBtn enabled";
				back.addEventListener("click",base.back,false);
			}
			else {
				back.className = "tbBtn disabled";
				back.removeEventListener("click",base.back,false);
			}
			if (nav.canGoForward) {
				sl.debug("canGoForward");
				forward.className = "tbBtn enabled";
				forward.addEventListener("click",base.forward,false);
			}
			else {
				forward.className = "tbBtn disabled";
				forward.removeEventListener("click",base.forward,false);
			}
		}
	}
}
