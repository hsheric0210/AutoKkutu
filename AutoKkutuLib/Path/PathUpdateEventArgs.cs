using System.Collections.Immutable;

namespace AutoKkutuLib.Path;

public class PathUpdateEventArgs : EventArgs
{
	public PathFindResultType Result { get; }
	public PathDetails Info { get; }
	public int TimeMillis { get; }
	public IImmutableList<PathObject> FoundWordList { get; }
	public int TotalWordCount => FoundWordList.Count;
	public IImmutableList<PathObject> FilteredWordList { get; }
	public int FilteredWordCount => FilteredWordList.Count;
	public PathUpdateEventArgs(PathDetails info, PathFindResultType result, IImmutableList<PathObject> found, IImmutableList<PathObject> filtered, int timeElapsed = 0)
	{
		Info = info;
		Result = result;
		FoundWordList = found;
		FilteredWordList = filtered;
		TimeMillis = timeElapsed;
	}

	public bool HasFlag(PathFlags flag) => Info.HasFlag(flag);
}
