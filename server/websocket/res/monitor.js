(function( $ ) {
	var prot;
	var sock_url; 
	var client;
	var table;
	
	var stream_handler = 	{
					"init_data_table" 	: 	init_data_table,	
					"add_client"		:	add_client,
					"remove_client"		:	remove_client,
					"stream_data_screenshot":	stream_data_screenshot
				}
			
	function init(config) {
		prot = (window.location.protocol === "https:") ? "wss:" : "ws:"; // for ssl
		sock_url = prot + "//" + window.location.host + "/websocket"; 
		client = new BinaryClient(sock_url);
		client.on('open', on_client_open);
		client.on('close', on_client_close);
		client.on('error', on_client_error);
		client.on('stream', on_client_stream); 
	} 
	
	function on_client_open() {
		out("on_client_open");	
	}
	
	function on_client_stream(stream, opts) {
		out("on_client_stream");
		//var opts = JSON.parse(meta); // transfering json objects does not work, have to parse strings??
		var handler = stream_handler[opts.handler];
		if (typeof handler === 'function') {
			handler.apply(undefined, [stream, opts]);
		}
		else {
			err("no such stream_handler: " + opts.handler);
		}
	}
	
	function on_client_close() {
		out("websocket closed.");
	}
	
	function on_client_error(e) {
		err(e);
	}
	
	// handler
	
	function print(txt) {
		$('#out').html(txt);
	}
	
	function init_data_table(stream, opts) {
		stream.on('data', function(data) {
			var d = JSON.parse( data );
			table = $('#clientsData').dataTable( d );
		});
	}
	
	function add_client(stream, opts) {
		stream.on('data', function(data) {
			var d = JSON.parse( data );
			out("add_client with id: " + d[0]);
			$('#clientsData').dataTable().fnAddData(d);
			//out("add_client" + d);
			/*
			var d = JSON.parse( data );
			$('#clientsData').dataTable( d );
			*/ 
		});
	}
	
	function remove_client(stream, opts) {
		stream.on('data', function(data) {
			var d = JSON.parse( data );
			out("remove_client with id: " + d[0]);
			var row_index = table.fnFindCellRowIndexes(d[0],0);
			$('#clientsData').dataTable().fnDeleteRow(row_index[0]);
		});
	}
	
	function stream_data_screenshot(stream, opts) {
		stream.on('data', function(data) {
			var d = JSON.parse( data );
			var row = table.fnFindCellRowNodes(d.client_id,0)[0];
			if (d.event == "data") {
				try {
					$(row).find(".streamNoSignal").toggleClass('streamNoSignal streamDataSignal');
				}
				catch(e) {
					out(e);
				}
			}
			else {
				try {
					$(row).find(".streamDataSignal").toggleClass('streamDataSignal streamNoSignal');
				}
				catch(e) {
					out(e);
				} 
			}
			//out(d.event);
			//out(d.client_id);
			//out("remove_client with id: " + d[0]);
			//$('#clientsData').dataTable().fnDeleteRow(d[0]);
		});
	}
	
	function send_message(cid, msg) {
		var opts = {
			handler 	: "send_message",
			client_id	: cid,
			msg		: msg
		}
		var stream = client.send("", JSON.stringify(opts));
	}
	
	function force_shutdown(cid) {
		var opts = {
			handler 	: "force_shutdown",
			client_id	: cid	
		}
		var stream = client.send("", JSON.stringify(opts));
	}
	
	// handler end
	
	function out(m) {
		console.log(m);
	}

	function err(e) {
		console.error(e);
	}
	
	function unload() {
		try {
			$.each(client.streams, function (k,v) { v._socket.close() });
		}
		catch (e) {
			err(e);
		}
	}
	
	var methods = {
		init 		: init,
		print		: print,
		send_message 	: send_message,
		force_shutdown 	: force_shutdown,
		out  		: out,
		err		: err,
		unload		: unload
	}
	
	$.fn.sebadmin = function ( method ) {
		// Method calling logic
		if ( methods[method] ) {
			return methods[ method ].apply( this, Array.prototype.slice.call( arguments, 1 ));
		} 
		else if ( typeof method === 'object' || ! method ) {
			return methods.init.apply( this, arguments );
		} 
		else {
			console.error( 'Method ' +  method + ' does not exist on jQuery.sebadmin' );
		} 
	};
})(jQuery);

$(document).ready( function() {
	$('#clients').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="clientsData"></table>' ); 
	$(this).sebadmin({debug : true}); 
});
