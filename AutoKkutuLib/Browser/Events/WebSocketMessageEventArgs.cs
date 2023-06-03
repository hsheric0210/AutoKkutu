using System.Text.Json.Nodes;

namespace AutoKkutuLib.Browser.Events;

public class WebSocketMessageEventArgs : EventArgs
{
	public Guid SocketId { get; set; }
	public bool IsReceived { get; set; }
	public string Type { get; set; }
	public JsonNode Json { get; set; }

	public WebSocketMessageEventArgs(Guid socketId, bool received, string json)
	{
		SocketId = socketId;
		IsReceived = received;
		Json = JsonNode.Parse(json) ?? throw new AggregateException("Failed to parse JSON");
		Type = Json["type"]?.ToJsonString() ?? throw new AggregateException("Message type unavailable");
	}
}