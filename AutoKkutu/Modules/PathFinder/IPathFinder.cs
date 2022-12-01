using AutoKkutu.Constants;
using System;
using System.Collections.Generic;

namespace AutoKkutu.Modules.PathFinder
{
	public interface IPathFinder
	{
		event EventHandler<PathUpdatedEventArgs>? OnPathUpdated;

		IList<PathObject> DisplayList
		{
			get;
		}

		IList<PathObject> QualifiedList
		{
			get;
		}

		void Find(GameMode mode, ResponsePresentedWord? word, string? missionChar, WordPreference pref, PathFinderOptions flags);
		void FindInternal(GameMode mode, ResponsePresentedWord word, string missionChar, WordPreference preference, PathFinderOptions options);
		void GenerateRandomPath(GameMode mode, ResponsePresentedWord word, string missionChar, PathFinderOptions options);
		void ResetFinalList();
	}
}