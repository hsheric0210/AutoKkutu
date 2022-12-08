using CefSharp;

namespace AutoKkutuLib.CefSharp;
public class CefSharpWrapper : IKkutuBrowser
{
	private readonly IWebBrowser browser;

	public CefSharpWrapper(IWebBrowser browser) => this.browser = browser;

	public void Load(string url) => browser.LoadUrl(url);
	public void ShowDevTools() => browser.ShowDevTools();
	public void ExecuteScriptAsync(string script)
	{
		if (browser.CanExecuteJavascriptInMainFrame)
			browser.GetMainFrame()?.ExecuteJavaScriptAsync(script);
	}

	public async Task<JSResponse> EvaluateScriptAsync(string script)
	{
		if (!browser.CanExecuteJavascriptInMainFrame)
			return new JSResponse("MainFrame not ready", false, null);
		IFrame frame = browser.GetMainFrame();
		if (frame is null)
			return new JSResponse("MainFrame is null", false, null);

		JavascriptResponse response = await frame.EvaluateScriptAsync(script);
		return new JSResponse(response.Message, response.Success, response.Result);
	}
}
