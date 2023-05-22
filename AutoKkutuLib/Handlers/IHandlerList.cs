namespace AutoKkutuLib.Handlers;

public interface IHandlerList
{
	HandlerBase? GetByUri(Uri uri);
	void InitDefaultHandlers(BrowserBase browser);
	void RegisterHandler(HandlerBase handler);
}