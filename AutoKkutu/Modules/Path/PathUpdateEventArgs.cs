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

		public PathFindResult ResultType
		{
			get;
		}

		public PathFinderParameter Result
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

		public PathUpdateEventArgs(PathFinderParameter result, PathFindResult arg, int totalWordCount = 0, int calcWordCount = 0, int time = 0)
		{
			Result = result;
			ResultType = arg;
			TotalWordCount = totalWordCount;
			CalcWordCount = calcWordCount;
			TimeMillis = time;
		}
	}
}
