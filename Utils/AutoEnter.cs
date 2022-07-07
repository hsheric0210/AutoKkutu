using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoKkutu.Utils
{
	public static class AutoEnter
	{
		// TODO: Remove those UpdateStatusMessage() calls and replace it with callback functions

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static int WordIndex
		{
			get; private set;
		}

		public static void ResetWordIndex() => WordIndex = 0;

		private static int GetDelay(string content)
		{
			int delay = AutoKkutuMain.Configuration.DelayInMillis;
			if (AutoKkutuMain.Configuration.DelayPerCharEnabled)
				delay *= content.Length;
			return delay;
		}

		public static void DelayedEnter(string content, UpdatedPathEventArgs? args, string? pathAttribute = null)
		{
			if (content is null)
				throw new ArgumentNullException(nameof(content));

			if (pathAttribute is null)
				pathAttribute = I18n.Main_Optimal;

			if (AutoKkutuMain.Configuration.DelayEnabled && args?.Flags.HasFlag(PathFinderOptions.AutoFixed) != true)
			{
				int delay = GetDelay(content);
				AutoKkutuMain.UpdateStatusMessage(StatusMessage.Delaying, delay);
				Logger.Debug(CultureInfo.CurrentCulture, I18n.Main_WaitingSubmit, delay);

				Task.Run(async () =>
				{
					if (AutoKkutuMain.Configuration.DelayStartAfterCharEnterEnabled)
						await AutoEnterTask2(content, delay, args, pathAttribute);
					else
						await AutoEnterTask(content, delay, args, pathAttribute);
				});
			}
			else
				// Enter immediately
				PerformAutoEnter(content, args);
		}


		/// <summary>
		/// Perform an automated enter
		/// </summary>
		/// <param name="content">Content to enter</param>
		/// <param name="args">Path arguments</param>
		/// <param name="pathAttribute">Explanation of auto-enter path</param>
		public static void PerformAutoEnter(string content, UpdatedPathEventArgs? args, string? pathAttribute = null)
		{
			if (pathAttribute == null)
				pathAttribute = I18n.Main_Optimal;

			if (!CanPerformAutoEnter(args))
				return;

			Logger.Info(CultureInfo.CurrentCulture, I18n.Main_AutoEnter, pathAttribute, content);
			AutoKkutuMain.SendMessage(content, true);
			AutoKkutuMain.UpdateStatusMessage(StatusMessage.AutoEntered, content);
		}

		public static bool CanPerformAutoEnter(UpdatedPathEventArgs? args) => AutoKkutuMain.Handler.RequireNotNull().IsGameStarted && AutoKkutuMain.Handler.IsMyTurn && (args == null || AutoKkutuMain.CheckPathIsValid(args.Word, args.MissionChar, PathFinderOptions.AutoFixed));

		private static async Task AutoEnterTask(string content, int delay, UpdatedPathEventArgs? args, string? pathAttributes = null)
		{
			await Task.Delay(delay);

			if (InputSimulation.CanSimulateInput())
				await InputSimulation.PerformAutoEnterInputSimulation(content, args, delay / content.Length, pathAttributes);
			else
				PerformAutoEnter(content, args, pathAttributes);
		}

		private static async Task AutoEnterTask2(string content, int delay, UpdatedPathEventArgs? args, string? pathAttribute = null)
		{
			int _delay = 0;
			if (AutoKkutuMain.InputStopwatch.ElapsedMilliseconds <= delay)
			{
				_delay = (int)(delay - AutoKkutuMain.InputStopwatch.ElapsedMilliseconds);
				await Task.Delay(_delay);
			}

			if (InputSimulation.CanSimulateInput())
				await InputSimulation.PerformAutoEnterInputSimulation(content, args, _delay / content.Length, pathAttribute);
			else
				PerformAutoEnter(content, args, pathAttribute);
		}

		public static void PerformAutoFix(Action<int>? delayCallback = null)
		{
			try
			{
				string? content = TimeFilterQualifiedWordListIndexed(PathFinder.QualifiedList, ++WordIndex);
				if (content is null)
				{
					Logger.Warn(I18n.Main_NoMorePathAvailable);
					AutoKkutuMain.UpdateStatusMessage(StatusMessage.NotFound);
					return;
				}

				if (AutoKkutuMain.Configuration.FixDelayEnabled)
				{
					// Setup delay
					int delay = AutoKkutuMain.Configuration.FixDelayInMillis;
					if (AutoKkutuMain.Configuration.FixDelayPerCharEnabled)
						delay *= content.Length;

					delayCallback?.Invoke(delay);
					Logger.Debug(CultureInfo.CurrentCulture, I18n.Main_WaitingSubmitNext, delay);
					Task.Run(async () => await AutoEnterTask(content, delay, null, I18n.Main_Next));
				}
				else
					PerformAutoEnter(content, null, I18n.Main_Next);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, I18n.Main_PathSubmitException);
			}
		}


		public static string? TimeFilterQualifiedWordList(IList<PathObject> qualifiedWordList)
		{
			if (qualifiedWordList is null)
				throw new ArgumentNullException(nameof(qualifiedWordList));

			if (AutoKkutuMain.Configuration.DelayPerCharEnabled)
			{
				int remain = Math.Max(300, AutoKkutuMain.Handler?.TurnTimeMillis ?? int.MaxValue);
				int delay = AutoKkutuMain.Configuration.DelayInMillis;
				string? word = qualifiedWordList.FirstOrDefault(po => po!.Content.Length * delay <= remain, null)?.Content;
				if (word == null)
					Logger.Debug(CultureInfo.CurrentCulture, I18n.TimeFilter_TimeOver, remain);
				else
					Logger.Debug(CultureInfo.CurrentCulture, I18n.TimeFilter_Success, remain, word.Length * delay);
				return word;
			}

			return qualifiedWordList[0].Content;
		}

		private static string? TimeFilterQualifiedWordListIndexed(IList<PathObject> qualifiedWordList, int wordIndex)
		{
			if (AutoKkutuMain.Configuration.DelayPerCharEnabled)
			{
				int remain = Math.Max(300, AutoKkutuMain.Handler?.TurnTimeMillis ?? int.MaxValue);
				int delay = AutoKkutuMain.Configuration.DelayInMillis;
				PathObject[] arr = qualifiedWordList.Where(po => po!.Content.Length * delay <= remain).ToArray();
				string? word = (arr.Length - 1 >= wordIndex) ? arr[wordIndex].Content : null;
				if (word == null)
					Logger.Debug(CultureInfo.CurrentCulture, I18n.TimeFilter_TimeOver, remain);
				else
					Logger.Debug(CultureInfo.CurrentCulture, I18n.TimeFilter_Success, remain, word.Length * delay);
				return word;
			}

			return qualifiedWordList.Count - 1 >= WordIndex ? qualifiedWordList[wordIndex].Content : null;
		}
	}
}
