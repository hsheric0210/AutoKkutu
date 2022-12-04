using AutoKkutu.Constants;
using System;
using System.Collections.Generic;

namespace AutoKkutu.Modules.Path;
public interface IPathFinder
{
	IList<PathObject> DisplayList { get; }
	IList<PathObject> QualifiedList { get; }

	event EventHandler<PathUpdateEventArgs>? OnPathUpdated;

	void Find(GameMode mode, PathFinderParameter param, WordPreference pref);
	void FindInternal(GameMode mode, PathFinderParameter param, WordPreference preference);
	void GenerateRandomPath(GameMode mode, PathFinderParameter param);
	void ResetFinalList();
}