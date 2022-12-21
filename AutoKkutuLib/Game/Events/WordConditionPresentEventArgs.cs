namespace AutoKkutuLib.Game.Events;

public class WordConditionPresentEventArgs : EventArgs
{
	public PresentedWord Word
	{
		get;
	}

	public string MissionChar
	{
		get;
	}

	public WordConditionPresentEventArgs(PresentedWord word, string missionChar)
	{
		Word = word;
		MissionChar = missionChar;
	}
}
