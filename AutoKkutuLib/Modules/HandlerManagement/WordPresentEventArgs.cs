using AutoKkutuLib.Constants;
using System;

namespace AutoKkutuLib.Modules.HandlerManagement;

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
