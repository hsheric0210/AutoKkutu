using AutoKkutu.Constants;
using System;
using System.Collections.Generic;

namespace AutoKkutu.Modules.AutoEnter
{
	public static class AutoEnter
	{
		private static readonly Lazy<AutoEnterCore> _impl = new();
		public static IAutoEnter Instance => _impl.Value;

		public static event EventHandler<InputDelayEventArgs>? EnterDelaying
		{
			add => Instance.InputDelayApply += value;
			remove => Instance.InputDelayApply -= value;
		}
		public static event EventHandler? PathNotFound
		{
			add => Instance.NoPathAvailable += value;
			remove => Instance.NoPathAvailable -= value;
		}
		public static event EventHandler<AutoEnterEventArgs>? AutoEntered
		{
			add => Instance.AutoEntered += value;
			remove => Instance.AutoEntered -= value;
		}

		public static int WordIndex => Instance.WordIndex;

		public static void ResetWordIndex() => Instance.ResetWordIndex();

		public static void PerformAutoEnter(string content, PathFinderParameters path , string? pathAttribute = null) => Instance.PerformAutoEnter(content, path, pathAttribute);

		public static void PerformAutoFix() => Instance.PerformAutoFix(PathFinder.PathFinder.QualifiedList, AutoKkutuMain.Configuration.DelayPerCharEnabled, AutoKkutuMain.Configuration.DelayInMillis, AutoKkutuMain.Handler?.TurnTimeMillis ?? 150000);

		public static bool CanPerformAutoEnterNow(PathFinderParameters path) => Instance.CanPerformAutoEnterNow(path);

		public static string? GetWordByIndex(IList<PathObject> qualifiedWordList, bool delayPerChar, int delay, int remainingTurnTime, int wordIndex = 0) => Instance.GetWordByIndex(qualifiedWordList, delayPerChar, delay, remainingTurnTime, wordIndex);
	}
}
