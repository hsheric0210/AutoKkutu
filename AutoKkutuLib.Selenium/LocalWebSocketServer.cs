using NetCoreServer;
using OpenQA.Selenium.Internal;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace AutoKkutuLib.Selenium;

internal class LocalWebSocketServer
{
	public static event EventHandler<WebSocketMessageEventArgs>? OnReceive;

	public class TheSession : WsSession
	{
		public TheSession(WsServer server) : base(server)
		{
		}

		public override void OnWsConnected(HttpRequest request)
		{
			Log.Information($"WebSocket session {Id} initiated.");
		}

		public override void OnWsDisconnected()
		{
			Log.Warning($"WebSocket session {Id} disconnected.");
		}

		public override void OnWsReceived(byte[] buffer, long offset, long size)
		{
			if (size - offset <= 0)
				return;
			var msg = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
			Log.Warning("message recv: " + msg);
			if (msg[0] is 'r' or 's')
			{
				try
				{
					OnReceive?.Invoke(null, new WebSocketMessageEventArgs(msg[0] == 'r', msg[1..])); // Message prefix: Received = 'r', Send = 's'
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error handling WebSocket event listener WebSocket.");
				}
			}
			base.OnWsReceived(buffer, offset, size);
		}
	}

	public class TheServer : WsServer
	{
		public TheServer(IPAddress address, int port) : base(address, port)
		{
		}

		protected override TcpSession CreateSession() => new TheSession(this);

		protected override void OnError(SocketError error) => Log.Error("WebSocket error: {err}", error);
	}

	public LocalWebSocketServer(int port, string caddr)
	{
		Log.Information("Event listener WebSocket running on port {port}", port);

		var srv = new TheServer(IPAddress.Any, port);
		srv.Start();
	}
}
