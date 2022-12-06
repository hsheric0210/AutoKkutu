namespace AutoKkutuLib.Modules.Path;

/// <summary>
/// Holder and handler class for special words such as already-used words, unsupported words, inexistent words, etc.
/// </summary>
public class SpecialPathList
{
	/// <summary>
	/// Inexistent paths such as inexistent word, invalid word, etc.
	/// </summary>
	public ICollection<string> InexistentPaths { get; } = new HashSet<string>();

	public ICollection<string> NewPaths { get; } = new HashSet<string>();

	public ICollection<string> PreviousPaths { get; } = new HashSet<string>();

	public ICollection<string> UnsupportedPaths { get; } = new HashSet<string>();

	public ReaderWriterLockSlim Lock
	{
		get;
	} = new();

	/// <summary>
	/// Filters out unqualified paths such as Inexistent paths, Unsupported paths, Already-used paths from the path list.
	/// </summary>
	/// <param name="pathList">The input path list</param>
	/// <returns>Qualified path list</returns>
	/// <exception cref="ArgumentNullException">If <paramref name="pathList"/> is null</exception>
	public IList<PathObject> CreateQualifiedWordList(IList<PathObject> pathList)
	{
		if (pathList is null)
			throw new ArgumentNullException(nameof(pathList));

		var qualifiedList = new List<PathObject>();
		foreach (PathObject path in pathList)
		{
			try
			{
				Lock.EnterReadLock();
				if (InexistentPaths.Contains(path.Content))
					path.RemoveQueued = true;
				if (UnsupportedPaths.Contains(path.Content))
					path.Excluded = true;
				else if (PreviousPaths.Contains(path.Content))
					path.AlreadyUsed = true;
				else
					qualifiedList.Add(path);
			}
			finally
			{
				Lock.ExitReadLock();
			}
		}

		return qualifiedList;
	}
}
