using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;

namespace AutoKkutuLib.Selenium;
public class SeleniumBrowserBase : BrowserBase
{
	private WebDriver driver;

	public SeleniumBrowserBase()
	{
		driver = new ChromeDriver();
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

	public override void Load(string url) => driver.Url = url;

	public override void ShowDevTools()
	{
		// unsupported in WebDriver
	}

	public IWebElement? FindElementQuery(string cssSelector, bool notify = true)
	{
		try
		{
			return driver.FindElement(By.CssSelector(cssSelector));
		}
		catch (NoSuchElementException ex)
		{
			if (notify)
				throw new NoSuchElementException($"Element matching CSS selector query {cssSelector} doesn't exist! Report this error to the developer.", ex);
			return null;
		}
	}

	public IReadOnlyCollection<IWebElement> FindElementsQuery(string cssSelector) => driver.FindElements(By.CssSelector(cssSelector));

	public IWebElement? FindElementClassName(string className, bool notify = true)
	{
		try
		{
			return driver.FindElement(By.ClassName(className));
		}
		catch (NoSuchElementException ex)
		{
			if (notify)
				throw new NoSuchElementException($"Element with class name {className} doesn't exist! Report this error to the developer.", ex);
			return null;
		}
	}

	public IReadOnlyCollection<IWebElement> FindElementsClassName(string className) => driver.FindElements(By.ClassName(className));


	public IWebElement? FindElementId(string id, bool notify = true)
	{
		try
		{
			return driver.FindElement(By.Id(id));
		}
		catch (NoSuchElementException ex)
		{
			if (notify)
				throw new NoSuchElementException($"Element with id {id} doesn't exist! Report this error to the developer.", ex);
			return null;
		}
	}

}
