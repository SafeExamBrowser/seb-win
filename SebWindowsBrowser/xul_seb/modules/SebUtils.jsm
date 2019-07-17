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
	{ appinfo, io, prefs, scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services,
	{ FileUtils } = Cu.import("resource://gre/modules/FileUtils.jsm",{}),
	{ OS } = Cu.import("resource://gre/modules/osfile.jsm");
Cu.import("resource://gre/modules/XPCOMUtils.jsm");

/* Services */
const 	hph = io.getProtocolHandler("http").QueryInterface(Ci.nsIHttpProtocolHandler),
	fph = io.getProtocolHandler("file").QueryInterface(Ci.nsIFileProtocolHandler),
	reg = Cc['@mozilla.org/chrome/chrome-registry;1'].getService(Ci.nsIChromeRegistry);

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"sl","resource://modules/SebLog.jsm","SebLog");
XPCOMUtils.defineLazyModuleGetter(this,"sg","resource://modules/SebConfig.jsm","SebConfig");

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");
scriptloader.loadSubScript("resource://globals/const.js");

/* ModuleGlobals */
let	base = null,
	seb = null;

this.SebUtils =  {

	checkUrl : /(http|https|file)\:\/\/.*/i,
	checkRelativeConfig : /^[^\/\\\s]+\.json$/,
	checkAbsoluteConfig : /[\/\\]+.*?\.json$/,
	checkP12 : /\.p12$/i,
	checkCRT : /\.crt$/i,
	checkJSON : /^\s*?\{.*\}\s*?$/,
	checkBase64 : /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{4})$/,
	userAgent : "",
	
	init : function(obj) {
		base = this;
		seb = obj;		
		var httpHandler = Cc["@mozilla.org/network/protocol;1?name=http"].getService(Ci.nsIHttpProtocolHandler);
		base.userAgent = httpHandler.userAgent;
		sl.out("SebUtils initialized: " + seb + " " + base.userAgent);
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
	
	isBase64 : function (str) {
		if (!str) {
			return false;
		} 
		return base.checkBase64.test(str);
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
			let rel = base.checkRelativeConfig.test(url.toString());
			let f = null;
			try {
				if (rel) {
					f = FileUtils.getFile("CurProcD",[url], null);
				}
				else {
					f = FileUtils.File(url);
				}
				if (!f || !f.exists()) {
					sl.err("wrong url for getJSON: " + url);
					callback(false);
					return;
				}
				else {
					url = fph.newFileURI(f).spec;
				}
			}
			catch (e) {
				sl.err("could not load url: " + url + "\n" + e);
				callback(false);
				return;
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
	
	mergeJSON : function (source1,source2) {
		var mergedJSON = Object.create(source2);// Copying Source2 to a new Object

		for (var attrname in source1) {
			if(mergedJSON.hasOwnProperty(attrname)) {
				if ( source1[attrname]!=null && source1[attrname].constructor==Object ) {
					mergedJSON[attrname] = base.mergeJSON(source1[attrname], mergedJSON[attrname]);
				} 

			} 
			else {
				//else copy the property from source1
				mergedJSON[attrname] = source1[attrname];

			}
		}
		return mergedJSON;
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
			//sl.debug(reg);
			return reg.convertChromeURL(base.getUri(url))
		}
		catch(e) {
			sl.err("Error in getUriFromChrome: " + e);
		}
	},
	
	getAppPath : function() {
		//return 	fph.getFileFromURLSpec(base.getUriFromChrome(SEB_URL).replace('/chrome/content/seb/seb.xul','')).path;
		return fph.getFileFromURLSpec(base.getUriFromChrome(SEB_URL).spec.replace('/chrome/content/seb/seb.xul','')).path;
	},
	
	getDicsPath : function() {
		return fph.getFileFromURLSpec(base.getUriFromChrome(SEB_URL).spec.replace('/chrome/content/seb/seb.xul','/dictionaries')).path;
	},
	
	getExternalDicsPath : function() { // only for windows hosting
		return OS.Path.join(OS.Path.dirname(OS.Constants.Path.profileDir),"Dictionaries");
	},
	
	getConfig : function(v,t,d) { // val, type, default
		return (typeof seb.config[v] === t) ? seb.config[v] : d;
	},
	
	getBool : function (b) {
		var ret;
		switch (typeof(b)) {
			case "string" :
				ret = (b == "false" || b == "undefined") ? false : true;
				break;
			case "number" :
				ret = (b > 0) ? true : false;
				break;
			case "boolean" :
				ret = b;
				break;
			case "object" :
				ret = (b === null) ? false : true;
				break;
			case "undefined" :
				ret = false;
				break;
			default :
				ret = true;
		}
		return ret;
	},
	
	getNumber : function (x) {
		return parseInt(x,10);
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
			return decodeURIComponent(escape(atob(data)));
		}
		return atob(data);
	},
	
	encodeBase64 : function (data,charset) {
		if (base.isUTF8(charset)) {
			return btoa(unescape(encodeURIComponent(data)));
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
		var s = "";
		for (var i=0; i<hash.length; i++) {
			s += toHexString(hash.charCodeAt(i));
		};
		return s;
		/*
		var s = [toHexString(hash.charCodeAt(i)) for (i in hash)].join(""); does not work anymore??
		return s;
		*/ 
	},
	
	UintToString : function(array) {
		var out, i, len, c;
		var char2, char3;

		out = "";
		len = array.length;
		i = 0;
		while(i < len) {
			c = array[i++];
			switch(c >> 4) { 
				case 0: case 1: case 2: case 3: case 4: case 5: case 6: case 7:
					// 0xxxxxxx
					out += String.fromCharCode(c);
					break;
				case 12: case 13:
					// 110x xxxx   10xx xxxx
					char2 = array[i++];
					out += String.fromCharCode(((c & 0x1F) << 6) | (char2 & 0x3F));
					break;
				case 14:
					// 1110 xxxx  10xx xxxx  10xx xxxx
					char2 = array[i++];
					char3 = array[i++];
					out += String.fromCharCode(((c & 0x0F) << 12) |
					       ((char2 & 0x3F) << 6) |
					       ((char3 & 0x3F) << 0));
				break;
			}
		}

		return out;
	},
	
	getEndianness : function () {
		var a = new ArrayBuffer(4);
		var b = new Uint8Array(a);
		var c = new Uint32Array(a);
		b[0] = 0xa1;
		b[1] = 0xb2;
		b[2] = 0xc3;
		b[3] = 0xd4;
		if (c[0] === 0xd4c3b2a1) {
			return LITTLE_ENDIAN;
		}
		if (c[0] === 0xa1b2c3d4) {
			return BIG_ENDIAN;
		} 
		else {
			throw new Error('Unrecognized endianness');
		}
	},
	
	swapBytes : function (buf, size) {
		var bytes = Uint8Array(buf);
		var len = bytes.length;
		if(size == 'WORD') {
			var holder;
			for(var i =0; i<len; i+=2) {
				holder = bytes[i];
				bytes[i] = bytes[i+1];
				bytes[i+1] = holder;
			}
		}
		else if(size == 'DWORD') {
			var holder;
			for(var i =0; i<len; i+=4) {
				holder = bytes[i];
				bytes[i] = bytes[i+3];
				bytes[i+3] = holder;
				holder = bytes[i+1];
				bytes[i+1] = bytes[i+2];
				bytes[i+2] = holder;
			}
		}
	},
	
	globToRegex : function (str) {
		//return new RegExp('^'+base.pregQuote(str).replace(/\\\*/g, '.*?').replace(/\\\?/g, '.') + '$');
		return new RegExp('^'+base.pregQuote(str).replace(/\\\*/g, '.*?') + '$');
	},
	
	pregQuote : function  (str, delimiter) {
	    return (str + '').replace(new RegExp('[.\\\\+*?\\[\\^\\]$(){}=!<>|:\\' + (delimiter || '') + '-]', 'g'), '\\$&');
	},

	escapeRegExp : function (str) {
		return str.replace(/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g, "\\$&");
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
