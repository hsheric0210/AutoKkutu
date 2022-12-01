using AutoKkutu.Modules.PathFinder;
using System;

namespace AutoKkutu.Modules.AutoEnter
{
	public static class AutoEnter
	{
		private static readonly Lazy<AutoEnterCore> _impl = new();
		private static AutoEnterCore Impl => _impl.Value;

		public static event EventHandler<EnterDelayingEventArgs>? EnterDelaying
		{
			add => Impl.EnterDelaying += value;
			remove => Impl.EnterDelaying -= value;
		}
		public static event EventHandler? PathNotFound
		{
			add => Impl.PathNotFound += value;
			remove => Impl.PathNotFound -= value;
		}
		public static event EventHandler<AutoEnteredEventArgs>? AutoEntered
		{
			add => Impl.AutoEntered += value;
			remove => Impl.AutoEntered -= value;
		}

		public static int WordIndex => Impl.WordIndex;

		public static void ResetWordIndex() => Impl.ResetWordIndex();

		public static void PerformAutoEnter(string content, PathUpdatedEventArgs? args, string? pathAttribute = null) => Impl.PerformAutoEnter(content, args, pathAttribute);

		public static void PerformAutoFix() => Impl.PerformAutoFix(PathFinder.PathFinder.QualifiedList, AutoKkutuMain.Configuration.DelayPerCharEnabled, AutoKkutuMain.Configuration.DelayInMillis, AutoKkutuMain.Handler?.TurnTimeMillis);

		public static bool CanPerformAutoEnterNow(PathUpdatedEventArgs? args) => Impl.CanPerformAutoEnterNow(args);
	}
}
