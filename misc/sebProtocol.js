Components.utils.import("resource://gre/modules/XPCOMUtils.jsm");
const Cc = Components.classes;
const Ci = Components.interfaces;

// sebProtocol
const kSCHEME = "seb";
const kPROTOCOL_NAME = "SEB Protocol";
const kPROTOCOL_CONTRACTID = "@mozilla.org/network/protocol;1?name=" + kSCHEME;
const kPROTOCOL_CID = Components.ID('{789409b9-2e3c-4682-a6d1-71ca80a76456}');

// Mozilla
const kSIMPLEURI_CONTRACTID = "@mozilla.org/network/simple-uri;1";
const kIOSERVICE_CONTRACTID = "@mozilla.org/network/io-service;1";
const nsISupports = Ci.nsISupports;
const nsIIOService = Ci.nsIIOService;
const nsIProtocolHandler = Ci.nsIProtocolHandler;
const nsIURI = Ci.nsIURI;

var sebProtocol = function () {};

sebProtocol.prototype = {
	classDescription: kPROTOCOL_NAME,
	classID: kPROTOCOL_CID,
	contractID: kPROTOCOL_CONTRACTID,
	QueryInterface: XPCOMUtils.generateQI([Ci.nsIProtocolHandler]),
	scheme: kSCHEME,
	defaultPort: -1,
	protocolFlags: 	nsIProtocolHandler.URI_NORELATIVE |
					nsIProtocolHandler.URI_NOAUTH,
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
		//dump("uri spec:" + aURI.spech + "\n");
		var ch = null;
		try {
			var ios = Cc[kIOSERVICE_CONTRACTID].getService(nsIIOService);
			var ch = ios.newChannel("http://www.google.com", null, null);
			// dump(ch + "\n");
		}
		catch (e) {
			dump(e + "\n");
		}
		return ch;
	},
	
	// CHANGEME: change the help info as appropriate, but
	// follow the guidelines in nsICommandLineHandler.idl
	// specifically, flag descriptions should start at
	// character 24, and lines should be wrapped at
	// 72 characters with embedded newlines,
	// and finally, the string should end with a newline
	helpInfo : "	-debug              set debug true|false\n" +
             "		-config	<uri>       additional json object url,\n" +
             "		wrapping this description\n"
};
var NSGetFactory = XPCOMUtils.generateNSGetFactory([sebProtocol]);
