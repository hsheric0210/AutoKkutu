using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;
using System.Collections.ObjectModel;
using SeleniumUndetectedChromeDriver;
using System.Xml.Serialization;
using AutoKkutuLib.Properties;
using AutoKkutuLib.Selenium.Properties;
using System.Net;
using System.Net.Sockets;
using AutoKkutuLib.Browser;
using OpenQA.Selenium.DevTools.V113;
using OpenQA.Selenium.DevTools.V113.Page;
using AutoKkutuLib.Game;

namespace AutoKkutuLib.Selenium;
public class SeleniumBrowser : BrowserBase, IDisposable
{
	private readonly static ReadOnlyCollection<IWebElement> emptyWebElements = new(new List<IWebElement>());
	private const string ConfigFile = "Selenium.xml";

	private readonly BrowserRandomNameMapping nameRandom;

	private UndetectedChromeDriver driver = null!;
	private IDisposable? WsServer;
	private SeleniumConfigDto config;
	private bool disposedValue;

	public override object? BrowserControl => null;

	public override string JavaScriptBaseNamespace { get; }

	public SeleniumBrowser()
	{
		var wsPort = FindFreePort();
		var wsAddrClient = "ws://" + new IPEndPoint(IPAddress.Loopback, wsPort).ToString();
		LocalWebSocketServer.MessageReceived += OnWebSocketMessage;
		WsServer = LocalWebSocketServer.Start(wsPort);
		Log.Information("Browser-side event listener WebSocket will connect to {addr}", wsAddrClient);

		var serializer = new XmlSerializer(typeof(SeleniumConfigDto));

		// Default config
		config = new SeleniumConfigDto()
		{
			MainPage = "https://kkutu.pink/",
			DriverExecutable = "chromedriver.exe"
		};

		if (File.Exists(ConfigFile))
		{
			try
			{
				using FileStream stream = File.Open(ConfigFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
				config = (SeleniumConfigDto?)serializer.Deserialize(stream)!;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to read Selenium config.");
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
				Log.Error(ex, "Failed to write default Selenium config.");
			}
		}

		JavaScriptBaseNamespace = config.JavaScriptInjectionBaseNamespace;
		if (string.IsNullOrWhiteSpace(JavaScriptBaseNamespace))
			JavaScriptBaseNamespace = "window";

		nameRandom = BrowserRandomNameMapping.MainHelperJs(this);
		nameRandom.GenerateScriptType("___wsGlobal___", 16383);
		nameRandom.GenerateScriptType("___wsBuffer___", 16384);
		nameRandom.Add("___wsAddr___", wsAddrClient);
	}

	public override async void LoadFrontPage()
	{
		var opt = new ChromeOptions()
		{
			BinaryLocation = config.BinaryLocation,
			LeaveBrowserRunning = config.LeaveBrowserRunning,
			DebuggerAddress = config.DebuggerAddress,
			MinidumpPath = config.MinidumpPath
		};

		if (config.Arguments != null)
			opt.AddArguments(config.Arguments.ToArray());
		if (config.ExcludedArguments != null)
			opt.AddExcludedArguments(config.ExcludedArguments.ToArray());
		if (config.EncodedExtensions != null)
			opt.AddEncodedExtensions(config.EncodedExtensions.ToArray());

		Log.Debug("communicatorJs + mainHelperJs name mapping: {nameRandom}", nameRandom);

		driver = UndetectedChromeDriver.Create(opt, config.UserDataDir, config.DriverExecutable, config.BrowserExecutable);
		driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", new Dictionary<string, object>()
		{
			["source"] = nameRandom.ApplyTo(LibResources.namespaceInitJs + ';' + LibResources.mainHelperJs + ';' + SeleniumResources.communicatorJs)
		});
		Log.Verbose("Injected pre-load scripts.");
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
			Log.Error(ex, errorMessage ?? "JavaScript execution error");
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
			Log.Warning("Alert window detected");
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
			Log.Warning("Alert window detected");
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
			Log.Warning("Alert window detected");
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
			Log.Warning("Alert window detected");
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
			Log.Warning("Alert window detected");
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
}
