var WebSocketServer = require('ws').Server
, wss = new WebSocketServer({port: 8767});
wss.on('connection', function(ws) {
ws.on('message', function(message) {
console.log('received: %s', message);
});
ws.send('something');
});
