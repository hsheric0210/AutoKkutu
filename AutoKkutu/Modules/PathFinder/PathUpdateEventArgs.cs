using AutoKkutu.Constants;
using System;

namespace AutoKkutu.Modules.PathFinder
{

	public class PathUpdateEventArgs : EventArgs
	{
		public int CalcWordCount
		{
			get;
		}

		public PathFinderOptions Flags
		{
			get;
		}

		public string MissionChar
		{
			get;
		}

		public PathFinderResult Result
		{
			get;
		}

		public int Time
		{
			get;
		}

		public int TotalWordCount
		{
			get;
		}

		public ResponsePresentedWord Word
		{
			get;
		}

		public PathUpdateEventArgs(ResponsePresentedWord word, string missionChar, PathFinderResult arg, int totalWordCount = 0, int calcWordCount = 0, int time = 0, PathFinderOptions flags = PathFinderOptions.None)
		{
			Word = word;
			MissionChar = missionChar;
			Result = arg;
			TotalWordCount = totalWordCount;
			CalcWordCount = calcWordCount;
			Time = time;
			Flags = flags;
		}
	}
}
