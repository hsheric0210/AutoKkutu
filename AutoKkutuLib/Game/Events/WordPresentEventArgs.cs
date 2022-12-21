namespace AutoKkutuLib.Game.Events;

public class WordPresentEventArgs : EventArgs
{
	public string Word
	{
		get;
	}

	public WordPresentEventArgs(string word) => Word = word;
}
