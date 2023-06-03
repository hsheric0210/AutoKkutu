using AutoKkutuLib.Browser;
using Serilog;
using System.Collections.Specialized;
using System.Text.Json.Nodes;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private IDictionary<string, Action<JsonNode>>? specializedSniffers;
	private string myUserId;
	private int mySeqIndex;
	private IList<string>? myGameSeq;

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
		myUserId = data.UserId ?? "<Unknown>";
		Log.Debug("Caught user id: {id}", myUserId);
	}

	private void OnWsRoom(WsRoom data)
	{
		if (!data.Players.Contains(myUserId)) // It's other room
			return;

		if (data.Gaming && data.GameSequence.Count > 0)
		{
			mySeqIndex = data.GameSequence.IndexOf(myUserId);
			if (mySeqIndex >= 0)
			{
				myGameSeq = data.GameSequence;
				Log.Information("MyGameSeqIdx: {idx}", mySeqIndex);
			}
		}

		GameMode gameMode = data.Mode;
		if (gameMode != GameMode.None && gameMode != CurrentGameMode)
		{
			CurrentGameMode = gameMode;
			GameModeChanged?.Invoke(this, new GameModeChangeEventArgs(gameMode));
		}
	}

	private void OnWsClassicTurnStart(WsClassicTurnStart data)
	{
		if (myGameSeq != null && data.Turn % myGameSeq.Count == mySeqIndex)
			Log.Information("WS: My turn start!");
	}

	private void OnWsClassicTurnEnd(WsClassicTurnEnd data)
	{
		if (data.Ok && myGameSeq != null)
		{
			var idx = myGameSeq.IndexOf(data.Target);
			var totalSeqCount = myGameSeq.Count;
			var turn = (idx + totalSeqCount) % totalSeqCount;
			var myPrevTurn = (mySeqIndex - 1 + totalSeqCount) % totalSeqCount;
			Log.Information("target idx: {tidx} turn idx: {turn} my-prev idx: {midx} my-idx: {mydx}", idx, turn, myPrevTurn, mySeqIndex);
			if (idx >= 0 && turn == myPrevTurn)
			{
				Log.Information("WS: The previous person submitted: {txt}!", data.Value);
			}
		}
	}

	private void OnWsTurnError(WsTurnError data)
	{
		Log.Warning("Turn error: {txt} - error code {err}", data.Value, data.ErrorCode);
	}
}
