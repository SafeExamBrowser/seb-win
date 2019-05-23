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
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu, Constructor: CC } = Components,
	{ prompt, scriptloader, obs, appinfo } = Cu.import("resource://gre/modules/Services.jsm").Services,
	{ OS } = Cu.import("resource://gre/modules/osfile.jsm"),
	{ SpellCheckHelper } = Cu.import("resource://gre/modules/InlineSpellChecker.jsm");
	
Cu.import("resource://gre/modules/XPCOMUtils.jsm");
Cu.import("resource://gre/modules/NetUtil.jsm");
Cu.import("resource://gre/modules/FileUtils.jsm");
Cu.import("resource://gre/modules/InlineSpellCheckerContent.jsm");
Cu.importGlobalProperties(['Blob']);
/* Services */
let	wpl = Ci.nsIWebProgressListener,
	wnav = Ci.nsIWebNavigation,
	ovs = Cc["@mozilla.org/security/certoverride;1"].getService(Ci.nsICertOverrideService);

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");
scriptloader.loadSubScript("resource://globals/const.js");

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"su","resource://modules/SebUtils.jsm","SebUtils");
XPCOMUtils.defineLazyModuleGetter(this,"sl","resource://modules/SebLog.jsm","SebLog");
XPCOMUtils.defineLazyModuleGetter(this,"sw","resource://modules/SebWin.jsm","SebWin");
XPCOMUtils.defineLazyModuleGetter(this,"sn","resource://modules/SebNet.jsm","SebNet");
XPCOMUtils.defineLazyModuleGetter(this,"sh","resource://modules/SebHost.jsm","SebHost");
XPCOMUtils.defineLazyModuleGetter(this,"sg","resource://modules/SebConfig.jsm","SebConfig");
XPCOMUtils.defineLazyModuleGetter(this,"sc","resource://modules/SebScreenshot.jsm","SebScreenshot");

/* ModuleGlobals */
let 	base = null,
	authMgr = null,
	cookieMgr = null,
	historySrv = null,
	seb = null,
	certdb = null,
	certdb2 = null,
	loadFlag = null,
	startDocumentFlags = wpl.STATE_IS_REQUEST | wpl.STATE_IS_DOCUMENT | wpl.STATE_START,
	stopDocumentFlags =  wpl.STATE_IS_WINDOW | wpl.STATE_STOP,
	//stopDocumentFlags = wpl.STATE_IS_DOCUMENT | wpl.STATE_STOP,
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
	mimeTypesRegs = { 
		flash : new RegExp(/^application\/x-shockwave-flash/),
		pdf : new RegExp(/^application\/(x-)?pdf/)
	},
	sebReg = new RegExp(/.*?\.seb/i),
	httpReg = new RegExp(/^http\:/i),
	windowTitleSuffix = "";
	
	/*
	installedDics = {
		"de-DE":"de-DE@dictionaries.addons.mozilla.org",
		"de-CH":"de_CH@dicts.j3e.de",
		"en-US":"en-US@dictionaries.addons.mozilla.org",
		"en-GB":"marcoagpinto@mail.telepac.pt",
		"fr-classic":"fr-dicollecte@dictionaries.addons.mozilla.org"
	};
	*/  
	
const	nsIX509CertDB = Ci.nsIX509CertDB,
	nsIX509CertDB2 = Ci.nsIX509CertDB2,
	nsX509CertDB = "@mozilla.org/security/x509certdb;1",
	DOCUMENT_BLOCKER = "about:document-onload-blocker",
	CERT_SSL = 0,
	CERT_USER = 1, //reserved to windows host
	CERT_CA = 2,
	CERT_SSL_DEBUG = 3;
	
function nsBrowserStatusHandler() {};
nsBrowserStatusHandler.prototype = {
	browser : null,
	originRequestURI : null,
	mainPageURI : null,	
	baseurl : null,
	lastSuccess : null,
	referrer : null,
	progress : null,
	request : null,
	flags : null,
	status : null,
	wintype : null,
	
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
	getLoadContext: function (aChannel) {
		if (!aChannel)
		    return null;

		let notificationCallbacks =
			aChannel.notificationCallbacks ? aChannel.notificationCallbacks : aChannel.loadGroup.notificationCallbacks;

		if (!notificationCallbacks) {
		    return null;
		}
		try {
		    if(notificationCallbacks.getInterface(Ci.nsIXMLHttpRequest)) {
			// ignore requests from XMLHttpRequest
			return null;
		    }
		}
		catch(e) { }
		try {
		    return notificationCallbacks.getInterface(Ci.nsILoadContext);
		}
		catch (e) {}
		return null;
	},
	isFromMainWindow: function (loadContext) {
		if (loadContext  && loadContext.isContent && this.browser.contentWindow == loadContext.associatedWindow) {
			return true;
		}
		else {
			return false;
		}
	},

	isLoadRequested: function(flags) {
		return (
		    flags & Ci.nsIWebProgressListener.STATE_START &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_NETWORK &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_WINDOW
		)
	},
	
	isLoadRessource : function(flags) {
		return (
		    flags & Ci.nsIWebProgressListener.STATE_START &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_REQUEST
		)
	},
	
	isLoadedRessource : function(flags) {
		return (
		    flags & Ci.nsIWebProgressListener.STATE_STOP &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_REQUEST
		)
	},
	
	isStart: function(flags) {
		return (
		    flags & Ci.nsIWebProgressListener.STATE_TRANSFERRING &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_REQUEST &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_DOCUMENT
		);
	},
	
	isRedirectionStart: function(flags) {
		return (
		    flags & Ci.nsIWebProgressListener.STATE_START &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_REQUEST &&
		    this.redirecting
		);
	},

	isTransferDone: function(flags) {
		return (
		    flags & Ci.nsIWebProgressListener.STATE_STOP &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_REQUEST
		);
	},

	isLoaded: function(flags) {
		return (
		    flags & Ci.nsIWebProgressListener.STATE_STOP &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_NETWORK &&
		    flags & Ci.nsIWebProgressListener.STATE_IS_WINDOW
		);
	},
	setJSStatus : function(status) {},  
	setJSDefaultStatus : function(status) {},
	setOverLink : function(link) {}
}

