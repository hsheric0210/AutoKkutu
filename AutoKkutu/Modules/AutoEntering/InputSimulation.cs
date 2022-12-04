using AutoKkutu.Constants;
using AutoKkutu.Modules.AutoEnter.HangulProcessing;
using AutoKkutu.Modules.HandlerManager;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.AutoEnter
{
	[ModuleDependency(typeof(IHandlerManager))]
	public class InputSimulation
	{
		private readonly IAutoEnter autoEnter;
		private readonly IHandlerManager handlerManager;

		public InputSimulation(IAutoEnter autoenter, IHandlerManager handlerManager)
		{
			autoEnter = autoenter;
			this.handlerManager = handlerManager;
		}

		public async Task PerformInputSimulationAutoEnter(AutoEnterParameter parameter)
		{
			if (parameter is null)
				return;

			string content = parameter.Content;
			int wordIndex = parameter.WordIndex;
			bool aborted = false;
			var list = new List<(JamoType, char)>();
			foreach (var ch in content)
				list.AddRange(ch.SplitConsonants().Serialize());

			Log.Information(I18n.Main_InputSimulating, wordIndex, content);
			handlerManager.UpdateChat("");
			foreach ((JamoType type, char ch) in list)
			{
				if (!autoEnter.CanPerformAutoEnterNow(parameter.PathFinderParams))
				{
					aborted = true; // Abort
					break;
				}
				handlerManager.AppendChat(s => s.AppendChar(type, ch));
				await Task.Delay(parameter.DelayInMillis);
			}

			if (aborted)
				Log.Warning(I18n.Main_InputSimulationAborted, wordIndex, content);
			else
			{
				handlerManager.ClickSubmitButton();
				Log.Information(I18n.Main_InputSimulationFinished, wordIndex, content);
			}
			handlerManager.UpdateChat("");
		}

		public async Task PerformInputSimulation(string message, int delay)
		{
			if (message is null || handlerManager is null)
				return;

			var list = new List<(JamoType, char)>();
			foreach (var ch in message)
				list.AddRange(ch.SplitConsonants().Serialize());

			Log.Information(I18n.Main_InputSimulating, "Input", message);
			handlerManager.UpdateChat("");
			foreach ((JamoType type, char ch) in list)
			{
				handlerManager.AppendChat(s => s.AppendChar(type, ch));
				await Task.Delay(delay);
			}
			handlerManager.ClickSubmitButton();
			handlerManager.UpdateChat("");
			Log.Information(I18n.Main_InputSimulationFinished, "Input ", message);
		}
	}
}
