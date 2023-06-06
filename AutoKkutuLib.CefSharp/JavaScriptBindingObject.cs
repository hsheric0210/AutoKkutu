namespace AutoKkutuLib.CefSharp;
public class JavaScriptBindingObject
{
	public event EventHandler<WebSocketJsonMessageEventArgs> WebSocketReceive;
	public event EventHandler<WebSocketJsonMessageEventArgs> WebSocketSend;

	public void onReceive(string data)
	{
		WebSocketReceive?.Invoke(this, new WebSocketJsonMessageEventArgs(data));
	}

	public void onSend(string data)
	{
		WebSocketSend?.Invoke(this, new WebSocketJsonMessageEventArgs(data));
	}

	public class WebSocketJsonMessageEventArgs : EventArgs
	{
		public string Json { get; set; }

		public WebSocketJsonMessageEventArgs(string json) => Json = json;
	}
}
