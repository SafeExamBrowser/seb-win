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
this.EXPORTED_SYMBOLS = ["SebUtils"];

/* Modules */
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ appinfo, io, scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services,
	{ FileUtils } = Cu.import("resource://gre/modules/FileUtils.jsm",{});
Cu.import("resource://gre/modules/XPCOMUtils.jsm");

/* Services */
const 	hph = io.getProtocolHandler("http").QueryInterface(Ci.nsIHttpProtocolHandler),
	fph = io.getProtocolHandler("file").QueryInterface(Ci.nsIFileProtocolHandler),
	reg = Cc['@mozilla.org/chrome/chrome-registry;1'].getService();

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"sl","resource://modules/SebLog.jsm","SebLog");

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");

/* ModuleGlobals */
let	base = null,
	seb = null;

this.SebUtils =  {

	checkUrl : /(http|https|file)\:\/\/.*/i,	
	checkP12 : /\.p12$/i,
	checkCRT : /\.crt$/i,
	checkJSON : /^\s*?\{.*\}\s*?$/,
	checkBase64 : /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{4})$/,
	
	init : function(obj) {
		base = this;
		seb = obj;
		sl.out("SebUtils initialized: " + seb);
	},
	
	getCmd : function (k) { // convert strings to data types
		let v = seb.cmdline.handleFlagWithParam(k,false); // beware this will remove the key and the value from the commandline list!
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
	
	getJSON : function (data,callback) {	
		// check base64
		if (base.checkBase64.test(data)) {
			try {
				var obj = JSON.parse(base.decodeBase64(data));
				if (typeof obj === "object") {
					callback(obj);
						return;
				}
				else {
					callback(false);
					return;
				}
			}
			catch(e) {
				sl.err(e);
				callback(false);
				return;
			}
		}
		// check json
		if (base.checkJSON.test(data)) {
			try {
				var obj = JSON.parse(data);
				if (typeof obj === "object") {
					callback(obj);
					return;
				}
				else {
					callback(false);
					return;
				}
			}
			catch(e) {
				sl.err(e);
				callback(false);
				return;
			}
		}
		// check url
		let url = data;
		var isUrl = base.checkUrl.test(url.toString());
		
		if (!isUrl) {
			let f = FileUtils.File(url);
			if (!f || !f.exists()) {
				sl.err("wrong url for getJSON: " + url);
				callback(false);
				return;
			}
			else {
				url = fph.newFileURI(f).spec;
			}
		}
		sl.debug("try to load json object: " + url);
		Cc["@mozilla.org/network/io-service;1"].getService(Ci.nsIIOService).newChannel(url, "", null).asyncOpen({
			_data: "",
			onDataAvailable: function (req, ctx, str, del, n) {
				var ins = Cc["@mozilla.org/scriptableinputstream;1"].createInstance(Ci.nsIScriptableInputStream)
				ins.init(str);
				this._data += ins.read(ins.available());
			},
			onStartRequest: function () {},
			onStopRequest: function () {
				try {
					var obj = JSON.parse(this._data);
					callback(obj);
				}
				catch(e) {
					sl.err("error: " + e);
					callback(false);
				}
			}
		}, null);
	},
		
	getUrl : function () {
		let url = base.getCmd("url");
		if (url !== null) {
			return url;
		}
		url = seb.config["startURL"];
		if (url !== undefined) {
			return url;
		}
		return false;
	},
	
	getUri : function (url) {
		try {
			return io.newURI(url,null,null);
		}
		catch (e) {
			sl.err("Error in getUri: " + e);
		}
	},
	
	getUriFromChrome : function(url) {
		try {
			sl.debug(reg);
			return reg.convertChromeURL(base.getUri(url))
		}
		catch(e) {
			sl.err("Error in getUriFromChrome: " + e);
		}
	},
	
	getConfig : function(v,t,d) { // val, type, default
		return (typeof seb.config[v] === t) ? seb.config[v] : d;
	},
	
	getBool : function (b) {
		var ret;
		switch (b) {
			case "1":
			case 1:
			case "true":
			case true :
				ret = true;
			break;
			default: 
				ret = false;
		}
		return ret;
	},
	
	getLocStr : function (k) {
		return seb.locs.getString(k);
	},
	
	getConstStr : function(k) {
		return seb.consts.getString(k);
	},
	
	isUTF8 : function (charset) {
		let type = typeof charset;
		if (type === "undefined") {
			return false;
		}
		if (type === "string" && charset.toLowerCase() === "utf-8") {
			return true;
		}
		throw new Error("The charset argument can be only 'utf-8'");
	},
	
	decodeBase64 : function (data,charset) {
		if (base.isUTF8(charset)) {
			return decodeURIComponent(escape(atob(data)))
		}
		return atob(data);
	},
	
	encodeBase64 : function (data,charset) {
		if (base.isUTF8(charset)) {
			return btoa(unescape(encodeURIComponent(data)))
		}
		return btoa(data);
	},
	
	getHash : function (str) {
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
	},
	
	isEmpty : function (obj) {
		if (base.isArray(obj)) {
			return (obj.length == 0);
		}
		if (base.isObject(obj)) {
			return (Object.getOwnPropertyNames(obj).length == 0);
		}
		sl.debug("su.isEmpty() : no array or object");
		return true;
	},
	
	isArray : function(arr) {
		return Array.isArray(arr);
	},
	
	isObject : function(obj) {
		return obj === Object(obj);
	}
	
}
