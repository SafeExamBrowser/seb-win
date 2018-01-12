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

this.EXPORTED_SYMBOLS = ["SebLog"];

/* Modules */
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ appinfo, scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services,
	{ OS } = Cu.import("resource://gre/modules/osfile.jsm");
Cu.import("resource://gre/modules/XPCOMUtils.jsm");
/* Services */
let console = Cc["@mozilla.org/consoleservice;1"].getService(Ci.nsIConsoleService);

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"su","resource://modules/SebUtils.jsm","SebUtils");
XPCOMUtils.defineLazyModuleGetter(this,"sh","resource://modules/SebHost.jsm","SebHost");

/* SebGlobals */
scriptloader.loadSubScript("resource://globals/prototypes.js");
scriptloader.loadSubScript("resource://globals/const.js");

/* ModuleGlobals */
let 	seb = null,
	base = null,
	logpath = "",
 	os = "",
	lf = "\n",
	message = {};

this.SebLog = {
	level : '',
	init : function(obj) {
		seb = obj;
		base = this;
		os = appinfo.OS.toUpperCase();
		switch (os) { // line feed for dump messages
			case "WINNT" :
				lf = "\n\r";
				break;
			case "UNIX" :
			case "LINUX" :
				lf = "\n";
				break;
			case "DARWIN" :
				lf = "\n";
				break;
			default :
				lf = "\n";
		}
		this.out("SebLog initialized: " + seb);
	},
	out : function(msg) {
		let str = appinfo.name + " out : " + msg;
		console.logStringMessage(str);
		dump(str + lf);
		sh.sendLog(str);
	},
	debug : function(msg) {
		if (typeof seb === "object" && !seb.DEBUG) {
			return;
		}
		let str = appinfo.name + " debug : " + msg;
		console.logStringMessage(str);
		dump(str + lf);
		sh.sendLog(str);
	},
	
	info : function(msg) {
		if (typeof seb === "object" && !seb.INFO) {
			return;
		}
		let str = appinfo.name + " info : " + msg;
		console.logStringMessage(str);
		dump(str + lf);
		sh.sendLog(str);
	},
	
	err : function(msg) {
		let str = appinfo.name + " err : " + msg;
		Cu.reportError(str);
		console.logStringMessage(str);
		dump(str + lf);
		sh.sendLog(str);
	},
	
	dir : function(val) { // does not work: infinite loop
		if (typeof seb === "object" && !seb.DEBUG) {
			return;
		}
		if (typeof val == 'object') {
			for (var propertyName in val) {
				if (val.hasOwnProperty(propertyName)) {
					console.logStringMessage(base.level + propertyName + ':');
					base.level += '  ';
					base.dir(val[propertyName]);
				}
			}
		}
		else {
			console.logStringMessage(base.level + val);
			base.level = base.level.substring(0, base.level.length - 2);
		}
	}
}
