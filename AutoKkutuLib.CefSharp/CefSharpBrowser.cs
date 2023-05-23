using CefSharp;
using CefSharp.Wpf;
using Serilog;
using System.IO;
using System.Xml.Serialization;

namespace AutoKkutuLib.CefSharp;
public class CefSharpBrowser : BrowserBase
{
	private const string ConfigFile = "CefSharp.xml";
	private IWebBrowser browser;

	public override object? BrowserControl => browser;

	public override void LoadFrontPage()
	{
		Log.Information("Initializing CefSharp");

		var serializer = new XmlSerializer(typeof(CefConfigDto));

		// Default config
		var config = new CefConfigDto()
		{
			MainPage = "https://kkutu.pink/",
			LogFile = "CefSharp.log",
			LogSeverity = LogSeverity.Default,
			CefCommandLineArgs =
			{
				"disable-direct-write=1",
				"disable-gpu",
				"enable-begin-frame-scheduling"
			},
			UserAgent = "Chrome",
			CachePath = Environment.CurrentDirectory + "\\CefSharp"
		};

		if (File.Exists(ConfigFile))
		{
			try
			{
				using FileStream stream = File.Open(ConfigFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
				config = (CefConfigDto?)serializer.Deserialize(stream)!;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to read CefSharp config.");
			}
		}
		else
		{
			try
			{
				using FileStream stream = File.Open(ConfigFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
				serializer.Serialize(stream, config);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to write default CefSharp config.");
			}
		}

		using var settings = new CefSettings
		{
			ResourcesDirPath = config.ResourcesDirPath,
			LogFile = config.LogFile,
			LogSeverity = config.LogSeverity,
			JavascriptFlags = config.JavascriptFlags,
			PackLoadingDisabled = config.PackLoadingDisabled,
			UserAgentProduct = config.UserAgentProduct,
			LocalesDirPath = config.LocalesDirPath,
			RemoteDebuggingPort = config.RemoteDebuggingPort,
			UserAgent = config.UserAgent,
			WindowlessRenderingEnabled = config.WindowlessRenderingEnabled,
			PersistSessionCookies = config.PersistSessionCookies,
			PersistUserPreferences = config.PersistUserPreferences,
			AcceptLanguageList = config.AcceptLanguageList,
			BackgroundColor = config.BackgroundColor,
			UncaughtExceptionStackSize = config.UncaughtExceptionStackSize,
			Locale = config.Locale,
			IgnoreCertificateErrors = config.IgnoreCertificateErrors,
			UserDataPath = config.UserDataPath,
			CookieableSchemesList = config.CookieableSchemesList,
			ChromeRuntime = config.ChromeRuntime,
			MultiThreadedMessageLoop = config.MultiThreadedMessageLoop,
			BrowserSubprocessPath = config.BrowserSubprocessPath,
			CachePath = config.CachePath,
			RootCachePath = config.RootCachePath,
			ExternalMessagePump = config.ExternalMessagePump,
			CookieableSchemesExcludeDefaults = config.CookieableSchemesExcludeDefaults
		};
		if (!config.GpuAcceleration)
			settings.DisableGpuAcceleration();
		foreach (var arg in config.CefCommandLineArgs)
			settings.CefCommandLineArgs.Add(arg);

		try
		{
			if (!Cef.IsInitialized && !Cef.Initialize(settings, true, (IApp?)null))
				Log.Warning("CefSharp initialization failed.");
		}
		catch (Exception ex)
		{
			Log.Error(ex, "CefSharp initialization exception.");
		}

		browser = new ChromiumWebBrowser()
		{
			Address = config.MainPage,
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
