namespace AutoKkutuLib.HandlerManagement.Events;

public class WordHistoryEventArgs : EventArgs
{
	public string Word
	{
		get;
	}

	// TODO: Add more arguments such as word group, description, etc.
	public WordHistoryEventArgs(string word) => Word = word;
}
