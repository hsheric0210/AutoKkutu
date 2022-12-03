using AutoKkutu.Constants;
using System;

namespace AutoKkutu.Modules.HandlerManager
{
	public class WordPresentEventArgs : EventArgs
	{
		public ResponsePresentedWord Word
		{
			get;
		}

		public string? MissionChar
		{
			get;
		}

		public WordPresentEventArgs(ResponsePresentedWord word, string? missionChar)
		{
			Word = word;
			MissionChar = missionChar;
		}
	}
}
