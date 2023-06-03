using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.WebSocketListener;

namespace AutoKkutuLib.Game.WsHandlers;

public interface IWsSniffingHandlerList
{
	WsSniffingHandlerBase? GetByUri(Uri uri);
	void InitDefaultHandlers(BrowserBase browser);
	void RegisterHandler(WsSniffingHandlerBase handler);
}