var 	fs 	= require('fs-extra'),
	express = require('express'),
	utils	= require('./utils.js');

const 	CA_CN 	= "eqsoft CA",
	USR_CN	= "eqsoft.user",
	ADM_CN	= "eqsoft.admin";


var conf = function conf() {
	if(conf.caller != conf.getInstance){
		throw new Error("This object cannot be instanciated");
	}
	
	this.caCN = CA_CN;
	this.usrCN = USR_CN;
	this.admCN = ADM_CN;
	
	this.getServerOptions = function() {
		var options = 	{
				key:    fs.readFileSync('ssl/server.key'),
				cert:   fs.readFileSync('ssl/server.crt'),
				ca:     [ fs.readFileSync('ssl/ca.crt') ],
				requestCert:        true, 	// client cert is required
				rejectUnauthorized: true 	// reject invalid client certs
				}
		return options;
	}
	
	this.getApp = function() {
		var app = express();
		app.use(function(req,res,next) { // Check Auth: only SSL connection with valid client certs are allowed, otherwise ANONYMOUS (demo certs see: user.p12 and admin.p12)
			// this should not be reached in productive ssl environments (rejectUnauthorized = true)
			if (!req.connection.getPeerCertificate().subject) {
				res.writeHead(403, {'Content-Type': 'text/plain'});
				res.end('You need a valid client certificate!');
			}
			else {		
				//var cn = req.connection.getPeerCertificate().subject.CN;
				var issuer = req.connection.getPeerCertificate().issuer.CN;
				if (issuer != CA_CN) {
					res.writeHead(403, {'Content-Type': 'text/plain'});
					res.end('You need a valid client certificate!');
					return;
				}
				next();
			}
		});

		app.use('/',express.static(__dirname));
		app.use('/demo',express.static('demo'));
		app.use('/websocket',express.static('websocket'));
		app.use('/websocket/data',express.directory('websocket/data'));
		return app;
	}
}

conf.instance = null;

/**
 * Singleton getInstance definition
 * @return singleton class
 */
conf.getInstance = function(){
	if(this.instance === null){
		this.instance = new conf();
	}
	return this.instance;
}

module.exports = conf.getInstance();
