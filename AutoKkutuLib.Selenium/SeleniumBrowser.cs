using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;
using System.Collections.ObjectModel;
using SeleniumUndetectedChromeDriver;
using System.Xml.Serialization;
using System.Diagnostics.Contracts;
using AutoKkutuLib.Properties;
using AutoKkutuLib.Selenium.Properties;
using System.Net;
using System.Net.Sockets;

namespace AutoKkutuLib.Selenium;
public class SeleniumBrowser : BrowserBase
{
	private const string ConfigFile = "Selenium.xml";
	private UndetectedChromeDriver driver;

	public override async void LoadFrontPage()
	{
		var serializer = new XmlSerializer(typeof(SeleniumConfigDto));

		// Default config
		var config = new SeleniumConfigDto()
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

		var wsHook = "Vue" + Random.Shared.NextInt64();
		var wsOriginal = "Vue" + Random.Shared.NextInt64();
		var wsVar = "Vue" + Random.Shared.NextInt64();
		var wsPort = FindFreePort();
		var wsAddrClient = "ws://" + new IPEndPoint(IPAddress.Loopback, wsPort).ToString();
		var wsAddrServer = "ws://" + new IPEndPoint(IPAddress.Any, wsPort).ToString();
		var wsServer = new EventListenerWebSocket(wsAddrServer);
		wsServer.OnReceive += WebSocketMessage;

		Log.Information("wsHook func: {wsHook}", wsHook);

		driver = UndetectedChromeDriver.Create(opt, config.UserDataDir, config.DriverExecutable, config.BrowserExecutable);
		driver.Url = config.MainPage;

		Log.Information("Browser-side event listener WebSocket will connect to {addr}", wsAddrClient);
		driver.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", new Dictionary<string, object>()
		{
			["source"] = (LibResources.wsHook + ';' + SeleniumResources.wsListener)
				.Replace("___wsHook___", wsHook)
				.Replace("___originalWS___", wsOriginal)
				.Replace("___wsAddr___", wsAddrClient)
				.Replace("___wsVar___", wsVar)
		});
		PageLoaded?.Invoke(this, new PageLoadedEventArgs(driver.Url));
	}

	/// <summary>
	/// https://stackoverflow.com/a/58924521
	/// </summary>
	/// <returns>Free port number</returns>
	public static int FindFreePort()
	{
		var port = 0;
		var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		try
		{
			var localEP = new IPEndPoint(IPAddress.Any, 0);
			socket.Bind(localEP);
			localEP = (IPEndPoint)socket.LocalEndPoint!;
			port = localEP.Port;
		}
		finally
		{
			socket.Close();
		}
		return port;
	}

	public override object? BrowserControl => null;

	public override Task<JavaScriptCallback> EvaluateJavaScriptAsync(string script)
	{
		var result = driver.ExecuteScript("return " + script); // EVERTHING is IIFE(Immediately Invoked Function Expressions) in WebDriver >:(
		if (result == null)
			Log.Error("Result is null of {js}", script);
		return Task.FromResult(new JavaScriptCallback(result?.ToString() ?? "null", true, result));
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
			return null;
		}
		catch (UnhandledAlertException)
		{
			Log.Warning("Alert window detected");
			return null;
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
			return null;
		}
		catch (UnhandledAlertException)
		{
			Log.Warning("Alert window detected");
			return null;
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

}
