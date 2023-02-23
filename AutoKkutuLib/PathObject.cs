namespace AutoKkutuLib;

public record PathObject(string Content, WordCategories Categories, int MissionCharCount)
{
	public bool AlreadyUsed { get; set; }
	public bool Excluded { get; set; }
	public bool RemoveQueued { get; set; }
}
