const	Cc = Components.classes,
	Ci = Components.interfaces,
	Cu = Components.utils;

const 	{ scriptloader } = Cu.import("resource://gre/modules/Services.jsm").Services;

Cu.import("resource://gre/modules/XPCOMUtils.jsm");
Cu.import("resource://gre/modules/NetUtil.jsm");
scriptloader.loadSubScript("resource://globals/const.js");

// sebProtocol
const kSCHEME = "sebs";
const kPROTOCOL_NAME = "SEBS Protocol";
const kPROTOCOL_CONTRACTID = "@mozilla.org/network/protocol;1?name=" + kSCHEME;
const kPROTOCOL_CID = Components.ID('{789409b9-2e3c-3682-a6d1-71ca80a76453}');

// Mozilla
const kSIMPLEURI_CONTRACTID = "@mozilla.org/network/simple-uri;1";
const kIOSERVICE_CONTRACTID = "@mozilla.org/network/io-service;1";
const nsISupports = Ci.nsISupports;
const nsIIOService = Ci.nsIIOService;
const nsIProtocolHandler = Ci.nsIProtocolHandler;
const nsIURI = Ci.nsIURI;

var sebsProtocol = function () {};

sebsProtocol.prototype = {
	classDescription: kPROTOCOL_NAME,
	classID: kPROTOCOL_CID,
	contractID: kPROTOCOL_CONTRACTID,
	QueryInterface: XPCOMUtils.generateQI([Ci.nsIProtocolHandler]),
	scheme: kSCHEME,
	defaultPort: -1,
	protocolFlags: 	nsIProtocolHandler.URI_NORELATIVE |
			nsIProtocolHandler.URI_NOAUTH | 
			nsIProtocolHandler.URI_LOADABLE_BY_ANYONE,
	allowPort: function(port, scheme) {
		return false;
	},

	newURI: function(spec, charset, baseURI) {
		// dump("new URI: " + spec + "\n");
		var uri = Cc[kSIMPLEURI_CONTRACTID].createInstance(nsIURI);
		uri.spec = spec;
		return uri;
	},

	newChannel: function(aURI) {
		//dump("uri spec:" + aURI.spec + "\n");
		var ch = null;
		try {
			let newUrl = aURI.spec.replace(/^sebs:/i,'https:');
			let uri = NetUtil.newURI(newUrl);
			ch = NetUtil.newChannel(uri);
			ch.QueryInterface(Ci.nsIHttpChannel);
			ch.setRequestHeader(SEB_FILE_HEADER, "1", false);
			ch.setRequestHeader("Content-Type",SEB_MIME_TYPE,false);
		}
		catch (e) {
			dump(e + "\n");
		}
		return ch;
	}
};
var NSGetFactory = XPCOMUtils.generateNSGetFactory([sebsProtocol]);
