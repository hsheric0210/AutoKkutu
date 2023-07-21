namespace AutoKkutuLib;

public record PathObject(string Content, WordCategories Categories, int MissionCharCount)
{
	public static readonly PathObject Empty = new("", WordCategories.None, 0);

	public bool AlreadyUsed { get; set; }
	public bool Excluded { get; set; }
	public bool RemoveQueued { get; set; }
}
