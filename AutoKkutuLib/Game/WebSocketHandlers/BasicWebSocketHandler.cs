using AutoKkutuLib.Browser;
using AutoKkutuLib.Properties;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace AutoKkutuLib.Game.WebSocketHandlers;

/// <summary>
/// '클래식' 모드(끝말잇기, 가운뎃말잇기, 앞말잇기 등)에 대한 쪼리핑의 원 통신 프로토콜에 따른 메세지를 파싱하는 클래스입니다.
/// 만약 특정 사이트가 이와는 다른 프로토콜을 사용한다면, 이 클래스를하여 속성이나 함수를 수정해 주세요.
/// 쪼리핑의 '클래식' 모드 구현체 프로토콜 구현체: https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Web/lib/kkutu/rule_classic.js
/// </summary>
public class BasicWebSocketHandler : IWebSocketHandler
{
	protected BrowserBase Browser { get; }

	public virtual string HandlerName => "BasicWebSocketHandler";
	public virtual string HandlerDetails => "JJorinping-compatible basic WebSocket handler";
	private readonly BrowserRandomNameMapping helperMapping;

	public virtual string MessageType_Welcome => "welcome";
	public virtual string MessageType_Room => "room";
	public virtual string MessageType_RoundReady => "roundReady";
	public virtual string MessageType_TurnStart => "turnStart";
	public virtual string MessageType_TurnEnd => "turnEnd";
	public virtual string MessageType_TurnError => "turnError";

	public BasicWebSocketHandler(BrowserBase browser)
	{
		Browser = browser;

		var helperNames = BrowserRandomNameMapping.BaseJs(browser);
		helperNames.GenerateScriptType("___roomMode2GameMode___", CommonNameRegistry.RoomModeToGameMode);
		helperNames.GenerateScriptType("___ruleKeys___", CommonNameRegistry.RuleKeys);
		helperNames.GenerateScriptType("___helperRegistered___", CommonNameRegistry.WebSocketHelperRegistered);
		LibLogger.Debug<BasicWebSocketHandler>("WebSocket Handler Helper name mapping: {nameRandom}", helperNames);
		helperMapping = helperNames;
	}

