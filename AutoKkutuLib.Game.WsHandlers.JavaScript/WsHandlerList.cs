using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.WebSocketListener;
using AutoKkutuLib.Game.WsHandlers;

namespace AutoKkutuLib.Handlers.JavaScript;
public class WsHandlerList : IWsHandlerList
{
	private readonly ISet<WsHandlerBase> RegisteredHandlers = new HashSet<WsHandlerBase>();

	public void InitDefaultHandlers(BrowserBase browser)
	{
		RegisterHandler(new WsHandlerJJoriping(browser));
	}

	public void RegisterHandler(WsHandlerBase handler) => RegisteredHandlers.Add(handler);

	public WsHandlerBase? GetByUri(Uri uri)
	{
		return (from handler in RegisteredHandlers
				where handler.UrlPattern.Any(baseUri => baseUri.IsBaseOf(uri))
				select handler).FirstOrDefault();
	}
}
