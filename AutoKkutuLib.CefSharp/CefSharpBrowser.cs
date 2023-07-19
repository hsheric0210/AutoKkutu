using AutoKkutuLib.Browser;
using AutoKkutuLib.CefSharp.Properties;
using AutoKkutuLib.Properties;
using CefSharp;
using CefSharp.Wpf;
using CefSharp.Wpf.Experimental;
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

	public override string JavaScriptBaseNamespace { get; }

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
					LibLogger.Debug<CefSharpBrowser>("Cef command-line argument added: {switch} = {value}", pieces[0], pieces[1]);
				}
				else
				{
					LibLogger.Debug<CefSharpBrowser>("Cef command-line switch added: {switch}", arg);
					settings.CefCommandLineArgs?.Add(arg);
				}
			}
		}
		return settings;
	}

	public CefSharpBrowser()
	{
		jsbGlobalObjectName = GenerateRandomString(1677243);
		jsbObjectName = GenerateRandomString(1677244);

		try
		{
			if (!File.Exists(ConfigFile))
				File.WriteAllText(ConfigFile, CefSharpResources.CefSharp);

			var serializer = new XmlSerializer(typeof(CefConfigDto));
			using var stream = File.Open(ConfigFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			config = (CefConfigDto?)serializer.Deserialize(stream)!;
		}
		catch (Exception ex)
		{
			LibLogger.Error<CefSharpBrowser>(ex, "Failed to reading/writing default CefSharp config.");
			throw new IOException("CefSharp configuration file load exception", ex);
		}

		JavaScriptBaseNamespace = config.JavaScriptInjectionBaseNamespace;
		if (string.IsNullOrWhiteSpace(JavaScriptBaseNamespace))
			JavaScriptBaseNamespace = "window";

		var settings = CefConfigToCefSettings(config);
		try
		{
			if (!Cef.IsInitialized && !Cef.Initialize(settings, true, (IApp?)null))
				LibLogger.Warn<CefSharpBrowser>("CefSharp initialization failed.");
		}
		catch (Exception ex)
		{
			LibLogger.Error<CefSharpBrowser>(ex, "CefSharp initialization exception.");
		}

		LibLogger.Verbose<CefSharpBrowser>("Cef settings applied.");

		nameRandom = BrowserRandomNameMapping.MainHelperJs(this);
		nameRandom.Add("___jsbGlobal___", jsbGlobalObjectName);
		nameRandom.Add("___jsbObj___", jsbObjectName);
		LibLogger.Debug<CefSharpBrowser>("communicatorJs + mainHelperJs name mapping: {nameRandom}", nameRandom);
	}

	public override void LoadFrontPage()
	{
		LibLogger.Verbose<CefSharpBrowser>("Initializing CefSharp");

		browser = new ChromiumWebBrowser();
		// Prevent getting detected by 'window.CefSharp' property existence checks
		browser.JavascriptObjectRepository.Settings.JavascriptBindingApiGlobalObjectName = jsbGlobalObjectName;
		var bindingObject = new JavaScriptBindingObject();
		bindingObject.WebSocketSend += OnWebSocketSend;
		bindingObject.WebSocketReceive += OnWebSocketReceive;
		browser.JavascriptObjectRepository.Register(jsbObjectName, bindingObject);
		browser.WpfKeyboardHandler = new WpfImeKeyboardHandler(browser); // https://github.com/cefsharp/CefSharp/issues/1262

		browser.FrameLoadEnd += OnFrameLoadEnd;
		browser.LoadError += OnLoadError;

		browser.IsBrowserInitializedChanged += OnInitialized;

		LibLogger.Verbose<CefSharpBrowser>("ChromiumWebBrowser instance created.");
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
		browser.GetDevToolsClient().Page.AddScriptToEvaluateOnNewDocumentAsync(nameRandom.ApplyTo(LibResources.namespaceInitJs + ';' + CefSharpResources.communicatorJs + ';' + LibResources.mainHelperJs));
		// browser.GetDevToolsClient().Page.AddScriptToEvaluateOnNewDocumentAsync(nameRandom.ApplyTo(LibResources.namespaceInitJs + ';' + LibResources.mainHelperJs + ';' + CefSharpResources.communicatorJs)); // TODO: try this.

		LibLogger.Info<CefSharpBrowser>("ChromiumWebBrowser initialized.");
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
				LibLogger.Error<CefSharpBrowser>(ex, "Handling WebSocket send event error. Message: {msg}", args.Json);
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
				LibLogger.Error<CefSharpBrowser>(ex, "Handling WebSocket receive event error. Message: {msg}", args.Json);
			}
		});
	}

	public void OnFrameLoadEnd(object? sender, FrameLoadEndEventArgs args)
	{
		LibLogger.Verbose<CefSharpBrowser>("CefSharp frame loaded: {url}", args.Url);
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
			LibLogger.Error<CefSharpBrowser>(ex, errorMessage ?? "JavaScript execution error");
		}
	}

	public override async Task<object?> EvaluateJavaScriptRawAsync(string script)
	{
		if (!browser.CanExecuteJavascriptInMainFrame)
		{
			LibLogger.Warn<CefSharpBrowser>("Trying to execute JavaScript before mainframe initialization finished.");
			return null;
		}

		var frame = browser.GetMainFrame();
		if (frame is null)
			throw new InvalidOperationException("MainFrame is null");
		var task = frame.EvaluateScriptAsync(script, timeout: TimeSpan.FromSeconds(1));
		await task.WaitAsync(TimeSpan.FromSeconds(1));
		var response = await task; // Will be finished immediately because the task had already finished @ L248
		return response.Result;
	}

	public override IntPtr GetWindowHandle() => browser.GetBrowserHost().GetWindowHandle();

	public override void SetFocus()
	{
		//browser.Focus();
		browser.GetBrowserHost().SetFocus(true);
		browser.GetBrowserHost().SendFocusEvent(true);
	}
}
