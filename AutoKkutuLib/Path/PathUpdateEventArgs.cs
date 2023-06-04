namespace AutoKkutuLib.Path;

public class PathUpdateEventArgs : EventArgs
{
	public int CalcWordCount
	{
		get;
	}

	public PathFindResultType ResultType
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

	public PathUpdateEventArgs(PathFinderParameter result, PathFindResultType type, int totalWordCount = 0, int calcWordCount = 0, int time = 0)
	{
		Result = result;
		ResultType = type;
		TotalWordCount = totalWordCount;
		CalcWordCount = calcWordCount;
		TimeMillis = time;
	}
}
