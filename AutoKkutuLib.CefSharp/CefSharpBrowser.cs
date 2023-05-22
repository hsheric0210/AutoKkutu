using CefSharp;
using CefSharp.Wpf;
using Serilog;

namespace AutoKkutuLib.CefSharp;
public class CefSharpBrowser : BrowserBase
{
	private IWebBrowser browser;

	public override object? BrowserControl => browser;

	public override void LoadFrontPage()
	{
		browser = new ChromiumWebBrowser()
		{
			Address = "https://kkutu.pink",
		};
		browser.FrameLoadEnd += OnFrameLoadEnd;
		browser.LoadError += OnLoadError;
	}

	public void OnFrameLoadEnd(object? sender, FrameLoadEndEventArgs args) => PageLoaded?.Invoke(sender, new PageLoadedEventArgs(args.Url));

	public void OnLoadError(object? sender, LoadErrorEventArgs args) => PageError?.Invoke(sender, new PageErrorEventArgs(args.ErrorText, args.FailedUrl));

	public override void Load(string url) => browser.LoadUrl(url);

	public override void ShowDevTools() => browser.ShowDevTools();

	public override void ExecuteJavaScript(string script, string? errorMessage = null)
	{
		try
		{
			if (browser.CanExecuteJavascriptInMainFrame)
				browser.GetMainFrame()?.ExecuteJavaScriptAsync(script);
		}
		catch (Exception ex)
		{
			Log.Error(ex, errorMessage ?? "JavaScript execution error");
		}
	}

	public override async Task<JavaScriptCallback> EvaluateJavaScriptAsync(string script)
	{
		if (!browser.CanExecuteJavascriptInMainFrame)
			return new JavaScriptCallback("MainFrame not ready", false, null);
		IFrame frame = browser.GetMainFrame();
		if (frame is null)
			return new JavaScriptCallback("MainFrame is null", false, null);

		JavascriptResponse response = await frame.EvaluateScriptAsync(script, timeout: TimeSpan.FromSeconds(3));
		return new JavaScriptCallback(response.Message, response.Success, response.Result);
	}
}
