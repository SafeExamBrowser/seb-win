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
	{ appinfo, scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services;
Cu.import("resource://gre/modules/XPCOMUtils.jsm");

/* Services */

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");

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
	ars : {},
	msgHandler : {},

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
			"Shutdown" : base.handleShutdown
		};
		base.initAdditionalResources();
		sl.out("SebHost initialized: " + seb);
	},
	
	initAdditionalResources : function (obj) {
		//var ar = {};
		if (obj === undefined) { // initial load
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
	
	messageSocketListener : function (e) {
		sl.debug("setMessageSocketListener");
		messageSocketWin = messageSocketBrowser.contentWindow.wrappedJSObject;
		const { WebSocket } = messageSocketWin;
		
		try {
			messageSocket = new WebSocket(socket);
			sl.debug("messageSocket: " + typeof messageSocket)
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
			messageServer = false;
		}; 
		
		messageSocket.onmessage = function(evt) { 
			// ToDo: message handling !!
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
							base.msgHandler[msgObj["Handler"]].call(base,msgObj["Opts"]);
						}
						catch(e) {
							sl.err(e);
						}
					}
					else {
						sl.debug("1 messageSocket: not handled msg: " + evt.data); 
					}
				}
				catch(e) {
					sl.debug("2 messageSocket: not handled msg: " + evt.data); 
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
		messageSocketBrowser.setAttribute("src","chrome://seb/content/message_socket.html"); // to fire window DOMContentLoaded
	},
	
	closeMessageSocket : function() {
		if (base.messageServer) {
			messageSocket.close();
		}
	},
	
	handleArs : function(opts) {
		try {
			var url = "";
			if (base.ars[opts.id].url) {
				url = base.ars[opts.id].url;
			}
			else {
				url = "file://" + opts.path + base.ars[opts.id].resourceDataFilename.replace("\\\\","/");
			}
			if (url != "") {
				sl.debug("try to open additional resource: " + url);
				if (/\.pdf$/i.test(url)) {
					sw.openPdfViewer(url);
				}
				else { // ToDo: mimetype handling
					sw.openDistinctWin(url);
				}
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
			// maybe controlled seb quit before?
			// base.quitFromHost(); no: because of application loop in netpoint
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
			sl.err("Error " + e);
		}
	},
	
	shutdownWindowsHost : function() {
		sl.debug("shutdown windows host is not defined, but i can shutdown seb client :-)");
		base.quitFromHost();	
	},
	
	shutdownMacHost : function() {
		sl.debug("shutdown mac host is not defined, but i can shutdown seb client :-)");
		base.quitFromHost();
	},
	
	
	shutdown : function() {
		let os = appinfo.OS.toUpperCase();
		switch (os) { // line feed for dump messages
			case "WINNT" :
				base.shutdownWindowsHost();
				break;
			case "UNIX" :
			case "LINUX" :
				base.shutdownLinuxHost();
				break;
			case "DARWIN" :
				base.shutdownMacHost();
				break;
			default :
				// do nothing
		}
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
			catch(e){};
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
	}
}
