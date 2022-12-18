namespace AutoKkutuLib.Game.Events;

public class UnsupportedWordEventArgs : EventArgs
{
	public string Word
	{
		get;
	}

	public bool IsExistingWord
	{
		get;
	}

	public UnsupportedWordEventArgs(string word, bool isExistingWord)
	{
		Word = word;
		IsExistingWord = isExistingWord;
	}
}
