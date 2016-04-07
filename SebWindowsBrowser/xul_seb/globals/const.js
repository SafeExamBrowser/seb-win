const 	SSL_SEC_NONE = 0, 		// allow all http / https and mixed contents
	SSL_SEC_BLOCK_MIXED_ACTIVE = 1,	// default: block mixed active contents (scripts...), display contents are allowed (img, css...) = firefox default behaviour
	SSL_SEC_BLOCK_MIXED_ALL = 2,	// block all mixed contents
	SSL_SEC_FORCE_HTTPS = 3,	// try redirecting http to https. Beware! this is not a common browser behaviour! The web app should be fixed instead of rewriting the request on the client side!
	SSL_SEC_BLOCK_HTTP = 4;		// block all http requests
