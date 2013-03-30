var util = require('util');
var utils = function utils() {
	if(utils.caller != utils.getInstance){
		throw new Error("This object cannot be instanciated");
	}
	
	this.inspect = function(obj) {
		return 	console.log(util.inspect(obj));
	}
	
	this.out = function(obj) {
		console.log(obj);
	}
}

utils.instance = null;

/**
 * Singleton getInstance definition
 * @return singleton class
 */
utils.getInstance = function() {
	if(this.instance === null){
		this.instance = new utils();
	}
	return this.instance;
}

module.exports = utils.getInstance();
