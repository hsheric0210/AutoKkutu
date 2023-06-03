using System.Text.Json.Nodes;

namespace AutoKkutuLib.Browser;

public class PageErrorEventArgs : EventArgs
{
	public PageErrorEventArgs(string errorText, string failedUrl)
	{
		ErrorText = errorText;
		FailedUrl = failedUrl;
	}

	public string FailedUrl { get; }
	public string ErrorText { get; }
}

public class PageLoadedEventArgs : EventArgs
{
	public PageLoadedEventArgs(string url) => Url = url;

	public string Url { get; }
}

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
		Type = Json["type"]?.GetValue<string>() ?? throw new AggregateException("Message type unavailable");
	}
}