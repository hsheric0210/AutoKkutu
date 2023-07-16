using AutoKkutuLib.Game.Enterer;
using System.Collections.Immutable;

namespace AutoKkutuGui.Enterer;

public class DefaultEntererProvider : IEntererProvider
{
	private readonly IImmutableList<EntererSupplier> enterers;

	public DefaultEntererProvider()
	{
		var builder = ImmutableList.CreateBuilder<EntererSupplier>();
		builder.Add(game => new DelayedInstantEnterer(game));
		builder.Add(game => new JavaScriptInputSimulator(game));
		builder.Add(game => new Win32InputSimulator(game));
		enterers = builder.ToImmutable();
	}

	public IImmutableList<EntererSupplier> GetEntererSuppliers() => enterers;
}
