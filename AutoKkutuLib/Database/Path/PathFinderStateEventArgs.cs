namespace AutoKkutuLib.Database.Path;

public class PathFinderStateEventArgs : EventArgs
{
	public PathFinderState State
	{
		get;
	}

	public PathFinderStateEventArgs(PathFinderState state) => State = state;
}
