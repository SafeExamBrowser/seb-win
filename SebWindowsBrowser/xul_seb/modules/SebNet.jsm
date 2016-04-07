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

this.EXPORTED_SYMBOLS = ["SebNet"];

/* Modules */
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ appinfo, prefs, scriptloader, io } = Cu.import("resource://gre/modules/Services.jsm").Services;
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
let 	seb = null,
	base = null,
	whiteListRegs =	[],
	blackListRegs = [],
	mimeTypesRegs = {
		flash : new RegExp(/^application\/x-shockwave-flash/),
		pdf : new RegExp(/^application\/(x-)?pdf/)
	},
	convertReg = /[-\[\]\/\{\}\(\)\+\?\.\\\^\$\|]/g,
	wildcardReg = /\*/g,
	httpReg = new RegExp(/^http\:/i);
	reqHeader = "",
	reqKey = null,
	reqSalt = null,
	sendBrowserExamKey = null,
	forceHTTPS = false,
	blockHTTP = false;

this.SebNet = {
		
	httpRequestObserver : {
		observe	: function(subject, topic, data) {
			if (topic == "http-on-modify-request" && subject instanceof Ci.nsIHttpChannel) {
				//sl.debug("http request: "+ subject.URI.spec);
				//sl.debug(data);
				//subject.QueryInterface(Ci.nsIHttpChannel);
				//sl.debug(subject.getRequestHeader('Accept'));
				//sl.debug(subject.referrer);
				let origUrl = "";
				let url = "";
				try {
					origUrl = subject.URI.spec;
					url = origUrl.split("#"); // url fragment is not transmitted to the server!
					url = url[0];
					//sl.debug("request: " + url);
					let urlTrusted = su.getConfig("urlFilterTrustedContent","boolean",true);
					if (!urlTrusted) {
						if (!base.isValidUrl(url)) {
							subject.cancel(Cr.NS_BINDING_ABORTED);
							return;
						}
					}
								
					//if (sendReqHeader && /text\/html/g.test(subject.getRequestHeader('Accept'))) { // experimental
					if (sendBrowserExamKey) {
						var k;
						if (reqSalt) {								
							k = base.getRequestValue(url, reqKey);
							//sl.debug("get req value: " + url + " : " + reqKey + " = " + k);
						}
						else {
							k = reqKey;
						}
						subject.setRequestHeader(reqHeader, k, false);
					}
					if (httpReg.test(url)) {
						if (blockHTTP) {
							sl.debug("block http request");
							subject.cancel(Cr.NS_BINDING_ABORTED);
							return;
						}
						if (forceHTTPS) { // non common browser behaviour, experimental
							sl.debug("try redirecting request to https: " + origUrl);
							try {
								subject.redirectTo(io.newURI(origUrl.replace("http:","https:"),null,null));
							}
							catch(e) {
								sl.debug(e + "\nurl: " + url);
							}
						}
					}
				}
				catch (e) {
					sl.debug(e + "\nurl: " + url);
				}
			}
		},

		get observerService() {  
			return Cc["@mozilla.org/observer-service;1"].getService(Ci.nsIObserverService);  
		},
	  
		register: function() {  
			this.observerService.addObserver(this, "http-on-modify-request", false);  
		},  
	  
		unregister: function()  {  
			this.observerService.removeObserver(this, "http-on-modify-request");  
		}  
	},
	
	httpResponseObserver : { // experimental
		observe	: function(subject, topic, data) {
			if (topic == ("http-on-examine-response" || "http-on-examine-cached-response") && subject instanceof Ci.nsIHttpChannel) {
				let mimeType = "";
				let url = "";
				try {
					url = subject.URI.spec;
					mimeType = subject.getResponseHeader("Content-Type");
					if (mimeTypesRegs.pdf.test(mimeType) && !/\.pdf$/i.test(url) && su.getConfig("sebPdfJsEnabled","boolean", true)) { // pdf file requests should already be captured by SebBrowser
						subject.cancel(Cr.NS_BINDING_ABORTED);
						sw.openPdfViewer(url);
					}
				}
				catch (e) {
					//sl.debug(e + "\nurl: " + url + "\nmimetype: " + mimeType);
				} 
			} 
		},

		get observerService() {  
			return Cc["@mozilla.org/observer-service;1"].getService(Ci.nsIObserverService);  
		},
	  
		register: function() {  
			this.observerService.addObserver(this, "http-on-examine-response", false);
			this.observerService.addObserver(this, "http-on-examine-cached-response", false);
		},  
	  
		unregister: function()  { 
			this.observerService.removeObserver(this, "http-on-examine-response", false);
			this.observerService.removeObserver(this, "http-on-examine-cached-response", false); 
		}
	},
	
	init : function(obj) {
		base = this;
		seb = obj;
		base.setListRegex();
		base.setReqHeader();
		base.setSSLSecurity();
		sl.out("SebNet initialized: " + seb);
	},
	
	initProxies : function() {
		if (typeof seb.config["proxies"] != "object") { sl.debug("no proxies defined."); return; }
		let proxies = su.getConfig("proxies","object",null);
		let p = base.getProxyType(proxies);
		if (typeof p === "number") {
			prefs.setIntPref("network.proxy.type",p);
			sl.debug("network.proxy.type:"+p);
		}
		p = proxies["AutoConfigurationURL"];
		if (typeof p === "string" && p != "") {
			prefs.setCharPref("network.proxy.autoconfig_url",p);
			sl.debug("network.proxy.autoconfig_url:"+p);
		}
		p = proxies["HTTPProxy"];
		if (typeof p === "string" && p != "") {
			prefs.setCharPref("network.proxy.http",p);
			sl.debug("network.proxy.http:"+p);
		}
		p = proxies["HTTPPort"];
		if (typeof p === "number") {
			prefs.setIntPref("network.proxy.http_port",p);
			sl.debug("network.proxy.http_port:"+p);
		}
		p = proxies["HTTPSProxy"];
		if (typeof p === "string" && p != "") {
			prefs.setCharPref("network.proxy.ssl",p);
			sl.debug("network.proxy.ssl:"+p);
		}
		p = proxies["HTTPSPort"];
		if (typeof p === "number") {
			prefs.setIntPref("network.proxy.ssl_port",p);
			sl.debug("network.proxy.ssl_port:"+p);
		}
		p = proxies["FTPProxy"];
		if (typeof p === "string" && p != "") {
			prefs.setCharPref("network.proxy.ftp",p);
			sl.debug("network.proxy.ftp:"+p);
		}
		p = proxies["FTPPort"];
		if (typeof p === "number") {
			prefs.setIntPref("network.proxy.ftp_port",p);
			sl.debug("network.proxy.ftp_port:"+p);
		}
		p = proxies["SOCKSProxy"];
		if (typeof p === "string" && p != "") {
			prefs.setCharPref("network.proxy.socks",p);
			sl.debug("network.proxy.socks:"+p);
		}
		p = proxies["SOCKSPort"];
		if (typeof p === "number") {
			prefs.setIntPref("network.proxy.socks_port",p);
			sl.debug("network.proxy.socks_port:"+p);
		}
		p = proxies["ExceptionsList"];
		if (typeof p === "object" && p != null) {
			p = p.join(",") + ",localhost,127.0.0.1";
			prefs.setCharPref("network.proxy.no_proxies_on",p);
			sl.debug("network.proxy.no_proxies_on:"+p);
		}
	},
	
	getProxyType : function(proxies) {
		let p = proxies["AutoDiscoveryEnabled"];
		if ( (typeof p === "boolean") && p) {
			return 4;
		}
		p = proxies["AutoConfigurationEnabled"];
		// auto config url
		if ( (typeof p === "boolean") && p) {
			return 2;
		}
		// http(s) proxy
		p = proxies["HTTPEnable"];
		let p2 = proxies["HTTPSEnable"];
		if ( (typeof p === "boolean" && p) || (typeof p2 === "boolean" && p2) ) {
			return 1;
		}
		return null;
	},
	
	setListRegex : function() { // for better performance compile RegExp objects and push them into arrays
		sl.debug("setListRegex"); 
		//sl.debug(typeof seb.config["urlFilterRegex"]);
		let is_regex = (typeof seb.config["urlFilterRegex"] === "boolean") ? seb.config["urlFilterRegex"] : false;
		sl.debug("urlFilterRegex: " + is_regex);
		
		let b = seb.config["blacklistURLFilter"];
		let w = seb.config["whitelistURLFilter"];
		
		switch (typeof b) {
			case "string" :
				if (b == "") {
					b = false;
				}
				else {
					b = b.split(";");
				}
			break;
			case "object" :
				// do nothing
			break;
			default :
				b = false;
		}
		
		switch (typeof w) {
			case "string" :
				if (w == "") {
					w = false;
				}
				else {
					w = w.split(";");
				}
			break;
			case "object" :
				// do nothing
			break;
			default :
				w = false;
		}
			
		if (b) {
			for (var i=0;i<b.length;i++) {
				sl.debug("Add blacklist pattern: " + b[i]);
				if (is_regex) {
					blackListRegs.push(new RegExp(b[i]));
				}
				else {
					blackListRegs.push(new RegExp(base.getRegex(b[i])));
				}
			}
		}
		if (w) {
			for (var i=0;i<w.length;i++) {
				sl.debug("Add whitelist pattern: " + w[i]);
				if (is_regex) {
					whiteListRegs.push(new RegExp(w[i]));
				}
				else {
					whiteListRegs.push(new RegExp(base.getRegex(w[i])));
				}
			}
		}
	},
	
	getRegex : function (p) {
		var reg = p.replace(convertReg, "\\$&");
		reg = reg.replace(wildcardReg,".*?");
		return reg;
	},
	
	isValidUrl : function (url) {
		if (whiteListRegs.length == 0 && blackListRegs.length == 0) return true;
		var m = false;
		var msg = "";		
		sl.debug("check url: " + url);
		msg = "NOT VALID: " + url + " is not allowed!";							
		for (var i=0;i<blackListRegs.length;i++) {
			if (blackListRegs[i].test(url)) {
				m = true;
				break;
			}
		}
		if (m) {
			sl.debug(msg);				
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
			sl.debug(msg);
			return false;
		}
		return true;	
	},
	
	setReqHeader : function() {
		sl.debug("setReqHeader");
		sendBrowserExamKey = su.getConfig("sendBrowserExamKey","boolean",false);
		if (!sendBrowserExamKey) { return; }
		let rh = su.getConfig("browserRequestHeader","string","");
		let rk = su.getConfig("browserExamKey","string","");
		let rs = su.getConfig("browserURLSalt","boolean",true);
		
		if (rh != "" && rk != "") {
			reqHeader = rh;
			reqKey = rk;
			reqSalt = rs;
		}
	},
	
	getRequestValue : function (url,key) {
		return su.getHash(url+key);
	},
	
	setSSLSecurity : function () {
		forceHTTPS = (su.getConfig("sslSecurityPolicy","number",SSL_SEC_BLOCK_MIXED_ACTIVE) == SSL_SEC_FORCE_HTTPS);
		blockHTTP = (su.getConfig("sslSecurityPolicy","number",SSL_SEC_BLOCK_MIXED_ACTIVE) == SSL_SEC_BLOCK_HTTP);
		sl.debug("forceHTTPS: " + forceHTTPS);
		sl.debug("blockHTTP: " + blockHTTP);
	}
}
