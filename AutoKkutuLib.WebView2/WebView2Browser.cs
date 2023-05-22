using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;

namespace AutoKkutuLib.CefSharp;
public class WebView2Browser : BrowserBase
{
	private readonly WebView2 control;

	public override object? BrowserControl => control;

	public WebView2Browser()
	{
		control = new WebView2();
		control.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
	}

	public void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		if (args.IsSuccess)
			PageLoaded.Invoke(sender, new PageLoadedEventArgs(control.Source.ToString()));
		else
			PageError.Invoke(sender, new PageErrorEventArgs($"HTTP {args.HttpStatusCode}, WebError {args.WebErrorStatus}", control.Source.ToString()));
	}

	public override void Load(string url) => control.Source = new Uri(url);

	public override void ShowDevTools() => control.CoreWebView2.OpenDevToolsWindow();

	public override void ExecuteJavaScript(string script, string? errorMessage = null)
	{
		try
		{
			control.ExecuteScriptAsync(script);
		}
		catch (Exception ex)
		{
			Log.Error(ex, errorMessage ?? "JavaScript execution error");
		}
	}

	public override async Task<JavaScriptCallback> EvaluateJavaScriptAsync(string script)
	{
		var message = await control.ExecuteScriptAsync(script);
		return new JavaScriptCallback(message, !string.IsNullOrEmpty(message), message);
	}
}
