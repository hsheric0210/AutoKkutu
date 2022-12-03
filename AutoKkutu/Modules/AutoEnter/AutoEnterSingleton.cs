using AutoKkutu.Constants;
using System;
using System.Collections.Generic;

namespace AutoKkutu.Modules.AutoEnter
{
	public static class AutoEnter
	{
		private static readonly Lazy<AutoEnterCore> _impl = new();
		private static AutoEnterCore Impl => _impl.Value;

		public static event EventHandler<InputDelayEventArgs>? EnterDelaying
		{
			add => Impl.InputDelayApply += value;
			remove => Impl.InputDelayApply -= value;
		}
		public static event EventHandler? PathNotFound
		{
			add => Impl.NoPathAvailable += value;
			remove => Impl.NoPathAvailable -= value;
		}
		public static event EventHandler<AutoEnterEventArgs>? AutoEntered
		{
			add => Impl.AutoEntered += value;
			remove => Impl.AutoEntered -= value;
		}

		public static int WordIndex => Impl.WordIndex;

		public static void ResetWordIndex() => Impl.ResetWordIndex();

		public static void PerformAutoEnter(string content, PathFinderParameters path , string? pathAttribute = null) => Impl.PerformAutoEnter(content, path, pathAttribute);

		public static void PerformAutoFix() => Impl.PerformAutoFix(PathFinder.PathFinder.QualifiedList, AutoKkutuMain.Configuration.DelayPerCharEnabled, AutoKkutuMain.Configuration.DelayInMillis, AutoKkutuMain.Handler?.TurnTimeMillis ?? 150000);

		public static bool CanPerformAutoEnterNow(PathFinderParameters path) => Impl.CanPerformAutoEnterNow(path);

		public static string? GetWordByIndex(IList<PathObject> qualifiedWordList, bool delayPerChar, int delay, int remainingTurnTime, int wordIndex = 0) => Impl.GetWordByIndex(qualifiedWordList, delayPerChar, delay, remainingTurnTime, wordIndex);
	}
}
