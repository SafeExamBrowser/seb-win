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

/* ***** GLOBAL winctrl SINGLETON *****

* *************************************/ 

/* 	for javascript module import
	see: https://developer.mozilla.org/en/Components.utils.import 
*/
var EXPORTED_SYMBOLS = ["winctrl"];
Components.utils.import("resource://modules/xullib.jsm");

var winctrl = (function() {
	// XPCOM Services, Interfaces and Objects
	const	x		=	xullib;
	var 	config 		= 	null,
		mapping		= 	{
						"seb.url"				: 	"startURL",
						"seb.language"				:	"browserLanguage",
						"seb.request.key"			:	browserExamKey,
						"seb.request.salt"			:	"browserURLSalt",
						"seb.mainWindow.titlebar.enabled"	:	titleBarEnabled,
						"seb.mainWindow.screen"			:	mainWindowScreen,
						"seb.popupWindows.screen"		:	popupScreen,
						"seb.taskbar.enabled"			:	"showTaskBar",
						"seb.taskbar.height"			:	"taskBarHeight",
						"seb.shutdown.enabled"			:	"allowQuit",
						"seb.popup.policy"			:	"newBrowserWindowByLinkPolicy",
						"seb.shutdown.url"			: 	"quitURL",
						"seb.shutdown.password"			: 	"hashedQuitPassword",					
						"seb.navigation.enabled"		:	"allowBrowsingBackForward",
						"seb.messaging.url"			:	"browserMessagingUrl",
						"seb.messaging.socket"			:	"browserMessagingSocket",
						"seb.messaging.ping.time"		:	"browserMessagingPingTime",
						"seb.screenkeyboard.controller"		:	browserScreenKeyboard,
						"seb.pattern.regex"		    	:	urlFilterRegex,
						"seb.trusted.content"			:	urlFilterTrustedContent,
						"seb.whitelist.pattern"			:	"whitelistURLFilter",
						"seb.blacklist.pattern"			:	"blacklistURLFilter",
						"network.proxy.type"			:	proxyType,
						"network.proxy.autoconfig_url"		:	proxyAutoConfig,
						"network.proxy.http" 			: 	proxyHttp,
						"network.proxy.http_port" 		: 	proxyHttpPort,
						"seb.removeProfile"			:	"removeBrowserProfile",
						"seb.restart.url"			:	"restartExamURL",
						"seb.touch.optimized"			:	"touchOptimized"
					},
		pos = {
				0 : "left",
				1 : "center",
				2 : "right"
		};
	
	function toString () {
			return "winctrl";
	}
	
	function init(conf,cb) {
		x.debug("init winctrl");
		config = conf;
		cb.call(null,true);
	}
	
	function hasParamMapping(param) {
		if (config === null) {
			return null;
		}
		return mapping[param];
	}
	
	function getParam(param) {
		if (config === null) {
			return null;
		}
		x.debug("ctrl.getParam: " + param);
		switch (typeof mapping[param]) {
			case "string" :
			case "number":
			case "boolean":
				if (config[mapping[param]] != null && config[mapping[param]] != undefined) {
					return config[mapping[param]];
				}
				else {
					return null;
				}
			break;
			case "function" : 
				return mapping[param].call(null,param);
			break;
			default :
				return null;
		}
	}
	
	function mainWindowScreen() {
		var ret = {};		
		ret['fullsize'] = (config["browserViewMode"] == 0) ? false : true;
		ret['width'] = config["mainBrowserWindowWidth"];
		ret['height'] = config["mainBrowserWindowHeight"];
		ret['position'] = pos[config["mainBrowserWindowPositioning"]];
		return ret;
	}
	
	function popupScreen() {
		var ret = {};				
		ret['fullsize'] = false;
		ret['width'] = config["newBrowserWindowByLinkWidth"];
		ret['height'] = config["newBrowserWindowByLinkHeight"];
		ret['position'] = pos[config["newBrowserWindowByLinkPositioning"]];
		return ret;
	}
	
	function titleBarEnabled() {
		var ret = (config["browserViewMode"] == 0) ? true : false;
		return ret;
	}
	
    	function urlFilterRegex() {
        	var ret = (config["urlFilterRegex"] == 1) ? true : false;
        	return ret;
    	}

    	function urlFilterTrustedContent() {
        	var ret = (config["urlFilterTrustedContent"] == 0) ? true : false;
        	return ret;
    	}
    	
    	function browserScreenKeyboard() {
	        var ret = (config["browserScreenKeyboard"] == 1) ? true : false;
	        return ret;
    	}
	
	function browserExamKey() {
		// add some logic
		return config["browserExamKey"];
	}
	
	function proxyType() {
		// see http://kb.mozillazine.org/Firefox_:_FAQs_:_About:config_Entries
		// if no proxy object, don't map anything
		if (!config["proxies"]) {
			return null;
		}
		// autodetect proxy
		if (config["proxies"]["AutoDiscoveryEnabled"]) {
			return 4;
		}
		// auto config url
		if (config["proxies"]["AutoConfigurationEnabled"]) {
			return 2;
		}
		// http(s) proxy
		if (config["proxies"]["HTTPEnable"] || config["proxies"]["HTTPSEnable"]) {
			return 1;
		}
		return 0;
	}
	
	function proxyAutoConfig() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["AutoConfigurationURL"]) {
			return null;
		}
		return config["proxies"]["AutoConfigurationURL"];
	}
	
	function proxyHttp() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["HTTPProxy"]) {
			return null;
		}
		return config["proxies"]["HTTPProxy"];
	}
	
	function proxyHttpPort() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["HTTPPort"]) {
			return null;
		}
		return config["proxies"]["HTTPPort"];
	}
	
	function paramHandler(fn) {
		return eval(fn).call(null); 
	}
	
	
	return {
		toString 			: 	toString,
		init				:	init,
		hasParamMapping			:	hasParamMapping,
		getParam			:	getParam
	};
}());