this.SebBrowser = {
	//lastDocumentUrl : null,
	quitURLRefererFilter : "",
    dialog : null,
    dialogHandler : function(message) {
        if (this.dialog) {
            this.dialog(message);
        }
    },
	init : function(obj) {
		base = this;
		seb = obj;
		certdb = Cc[nsX509CertDB].getService(nsIX509CertDB);
		let _certs = certdb.getCerts();
		//base.disableBuiltInCerts();
		authMgr = Cc["@mozilla.org/network/http-auth-manager;1"].getService(Ci.nsIHttpAuthManager); // clearAll
		cookieMgr = Cc["@mozilla.org/cookiemanager;1"].getService(Ci.nsICookieManager); // removeAll
		historySrv = Cc["@mozilla.org/browser/nav-history-service;1"].getService(Ci.nsIBrowserHistory); // removeAllPages
		sl.out("SebBrowser initialized: " + seb);
	},
	
	stateListener : function(progress, request, flags, status) {
		if (!(request instanceof Ci.nsIChannel || "URI" in request)) {
		    // ignore requests that are not a channel
		    return
		}
		let uri = request.URI.spec.replace(/\/$/,"");
		let loadContext = this.getLoadContext(request);
		if (!this.isFromMainWindow(loadContext)) {
			if (this.isLoadRequested(flags)) {
				sl.info("Frame loading: " + uri);
				//base.startLoading(sw.getChromeWin(progress.DOMWindow));
			}
			else if (this.isLoaded(flags)) {
				sl.info("Frame loaded: " + uri);
				//base.stopLoading(sw.getChromeWin(progress.DOMWindow));
			}
			return;
		}
		
		try {
			if (this.mainPageURI == null) { // new window request
				if (this.isLoadRequested(flags)) {
					// QueryInterface nsIHttpChannel  for async seb file download
					try {
						request.QueryInterface(Ci.nsIHttpChannel);
						if (request.getRequestHeader(SEB_FILE_HEADER)) {
							sl.debug("seb file request ist handled by http request observer...")
							return;
						} 
					}
					catch(e) {}
					sl.debug("load uri: " + uri); 
					this.request = request;
					this.progress = progress;
					this.flags = flags;
					this.status = status;
					this.wintype = sw.getWinType(sw.getChromeWin(progress.DOMWindow));
					if (!sw.winTypesReg.errorViewer.test(progress.DOMWindow.document.URL)) {
						this.referrer = progress.DOMWindow.document.URL;
					}
					this.originRequestURI = request.URI;
					this.mainPageURI = request.URI;
					
					if (seb.quitURL == uri) {
						if (base.quitURLRefererFilter != "") {
							let filter = base.quitURLRefererFilter;
							
							if (this.referrer.indexOf(filter) < 0) {
								sl.debug("quitURL \"" + seb.quitURL + "\" is only allowed if string in referrer: \"" + filter + "\"");
								this.onStatusChange(progress, request, STATUS_QUIT_URL_WRONG_REFERRER.status, STATUS_QUIT_URL_WRONG_REFERRER.message);
								return;
							}
						}
						this.onStatusChange(progress, request, STATUS_QUIT_URL_STOP.status, STATUS_QUIT_URL_STOP.message);
						return;
					}
                    
                    //if (seb.clearClipboardUrl == uri) {
                    if (seb.clearClipboardUrlRegex.test(uri)) {
                        this.onStatusChange(progress, request, STATUS_CLEAR_CLIPBOARD_URL_STOP.status, STATUS_CLEAR_CLIPBOARD_URL_STOP.message);
						return;
                    }
					
					for (var key in seb.ars) {
						if (seb.ars[key].checkTrigger(uri,progress.DOMWindow.document.URL)) {
							if (seb.ars[key].isLink) {
								
								this.onStatusChange(progress, request, STATUS_LOAD_AR.status, key);
								return;
							}
							else {
								seb.loadAR(sw.getChromeWin(progress.DOMWindow), key);
							}	
						}
					}
					 
					// special chrome pdfViewer
					if (sw.winTypesReg.pdfViewer.test(uri)) {
						sl.debug(PDF_VIEWER_TITLE);
						return;
					}
					// special chrome errorPage
					if (sw.winTypesReg.errorViewer.test(uri)) {
						sl.debug(ERROR_PAGE_TITLE);
						return;
					}
					
					if (!sn.isValidUrl(uri)) {
						this.onStatusChange(progress, request, STATUS_INVALID_URL.status, STATUS_INVALID_URL.message);
						return;
					}
				
					if (httpReg.test(uri)) {
						if (sn.blockHTTP) {
							this.onStatusChange(progress, request, STATUS_BLOCK_HTTP.status, STATUS_BLOCK_HTTP.message);
							return;	
						}
					}
					
					// PDF Handling
					// don't trigger if pdf is part of the query string: infinite loop
					// don't trigger from pdfViewer itself: infinite loop
					if (su.getConfig("sebPdfJsEnabled","boolean", true) && /^[^\?]+\.pdf$/i.test(uri)) {
						sl.debug("redirect pdf start request");
						this.onStatusChange(progress, request, STATUS_PDF_REDIRECT.status, STATUS_PDF_REDIRECT.message);
						return;
					}
					return;
				}
				else if (this.isRedirectionStart(flags)) {
					sl.debug("redirected uri: " + uri);
					this.request = request;
					this.progress = progress;
					this.flags = flags;
					this.status = status; 
					this.wintype = sw.getWinType(sw.getChromeWin(progress.DOMWindow));
					this.redirecting = false;
					this.mainPageURI = request.URI;
					if (!sn.isValidUrl(uri)) {
						this.onStatusChange(progress, request, STATUS_INVALID_URL.status, STATUS_INVALID_URL.message);
						return;
					}
					return;
				}
				else if (this.isFromMainWindow(loadContext)) { // loading ressources after main request is loaded
					if (this.isLoadRessource(flags)) {
						sl.info("late request start: " + uri);
					}
					if (this.isLoadedRessource(flags)) {
						sl.info("late request stop: " + uri);
					}
					return;
				}
				else {
					sl.info("main request not started yet uri: " + uri);
					return;
				}
				return;
			}
			
			if (!this.mainPageURI.equalsExceptRef(request.URI)) {
				sl.info("request ignored: " + uri);
				return;
			}
 
			//sl.debug("main request: " + uri);
			if (this.isStart(flags)) {
				sl.debug("main request transfer started: " + uri);
				base.startLoading(sw.getChromeWin(progress.DOMWindow));
				this.baseurl = btoa(this.originRequestURI.spec.replace(/\/$/,"")); // origin (not redirected) requests URI for identifiying distinct windows
				sl.debug("baseurl: "+ this.originRequestURI.spec.replace(/\/$/,"") + " : " + this.baseurl);
				this.request = request;
				this.progress = progress;
				this.flags = flags;
				this.status = status; 
				this.wintype = sw.getWinType(sw.getChromeWin(progress.DOMWindow));
				return;
			}
			if (this.isTransferDone(flags)) {
				sl.debug("main request transfer done: " + uri);
				return;
			}
			if (this.isLoaded(flags)) {
				// ignore custom tracing status
				if (request && request.status && (request.status > 0 && request.status < 10)) {
					switch (request.status) {
						case STATUS_PDF_REDIRECT.status :
						case STATUS_QUIT_URL_STOP.status :
						case STATUS_QUIT_URL_WRONG_REFERRER.status :
						case STATUS_BLOCK_HTTP.status :
						case STATUS_INVALID_URL.status :
						case STATUS_REDIRECT_TO_SEB_FILE_DOWNLOAD_DIALOG.status :
                        case STATUS_CLEAR_CLIPBOARD_URL_STOP.status :
							sl.debug("custom request stop: " + uri + " status: " + request.status);
						return;
					}
				}
				sl.debug("main request loaded: " + request.name);
				let win = sw.getChromeWin(progress.DOMWindow);
				let domWin = progress.DOMWindow;
				base.stopLoading(win);
				this.mainPageURI = null;
				
				// sl.debug("wintype: " + this.wintype);
				// ReconfDialog
				/*
				if (this.wintype == RECONFIG_TYPE) {
					sl.debug("reconf: " + this.wintype);
					return;
				}
				*/
				//
				if (!sw.winTypesReg.errorViewer.test(domWin.document.URL)) {
					this.referrer = domWin.document.URL;
				}
				
				if (sw.winTypesReg.pdfViewer.test(uri)) {
					let title = PDF_VIEWER_TITLE + ": " + win.XulLibBrowser.contentDocument.title;
					win.document.title = (windowTitleSuffix == '') ? title : title + " - " + windowTitleSuffix;
					return;
				}
				
				if (sw.winTypesReg.errorViewer.test(uri)) {
					win.document.title = (windowTitleSuffix == '') ? ERROR_PAGE_TITLE : ERROR_PAGE_TITLE + " - " + windowTitleSuffix;
					return;
				}
				
				// QueryInterface nsIHttpChannel for request.responseStatus request.requestSucceeded
				try {
					request.QueryInterface(Ci.nsIHttpChannel);
				}
				catch(e) { // local chrome urls and other. Maybe something to do like handling of cached ressources?
					sl.debug("Error QueryInterface Ci.nsIHttpChannel");
					sl.debug(uri);
					return;
				}
				
				let reqErr = false;
				let reqStatus = false; // throw err notAvailable if failed
				let reqSucceeded = false; // possible content but not 200 status p.e. 404 Custom server page reqSucceeded = false, is skipped for lastSuccess bur not processed for errorPage
				let notAvailable = false; // request did not started
				let contentLength = 1; // use only if explicit value = 0
				let contentType = "";
				try {
					reqStatus = request.responseStatus;
					sl.debug("reqStatus: " + reqStatus);
					reqSucceeded = request.requestSucceeded;
					sl.debug("reqSucceeded: " + reqSucceeded);
					try {
						contentType = request.getResponseHeader("content-type");
						sl.debug("contentType: " + contentType);
					}
					catch(e) { }
					try {
						contentLength = request.getResponseHeader("content-length");
						sl.debug("contentLength: " + contentLength);
					}
					catch(e) { }
					// a simple workaround for the errorpage back button that always links to the last page with successful response
					if (reqSucceeded) {
						this.lastSuccess = uri;
					}
					else { 
						this.referrer = this.lastSuccess;
					}
				}
				catch(e) {
					reqErr = e;
					sl.debug("reqErr.result: " + reqErr.result);
					notAvailable = (reqErr.result === Cr.NS_ERROR_NOT_AVAILABLE);
					sl.debug("notAvailable: " + notAvailable);
				}
				
				let showErrorPage = false;
				if (su.getConfig("sebErrorPage","boolean",true)) { // only enable if config set
					if (reqErr || !reqStatus || contentLength == 0) { // any error?
						if (this.lastSuccess === null) { // firstPage failed, no referrer -> showing ErrorPage is better than blank page
							showErrorPage = true;
						}
						else {
							if (!notAvailable) { // show ErrorPage only if request is already started and aborted or any error response status from server but with html output
								showErrorPage = true;
							}
						}
					}
					else {
						showErrorPage = false;
					}
				}
				
				if (showErrorPage) {
					request.cancel(status);
					win.setTimeout(function() {
						if (this.XULBrowserWindow.mainPageURI == null) { // no new start request until now (capturing double clicks on links: experimental)
							progress.DOMWindow.location.replace("chrome://seb/content/error.xhtml?req=" + btoa(uri) + "&ref=" + btoa(this.XULBrowserWindow.referrer));
							this.XULBrowserWindow.mainPageURI = null;
						}
					}, 100);
					return;
				}
				else {
					sl.debug("document request stop " + uri + " status: " + status);
					try { // main window focus?
						win.focus();
					}
					catch(e) {
						sl.err("Fokus Error: " + e);
					}
				}
				
				var w = domWin.wrappedJSObject;
			
				try {
					win.document.title = (windowTitleSuffix == '') ? win.XulLibBrowser.contentDocument.title : win.XulLibBrowser.contentDocument.title + " - " + windowTitleSuffix;
				}
				catch(e) {
					sl.debug(e);
				}
				if (su.getConfig("enableBrowserWindowToolbar","boolean",false)) {
					base.refreshNavigation(win);
				} 
				return;
			}
			if (flags & Ci.nsIWebProgressListener.STATE_REDIRECTING) {
				this.redirecting = true;
				this.mainPageURI = null;
				request.QueryInterface(Ci.nsIHttpChannel);
				sl.debug("main request redirect from "+request.name);
				return;
			}
		}
		catch(e) {
			sl.err(e);
		}
	},
	
	locationListener : function(aProgress, aRequest, aURI, aFlag) {
		//let w = sw.getChromeWin(aProgress.DOMWindow);
		//let br = w.XULBrowserWindow;
		//sl.debug("LOCATION CHANGE aURI: " + aURI.spec);
		//sl.debug("LOCATION CHANGE aRequest.name: " + aRequest.name);
		//br.isStarted = true;
	},
	
	progressListener : function(aWebProgress, aRequest, curSelf, maxSelf, curTot, maxTot) {},
	
	statusListener : function(progress, request, status, message) {
		if (seb.reconfWinStart) { // don't track anything in reconf transaction
			return;
		}
		if (!(request instanceof Ci.nsIChannel || "URI" in request)) {
		    // ignore requests that are not a channel
		    return
		}
		if (status > 0 && status < 10) {
			sl.debug("custom request handling: " + status + " - " + message);
			let uri = request.URI.spec.replace(/\/$/,"");
			switch (status) {
				case STATUS_PDF_REDIRECT.status :
					request.cancel(status);
					this.mainPageURI = null;
					base.stopLoading(sw.getChromeWin(progress.DOMWindow));
					sw.openPdfViewer(uri);
					break;  
				case STATUS_QUIT_URL_STOP.status :
					request.cancel(status);
					this.mainPageURI = null;
					base.stopLoading(sw.getChromeWin(progress.DOMWindow));
					if (seb.quitURL == seb.url.replace(/\/$/,"")) {
						seb.hostForceQuit = true;
						seb.quitIgnoreWarning = true;
						seb.quitIgnorePassord = true;
						seb.allowQuit = true;
						seb.quit();
					}
					else {
						var tmpQuit = seb.allowQuit; // store default shutdownEnabled
						var tmpIgnorePassword = seb.quitIgnorePassword; // store default quitIgnorePassword
						seb.allowQuit = true; // set to true
						seb.quitIgnorePassword = true;
						seb.quit();
						seb.allowQuit = tmpQuit; // set default shutdownEnabled
						seb.quitIgnorePassword = tmpIgnorePassword; // set default shutdownIgnorePassword
					}
					break;
				case STATUS_QUIT_URL_WRONG_REFERRER.status : 
					request.cancel(status);
					this.mainPageURI = null;
					base.stopLoading(sw.getChromeWin(progress.DOMWindow));
					break;
				case STATUS_LOAD_AR.status : // maybe deprecated
					request.cancel(status);
					this.mainPageURI = null;
					base.stopLoading(sw.getChromeWin(progress.DOMWindow));
					seb.loadAR(sw.getChromeWin(progress.DOMWindow), message);
					break;
				case  STATUS_INVALID_URL.status :
					request.cancel(status);
					this.mainPageURI = null;
					let w = sw.getChromeWin(progress.DOMWindow);
					base.stopLoading(w);
					if (sw.getWinType(w) !== "main") {
						w.close();
						seb.mainWin.focus();
					}
					prompt.alert(seb.mainWin, su.getLocStr("seb.title"), su.getLocStr("seb.url.blocked"));
					break;
				case STATUS_BLOCK_HTTP.status :
					request.cancel(status);
					this.mainPageURI = null;
					base.stopLoading(sw.getChromeWin(progress.DOMWindow));
					prompt.alert(seb.mainWin, su.getLocStr("seb.title"), su.getLocStr("seb.url.blocked"));
					break;
                case STATUS_CLEAR_CLIPBOARD_URL_STOP.status :
                    request.cancel(status);
					this.mainPageURI = null;
					base.stopLoading(sw.getChromeWin(progress.DOMWindow));
                    seb.clearClipboard();
                    break;
			}
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
		// 	see: http://bugs.conkeror.org/issue513
		//	https://hg.mozilla.org/mozilla-central/diff/6a563348b8be/toolkit/components/passwordmgr/nsLoginManagerPrompter.js
		// 	gBrowser is the gloabel tabbrowser element in firefox: https://developer.mozilla.org/en-US/docs/Mozilla/Tech/XUL/tabbrowser
		//	for inspection:
		//	chrome://browser/content/browser.xul
		//	chrome://browser/content/browser.js
		win.gBrowser = {
			getBrowserForContentWindow : function (aWin) {
				return br;
			}
		}
		win.XulLibBrowser = br; // extend window property to avoid multiple getBrowser() calls
		win.XULBrowserWindow = new nsBrowserStatusHandler();
		win.XULBrowserWindow.browser = br;
		
		// hook up UI through progress listener
		var interfaceRequestor = win.XulLibBrowser.docShell.QueryInterface(Ci.nsIInterfaceRequestor);
		var webProgress = interfaceRequestor.getInterface(Ci.nsIWebProgress);
		webProgress.addProgressListener(win.XULBrowserWindow, Ci.nsIWebProgress.NOTIFY_ALL);
		sl.debug("initBrowser");
	},
	
	initSecurity : function () {
		windowTitleSuffix = su.getConfig("browserWindowTitleSuffix","string","");
		base.quitURLRefererFilter = su.getConfig("quitURLRefererFilter","string","");
		if (su.getConfig("sebDisableOCSP","boolean",true)) {
			sg.setPref("security.OCSP.enabled",0);
		}
		else {
			sg.setPref("security.OCSP.enabled",1);
		}
		switch (su.getConfig("sebSSLSecurityPolicy","number",SSL_SEC_BLOCK_MIXED_ACTIVE)) {
			case SSL_SEC_NONE : // allow mixed content
				sg.setPref("security.mixed_content.block_active_content",false);
				sg.setPref("security.mixed_content.block_display_content",false);
				break;
			case SSL_SEC_BLOCK_MIXED_ACTIVE :
				sg.setPref("security.mixed_content.block_active_content",true);
				sg.setPref("security.mixed_content.block_display_content",false);
				break;
			case SSL_SEC_BLOCK_MIXED_ALL :
				sg.setPref("security.mixed_content.block_active_content",true);
				sg.setPref("security.mixed_content.block_display_content",true);
				break;
		}
		if (su.getConfig("pinEmbeddedCertificates","boolean",false)) {
			base.disableBuiltInCerts();
		}
		// set proxy auth
		if (seb.config.proxies && seb.config.proxies.Auth) {
			let authType = seb.config.proxies.AuthType;
			let realm = seb.config.proxies.Realm;
			let host = seb.config.proxies.HTTPProxy;
			let port = seb.config.proxies.HTTPPort;
			let usr = seb.config.proxies.User;
			let pwd = seb.config.proxies.Password;
			sl.debug("set proxies auth: " + host + "://" + usr + ":" + pwd + "@" + host + ":" + port);
			if (!seb.config.proxies.User || !seb.config.proxies.Password) {
				sl.debug("No proxy user or password defined");
				return;
			}
			if (!seb.config.proxies.HTTPEnable && !seb.config.proxies.HTTPSEnable) {
				sl.debug("No http proxy enabled");
				return;
			}
			if (!seb.config.proxies.HTTPProxy && !seb.config.proxies.HTTPPort) {
				sl.debug("No http proxy or port defined");
				return;
			}
			try {
				authMgr.setAuthIdentity("http",host,port,authType,realm,null,null,usr,pwd);
			}
			catch(e) {
				sl.err(e);
			}
		}	
	},
	
	initSpellChecker : function() {
		sl.debug("initSpellChecker");
		// if allowSpellCheck false, abort
		if (!su.getConfig("allowSpellCheck","boolean",false)) {
			sl.debug("spell checking is not allowed");
			return;
		}
		// get array of allowSpellCheckDictionary in config (must be an array NOT comma seperated list) 
		let allowedDics = su.getConfig("allowSpellCheckDictionary","object",[]);
		// abort if no allowed dictionaries are defined
		if (allowedDics.length == 0) {
			sl.debug("no allowed dictionaries defined");
			return;
		}
		var spellclass = "@mozilla.org/spellchecker/myspell;1";
		spellclass = "@mozilla.org/spellchecker/engine;1";
		let spe = Cc[spellclass].getService(Ci.mozISpellCheckingEngine);
		let dics = [];
		let dicsPath = su.getDicsPath();
		let dicsDir = new FileUtils.File(dicsPath);
		// try to load all allowSpellCheckDictionary from app/dictionaries/DIC_NAME/* 
		// requires exact matching of allowSpellCheckDictionary in config array and DIC_NAME!
		for (var i=0; i<allowedDics.length; i++) {
			let dic = allowedDics[i];
			let dicDir = new FileUtils.File(OS.Path.join(dicsDir.path,dic));
			if (dicDir.exists()) {
				spe.addDirectory(dicDir);
				 // set first as default spellcheck language, but is overridden by seb.locale if exists
				if (i == 0) {
					spe.dictionary = dic;
				}
			}
			else {
				sl.debug("dictionary " + dic + " does not exist, maybe provided in external dics path...")
			}
		}

		let xDicsPath = su.getExternalDicsPath();
		let xDicsDir = new FileUtils.File(xDicsPath);
		if (xDicsDir.exists()) {
			for (var i=0; i<allowedDics.length; i++) {
				let dic = allowedDics[i];
				let dicDir = new FileUtils.File(OS.Path.join(xDicsDir.path,dic));
				if (dicDir.exists()) {
					spe.addDirectory(dicDir);
				}
				else {
					//sl.debug("dictionary " + dic + " does not exist")
				}
			}
		}
		spe.getDictionaryList(dics,{});
		if (dics.value.includes(seb.locale) && allowedDics.includes(seb.locale)) {
			sl.debug("override default spellcheck language with seb.locale: " + seb.locale);
			spe.dictionary = seb.locale;
		}
		sl.debug("available dictionaries: " + dics.value);
		try {
            sl.debug("current spellcheck language: " + spe.language);
        }
        catch(e) {
            sl.debug("no current spellcheck language?");
        }
	},
	
	addAdditionalDictionaries : function(opt) {
		sl.debug("addDictionaries: " + opt.path);
		// if allowSpellCheck false, abort
		// DANIEL: abort even on websocket event?
		if (!su.getConfig("allowSpellCheck","boolean",false)) {
			sl.debug("spell checking is not allowed");
			return;
		}
		// get array of allowSpellCheckDictionary in config (must be an array NOT comma seperated list) 
		// DANIEL: abort even on websocket event?
		let allowedDics = su.getConfig("allowSpellCheckDictionary","object",[]);
		// abort if no allowed dictionaries are defined
		if (allowedDics.length == 0) {
			sl.debug("no allowed dictionaries defined");
			return;
		}
		var spellclass = "@mozilla.org/spellchecker/myspell;1";
		spellclass = "@mozilla.org/spellchecker/engine;1";
		let spe = Cc[spellclass].getService(Ci.mozISpellCheckingEngine);
		let dics = [];
		let dicsPath = opt.path;
		let dicsDir = new FileUtils.File(dicsPath);
		if (!dicsDir.exists() || !dicsDir.isDirectory()) {
			sl.err('invalid dictionary path: ' + dicsPath)
		}
		let entries = dicsDir.directoryEntries;						
		while(entries.hasMoreElements()) {
			let entry = entries.getNext();
			entry.QueryInterface(Components.interfaces.nsIFile);
			// DANIEL: process config entry allowSpellCheckDictionary?
			if (entry.isDirectory() && allowedDics.includes(entry.leafName)) {
				sl.debug('add addtional dictionary ' + entry.leafName)
				spe.addDirectory(entry);
			}
		} 
		spe.getDictionaryList(dics,{});
		sl.debug("available dictionaries: " + dics.value);
	},
	
	initReconf : function(win,url,handler) {
		sl.debug("reconfigure started");
		base.initBrowser(win);
		//seb.reconfState = RECONF_START;
        base.dialog = handler;
		//base.setBrowserHandler(win);
		base.loadPage(win,url);
	},
	
	abortReconf : function(win) {
        if (seb.reconfState == RECONF_PROCESSING) {
            sl.debug("reconfigure abort ignored - processing seb file");
            base.dialogHandler("Waiting to finish SEB reconfiguration");
        } else {
            sl.debug("reconfigure aborted");
            seb.reconfState = RECONF_ABORTED;
            sh.sendReconfigureAborted();
            win.close();
        }
	},
	
	resetReconf : function() {
		//base.dialogHandler("reconfigure succeeded");
		base.dialogHandler("closeDialog");
		seb.reconfState = RECONF_SUCCESS;
	},
	
	openSebFileDialog : function(url) { // original request is canceled by SebNet.jsm requestObserver
		sl.debug("openSebFileDialog");
		seb.reconfState = RECONF_START;
		seb.mainWin.openDialog(RECONFIG_URL,"",RECONFIG_FEATURES,url,base.initReconf,base.abortReconf).focus();
	},
	
	addSSLCert : function(cert,debug) {
		try {
			let flags = (debug) ? ovs.ERROR_UNTRUSTED | ovs.ERROR_MISMATCH | ovs.ERROR_TIME : ovs.ERROR_UNTRUSTED;
			let certData = (cert.certificateDataBase64 != "") ? cert.certificateDataBase64 : cert.certificateDataWin;
			let x509 = certdb.constructX509FromBase64(certData);
			//certlist.addCert(x509); // maybe needed for type 1 Identity Certs
			let cn = (cert.name) ? cert.name : x509.commonName; // don't ommit the name Attribut in the config file!
			let host = cn;
			let port = 443;
			let fullhost = cn.split(":");
			if (fullhost.length==2) {
				host = fullhost[0];
				port = parseInt(fullhost[1]);
			}
			ovs.rememberValidityOverride(host,port,x509,flags,false);
			sl.debug("add ssl cert: " + host + ":" + port);
		}
		catch (e) { sl.err(e); }
	},
	
	addCert : function(cert,type) {
		try {
			let t = "";
			let trustargs = "";
			let certData = (cert.certificateDataBase64 != "") ? cert.certificateDataBase64 : cert.certificateDataWin;
			let x509 = certdb.constructX509FromBase64(certData);
			if (su.getConfig("sebAllCARootTrust", "boolean", false)) {
				sl.debug("treat all CA certs as root");
				t = "CA";
				trustargs = "C,C,C";
			}
			else {
				switch (x509.getChain().length) { // experimental
					case 1 : // Root CA
						t = "ROOT CA";
						trustargs = "C,C,C";
					break;
					default : // Intermediate CA
						t = "INTERMEDIATE CA";
						trustargs = 'c,c,c';
					}
			}
			sl.debug("add Cert: " + t);
			sl.debug("cert subject: " + x509.subjectName);
			sl.debug("cert cn: "+ x509.commonName);
			sl.debug("cert org: "+ x509.organization);
			sl.debug("cert issuer: "+ x509.issuerName);
			var cdb = Cc["@mozilla.org/security/x509certdb;1"].getService(Ci.nsIX509CertDB);
			var certdb2 = cdb;
			try {
				certdb2 = Cc["@mozilla.org/security/x509certdb;1"].getService(Ci.nsIX509CertDB2);
			} catch (e) {}
			certdb2.addCertFromBase64(certData, trustargs, x509.commonName);
		}
		catch (e) { sl.err(e); }
	},
	
	setEmbeddedCerts : function() {
		let certs = seb.config["embeddedCertificates"];
		if ( typeof certs != "object") { return; }
		sl.debug("setEmbeddedCerts");
		for (var i=0;i<certs.length;i++) {
			switch (certs[i].type) {
				case CERT_CA :
					base.addCert(certs[i],certs[i].type);
					break;
				case CERT_SSL :
					base.addSSLCert(certs[i],false);
					break;
				case CERT_SSL_DEBUG :
					base.addSSLCert(certs[i],true);
					break;
			}
		}
	},
	
	disableBuiltInCerts : function () {
		sl.debug("disableBuiltInCerts");
		let certs = certdb.getCerts();
		let enumerator = certs.getEnumerator();
		while (enumerator.hasMoreElements()) {
			let cert = enumerator.getNext().QueryInterface(Ci.nsIX509Cert);
			//let sslTrust = certdb.isCertTrusted(cert, Ci.nsIX509Cert.CA_CERT, Ci.nsIX509CertDB.TRUSTED_SSL);
			//let emailTrust = certdb.isCertTrusted(cert, Ci.nsIX509Cert.CA_CERT, Ci.nsIX509CertDB.TRUSTED_EMAIL);
			//let objsignTrust = certdb.isCertTrusted(cert, Ci.nsIX509Cert.CA_CERT, Ci.nsIX509CertDB.TRUSTED_OBJSIGN);
			certdb.setCertTrust(cert, Ci.nsIX509Cert.CA_CERT, Ci.nsIX509Cert.CERT_NOT_TRUSTED);
			/*
			sl.debug("issuer: " + cert.issuerName + "\n");
			sl.debug("serial: " + cert.serialNumber + "\n");
			sl.debug("trust:\n");
			sl.debug("  SSL:\t\t" + sslTrust + "\n");
			sl.debug("  EMAIL:\t" + emailTrust + "\n");
			sl.debug("  OBJSIGN:\t" + objsignTrust + "\n");
			*/ 
		}
	},
	
	loadPage : function (win,url,flag) {	// only use for real http requests
		sl.debug("try to load: " + url);
		win.XulLibBrowser.contentDocument.location.href = url;
	},
	
	reload : function (win) {
		sl.debug("try reload in browser ...");
		if (!win.XulLibBrowser) {
			sl.err("no xullib.browser in ChromeWindow!");
			return false;
		}
		// Changed reload from hard to soft reload, to not break offline functionality with Service Workers
 		// win.XulLibBrowser.webNavigation.reload(wnav.LOAD_FLAGS_BYPASS_CACHE);
		win.XulLibBrowser.reload();
	},
	
	restart : function() {
		if (!su.getConfig("mainBrowserRestart","boolean",true)) {
			sl.debug("restart not allowed.");
			return;
		}
		sl.debug("restart...");
		sw.removeSecondaryWins();
		let urlConfig = su.getConfig("restartExamURL","string","");
		let url = (urlConfig != "") ? urlConfig : su.getUrl();
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
	
	clearSession : function() { // what about localStorage?
		sl.debug("clearSession");
		authMgr.clearAll();
		cookieMgr.removeAll();
		try {
			historySrv.removeAllPages(); // does not work...we should start firefox in private modus
		}
		catch(e) {
			//sl.err("error remove history pages: " + e + "\n typeof removeAllPages " + typeof(historySrv.removeAllPages));
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
	
	stopLoading : function(win) { // stop loading on all windows 
		try {
			if (win) {
				win.document.getElementById('loadingBox').className = "hidden";
			}
			for (var i=0;i<sw.wins.length;i++) {
				sw.wins[i].document.getElementById('loadingBox').className = "hidden";
			}	
		}
		catch (e) {
			sl.debug("error stopLoading: " + e);
		}
	}, 
	
	back : function(win) {
		var nav = win.XulLibBrowser.webNavigation;
		if (nav.canGoBack) {
			sl.debug("navigation: back");	
			nav.goBack();
		}
	},
	
	forward : function(win) {
		var nav = win.XulLibBrowser.webNavigation;
		if (nav.canGoForward) {
			sl.debug("navigation: forward");	
			nav.goForward();
		}		
	},
	
	refreshNavigation : function(win) {
		sl.debug("refreshNavigation");
		var nav = win.XulLibBrowser.webNavigation;
		var visible = (win === seb.mainWin) ? su.getConfig("allowBrowsingBackForward","boolean",false) : su.getConfig("newBrowserWindowNavigation","boolean",false);
		if (visible) { // if not visible do nothing 
			var back = win.document.getElementById("btnBack");
			var forward = win.document.getElementById("btnForward");
			if (nav.canGoBack) {
				sl.debug("canGoBack");
				back.className = "tbBtn enabled";
				//back.addEventListener("click",base.back,false);
			}
			else {
				back.className = "tbBtn disabled";
				//back.removeEventListener("click",base.back,false);
			}
			if (nav.canGoForward) {
				sl.debug("canGoForward");
				forward.className = "tbBtn enabled";
				//forward.addEventListener("click",base.forward,false);
			}
			else {
				forward.className = "tbBtn disabled";
				//forward.removeEventListener("click",base.forward,false);
			}
		}
	},
	
	createSpellCheckController : function (win) {
		if (!su.getConfig("allowSpellCheck","boolean",false)) {
			return;
		}
		sl.debug("createSpellCheckController");
		let ctxMenu = win.document.getElementById("spellCheckMenu");
		let ctxSpellEnabled = win.document.getElementById("spell-check-enabled");
		let ctxNoSuggestions = win.document.getElementById("spell-no-suggestions");
		let ctxSepEnabled = win.document.getElementById("sep-enabled");
		let ctxDics = win.document.getElementById("spell-dictionaries");
		
		win.document.addEventListener("click",onClick,false);
		function onClick(evt) {
			if (evt.button !== 2) return; // only right click
			//sl.debug("SpellCheckController Right Click");
			let editFlags = SpellCheckHelper.isEditable(evt.target, win);
			let spellInfo;
			if (editFlags & (SpellCheckHelper.EDITABLE | SpellCheckHelper.CONTENTEDITABLE)) { // only editable text and input fields
				//sl.debug("isEditable")
				spellInfo = InlineSpellCheckerContent.initContextMenu(evt, editFlags, win.XulLibBrowser.messageManager);
				if (!spellInfo.canSpellCheck) {
					//sl.debug("canSpellCheck = false");
					return;
				}
				if (spellInfo.overMisspelling) {
					ctxSepEnabled.setAttribute("hidden","false");
					if (spellInfo.spellSuggestions.length > 0) {
						InlineSpellCheckerContent._spellChecker.addSuggestionsToMenu(ctxMenu,ctxNoSuggestions,5);
						ctxNoSuggestions.setAttribute("hidden","true");
					}
					else {
						ctxNoSuggestions.setAttribute("hidden","false");
					}
				}
				else {
					ctxSepEnabled.setAttribute("hidden","true");
					ctxNoSuggestions.setAttribute("hidden","true");
				}
				ctxSpellEnabled.setAttribute("checked", spellInfo.enableRealTimeSpell);
				if (spellInfo.enableRealTimeSpell) {
					ctxDics.setAttribute("hidden","false");
				}
				else {
					ctxDics.setAttribute("hidden","true");
				}
				//sl.debug(JSON.stringify(spellInfo));
				ctxMenu.openPopupAtScreen(evt.screenX+5,evt.screenY+10,true);
			}
		}
	},
	
	spellCheckClosed : function () {
		InlineSpellCheckerContent._spellChecker.clearSuggestionsFromMenu(); // using private variable directly instead of messages
	},
	
	toggleSpellCheckEnabled : function (win) {
		sl.debug("toggleSpellCheckEnabled");
		InlineSpellCheckerContent._spellChecker.toggleEnabled(win); // using private variable directly instead of messages
	},
    
	createDictionaryList : function (menu) {
		sl.debug("createDictionaryList");
		InlineSpellCheckerContent._spellChecker.addDictionaryListToMenu(menu,null);
		// check allowed dics, p.e. after reconfiguration changes
		let allowedDics = su.getConfig("allowSpellCheckDictionary","object",[]);
		let items = InlineSpellCheckerContent._spellChecker.mDictionaryItems;
		for (var i=0;i<items.length;i++) {
			let dicId = items[i].id.replace('spell-check-dictionary-','');
			if (!allowedDics.includes(dicId)) {
				items[i].setAttribute("hidden","true");
			}
		}
	},
	
	clearDictionaryList : function (menu) {
		sl.debug("clearDictionaryList");
		InlineSpellCheckerContent._spellChecker.clearDictionaryListFromMenu();
	},
    
    createClipboardController : function (win) {
        if (!su.getConfig("enablePrivateClipboard","boolean",false)) {
            return;
        }
        sl.debug("createClipboardController");
        win.document.addEventListener("copy",onCopy,true);
        win.document.addEventListener("cut",onCut,true);
        win.document.addEventListener("paste",onPaste,true);
        function getData(evt) {
            if (evt.target.contentEditable && evt.target.setRangeText) { // Textarea, Input, Isindex: only text data
                seb.privateClipboard.text = evt.target.value.substring(evt.target.selectionStart, evt.target.selectionEnd);
                seb.privateClipboard.ranges = [];
            }
            else { // all other
                let w = evt.target.ownerDocument.defaultView;
                let sel = w.getSelection();
                let text = "";
                for (let i = 0; i < sel.rangeCount; i++) {
                    seb.privateClipboard.ranges[i] = sel.getRangeAt(i).cloneContents();
                    text += seb.privateClipboard.ranges[i].textContent;
                }
                seb.privateClipboard.text = text;
            }
        }
        
        function onCopy(evt) {
            sl.debug("captured copy:" + evt);
            try {
                getData(evt);
            }
            catch (e) {
                sl.debug(e);
            }
            finally {
                evt.preventDefault();
                evt.returnValue = false;
                return false;
            }
        }
        
        function onCut(evt) {
            sl.debug("captured cut:" + evt);
            try {
                getData(evt);
                
                if (evt.target.contentEditable && evt.target.setRangeText) { // Textarea, Input
                    evt.target.setRangeText("",evt.target.selectionStart,evt.target.selectionEnd,'select');
                }
                else { // all other (check designMode and contenteditable!)
                    let w = evt.target.ownerDocument.defaultView;
                    let designMode = evt.target.ownerDocument.designMode;
                    sl.debug("document.designMode: " + designMode);
                    let contentEditables = evt.target.ownerDocument.querySelectorAll('*[contenteditable]');
                    sl.debug("elements with contenteditable attribute: " + contentEditables.length);
                    let sel = w.getSelection();
                    let ranges = [];
                    for (let i = 0; i < sel.rangeCount; i++) {
                        let r = sel.getRangeAt(i);
                        if (designMode === 'on') {
                            r.deleteContents();
                        }
                        else {
                            if (contentEditables.length) {
                                contentEditables.forEach( node => {
                                    if (node.contains(r.commonAncestorContainer)) {
                                        r.deleteContents();
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (e) {
                sl.debug(e);
            }
            finally {
                evt.preventDefault();
                evt.returnValue = false;
                return false;
            }
        }
        
        function onPaste(evt) { 
            //sl.debug("captured paste for text length:" + seb.privateClipboard.data.length);
            try {
                if (evt.target.contentEditable && evt.target.setRangeText) { // Textarea, Input
                    evt.target.setRangeText("",evt.target.selectionStart,evt.target.selectionEnd,'select'); // delete selection if any
                    evt.target.setRangeText(seb.privateClipboard.text,evt.target.selectionStart,evt.target.selectionStart+seb.privateClipboard.text.length,'end');
                }
                else { // all other (check designMode and contenteditable!)
                    let w = evt.target.ownerDocument.defaultView;
                    let designMode = evt.target.ownerDocument.designMode;
                    sl.debug("document.designMode: " + designMode);
                    let contentEditables = evt.target.ownerDocument.querySelectorAll('*[contenteditable]');
                    sl.debug("elements with contenteditable attribute: " + contentEditables.length);
                    let sel = w.getSelection();
                    let ranges = [];
                    
                    for (let i = 0; i < sel.rangeCount; i++) {
                        let r = sel.getRangeAt(i);
                        if (designMode === 'on') {
                            r.deleteContents();
                        }
                        else {
                            if (contentEditables.length) {
                                contentEditables.forEach( node => {
                                    if (node.contains(r.commonAncestorContainer)) {
                                        r.deleteContents();
                                    }
                                });
                            }
                        }
                    }
                     
                    if (designMode === 'on') {
                        let range = w.getSelection().getRangeAt(0);
                        if (seb.privateClipboard.ranges.length > 0) {
                            seb.privateClipboard.ranges.map(r => {
                                range = w.getSelection().getRangeAt(0);
                                range.collapse();
                                const newNode = r.cloneNode(true);
                                range.insertNode(newNode);
                                range.collapse();
                            });
                        }
                        else {
                            range.collapse();
                            range.insertNode(w.document.createTextNode(seb.privateClipboard.text));
                            range.collapse();
                        }
                    }
                    else {
                        if (contentEditables.length) {
                            contentEditables.forEach( node => {
                                let range = w.getSelection().getRangeAt(0);
                                if (node.contains(range.commonAncestorContainer)) {
                                    if (seb.privateClipboard.ranges.length > 0) {
                                        seb.privateClipboard.ranges.map(r => {
                                            range = w.getSelection().getRangeAt(0);
                                            range.collapse();
                                            const newNode = r.cloneNode(true);
                                            range.insertNode(newNode);
                                            range.collapse();
                                        });
                                    }
                                    else {
                                        range = w.getSelection().getRangeAt(0);
                                        range.collapse();
                                        range.insertNode(w.document.createTextNode(seb.privateClipboard.text));
                                        range.collapse();
                                    }
                                }
                            });
                        }
                    }
                }
            }
            catch(e) {
                sl.debug(e);
            }
            finally {
                evt.preventDefault();
                evt.returnValue = false;
                return false;
            }
        }
    }
}
