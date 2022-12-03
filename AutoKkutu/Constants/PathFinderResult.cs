namespace AutoKkutu.Constants
{
	public enum PathType
	{
		Found,
		NotFound,
		Error
	}

	public sealed record PathFound(PresentedWord Word, string MissionChar, PathFinderOptions Options);
}
