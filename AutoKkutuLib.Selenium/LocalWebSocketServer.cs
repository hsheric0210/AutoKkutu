using AutoKkutuLib.Browser.Events;
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
	public static event EventHandler<WebSocketMessageEventArgs>? MessageReceived;

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
			if (msg[0] is 'r' or 's')
			{
				try
				{
					MessageReceived?.Invoke(null, new WebSocketMessageEventArgs(Id, msg[0] == 'r', msg[1..])); // Message prefix: Received = 'r', Send = 's'
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

		protected override TcpSession CreateSession()
		{
			var session = new TheSession(this);
			return session;
		}

		protected override void OnError(SocketError error) => Log.Error("WebSocket error: {err}", error);
	}

	public static IDisposable Start(int port)
	{
		Log.Information("Event listener WebSocket running on port {port}", port);

		var server = new TheServer(IPAddress.Any, port);
		server.Start();
		return server;
	}
}
