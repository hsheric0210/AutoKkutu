using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;
using System.Collections.ObjectModel;
using SeleniumUndetectedChromeDriver;
using System.Xml.Serialization;
using System.Diagnostics.Contracts;

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

		driver = UndetectedChromeDriver.Create(opt, driverExecutablePath: "chromedriver.exe");
		driver.Url = config.MainPage;
		PageLoaded?.Invoke(this, new PageLoadedEventArgs(driver.Url));
	}

	public override object? BrowserControl => null;

	public override Task<JavaScriptCallback> EvaluateJavaScriptAsync(string script) => Task.FromResult(new JavaScriptCallback("", true, driver.ExecuteScript(script)));
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
