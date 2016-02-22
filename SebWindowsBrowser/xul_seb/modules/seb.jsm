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
var EXPORTED_SYMBOLS = ["seb"];
Components.utils.import("resource://modules/xullib.jsm");
Components.utils.import("resource://modules/screen.jsm");
Components.utils.import("resource://gre/modules/FileUtils.jsm");
Components.utils.import("resource://gre/modules/Services.jsm");
Components.utils.import("resource://gre/modules/NetUtil.jsm");


var seb = (function() {
	// XPCOM Services, Interfaces and Objects
	const	x					=	xullib,
			sc				=	screen,
			Cc				=	x.Cc,
			Ci				=	x.Ci,
			Cu				=	x.Cu,
			Cr				=	x.Cr,
			prompt				= 	Services.prompt,
			wpl				=	Ci.nsIWebProgressListener,
			wnav				= 	Ci.nsIWebNavigation,
			xulFrame			=	"xullib.frame",
			xulBrowser			=	"xullib.browser",
			xulErr				=	"chrome://seb/content/err.xul",
			xulLoad				=	"chrome://seb/content/load.xul",
			errDeck				=	0,
			loadDeck			=	0,
			contentDeck			=	1,
			hiddenDeck			=	2,
			XUL_NS_URI			=	"http://www.mozilla.org/keymaster/gatekeeper/there.is.only.xul";
			
	var 	__initialized 				= 	null,
			hostForceShutdown		=	false,
			messageServer			=	false,
			messageSocket			=	null,
			server				=	null,
			message				=	{},
			client				=	null,
			url				=	"",
			reqHeader			=	null,
			reqKey				=	null,
			reqSalt				=	null,
			sendReqHeader			=	false,
			loadFlag			=	0,
			locs				=	null, 
			consts				=	null,
			mainWin				=	null,
			shutdownEnabled			=	false,
			shutdownIgnorePassword		=	false,
			shutdownIgnoreWarning		=	false,
			hiddenWin			=	null,
			netMaxTimes			=	0,
			netTimeout			=	0,
			net_tries			=	0,
			whiteListRegs			=	[],
			blackListRegs			= 	[],
			shutdownUrl			=	"",
			shutdownPassword		=	false,
			tryAgainUrl			=	"",
			elToScrollIntoView		=	null,
			convertReg			= 	/[-\[\]\/\{\}\(\)\+\?\.\\\^\$\|]/g,
			wildcardReg			=	/\*/g,
			stream_handler			=	{
									send_message 	: send_message,
									force_shutdown 	: force_shutdown	
								},
			shutdownObserver = {
				observe	: function(subject, topic, data) {
					if (topic == "xpcom-shutdown") {
						let p = x.getProfile();
						for (var i=0;i<p.dirs.length;i++) { // don't delete data folder
							x.debug("try to remove everything from profile folder: " + p.dirs[i].path);
							let entries = p.dirs[i].directoryEntries;
							while(entries.hasMoreElements()) {  
								let entry = entries.getNext();  
								entry.QueryInterface(Components.interfaces.nsIFile);
								//x.debug("try to remove: " + entry.path);
								try {
									x.debug("remove: " + entry.path);
									entry.remove(true);
								}
								catch(e) {
									x.err(e);
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
					x.debug("shutdownObserver registered");
				},  
				unregister: function()  {  
					this.observerService.removeObserver(this, "xpcom-shutdown");  
				}  
			},
			httpRequestObserver = {
				observe	: function(subject, topic, data) {
					if (topic == "http-on-modify-request") {
						//x.debug(data);
						subject.QueryInterface(Ci.nsIHttpChannel);
						//x.debug(subject.getRequestHeader('Accept'));
						//x.debug(subject.referrer);
						let url = subject.URI.spec.split("#"); // url fragment is not transmitted to the server!
						//url = url[0].toLowerCase();
						url = url[0];
						
						if (!x.getParam("seb.trusted.content")) {
							if (!isValidUrl(url)) {
								subject.cancel(Cr.NS_BINDING_ABORTED);
							}
						}						
						//if (sendReqHeader && /text\/html/g.test(subject.getRequestHeader('Accept'))) { // experimental
						if (sendReqHeader) { // experimental
							var k;
							if (reqSalt) {								
								k = getRequestValue(url, reqKey);
								//x.debug("get req value: " + url + " : " + reqKey + " = " + k);
							}
							else {
								k = reqKey;
							}
							subject.setRequestHeader(reqHeader, k, false);
						}
						// ToDo: MimeType and file extension detection: if handler exists, hook into the request
					} 
					
				},
  
				get observerService() {  
					return Components.classes["@mozilla.org/observer-service;1"].getService(Components.interfaces.nsIObserverService);  
				},
			  
				register: function() {  
					this.observerService.addObserver(this, "http-on-modify-request", false);  
				},  
			  
				unregister: function()  {  
					this.observerService.removeObserver(this, "http-on-modify-request");  
				}  
			},
			browserStateListener = function(aWebProgress, aRequest, aStateFlags, aStatus) {
				if(aStateFlags & wpl.STATE_IS_NETWORK) {
					if (aStateFlags & wpl.STATE_STOP) {
						net_tries = 0;
						//x.debug("network stop: content to show: " + aRequest.name);
						let win = x.getChromeWin(aWebProgress.DOMWindow);
						//win.setCursor("-moz-auto");
						//x.debug("maximize?:"+win.maximize);
						/* alert controller */
						var w = aWebProgress.DOMWindow.wrappedJSObject;
						//x.debug(w.document.getElementByTagName("Body")mozRequestFullScreen);
						//x.debug("TEST:"+w.document.mozFullScreenEnabled);
						if (x.getParam("seb.alert.controller")) {
							createAlertController(w);
						}
						if (win === mainWin && x.getParam("seb.screenshot.controller")) {
							createScreenshotController(w);
						}
						if (x.getParam("seb.screenkeyboard.controller")) {
							createScreenKeyboardController(w);
						}
						showContent(win);
					}
					if (aStateFlags & wpl.STATE_START) {												
						try {		
							
							//let win = x.getChromeWin(aWebProgress.DOMWindow);
							if (aRequest && aRequest.name) {
								let win = x.getChromeWin(aWebProgress.DOMWindow);
								//win.setCursor("-moz-spinning");
								if ((shutdownUrl === aRequest.name) || (shutdownUrl+"/" === aRequest.name)) {
									aRequest.cancel(aStatus);
									var tmpShutdown = shutdownEnabled; // store default shutdownEnabled
									var tmpPassword = shutdownIgnorePassword; // store default shutdownIgnorePassword
									shutdownEnabled = true; // set to true
									shutdownIgnorePassword = true;
									shutdown();									
									shutdownEnabled = tmpShutdown; // set default shutdownEnabled
									shutdownIgnorePassword = tmpPassword; // set default shutdownIgnorePassword
									return;
								}
								
								if (!isValidUrl(aRequest.name)) {
									aRequest.cancel(aStatus);
									prompt.alert(win, getLocStr("seb.title"), getLocStr("seb.url.blocked"));
									return 1; // 0?
								}
								// don't affect the main window 
								if (win === mainWin) {
									return 0;
								}
								// don't do anything on already initialized windows, only new opened windows that means request attribute is not empty
								if (win.XulLibBrowser.getAttribute("request") && win.XulLibBrowser.getAttribute("request") != "") {																		
									return 0;								
								}
								// check the param
								if (x.getParam("seb.distinct.popup")) {
 									let w = x.getWinFromRequest(aRequest.name); // find already opened popup with the same url								
									if (typeof w === "object") {
										aRequest.cancel(aStatus); // if found, cancle new request
										x.removeWin(win); // remove the new window with canceled request from internal window array 
										win.close(); // close the win
										w.focus();	// focusing the already opened window from the internal array
										return 1; // 0?								
									}
									else {
										//win.maximize();
										x.debug("set request " + aRequest.name + " for popup.");
										win.XulLibBrowser.setAttribute("request",aRequest.name);
									}
								}												
							}							
						}
						catch(e) {
							x.err(e);
							return 0;
						}
					}
					return 0;
				}
			};
			
	function toString () {
			return "seb";
	}
	
	function init(win) {
		
		Cc["@mozilla.org/net/osfileconstantsservice;1"].getService(Ci.nsIOSFileConstantsService).init();
		x.debug("init window");
		x.debug("externalHost: "+x.getExternalHost());
		x.addWin(win);
		getKeys(win); 					// for every window call
		setBrowserHandler(win); 			// for every window call
		setReqHeader();
		setLoadFlag();
		shutdownEnabled = x.getParam("seb.shutdown.enabled");
		
		//x.getParam("seb.embedded.certs"); // trigger ctrl to override default behaviour for embedded certs	
		if  (x.getWinType(win) == "main") {
			var certs = x.getParam("seb.embedded.certs");
			if (typeof certs == "object") {
				embeddedCerts(certs);
			}
			let hf = win.document.getElementById("hidden.iframe");			
			if (x.getParam("seb.server.enabled")) {
				server = x.getParam("seb.server");
				if (typeof server === "object") {
					x.debug("connect to seb server...");
					setHiddenIFrameHandler(hf);			
					hf.setAttribute("src",server.url);				
				}
				else {
					x.debug("no server configured");
				}
			}
			if (x.getParam("seb.messaging.socket")) {				
				message["socket"] = x.getParam("seb.messaging.socket");
				message["pingtime"] = x.getParam("seb.messaging.ping.time");
				x.debug("connect to message server...");
				setHiddenMessageHandler(hf);			
				//hf.setAttribute("src",message.url);
				hf.setAttribute("src",'about:config');	
			}
			setShutdownHandler(win);
			mainWin = win;
			initMain(win);
		}
		else {	
			setSize(win);
			setTitlebar(win);
		} 	
	}
	
	function embeddedCerts(certs) {
		x.debug("embedded certs: " + certs.length);
		for (var i=0;i<certs.length;i++) {
			addCert(certs[i]);
		}
	}
	
	function addCert(cert) {
		//https://developer.mozilla.org/en-US/docs/Cert_override.txt
		try {
			var overrideService = Cc["@mozilla.org/security/certoverride;1"].getService(x.Ci.nsICertOverrideService);
			var flags = overrideService.ERROR_UNTRUSTED | overrideService.ERROR_MISMATCH | overrideService.ERROR_TIME;
			var certdb = x.getCertDB();
			//var certcache = x.getCertCache();
			//var certlist = x.getCertList();
			var x509 = certdb.constructX509FromBase64(cert.certificateData);
			//certlist.addCert(x509); // maybe needed for type 1 Identity Certs
			//certcache.cacheCertList(certlist);
			var host = cert.name;
			var port = 443;
			var fullhost = cert.name.split(":");
			if (fullhost.length==2) {
				host = fullhost[0];
				port = parseInt(fullhost[1]);
			}
			x.debug("add cert: " + host + ":" + port + "\n" + cert.certificateData);	
			overrideService.rememberValidityOverride(host,port,x509,flags,true); 
		}
		catch (e) {
			x.err(e);
		}
	}
	
	// hidden iFrame for seb server
	function setHiddenIFrameHandler(fr) {
		x.debug("setHiddenIFrameHandler");
		fr.addEventListener("DOMContentLoaded",hiddenIFrameListener, true);
	}
	
	function setHiddenMessageHandler(fr) {
		x.debug("setHiddenMessageHandler");
		fr.addEventListener("DOMContentLoaded",hiddenMessageListener, true);
	}
	
	function hiddenMessageListener(e) {
		
		hiddenWin = mainWin.document.getElementById("hidden.message").contentWindow.wrappedJSObject;
		const { WebSocket } = hiddenWin;
		
		try {
			messageSocket = new WebSocket(message.socket);
			x.setMessageSocket(messageSocket);
		}
		catch (e) {
			x.debug("messageSocket connection failed: " + message.socket + "\n"+e);
			messageSocket = null;
			return; 
		}
		
		messageSocket.onopen = function(evt) { 			
			x.debug("messageSocket open: " + evt); 
			messageSocket.send("seb.connected"); 
			messageServer = true;
			x.debug("set ping intervall " + message.pingtime);
			hiddenWin.setInterval(function() {
				messageSocket.send("seb.ping"); 
			} ,message.pingtime);
		}
			
		messageSocket.onclose = function(e) { 
			x.debug("messageSocket close: " + e); 
			messageSocket = null; 
			messageServer = false;
		}; 
		
		messageSocket.onmessage = function(evt) { 
			// ToDo: message handling
			switch (evt.data) {
				case "SEB.close" :
					x.debug("messageSocket handled: " + evt.data); 
					hostForceShutdown = true;
					break;
				case "SEB.restartExam" :
					x.debug("messageSocket handled: " + evt.data);
					hostRestartUrl();
					break;
				case "SEB.displaySettingsChanged" :
					x.debug("messageSocket handled: " + evt.data);
					hostDisplaySettingsChanged();
					break;
				case "SEB.reload" :
					x.debug("messageSocket handled: " + evt.data);
					reload(null);
					break;
				case "SEB.keyboardShown" :
					x.debug("messageSocket handled: " + evt.data);
					if (elToScrollIntoView != null) {
						try {
							elToScrollIntoView.scrollIntoView(false);
						}
						catch (e) { 
							x.err(e);
						}
					}
					break;
				default :
					x.debug("messageSocket not handled msg: " + evt.data); 
			}
		};
		 
		messageSocket.onerror = function(e) { 
			x.debug("messageSocket err: " + e); 
			messageSocket = null;
			messageServer = false; 
		};
		e.preventDefault();
		e.stopPropagation();
	}
	
	function hiddenIFrameListener(e) {
		hiddenWin = mainWin.document.getElementById("hidden.iframe").contentWindow.wrappedJSObject; 
		client = new hiddenWin.BinaryClient(server.socket); 
		client.on('open', function() { x.debug("websocket connection established to " + server.socket + ".") });
		client.on('error', function(err) { x.debug("websocket error: " + err) });
		client.on('close', function(err) { x.debug("websocket closed.") });
		client.on('stream', on_client_stream );
		
		/*
		const { io } = w;
		var socket = io.connect(server.url);
		socket.on('message', function (data) {
			x.debug("data :" + JSON.stringify(data));
			//socket.emit('message', {'my' : 'data'});
		});
		socket.on('error', function (data) {
			x.debug("error :" + JSON.stringify(data));
			//socket.emit('client', {'my' : 'data'});
		});
		*/
		
		/*
		const { WebSocket } = w;
		ws = WebSocket(server.socket);
		ws.onopen = function(evt) { x.debug("open: " + evt); ws.send("connected to seb server"); };
		ws.onclose = function(evt) { x.debug("close: " + evt.data) }; 
		ws.onmessage = function(evt) { x.debug("msg: " + evt.data) }; 
		ws.onerror = function(evt) { x.debug("err: " + evt.data) };
		*/
		
		
		//x.debug(content);
		//var shot = sc.screenShot(mainWin.XulLibBrowser.contentWindow.wrappedJSObject,sc.BASE64); // just a test
		//x.debug("shot:" + shot);
		//websocket.send("sdfsdfsdfsdf");
		//var ws = new w.WebSocket("ws://echo.websocket.org"); // the same
		//x.debug("hiddenIFrameListener:";
		e.preventDefault();
		e.stopPropagation();
	}
	
	function initMain(win) {
		if (__initialized) {
			x.err("something is going wrong. The main seb window is already initialized!");
			return;
		}
				
		if (x.getParam("seb.removeProfile")) {
			x.debug("register shutdownObserver...");
			shutdownObserver.register();
		}
		httpRequestObserver.register();	
		url = getUrl();
		if (!url) {
			x.err("could not get url!");
			return false;
		}
		x.debug("seb init with url: " + url);
		setListRegex(); 	// compile regexs 
		locs = win.document.getElementById("locale");	
		consts = win.document.getElementById("const");
		
		setSize(win);
		setTitlebar(win);									
		showLoading(win);
		netMaxTimes = x.getParam("seb.net.max.times");
		netTimeout = x.getParam("seb.net.timeout");
		if (x.getParam("seb.screenshot.controller")) {
			sc.init(win.document.getElementById("seb.snapshot"));
		}
		loadPage(url);
		x.out("seb started...");
		__initialized = true;
	}
	
	function loadPage(url) {
		// loadPage wrapper:
		// sometimes there are initial connection problems with live boot media like sebian
		// try to get a valid connection for MAX_NET_TRIES times after NET_TRIES_TIMEOUT
		// call loadURI only if the NetChannel connection is successful
		
		if (x.getParam("seb.net.tries.enabled")) {
			let uri = NetUtil.newURI(url);
			let channel = NetUtil.newChannel(uri);
			channel.QueryInterface(Components.interfaces.nsIHttpChannel);
			channel.requestMethod = "HEAD";
			
			//debug(channel.notificationCallbacks);
			NetUtil.asyncFetch(channel, function(inputStream, status) {
							net_tries += 1;
							if (net_tries > netMaxTimes) {
								net_tries = 0; // reset net_tries 
								// try anyway and take a look what happens (ugly: // ToDo: internal error page and detailed response headers)
								tryAgainUrl = url;
								showError(mainWin);
								return;
							}
							if (!Components.isSuccessCode(status)) {  // ToDo: detailed response header
								x.debug(net_tries + ". try: could not open " + uri.spec + " - status " + status);
								mainWin.setTimeout(function() { loadPage(url); },netTimeout);
								//let metaString = NetUtil.readInputStreamToString(inputStream, inputStream.available());
								//debug(metaString);
								//return;  
							}
							else {
								//_debug("channel response code: " + channel.getResponse);
								x.loadPage(mainWin,url,loadFlag);
							}										
						});
		}
		else {
			try {
				x.loadPage(mainWin,url,loadFlag);
			}
			catch(e) {
				x.err("Loading Error: " + e);
			}
		}
	}
	
	function onclose(win) { // will only be handled for secondary wins
		// the close event does not fire on modal windows like the Sonderzeichen dialog in ILIAS
		// therefore only the onunload event will be catched for cleaning the internal array
		if (win === mainWin) {
			return;
		}
		x.debug("close secondary win");
	}
	
	function onunload(win) { // will only be handled for secondary wins
		if (win === mainWin) {
			return;
		}
		x.debug("unload secondary win");
		x.removeWin(win);
	}
	
	function setShutdownHandler(win) {
		x.debug("setShutdownHandler");
		shutdownUrl = x.getParam("seb.shutdown.url");
		shutdownPassword = x.getParam("seb.shutdown.password");
		win.addEventListener( "close", shutdown, true); // controlled shutdown for main window
	}
	
	function setBrowserHandler(win) { // Event handler for both wintypes
		x.debug("setBrowserHandler");
		x.addBrowserStateListener(win,browserStateListener); // for both types
	}
	
	function setReqHeader() {
		let rh = x.getParam("seb.request.header");
		let rk = x.getParam("seb.request.key");
		let rs = x.getParam("seb.request.salt");
		
		if (typeof rh === 'string' && rh != "" && typeof rk === 'string' && rk != "") {
			reqHeader = rh;
			reqKey = rk;
			reqSalt = rs;
			sendReqHeader = true;
		}
	}
	
	function setLoadFlag() {
		loadFlag = wnav.LOAD_FLAGS_NONE;
		if (x.getParam("seb.bypass.cache")) {
			loadFlag |= wnav.LOAD_FLAGS_BYPASS_CACHE;
		}
		if (!x.getParam("seb.navigation.enabled")) {
			loadFlag |= wnav.LOAD_FLAGS_BYPASS_HISTORY;
		}
		//x.debug("loadFlag: " + loadFlag);
	}
	
	// app lifecycle
	function shutdown(e) { // only for mainWin
		var w = mainWin;
		var prompts = Cc["@mozilla.org/embedcomp/prompt-service;1"].getService(Ci.nsIPromptService);
		if (e != null) { // catch event
			if (hostForceShutdown) {
				x.debug("host force shutdown");
				return true;
			}
			else {
				e.preventDefault();
				e.stopPropagation();				
			}
		}
		
		if (messageServer) {
			x.debug("shutdown should be handled by host");
			var msg = (e) ? "seb.beforeclose.manual" : "seb.beforeclose.quiturl";
			messageSocket.send(msg);
			return;
		}
		x.debug("try shutdown...");				
		
		if (!shutdownEnabled) { // on shutdown url the global variable "shutdownEnabled" is set to true
			x.out("no way! seb is locked :-)");
		}
		else {
			if (e) { // close window event: user action or host event 
				// first look for warning
				let passwd = x.getParam("seb.shutdown.password");
				
				if (!shutdownIgnoreWarning && !passwd) {
					var result = prompts.confirm(null, getLocStr("seb.shutdown.warning.title"), getLocStr("seb.shutdown.warning"));
					if (!result) {
						return;
					}
				}							
				
				if (passwd && !shutdownIgnorePassword) {				
					var password = {value: ""}; // default the password to pass
					var check = {value: true}; // default the checkbox to true
					var result = prompts.promptPassword(null, getLocStr("seb.password.title"), getLocStr("seb.password.text"), password, null, check);
					if (!result) {
						return;
					}
					var check = getHash(password.value);
					if (check.toLowerCase() != passwd.toLowerCase()) {
						//prompt.alert(mainWin, getLocStr("seb.title"), getLocStr("seb.url.blocked"));
						prompt.alert(mainWin, getLocStr("seb.password.title"), getLocStr("seb.password.wrong"));
						return;
					}
				}
			}
			else { // shutdown link: browser takes shutdown control
				if (!shutdownIgnoreWarning) {
					var result = prompts.confirm(null, getLocStr("seb.shutdown.warning.title"), getLocStr("seb.shutdown.warning"));
					if (!result) {
						return;
					}
				}
				
				let passwd = x.getParam("seb.shutdown.password");
				
				if (passwd && !shutdownIgnorePassword) {				
					var password = {value: ""}; // default the password to pass
					var check = {value: true}; // default the checkbox to true
					var result = prompts.promptPassword(null, getLocStr("seb.password.title"), getLocStr("seb.password.text"), password, null, check);
					if (!result) {
						return;
					}
					var check = getHash(password.value);
					if (check.toLowerCase() != passwd.toLowerCase()) {
						//prompt.alert(mainWin, getLocStr("seb.title"), getLocStr("seb.url.blocked"));
						prompt.alert(mainWin, getLocStr("seb.password.title"), getLocStr("seb.password.wrong"));
						return;
					}
				}
			}
			
			if (client) {
				for (var s in client.streams) {
					x.debug("close stream " + s);
					client.streams[s]._socket.close();
				}
			}
			x.closeAllWin();
			x.quit();
			/*
			for (var i=x.getWins().length-1;i>=0;i--) { // ich nehm Euch alle MIT!!!!
				try {
					x.debug("close window ...");
					x.getWins()[i].close();
				}
				catch(e) {
					x.err(e);
				}
			}
			*/ 
		}
	}
	
	function shutdownForce() {
		if (client) {
			for (var s in client.streams) {
				x.debug("close stream " + s);
				client.streams[s]._socket.close();
			}
		}
		for (var i=x.getWins().length-1;i>=0;i--) { // ich nehm Euch alle MIT!!!!
			try {
				x.debug("close window ...");
				x.getWins()[i].close();
			}
			catch(e) {
				x.err(e);
			}
		}
	}
	
	
	// browser and seb windows
	function showLoading(win) {
		let w = (win) ? win : x.getRecentWin();
		x.debug("showLoading...");
		getFrameElement(w).setAttribute("src",xulLoad);
		setDeckIndex(w,loadDeck);
	}
	
	function showError(win) {
		let w = (win) ? win : x.getRecentWin();
		x.debug("showError...");
		getFrameElement(w).setAttribute("src",xulErr);
		setDeckIndex(w,errDeck);
	}
	
	function showContent(win) { 
		let w = (win) ? win : x.getRecentWin();
		x.debug("showContent..." + x.getWinType(w));
		setDeckIndex(w,contentDeck);
		try {
			w.document.title = w.content.document.title;
		}
		catch(e) {}
		w.focus();
		w.XulLibBrowser.focus();
	}
	
	function showAll() {
		x.debug("show all...");
		x.showAllWin();
	}
	
	function toggleHidden(w) {
		if (!x.getParam("seb.togglehidden.enabled") || w !== mainWin) {
			return;
		}		
		setDeckIndex(w,(getDeckIndex(w) == contentDeck) ? hiddenDeck : contentDeck);
		x.debug("toggled to : " + getDeckIndex(w));
	}
	
	function reload(win) {
		x.debug("try reload...");
		net_tries = 0;
		win = (win === null) ? x.getRecentWin() : win; // klassische win win situation!
		//
		//var doc = (win) ? win.content.document : mainWin.content.document;
		//doc.location.reload();
		if (x.getParam("seb.reload.warning")) {
			var prompts = Cc["@mozilla.org/embedcomp/prompt-service;1"].getService(Ci.nsIPromptService);
			var result = prompts.confirm(null, getLocStr("seb.reload.warning.title"), getLocStr("seb.reload.warning"));
			if (result) {
				x.reload(win);
			}
		}
		else {
			x.reload(win);
		}
	}
	
	function restart() { // only mainWin, experimental
		net_tries = 0;
		if (x.getParam("seb.restart.mode") === 0) {
			return;
		}
		if ((x.getParam("seb.restart.mode") === 1) && (getFrameElement().getAttribute("src") != xulErr)) {
			return;
		}
		x.debug("restart...");
		x.removeSecondaryWins();
		let url = getUrl();
		showLoading(mainWin);
		loadPage(url);
	}
	
	function tryAgain() {
		x.debug("try again...");
		showLoading();
		loadPage(tryAgainUrl);
	}
	
	function hostRestartUrl() {
		let url = x.getParam("seb.restart.url");
		if (!url || url == "") {
			x.err("no restart url from host");
			return;
		}
		x.debug("host restart url: " + url);
		x.removeSecondaryWins();
		showLoading(mainWin);
		loadPage(url);
	}
	
	function hostDisplaySettingsChanged() {
		x.debug("host display settings changed");
		var wins = x.getWins();
		for (i=0;i<wins.length;i++) {
			setSize(wins[i]);
		}
	}
	
	function load() {		
		x.debug("try load...");
		if (typeof x.getParam("seb.load") != "string" || x.getParam("seb.load") == "") return;
		var doc = mainWin.content.document;
		var url = x.getParam("seb.load");
		var ref = doc.location.href;
		var refreg = "";
		if (typeof x.getParam("seb.load.referrer.instring") === "string") {
			refreg = x.getParam("seb.load.referrer.instring");
		}
		if (refreg != "") {
			if (ref.indexOf(refreg) > -1) {
				if (isValidUrl(url)) {
					x.debug("load from command " + url);
					doc.location.href = url;
				}
				else {
					prompt.alert(mainWin, getLocStr("seb.title"), getLocStr("seb.url.blocked"));
				}
				return false;
			}
			else {
				x.debug("loading \"" + url + "\" is only allowed if string in referrer: \"" + refreg + "\"");
				return false;
			}
		}
		else {
			x.debug("load from command " + url);
			doc.location.href = url;
		}
	}
	
	function back(win) {
		if (x.getParam("seb.navigation.enabled")) {
			x.goBack(win);
		}
		else {
			x.debug("navigation not enabled!");
		}
	}
	
	function forward(win) {
		if (x.getParam("seb.navigation.enabled")) {
			x.goForward(win);
		}
		else {
			x.debug("navigation not enabled!");
		}
	}
	
	/* locales const, UI keys and commands */
	function getDeck(win) {
		let w = (win) ? win	: x.getRecentWin();
		return w.document.getElementById("deckContents");
	}
	
	function getDeckIndex(win) {
		let w = (win) ? win	: x.getRecentWin();
		return getDeck(win).selectedIndex;
	}
	
	function setDeckIndex(win,index) {
		let w = (win) ? win	: x.getRecentWin();
		getDeck(win).selectedIndex = index;
	}
	
	function getFrameElement(win) {
		let w = (win) ? win	: x.getRecentWin();
		return w.document.getElementById(xulFrame);
	}
	
	function getBrowserElement(win) {
		let w = (win) ? win	: x.getRecentWin();
		return w.document.getElementById(xulBrowser);
	}
	
	function getLocStr(k) {
		return locs.getString(k);
	}
	
	function getConstStr(k) {
		return consts.getString(k);
	}
	
	function setSize(win) {
		x.debug("setSize: " + x.getWinType(win));
		
		let scr = (x.getWinType(win) == "main") ? x.getParam("seb.mainWindow.screen") : x.getParam("seb.popupWindows.screen");
		if (scr.fullsize) { // no resizing
			return;
		}
		
		let os = Services.appinfo.OS.toUpperCase();
		//x.debug("mainScreen: " + JSON.stringify(scr));
		
		let swt = mainWin.screen.width;
		let sht = mainWin.screen.height;
		let stp = mainWin.screen.top;
		let slt = mainWin.screen.left;
		
		x.debug("screenWidth: " + swt);
		x.debug("screenHeight: " + sht);
		x.debug("screenTop: " + stp);
		x.debug("screenLeft: " + slt);
		
		let sawt = mainWin.screen.availWidth;
		let saht = mainWin.screen.availHeight;
		let satp = mainWin.screen.availTop;
		let salt = mainWin.screen.availLeft;
		
		x.debug("screenAvailWidth: " + sawt);
		x.debug("screenAvailHeight: " + saht);
		x.debug("screenAvailTop: " + satp);
		x.debug("screenAvailLeft: " + salt);
		
		let wow = win.outerWidth;
		let wiw = win.innerWidth;
		let woh = win.outerHeight;
		let wih = win.innerHeight;
		
		x.debug("winOuterWidth: " + wow);
		x.debug("winInnerWidth: " + wiw);
		x.debug("winOuterHeight: " + woh);
		x.debug("winInnerHeight: " + wih);
		
		let wsx = win.screenX;
		let wsy = win.screenY;
		
		x.debug("winScreenX: " + wsx);
		x.debug("winScreenY: " + wsy);
		
		let offWidth = win.outerWidth - win.innerWidth;
		let offHeight = win.outerHeight - win.innerHeight;
		x.debug("offWidth: " + offWidth);
		x.debug("offHeight: " + offHeight);
		
		let tb = x.getParam("seb.taskbar.enabled");
		x.debug("showTaskBar:" + tb);
		
		if (tb) {
			let defaultTbh = (sht - saht);
			let tbh = x.getParam("seb.taskbar.height");
			tbh = (tbh > 0) ? tbh : defaultTbh;
			sht -= tbh;
			x.debug("showTaskBar: change height to " + sht);
		}
		
		let wx = swt;
		let hx = sht;
		if (typeof scr.width == "string" && /^\d+\%$/.test(scr.width)) {
			let w = scr.width.replace("%","");
			wx = (w > 0) ? ((swt / 100) * w) : swt;
		}
		else {
			wx = (scr.width > 0) ? scr.width : swt;
		}
		x.debug("wx: " + wx);
		
		if (typeof scr.height == "string" && /^\d+\%$/.test(scr.height)) {
			var h = scr.height.replace("%","");
			hx = (h > 0) ? ((sht / 100) * h) : sht;	
		}
		else {
			hx = (scr.height > 0) ? scr.height : sht;
		}
		x.debug("hx: " + hx);
		
		
		if (x.getWinType(win) == "main" && x.getParam('seb.mainWindow.titlebar.enabled')) {
			wx -= getFrameWidth();
			hx -= getFrameHeight();
		}
		
		if (x.getWinType(win) != "main" && x.getParam('seb.popupWindows.titlebar.enabled')) {
			wx -= getFrameWidth();
			hx -= getFrameHeight();
		}
		
		x.debug("resizeTo: " + wx + ":" + hx);
		win.setTimeout(function() { 
						this.resizeTo(wx,hx); 
						this.setTimeout(function () { 
								setPosition(this); 
							}, 100 );
						}, 100);
		
		//setPosition(win);
		//win.setTimeout(function () { setPosition(this) }, 100 );
		function getFrameWidth() {
			switch (os) {
				case "DARWIN" :
					return 0;
				case "UNIX" :
				case "LINUX" :
					return 0;
				case "WINNT" :
					return 0;
			}
		}
		
		function getFrameHeight() {
			switch (os) {
				case "DARWIN" :
					return 0;
				case "UNIX" :
				case "LINUX" :
					return 20;
				case "WINNT" :
					return 0;
			}
		}
		
		function setPosition(win) {
			x.debug("setPosition: " + scr.position);
			switch (scr.position) {
				case "center" :
					//x.debug();
					x.debug("moveTo: " + ((swt/2)-(wx/2)) + ":" + satp);
					win.moveTo(((swt/2)-(wx/2)),satp);
					break;
				case "right" :
					x.debug("moveTo: " + (swt-wx) + ":" + satp);
					win.moveTo((swt-wx),satp);
					break;
				case "left" :
					x.debug("moveTo: " + salt + ":" + satp);
					win.moveTo(salt,satp);
					break;
				default :
					// do nothing
			}
		}
	}
	
	/*
	function setSize(win) {
		x.debug("set size");
		let sn = (win === mainWin) ? x.getParam("seb.mainWindow.screen") : x.getParam("seb.popupWindows.screen");
		
		if (win !== mainWin) {
			//x.debug(win.document.mozRequestFullScreen);
		}
		
		if (typeof sn != "object") {
			return;
		}
		
		let offWidth = win.outerWidth - win.innerWidth;
		let offHeight = win.outerHeight - win.innerHeight;
		
		let sw = mainWin.screen.width;
		let sh = mainWin.screen.height;
		
		var tb = x.getParam("seb.taskbar.enabled");
		x.debug("taskbar.enabled:" + tb);
		
		if (tb) {
			let tbh = x.getParam("seb.taskbar.height");
			sh -= tbh;
		}
		
		let st = mainWin.screen.availTop;
		let sl = mainWin.screen.availLeft;
		let wins = x.getWins();
		let wx = 0;
		let hx = 0;
		if (typeof sn.width == "string" && /^\d+\%$/.test(sn.width)) {
			var w = sn.width.replace("%","");
			wx = (w > 0) ? ((sw / 100) * w) : sw;	
		}
		else {
			wx = (sn.width > 0) ? sn.width : sw;
		}
		
		if (typeof sn.height == "string" && /^\d+\%$/.test(sn.height)) {
			var h = sn.height.replace("%","");
			hx = (h > 0) ? ((sh / 100) * h) : sh;	
		}
		else {
			hx = (sn.height > 0) ? sn.height : sh;
		}
		
		if (sn.fullsize) { // needs to be resized with offWidth and offHeight browser frames
			if (tb) {	
				win.resizeTo(sw+offWidth,sh+offHeight); // don't know the correct size
				win.setTimeout(function () { this.moveTo(0,0); }, 100);
			}
		}
		else {
			x.debug("no fullsize. resize to: " + wx + ":" + hx);
			win.resizeTo(wx,hx);
			win.setTimeout(function () { setPosition(this) }, 100 );
		}
		
		function setPosition(win) {
			switch (sn.position) {
				case "center" :
					win.moveTo(((sw/2)-(wx/2)),st);
					break;
				case "right" :
					win.moveTo((sw-wx),st);
					break;
				case "left" :
					win.moveTo(sl,st);
					break;
				default :
					// do nothing
			}
		}
	}
	*/
	
	function __setTitlebar(win) { // old
		let w = (win) ? win : x.getRecentWin();
		let val = '';
		if (x.getWinType(win) == 'main') {
			val = (x.getParam('seb.mainWindow.titlebar.enabled')) ? "-1,-1,-1,-1" : "0,0,0,0";
		}
		else {
			val = (x.getParam('seb.popupWindows.titlebar.enabled')) ? "-1,-1,-1,-1" : "0,0,0,0";
		}
		w.document.getElementById("sebWindow").setAttribute("chromemargin",val);
		//w.document.getElementById("sebWindow").setAttribute("hidechrome",!x.getParam('seb.mainWindow.titlebar.enabled'));
	}
	
	function setTitlebar(win) {
		let os = Services.appinfo.OS.toUpperCase();
		let attr = "";
		let val = "";
		let margintop = "0px";
		let sebwin = win.document.getElementById("sebWindow");
		let titlebarEnabled = (x.getWinType(win) == 'main') ? x.getParam('seb.mainWindow.titlebar.enabled') : x.getParam('seb.popupWindows.titlebar.enabled');
		
		switch (os) { 
			case "WINNT" :
				//win.setTimeout(function() { this.fullScreen=true },1);
				attr = "chromemargin";
				val = (titlebarEnabled) ? "-1,-1,-1,-1" : "0,-1,-1,-1";
				margintop = (titlebarEnabled) ? "0px" : "6px";
				//attr = "hidechrome";
				//val = (!scr.titlebarEnabled);
				break;
			case "DARWIN" : // maybe the best would be hidechrome and resizing
				attr = "chromemargin";
				val = (titlebarEnabled) ? "-1,-1,-1,-1" : "0,-1,-1,-1";
				//attr = "hidechrome";
				//val = (!scr.titlebarEnabled);
				//win.setTimeout(function() { this.maximize(); },1);
				break;
			case "UNIX" :
			case "LINUX" :
				attr = "hidechrome";
				val = (!titlebarEnabled);
				break;
			default :
				x.err("Unknown OS: " + os)
		}
		x.debug(attr + ":" + val);
		sebwin.style.marginTop = margintop;
		sebwin.setAttribute(attr,val);
		//win.maximize();
		//win.setTimeout(function() { this.maximize(); },1);
	}
	
	function getKeys(win) {		
		var ks = win.document.getElementsByTagName("key");		
		for (var i=0;i<ks.length;i++) {
			var p = ks[i].id;
			var kc = p + ".keycode";
			var md = p + ".modifiers";
			
			if (x.getParam(kc)) {
				ks[i].setAttribute("keycode", x.getParam(kc)); 
				//x.debug(kc + " set to " + x.getParam(kc));
			}
			else {
				ks[i].setAttribute("keycode", "");
				//x.debug(kc + " set to ''");
			}
			if (x.getParam(md)) {
				ks[i].setAttribute("modifiers", x.getParam(md));
				//x.debug(md + " set to " + x.getParam(md));
			}
			else {
				ks[i].setAttribute("modifiers", "");
				//x.debug(md + " set to ''");
			}
		}
	}
	/* controller for alerts and confirms to override the titlebar */
	function createAlertController(win) {	
		x.debug("create alert controller...");	
		win.alert = alert;
	}
	
	function alert(msg,win,title) {
		let t = (title) ? title  : "alert";  
		if (win) {
			prompt.alert(win, t, msg);
		}
		else {
			prompt.alert(mainWin, t, msg);
		}
	}
	
	var textTypes = ['color','date','datetime','datetime-local','email','month','number','password','search','tel','text','time','url','week'];
	function createScreenKeyboardController(win) {
		win.document.addEventListener("click",onClick,false);
		var elArr = new Array();
		function onClick(evt) {
			var el = evt.target;
			switch (el.tagName) {
				case "INPUT" :
					var typ = el.getAttribute("type");
					if (textTypes.indexOf(typ) > -1) {
						handleClick(evt);
					}
				break;
				case "TEXTAREA" :
					handleClick(evt);
				break;
				default :
					// do nothing
			}
			//x.debug(evt.target.tagName);
		}
		function handleClick(evt) {
			var el = evt.target;
			if (el.getAttribute("readonly")) {
				//x.debug("readonly");
				return;
			}
			onFocus(evt);
			if (elArr.contains(el)) {
				//x.debug("input already exists in array");
				return;
			}
			el.addEventListener("blur",onBlur,false);
			elArr.push(el);
		}
		
		function onFocus(evt) {
			x.debug("input onFocus");
			try {
				messageSocket.send("seb.input.focus");
				elToScrollIntoView = evt.target;
				//evt.target.scrollIntoView();
			}
			catch(e){}
		}
		function onBlur(evt) {
			x.debug("input onBlur");
			try {
				elToScrollIntoView = null;
				messageSocket.send("seb.input.blur"); 
			}
			catch(e){}
		} 
	}
	
	/* controller for webapplication */
	function createScreenshotController(win) {
		//const XMLHttpRequest = Components.Constructor("@mozilla.org/xmlextras/xmlhttprequest;1");
		//var req = XMLHttpRequest();
		//let url = getUrl();
		//let hw = Services.appShell.hiddenDOMWindow;
		//hw.addEventListener("load",hwLoad,true);
		//hw.open("http://www.heise.de","hiddenWindow","width=0,height=0,alwaysLowerd=yes");					
		//x.debug(hw.content);
		/*
		var b = hw.document.createElementNS(XUL_NS_URI, 'browser');
		b.setAttribute('type', 'content');
		*/
		
		//const { WebSocket } = Services.appShell.hiddenDOMWindow;
		//var ws = WebSocket("ws://echo.websocket.org");		
		x.debug("create screenshot controller...");				
		win.seb_ScreenShot = function(w,file) {
			let mimetype = x.getParam("sc.image.mimetype");
			mimetype = (mimetype == sc.PNG || mimetype == sc.JPEG) ? mimetype : sc.PNG;
			// we want to use the default mimetype and ratio in config.json
			let opts = {	
				format 		: 	sc.BLOB,
				mimetype	:	mimetype,
				ratio		:	0.5
			};
			x.debug(opts.mimetype);
			// add mimetype to file object
			file["mimetype"] = opts.mimetype;
			// assign file extension to the filename
			switch (file.mimetype) {
				case sc.JPEG :
					file.filename += '.jpg';
					break;
				case sc.PNG :
					file.filename += '.png';
					break;
			} 
			sc.screenShot(w, blobHandler,opts);
			
			function blobHandler(data) {
				x.debug("blobHandler: " + data);
				x.debug("websocket: " + client);
				let opts = {handler : "screenshot", size : data.size, file : file };
				var stream;
				if (client) {
					let opts_str = JSON.stringify(opts);
					stream = client.send(data, opts_str);
				}
				else {
					x.debug("no websocket for sending image data...");
				}
				stream.on('data', function(data) {
					x.debug("file written to: " + data);
					if (typeof hiddenWin.log == 'function') {
						hiddenWin.log(data);
					}
					//$('#progress').text(Math.round(tx+=data.rx*100) + '% complete');
				});							
			}
		}
	}
	// stream_handler 
	function on_client_stream(stream, opts) {
		x.debug("on_client_stream");
		//debug("on_client_stream");
		var o = JSON.parse(opts); // transfering json objects does not work, have to parse strings??
		var handler = stream_handler[o.handler];
		if (typeof handler === 'function') {
			handler.apply(undefined, [stream, o]);
		}
		else {
			err("no such stream_handler: " + o.handler);
		}
	}
	
	function send_message(stream, opts) {
		prompt.alert(mainWin, "Message from Admin", opts.msg);
	}	
	
	function force_shutdown() {
		var os = Services.appinfo.OS.toUpperCase();
		if (os == "LINUX" || os == "UNIX") { // try sudo /sbin/halt from sebserver websocket message
			// create an nsIFile for the executable
			try {
				var file = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsIFile);
				file.initWithPath("/usr/bin/sudo");
				// create an nsIProcess
				var process = Cc["@mozilla.org/process/util;1"].createInstance(Ci.nsIProcess);
				process.init(file);
				// Run the process.
				// If first param is true, calling thread will be blocked until
				// called process terminates.
				// Second and third params are used to pass command-line arguments
				// to the process.
				var args = ["/sbin/halt"];
				process.run(false, args, args.length);
			}
			catch(e) {
				//prompt.alert(mainWin, "Message from Admin", e);
				x.err("Error " + e);
			}
		}
		else { // this is the windows SEB websocket call
			shutdownIgnoreWarning = true;
			shutdownIgnorePassord = true;
			shutdownEnabled = true;
			shutdown();
		}
	}
	
	// url processing
	function getUrl() {
		let url = x.getCmd("url");
		if (url !== null) {
			return url;
		}
		url = x.getParam("seb.url");
		if (url !== undefined) {
			return url;
		}
		return false;
	}
	
	
	function setListRegex() { // for better performance compile RegExp objects and push them into arrays 
		var bs;
		var ws;
		var is_regex = (typeof x.getParam("seb.pattern.regex") === "boolean") ? x.getParam("seb.pattern.regex") : false;		
		var b = (typeof x.getParam("seb.blacklist.pattern") === "string") ? x.getParam("seb.blacklist.pattern") : false;
		var w = (typeof x.getParam("seb.whitelist.pattern") === "string") ? x.getParam("seb.whitelist.pattern") : false;		
		if (b) {
			bs = b.split(";");
			for (var i=0;i<bs.length;i++) {
				if (is_regex) {
					blackListRegs.push(new RegExp(bs[i]));
				}
				else {
					blackListRegs.push(new RegExp(getRegex(bs[i])));
				}
			}
		}
		if (w) {
			ws = w.split(";");
			for (var i=0;i<ws.length;i++) {
				if (is_regex) {
					whiteListRegs.push(new RegExp(ws[i]));
				}
				else {
					whiteListRegs.push(new RegExp(getRegex(ws[i])));
				}
			}
		}
	}
	
	function getRegex(p) {
		var reg = p.replace(convertReg, "\\$&");
		reg = reg.replace(wildcardReg,".*?");
		return reg;
	}
	
	function isValidUrl(url) {
		if (whiteListRegs.length == 0 && blackListRegs.length == 0) return true;
		var m = false;
		var msg = "";		
		x.debug("check url: " + url);
		msg = "NOT VALID: " + url + " is not allowed!";							
		for (var i=0;i<blackListRegs.length;i++) {
			if (blackListRegs[i].test(url)) {
				m = true;
				break;
			}
		}
		if (m) {
			x.debug(msg);				
			return false; 
		}
		if (whiteListRegs.length == 0) {
			return true;
		}
		for (var i=0;i<whiteListRegs.length;i++) {
			if (whiteListRegs[i].test(url)) {
				m = true;
				break;
			}
		}
		if (!m) {								
			x.debug(msg);
			return false;
		}
		return true;	
	}
	
	function getRequestValue(url,key) {
		//x.debug("getRequestValue with url salt");
		return getHash(url+key);
	}
	
	function getHash(str) {
		function toHexString(charCode) {
			return ("0" + charCode.toString(16)).slice(-2);
		}
		var cv = Cc["@mozilla.org/intl/scriptableunicodeconverter"].createInstance(Ci.nsIScriptableUnicodeConverter);
		var ch = Cc["@mozilla.org/security/hash;1"].createInstance(Ci.nsICryptoHash);
		cv.charset = "UTF-8";
		//var arrUrl = {};
		var strKey = str;
		var arrKey = {};
		//var urlData = cv.convertToByteArray(url, arrUrl);
		var keyData = cv.convertToByteArray(strKey, arrKey);
		ch.init(ch.SHA256);
		//ch.update(urlData, urlData.length);
		ch.update(keyData, keyData.length);
		var hash = ch.finish(false);
		var s = [toHexString(hash.charCodeAt(i)) for (i in hash)].join("");
		return s;
	}
	
	String.prototype.trim = function () {
		return this.replace(/^\s*/, "").replace(/\s*$/, "");
	}
	
	Array.prototype.contains = function(obj) {
		var i = this.length;
		while (i--) {
			if (this[i] === obj) {
				return true;
			}
		}	
		return false;
	}
	
	/* export public functions */
	return {
		toString 			: 	toString,
		init				:	init,
		onunload			:	onunload,
		onclose				:	onclose,
		shutdown			:	shutdown,
		reload				:	reload,
		restart				:	restart,
		load				:	load,
		back				:	back,
		forward				:	forward,
		showAll				:	showAll,
		showError			:	showError,
		toggleHidden			:	toggleHidden,
		tryAgain			:	tryAgain	
	};	
}());

