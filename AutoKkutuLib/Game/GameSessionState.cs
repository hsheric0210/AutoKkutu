using System.Collections.Immutable;

namespace AutoKkutuLib.Game;
public sealed class GameSessionState
{
	private int myTurnOrdinalCache = -1;

	private readonly object gameSeqLock = new();
	private readonly object gameModeLock = new();

	/// <summary>
	/// 현재 세션의 유저 ID를 나타냅니다.
	/// 해당 속성은 현재 세션에 종속적입니다.
	/// </summary>
	public string MyUserId { get; }

	/// <summary>
	/// 현재 세션에서 내가 참여한 게임이 진행 중인지 여부를 나타냅니다.
	/// 해당 속성은 현재 세션에 종속적이며, 스레드 안전합니다.
	/// </summary>
	public bool AmIGaming { get; private set; }

	/// <summary>
	/// 현재 세션의 게임의 게임 모드를 나타냅니다.
	/// 해당 속성은 현재 세션에 종속적이며, 스레드 안전합니다.
	/// </summary>
	public GameMode GameMode { get; private set; } = GameMode.None;

	/// <summary>
	/// 현재 세션의 게임 참여 유저 목록을 나타냅니다.
	/// 해당 속성은 현재 세션에 종속적입니다.
	/// </summary>
	public IImmutableList<string> GameSequence { get; private set; } = ImmutableList<string>.Empty;

	/// <summary>
	/// 현재 세션의 게임의 바로 이전 턴 미션 단어를 나타냅니다.
	/// 미션 모드가 활성화되지 않았거나, 현재 턴이 첫 턴이라면 빈 문자열일 수 있습니다.
	/// 해당 속성은 현재 게임에 종속적으로, 더 이상 게임 중인 상태가 아닐 시 빈 문자열로 초기화됩니다.
	/// </summary>
	/// <remarks>
	/// Pre-search 기능을 도울 목적으로 만들어졌습니다. 만약 turnEnd 이벤트를 받았으나 이 때의 <c>value</c>가 이 문자를 포함하고 있다면
	/// 이전 유저가 미션 글자가 포함된 단어를 친 것으로, 이번 턴에서 미션 글자는 거의 100% 다시 재배정될 것이므로 Pre-search를 수행해서는 안 됩니다.
	/// </remarks>
	public string PreviousTurnMission { get; internal set; } = "";

	/// <summary>
	/// 현재 세션의 게임의 턴 인덱스를 나타냅니다.
	/// 해당 속성은 현재 게임에 종속적으로, 더 이상 게임 중인 상태가 아닐 시 <c>-1</c>로 초기화됩니다.
	/// </summary>
	/// <remarks>
	/// 턴 인덱스는 해당 턴이 끝난 이후에도 여전히 해당 턴을 가리키고 있습니다.
	/// 해당 턴이 진행 중인지 확인하기 위해서는 <c>IsTurnInProgress</c> 속성을 사용해야 합니다.
	/// </remarks>
	public int TurnIndex { get; internal set; } = -1;

	/// <summary>
	/// 현재 세션의 게임의 턴이 진행 중인지의 여부를 나타냅니다.
	/// 해당 속성은 현재 게임에 종속적으로, 더 이상 게임 중인 상태가 아닐 시 <c>false</c>로 초기화됩니다.
	/// </summary>
	/// <remarks>
	/// 현재 세션의 게임의 턴 인덱스를 구하려면 <c>TurnIndex</c>을 사용하세요.
	/// </remarks>
	public bool IsTurnInProgress { get; internal set; }

	/// <summary>
	/// 현재 세션의 게임의 턴의 현재 단어 조건을 나타냅니다.
	/// 해당 속성은 현재 게임에 종속적으로, 더 이상 게임 중인 상태가 아닐 시 <c>WordCondition.Empty</c>로 초기화됩니다.
	/// </summary>
	public WordCondition WordCondition { get; internal set; } = WordCondition.Empty;

	/// <summary>
	/// 현재 세션의 게임이 리턴(이미 사용한 단어도 사용 가능) 모드인지의 여부를 나타냅니다.
	/// 해당 속성은 현재 게임에 종속적으로, 더 이상 게임 중인 상태가 아닐 시 <c>false</c>로 초기화됩니다.
	/// </summary>
	public bool ReturnMode { get; internal set; }

	/// <summary>
	/// (타자 대결 모드 한정) 타자 대결 단어 목록입니다. 모든 단어를 다 쓴 경우 다시 첫 단어부터 재사용됩니다.
	/// </summary>
	public int TypingWordIndex { get; internal set; }

	/// <summary>
	/// (타자 대결 모드 한정) 타자 대결 단어 목록입니다. 모든 단어를 다 쓴 경우 다시 첫 단어부터 재사용됩니다.
	/// </summary>
	public IImmutableList<string> TypingWordList { get; internal set; }

