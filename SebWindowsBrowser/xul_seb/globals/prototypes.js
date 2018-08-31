

String.prototype.normalize = function () {
	return this.replace(/\W/g, "");
}
String.prototype.trim = function () {
	return this.replace(/^\s+|\s+$/g,"");
}
String.prototype.isEmpty = function () {
	return (!this || this == "[object Null]" || this == "" || this == "undefined");
}
Array.prototype.contains = function(obj) {
	var i = this.length;
	while (i--) {
		if (this[i] === obj) {
			return true;
		}
	}	
	return false;
}

