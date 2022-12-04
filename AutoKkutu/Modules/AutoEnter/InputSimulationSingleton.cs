using AutoKkutu.Constants;
using AutoKkutu.Modules.AutoEnter.HangulProcessing;
using AutoKkutu.Modules.HandlerManager;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.AutoEnter
{
	[ModuleDependency(typeof(IHandlerManager))]
	public static class InputSimulation
	{
		private static readonly Lazy<InputSimulationCore> _impl = new Lazy<InputSimulationCore>(() => new InputSimulationCore(AutoEnter.Instance, HandlerManager.Impl));
		private static IInputSimulation Impl => _impl.Value;

		public InputSimulationCore(IAutoEnter autoenter, IHandlerManager handler)
		{
			AutoEnter = autoenter;
			Handler = handler;
		}

		public bool CanSimulateInput()
		{
			AutoKkutuConfiguration config = AutoKkutuMain.Configuration;
			return config is not null && config.DelayEnabled && config.DelayPerCharEnabled && config.InputSimulate;
		}

		public async Task PerformAutoEnterInputSimulation(string content, PathFinderParameters? path, int delay, string? pathAttribute = null)
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
				if (!AutoEnter.CanPerformAutoEnterNow(path))
				{
					aborted = true; // Abort
					break;
				}
				Handler.AppendChat(s => s.AppendChar(type, ch));
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
		}

		public async Task PerformInputSimulation(string message, int delay)
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
				Handler.AppendChat(s => s.AppendChar(type, ch));
				await Task.Delay(delay);
			}
			Handler.ClickSubmitButton();
			Handler.UpdateChat("");
			Log.Information(I18n.Main_InputSimulationFinished, "Input ", message);
		}
	}
}
