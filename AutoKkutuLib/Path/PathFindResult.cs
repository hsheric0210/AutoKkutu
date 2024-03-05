using System.Collections.Immutable;

namespace AutoKkutuLib.Path;

public readonly struct PathFindResult
{
	private static readonly IImmutableList<PathObject> emptyList = ImmutableList<PathObject>.Empty;

	public readonly PathFindResultType Result { get; }
	public readonly PathDetails Details { get; }
	public readonly long TimeMillis { get; }
	public readonly PathList FoundWordList { get; }
	public readonly PathList FilteredWordList { get; }

	private PathFindResult(PathFindResultType result, PathDetails details, IImmutableList<PathObject> found, IImmutableList<PathObject> filtered, long timeElapsed)
	{
		Result = result;
		Details = details;
		FoundWordList = new PathList(found, details);
		FilteredWordList = new PathList(filtered, details);
		TimeMillis = timeElapsed;
	}

	// Utility methods
	public bool HasFlag(PathFlags flag) => Details.HasFlag(flag);

	// Factory methods

	public static PathFindResult Empty(PathDetails details)
		=> new(PathFindResultType.NotFound, details, emptyList, emptyList, 0);

	public static PathFindResult Finished(PathDetails details, IImmutableList<PathObject> found, IImmutableList<PathObject> filtered, long time)
		=> new(filtered.Count == 0 ? PathFindResultType.NotFound : PathFindResultType.Found, details, found, filtered, time);

	public static PathFindResult Error(PathDetails details)
		=> new(PathFindResultType.Error, details, emptyList, emptyList, 0);

	public static PathFindResult EndWord(PathDetails details)
		=> new(PathFindResultType.EndWord, details, emptyList, emptyList, 0);
}
