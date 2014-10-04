var WebSocketServer = require('ws').Server
, port = 8706
, wss = new WebSocketServer({port: 8706});
wss.on('connection', function(ws) {
ws.on('message', function(message) {
console.log('received: %s', message);
});
ws.send('winserver connected');
});
console.log('websocket server started on port '+ port);
