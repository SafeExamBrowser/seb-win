var 	express = require('express'),
	static = require('serve-static'),
	directory = require('serve-index');


var winconf = function winconf() {
	if(winconf.caller != winconf.getInstance){
		throw new Error("This object cannot be instanciated");
	}
	
	this.getApp = function() {
		var app = express();
		app.use('/win',static('win'));
		return app;
	}
}

winconf.instance = null;

/**
 * Singleton getInstance definition
 * @return singleton class
 */
winconf.getInstance = function(){
	if(this.instance === null){
		this.instance = new winconf();
	}
	return this.instance;
}

module.exports = winconf.getInstance();
