namespace AutoKkutuLib.Game;
public partial class Game
{
	private void OnWsTypingBattleRoundReady(WsTypingBattleRoundReady data)
	{
		NotifyTypingBattleRoundChange(data.Round, data.List);
	}

	private void OnWsTypingBattleTurnStart(WsTypingBattleTurnStart data)
	{
		NotifyTypingBattleTurnStart();
	}

	private void OnWsTypingBattleTurnEnd(WsTypingBattleTurnEnd data)
	{
		if (data.Ok)
			NotifyTypingBattleTurnEndOk();
		else // on error
			NotifyTypingBattleUpdate();
	}
}
