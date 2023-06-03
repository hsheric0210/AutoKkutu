using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.WebSocketListener;

namespace AutoKkutuLib.Game.WsHandlers;

public interface IWsHandlerList
{
	WsHandlerBase? GetByUri(Uri uri);
	void InitDefaultHandlers(BrowserBase browser);
	void RegisterHandler(WsHandlerBase handler);
}