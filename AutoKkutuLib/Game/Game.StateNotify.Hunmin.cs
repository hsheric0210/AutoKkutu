namespace AutoKkutuLib.Game;
public partial class Game
{
	public void NotifyHunminTurnStart(bool isMyTurn, int turnIndex, string missionChar)
	{
		if (Session.WordCondition.IsEmpty())
			throw new InvalidOperationException("Received hunmin turn-start but the round word condition is empty. Is there were any exception handling round-ready event?");

		var condition = new WordCondition(Session.WordCondition.Char, missionChar: missionChar);
		NotifyClassicTurnStart(isMyTurn, turnIndex, condition);
	}

	public void NotifyHunminRoundChange(int roundIndex, WordCondition condition)
	{
		NotifyRoundChange(roundIndex);

		lock (sessionLock)
		{
			Session.WordCondition = condition;
		}
	}
}
