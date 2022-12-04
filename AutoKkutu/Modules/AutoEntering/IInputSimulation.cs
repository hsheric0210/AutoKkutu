using AutoKkutu.Constants;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.AutoEnter;
public interface IInputSimulation
{
	bool CanSimulateInput();
	Task PerformAutoEnterInputSimulation(string content, PathFinderParameter? path, int delay, string? pathAttribute = null);
	Task PerformInputSimulation(string message, int delay);
}