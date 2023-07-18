namespace AutoKkutuLib.Game;
public class EnterFinishedEventArgs : EventArgs
{
	public string Content { get; }
	public EnterFinishedEventArgs(string content) => Content = content;
}

public class GameModeChangeEventArgs : EventArgs
{
	public GameMode GameMode { get; }
	public GameModeChangeEventArgs(GameMode gameMode) => GameMode = gameMode;
}

public class InputDelayEventArgs : EventArgs
{
	public int Delay { get; }
	public int WordIndex { get; }
	public InputDelayEventArgs(int delay, int wordIndex)
	{
		Delay = delay;
		WordIndex = wordIndex;
	}
}

public class AllPathTimeOverEventArgs : EventArgs
{
	public long RemainingTurnTime { get; }
	public AllPathTimeOverEventArgs(long remainingTurnTime) => RemainingTurnTime = remainingTurnTime;
}

public class RoundChangeEventArgs : EventArgs
{
	public int RoundIndex { get; }
	public RoundChangeEventArgs(int roundIndex) => RoundIndex = roundIndex;
}

public class UnsupportedWordEventArgs : EventArgs
{
	public GameSessionState Session;
	public string Word { get; }
	public bool IsExistingWord { get; }
	public bool IsEndWord { get; }
	public UnsupportedWordEventArgs(GameSessionState gameSession, string word, bool isExistingWord, bool isEndWord)
	{
		Session = gameSession;
		Word = word;
		IsExistingWord = isExistingWord;
		IsEndWord = isEndWord;
	}
}

public class WordConditionPresentEventArgs : EventArgs
{
	public WordCondition Condition { get; }
	public WordConditionPresentEventArgs(WordCondition condition)
	{
		Condition = condition;
	}
}

public class TurnStartEventArgs : WordConditionPresentEventArgs
{
	public GameSessionState Session { get; }
	public TurnStartEventArgs(GameSessionState session, WordCondition condition) : base(condition) => Session = session;
}

public class PreviousUserTurnEndedEventArgs : EventArgs
{
	public enum PresearchAvailability
	{
		/// <summary>
		/// Pre-search를 사용할 수 있는 경우
		/// </summary>
		Available,

		/// <summary>
		/// 이전 유저가 입력한 단어에 미션 글자가 끼어 있는 경우;
		/// 이 경우, 다음 턴에 미션 글자가 확정적으로 갱신되기에 Pre-search를 해 놓는다고 하더라도 어차피 미션 글자가 바뀌어서 다시 검색해야 함
		/// </summary>
		ContainsMissionChar,

		/// <summary>
		/// 이전 유저의 입력 단어에서 Tail 노드를 추출할 수 없는 경우 (또는, Tail 노드가 한 가지로 정해지지 않는 경우);
		/// 예시로, 가운뎃말잇기에서 길이가 짝수인 단어를 입력할 시 단어 정 중앙의 두 글자 중 '랜덤으로' 다음 글자가 정해지기에 추출에 실패함
		/// </summary>
		UnableToParse
	}

	public PresearchAvailability Presearch { get; }
	public WordCondition? Condition { get; }
	public PreviousUserTurnEndedEventArgs(PresearchAvailability presearch, WordCondition? condition)
	{
		Presearch = presearch;
		Condition = condition;
	}
}

// TODO: Add more arguments such as word group, description, etc.
public class WordHistoryEventArgs : EventArgs
{
	public string Word { get; }
	public int Category { get; }
	public int Theme { get; }
	public WordHistoryEventArgs(string word) => Word = word;
}

public class WordPresentEventArgs : EventArgs
{
	public string Word { get; }
	public WordPresentEventArgs(string word) => Word = word;
}

public class TurnEndEventArgs : EventArgs
{
	public GameSessionState Session { get; }
	public string Value { get; }
	public TurnEndEventArgs(GameSessionState session, string value)
	{
		Session = session;
		Value = value;
	}
}