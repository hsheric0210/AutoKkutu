using AutoKkutuLib.Browser;
using AutoKkutuLib.Extension;
using Serilog;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Text.Json.Nodes;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private IDictionary<string, Action<JsonNode>>? specializedSniffers;
	private WsSessionInfo? wsSession;

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
	/// 웹소켓으로부터 메세지 수신 시, 핸들링을 위해 실행되는 제일 첫 단계의 함수.
	/// 메세지는 이 함수에서 메세지 종류에 따라 버려지거나, 다른 특화된 처리 함수들로 갈라져 들어갑니다.
	/// </summary>
	private void OnWebSocketMessage(object? sender, WebSocketMessageEventArgs args)
	{
		if (specializedSniffers?.TryGetValue(args.Type, out Action<JsonNode>? mySniffer) ?? false)
		{
			Log.Debug("WS Message (type: {type}) - {json}", args.Type, args.Json.ToString());
			mySniffer(args.Json);
		}
	}

	private void OnWsWelcome(WsWelcome data)
	{
		wsSession = new WsSessionInfo(data.UserId);
		Log.Debug("Caught user id: {id}", data.UserId);
	}

	private void OnWsRoom(WsRoom data)
	{
		if (wsSession is not WsSessionInfo session)
			return;

		if (!data.Players.Contains(session.MyUserId)) // It's other room
			return;

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
				NotifyMyTurn(true, data.Condition);
			}
			else if (session.GetRelativeTurn(data.Turn) == session.GetMyPreviousUserTurn())
			{
				wsSession = session with { MyGamePreviousUserMission = data.Condition.MissionChar };
				Log.Information("WS: Captured previous user mission char: {missionChar}", data.Condition.MissionChar);
			}
		}
	}

	private void OnWsClassicTurnEnd(WsClassicTurnEnd data)
	{
		if (data.Ok && wsSession is WsSessionInfo session && session.IsGaming && !string.IsNullOrWhiteSpace(data.Value))
		{
			var turn = session.GetTurnOf(data.Target);
			if (turn >= 0 && session.GetRelativeTurn(turn) == session.GetMyPreviousUserTurn())
			{
				Log.Debug("WS: The previous user submit: {txt}!", data.Value);
				var missionChar = session.MyGamePreviousUserMission;
				if (missionChar != null && data.Value.Contains(missionChar))
					return; // The mission char will be changed. Thus, our effort to pre-search word database will become useless. Use standard search method instead. (Search on my turn started)

				WordCondition? condition = CurrentGameMode.ConvertWordToCondition(data.Value, session.MyGamePreviousUserMission);
				if (condition == null)
					return; // Conversion unavailable

				PreviousUserTurnEnded?.Invoke(this, new WordConditionPresentEventArgs((WordCondition)condition));
			}

			if (session.IsMyTurn(turn))
				NotifyMyTurn(false);
			else
				NotifyWordHistory(data.Value);
		}
	}

	private void OnWsTurnError(WsTurnError data)
	{
		if (!string.IsNullOrWhiteSpace(data.Value))
			NotifyTurnError(data.Value, data.ErrorCode);
	}
}
