using AutoKkutu.Constants;
using System;

namespace AutoKkutu.Modules.HandlerManagement;

public class WordPresentEventArgs : EventArgs
{
	public PresentedWord Word
	{
		get;
	}

	public string MissionChar
	{
		get;
	}

	public WordPresentEventArgs(PresentedWord word, string missionChar)
	{
		Word = word;
		MissionChar = missionChar;
	}
}
