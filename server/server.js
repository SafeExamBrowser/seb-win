var 	fs 	= require('fs-extra'),
	util 	= require('util'),
	utils	= require('./utils.js'),
	https 	= require('https'),
	bins	= require('binaryjs').BinaryServer,
	conf	= require('./conf.js'),
	monitor	= require('./monitor.js'),
	out	= utils.out;

const port = 8443;

var stream_handler = 	{
				"screenshot" 	: 	screenshot	 
			}
			
var server = https.createServer(conf.getServerOptions(), conf.getApp());
var bss = bins({ server: server });
monitor.init(bss);
server.listen(port);
bss.on('connection', con);
bss.on('connection', monitor.con); // connection listener for monitor

util.puts('HTTPS and BinaryJS server for SEB Server started on port ' + port);

function con(client) {
	client.on('error', on_client_error);
	client.on('error', monitor.on_client_error);
	client.on('close', on_client_close);
	client.on('close', monitor.on_client_close);
	client.on('stream', on_client_stream);
	client.on('stream', monitor.on_client_stream);
	var cn = client._socket.upgradeReq.connection.getPeerCertificate().subject.CN;
	if (cn != conf.usrCN ) { // only clients with valid user certificates are allowed
		out("invalid user CN: " + cn);
		client.close();
	}
	else {
		out("seb client connected");
	}
}

function on_client_stream(stream, meta) {
	out("on_client_stream");
	var opts = JSON.parse(meta); // transfering json objects does not work, have to parse strings??
	var handler = stream_handler[opts.handler];
	if (typeof handler === 'function') {
		handler.apply(this, [stream, opts]);
	}
}

function on_client_error(e) {
	out("seb client error: " + e);
}

function on_client_close() {
	out("seb client disconnected");
}
	
// handler
function screenshot(stream, opts) {
	var client = this;
	var p = opts.file.path.join("/");
	var filepath = __dirname + '/websocket/data/' + p;
	fs.mkdirs(filepath, function() {
		var file = fs.createWriteStream(filepath + '/' + opts.file.filename);
		var sumdata = 0;
		stream.pipe(file);
		//stream.on('data', monitor.on_stream_data_screenshot);
		stream.on('data', function(data) {
			monitor.on_stream_data_screenshot(client.id); // just notify, don't hook the data
			sumdata += data.length;
			// stream.write({rx: data.length / opts.size});
			if (parseInt(sumdata) == parseInt(opts.size)) {
				var txt = Date.now() + " : " + filepath + '/' + opts.file.filename;
				console.log(txt);
				stream.write(txt);
			}
		});
		stream.on('end', function() { monitor.on_stream_end_screenshot(client.id) } );
		stream.on('end',function() { 
			console.log("on_stream_end"); 
		});
	});
}

