using System;
using System.Collections.Immutable;

namespace AutoKkutuGui;
public class PathListUpdateEventArgs : EventArgs
{
	public IImmutableList<GuiPathObject> GuiPathList;
	public PathListUpdateEventArgs(IImmutableList<GuiPathObject> pathList) => GuiPathList = pathList;
}