using AutoKkutuGui.Properties;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Xml.Serialization;

namespace AutoKkutuGui;
public class ServerConfig
{
	public ServerInfo Default { get; }
	public IImmutableList<ServerInfo> Servers { get; }

	public ServerConfig(string file)
	{
		if (!File.Exists(file))
			File.WriteAllText(file, GuiResources.DefaultServers);

		var builder = ImmutableList.CreateBuilder<ServerInfo>();
		using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
		{
			var ser = new XmlSerializer(typeof(ServerConfigDto));
			var dto = (ServerConfigDto?)ser.Deserialize(stream);
			if (dto != null)
			{
				var defaultDomHandler = dto.DefaultServer.DomHandlerName;
				var defaultWebSocketHandler = dto.DefaultServer.WebSocketHandlerName;
				var defaultDatabaseType = dto.DefaultServer.Database.DatabaseConnectionString;
				var defaultDatabaseConnectionString = dto.DefaultServer.Database.DatabaseConnectionString;
				Default = new ServerInfo(new Uri("about:blank"), defaultDomHandler, defaultWebSocketHandler, defaultDatabaseType, defaultDatabaseConnectionString);

				foreach (ServerEntry server in dto.Servers)
				{
					var serverUri = new Uri(new Uri(server.Url).Host);
					var domHandler = DefaultIfNullOrEmpty(server.DomHandlerName, defaultDomHandler);
					var webSocketHandler = DefaultIfNullOrEmpty(server.WebSocketHandlerName, defaultWebSocketHandler);
					var databaseType = DefaultIfNullOrEmpty(server.Database?.DatabaseType, defaultDatabaseType);
					var databaseConnectionString = DefaultIfNullOrEmpty(server.Database?.DatabaseConnectionString, defaultDatabaseConnectionString);
					builder.Add(new ServerInfo(serverUri, domHandler, webSocketHandler, databaseType, databaseConnectionString));
				}
			}
		}

		Servers = builder.ToImmutable();
	}

	private static string DefaultIfNullOrEmpty(string? str, string other) => string.IsNullOrEmpty(str) ? other : str;
}

public readonly struct ServerInfo
{
	public readonly Uri ServerUri { get; }
	public readonly string DomHandler { get; }
	public readonly string WebSocketHandler { get; }
	public readonly string DatabaseType { get; }
	public readonly string DatabaseConnectionString { get; }

	public ServerInfo(Uri serverUri, string domHandler, string webSocketHandler, string databaseType, string databaseConnectionString)
	{
		ServerUri = serverUri;
		DomHandler = domHandler;
		WebSocketHandler = webSocketHandler;
		DatabaseType = databaseType;
		DatabaseConnectionString = databaseConnectionString;
	}
}