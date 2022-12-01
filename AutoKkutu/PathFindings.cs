using AutoKkutu.Constants;
using AutoKkutu.Modules.AutoEnter;
using AutoKkutu.Modules.PathManager;
using AutoKkutu.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public static class PathFindings
	{

		// Modules: PathManager
		public static ICollection<string>? GetEndWordList(GameMode mode) => mode switch
		{
			GameMode.FirstAndLast => PathManager.ReverseEndWordList,
			GameMode.Kkutu => PathManager.KkutuEndWordList,
			_ => PathManager.EndWordList,
		};


		// Modules: Handler, PathFinder -> Multiple dependencies -> Implement with event pattern, call event from PathFinder
		public static bool CheckPathIsValid(AutoKkutuConfiguration config, CommonHandler handler, ResponsePresentedWord word, string missionChar, PathFinderOptions flags)
		{
			if (word is null || handler is null || flags.HasFlag(PathFinderOptions.ManualSearch))
				return true;

			bool differentWord = handler.CurrentPresentedWord != null && !word.Equals(handler.CurrentPresentedWord);
			bool differentMissionChar = config?.MissionAutoDetectionEnabled != false && !string.IsNullOrWhiteSpace(handler.CurrentMissionChar) && !string.Equals(missionChar, handler.CurrentMissionChar, StringComparison.OrdinalIgnoreCase);
			if (handler.IsMyTurn && (differentWord || differentMissionChar))
			{
				Log.Warning(I18n.PathFinder_InvalidatedUpdate, differentWord, differentMissionChar);
				StartPathFinding(config, handler.CurrentPresentedWord, handler.CurrentMissionChar, flags);
				return false;
			}
			return true;
		}

		// Modules: PathFinder
		/// <summary>
		/// Warning: Shouldn't use this method to send message directly as it applies undiscriminating delay any time.
		/// </summary>
		/// <param name="message"></param>
		// Modules: Handler, InputSimulation, InputStopwatch
		public static void SendMessage(string message, bool direct = false)
		{
			CommonHandler handler = Handler.RequireNotNull();
			if (!direct && InputSimulation.CanSimulateInput())
			{
				Task.Run(async () => await InputSimulation.PerformInputSimulation(message));
			}
			else
			{
				handler.UpdateChat(message);
				handler.ClickSubmitButton();
			}
			InputStopwatch.Restart();
		}

	}
}
