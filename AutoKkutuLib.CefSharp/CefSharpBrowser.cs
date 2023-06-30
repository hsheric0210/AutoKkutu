using AutoKkutuLib.Browser;
using AutoKkutuLib.CefSharp.Properties;
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
	private readonly CefConfigDto config;
	private ChromiumWebBrowser browser;

	private readonly string jsbGlobalObjectName;
	private readonly string jsbObjectName;

	// Will be randomized
	private readonly NameMapping nameRandom;

	public override object? BrowserControl => browser;

	private static CefConfigDto GetDefaultCefConfig()
	{
		var dflt = new CefSettings();
		return new CefConfigDto()
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
	}

	private static void SetIfAvail(string? value, Action<string> setter)
	{
		if (!string.IsNullOrWhiteSpace(value))
			setter(value!);
	}

	private static CefSettings CefConfigToCefSettings(CefConfigDto config)
	{
		var settings = new CefSettings();
		SetIfAvail(config.ResourcesDirPath, s => settings.ResourcesDirPath = s);
		SetIfAvail(config.LogFile, s => settings.LogFile = s);
		SetIfAvail(config.LogFile, s => settings.LogFile = s);
		SetIfAvail(config.JavascriptFlags, s => settings.JavascriptFlags = s);
		SetIfAvail(config.UserAgentProduct, s => settings.UserAgentProduct = s);
		SetIfAvail(config.LocalesDirPath, s => settings.LocalesDirPath = s);
		SetIfAvail(config.UserAgent, s => settings.UserAgent = s);
		SetIfAvail(config.AcceptLanguageList, s => settings.AcceptLanguageList = s);
		SetIfAvail(config.Locale, s => settings.Locale = s);
		SetIfAvail(config.UserDataPath, s => settings.UserDataPath = s);
		SetIfAvail(config.CookieableSchemesList, s => settings.CookieableSchemesList = s);
		SetIfAvail(config.BrowserSubprocessPath, s => settings.BrowserSubprocessPath = s);
		SetIfAvail(config.CachePath, s => settings.CachePath = s);
		SetIfAvail(config.RootCachePath, s => settings.RootCachePath = s);
		settings.LogSeverity = config.LogSeverity;
		settings.PackLoadingDisabled = config.PackLoadingDisabled;
		settings.RemoteDebuggingPort = config.RemoteDebuggingPort;
		settings.WindowlessRenderingEnabled = config.WindowlessRenderingEnabled;
		settings.PersistSessionCookies = config.PersistSessionCookies;
		settings.PersistUserPreferences = config.PersistUserPreferences;
		settings.BackgroundColor = config.BackgroundColor;
		settings.UncaughtExceptionStackSize = config.UncaughtExceptionStackSize;
		settings.IgnoreCertificateErrors = config.IgnoreCertificateErrors;
		settings.ChromeRuntime = config.ChromeRuntime;
		settings.MultiThreadedMessageLoop = config.MultiThreadedMessageLoop;
		settings.ExternalMessagePump = config.ExternalMessagePump;
		settings.CookieableSchemesExcludeDefaults = config.CookieableSchemesExcludeDefaults;
		if (config.CefCommandLineArgs != null)
		{
			foreach (var arg in config.CefCommandLineArgs)
			{
				if (arg.Contains('=')) // key-value type
				{
					var pieces = arg.Split('=', 2);
					settings.CefCommandLineArgs?.Add(pieces[0], pieces[1]);
					Log.Information("Cef command-line argument added: {switch} = {value}", pieces[0], pieces[1]);
				}
				else
				{
					Log.Information("Cef command-line switch added: {switch}", arg);
					settings.CefCommandLineArgs?.Add(arg);
				}
			}
		}
		return settings;
	}

	public CefSharpBrowser()
	{
		var serializer = new XmlSerializer(typeof(CefConfigDto));

		jsbGlobalObjectName = GenerateScriptTypeName(1677243, true);
		jsbObjectName = GenerateScriptTypeName(1677244, true);

		nameRandom = BrowserRandomNameMapping.CreateForWsHook(this);
		nameRandom.Add("___jsbGlobal___", jsbGlobalObjectName);
		nameRandom.Add("___jsbObj___", jsbObjectName);

		config = GetDefaultCefConfig();
		try
		{
			if (File.Exists(ConfigFile))
			{
				using FileStream stream = File.Open(ConfigFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
				config = (CefConfigDto?)serializer.Deserialize(stream)!;
			}
			else
			{
				using FileStream stream = File.Open(ConfigFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
				serializer.Serialize(stream, config);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to reading/writiting default CefSharp config.");
		}

		var settings = CefConfigToCefSettings(config);
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

		browser = new ChromiumWebBrowser();
		// Prevent getting detected by 'window.CefSharp' property existence checks
		browser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = jsbGlobalObjectName;
		var bindingObject = new JavaScriptBindingObject();
		bindingObject.WebSocketSend += OnWebSocketSend;
		bindingObject.WebSocketReceive += OnWebSocketReceive;
		browser.JavascriptObjectRepository.Register(jsbObjectName, bindingObject);

		browser.FrameLoadEnd += OnFrameLoadEnd;
		browser.LoadError += OnLoadError;

		Log.Verbose("Name randomization: {nameRandom}", nameRandom);
		browser.IsBrowserInitializedChanged += OnInitialized;

		Log.Information("ChromiumWebBrowser instance created.");
	}

	private void OnInitialized(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
	{
		if (!browser.IsBrowserInitialized)
			return;

		browser.Address = config?.MainPage ?? "https://www.whatsmyip.org/";

		// 'Page.addScriptToEvaluateOnNewDocumentAsync' CDP 커맨드가 'FrameLoadStart' 이벤트 받고 'ExecuteScriptAsync' 호출하는 것보다 훨씬 더 확실함.
		// 'FrameLoadStart' 이벤트 받자마자 스크립트 실행 등록시키더라도, 스크립트가 큐에 등록되고 전송되는 사이에 사이트 측 스크립트가 먼저 실행되어 버릴 위험성이 있음.
		// 그러나, 'Page.addScriptToEvaluateOnNewDocumentAsync'는 크롬 자체 지원 기능이기도 하고, 무엇보다 사이트의 스크립트가 로드되기 전에 실행되는 것을 보장함. (https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-addScriptToEvaluateOnNewDocument)
		browser.GetDevToolsClient().Page.EnableAsync();
		browser.GetDevToolsClient().Page.AddScriptToEvaluateOnNewDocumentAsync(nameRandom.ApplyTo(LibResources.wsHook + ';' + CefSharpResources.wsListener));

		Log.Information("ChromiumWebBrowser initialized.");
	}

	public void OnWebSocketSend(object? sender, JavaScriptBindingObject.WebSocketJsonMessageEventArgs args)
	{
		Task.Run(() =>
		{
			try
			{
				WebSocketMessage?.Invoke(this, new WebSocketMessageEventArgs(Guid.Empty, false, args.Json));
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Handling WebSocket send event error. Message: {msg}", args.Json);
			}
		});
	}

	public void OnWebSocketReceive(object? sender, JavaScriptBindingObject.WebSocketJsonMessageEventArgs args)
	{
		Task.Run(() =>
		{
			try
			{
				WebSocketMessage?.Invoke(this, new WebSocketMessageEventArgs(Guid.Empty, true, args.Json));
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Handling WebSocket receive event error. Message: {msg}", args.Json);
			}
		});
	}

	public void OnFrameLoadEnd(object? sender, FrameLoadEndEventArgs args)
	{
		Log.Verbose("CefSharp frame loaded: {url}", args.Url);
		PageLoaded?.Invoke(sender, new PageLoadedEventArgs(args.Url));
	}

	public void OnLoadError(object? sender, LoadErrorEventArgs args) => PageError?.Invoke(sender, new PageErrorEventArgs(args.ErrorText, args.FailedUrl));

	public override void Load(string url) => browser.LoadUrl(url);

	public override void ShowDevTools()
	{
		browser.ShowDevTools();
	}

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
		Task<JavascriptResponse> task = frame.EvaluateScriptAsync(script, timeout: TimeSpan.FromSeconds(1));
		await task.WaitAsync(TimeSpan.FromSeconds(1));
		JavascriptResponse response = await task; // Will be finished immediately because the task had already finished @ L248
		return new JavaScriptCallback(response.Message, response.Success, response.Result);
	}
}
