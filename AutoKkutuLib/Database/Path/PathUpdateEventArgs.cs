using System.Collections.Immutable;

namespace AutoKkutuLib.Database.Path;

public class PathUpdateEventArgs : EventArgs
{
	public PathFindResultType Result { get; }
	public PathDetails Details { get; }
	public int TimeMillis { get; }
	public PathList FoundWordList { get; }
	public PathList FilteredWordList { get; }
	public PathUpdateEventArgs(PathDetails details, PathFindResultType result, IImmutableList<PathObject> found, IImmutableList<PathObject> filtered, int timeElapsed = 0)
	{
		Details = details;
		Result = result;
		FoundWordList = new PathList(found, details);
		FilteredWordList = new PathList(filtered, details);
		TimeMillis = timeElapsed;
	}

	public bool HasFlag(PathFlags flag) => Details.HasFlag(flag);
}
