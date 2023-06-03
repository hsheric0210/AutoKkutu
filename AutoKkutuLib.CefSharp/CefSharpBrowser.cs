using AutoKkutuLib.Browser;
using AutoKkutuLib.Browser.Events;
using AutoKkutuLib.CefSharp.Properties;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Properties;
using CefSharp;
using CefSharp.Wpf;
using Serilog;
using System.IO;
using System.Xml.Serialization;

namespace AutoKkutuLib.CefSharp;
public class CefSharpBrowser : BrowserBase
{
	private const string ConfigFile = "CefSharp.xml";
	private CefConfigDto config;
	private ChromiumWebBrowser browser;

	// Will be randomized
	private string JavascriptBindingGlobalObjectName = "jsbGlobal";
	private string JavascriptBindingObjectName = "jsbObj";
	private string WsHookName = "wsHook";
	private string OriginalWsName = "wsHook";

	public override object? BrowserControl => browser;

	public CefSharpBrowser()
	{
		var serializer = new XmlSerializer(typeof(CefConfigDto));

		JavascriptBindingGlobalObjectName = "Vue" + Random.Shared.NextInt64();
		JavascriptBindingObjectName = "Vue" + Random.Shared.NextInt64();
		WsHookName = "Vue" + Random.Shared.NextInt64();
		OriginalWsName = "Vue" + Random.Shared.NextInt64();

		var dflt = new CefSettings();
		config = new CefConfigDto()
		{
			MainPage = "https://kkutu.pink/",
			ResourcesDirPath = dflt.ResourcesDirPath,
			LogFile = "CefSharp.log",
			LogSeverity = LogSeverity.Default,
			JavascriptFlags = dflt.JavascriptFlags,
			PackLoadingDisabled = dflt.PackLoadingDisabled,
			UserAgentProduct = dflt.UserAgentProduct,
			UserAgent = dflt.UserAgent,
			LocalesDirPath = dflt.LocalesDirPath,
			RemoteDebuggingPort = dflt.RemoteDebuggingPort,
			WindowlessRenderingEnabled = dflt.WindowlessRenderingEnabled,
			PersistSessionCookies = dflt.PersistSessionCookies,
			PersistUserPreferences = dflt.PersistUserPreferences,
			AcceptLanguageList = dflt.AcceptLanguageList,
			BackgroundColor = dflt.BackgroundColor,
			UncaughtExceptionStackSize = dflt.UncaughtExceptionStackSize,
			Locale = dflt.Locale,
			IgnoreCertificateErrors = dflt.IgnoreCertificateErrors,
			UserDataPath = Environment.CurrentDirectory + "\\CefUser",
			CookieableSchemesList = dflt.CookieableSchemesList,
			ChromeRuntime = dflt.ChromeRuntime,
			MultiThreadedMessageLoop = dflt.MultiThreadedMessageLoop,
			BrowserSubprocessPath = dflt.BrowserSubprocessPath,
			CachePath = dflt.CachePath,
			RootCachePath = dflt.RootCachePath,
			ExternalMessagePump = dflt.ExternalMessagePump,
			CookieableSchemesExcludeDefaults = dflt.CookieableSchemesExcludeDefaults,
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

		var settings = new CefSettings();
		if (!string.IsNullOrWhiteSpace(config.ResourcesDirPath))
			settings.ResourcesDirPath = config.ResourcesDirPath;
		if (!string.IsNullOrWhiteSpace(config.LogFile))
			settings.LogFile = config.LogFile;
		settings.LogSeverity = config.LogSeverity;
		if (!string.IsNullOrWhiteSpace(config.JavascriptFlags))
			settings.JavascriptFlags = config.JavascriptFlags;
		settings.PackLoadingDisabled = config.PackLoadingDisabled;
		if (!string.IsNullOrWhiteSpace(config.UserAgentProduct))
			settings.UserAgentProduct = config.UserAgentProduct;
		if (!string.IsNullOrWhiteSpace(config.LocalesDirPath))
			settings.LocalesDirPath = config.LocalesDirPath;
		settings.RemoteDebuggingPort = config.RemoteDebuggingPort;
		if (!string.IsNullOrWhiteSpace(config.UserAgent))
			settings.UserAgent = config.UserAgent;
		settings.WindowlessRenderingEnabled = config.WindowlessRenderingEnabled;
		settings.PersistSessionCookies = config.PersistSessionCookies;
		settings.PersistUserPreferences = config.PersistUserPreferences;
		if (!string.IsNullOrWhiteSpace(config.AcceptLanguageList))
			settings.AcceptLanguageList = config.AcceptLanguageList;
		settings.BackgroundColor = config.BackgroundColor;
		settings.UncaughtExceptionStackSize = config.UncaughtExceptionStackSize;
		if (!string.IsNullOrWhiteSpace(config.Locale))
			settings.Locale = config.Locale;
		settings.IgnoreCertificateErrors = config.IgnoreCertificateErrors;
		if (!string.IsNullOrWhiteSpace(config.UserDataPath))
			settings.UserDataPath = config.UserDataPath;
		if (!string.IsNullOrWhiteSpace(config.CookieableSchemesList))
			settings.CookieableSchemesList = config.CookieableSchemesList;
		settings.ChromeRuntime = config.ChromeRuntime;
		settings.MultiThreadedMessageLoop = config.MultiThreadedMessageLoop;
		if (!string.IsNullOrWhiteSpace(config.BrowserSubprocessPath))
			settings.BrowserSubprocessPath = config.BrowserSubprocessPath;
		if (!string.IsNullOrWhiteSpace(config.CachePath))
			settings.CachePath = config.CachePath;
		if (!string.IsNullOrWhiteSpace(config.RootCachePath))
			settings.RootCachePath = config.RootCachePath;
		settings.ExternalMessagePump = config.ExternalMessagePump;
		settings.CookieableSchemesExcludeDefaults = config.CookieableSchemesExcludeDefaults;
		if (config.CefCommandLineArgs != null)
			foreach (var arg in config.CefCommandLineArgs)
			{
				if (arg.Contains('='))
				{
					var pieces = arg.Split('=', 2);
					settings?.CefCommandLineArgs.Add(pieces[0], pieces[1]);
				}
				else
					settings?.CefCommandLineArgs?.Add(arg);
			}

		try
		{
			if (!Cef.IsInitialized && !Cef.Initialize(settings, true, (IApp?)null))
				Log.Warning("CefSharp initialization failed.");
		}
		catch (Exception ex)
		{
			Log.Error(ex, "CefSharp initialization exception.");
		}

		Log.Information("Cef settings applied.");
	}

	public override void LoadFrontPage()
	{
		Log.Information("Initializing CefSharp");

		browser = new ChromiumWebBrowser()
		{
			Address = config?.MainPage ?? "https://www.whatsmyip.org/",
		};
		// Prevent getting detected by 'window.CefSharp' property existence checks
		browser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = JavascriptBindingGlobalObjectName;
		var bindingObject = new JavaScriptBindingObject();
		bindingObject.WebSocketSend += OnWebSocketSend;
		bindingObject.WebSocketReceive += OnWebSocketReceive;
		browser.JavascriptObjectRepository.Register(JavascriptBindingObjectName, bindingObject);
		Log.Debug("JSB Global Object: {jsbGlobal}, My JSB Object: {jsbObj}, wsHook func: {wsHook}", JavascriptBindingGlobalObjectName, JavascriptBindingObjectName, WsHookName);

		Log.Information("ChromiumWebBrowser instance created.");

		browser.FrameLoadEnd += OnFrameLoadEnd;
		browser.LoadError += OnLoadError;
		browser.FrameLoadStart += OnFrameLoadStart;
	}

	public void OnWebSocketSend(object? sender, JavaScriptBindingObject.WebSocketJsonMessageEventArgs args)
	{
		try
		{
			WebSocketMessage?.Invoke(this, new WebSocketMessageEventArgs(Guid.Empty, false, args.Json));
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Handling WebSocket send event error.");
		}
	}

	public void OnWebSocketReceive(object? sender, JavaScriptBindingObject.WebSocketJsonMessageEventArgs args)
	{
		try
		{
			WebSocketMessage?.Invoke(this, new WebSocketMessageEventArgs(Guid.Empty, true, args.Json));
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Handling WebSocket receive event error.");
		}
	}

	public void OnFrameLoadStart(object? sender, FrameLoadStartEventArgs args)
	{
		Log.Verbose("Injecting wsHook and wsListener: {url}", args.Url);
		browser.ExecuteScriptAsync((LibResources.wsHookObf + ';' + CefSharpResources.wsListenerObf)
			.Replace("___wsHook___", WsHookName)
			.Replace("___jsbGlobal___", JavascriptBindingGlobalObjectName)
			.Replace("___jsbObj___", JavascriptBindingObjectName)
			.Replace("___originalWS___", OriginalWsName), false);
	}

	public void OnFrameLoadEnd(object? sender, FrameLoadEndEventArgs args)
	{
		Log.Verbose("CefSharp frame loaded: {url}", args.Url);
		PageLoaded?.Invoke(sender, new PageLoadedEventArgs(args.Url));
	}

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

	public override async Task<JavaScriptCallback> EvaluateJavaScriptRawAsync(string script)
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
