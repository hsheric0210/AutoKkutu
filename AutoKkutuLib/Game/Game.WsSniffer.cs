using AutoKkutuLib.Browser;
using AutoKkutuLib.Extension;
using Serilog;
using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private IDictionary<string, Action<JsonNode>>? specializedSniffers;
	private WsSessionInfo? wsSession;
	private readonly JsonSerializerOptions unescapeUnicodeJso = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private void BeginWebSocketSniffing()
	{
		if (wsSniffHandler == null)
			return;

		Log.Information("WebSocket Sniffer initialized.");
		specializedSniffers = new Dictionary<string, Action<JsonNode>>();
		specializedSniffers[wsSniffHandler.MessageType_Welcome] = json => OnWsWelcome(wsSniffHandler.ParseWelcome(json));
		specializedSniffers[wsSniffHandler.MessageType_Room] = json => OnWsRoom(wsSniffHandler.ParseRoom(json));
		specializedSniffers[wsSniffHandler.MessageType_TurnStart] = json => OnWsClassicTurnStart(wsSniffHandler.ParseClassicTurnStart(json));
		specializedSniffers[wsSniffHandler.MessageType_TurnEnd] = json => OnWsClassicTurnEnd(wsSniffHandler.ParseClassicTurnEnd(json));
		specializedSniffers[wsSniffHandler.MessageType_TurnError] = json => OnWsTurnError(wsSniffHandler.ParseClassicTurnError(json));
		Browser.WebSocketMessage += OnWebSocketMessage;
	}

	private void EndWebSocketSniffing()
	{
		if (wsSniffHandler == null)
			return;

		Browser.WebSocketMessage -= OnWebSocketMessage;
		specializedSniffers = null;
		Log.Information("WebSocket Sniffer uninitialized.");
	}

	/// <summary>
	/// 과도한 양의 WebSocket 메시지 처리에 의한 부하와 프리징, 그리고 정작 중요한 JavaScript들의 실행 실패를 막기 위해
	/// 일차적으로 브라우저 단에서 받을 메시지들을 필터링합니다. (이전까지 만연했던 프리징과 JavaScript 실행 타임아웃 오류의 원인은 모두 이것 때문이었습니다...)
	/// 그 다음 이차적으로 프로그램 단에서 메시지를 검증하고 필터링하는 과정을 거칩니다.
	/// </summary>
	/// <returns></returns>
	private async Task RegisterWebSocketFilters()
	{
		var wsFilter = Browser.GetScriptTypeName(CommonNameRegistry.WsFilter, false, true);
		if (await Browser.EvaluateJavaScriptBoolAsync($"{wsFilter}.registered"))
			return;
		Browser.ExecuteJavaScript(@$"
		{wsFilter}['{wsSniffHandler.MessageType_Welcome}']=function(d){{return true;}};
		{wsFilter}['{wsSniffHandler.MessageType_TurnStart}']=function(d){{return true;}};
		{wsFilter}['{wsSniffHandler.MessageType_TurnEnd}']=function(d){{return true;}};
		{wsFilter}['{wsSniffHandler.MessageType_TurnError}']=function(d){{return true;}};
		{wsFilter}.registered=true;
		{wsFilter}.active=true;
		", "WsFilter register");
	}

	/// <summary>
	/// 웹소켓으로부터 메세지 수신 시, 핸들링을 위해 실행되는 제일 첫 단계의 함수.
	/// 메세지는 이 함수에서 메세지 종류에 따라 버려지거나, 다른 특화된 처리 함수들로 갈라져 들어갑니다.
	/// </summary>
	private void OnWebSocketMessage(object? sender, WebSocketMessageEventArgs args)
	{
		if (specializedSniffers?.TryGetValue(args.Type, out Action<JsonNode>? mySniffer) ?? false)
		{
			if (args.Type != wsSniffHandler.MessageType_Room)
			{
				Log.Debug("WS Message (type: {type}) - {json}", args.Type, args.Json.ToJsonString(unescapeUnicodeJso));
			}
			mySniffer(args.Json);
		}
	}

	private void OnWsWelcome(WsWelcome data)
	{
		wsSession = new WsSessionInfo(data.UserId);
		Log.Debug("Caught user id: {id}", data.UserId);

		// Register events to wsFilter
		var wsFilter = Browser.GetScriptTypeName(CommonNameRegistry.WsFilter, false, true);
		Browser.ExecuteJavaScript($"{wsFilter}['{wsSniffHandler.MessageType_Room}']=function(d){{return d&&d.room&&d.room.players&&Array.prototype.includes.call(d.room.players,'{data.UserId}');}};", "WsFilter register");
	}

	private void OnWsRoom(WsRoom data)
	{
		if (wsSession is not WsSessionInfo session)
			return;

		if (!data.Players.Contains(session.MyUserId)) // It's other room
			return;

		if (data.Mode == GameMode.None)
			Log.Warning("Unknown or unsupported game mode: {mode}", data.ModeString);

		if (data.Gaming && data.GameSequence.Count > 0)
			wsSession = session with { MyGameTurns = data.GameSequence.ToImmutableList() };

		NotifyGameMode(data.Mode);
	}

	private void OnWsClassicTurnStart(WsClassicTurnStart data)
	{
		if (wsSession is WsSessionInfo session && session.IsGaming)
		{
			if (session.IsMyTurn(data.Turn))
			{
				Log.Information("WS: My turn start! Condition is {cond}", data.Condition);
				NotifyMyTurn(true, data.Condition, bypassCache: true);
				return;
			}
			else if (session.GetRelativeTurn(data.Turn) == session.GetMyPreviousUserTurn())
			{
				wsSession = session with { MyGamePreviousUserMission = data.Condition.MissionChar };
				Log.Information("WS: Captured previous user mission char: {missionChar}", data.Condition.MissionChar);
			}

			NotifyMyTurn(false); // Other user turn start -> not my turn
		}
	}

	private void OnWsClassicTurnEnd(WsClassicTurnEnd data)
	{
		if (wsSession is not WsSessionInfo session || !session.IsGaming)
			return;

		if (data.Ok)
		{
			if (string.IsNullOrWhiteSpace(data.Value))
				return;

			var turn = session.GetTurnOf(data.Target);
			var prvUsrTurn = session.GetMyPreviousUserTurn();
			Log.Information("turn: {turn}, prev_user_turn: {puturn}", turn, prvUsrTurn);
			if (turn >= 0 && turn == prvUsrTurn)
			{
				Log.Debug("WS: The previous user {target} submit: {txt}!", data.Target, data.Value);
				var missionChar = session.MyGamePreviousUserMission;
				var canPresearch = true;
				if (missionChar != null && data.Value.Contains(missionChar))
					canPresearch = false; // The mission char will be changed. Thus, our effort to pre-search word database will become useless. Use standard search method instead. (Search on my turn started)

				WordCondition? condition = CurrentGameMode.ConvertWordToCondition(data.Value, session.MyGamePreviousUserMission);
				if (condition == null)
					return; // Conversion unavailable

				CurrentPresentedWord = condition; // Required to bypass initial 'CheckPathExpired' check
				PreviousUserTurnEnded?.Invoke(this, new PreviousUserTurnEndedEventArgs(canPresearch, condition));
			}

			if (!session.IsMyTurn(turn))
				NotifyWordHistory(data.Value);
		}

		if (session.IsMyTurn(session.GetTurnOf(data.Target)))
			NotifyMyTurn(false, bypassCache: true); // My turn ended

		if (!string.IsNullOrWhiteSpace(data.Hint))
			NotifyWordHint(data.Hint);
	}

	private void OnWsTurnError(WsTurnError data)
	{
		if (!string.IsNullOrWhiteSpace(data.Value))
			NotifyTurnError(data.Value, data.ErrorCode);
	}
}
