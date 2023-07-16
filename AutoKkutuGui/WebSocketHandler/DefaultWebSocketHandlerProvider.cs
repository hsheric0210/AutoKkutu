using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.WebSocketHandlers;
using System.Collections.Immutable;

namespace AutoKkutuGui.Enterer;

public class DefaultWebSocketHandlerProvider : IWebSocketHandlerProvider
{
	private readonly IImmutableList<IWebSocketHandler> handlers;

	public DefaultWebSocketHandlerProvider(BrowserBase browser)
	{
		var builder = ImmutableList.CreateBuilder<IWebSocketHandler>();
		builder.Add(new BasicWebSocketHandler(browser));
		builder.Add(new RioDecodeWebSocketHandler(browser));
		handlers = builder.ToImmutable();
	}

	public IImmutableList<IWebSocketHandler> GetWebSocketHandlers() => handlers;
}
