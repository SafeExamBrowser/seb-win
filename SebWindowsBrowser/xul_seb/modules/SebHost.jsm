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

this.EXPORTED_SYMBOLS = ["SebHost"];

/* Modules */
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ appinfo, scriptloader, prompt } = Cu.import("resource://gre/modules/Services.jsm").Services;
Cu.import("resource://gre/modules/XPCOMUtils.jsm");

/* Services */

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");
scriptloader.loadSubScript("resource://globals/const.js");

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"sl","resource://modules/SebLog.jsm","SebLog");
XPCOMUtils.defineLazyModuleGetter(this,"su","resource://modules/SebUtils.jsm","SebUtils");
XPCOMUtils.defineLazyModuleGetter(this,"sw","resource://modules/SebWin.jsm","SebWin");
XPCOMUtils.defineLazyModuleGetter(this,"sb","resource://modules/SebBrowser.jsm","SebBrowser");

/* ModuleGlobals */
let 	base = null,
	seb = null,
	socket = "",
	pingtime = "",
	socketlog = false,
	messageSocketBrowser = null,
	messageSocketWin = null,
	messageSocket = null,
	elToScrollIntoView = null,
	textTypes = ['color','date','datetime','datetime-local','email','month','number','password','search','tel','text','time','url','week'];
	
	
