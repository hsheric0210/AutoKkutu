using AutoKkutuLib.Constants;

namespace AutoKkutuLib.Modules.AutoEntering;
public interface IInputSimulation
{
	bool CanSimulateInput();
	Task PerformAutoEnterInputSimulation(string content, PathFinderParameter? path, int delay, string? pathAttribute = null);
	Task PerformInputSimulation(string message, int delay);
}