	public GameSessionState(GameSessionState other)
	{
		MyUserId = other.MyUserId;
		AmIGaming = other.AmIGaming;
		GameMode = other.GameMode;
		GameSequence = ImmutableList<string>.Empty.AddRange(other.GameSequence); // Copy immutable list: https://stackoverflow.com/a/35849446
		PreviousTurnMission = other.PreviousTurnMission;
		TurnIndex = other.TurnIndex;
		IsTurnInProgress = other.IsTurnInProgress;
		WordCondition = other.WordCondition;
		ReturnMode = other.ReturnMode;
	}

	public GameSessionState(string myUserId = "") => MyUserId = myUserId;

	public GameSessionState(string myUserId, IImmutableList<string> seq) : this(myUserId) => UpdateGameSequence(seq);

	public bool UpdateGameSequence(IImmutableList<string> seq)
	{
		var imGaming = seq.Contains(MyUserId);
		lock (gameSeqLock)
		{
			if (GameSequence.SequenceEqual(seq)) // TODO: Should use some kind of hashing or timestamping to mitigate array-comparison overhead
				return false;

			GameSequence = seq;

			myTurnOrdinalCache = -1; // invalidate cache

			if (AmIGaming && !imGaming)
			{
				// Flush game-specific caches
				PreviousTurnMission = "";
				TurnIndex = -1;
				IsTurnInProgress = false;
				WordCondition = WordCondition.Empty;
				ReturnMode = false;
				LibLogger.Verbose<GameSessionState>("Flushed game-specific caches.");
			}

			AmIGaming = imGaming;
		}
		return true;
	}

	public bool UpdateGameMode(GameMode gameMode)
	{
		lock (gameModeLock)
		{
			if (gameMode == GameMode)
				return false;
			GameMode = gameMode;
			return true;
		}
	}

	/// <summary>
	/// 실제로 지금 '몇 번째 플레이어의 턴인지'를 반환합니다.
	/// 예시로, 플레이어가 3명이고 지금이 63번째 턴이라면 지금은 0번째 사람(첫 번째 사람)의 턴입니다.
	/// </summary>
	public int GetRelativeTurn() => GameSequence.Count == 0 ? -1 : ((TurnIndex + GameSequence.Count) % GameSequence.Count);

	/// <summary>
	/// 실제로 지금 '몇 번째 플레이어의 턴인지'를 반환합니다.
	/// 예시로, 플레이어가 3명이고 지금이 63번째 턴이라면 지금은 0번째 사람(첫 번째 사람)의 턴입니다.
	/// 만약 게임이 진행 중이지 않다면, <c>-1</c>을 반환합니다.
	/// </summary>
	public int GetTurnOf(string userId) => GameSequence.IndexOf(userId);

	/// <summary>
	/// 현재 세션의 게임의 내 턴 인덱스를 나타냅니다.
	/// 값은 <c>GameSequence</c>로부터 계산되며 캐싱됩니다.
	/// 해당 속성은 현재 게임에 종속적으로, 더 이상 게임 중인 상태가 아닐 시 항상 <c>-1</c>을 반환합니다.
	/// </summary>
	public int GetMyTurnIndex()
	{
		if (!AmIGaming)
			return -1;
		if (GameSequence.Count > 0 && myTurnOrdinalCache < 0)
			myTurnOrdinalCache = GameSequence.IndexOf(MyUserId);
		return myTurnOrdinalCache;
	}

	/// <summary>
	/// 현재 세션의 게임에서 현재 턴이 내 턴인지의 여부를 반환합니다.
	/// 만약 현재 게임이 진행 중이지 않다면, 항상 <c>false</c>를 반환합니다.
	/// </summary>
	public bool IsMyTurn()
	{
		var turn = GetRelativeTurn();
		return turn >= 0 && turn == GetMyTurnIndex();
	}

	/// <summary>
	/// 내 바로 이전 사람의 턴 번째수를 반환합니다.
	/// </summary>
	/// <remarks>
	/// 만약 '랜덤턴' 모드가 활성화되었다면 내 바로 이전 사람이 단어 입력을 마치더라도, 다음 턴이 나에게 오지 않을 수 있다는 것에 주의합니다.
	/// </remarks>
	public int GetMyPreviousUserTurn() => GameSequence.Count == 0 ? -1 : ((GetMyTurnIndex() - 1 + GameSequence.Count) % GameSequence.Count);

	/// <summary>
	/// 현재 세션이 빈 세션인지의 여부를 반환합니다.
	/// </summary>
	public bool IsEmpty() => string.IsNullOrEmpty(MyUserId);
}