this.SebHost = {
	
	messageServer : false,
	os : "",
	msgHandler : {},
	sendHandler : {},
	reconnectTry : 0,
	reconnectTriesReached : false,
	reconnectInterval : 0,
	reconnectMaxTries : 0,
	reconnectIntervalObj : null,

	init : function(obj) {
		base = this;
		seb = obj;
		socketlog = su.getBool(su.getCmd("socketlog"));
		this.os = appinfo.OS.toUpperCase();
		base.msgHandler = {
			"AdditionalResources" : base.handleArs,
			"DisplaySettingsChanged" : base.handleDisplaySettingsChanged,
			"Reload" : base.handleReload,
			"RestartExam" : base.handleRestartExam,
			"Close" : base.handleClose,
			"KeyboardShown" : base.handleKeyboardShown,
			"Shutdown" : base.handleShutdown,
			"SebFileTransfer" : base.handleSebFileTransfer,
			"ReconfigureAborted" : base.handleReconfigureAborted,
			"Reconfigure" : base.handleReconfigure,
			"ClearSession" : base.handleClearSession,
			"AdditionalDictionaries" : base.handleAdditionalDictionaries,
            "LockSeb" : base.handleLockSeb,
            "UnlockSeb" : base.handleUnlockSeb,
            "UserSwitchLockScreen" : base.handleUserSwitchLockScreen
		};
		base.sendHandler = {
			"SebFile" : base.sendSebFile,
			"ReconfigureAborted" : base.sendReconfigureAborted,
			"ReconfigureSuccess" : base.sendReconfigureSuccess,
			"AdditionalRessourceTriggered" : base.sendAdditionalRessourceTriggered,
			"FullScreenChanged" : base.sendFullScreenChanged,
            "ClearClipboard" : base.sendClearClipboard
		};
		base.reconnectInterval = su.getConfig("lockOnMessageSocketCloseTriesIntervallMSec","number",1000);
		base.reconnectMaxTries = su.getConfig("lockOnMessageSocketCloseTries","number",10);
		
		sl.out("SebHost initialized: " + seb);
	},
	
	messageSocketListener : function (e) {
		sl.debug("setMessageSocketListener");
		messageSocketWin = messageSocketBrowser.contentWindow.wrappedJSObject;
		const { WebSocket } = messageSocketWin;
		
		try {
			messageSocket = new WebSocket(socket);
			messageSocket.binaryType = "blob";
			//sl.debug("messageSocket: " + typeof messageSocket)
		}
		catch (e) {
			sl.debug("messageSocket connection failed: " + socket + "\n"+e);
			messageSocket = null;
			return; 
		}
		
		messageSocket.onopen = function(evt) { 			
			sl.debug("messageSocket open: " + evt); 
			messageSocket.send("seb.connected"); 
			base.messageServer = true;
			sl.debug("set ping intervall " + pingtime);
			messageSocketWin.setInterval(function() {
				messageSocket.send("seb.ping"); 
			} ,pingtime);
		}
			
		messageSocket.onclose = function(e) { 
			sl.debug("messageSocket close: " + e);
			messageSocket = null;
			base.messageServer = false;
			if (!seb.isLocked && su.getConfig("lockOnMessageSocketClose","boolean",true) && !base.reconnectTriesReached) {
				seb.lock();
			} 
		} 
		
		messageSocket.onmessage = function(evt) { 
			// ToDo: message handling !!
			// sl.debug("socket message type: " + evt.type);
			if (/^SEB\./.test(evt.data)) { // old string messages
				switch (evt.data) {
					case "SEB.close" :
						sl.debug("obsolet messageSocket handled: " + evt.data); 
						seb.hostForceQuit = true;
						sl.debug("seb.hostForceQuit: " + seb.hostForceQuit);
						break;
					case "SEB.shutdown" :  // only for socket debugging
						sl.debug("obsolet messageSocket handled: " + evt.data);
						base.shutdown();
						break;
					case "SEB.restartExam" :
						sl.debug("obsolet messageSocket handled: " + evt.data);
						sb.hostRestartUrl();
						break;
					case "SEB.displaySettingsChanged" :
						sl.debug("obsolet messageSocket handled: " + evt.data);
						sw.hostDisplaySettingsChanged();
						break;
					case "SEB.reload" :
						sl.debug("obsolet messageSocket handled: " + evt.data);
						seb.reload(null);
						break;
					case "SEB.keyboardShown" :
						sl.debug("obsolet messageSocket handled: " + evt.data);
						if (base.elToScrollIntoView != null) {
							try {
								base.elToScrollIntoView.scrollIntoView(false);
							}
							catch (e) { 
								sl.err(e);
							}
						}
						break;
					default :
						sl.debug("obsolet messageSocket: not handled msg: " + evt.data); 
				}
			}
			else { // new object handler
				try {
					var msgObj = JSON.parse(evt.data);
					if (typeof msgObj === "object") {
						//sl.debug(msgObj);
						try {
							//sl.debug(JSON.stringify(msgObj));
							if (typeof base.msgHandler[msgObj["Handler"]] == "function") {
								base.msgHandler[msgObj["Handler"]].call(base,msgObj["Opts"]);
							}
							else {
								sl.info("not handled msg: " + msgObj["Handler"]);
							}
						}
						catch(e) {
							sl.err(e);
						}
					}
					else {
						sl.info("messageSocket(1): not handled msg: " + evt.data); 
					}
				}
				catch(e) {
					sl.info("messageSocket(2): not handled msg: " + evt.data); 
				}
			}
		};
		 
		messageSocket.onerror = function(e) { 
			sl.debug("messageSocket err: " + e); 
			messageSocket = null;
			base.messageServer = false; 
		};
		
		e.preventDefault();
		e.stopPropagation();
	},
	
	setMessageSocketHandler : function (win) {
		if (!su.getConfig("browserMessagingSocketEnabled","boolean",false)) {
			return;
		}
		sl.debug("setMessageSocketHandler");
		socket = su.getConfig("browserMessagingSocket","string","");
		if (socket == "") { 
			sl.debug("no message server defined."); 
			return
		}
		pingtime = parseInt(su.getConfig("browserMessagingPingTime","number",120000));
		messageSocketBrowser = win.document.getElementById("message.socket");	
		messageSocketBrowser.addEventListener("DOMContentLoaded", base.messageSocketListener, true);
		messageSocketBrowser.setAttribute("src",MESSAGE_SOCKET_URL); // to fire window DOMContentLoaded
	},
	
	closeMessageSocket : function() {
		if (base.messageServer) {
			messageSocket.close();
		}
	},
	
	reconnect : function() {
		let w = (seb.mainWin) ? seb.mainWin : sw.getRecentWin();
		base.reconnectIntervalObj = w.setInterval(function() { base.reconnectSocket(); }, base.reconnectInterval);
	},
	
	reconnectSocket : function() {
        if (base.reconnectTriesReached) {
            base.resetReconnecting();
            return;
        }
		sl.debug("reconnectSocket...");
		if (base.messageServer) {
			sl.debug("message socket reconnected!");
			base.resetReconnecting();
			seb.unlockAll();
			return;
		}
		base.reconnectTry += 1;
		if (base.reconnectTry > base.reconnectMaxTries) {
			sl.debug("reconnectMaxTries reached");
            base.reconnectTriesReached = true;
			base.resetReconnecting();
			seb.setUnlockScreen();
			return;
		}
		if (!messageSocketBrowser) {
			sl.debug("no messageSocketBrowser");
			base.resetReconnecting();
			return;
		}
		messageSocketBrowser.reload();
	},
	
	resetReconnecting : function() {
		let w = (seb.mainWin) ? seb.mainWin : sw.getRecentWin();
		w.clearInterval(base.reconnectIntervalObj);
		base.reconnectTry = 0;
	},
	
	handleArs : function(opts) {
		try {
			let ar = seb.ars[opts.id];
			let url = ar["url"];
			let filter = ar["refererFilter"];
			let reset = ar["resetSession"];
			/*
			let confirm = ar["confirm"];
			let confirmText = (ar["confirmText"] && ar["confirmText"] != "") ? ar["confirmText"] : su.getLocStr("seb.load.warning");
			*/ 
			if (!url || url == "") {
				url = "file://" + opts.path + ar.resourceDataFilename.replace("\\\\","/");
			}
			sl.debug("try to open additional resource: " + url);
			// first check referrer
			if (filter && filter != "") {
				let w = sw.getRecentWin();
				let loadReferrer = w.content.document.location.href;
				if (loadReferrer.indexOf(filter) < 0) {
					sl.debug("loading \"" + url + "\" is only allowed if string in referrer: \"" + filter + "\"");
					return false;
				}
			}
			// check confirmation
			/*
			if (confirm) {
				var result = prompt.confirm(null, su.getLocStr("seb.load.warning.title"), confirmText);
				if (!result) {
					sl.debug("loadURL aborted by user");
					return;
				}
			}
			*/ 
			if (reset) {
				sb.clearSession();
			}
			if (/\.pdf$/i.test(url)) {
				sw.openPdfViewer(url);
			}
			else { // ToDo: mimetype handling
				sw.openDistinctWin(url);
			}
		}
		catch(e) {
			sl.err(e);
		}
	},
	
	handleDisplaySettingsChanged : function(opts) {
		sl.debug("messageSocket handled: " + opt);
		sw.hostDisplaySettingsChanged();
	},
	
	handleReload : function(opt) {
		sl.debug("messageSocket handled: " + opt);
		seb.reload(null);
	},
	
	handleRestartExam : function(opt) {
		sl.debug("messageSocket handled: " + opt);
		sb.hostRestartUrl();
	},
	
	handleClose : function(opt) {
		sl.debug("messageSocket handled: " + opt); 
		seb.hostForceQuit = true;
		sl.debug("seb.hostForceQuit: " + seb.hostForceQuit);
	},
	
	handleKeyboardShown : function(opt) {
		sl.debug("messageSocket handled: " + opt);
		if (base.elToScrollIntoView != null) {
			try {
				base.elToScrollIntoView.scrollIntoView(false);
			}
			catch (e) { 
				sl.err(e);
			}
		}
	},
	
	handleShutdown : function(opt) {
		sl.debug("messageSocket handled: " + opt);
		base.shutdown();
	},
	
	handleSebFileTransfer : function (opts) {
        sl.debug("handleSebFileTransfer handled: " + opts);
		if (opts) {
			sb.dialogHandler("SEB Config File transfer succeeded. Waiting for new SEB settings...");
		}
        if (seb.reconfState == RECONF_START) {
            seb.reconfState = RECONF_PROCESSING;
		}
	},
	
	handleReconfigureAborted : function(opts) {
		sl.debug("handleReconfigureAborted handled");
		seb.reconfState = RECONF_ABORTED;
		sb.dialogHandler("closeDialog");
		seb.reconfWinStart = false;
	}, 
	
	handleReconfigure : function (opts) {
		sl.debug("handleReconfigure handled");
		seb.reconfigure(opts.configBase64.trim());
	},
	
	handleClearSession : function (opts) {
		sl.debug("handleClearSession handled");
		sb.clearSession();
	},
	
	handleAdditionalDictionaries : function (opts) {
		sl.debug("handleAdditionalDictionaries handled");
		sb.addAdditionalDictionaries(opts);
	},
	
    handleLockSeb : function() {
        sl.debug("handleLockSeb");
        seb.lock(MODE_RECONNECT);
    },
    
    handleUnlockSeb : function() {
        sl.debug("handleUnlockSeb");
        seb.unlockAll();
    },
    
    handleUserSwitchLockScreen: function() {
        sl.debug("handleUserSwitchLockScreen");
        seb.lock(MODE_USERSWITCH);
    },
    
	sendSebFile : function (base64) {
		sl.debug("sendSebFile");
		let msg = {Handler:"SebFile",Opts:{"fileBase64":base64}};
		base.sendMessage(JSON.stringify(msg));
	},
	
	sendReconfigureAborted : function () {
		let msg = {Handler:"ReconfigureAborted",Opts:{}};
		base.sendMessage(JSON.stringify(msg));
	},
	
	sendReconfigureSuccess : function () {
		let msg = {Handler:"ReconfigureSuccess",Opts:{}};
		base.sendMessage(JSON.stringify(msg));
	},
	
	sendAdditionalRessourceTriggered : function(id) {
		let msg = {Handler:"AdditionalRessourceTriggered",Opts:{"Id":id}};
		base.sendMessage(JSON.stringify(msg));
	},
	
	sendFullScreenChanged : function(state,win) {
		//let w = (win) ? win : sw.getRecentWin();
		//sw.hideToolbar(w)
		let msg = {Handler:"FullScreenChanged",Opts:{"fullscreen":state}};
		base.sendMessage(JSON.stringify(msg));
	},
	
    sendClearClipboard : function() {
		let msg = {Handler:"ClearClipboard",Opts:{}};
		base.sendMessage(JSON.stringify(msg));
	},
    
	quitFromHost : function () {
		seb.hostForceQuit = true;
		seb.quitIgnoreWarning = true;
		seb.quitIgnorePassord = true;
		seb.allowQuit = true;
		seb.quit();
	},
	
	shutdownLinuxHost : function() {
		// create an nsIFile for the executable
		sl.out("try shutdown linux host...");
		try {
			var file = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsIFile);
			file.initWithPath("/usr/bin/sudo");
			var process = Cc["@mozilla.org/process/util;1"].createInstance(Ci.nsIProcess);
			process.init(file);
			var args = ["/usr/local/bin/shutdown_system"];
			process.run(false, args, args.length);		
		}
		catch(e) {
			//prompt.alert(mainWin, "Message from Admin", e);
			sl.err("Error " + e);
		}
	},
	
	rebootLinuxHost : function() {
        // create an nsIFile for the executable
        sl.out("try reboot linux host...");
        try {
                var file = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsIFile);
                file.initWithPath("/usr/bin/sudo");
                var process = Cc["@mozilla.org/process/util;1"].createInstance(Ci.nsIProcess);
                process.init(file);
                var args = ["/usr/local/bin/reboot_system"];
                process.run(false, args, args.length);
        }
        catch(e) {
                //prompt.alert(mainWin, "Message from Admin", e);
                sl.err("Error " + e);
        }
    },
	
    rebootWindowsHost : function() {
        // create an nsIFile for the executable
        sl.out("try reboot windows host...");
        try {
            var file = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsIFile);
            file.initWithPath('C:\\windows\\system32\\shutdown.exe');
            var process = Cc["@mozilla.org/process/util;1"].createInstance(Ci.nsIProcess);
            process.init(file);
            var args = ['-r','-t','0','-f'];
            process.run(false, args, args.length);		
        }
        catch(e) {
            //prompt.alert(mainWin, "Message from Admin", e);
            sl.err("Error " + e);
        }
    },
    
	shutdownWindowsHost : function() {
        // create an nsIFile for the executable
        sl.out("try shutdown windows host...");
        try {
            var file = Cc["@mozilla.org/file/local;1"].createInstance(Ci.nsIFile);
            file.initWithPath('C:\\windows\\system32\\shutdown.exe');
            var process = Cc["@mozilla.org/process/util;1"].createInstance(Ci.nsIProcess);
            process.init(file);
            var args = ['-s','-t','0','-f'];
            process.run(false, args, args.length);		
        }
        catch(e) {
            //prompt.alert(mainWin, "Message from Admin", e);
            sl.err("Error " + e);
        }
        //sl.debug("shutdown windows host is not defined, but i can shutdown seb client :-)");
        //base.quitFromHost();	
	},
	
	shutdownMacHost : function() {
		sl.debug("shutdown mac host is not defined, but i can shutdown seb client :-)");
		base.quitFromHost();
	},
	
	shutdown : function() {
		let os = appinfo.OS.toUpperCase();
		switch (os) { // line feed for dump messages
			case "WINNT" :
				seb.hostQuitHandler = base.shutdownWindowsHost;
				base.quitFromHost();
				break;
			case "UNIX" :
			case "LINUX" :
				seb.hostQuitHandler = base.shutdownLinuxHost;
				base.quitFromHost();
				break;
			case "DARWIN" :
				base.shutdownMacHost();
				break;
			default :
				// do nothing
		}
	},
	
	shutdownKey : function() {
		if (!su.getConfig("sebShutdownKeyEnabled","boolean",false)) {
			return;
		}
		base.shutdown();
	},
	
	reboot : function() {
                let os = appinfo.OS.toUpperCase();
                switch (os) { // line feed for dump messages
                        case "WINNT" :
                                seb.hostQuitHandler = base.rebootWindowsHost;
                                base.quitFromHost();
                                break;
                        case "UNIX" :
                        case "LINUX" :
                                seb.hostQuitHandler = base.rebootLinuxHost;
                                base.quitFromHost();
                                break;
                        case "DARWIN" :
                                // base.rebootMacHost();
                                break;
                        default :
                                // do nothing
                }
        },

	rebootKey : function() {
		if (!su.getConfig("sebRebootKeyEnabled","boolean",false)) {
			return;
		}
		base.reboot();
	},
	
	sendLog : function (str) {
		if (socketlog && messageSocket != null) {
			try {
				messageSocket.send(str);
			}
			catch(e){};
		}
	},
	
	sendMessage : function(msg) {
		if (messageSocket != null) {
			try {
				messageSocket.send(msg);
			}
			catch(e){
				sl.debug("sendMessage error: " + e);
			};
		}
		else {
			sl.debug("Ups! messageSocket is null!!");
		}
	},
	
	getFrameWidth : function() {
		switch (this.os) {
			case "DARWIN" :
				return 0;
			case "UNIX" :
			case "LINUX" :
				return 0;
			case "WINNT" :
				return 0;
		}
	},
	
	getFrameHeight : function() {
		switch (this.os) {
			case "DARWIN" :
				return 0;
			case "UNIX" :
			case "LINUX" :
				return 20;
			case "WINNT" :
				return 0;
		}
	},
	
	createScreenKeyboardController : function (win) {
		if (!su.getConfig("browserScreenKeyboard","boolean",false)) {
			return;
		}
		sl.debug("createScreenKeyboardController");
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
			//sl.debug(evt.target.tagName);
		}
		function handleClick(evt) {
			var el = evt.target;
			if (el.getAttribute("readonly")) {
				//sl.debug("readonly");
				return;
			}
			onFocus(evt);
			if (elArr.contains(el)) {
				//sl.debug("input already exists in array");
				return;
			}
			el.addEventListener("blur",onBlur,false);
			elArr.push(el);
		}
		
		function onFocus(evt) {
			sl.debug("input onFocus");
			try {
				messageSocket.send("seb.input.focus");
				base.elToScrollIntoView = evt.target;
				//evt.target.scrollIntoView();
			}
			catch(e){}
		}
		function onBlur(evt) {
			sl.debug("input onBlur");
			try {
				messageSocket.send("seb.input.blur"); 
				base.elToScrollIntoView = null;
			}
			catch(e){}
		} 
	},
	
	createFullscreenController : function (win) {
		sl.debug("createFullscreenController");
		
		win.document.onfullscreenchange = function ( evt ) {
			sw.setToolbar(win,true);
			if (evt.target != evt.currentTarget) {
				return;
			}
			if ( win.document.fullscreenElement ) {
				base.sendFullScreenChanged(true, win);
			}
			else {
				base.sendFullScreenChanged(false, win);
				
			}
		};
	}
}
