namespace AutoKkutuLib.Handlers;
public class JavaScriptHandlerList : IHandlerList
{
	private readonly ISet<HandlerBase> RegisteredHandlers = new HashSet<HandlerBase>();

	public void InitDefaultHandlers(BrowserBase browser)
	{
		RegisterHandler(new BasicHandler(browser));
		RegisterHandler(new SimpleBypassHandler(browser));
		RegisterHandler(new OptimizedBypassHandler(browser));
	}

	public void RegisterHandler(HandlerBase handler) => RegisteredHandlers.Add(handler);

	public HandlerBase? GetByUri(Uri uri)
	{
		return (from handler in RegisteredHandlers
				where handler.UrlPattern.Any(baseUri => baseUri.IsBaseOf(uri))
				select handler).FirstOrDefault();
	}
}
