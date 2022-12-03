using AutoKkutu.Constants;
using System;
using System.Collections.Generic;

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

		bool CanPerformAutoEnterNow(PathFinderParameters? path);
		string? GetWordByIndex(IList<PathObject> qualifiedWordList, bool delayPerChar, int delay, int remainingTurnTime, int wordIndex = 0);
		void PerformAutoEnter(string content, PathFinderParameters path, string? pathAttribute = null);
		void PerformAutoFix(IList<PathObject> paths, bool delayPerCharEnabled, int delayPerChar, int remainingTurnTime);
		void ResetWordIndex();
	}
}