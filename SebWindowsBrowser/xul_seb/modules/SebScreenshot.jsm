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

this.EXPORTED_SYMBOLS = ["SebScreenshot"];

/* Modules */
const 	{ classes: Cc, interfaces: Ci, results: Cr, utils: Cu } = Components,
	{ scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services;
Cu.import("resource://gre/modules/XPCOMUtils.jsm");
//Cu.importGlobalProperties(["Blob"]);
/* Services */
/* Services */

/* SebGlobals */

/* SebModules */
XPCOMUtils.defineLazyModuleGetter(this,"sl","resource://modules/SebLog.jsm","SebLog");
XPCOMUtils.defineLazyModuleGetter(this,"su","resource://modules/SebUtils.jsm","SebUtils");
XPCOMUtils.defineLazyModuleGetter(this,"ss","resource://modules/SebServer.jsm","SebServer");

/* ModuleGlobals */
const	BASE64	=	0,
	BINARY	=	1,
	BLOB	=	2,
	FILE	=	3,
	JPEG	=	"image/jpeg",
	PNG	=	"image/png";
	
let 	base = null,
	seb = null,
	audio = null,
	blobs = {};
	
this.SebScreenshot = {
	init : function(obj) {
		base = this;
		seb = obj;
		sl.out("SebScreenshot initialized: " + seb);
	},
	
	initAudio : function(el) {
		audio = el;
	},
	
	createScreenshotController : function (win) {
		sl.debug("createScreenshotController");
		win.seb_ScreenShot = function(w,file) {
			let mimetype = su.getConfig("sebScreenshotImageType","string",JPEG);
			mimetype = (mimetype == PNG || mimetype == JPEG) ? mimetype : PNG;
			// we want to use the default mimetype and ratio in config.json
			let opts = {
				"format" 	: 	BASE64,
				"mimetype"	:	mimetype,
				"ratio"		:	0.5
			};
			//sl.debug("sc opts: " + JSON.stringify(opts.mimetype));
			// add mimetype to file object
			file["mimetype"] = opts.mimetype;
			// assign file extension to the filename
			switch (file.mimetype) {
				case JPEG :
					file.filename += '.jpg';
					break;
				case PNG :
					file.filename += '.png';
					break;
			}
			
			let data = base.screenShot(w,opts);
			let obj = {"handler":"screenshot","opts":{"file": file, "data":data}};
			ss.send(JSON.stringify(obj));
			
			//base.screenShot(w, blobHandler,opts);
			/*
			function blobHandler(blob) {
				sl.debug("blobHandler");
				blobs[file.filename] = blob;
				// send metadata and wait for server request binary object
				let obj = {"handler":"filemetadata","opts":{file}};
				ss.send(JSON.stringify(obj));					
			}
			*/
		}
	},
	
	sendScreenshotData : function(file) {
		ss.send(blobs[file.filename]);
	},
	
	screenShot : function (win,opts,blobhandler) {
		sl.out("screenShot");
		if(!win) {
			sl.err("no window for screenshot");
			return null;
		}
		if (audio === null) {
			audio = seb.mainWin.document.getElementById("seb.snapshot");
			sl.debug("audio: " + typeof audio);
		}
		let format = BASE64;
		let mimetype = su.getConfig("sebScreenshotImageType","string",JPEG);
		let ratio = 0.5; // does not take any effect
		let sound = su.getConfig("sebScreenshotSound","boolean",false);
		let filename = "";
		if (opts) {
			try {
				format = (opts.format) ? parseInt(opts.format) : format;
				mimetype = (opts.mimetype) ? (opts.mimetype) : mimetype;
				ratio = (opts.ratio) ? parseFloat(opts.ratio) : ratio;
				sound = (opts.sound) ? opts.sound : sound;
				filename = (opts.filename) ? opts.filename : filename; // only for format = FILE
			}
			catch(e) {
				sl.err("wrong options for screenshot module");
				return false;
			}
		}
		
		//Get a reference to the document in the window
		var doc = win.document;
		//Create a canvas element using the document object. This element is never added to
		//the page.
		var cvs = doc.createElement("canvas");
		//Create some vars to hold the size of our screen shot
		var w = win.innerWidth + win.scrollMaxX;
		var h = win.innerHeight + win.scrollMaxY;
			
		if (w > 10000) w = 10000;
		if (h > 10000) h = 10000;

		//Setup the size of our canvas element
		cvs.style.width = w +"px";
		cvs.style.height = h +"px";
		cvs.width = w;
		cvs.height = h;
			
		//Get the nsIDOMCanvasRenderingContext2D interface from our canvas element
		var ctx = cvs.getContext("2d");
					
		//Get the drawing context setup
		ctx.clearRect(0, 0, w, h);
		ctx.save();
		//Call our 'drawWindow' method. This is the method we can't use from C#
		ctx.drawWindow(win, 0, 0, w, h, "rgba(0,0,0,0)");
		ctx.restore();
		
		switch (format) {
			case 	BASE64 : 
				//Return the screen shot as a base 64 encoded string.
				let ret = cvs.toDataURL(mimetype,ratio).replace(/^data:image\/.*?;base64,/,"");
				if (sound && audio) {
					audio.play();
				}
				return ret;
				break;
			case	BINARY :
				//Return the screen shot as new Uint8Array.
				let imagedata = ctx.getImageData(0, 0, w,h);
				let canvaspixelarray = imagedata.data;
				let canvaspixellen = canvaspixelarray.length;
				let bytearray = new Uint8Array(canvaspixellen);
				for (var i=0;i<canvaspixellen;++i) {
					bytearray[i] = canvaspixelarray[i];
				}
				return bytearray;
				break;
			case 	BLOB :
				//Return the screen shot as blob
				//cvs.toBlob(blobHandler,mimetype,{'interlacing' : true, 'compressionQuality' : 0.1});
				cvs.toBlob(blobHandler, mimetype); // ratio does not take any effect
				if (sound && audio) {
					audio.play();
				}
				break;
			case 	FILE :
				//Return the screen shot as file object
				return cvs.mozGetAsFile(filename,mimetype);
				break;
			default		 :
				// do nothing
		}
	}
}
