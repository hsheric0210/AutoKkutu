using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;

namespace AutoKkutuLib.Handlers.JavaScript;
public class JavaScriptHandlerList : IDomHandlerList
{
	private readonly ISet<DomHandlerBase> RegisteredHandlers = new HashSet<DomHandlerBase>();

	public void InitDefaultHandlers(BrowserBase browser)
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
