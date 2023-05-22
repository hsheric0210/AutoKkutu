using AutoKkutuLib.Selenium;

namespace AutoKkutuLib.Handlers.WebDriver.Handlers;
public class WebDriverHandlerList
{
	private readonly ISet<HandlerBase> RegisteredHandlers = new HashSet<HandlerBase>();

	public void InitDefaultHandlers(SeleniumBrowserBase browser)
	{
		RegisterHandler(new BasicHandler(browser));
		RegisterHandler(new SimpleBypassHandler(browser));
	}

	public void RegisterHandler(HandlerBase handler) => RegisteredHandlers.Add(handler);

	public HandlerBase? GetByUri(Uri uri)
	{
		return (from handler in RegisteredHandlers
				where handler.UrlPattern.Any(baseUri => baseUri.IsBaseOf(uri))
				select handler).FirstOrDefault();
	}
}
