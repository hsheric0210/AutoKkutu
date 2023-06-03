using AutoKkutuLib.Browser.Events;
using Serilog;
using System.Text.Json.Nodes;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private IDictionary<string, Action<JsonNode>>? specializedSniffers;
	private string myUserId;

	private void BeginWebSocketSniffing()
	{
		if (wsSniffHandler == null)
			return;

		specializedSniffers = new Dictionary<string, Action<JsonNode>>()
		{
			[wsSniffHandler.MessageType_Welcome] = json => OnWsWelcome(wsSniffHandler.ParseWelcome(json)),
			[wsSniffHandler.MessageType_Room] = json => OnWsRoom(wsSniffHandler.ParseRoom(json)),
			[wsSniffHandler.MessageType_TurnStart] = json => OnWsClassicTurnStart(wsSniffHandler.ParseClassicTurnStart(json)),
			[wsSniffHandler.MessageType_TurnEnd] = json => OnWsClassicTurnEnd(wsSniffHandler.ParseClassicTurnEnd(json)),
			[wsSniffHandler.MessageType_TurnError] = json => OnWsTurnError(wsSniffHandler.ParseClassicTurnError(json)),
		};
		Browser.WebSocketMessage += OnWebSocketMessage;
	}

	private void EndWebSocketSniffing()
	{
		if (wsSniffHandler == null)
			return;

		Browser.WebSocketMessage -= OnWebSocketMessage;
		specializedSniffers = null;
	}

	/// <summary>
	/// 웹소켓으로부터 메세지 수신 시, 핸들링을 위해 실행되는 제일 첫 단계의 함수.
	/// 메세지는 이 함수에서 메세지 종류에 따라 버려지거나, 다른 특화된 처리 함수들로 갈라져 들어갑니다.
	/// </summary>
	private void OnWebSocketMessage(object? sender, WebSocketMessageEventArgs args)
	{
		if (specializedSniffers?.TryGetValue(args.Type, out Action<JsonNode>? mySniffer) ?? false)
			mySniffer(args.Json);
	}

	private void OnWsWelcome(WsWelcome data)
	{
		myUserId = data.UserId ?? "<Unknown>";
		Log.Debug("Caught user id: {id}", myUserId);
	}

	private void OnWsRoom(WsRoom data)
	{
		GameMode gameMode = data.Mode;
		if (gameMode != GameMode.None && gameMode != CurrentGameMode)
		{
			CurrentGameMode = gameMode;
			GameModeChanged?.Invoke(this, new GameModeChangeEventArgs(gameMode));
		}
	}

	private void OnWsClassicTurnStart(WsClassicTurnStart data)
	{
	}

	private void OnWsClassicTurnEnd(WsClassicTurnEnd data)
	{
	}

	private void OnWsTurnError(WsTurnError data)
	{
	}
}
