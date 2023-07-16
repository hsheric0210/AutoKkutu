using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using System.Collections.Immutable;

namespace AutoKkutuGui.Enterer;

public class DefaultDomHandlerProvider : IDomHandlerProvider
{
	private readonly IImmutableList<IDomHandler> handlers;

	public DefaultDomHandlerProvider(BrowserBase browser)
	{
		var builder = ImmutableList.CreateBuilder<IDomHandler>();
		builder.Add(new BasicDomHandler(browser));
		builder.Add(new BasicBypassDomHandler(browser));
		handlers = builder.ToImmutable();
	}

	public IImmutableList<IDomHandler> GetDomHandlers() => handlers;
}
