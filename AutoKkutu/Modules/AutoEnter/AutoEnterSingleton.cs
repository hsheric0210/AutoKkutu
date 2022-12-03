using AutoKkutu.Modules.PathFinder;
using System;

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
			add => Impl.AutoEnter += value;
			remove => Impl.AutoEnter -= value;
		}

		public static int WordIndex => Impl.WordIndex;

		public static void ResetWordIndex() => Impl.ResetWordIndex();

		public static void PerformAutoEnter(string content, PathUpdateEventArgs? args, string? pathAttribute = null) => Impl.PerformAutoEnter(content, args, pathAttribute);

		public static void PerformAutoFix() => Impl.PerformAutoFix(PathFinder.PathFinder.QualifiedList, AutoKkutuMain.Configuration.DelayPerCharEnabled, AutoKkutuMain.Configuration.DelayInMillis, AutoKkutuMain.Handler?.TurnTimeMillis);

		public static bool CanPerformAutoEnterNow(PathUpdateEventArgs? args) => Impl.CanPerformAutoEnterNow(args);
	}
}
