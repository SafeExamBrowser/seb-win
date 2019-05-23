const 	DEBUG_LEVEL = 1,
	INFO_LEVEL = 2,
	SSL_SEC_NONE = 0, 		// allow all http / https and mixed contents
	SSL_SEC_BLOCK_MIXED_ACTIVE = 1,	// default: block mixed active contents (scripts...), display contents are allowed (img, css...) = firefox default behaviour
	SSL_SEC_BLOCK_MIXED_ALL = 2,	// block all mixed contents
	SSL_SEC_FORCE_HTTPS = 3,	// try redirecting http to https. Beware! this is not a common browser behaviour! The web app should be fixed instead of rewriting the request on the client side!
	SSL_SEC_BLOCK_HTTP = 4,		// block all http requests
	SEB_FILE_HEADER = 'X-Seb-File', // Seb File Request-Header
	SEB_MIME_TYPE = 'application/seb',
	LITTLE_ENDIAN = 0,
	BIG_ENDIAN = 1,
	SEB_URL = "chrome://seb/content/seb.xul",
	MESSAGE_SOCKET_URL="chrome://seb/content/message_socket.html",
	LOCK_URL = "chrome://seb/content/lockscreen.xul",
    MODE_RECONNECT = 0,
    MODE_LOCKED = 1,
    MODE_USERSWITCH = 2,
	SEB_FEATURES = "chrome,dialog=no,resizable=yes,scrollbars=yes",
	HIDDEN_URL= "chrome://seb/content/hidden.xul",
	HIDDEN_FEATURES = "chrome,modal=no,dialog,resizable=no,width=1,height=1",
	RECONF_NO = 0,
	RECONF_START = 1,
	RECONF_SUCCESS = 2,
	RECONF_ABORTED = 3,
    RECONF_PROCESSING = 4,
	RECONFIG_URL = "chrome://seb/content/reconf.xul",
	RECONFIG_TYPE = "reconf",
	RECONFIG_FEATURES = "chrome,dialog,modal,resizable=yes,width=800,height=600,scrollbars=yes",
	BROWSER_UA_DESKTOP_PREF = "browserUserAgentWinDesktopMode",
	BROWSER_UA_TOUCH_PREF = "browserUserAgentWinTouchMode",
	BROWSER_UA_DESKTOP_CUSTOM_PREF = "browserUserAgentWinDesktopModeCustom",
	BROWSER_UA_TOUCH_IPAD_PREF = "browserUserAgentWinTouchModeIPad",
	BROWSER_UA_TOUCH_CUSTOM_PREF = "browserUserAgentWinTouchModeCustom",
	BROWSER_UA_DESKTOP_DEFAULT = 0,
	BROWSER_UA_DESKTOP_CUSTOM = 1,
	BROWSER_UA_TOUCH_DEFAULT = 0, 
	BROWSER_UA_TOUCH_IPAD = 1,
	BROWSER_UA_TOUCH_CUSTOM = 2,
	PDF_VIEWER_TITLE = "SEB PDF Viewer",
	ERROR_PAGE_TITLE = "SEB Error Page",
	STATUS_PDF_REDIRECT = {status:1, message:"STATUS_PDF_REDIRECT"},
	STATUS_QUIT_URL_STOP = {status:2, message:"STATUS_QUIT_URL_STOP"},
	STATUS_QUIT_URL_WRONG_REFERRER = {status:3, message:"STATUS_QUIT_URL_WRONG_REFERRER"},
	STATUS_DOCUMENT_STOP_ERROR = {status:4, message:"STATUS_DOCUMENT_STOP_ERROR"},
	STATUS_LOAD_AR = {status:5, message:"STATUS_LOAD_AR"},
	STATUS_INVALID_URL = {status:6, message:"STATUS_INVALID_URL"},
	STATUS_BLOCK_HTTP = {status:7, message:"STATUS_BLOCK_HTTP"},
    STATUS_CLEAR_CLIPBOARD_URL_STOP = {status:8, message:"STATUS_CLEAR_CLIPBOARD_URL_STOP"};
	
	

	

