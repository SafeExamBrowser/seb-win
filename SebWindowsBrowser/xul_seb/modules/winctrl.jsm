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
						"seb.popupWindows.titlebar.enabled"	:	popupTitleBarEnabled,
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
						"seb.screenkeyboard.controller"		:	"browserScreenKeyboard",
						"seb.pattern.regex"		    	:	urlFilterRegex,
						"seb.trusted.content"			:	urlFilterTrustedContent,
						"seb.whitelist.pattern"			:	"whitelistURLFilter",
						"seb.blacklist.pattern"			:	"blacklistURLFilter",
						"network.proxy.type"			:	proxyType,
						"network.proxy.autoconfig_url"		:	proxyAutoConfig,
						"network.proxy.no_proxies_on"		:	proxyExceptionsList,
						"network.proxy.http" 			: 	proxyHttp,
						"network.proxy.http_port" 		: 	proxyHttpPort,
						"network.proxy.ssl" 			: 	proxyHttps,
						"network.proxy.ssl_port" 		: 	proxyHttpsPort,
						"network.proxy.ftp" 			: 	proxyFtp,
						"network.proxy.ftp_port" 		: 	proxyFtpPort,
						"network.proxy.socks" 			: 	proxySocks,
						"network.proxy.socks_port" 		: 	proxySocksPort,
						"seb.removeProfile"			:	"removeBrowserProfile",
						"seb.restart.url"			:	"restartExamURL",
						"seb.embedded.certs"			:	embeddedCerts,
            "seb.reload.warning"			:	"showReloadWarning",
            "browser.download.dir"			:	"downloadDirectoryWin",
            "browser.zoom.full"			:	browserZoomFull,
            "zoom.maxPercent"			:	zoomMaxPercent,
            "zoom.minPercent"			:	zoomMinPercent,
            //"browser.link.open_newwindow" : browserLinkOpenNewWindow,
            //"browser.link.open_newwindow.restriction" : browserLinkOpenNewWindowRestriction,
            "plugin.state.flash" : pluginEnableFlash,
            "plugin.state.java" : pluginEnableJava,
            "javascript.enabled" : "enableJavaScript",
            "dom.disable_open_during_load" : "blockPopUpWindows",
            "layout.spellcheckDefault" : spellcheckDefault,
	    "general.useragent.override" : "browserUserAgent"
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
		switch (typeof mapping[param]) {
			case "string" :
			case "number":
			case "boolean":
				if (config[mapping[param]] != null && config[mapping[param]] != undefined) {
					//x.debug("ctrl.getParam: " + param);
					return config[mapping[param]];
				}
				else {
					return null;
				}
			break;
			case "function" :
				//x.debug("ctrl.getParam function: " + param);
				return mapping[param].call(null,param);
			break;
			default :
				return null;
		}
	}

	function mainWindowScreen() {
		var ret = {};		 
		ret['fullsize'] = ((config["browserViewMode"] == 1) || (config["touchOptimized"] == 1)) ? true : false;
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

		if (config["touchOptimized"] == 1) {
			ret['fullsize'] = true;
		}
		return ret;
	}

	function titleBarEnabled() {
		var ret = ((config["browserViewMode"] == 1) || (config["touchOptimized"] == 1)) ? false : true;
		return ret;
	}
	
	function popupTitleBarEnabled() {
		var ret = (config["touchOptimized"] == 1) ? false : true;
		return ret;
	}
	
    	function browserScreenKeyboard() {
	        var ret = (config["browserScreenKeyboard"] == 1) ? true : false;
	        return ret;
    	}
	
  function browserZoomFull() {
    var ret = (config["zoomMode"] == 0) ? true : false;
    return ret;
  }

  function zoomMaxPercent() {
    var ret = (config["enableZoomPage"] == false && config["enableZoomText"] == false) ? 100 : 300;
    return ret;
  }

  function zoomMinPercent() {
    var ret = (config["enableZoomPage"] == false && config["enableZoomText"] == false) ? 100 : 30;
    return ret;
  }

  function spellcheckDefault() {
    var ret = (config["allowSpellCheck"] == true) ? 2 : 0;
    return ret;
  }

  function pluginEnableFlash() {
    var ret = (config["enablePlugIns"] == true) ? 2 : 0;
    return ret;
  }

  function pluginEnableJava() {
    var ret = (config["enableJava"] == true) ? 2 : 0;
    return ret;
  }

  function browserLinkOpenNewWindow() {
    if (config["newBrowserWindowByLinkPolicy"] == 1) {
      return 1;
    }
    return 2;
  }

  function browserLinkOpenNewWindowRestriction() {
    if (config["newBrowserWindowByScriptPolicy"] == 1) {
      return 0;
    }
    return 2;
  }

    	function urlFilterRegex() {
        	var ret = (config["urlFilterRegex"] == 1) ? true : false;
        	return ret;
    	}


    	function urlFilterTrustedContent() {
        	var ret = (config["urlFilterTrustedContent"] == 0) ? true : false;
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
		// system proxy
		if (config["proxies"]["proxySettingsPolicy"] == 0) {
			return 5;
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
		return null;
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

	function proxyHttps() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["HTTPSProxy"]) {
			return null;
		}
		return config["proxies"]["HTTPSProxy"];
	}

	function proxyHttpsPort() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["HTTPSPort"]) {
			return null;
		}
		return config["proxies"]["HTTPSPort"];
	}

	function proxyFtp() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["FTPProxy"]) {
			return null;
		}
		return config["proxies"]["FTPProxy"];
	}

	function proxyFtpPort() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["FTPPort"]) {
			return null;
		}
		return config["proxies"]["FTPPort"];
	}

	function proxySocks() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["SOCKSProxy"]) {
			return null;
		}
		return config["proxies"]["SOCKSProxy"];
	}

	function proxySocksPort() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["SOCKSPort"]) {
			return null;
		}
		return config["proxies"]["SOCKSPort"];
	}

	function proxyExceptionsList() {
		if (!config["proxies"]) {
			return null;
		}
		if (!config["proxies"]["ExceptionsList"]) {
			return null;
		}
		var exceptList = config["proxies"]["ExceptionsList"];
		//x.debug("proxyExceptionsList: " + typeof(exceptList));
		if (typeof(exceptList) != "object") {
			return null;
		}
		return config["proxies"]["ExceptionsList"].join(",") + ",localhost,127.0.0.1";
	}

	function embeddedCerts() {
		if (!config["embeddedCertificates"]) {
			return null;
		}
		var certlist = config["embeddedCertificates"];
		for (i=0;i<certlist.length;i++) {
			addCert(certlist[i]);
		}
	}

	function addCert(cert) {
		//https://developer.mozilla.org/en-US/docs/Cert_override.txt
		try {
			var overrideService = x.Cc["@mozilla.org/security/certoverride;1"].getService(x.Ci.nsICertOverrideService);
			var flags = overrideService.ERROR_UNTRUSTED | overrideService.ERROR_MISMATCH | overrideService.ERROR_TIME;
			var certdb = x.getCertDB();
			//var certcache = x.getCertCache();
			//var certlist = x.getCertList();
			var x509 = certdb.constructX509FromBase64(cert.certificateDataWin);
			//certlist.addCert(x509); // maybe needed for type 1 Identity Certs
			//certcache.cacheCertList(certlist);
			var host = cert.name;
			var port = 443;
			var fullhost = cert.name.split(":");
			if (fullhost.length==2) {
				host = fullhost[0];
				port = parseInt(fullhost[1]);
			}
			overrideService.rememberValidityOverride(host,port,x509,flags,true);
		}
		catch (e) {
			x.err(e);
		}
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
