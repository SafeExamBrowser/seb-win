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
this.EXPORTED_SYMBOLS = ["SebConfig"];

/* Modules */
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ prefs, scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services,
	{ FileUtils } = Cu.import("resource://gre/modules/FileUtils.jsm",{});
Cu.import("resource://gre/modules/XPCOMUtils.jsm");

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");
scriptloader.loadSubScript("resource://globals/const.js");

/* Services */

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"sl","resource://modules/SebLog.jsm","SebLog");
XPCOMUtils.defineLazyModuleGetter(this,"su","resource://modules/SebUtils.jsm","SebUtils");

/* SebGlobals */

/* ModuleGlobals */
let	base = null,
	seb = null,
	uaReg = new RegExp(/^.*?rv\:(.+)?\).*?(seb\/.*)$/);

this.SebConfig =  {
	defaultFile : null,
	prefsMap : {},
	callback : null,

	init : function(obj) {
		base = this;
		seb = obj;
		base.prefsMap["browserUserAgent"] = base.browserUserAgent;
		base.prefsMap["browserZoomFull"] = base.browserZoomFull;
		base.prefsMap["zoomMaxPercent"] = base.zoomMaxPercent;
		base.prefsMap["zoomMinPercent"] = base.zoomMinPercent;
		base.prefsMap["pluginEnableFlash"] = base.pluginEnableFlash;
		base.prefsMap["pluginEnableJava"] = base.pluginEnableJava;
		base.prefsMap["spellcheckDefault"] = base.spellcheckDefault;
        base.prefsMap["openDownloads"] = base.openDownloads;
		sl.out("SebConfig initialized: " + seb);
	},

	initConfig : function(callback) {
		base.callback = callback;
		base.initDefaultConfig();
	},

	initDefaultConfig : function() {
		sl.debug("initDefaultConfig");

		function cb(obj) {
			if (typeof obj == "object") {
				sl.debug("default config object found");
				seb.defaultConfig = obj;
				base.initCustomConfig.call(null,null);
			}
		}
		base.defaultFile = FileUtils.getFile("CurProcD",["default.json"], null);
		if (base.defaultFile.exists()) {
			su.getJSON(base.defaultFile.path,cb);
		}
		else {
				sl.err("no default config found!");
		}
	},


	initCustomConfig : function (config) {
		sl.debug("initCustomConfig");
		function cb(obj) {
			if (typeof obj == "object") {
				sl.debug("custom config object found");
				seb.config = su.mergeJSON(obj, seb.defaultConfig);
				sl.info(JSON.stringify(seb.config));

				if (!su.isEmpty(seb.config.sebPrefs)) {
					base.setPrefs(seb.config.sebPrefs);
				}

				if (!su.isEmpty(seb.config.sebPrefsMap)) {
					base.setPrefsMap(seb.config.sebPrefsMap);
				}
				base.callback.call(null);
			}
		}
		if (config) { // for reconfiguring on-the-fly
			sl.debug("initCustomConfig: reconfiguration");
			su.getJSON(config.trim(), cb);
		}
		else {
			let configParam = su.getCmd("config");
			let configFile = FileUtils.getFile("CurProcD",["config.json"], null);
			if (configParam != null) {
				sl.debug("config param found: " + configParam);
				su.getJSON(configParam.trim(), cb);
			}
			else {
				if (configFile.exists()) {
					su.getJSON(configFile.path.trim(),cb);
				}
				else {
					sl.err("no config param and no default config.json!");
				}
			}
		}
	},

	setPref : function (k,v) {
		switch (typeof v) {
			case "string" :
				sl.debug("setCharPref: " + k + ":" + v);
				prefs.setCharPref(k,v);
			break
			case "number" :
				sl.debug("setIntPref: " + k + ":" + v);
				prefs.setIntPref(k,v);
			break;
			case "boolean" :
				sl.debug("setBoolPref: " + k + ":" + v);
				prefs.setBoolPref(k,v);
			break;
			default :
				sl.debug("no pref type: " + k + ":" + v);
		}
	},

	setPrefs : function (ps) {
		sl.debug("setPrefs from config object");
		for (var k in ps) {
			var v = ps[k];
			sl.debug("setPref: " + k + ":" + v);
			base.setPref(k, v);
		}
	},

	setPrefsMap : function (pm) {
		sl.debug("setPrefsMap from config object");
		for (var k in pm) {
			//sl.debug("typeof pm: " + typeof pm[k]);
			var v = null;
			if (typeof pm[k] == "object" && typeof pm[k].cb == "string") {
				if (typeof base.prefsMap[pm[k].cb] == "function") {
					v = base.prefsMap[pm[k].cb].call(null,k);
				}
				else {
					sl.debug("no prefMap function: " + pm[k].cb);
				}
			}
			if (typeof pm[k] == "string") {
				v = seb.config[pm[k]];
			}
			base.setPref(k,v);
		}
	},
	
	browserUserAgent : function(param) {
		var ret = "";
		var topt = su.getConfig("touchOptimized","boolean",false);
		var uaPref = (topt === false) ? su.getConfig(BROWSER_UA_DESKTOP_PREF,"number",0) : su.getConfig(BROWSER_UA_TOUCH_PREF,"number",0);
		var bua = su.getConfig("browserUserAgent","string","");
		switch (uaPref) {
			case BROWSER_UA_DESKTOP_DEFAULT :
			case BROWSER_UA_TOUCH_DEFAULT :
				var match = uaReg.exec(su.userAgent);
				if (match) {
					//sl.out(match[0]);
					//sl.out(match[1]);
					//sl.out(match[2]);
					ret = su.userAgent.replace(match[2],"Firefox/"+match[1]) + " " + bua;
					if (topt === true) {
						sl.out("ret=" + ret);
						ret = ret.replace(match[1]+")",match[1]+"; Touch)");
					}
				}
				else {
					sl.debug("Could not match seb user-agent: " + su.userAgent);
					sl.debug("Maybe User-Agent is already configured in prefs.js");
					ret = su.userAgent;
				}
				break;
			case BROWSER_UA_DESKTOP_CUSTOM :
			case BROWSER_UA_TOUCH_IPAD :
			case BROWSER_UA_TOUCH_CUSTOM :
				if (topt === true) {
					ret = (uaPref === BROWSER_UA_TOUCH_IPAD) ? su.getConfig(BROWSER_UA_TOUCH_IPAD_PREF,"string","") : su.getConfig(BROWSER_UA_TOUCH_CUSTOM_PREF,"string","");
				}
				else {
					ret = su.getConfig(BROWSER_UA_DESKTOP_CUSTOM_PREF,"string","")
				}
				ret = ret + " " + bua;
				break;
		}
		return ret;
	},
	
	browserZoomFull : function(param) {
		var ret = (seb.config["zoomMode"] == 0) ? true : false;
		return ret;
	},

	zoomMaxPercent : function(param) {
		var ret = (seb.config["enableZoomPage"] == false && seb.config["enableZoomText"] == false) ? 100 : 300;
		return ret;
	},

	zoomMinPercent : function(param) {
		var ret = (seb.config["enableZoomPage"] == false && seb.config["enableZoomText"] == false) ? 100 : 30;
		return ret;
	},

	pluginEnableFlash : function(param) {
		var ret = (seb.config["enablePlugIns"] == true) ? 2 : 0;
		return ret;
	},

	pluginEnableJava : function(param) {
		var ret = (seb.config["enableJava"] == true) ? 2 : 0;
		return ret;
	},

	spellcheckDefault : function(param) {
		var ret = (seb.config["allowSpellCheck"] == true) ? 2 : 0;
		return ret;
	},
    
    openDownloads : function(param) {
		return !su.getConfig("openDownloads","boolean",false);
	}
}
