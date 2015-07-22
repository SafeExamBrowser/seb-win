
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

/* ***** GLOBAL screen SINGLETON *****

* *************************************/ 

/* 	for javascript module import
	see: https://developer.mozilla.org/en/Components.utils.import 
*/
var EXPORTED_SYMBOLS = ["screen"];
Components.utils.import("resource://modules/xullib.jsm");

var screen = (function() {
	// XPCOM Services, Interfaces and Objects
	const	x	=	xullib,
		Cc	=	x.Cc,
		Ci	=	x.Ci,
		Cu	=	x.Cu,
		Cr	=	x.Cr,
		BASE64	=	0,
		BINARY	=	1,
		BLOB	=	2,
		FILE	=	3,
		JPEG	=	"image/jpeg",
		PNG	=	"image/png";
		
	var	audio	=	null;
	
	function toString() {
		return "screenshot";
	}
	
	function init(el) {
		audio = el;
	}
	//This method will do the work of grabbing the screen shot for us
	//The parameter 'win' is a reference to the nsIDOMWindow interface for the browser
	//control we want to take a screen shot of.  
	function screenShot(win,blobHandler,opts) {
		if(!win) {
			x.err("no window for screenshot");
			return null;
		}
		let format = BLOB;
		let mimetype = x.getParam("sc.image.mimetype");
		let ratio = 0.5; // does not take any effect
		let sound = x.getParam("sc.sound");
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
				x.err("wrong options for screenshot module");
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
				return cvs.toDataURL(mimetype,ratio);
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
  
	/* export public functions */
	return {
		toString 		: 	toString,
		init			:	init,
		screenShot		:	screenShot,
		BASE64			:	BASE64,
		BINARY			:	BINARY,
		BLOB			:	BLOB,
		FILE			:	FILE,
		JPEG			:	JPEG,
		PNG				:	PNG			
	};	
}());
