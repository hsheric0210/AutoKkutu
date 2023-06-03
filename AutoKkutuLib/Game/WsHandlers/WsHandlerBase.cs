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
	public abstract string MessageType_TurnStart { get; }
	public abstract string MessageType_TurnEnd { get; }
	public abstract string MessageType_TurnError { get; }

	protected WsHandlerBase(BrowserBase browser) => Browser = browser;

	public abstract WsWelcome ParseWelcome(JsonNode json);
	public abstract WsRoom ParseRoom(JsonNode json);
	public abstract WsClassicTurnStart ParseClassicTurnStart(JsonNode json);

	// TODO: 내 바로 이전 사람 턴이 끝남을 감지하고, 그 사람이 입력한 단어에서 내가 입력해야 할 단어의 조건 노드 파싱하기 (두음법칙 적용해서)
	// TODO: 'BFKKUTU'의 '두음법칙 무시' 조건 감지 - RoomHead에 '두음법칙 무시' 단어가 있는지 DOM 파싱해서 확인
	public abstract WsClassicTurnEnd ParseClassicTurnEnd(JsonNode json);
	public abstract WsTurnError ParseClassicTurnError(JsonNode json);

	public virtual async Task RegisterInGameFunctions(ISet<int> alreadyRegistered) { }
}