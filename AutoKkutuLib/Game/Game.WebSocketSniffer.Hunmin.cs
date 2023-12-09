namespace AutoKkutuLib.Game;
public partial class Game
{
	private void OnWsHunminRoundReady(WsHunminRoundReady data) => NotifyHunminRoundChange(data.Round, data.Condition);

	private void OnWsHunminTurnStart(WsHunminTurnStart data)
	{
		LibLogger.Debug(gameWebSocketSniffer, "WebSocket Handler detected hunmin turn start: turn={turn} mission={condition}", data.Turn, data.Mission);
		NotifyHunminTurnStart(false, data.Turn, data.Mission);
	}
}
