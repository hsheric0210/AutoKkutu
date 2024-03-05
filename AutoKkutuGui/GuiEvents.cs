using AutoKkutuLib;
using AutoKkutuLib.Path;
using System;
using System.Collections.Immutable;

namespace AutoKkutuGui;
public class PathListUpdateEventArgs : EventArgs
{
	public IImmutableList<GuiPathObject> GuiPathList;
	public PathListUpdateEventArgs(IImmutableList<GuiPathObject> pathList) => GuiPathList = pathList;
}

public class PathFindResultUpdateEventArgs : EventArgs
{
	public PathFindResult Arguments { get; }

	public PathFindResultUpdateEventArgs(PathFindResult arguments) => Arguments = arguments;
}

public class StatusMessageChangedEventArgs : EventArgs
{
	private readonly object?[] formatterArguments;

	public StatusMessage Status { get; }

	public object?[] GetFormatterArguments() => formatterArguments;

	public StatusMessageChangedEventArgs(StatusMessage status, params object?[] formatterArgs)
	{
		Status = status;
		formatterArguments = formatterArgs;
	}
}

public class AutoKkutuInitializedEventArgs : EventArgs
{
	public AutoKkutu Instance { get; }
	public AutoKkutuInitializedEventArgs(AutoKkutu instance) => Instance = instance;
}