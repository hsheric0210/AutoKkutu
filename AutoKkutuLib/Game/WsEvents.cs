namespace AutoKkutuLib.Game;

public class WsWelcome : EventArgs
{
	public string? UserId { get; set; }
	public WsWelcome(string? userId) => UserId = userId;
}

public class WsRoom : EventArgs
{
	public GameMode Mode { get; set; }
	public WsRoom(GameMode mode) => Mode = mode;
}

public class WsClassicTurnStart : EventArgs
{
	public int Turn { get; set; }
	public long RoundTime { get; set; }
	public long TurnTime { get; set; }
	public WordCondition Condition { get; set; }
	public string? Mission { get; set; }
	public WsClassicTurnStart(int turn, long roundTime, long turnTime, WordCondition condition, string? mission)
	{
		Turn = turn;
		RoundTime = roundTime;
		TurnTime = turnTime;
		Condition = condition;
		Mission = mission;
	}
}

public class WsClassicTurnEnd : EventArgs
{
	public string? Hint { get; set; }
	public WsClassicTurnEnd(string? hint) => Hint = hint;
}

public class WsTurnError : EventArgs
{
	/// <summary>
	/// https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Web/lang/en_US.json#L213 참조
	/// </summary>
	public int ErrorCode { get; set; }
	public string? Value { get; set; }
	public WsTurnError(int errCode, string? value)
	{
		ErrorCode = errCode;
		Value = value;
	}
}
