namespace AutoKkutuLib;

public struct PathObject
{
	public static readonly PathObject Empty = new() { Content = "", Categories = WordCategories.None, MissionCharCount = 0, Marks = PathMarks.None };

	public readonly string Content { get; init; }
	public readonly WordCategories Categories { get; init; }
	public readonly int MissionCharCount { get; init; } // fixme: why this is required?
	public PathMarks Marks { get; set; }

	public void UpdateMarks(PathMarks marks) => Marks = marks; // Hope this CS1612 workaround works. If not, another tricky bug occurs.
}

[Flags]
public enum PathMarks
{
	None = 0,
	AlreadyUsed = 1 << 0,
	Excluded = 1 << 1,
	RemoveQueued = 1 << 2,
}
