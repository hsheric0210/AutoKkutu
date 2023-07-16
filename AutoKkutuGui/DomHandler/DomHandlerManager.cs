using AutoKkutuLib.Game.DomHandlers;
using System;
using System.Collections.Immutable;

namespace AutoKkutuGui.Enterer;
public class DomHandlerManager
{
	private readonly IImmutableDictionary<string, IDomHandler> handlers;

	public DomHandlerManager(IImmutableList<IDomHandlerProvider> providers)
	{
		if (providers == null)
			throw new ArgumentNullException(nameof(providers));

		var builder = ImmutableDictionary.CreateBuilder<string, IDomHandler>();
		foreach (var provider in providers)
		{
			foreach (var handler in provider.GetDomHandlers())
				builder.Add(handler.HandlerName, handler);
		}
		handlers = builder.ToImmutable();
	}

	public IDomHandler? GetHandler(string name) => handlers.GetValueOrDefault(name);

	public bool TryGetHandler(string name, out IDomHandler? handler) => handlers.TryGetValue(name, out handler);
}
