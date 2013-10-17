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
var EXPORTED_SYMBOLS = ["linuxctrl"];
Components.utils.import("resource://modules/xullib.jsm");

var linuxctrl = (function() {
	// XPCOM Services, Interfaces and Objects
	const	x		=	xullib;
	var 	config 		= 	null,
		mapping		= 	{
						"seb.url"			: 	"startURL",
						"seb.shutdown.url"		: 	"quitURL",
						"seb.request.key"		:	function() { paramHandler('browserExamKey') },
						"seb.navigation.enabled"	:	"allowBrowsingBackForward",
						"seb.shutdown.enabled"		:	function() { paramHandler('allowQuit') }
							
					};
	
	function toString () {
			return "linuxctrl";
	}
	
	function init(conf,cb) {
		x.debug("init linuxctrl");
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
				//x.debug(config[mapping[param]]);
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
	
	function browserExamKey() {
		// add some logic
		return config["browserExamKey"];
	}
	
	function allowQuit() {
		// add some logic
		return config["allowQuit"];
	}
	
	function paramHandler(fn) {
		if (config && config[fn] != null) {
			return eval(fn).call(null);
		}
		else {
			return null;
		}
	}
	
	return {
		toString 			: 	toString,
		init				:	init,
		hasParamMapping			:	hasParamMapping,
		getParam			:	getParam			
	};
}());

