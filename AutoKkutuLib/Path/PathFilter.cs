using ConcurrentCollections;
using System.Collections.Immutable;

namespace AutoKkutuLib.Path;

/// <summary>
/// Holder and handler class for special words such as already-used words, unsupported words, inexistent words, etc.
/// </summary>
public class PathFilter
{
	/// <summary>
	/// Inexistent paths such as inexistent word, invalid word, etc.
	/// </summary>
	public ICollection<string> InexistentPaths { get; } = new ConcurrentHashSet<string>();

	public ICollection<string> NewPaths { get; } = new ConcurrentHashSet<string>();

	public ICollection<(GameMode, string)> NewEndPaths { get; } = new ConcurrentHashSet<(GameMode, string)>();

	public ICollection<string> PreviousPaths { get; } = new ConcurrentHashSet<string>();

	public ICollection<string> UnsupportedPaths { get; } = new ConcurrentHashSet<string>();

	/// <summary>
	/// Filters out unqualified paths such as Inexistent paths, Unsupported paths, Already-used paths from the path list.
	/// </summary>
	/// <param name="pathList">The input path list</param>
	/// <returns>Qualified path list</returns>
	/// <exception cref="ArgumentNullException">If <paramref name="pathList"/> is null</exception>
	public IImmutableList<PathObject> FilterPathList(IImmutableList<PathObject> pathList, bool reuseAlreadyUsed)
	{
		if (pathList is null)
			throw new ArgumentNullException(nameof(pathList));

		var qualifiedList = new List<PathObject>();
		foreach (var path in pathList)
		{
			if (InexistentPaths.Contains(path.Content))
				path.RemoveQueued = true;
			if (UnsupportedPaths.Contains(path.Content))
				path.Excluded = true;
			else if (!reuseAlreadyUsed && PreviousPaths.Contains(path.Content))
				path.AlreadyUsed = true;
			else
				qualifiedList.Add(path);
		}

		return qualifiedList.ToImmutableList();
	}
}
