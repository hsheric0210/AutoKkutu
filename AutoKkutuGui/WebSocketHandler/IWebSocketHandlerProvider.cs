using AutoKkutuLib.Game.WebSocketHandlers;
using System.Collections.Immutable;

namespace AutoKkutuGui.Enterer;

public interface IWebSocketHandlerProvider
{
	IImmutableList<IWebSocketHandler> GetWebSocketHandlers();
}
