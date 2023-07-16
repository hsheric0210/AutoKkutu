using System.Collections.Generic;
using System.Xml.Serialization;

namespace AutoKkutuGui;
[XmlRoot("servers")]
public sealed class ServerConfigDto
{
	[XmlElement("default")]
	public DefaultServerEntry DefaultServer { get; set; }

	[XmlArray("servers")]
	[XmlArrayItem("server")]
	public List<ServerEntry> Servers { get; set; }
}

public sealed class DefaultServerEntry
{

	[XmlElement("domHandler")]
	public string DomHandlerName { get; set; }

	[XmlElement("webSocketHandler")]
	public string WebSocketHandlerName { get; set; }

	[XmlElement("database")]
	public ServerDatabaseEntry Database { get; set; }
}

public sealed class ServerEntry
{
	[XmlElement("url")]
	public string Url { get; set; }

	[XmlElement("domHandler", IsNullable = true)]
	public string? DomHandlerName { get; set; }

	[XmlElement("webSocketHandler", IsNullable = true)]
	public string? WebSocketHandlerName { get; set; }

	[XmlElement("database", IsNullable = true)]
	public ServerDatabaseEntry? Database { get; set; }
}

public sealed class ServerDatabaseEntry
{
	[XmlAttribute("type")]
	public string DatabaseType { get; set; }

	[XmlText]
	public string DatabaseConnectionString { get; set; }
}