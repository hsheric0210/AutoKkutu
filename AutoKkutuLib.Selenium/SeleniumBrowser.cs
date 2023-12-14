using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.ObjectModel;
using SeleniumUndetectedChromeDriver;
using System.Xml.Serialization;
using AutoKkutuLib.Properties;
using AutoKkutuLib.Selenium.Properties;
using System.Net;
using System.Net.Sockets;
using AutoKkutuLib.Browser;
using System.Reflection;
using System.Diagnostics;
using OpenQA.Selenium.Chrome.ChromeDriverExtensions;

namespace AutoKkutuLib.Selenium;
public class SeleniumBrowser : BrowserBase, IDisposable
{
	private readonly static ReadOnlyCollection<IWebElement> emptyWebElements = new(new List<IWebElement>());
	private const string BrowserConfigFile = "Browser.xml";

	private readonly BrowserRandomNameMapping nameRandom;

	private UndetectedChromeDriver driver = null!;
	private IDisposable? WsServer;
	private SeleniumConfigDto config;
	private bool disposedValue;
	private IntPtr mainHwnd;

	public override object? BrowserControl => null;

	public override string JavaScriptBaseNamespace { get; }

	public SeleniumBrowser()
	{
		var wsPort = FindFreePort();
		var wsAddrClient = "ws://" + new IPEndPoint(IPAddress.Loopback, wsPort).ToString();
		LocalWebSocketServer.MessageReceived += OnWebSocketMessage;
		WsServer = LocalWebSocketServer.Start(wsPort);
		LibLogger.Info<SeleniumBrowser>("Browser-side event listener WebSocket will connect to {addr}", wsAddrClient);


		try
		{
			// Write default config
			if (!File.Exists(BrowserConfigFile))
				File.WriteAllText(BrowserConfigFile, SeleniumResources.DefaultBrowserConfig);

			using var stream = File.Open(BrowserConfigFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			var serializer = new XmlSerializer(typeof(SeleniumConfigDto));
			config = (SeleniumConfigDto?)serializer.Deserialize(stream)!;
		}
		catch (Exception ex)
		{
			LibLogger.Error<SeleniumBrowser>(ex, "Failed to read Selenium config.");
			throw new IOException("Selenium configuration file load exception", ex);
		}

		JavaScriptBaseNamespace = config.JavaScriptInjectionBaseNamespace;
		if (string.IsNullOrWhiteSpace(JavaScriptBaseNamespace))
			JavaScriptBaseNamespace = "window";

		nameRandom = BrowserRandomNameMapping.MainHelperJs(this);
		nameRandom.GenerateScriptType("___wsGlobal___", 16383);
		nameRandom.GenerateScriptType("___wsBuffer___", 16384);
		nameRandom.Add("___wsAddr___", wsAddrClient);
	}

	private static string? NullIfWhiteSpace(string? str) => string.IsNullOrWhiteSpace(str) ? null : str;

	private static string? ReplaceCd(string? str) => NullIfWhiteSpace(str)?.Replace("%CURRENTDIR%", Environment.CurrentDirectory);

	public override async void LoadFrontPage()
	{
		var opt = new ChromeOptions() { LeaveBrowserRunning = config.LeaveBrowserRunning };
		opt.BinaryLocation = ReplaceCd(config.BinaryLocation);
		opt.DebuggerAddress = NullIfWhiteSpace(config.DebuggerAddress);
		opt.MinidumpPath = ReplaceCd(config.MinidumpPath);
		if (!string.IsNullOrWhiteSpace(config.ProxyIp))
		{
			if (config.ProxyIp.StartsWith("http") && !string.IsNullOrWhiteSpace(config.ProxyAuthPassword))
			{
				// OpenQA.Selenium.Chrome.ChromeDriverExtensions is very convenient, but it only supports HTTP proxy
				// https://github.com/RDavydenko/OpenQA.Selenium.Chrome.ChromeDriverExtensions/blob/18a0fba0c89adfd75765398232f5ecbbed1e5644/OpenQA.Selenium.Chrome.ChromeDriverExtensions/ChromeOptionsExtensions.cs#L17
				opt.AddHttpProxy(config.ProxyIp, config.ProxyPort, config.ProxyAuthUserName, config.ProxyAuthPassword);
			}
			else
			{
				// https://stackoverflow.com/a/24237188
				var addr = config.ProxyIp + ':' + config.ProxyPort;
				opt.Proxy = new Proxy()
				{
					HttpProxy = addr,
					FtpProxy = addr,
					SslProxy = addr,
					SocksProxy = addr,
					Kind = ProxyKind.Manual,
					IsAutoDetect = false,
					SocksUserName = config.ProxyAuthUserName,
					SocksPassword = config.ProxyAuthPassword
				};
			}
		}

		if (config.Arguments != null)
			opt.AddArguments(config.Arguments.ToArray());
		if (config.ExcludedArguments != null)
			opt.AddExcludedArguments(config.ExcludedArguments.ToArray());
		if (config.EncodedExtensions != null)
			opt.AddEncodedExtensions(config.EncodedExtensions.ToArray());

		LibLogger.Debug<SeleniumBrowser>("communicatorJs + mainHelperJs name mapping: {nameRandom}", nameRandom);

		driver = UndetectedChromeDriver.Create(opt, ReplaceCd(config.UserDataDir), ReplaceCd(config.DriverExecutable));
		try
		{
			mainHwnd = GetHwnd(driver);
		}
		catch (Exception ex)
		{
			LibLogger.Error<SeleniumBrowser>(ex, "Failed to get HWND of the Chrome window.");
		}
		driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", new Dictionary<string, object>()
		{
			["source"] = nameRandom.ApplyTo(LibResources.namespaceInitJs + ';' + LibResources.mainHelperJs + ';' + SeleniumResources.communicatorJs)
		});
		LibLogger.Verbose<SeleniumBrowser>("Injected pre-load scripts.");
		driver.Url = config.MainPage;
		PageLoaded?.Invoke(this, new PageLoadedEventArgs(driver.Url));
	}

