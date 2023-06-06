using AutoKkutuLib.Browser;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace AutoKkutuLib.Game.WebSocketListener;

// TODO: Move to specialized class library project 'AutoKkutuLib.WsHandlers.JavaScript'
/// <summary>
/// '클래식' 모드(끝말잇기, 가운뎃말잇기, 앞말잇기 등)에 대한 쪼리핑의 원 통신 프로토콜에 따른 메세지를 파싱하는 클래스입니다.
/// 만약 특정 사이트가 이와는 다른 프로토콜을 사용한다면, 이 클래스를 Override하여 속성이나 함수를 수정해 주세요.
/// 쪼리핑의 '클래식' 모드 구현체 프로토콜 구현체: https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Web/lib/kkutu/rule_classic.js
/// </summary>
public class WsHandlerJJoriping : WsHandlerBase
{
	public override string HandlerName => "JJoriping-compatible";
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] {
		new Uri("https://kkutu.pink/"),
		new Uri("https://musickkutu.xyz/"),
		new Uri("https://kkutu.org/"),
		new Uri("https://kkutu.co.kr/"),
		new Uri("https://kkutu.io/"),
	};

	public override string MessageType_Welcome => "welcome";
	public override string MessageType_Room => "room";
	public override string MessageType_TurnStart => "turnStart";
	public override string MessageType_TurnEnd => "turnEnd";
	public override string MessageType_TurnError => "turnError";

	public WsHandlerJJoriping(BrowserBase browser) : base(browser) { }

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
		=> await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.RoomModeToGameMode, "id", "return Object.keys(JSON.parse(document.getElementById('RULE').textContent))[id]");

	public override WsWelcome ParseWelcome(JsonNode json) => new(json["id"]?.GetValue<string>());

	private static string? ParseIntOrString(JsonNode? node)
	{
		try
		{
			return node?.GetValue<int>().ToString();
		}
		catch
		{
			return node?.GetValue<string>();
		}
	}

	private string ParsePlayer(JsonNode? node) => (node is JsonValue ? node?.GetValue<string>() : ParseIntOrString(node?["id"])) ?? "";

	public override WsRoom ParseRoom(JsonNode json)
	{
		JsonObject room = (json["room"] ?? throw InvalidWsMessage("room", "room")).AsObject();
		var players = (room["players"] ?? throw InvalidWsMessage("room", "room.players")).AsArray().Select(ParsePlayer).ToImmutableList();
		var gaming = (room["gaming"] ?? throw InvalidWsMessage("room", "room.gaming")).GetValue<bool>();
		var modeId = (room["mode"] ?? throw InvalidWsMessage("room", "room.mode")).GetValue<int>();

		JsonObject game = (room["game"] ?? throw InvalidWsMessage("room", "room.game")).AsObject();
		var gameSeq = (game["seq"] ?? throw InvalidWsMessage("room", "room.game.seq")).AsArray().Select(ParsePlayer).ToImmutableList();

		var modeString = Browser.EvaluateJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.RoomModeToGameMode, false)}({modeId})", errorPrefix: "ParseRoom");
		var mode = modeString switch
		{
			"ESH" or "KSH" => GameMode.LastAndFirst,
			"KGT" => GameMode.MiddleAndFirst,
			"EAP" or "KAP" => GameMode.FirstAndLast,
			"EKT" or "KMT" => GameMode.Kkutu,
			"KKT" => GameMode.KungKungTta,
			"EAW" or "KAW" => GameMode.Free,
			"EJH" or "KJH" => GameMode.LastAndFirstFree,
			"ETY" or "KTY" => GameMode.TypingBattle,
			"KAD" or "EAD" => GameMode.All,
			_ => GameMode.None
		};
		return new WsRoom(modeString, mode, players, gaming, gameSeq);
	}

	public override WsClassicTurnStart ParseClassicTurnStart(JsonNode json)
	{
		return new WsClassicTurnStart(
			json["turn"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "turn"),
			json["roundTime"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "roundTime"),
			json["turnTime"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "turnTime"),
			new WordCondition(
				json["char"]?.GetValue<string>() ?? "",
				json["subChar"]?.GetValue<string>(),
				json["mission"]?.GetValue<string>()));
	}

	// TODO: 내 바로 이전 사람 턴이 끝남을 감지하고, 그 사람이 입력한 단어에서 내가 입력해야 할 단어의 조건 노드 파싱하기 (두음법칙 적용해서)
	// TODO: 'BFKKUTU'의 '두음법칙 무시' 조건 감지 - RoomHead에 '두음법칙 무시' 단어가 있는지 DOM 파싱해서 확인
	public override WsClassicTurnEnd ParseClassicTurnEnd(JsonNode json)
	{
		return new(
			json["ok"]?.GetValue<bool>() ?? throw InvalidWsMessage("turnEnd", "ok"),
			ParseIntOrString(json["target"]) ?? throw InvalidWsMessage("turnEnd", "target"),
			json["value"]?.GetValue<string>(),
			json["hint"]?["_id"]?.GetValue<string>());
	}

	public override WsTurnError ParseClassicTurnError(JsonNode json)
	{
		return new(
			(TurnErrorCode?)json["code"]?.GetValue<int>() ?? throw InvalidWsMessage("TurnError", "code"),
			json["value"]?.GetValue<string>());
	}

	private static Exception InvalidWsMessage(string messageType, string expectedAttribute)
		=> new FormatException($"'{messageType}' message without '{expectedAttribute}' attribute");
}