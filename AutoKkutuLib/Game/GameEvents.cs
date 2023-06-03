namespace AutoKkutuLib.Game;
public class AutoEnterEventArgs : EventArgs
{
	public string Content { get; }
	public AutoEnterEventArgs(string content) => Content = content;
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

public class NoPathAvailableEventArgs : EventArgs
{
	public bool TimeOver { get; }
	public long RemainingTurnTime { get; }
	public NoPathAvailableEventArgs(bool timeover, long remainingTurnTime)
	{
		TimeOver = timeover;
		RemainingTurnTime = remainingTurnTime;
	}
}

public class RoundChangeEventArgs : EventArgs
{
	public int RoundIndex { get; }
	public RoundChangeEventArgs(int roundIndex) => RoundIndex = roundIndex;
}

public class UnsupportedWordEventArgs : EventArgs
{
	public string Word { get; }
	public bool IsExistingWord { get; }
	public bool IsEndWord { get; }
	public UnsupportedWordEventArgs(string word, bool isExistingWord, bool isEndWord)
	{
		Word = word;
		IsExistingWord = isExistingWord;
		IsEndWord = isEndWord;
	}
}

public class WordConditionPresentEventArgs : EventArgs
{
	public WordCondition Word { get; }
	public string MissionChar { get; }
	public WordConditionPresentEventArgs(WordCondition word, string missionChar)
	{
		Word = word;
		MissionChar = missionChar;
	}
}

// TODO: Add more arguments such as word group, description, etc.
public class WordHistoryEventArgs : EventArgs
{
	public string Word { get; }
	public WordHistoryEventArgs(string word) => Word = word;
}

public class WordPresentEventArgs : EventArgs
{
	public string Word { get; }
	public WordPresentEventArgs(string word) => Word = word;
}
