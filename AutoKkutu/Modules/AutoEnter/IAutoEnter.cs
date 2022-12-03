using AutoKkutu.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AutoKkutu.Modules.AutoEnter
{
	public interface IAutoEnter
	{
		int WordIndex
		{
			get;
		}

		event EventHandler<AutoEnterEventArgs>? AutoEntered;
		event EventHandler<InputDelayEventArgs>? InputDelayApply;
		event EventHandler? NoPathAvailable;

		bool CanPerformAutoEnterNow([NotNull] PathFound path);
		void PerformAutoEnter(string content, PathFound path, string? pathAttribute = null);
		void PerformAutoFix(IList<PathObject> paths, bool delayPerCharEnabled, int delayPerChar, int? remainingTurnTime = null);
		void ResetWordIndex();
	}
}