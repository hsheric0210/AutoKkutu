using AutoKkutuLib.Game.WebSocketHandlers;
using System;
using System.Collections.Immutable;

namespace AutoKkutuGui.Enterer;
public class WebSocketHandlerManager
{
	private readonly IImmutableDictionary<string, IWebSocketHandler> handlers;

	public WebSocketHandlerManager(IImmutableList<IWebSocketHandlerProvider> providers)
	{
		if (providers == null)
			throw new ArgumentNullException(nameof(providers));

		var builder = ImmutableDictionary.CreateBuilder<string, IWebSocketHandler>();
		foreach (var provider in providers)
		{
			foreach (var handler in provider.GetWebSocketHandlers())
				builder.Add(handler.HandlerName, handler);
		}
		handlers = builder.ToImmutable();
	}

	public IWebSocketHandler? GetHandler(string name) => handlers.GetValueOrDefault(name);

	public bool TryGetHandler(string name, out IWebSocketHandler? handler) => handlers.TryGetValue(name, out handler);
}
