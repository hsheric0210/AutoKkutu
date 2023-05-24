namespace AutoKkutuLib.Game.Events;

public class NoPathAvailableEventArgs : EventArgs
{
	public bool TimeOver
	{
		get;
	}

	public long RemainingTurnTime
	{
		get;
	}

	public NoPathAvailableEventArgs(bool timeover, long remainingTurnTime)
	{
		TimeOver = timeover;
		RemainingTurnTime = remainingTurnTime;
	}
}
