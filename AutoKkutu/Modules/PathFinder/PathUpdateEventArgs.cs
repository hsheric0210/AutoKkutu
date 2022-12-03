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

		public PathType ResultType
		{
			get;
		}

		public PathFound Result
		{
			get;
		}

		public int TimeMillis
		{
			get;
		}

		public int TotalWordCount
		{
			get;
		}

		public PathUpdateEventArgs(PathFound result, PathType arg, int totalWordCount = 0, int calcWordCount = 0, int time = 0)
		{
			Result = result;
			ResultType = arg;
			TotalWordCount = totalWordCount;
			CalcWordCount = calcWordCount;
			TimeMillis = time;
		}
	}
}
