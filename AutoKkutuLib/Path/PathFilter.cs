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
	/// Apply special marks to the list of paths.
	/// </summary>
	/// <param name="pathList">The list of path to mark.</param>
	/// <param name="reuseAlreadyUsed"><c>true</c> if re-using the previously used word is permitted, <c>false</c> otherwise.</param>
	/// <returns>The marked path list.</returns>
	/// <exception cref="ArgumentNullException">If <paramref name="pathList"/> is null.</exception>
	public IImmutableList<PathObject> MarkPathList(IImmutableList<PathObject> pathList, bool reuseAlreadyUsed)
	{
		if (pathList is null)
			throw new ArgumentNullException(nameof(pathList));

		var marked = ImmutableList.CreateBuilder<PathObject>();
		foreach (var path in pathList)
		{
			var marks = path.Marks;
			if (InexistentPaths.Contains(path.Content))
				marks |= PathMarks.RemoveQueued;
			if (UnsupportedPaths.Contains(path.Content))
				marks |= PathMarks.Excluded;
			else if (!reuseAlreadyUsed && PreviousPaths.Contains(path.Content))
				marks |= PathMarks.AlreadyUsed;
			marked.Add(path with { Marks = marks });
		}

		return marked.ToImmutable();
	}
}
