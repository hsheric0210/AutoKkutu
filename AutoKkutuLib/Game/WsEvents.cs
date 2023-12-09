using System.Collections.Immutable;

namespace AutoKkutuLib.Game;

public class WsWelcome
{
	public string UserId { get; }
	public WsWelcome(string userId) => UserId = userId;
}

public class WsRoom
{
	public string? ModeString { get; }
	public GameMode Mode { get; }
	public IImmutableList<string> Players { get; }
	public bool Gaming { get; }
	public IImmutableList<string> GameSequence { get; }
	public WsRoom(string? modeString, GameMode mode, IImmutableList<string> players, bool gaming, IImmutableList<string> gameSeq)
	{
		ModeString = modeString;
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
	public bool Ok { get; }
	public string? Value { get; }
	public string? Hint { get; }
	public WsClassicTurnEnd(bool ok, string? value, string? hint)
	{
		Ok = ok;
		Value = value;
		Hint = hint;
	}
}

public class WsClassicTurnError
{
	/// <summary>
	/// https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Web/lang/en_US.json#L213 참조
	/// </summary>
	public TurnErrorCode ErrorCode { get; }
	public string? Value { get; }
	public WsClassicTurnError(TurnErrorCode errorCode, string? value)
	{
		ErrorCode = errorCode;
		Value = value;
	}
}

public class WsTypingBattleRoundReady
{
	public int Round { get; }
	public IImmutableList<string> List { get; }
	public WsTypingBattleRoundReady(int round, IImmutableList<string> list)
	{
		Round = round;
		List = list;
	}
}

public class WsTypingBattleTurnStart
{
	public long RoundTime { get; }
	public WsTypingBattleTurnStart(long roundTime)
	{
		RoundTime = roundTime;
	}
}

public class WsTypingBattleTurnEnd
{
	public bool Ok { get; }
	public WsTypingBattleTurnEnd(bool ok)
	{
		Ok = ok;
	}
}

public class WsHunminRoundReady
{
	public int Round { get; }
	public WordCondition Condition { get; }

	public WsHunminRoundReady(int round, WordCondition condition)
	{
		Round = round;
		Condition = condition;
	}
}

public class WsHunminTurnStart
{
	public int Turn { get; }
	public long RoundTime { get; }
	public long TurnTime { get; }
	public string Mission { get; }

	public WsHunminTurnStart(int turn, long roundTime, long turnTime, string mission)
	{
		Turn = turn;
		RoundTime = roundTime;
		TurnTime = turnTime;
		Mission = mission;
	}
}
