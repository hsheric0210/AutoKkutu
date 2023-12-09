namespace AutoKkutuLib.Game;
public partial class Game
{
	private void OnWsClassicTurnStart(WsClassicTurnStart data)
	{
		LibLogger.Debug(gameWebSocketSniffer, "WebSocket Handler detected turn start: turn={turn} condition={condition}", data.Turn, data.Condition);
		NotifyClassicTurnStart(false, data.Turn, data.Condition);
	}

	private void OnWsClassicTurnEnd(WsClassicTurnEnd data)
	{
		if (data.Ok)
		{
			LibLogger.Debug(gameWebSocketSniffer, "WebSocket Handler detected turn end (ok): value='{value}'", data.Value);
			NotifyClassicTurnEndOk(data.Value ?? "");

			if (!string.IsNullOrWhiteSpace(data.Value))
				NotifyWordHistory(data.Value);
		}

		if (!string.IsNullOrWhiteSpace(data.Hint))
		{
			LibLogger.Debug(gameWebSocketSniffer, "WebSocket Handler detected turn end (hint): hint='{hint}'", data.Hint);
			NotifyWordHint(data.Hint);
		}
	}
}
