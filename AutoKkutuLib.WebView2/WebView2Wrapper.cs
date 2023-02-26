using Microsoft.Web.WebView2.Core;

namespace AutoKkutuLib.CefSharp;
public class WebView2Wrapper : IKkutuBrowser
{
	private readonly CoreWebView2 core;

	public WebView2Wrapper(CoreWebView2 core) => this.core = core;

	public void Load(string url) => core.Navigate(url);
	public void ShowDevTools() => core.OpenDevToolsWindow();
	public void ExecuteScriptAsync(string script)
	{
		core.ExecuteScriptAsync(script);
	}

	public async Task<JSResponse> EvaluateScriptAsync(string script)
	{
		var message = await core.ExecuteScriptAsync(script);
		return new JSResponse(message, true, message);
	}
}
