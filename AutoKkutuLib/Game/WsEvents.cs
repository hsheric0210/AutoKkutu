namespace AutoKkutuLib.Game;

public class WsWelcome
{
	public string? UserId { get; }
	public WsWelcome(string? userId) => UserId = userId;
}

public class WsRoom
{
	public GameMode Mode { get; }
	public ICollection<string> Players { get; }
	public bool Gaming { get; }
	public IList<string> GameSequence { get; }
	public WsRoom(GameMode mode, ICollection<string> players, bool gaming, IList<string> gameSeq)
	{
		Mode = mode;
		Players = players;
		Gaming = gaming;
		GameSequence = gameSeq;
	}
}

public class WsClassicTurnStart
{
	public int Turn { get; }
	public long RoundTime { get; }
	public long TurnTime { get; }
	public WordCondition Condition { get; }
	public WsClassicTurnStart(int turn, long roundTime, long turnTime, WordCondition condition)
	{
		Turn = turn;
		RoundTime = roundTime;
		TurnTime = turnTime;
		Condition = condition;
	}
}

public class WsClassicTurnEnd
{
	public string? Hint { get; }
	public string Target { get; }
	public string Value { get; }
	public bool Ok { get; }
	public WsClassicTurnEnd(bool ok, string value, string target, string? hint)
	{
		Ok = ok;
		Value = value;
		Target = target;
		Hint = hint;
	}
}

public class WsTurnError
{
	/// <summary>
	/// https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Web/lang/en_US.json#L213 참조
	/// </summary>
	public TurnErrorCode ErrorCode { get; }
	public string? Value { get; }
	public WsTurnError(int errCode, string? value)
	{
		ErrorCode = (TurnErrorCode)errCode;
		Value = value;
	}
}
