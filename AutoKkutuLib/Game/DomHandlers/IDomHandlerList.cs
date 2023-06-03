using AutoKkutuLib.Browser;

namespace AutoKkutuLib.Game.DomHandlers;

public interface IDomHandlerList
{
	DomHandlerBase? GetByUri(Uri uri);
	void InitDefaultHandlers(BrowserBase browser);
	void RegisterHandler(DomHandlerBase handler);
}