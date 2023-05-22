using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;

namespace AutoKkutuLib.Selenium;
public class SeleniumBrowser : BrowserBase
{
	private WebDriver driver;

	public override void LoadFrontPage()
	{
		driver = new ChromeDriver();
		driver.Url = "https://kkutu.pink/";
		PageLoaded?.Invoke(this, new PageLoadedEventArgs(driver.Url));
	}

	public override object? BrowserControl => null;

	public override Task<JavaScriptCallback> EvaluateJavaScriptAsync(string script) => Task.FromResult(new JavaScriptCallback("", true, driver.ExecuteScript(script)));
	public override void ExecuteJavaScript(string script, string? errorMessage = null)
	{
		try
		{
			driver.ExecuteAsyncScript(script);
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
			Log.Warning("Alert window detected. Please close it.");
			return null;
		}
	}

	public IReadOnlyCollection<IWebElement> FindElementsQuery(string cssSelector)
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
			Log.Warning("Alert window detected. Please close it.");
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
			Log.Warning("Alert window detected. Please close it.");
			return null;
		}
	}

	public IReadOnlyCollection<IWebElement> FindElementsClassName(string className)
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
			Log.Warning("Alert window detected. Please close it.");
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
			Log.Warning("Alert window detected. Please close it.");
			return null;
		}
	}

}
