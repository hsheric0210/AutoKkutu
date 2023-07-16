﻿using AutoKkutuGui.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AutoKkutuGui;
public class ServerConfig
{
	public ServerInfo Default { get; }
	public IImmutableList<ServerInfo> Servers { get; }

	private IImmutableDictionary<string, ServerInfo>? serverCache;

	public ServerConfig(string file)
	{
		if (!File.Exists(file))
			File.WriteAllText(file, GuiResources.Servers);

		ImmutableList<ServerInfo>.Builder builder = ImmutableList.CreateBuilder<ServerInfo>();
		using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
		{
			using var xr = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, ValidationFlags = XmlSchemaValidationFlags.None });
			var ser = new XmlSerializer(typeof(ServerConfigDto));
			var dto = (ServerConfigDto?)ser.Deserialize(xr);
			if (dto != null)
			{
				var defaultDomHandler = dto.DefaultServer.DomHandlerName;
				var defaultWebSocketHandler = dto.DefaultServer.WebSocketHandlerName;
				var defaultDatabaseType = dto.DefaultServer.Database.DatabaseConnectionString;
				var defaultDatabaseConnectionString = dto.DefaultServer.Database.DatabaseConnectionString;
				Default = new ServerInfo("blank", defaultDomHandler, defaultWebSocketHandler, defaultDatabaseType, defaultDatabaseConnectionString);

				foreach (ServerEntry server in dto.Servers)
				{
					var serverUri = new Uri(server.Url).Host;
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

	public bool TryGetServer(string serverHost, [MaybeNullWhen(false)] out ServerInfo info)
	{
		if (serverCache == null)
		{
			ImmutableDictionary<string, ServerInfo>.Builder builder = ImmutableDictionary.CreateBuilder<string, ServerInfo>();
			foreach (ServerInfo server in Servers)
				builder.Add(server.ServerHost, server);
			serverCache = builder.ToImmutable();
		}

		return serverCache.TryGetValue(serverHost, out info);
	}

	public bool TryGetServer(Uri uri, [MaybeNullWhen(false)] out ServerInfo info) => TryGetServer(uri.Host, out info);

	private static string DefaultIfNullOrEmpty(string? str, string other) => string.IsNullOrEmpty(str) ? other : str;
}

public readonly struct ServerInfo : IEquatable<ServerInfo>
{
	public readonly string ServerHost { get; }
	public readonly string DomHandler { get; }
	public readonly string WebSocketHandler { get; }
	public readonly string DatabaseType { get; }
	public readonly string DatabaseConnectionString { get; }

	public ServerInfo(string serverHost, string domHandler, string webSocketHandler, string databaseType, string databaseConnectionString)
	{
		ServerHost = serverHost;
		DomHandler = domHandler;
		WebSocketHandler = webSocketHandler;
		DatabaseType = databaseType;
		DatabaseConnectionString = databaseConnectionString;
	}

	public override bool Equals(object? obj) => obj is ServerInfo info && Equals(info);
	public bool Equals(ServerInfo other) => ServerHost.Equals(other.ServerHost, StringComparison.OrdinalIgnoreCase) && DomHandler == other.DomHandler && WebSocketHandler == other.WebSocketHandler && DatabaseType == other.DatabaseType && DatabaseConnectionString == other.DatabaseConnectionString;
	public override int GetHashCode() => HashCode.Combine(ServerHost, DomHandler, WebSocketHandler, DatabaseType, DatabaseConnectionString);

	public static bool operator ==(ServerInfo left, ServerInfo right) => left.Equals(right);
	public static bool operator !=(ServerInfo left, ServerInfo right) => !(left == right);
}