	public virtual async ValueTask RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		if (!await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.WebSocketHelperRegistered)))
		{
			var script = helperMapping.ApplyTo(LibResources.baseWebSocketHelperJs);
			LibLogger.Debug<BasicWebSocketHandler>("WebSocket Helper injection result: {result}", await Browser.EvaluateJavaScriptRawAsync(script));
		}
	}

	public virtual async ValueTask RegisterWebSocketFilter()
	{
		var wsFilter = Browser.GetScriptTypeName(CommonNameRegistry.WebSocketFilter);
		if (await Browser.EvaluateJavaScriptBoolAsync($"{wsFilter}.registered"))
			return;

		var mapping = BrowserRandomNameMapping.BaseJs(Browser);
		mapping.GenerateScriptType("___wsFilter___", CommonNameRegistry.WebSocketFilter);
		mapping.Add("___welcome___", MessageType_Welcome);
		mapping.Add("___turnStart___", MessageType_TurnStart);
		mapping.Add("___turnEnd___", MessageType_TurnEnd);
		mapping.Add("___turnError___", MessageType_TurnError);

		Browser.ExecuteJavaScript(mapping.ApplyTo(LibResources.baseWebSocketFilterJs), "WsFilter register");
	}

	public virtual async ValueTask<WsWelcome> ParseWelcome(JsonNode json)
	{
		var userId = json["id"]?.GetValue<string>() ?? throw InvalidWsMessage("welcome", "id");

		var mapping = BrowserRandomNameMapping.BaseJs(Browser);
		mapping.GenerateScriptType("___wsFilter___", CommonNameRegistry.WebSocketFilter);
		mapping.Add("___room___", MessageType_Room);
		mapping.Add("___userId___", userId);

		// IPC QUOTA OPTIMIZATION: only copy room.players, room.gaming, room.mode, room.game.seq
		Browser.ExecuteJavaScript(mapping.ApplyTo(LibResources.baseWebSocketRoomFilterJs), "WsFilter-room register");
		return new(userId);
	}

	public virtual async ValueTask<WsRoom> ParseRoom(JsonNode json)
	{
		var room = (json["room"] ?? throw InvalidWsMessage("room", "room")).AsObject();
		var players = (room["players"] ?? throw InvalidWsMessage("room", "room.players")).AsArray().Select(ParsePlayer).ToImmutableList();
		var gaming = (room["gaming"] ?? throw InvalidWsMessage("room", "room.gaming")).GetValue<bool>();
		var modeId = (room["mode"] ?? throw InvalidWsMessage("room", "room.mode")).GetValue<int>();

		var game = (room["game"] ?? throw InvalidWsMessage("room", "room.game")).AsObject();
		var gameSeq = (game["seq"] ?? throw InvalidWsMessage("room", "room.game.seq")).AsArray().Select(ParsePlayer).ToImmutableList();

		var modeString = await Browser.EvaluateJavaScriptAsync($"{Browser.GetScriptTypeName(CommonNameRegistry.RoomModeToGameMode)}({modeId})", errorPrefix: "ParseRoom");
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
			"KEA" => GameMode.All,
			"KAD" => GameMode.AllKorean,
			"EAD" => GameMode.AllEnglish,
			"HUN" => GameMode.Hunmin,
			_ => GameMode.None
		};
		return new(modeString, mode, players, gaming, gameSeq);
	}

	public virtual async ValueTask<WsClassicTurnStart> ParseClassicTurnStart(JsonNode json)
	{
		return new(
			json["turn"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "turn"),
			json["roundTime"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "roundTime"),
			json["turnTime"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "turnTime"),
			new WordCondition(
				json["char"]?.GetValue<string>() ?? "",
				json["subChar"]?.GetValue<string>() ?? "",
				json["mission"]?.GetValue<string>() ?? "",
				json["wordLength"]?.GetValue<int>() ?? 3));
	}

	// TODO: 'BFKKUTU'의 '두음법칙 무시' 조건 감지 - RoomHead에 '두음법칙 무시' 단어가 있는지 DOM 파싱해서 확인
	public virtual async ValueTask<WsClassicTurnEnd> ParseClassicTurnEnd(JsonNode json)
	{
		var hintObj = json["hint"];
		var hint = hintObj is JsonObject val ? val["_id"]?.GetValue<string>() : null;
		return new(
			json["ok"]?.GetValue<bool>() ?? throw InvalidWsMessage("turnEnd", "ok"),
			json["value"]?.GetValue<string>(),
			hint);
	}

	public virtual async ValueTask<WsClassicTurnError> ParseClassicTurnError(JsonNode json)
	{
		return new(
			(TurnErrorCode?)json["code"]?.GetValue<int>() ?? throw InvalidWsMessage("turnError", "code"),
			json["value"]?.GetValue<string>());
	}

	public virtual async ValueTask<WsTypingBattleRoundReady> ParseTypingBattleRoundReady(JsonNode json)
	{
		return new(json["round"]?.GetValue<int>() ?? throw InvalidWsMessage("TurnError", "round"),
			(json["list"] ?? throw InvalidWsMessage("TurnError", "list")).AsArray().Select(n => n!.GetValue<string>()).ToImmutableList());
	}

	public virtual async ValueTask<WsTypingBattleTurnStart> ParseTypingBattleTurnStart(JsonNode json) => new(json["roundTime"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "roundTime"));

	public virtual async ValueTask<WsTypingBattleTurnEnd> ParseTypingBattleTurnEnd(JsonNode json) => new(json["ok"]?.GetValue<bool>() ?? false);
	public virtual async ValueTask<WsHunminRoundReady> ParseHunminRoundReady(JsonNode json)
	{
		return new(
		json["round"]?.GetValue<int>() ?? throw InvalidWsMessage("roundReady", "round"),
		new WordCondition(
			json["theme"]?.GetValue<string>() ?? throw InvalidWsMessage("roundReady", "theme"),
			json["mission"]?.GetValue<string>() ?? ""));
	}

	public virtual async ValueTask<WsHunminTurnStart> ParseHunminTurnStart(JsonNode json)
	{
		return new(
			json["turn"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "turn"),
			json["roundTime"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "roundTime"),
			json["turnTime"]?.GetValue<int>() ?? throw InvalidWsMessage("turnStart", "turnTime"),
			json["mission"]?.GetValue<string>() ?? "");
	}

	public virtual void OnWebSocketMessage(JsonNode json) { }

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

	private static Exception InvalidWsMessage(string messageType, string expectedAttribute)
		=> new FormatException($"'{messageType}' message without '{expectedAttribute}' attribute");
}