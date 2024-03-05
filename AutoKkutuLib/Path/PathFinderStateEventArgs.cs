namespace AutoKkutuLib.Path;

public class PathFinderStateEventArgs : EventArgs
{
	public PathFinderState State { get; }

	public Exception? Exception { get; }

	public PathFinderStateEventArgs(PathFinderState state) => State = state;

	public PathFinderStateEventArgs(Exception exception)
	{
		State = PathFinderState.Error;
		Exception = exception;
	}
}

public enum PathFinderState
{
	EndWord,
	Finding,
	Sorting,
	Error,
}
