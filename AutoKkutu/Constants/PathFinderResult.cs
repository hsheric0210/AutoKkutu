namespace AutoKkutu.Constants
{
	public enum PathFindResult
	{
		Found,
		NotFound,
		Error
	}

	public sealed record PathFinderParameters(PresentedWord Word, string MissionChar, PathFinderOptions Options);
}
