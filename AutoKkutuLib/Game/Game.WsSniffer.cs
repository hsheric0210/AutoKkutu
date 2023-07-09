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
	private IDictionary<GameImplMode, IDictionary<string, Func<JsonNode, Task>>>? specializedSniffers;
	private IDictionary<string, Func<JsonNode, Task>>? baseSniffers;
	private WsSessionState? wsSession;
	private readonly JsonSerializerOptions unescapeUnicodeJso = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private void BeginWebSocketSniffing()
	{
		if (wsSniffHandler == null)
			return;

		Func<JsonNode, Task> SimpleHandler<T>(string messageType, Action<T> handler, Func<JsonNode, Task<T>> parser)
		{
			return async json =>
			{
				try
				{
					handler(await parser(json));
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error processing {messageType} message.", messageType);
				}
			};
		}

		Log.Information("WebSocket Sniffer initialized.");

		baseSniffers = new Dictionary<string, Func<JsonNode, Task>>
		{
			[wsSniffHandler.MessageType_Welcome] = SimpleHandler("welcome", OnWsWelcome, wsSniffHandler.ParseWelcome),
			[wsSniffHandler.MessageType_Room] = SimpleHandler("room", OnWsRoom, wsSniffHandler.ParseRoom),
		};

		specializedSniffers = new Dictionary<GameImplMode, IDictionary<string, Func<JsonNode, Task>>>()
		{
			[GameImplMode.Classic] = new Dictionary<string, Func<JsonNode, Task>>
			{
				[wsSniffHandler.MessageType_TurnStart] = SimpleHandler("turnStart", OnWsClassicTurnStart, wsSniffHandler.ParseClassicTurnStart),
				[wsSniffHandler.MessageType_TurnEnd] = SimpleHandler("turnEnd", OnWsClassicTurnEnd, wsSniffHandler.ParseClassicTurnEnd),
				[wsSniffHandler.MessageType_TurnError] = SimpleHandler("turnError", OnWsTurnError, wsSniffHandler.ParseClassicTurnError)
			},
			[GameImplMode.TypingBattle] = new Dictionary<string, Func<JsonNode, Task>>
			{
				[wsSniffHandler.MessageType_RoundReady] = SimpleHandler("roundReady", OnWsTypingBattleRoundReady, wsSniffHandler.ParseTypingBattleRoundReady),
				[wsSniffHandler.MessageType_TurnStart] = SimpleHandler("turnStart", OnWsTypingBattleTurnStart, wsSniffHandler.ParseTypingBattleTurnStart),
				[wsSniffHandler.MessageType_TurnEnd] = SimpleHandler("turnEnd", OnWsTypingBattleTurnEnd, wsSniffHandler.ParseTypingBattleTurnEnd),
			}
		};
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
		if (wsSniffHandler != null)
			await wsSniffHandler.RegisterWebSocketFilter();
	}

	/// <summary>
	/// 웹소켓으로부터 메세지 수신 시, 핸들링을 위해 실행되는 제일 첫 단계의 함수.
	/// 메세지는 이 함수에서 메세지 종류에 따라 버려지거나, 다른 특화된 처리 함수들로 갈라져 들어갑니다.
	/// </summary>
	private void OnWebSocketMessage(object? sender, WebSocketMessageEventArgs args)
	{
		wsSniffHandler?.OnWebSocketMessage(args.Json);
		Log.Verbose("WS Message (type: {type}) - {json}", args.Type, args.Json.ToJsonString(unescapeUnicodeJso));
		if (specializedSniffers != null && specializedSniffers.TryGetValue(CurrentGameMode.ToGameImplMode(), out var snifferTable) && snifferTable.TryGetValue(args.Type, out var mySpecialSniffer))
			Task.Run(async () => await mySpecialSniffer(args.Json));
		else if (baseSniffers?.TryGetValue(args.Type, out var myBaseSniffer) ?? false)
			Task.Run(async () => await myBaseSniffer(args.Json));
	}

	private void OnWsWelcome(WsWelcome data)
	{
		if (wsSniffHandler == null)
			return;

		wsSession = new WsSessionState(data.UserId);
		Log.Debug("Caught user id: {id}", data.UserId);
	}

	private void OnWsRoom(WsRoom data)
	{
		if (wsSession == null)
			return;

		if (!data.Players.Contains(wsSession.MyUserId)) // It's other room
			return;

		if (data.Mode == GameMode.None)
			Log.Warning("Unknown or unsupported game mode: {mode}", data.ModeString);

		if (data.Gaming && data.GameSequence.Count > 0)
			wsSession.UpdateGameSeq(data.GameSequence);

		NotifyGameMode(data.Mode);
	}

	private void OnWsClassicTurnStart(WsClassicTurnStart data)
	{
		if (wsSession != null && wsSession.IsGaming)
		{
			wsSession.Turn = data.Turn;
			if (wsSession.IsMyTurn())
			{
				Log.Debug("WS: My turn start! Condition is {cond}", data.Condition);
				NotifyMyTurn(true, data.Condition, bypassCache: true);
				return;
			}
			else if (wsSession.GetRelativeTurn() == wsSession.GetMyPreviousUserTurn())
			{
				wsSession.MyGamePreviousUserMission = data.Condition.MissionChar;
				Log.Debug("WS: Captured previous user mission char: {missionChar}", data.Condition.MissionChar);
			}

			NotifyMyTurn(false); // Other user turn start -> not my turn
		}
	}

	private void OnWsClassicTurnEnd(WsClassicTurnEnd data)
	{
		if (wsSession == null || !wsSession.IsGaming)
			return;

		if (data.Ok)
		{
			if (string.IsNullOrWhiteSpace(data.Value))
				return;

			var turn = wsSession.Turn;
			var prvUsrTurn = wsSession.GetMyPreviousUserTurn();
			Log.Verbose("turn: {turn}, prev_user_turn: {puturn}", turn, prvUsrTurn);
			if (turn >= 0 && turn == prvUsrTurn)
			{
				var missionChar = wsSession.MyGamePreviousUserMission;
				var presearch = PreviousUserTurnEndedEventArgs.PresearchAvailability.Available;
				if (missionChar != null && data.Value.Contains(missionChar))
					presearch = PreviousUserTurnEndedEventArgs.PresearchAvailability.ContainsMissionChar;

				WordCondition? condition = CurrentGameMode.ConvertWordToCondition(data.Value, wsSession.MyGamePreviousUserMission);
				if (condition == null)
					presearch = PreviousUserTurnEndedEventArgs.PresearchAvailability.UnableToParse;

				CurrentWordCondition = condition; // Required to bypass initial 'CheckPathExpired' check
				PreviousUserTurnEnded?.Invoke(this, new PreviousUserTurnEndedEventArgs(presearch, condition));
			}

			//if (!wsSession.IsMyTurn())
			NotifyWordHistory(data.Value);
		}

		if (wsSession.IsMyTurn())
			NotifyMyTurn(false, bypassCache: true); // My turn ended

		if (!string.IsNullOrWhiteSpace(data.Hint))
			NotifyWordHint(data.Hint);
	}

	private void OnWsTurnError(WsTurnError data)
	{
		if (!string.IsNullOrWhiteSpace(data.Value))
			NotifyTurnError(data.Value, data.ErrorCode);
	}

	private void OnWsTypingBattleRoundReady(WsTypingBattleRoundReady data)
	{
		// TODO
	}


	private void OnWsTypingBattleTurnStart(WsTypingBattleTurnStart data)
	{
		// TODO
	}


	private void OnWsTypingBattleTurnEnd(WsTypingBattleTurnEnd data)
	{
		// TODO
	}
}
