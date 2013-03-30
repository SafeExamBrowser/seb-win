var 	fs 	= require('fs-extra'),
	util 	= require('util'),
	utils	= require('./utils.js'),
	https 	= require('https'),
	bins	= require('binaryjs').BinaryServer,
	conf	= require('./conf.js'),
	out	= utils.out;

const port = 8442;

var stream_handler = 	{
				send_message	: send_message,
				force_shutdown	: force_shutdown
			}

var columns =	[
			{ sTitle 	: "client id" },
			{ sTitle  	: "remote address" },
			{ sTitle 	: "screenshot data", "sClass": "streamNoSignal" },
			{ sTitle 	: "send message" },
			{ sTitle 	: "force shutdown" }
		]	
var server = https.createServer(conf.getServerOptions(), conf.getApp());
var bss = bins({ server: server });
server.listen(port);
bss.on('connection', con);

util.puts('HTTPS and BinaryJS server for monitoring started on port ' + port);

function con(client) {
	client.on('error', on_client_close);
	client.on('close', on_client_close);
	client.on('stream', on_client_stream);
	var cn = client._socket.upgradeReq.connection.getPeerCertificate().subject.CN;
	if (cn != conf.admCN ) { // only clients with valid admin certificates are allowed
		out("invalid user CN: " + cn);
		client.close();
	}
	else {
		out("monitor admin connected");
		init_data_table(client);
	}
}

function on_client_error(e) {
	out("monitor admin error: " + e);
}

function on_client_close() {
	out("monitor admin disconnected");
}

function on_client_stream(stream, meta) {
	var opts = JSON.parse(meta); // transfering json objects does not work, have to parse strings??
	var handler = stream_handler[opts.handler];
	if (typeof handler === 'function') {
		handler.apply(undefined, [stream, opts]);
	}
}
	
// listener
function client_connection(client) {
	add_client(client);
}
// listener end

// monitor stream handler
function send_message(stream, opts) {
	out("send_message to: " + opts.client_id);
	var c = monitor.bss.clients[opts.client_id];
	c.send("", JSON.stringify(opts));
}

function force_shutdown(stream, opts) {
	out("force_shutdown to: " + opts.client_id);
	var c = monitor.bss.clients[opts.client_id];
	//inspect(c);
	c.send("", JSON.stringify(opts));
}
// monitor stream handler end

// data table manipulation
function get_data() {
	var data = [];
	for (var k in monitor.bss.clients) {
		var c = monitor.bss.clients[k];
		data.push(get_client_data(c));
		//inspect(c._socket);
		/*
		var row = [];
		row.push(c.id);
		row.push(c._socket.upgradeReq.connection.remoteAddress);
		row.push("");
		//row.push("send");
		//row.push("<div style='font-weight: bold'>shutdown</div>");
		row.push("send"); 
		//onclick='send_message()' value='send message' />"); // ToDo: fnRender 
		row.push("shutdown");
		//row.push('<input type="button" onclick="shutdown(' + c.id + ')" value="shutdown">');
		//row.push(Object.keys(c.streams).length);
		//data.push(row);
		*/ 
	}
	return data;
}

function get_client_data(client) {
	var row = [];
	row.push(client.id);
	row.push(client._socket.upgradeReq.connection.remoteAddress);
	row.push("");
	row.push('<input type="button" onclick="$.fn.sebadmin(\'send_message\',' + client.id + ', \'Admin Message!\')" value="send message">');
	row.push('<input type="button" onclick="$.fn.sebadmin(\'force_shutdown\',' + client.id + ')" value="shutdown">');
	//row.push(Object.keys(client.streams).length);
	return row;
}

function init_data_table(client) { // admin client! 
	var data = get_data();
	var d = { aaData : data, aoColumns : columns };
	var meta = {
		handler : "init_data_table"
	}
	client.send(JSON.stringify(d), meta);
}

function add_client(client) { // seb client!
	var data = get_client_data(client);
	var meta = {
		handler : "add_client"
	}
	broadcast(data, meta);
}

function remove_client(client) { // seb client
	var data = get_client_data(client);
	var meta = {
		handler : "remove_client"
	}
	broadcast(data, meta);
}

function update_stream_info(client, stream, data) {
	/*
	var data = get_client_data(client);
	var meta = {
		handler : "update_stream_info"
	}
	stream.on('data', function(data) {
				//out("update_stream_info");
			});
	// ToDo
	*/
}

function screenshot_data(client_id, event) {
	var data = {client_id : client_id, event : event};
	var meta = {
		handler : "stream_data_screenshot"
	}
	broadcast(data, meta);
}
 
function broadcast(data, meta) { // to all connected admin clients
	for (var k in bss.clients) {
		var c = bss.clients[k];
		c.send(JSON.stringify(data), meta);
	}
}
// data table manipulation end

// monitor
var monitor = function () {
	if(monitor.caller != monitor.getInstance) {
		throw new Error("This object cannot be instanciated");
	}
	this.bss = null;
	this.init = function(binaryserver) {
		monitor.bss = binaryserver;
		//init_data_table();
		//utils.inspect(monitor.bss);
	}
	this.con = client_connection;
	this.on_client_error = function(e) { out(e) }; // ToDo
	this.on_client_close = function() { remove_client( this ) }; // in that context "this" is a client object (see: "client.on('close', monitor.on_client_close)" in server.js); 
	this.on_client_stream = function(stream, meta) { update_stream_info( this, stream, meta ) };
	this.on_stream_data_screenshot = function (client_id) { screenshot_data( client_id, "data" ) }; // "this" is stream object 
	this.on_stream_end_screenshot = function (client_id) { screenshot_data( client_id, "end" ) }; // "this" is stream object 
}
monitor.instance = null;

monitor.getInstance = function(){
	if(this.instance === null){
		this.instance = new monitor();
	}
	return this.instance;
}

module.exports = monitor.getInstance();
