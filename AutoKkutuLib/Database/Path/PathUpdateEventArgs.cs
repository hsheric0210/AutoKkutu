using System.Collections.Immutable;

namespace AutoKkutuLib.Database.Path;

public class PathUpdateEventArgs : EventArgs
{
	public PathFindResultType Result { get; }
	public PathDetails Details { get; }
	public int TimeMillis { get; }
	public IImmutableList<PathObject> FoundWordList { get; }
	public int TotalWordCount => FoundWordList.Count;
	public IImmutableList<PathObject> FilteredWordList { get; }
	public int FilteredWordCount => FilteredWordList.Count;
	public PathUpdateEventArgs(PathDetails details, PathFindResultType result, IImmutableList<PathObject> found, IImmutableList<PathObject> filtered, int timeElapsed = 0)
	{
		Details = details;
		Result = result;
		FoundWordList = found;
		FilteredWordList = filtered;
		TimeMillis = timeElapsed;
	}

	public bool HasFlag(PathFlags flag) => Details.HasFlag(flag);
}
