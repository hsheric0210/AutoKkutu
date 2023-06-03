using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Selenium;

namespace AutoKkutuLib.Handlers.WebDriver;
public class WebDriverHandlerList
{
	private readonly ISet<DomHandlerBase> RegisteredHandlers = new HashSet<DomHandlerBase>();

	public void InitDefaultHandlers(SeleniumBrowser browser)
	{
		RegisterHandler(new BasicHandler(browser));
		RegisterHandler(new SimpleBypassHandler(browser));
	}

	public void RegisterHandler(DomHandlerBase handler) => RegisteredHandlers.Add(handler);

	public DomHandlerBase? GetByUri(Uri uri)
	{
		return (from handler in RegisteredHandlers
				where handler.UrlPattern.Any(baseUri => baseUri.IsBaseOf(uri))
				select handler).FirstOrDefault();
	}
}
