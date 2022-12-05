using AutoKkutuLib.Constants;

namespace AutoKkutuLib.Modules.AutoEntering;
public interface IAutoEnter
{
	event EventHandler<AutoEnterEventArgs>? AutoEntered;
	event EventHandler<InputDelayEventArgs>? InputDelayApply;
	event EventHandler? NoPathAvailable;

	bool CanPerformAutoEnterNow(PathFinderParameter? path);
	string? GetWordByIndex(IList<PathObject> qualifiedWordList, bool delayPerChar, int delay, int remainingTurnTime, int wordIndex = 0);
	void PerformAutoEnter(AutoEnterParameter parameter);
	void PerformAutoFix(IList<PathObject> availablePaths, AutoEnterParameter parameter, int remainingTurnTime);
}