using AutoKkutu.Modules.AutoEnter.HangulProcessing;
using AutoKkutu.Modules.HandlerManager;
using AutoKkutu.Modules.HandlerManager.Handler;
using AutoKkutu.Modules.PathFinder;
using AutoKkutu.Utils;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.AutoEnter
{
	[ModuleDependency(typeof(IHandlerManager))]
	public class InputSimulation
	{
		private readonly IHandlerManager Handler;

		public InputSimulation(IHandlerManager handler) => Handler = handler;

		public bool CanSimulateInput()
		{
			AutoKkutuConfiguration config = AutoKkutuMain.Configuration;
			return config is not null && config.DelayEnabled && config.DelayPerCharEnabled && config.InputSimulate;
		}

		public async Task PerformAutoEnterInputSimulation(string content, PathUpdateEventArgs? args, int delay, string? pathAttribute = null)
		{
			if (pathAttribute is null)
				pathAttribute = I18n.Main_Optimal;

			if (content is null || Handler is null)
				return;

			bool aborted = false;
			var list = new List<(JamoType, char)>();
			foreach (var ch in content)
				list.AddRange(ch.SplitConsonants().Serialize());

			Log.Information(I18n.Main_InputSimulating, pathAttribute, content);
			Handler.UpdateChat("");
			foreach ((JamoType type, char ch) in list)
			{
				if (!AutoEnter.CanPerformAutoEnterNow(args))
				{
					aborted = true; // Abort
					break;
				}
				Handler.AppendChat(type, ch);
				await Task.Delay(delay);
			}

			if (aborted)
				Log.Warning(I18n.Main_InputSimulationAborted, pathAttribute, content);
			else
			{
				Handler.ClickSubmitButton();
				Log.Information(I18n.Main_InputSimulationFinished, pathAttribute, content);
			}
			Handler.UpdateChat("");
			AutoKkutuMain.UpdateStatusMessage(StatusMessage.AutoEntered, content);
		}

		public async Task PerformInputSimulation(string message)
		{
			if (message is null || Handler is null)
				return;

			var list = new List<(JamoType, char)>();
			foreach (var ch in message)
				list.AddRange(ch.SplitConsonants().Serialize());

			Log.Information(I18n.Main_InputSimulating, "Input", message);
			Handler.UpdateChat("");
			foreach ((JamoType type, char ch) in list)
			{
				Handler.append(type, ch);
				await Task.Delay(AutoKkutuMain.Configuration.DelayInMillis);
			}
			Handler.ClickSubmitButton();
			Handler.UpdateChat("");
			Log.Information(I18n.Main_InputSimulationFinished, "Input ", message);
		}
	}
}
