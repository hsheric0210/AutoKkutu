using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Handlers.JavaScript;

namespace AutoKkutuLib.Handlers.JavaScript;
public class JavaScriptHandlerList : IWsSniffingHandlerList
{
	private readonly ISet<DomHandlerBase> RegisteredHandlers = new HashSet<DomHandlerBase>();

	public void InitDefaultHandlers(BrowserBase browser)
	{
		RegisterHandler(new BasicHandler(browser));
		RegisterHandler(new SimpleBypassHandler(browser));
		RegisterHandler(new OptimizedBypassHandler(browser));
	}

	public void RegisterHandler(DomHandlerBase handler) => RegisteredHandlers.Add(handler);

	public DomHandlerBase? GetByUri(Uri uri)
	{
		return (from handler in RegisteredHandlers
				where handler.UrlPattern.Any(baseUri => baseUri.IsBaseOf(uri))
				select handler).FirstOrDefault();
	}
}
