namespace AutoKkutuLib.Game.Events;

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
