// https://gist.github.com/wakirin/ad10109053f268dfac0f3f3dcf438010

(function () {
	WebSocket.prototype._send = WebSocket.prototype.send;
	WebSocket.prototype.send = function (data) {
		this._send(data);
        this.addEventListener('message', function (msg) {
            console.log('>> ' + msg.data);
        }, false);
        this.send = function (data) {
            this._send(data);
            console.log("<< " + data);
        };
		console.log("<< " + data);
	}
})()