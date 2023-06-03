using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketListener;
using AutoKkutuLib.Game.WsHandlers;
using AutoKkutuLib.Handlers.JavaScript;

namespace AutoKkutuLib.Handlers.JavaScript;
public class WsSniffingHandlerList : IWsSniffingHandlerList
{
	private readonly ISet<WsSniffingHandlerBase> RegisteredHandlers = new HashSet<WsSniffingHandlerBase>();

	public void InitDefaultHandlers(BrowserBase browser)
	{
		RegisterHandler(new WsSniffingHandlerJJoriping(browser));
	}

	public void RegisterHandler(WsSniffingHandlerBase handler) => RegisteredHandlers.Add(handler);

	public WsSniffingHandlerBase? GetByUri(Uri uri)
	{
		return (from handler in RegisteredHandlers
				where handler.UrlPattern.Any(baseUri => baseUri.IsBaseOf(uri))
				select handler).FirstOrDefault();
	}
}