	// Because of directly setting 'LocalWebSocketServer.MessageReceived += WebSocketMessage;' fails to handle event handler addition/removal
	private void OnWebSocketMessage(object? sender, WebSocketMessageEventArgs args) => WebSocketMessage?.Invoke(sender, args);

	/// <summary>
	/// https://stackoverflow.com/a/58924521
	/// </summary>
	/// <returns>Free port number</returns>
	public static int FindFreePort()
	{
		using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		var localEP = new IPEndPoint(IPAddress.Any, 0);
		socket.Bind(localEP);
		localEP = (IPEndPoint)socket.LocalEndPoint!;
		return localEP.Port;
	}

	public override Task<object?> EvaluateJavaScriptRawAsync(string script)
	{
		var result = driver.ExecuteScript("return " + script); // EVERTHING is IIFE(Immediately Invoked Function Expressions) in WebDriver >:(
		return Task.FromResult<object?>(result);
	}

	public override void ExecuteJavaScript(string script, string? errorMessage = null)
	{
		try
		{
			driver.ExecuteScript(script);
		}
		catch (Exception ex)
		{
			LibLogger.Error<SeleniumBrowser>(ex, errorMessage ?? "JavaScript execution error");
		}
	}

	public override void Load(string url)
	{
		driver.Url = url;
		PageLoaded?.Invoke(this, new PageLoadedEventArgs(driver.Url));
	}

	public override void ShowDevTools()
	{
		// unsupported in WebDriver
	}

	public IWebElement? FindElementQuery(string cssSelector)
	{
		try
		{
			return driver.FindElement(By.CssSelector(cssSelector));
		}
		catch (Exception ex) when (ex is NoSuchElementException or StaleElementReferenceException)
		{
			return null;
		}
		catch (UnhandledAlertException)
		{
			LibLogger.Warn<SeleniumBrowser>("Alert window detected");
			return null;
		}
	}

	public ReadOnlyCollection<IWebElement> FindElementsQuery(string cssSelector)
	{
		try
		{
			return driver.FindElements(By.CssSelector(cssSelector));
		}
		catch (Exception ex) when (ex is NoSuchElementException or StaleElementReferenceException)
		{
			return emptyWebElements;
		}
		catch (UnhandledAlertException)
		{
			LibLogger.Warn<SeleniumBrowser>("Alert window detected");
			return emptyWebElements;
		}
	}

	public IWebElement? FindElementClassName(string className)
	{
		try
		{
			return driver.FindElement(By.ClassName(className));
		}
		catch (Exception ex) when (ex is NoSuchElementException or StaleElementReferenceException)
		{
			return null;
		}
		catch (UnhandledAlertException)
		{
			LibLogger.Warn<SeleniumBrowser>("Alert window detected");
			return null;
		}
	}

	public ReadOnlyCollection<IWebElement> FindElementsClassName(string className)
	{
		try
		{
			return driver.FindElements(By.ClassName(className));
		}
		catch (Exception ex) when (ex is NoSuchElementException or StaleElementReferenceException)
		{
			return emptyWebElements;
		}
		catch (UnhandledAlertException)
		{
			LibLogger.Warn<SeleniumBrowser>("Alert window detected");
			return emptyWebElements;
		}
	}

	public IWebElement? FindElementId(string id)
	{
		try
		{
			return driver.FindElement(By.Id(id));
		}
		catch (Exception ex) when (ex is NoSuchElementException or StaleElementReferenceException)
		{
			return null;
		}
		catch (UnhandledAlertException)
		{
			LibLogger.Warn<SeleniumBrowser>("Alert window detected");
			return null;
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				WsServer?.Dispose();
				driver?.Dispose();
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public override IntPtr GetWindowHandle() => mainHwnd;

	private static IntPtr GetHwnd(UndetectedChromeDriver driver)
	{
		var field = Array.Find(typeof(UndetectedChromeDriver).GetFields(BindingFlags.NonPublic | BindingFlags.Instance), f => f.FieldType == typeof(Process) || f.FieldType == typeof(Nullable) && f.FieldType.GetGenericArguments()[0] == typeof(Process));
		if (field == null)
			throw new MissingFieldException("Browser process field is not available.");

		var val = field.GetValue(driver);
		if (val == null)
			throw new FieldAccessException("Browser process field is not set yet.");
		var process = (Process)val;
		LibLogger.Debug<SeleniumBrowser>("Got browser process: module={module} pid={pid} handle={handle}", process.MainModule?.FileName, process.Handle, process.MainWindowHandle);
		return process.MainWindowHandle;
	}
}
