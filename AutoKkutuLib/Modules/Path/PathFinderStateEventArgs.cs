using AutoKkutuLib.Constants;

namespace AutoKkutuLib.Modules.Path;

public class PathFinderStateEventArgs : EventArgs
{
	public PathFinderState State
	{
		get;
	}

	public PathFinderStateEventArgs(PathFinderState state) => State = state;
}
