using Fleck;
using Serilog;

namespace AutoKkutuLib.Selenium;
internal class EventListenerWebSocket
{
	public event EventHandler<WebSocketMessageEventArgs>? OnReceive;

	public EventListenerWebSocket(string address)
	{
		Log.Information("Event listener WebSocket running on {addr}", address);
		var server = new WebSocketServer(address);
		server.ListenerSocket.NoDelay = true;
		server.RestartAfterListenError = true;
		_ = Task.Run(() =>
		{
			try
			{
				server.Start(socket =>
				{
					socket.OnMessage = HandleMessage;
					socket.OnOpen = () => Log.Information("WebSocket client connected!");
					socket.OnClose = () => Log.Warning("WebSocket event listener WebSocket closed.");
					socket.OnError = ex => Log.Error(ex, "WebSocket event listener WebSocket error.");
				});
			}
			catch (Exception ex)
			{
				Log.Error(ex, "WebSocket server exception.");
			}
		});
	}

	public void HandleMessage(string msg)
	{
		if (string.IsNullOrEmpty(msg))
			return;

		Log.Warning("message recv: " + msg);
		if (msg[0] is 'r' or 's')
		{
			try
			{
				OnReceive?.Invoke(this, new WebSocketMessageEventArgs(msg[0] == 'r', msg[1..])); // Message prefix: Received = 'r', Send = 's'
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error handling WebSocket event listener WebSocket.");
			}
		}
	}
}
