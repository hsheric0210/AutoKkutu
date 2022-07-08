using AutoKkutu.Utils;
using NLog;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoKkutu.Modules
{
	public static class InputSimulation
	{
		private static readonly Logger Logger = LogManager.GetLogger(nameof(InputSimulation));

		public static bool CanSimulateInput()
		{
			AutoKkutuConfiguration config = AutoKkutuMain.Configuration;
			return config is not null && config.DelayEnabled && config.DelayPerCharEnabled && config.InputSimulate;
		}

		public static async Task PerformAutoEnterInputSimulation(string content, PathUpdatedEventArgs? args, int delay, string? pathAttribute = null)
		{
			if (pathAttribute is null)
				pathAttribute = I18n.Main_Optimal;

			CommonHandler? handler = AutoKkutuMain.Handler;
			if (content is null || handler is null)
				return;

			bool aborted = false;
			var list = new List<(JamoType, char)>();
			foreach (var ch in content)
				list.AddRange(ch.SplitConsonants().Serialize());

			Logger.Info(CultureInfo.CurrentCulture, I18n.Main_InputSimulating, pathAttribute, content);
			handler.UpdateChat("");
			foreach ((JamoType type, char ch) in list)
			{
				if (!AutoEnter.CanPerformAutoEnterNow(args))
				{
					aborted = true; // Abort
					break;
				}
				handler.AppendChat(type, ch);
				await Task.Delay(delay);
			}

			if (aborted)
				Logger.Warn(CultureInfo.CurrentCulture, I18n.Main_InputSimulationAborted, pathAttribute, content);
			else
			{
				handler.ClickSubmitButton();
				Logger.Info(CultureInfo.CurrentCulture, I18n.Main_InputSimulationFinished, pathAttribute, content);
			}
			handler.UpdateChat("");
			AutoKkutuMain.UpdateStatusMessage(StatusMessage.AutoEntered, content);
		}

		public static async Task PerformInputSimulation(string message)
		{
			CommonHandler? handler = AutoKkutuMain.Handler;
			if (message is null || handler is null)
				return;

			var list = new List<(JamoType, char)>();
			foreach (var ch in message)
				list.AddRange(ch.SplitConsonants().Serialize());

			Logger.Info(CultureInfo.CurrentCulture, I18n.Main_InputSimulating, "Input", message);
			handler.UpdateChat("");
			foreach ((JamoType type, char ch) in list)
			{
				handler.AppendChat(type, ch);
				await Task.Delay(AutoKkutuMain.Configuration.DelayInMillis);
			}

			handler.ClickSubmitButton();
			handler.UpdateChat("");
			Logger.Info(CultureInfo.CurrentCulture, I18n.Main_InputSimulationFinished, "Input ", message);
		}
	}
}
