namespace AutoKkutuLib;

public readonly struct PathFinderParameter
{
	public static PathFinderParameter Empty { get; } = new PathFinderParameter(WordCondition.Empty, PathFinderFlags.None, false, 0);

	private readonly PathFinderFlags flags;

	public WordCondition Condition { get; }
	public bool ReuseAlreadyUsed { get; }
	public int MaxDisplayed { get; }

	public PathFinderParameter(WordCondition condition, PathFinderFlags flags, bool reuse, int maxDisplay)
	{
		this.flags = flags;

		Condition = condition;
		ReuseAlreadyUsed = reuse;
		MaxDisplayed = maxDisplay;
	}

	public static implicit operator WordCondition(PathFinderParameter param) => param.Condition;

	public bool HasFlag(PathFinderFlags flag) => flags.HasFlag(flag);
	public PathFinderParameter WithFlags(PathFinderFlags flags) => new(Condition, this.flags | flags, ReuseAlreadyUsed, MaxDisplayed);
	public PathFinderParameter WithoutFlags(PathFinderFlags flags) => new(Condition, this.flags & ~flags, ReuseAlreadyUsed, MaxDisplayed);
}
