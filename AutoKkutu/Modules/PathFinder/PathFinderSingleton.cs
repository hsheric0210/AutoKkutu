using AutoKkutu.Constants;
using System;
using System.Collections.Generic;

namespace AutoKkutu.Modules.PathFinder
{
	public static class PathFinder
	{
		private static readonly Lazy<PathFinderCore> _impl = new();
		private static PathFinderCore Impl => _impl.Value;

		public static IList<PathObject> DisplayList => Impl.DisplayList;

		public static IList<PathObject> QualifiedList => Impl.QualifiedList;

		public static event EventHandler<PathUpdateEventArgs>? OnPathUpdated
		{
			add => Impl.OnPathUpdated += value;
			remove => Impl.OnPathUpdated -= value;
		}

		public static void StartPathFinding(AutoKkutuConfiguration config, ResponsePresentedWord? word, string? missionChar, PathFinderOptions flags) => Impl.Find(config.GameMode, word, missionChar, config.ActiveWordPreference, flags);

		public static void FindPath(GameMode mode, ResponsePresentedWord word, string missionChar, WordPreference preference, PathFinderOptions options) => Impl.FindInternal(mode, word, missionChar, preference, options);

		//  might going to be deleted..?
		public static void ResetFinalList() => Impl.ResetFinalList();
	}
}
