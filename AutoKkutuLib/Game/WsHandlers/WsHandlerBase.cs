using AutoKkutuLib.Browser;
using System.Text.Json.Nodes;

namespace AutoKkutuLib.Game.WebSocketListener;

/// <summary>
/// '클래식' 모드(끝말잇기, 가운뎃말잇기, 앞말잇기 등)에 대한 쪼리핑의 원 통신 프로토콜에 따른 메세지를 파싱하는 클래스입니다.
/// 만약 특정 사이트가 이와는 다른 프로토콜을 사용한다면, 이 클래스를 Override하여 속성이나 함수를 수정해 주세요.
/// 쪼리핑의 '클래식' 모드 구현체 프로토콜 구현체: https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Web/lib/kkutu/rule_classic.js
/// </summary>
public abstract class WsHandlerBase
{
	protected BrowserBase Browser { get; }

	public abstract string HandlerName { get; }
	public abstract IReadOnlyCollection<Uri> UrlPattern { get; }

	public abstract string MessageType_Welcome { get; }
	public abstract string MessageType_Room { get; }
	public abstract string MessageType_RoundReady { get; }
	public abstract string MessageType_TurnStart { get; }
	public abstract string MessageType_TurnEnd { get; }
	public abstract string MessageType_TurnError { get; }

	protected WsHandlerBase(BrowserBase browser) => Browser = browser;

	public abstract Task RegisterWebSocketFilter();
	public virtual Task RegisterInGameFunctions(ISet<int> alreadyRegistered) => Task.CompletedTask;
	public virtual bool OnWebSocketMessage(JsonNode json) => false;

	public abstract Task<WsWelcome> ParseWelcome(JsonNode json);
	public abstract Task<WsRoom> ParseRoom(JsonNode json);
	public abstract Task<WsClassicTurnStart> ParseClassicTurnStart(JsonNode json);

	public abstract Task<WsClassicTurnEnd> ParseClassicTurnEnd(JsonNode json);
	public abstract Task<WsTurnError> ParseClassicTurnError(JsonNode json);

	public abstract Task<WsTypingBattleRoundReady> ParseTypingBattleRoundReady(JsonNode json);
	public abstract Task<WsTypingBattleTurnStart> ParseTypingBattleTurnStart(JsonNode json);
	public abstract Task<WsTypingBattleTurnEnd> ParseTypingBattleTurnEnd(JsonNode json);
}