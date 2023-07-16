using AutoKkutuLib.Game.DomHandlers;
using System.Collections.Immutable;

namespace AutoKkutuGui.Enterer;

public interface IDomHandlerProvider
{
	IImmutableList<IDomHandler> GetDomHandlers();
}
