using System.Text.Json.Nodes;

namespace AutoKkutuLib.Game.WebSocketHandlers;
public interface IWebSocketHandler
{
	string HandlerName { get; }
	string HandlerDetails { get; }
	string MessageType_Room { get; }
	string MessageType_RoundReady { get; }
	string MessageType_TurnEnd { get; }
	string MessageType_TurnError { get; }
	string MessageType_TurnStart { get; }
	string MessageType_Welcome { get; }

	void OnWebSocketMessage(JsonNode json);
	ValueTask<WsClassicTurnEnd> ParseClassicTurnEnd(JsonNode json);
	ValueTask<WsClassicTurnError> ParseClassicTurnError(JsonNode json);
	ValueTask<WsClassicTurnStart> ParseClassicTurnStart(JsonNode json);
	ValueTask<WsRoom> ParseRoom(JsonNode json);
	ValueTask<WsTypingBattleRoundReady> ParseTypingBattleRoundReady(JsonNode json);
	ValueTask<WsTypingBattleTurnEnd> ParseTypingBattleTurnEnd(JsonNode json);
	ValueTask<WsTypingBattleTurnStart> ParseTypingBattleTurnStart(JsonNode json);
	ValueTask<WsWelcome> ParseWelcome(JsonNode json);
	ValueTask<WsHunminRoundReady> ParseHunminRoundReady(JsonNode json);
	ValueTask<WsHunminTurnStart> ParseHunminTurnStart(JsonNode json);
	ValueTask RegisterInGameFunctions(ISet<int> alreadyRegistered);
	ValueTask RegisterWebSocketFilter();
}