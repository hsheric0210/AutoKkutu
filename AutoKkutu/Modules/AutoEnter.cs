using AutoKkutu.Constants;
using AutoKkutu.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoKkutu.Modules
{
	public static class AutoEnter
	{
		public static event EventHandler<EnterDelayingEventArgs>? EnterDelaying;
		public static event EventHandler? PathNotFound;
		public static event EventHandler<AutoEnteredEventArgs>? AutoEntered;

		/* PUBLIC API  */

		public static int WordIndex
		{
			get; private set;
		}

		public static void ResetWordIndex() => WordIndex = 0;

		public static void PerformAutoEnter(string content, PathUpdatedEventArgs? args, string? pathAttribute = null)
		{
			if (content is null)
				throw new ArgumentNullException(nameof(content));

			if (pathAttribute is null)
				pathAttribute = I18n.Main_Optimal;

			if (AutoKkutuMain.Configuration.DelayEnabled && args?.Flags.HasFlag(PathFinderOptions.AutoFixed) != true)
			{
				int delay = GetDelay(content);
				EnterDelaying?.Invoke(null, new EnterDelayingEventArgs(delay, pathAttribute));
				Log.Debug(I18n.Main_WaitingSubmit, delay);

				Task.Run(async () =>
				{
					if (AutoKkutuMain.Configuration.DelayStartAfterCharEnterEnabled)
						await AutoEnterInputTimerTask(content, delay, args, pathAttribute);
					else
						await AutoEnterTask(content, delay, args, pathAttribute);
				});
			}
			else
				// Enter immediately
				PerformAutoEnterImmediately(content, args);
		}

		public static void PerformAutoFix()
		{
			try
			{
				string? content = ApplyTimeFilter(PathFinder.QualifiedList, ++WordIndex);
				if (content is null)
				{
					Log.Warning(I18n.Main_NoMorePathAvailable);
					PathNotFound?.Invoke(null, EventArgs.Empty);
					return;
				}

				if (AutoKkutuMain.Configuration.FixDelayEnabled)
				{
					int delay = GetFixDelay(content);
					EnterDelaying?.Invoke(null, new EnterDelayingEventArgs(delay, I18n.Main_Next));
					Log.Debug(I18n.Main_WaitingSubmitNext, delay);
					Task.Run(async () => await AutoEnterTask(content, delay, null, I18n.Main_Next));
				}
				else
					PerformAutoEnterImmediately(content, null, I18n.Main_Next);
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.Main_PathSubmitException);
			}
		}

		public static bool CanPerformAutoEnterNow(PathUpdatedEventArgs? args) => AutoKkutuMain.Handler.RequireNotNull().IsGameStarted && AutoKkutuMain.Handler.IsMyTurn && (args == null || AutoKkutuMain.CheckPathIsValid(args.Word, args.MissionChar, PathFinderOptions.AutoFixed));

		/* INTERNAL-USE API */

		private static void PerformAutoEnterImmediately(string content, PathUpdatedEventArgs? args, string? pathAttribute = null)
		{
			if (pathAttribute == null)
				pathAttribute = I18n.Main_Optimal;

			if (!CanPerformAutoEnterNow(args))
				return;

			Log.Information(I18n.Main_AutoEnter, pathAttribute, content);
			AutoKkutuMain.SendMessage(content, true);
			AutoEntered?.Invoke(null, new AutoEnteredEventArgs(content));
		}

		private static async Task AutoEnterTask(string content, int delay, PathUpdatedEventArgs? args, string? pathAttributes = null)
		{
			await Task.Delay(delay);

			if (InputSimulation.CanSimulateInput())
				await InputSimulation.PerformAutoEnterInputSimulation(content, args, delay / content.Length, pathAttributes);
			else
				PerformAutoEnterImmediately(content, args, pathAttributes);
		}

		private static async Task AutoEnterInputTimerTask(string content, int delay, PathUpdatedEventArgs? args, string? pathAttribute = null)
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
				PerformAutoEnterImmediately(content, args, pathAttribute);
		}

		private static int GetDelay(string content)
		{
			int delay = AutoKkutuMain.Configuration.DelayInMillis;
			if (AutoKkutuMain.Configuration.DelayPerCharEnabled)
				delay *= content.Length;
			return delay;
		}

		private static int GetFixDelay(string content)
		{
			int delay = AutoKkutuMain.Configuration.FixDelayInMillis;
			if (AutoKkutuMain.Configuration.FixDelayPerCharEnabled)
				delay *= content.Length;
			return delay;
		}

		public static string? ApplyTimeFilter(IList<PathObject> qualifiedWordList, int wordIndex = 0)
		{
			if (qualifiedWordList is null)
				throw new ArgumentNullException(nameof(qualifiedWordList));

			if (AutoKkutuMain.Configuration.DelayPerCharEnabled)
			{
				int remain = Math.Max(300, AutoKkutuMain.Handler?.TurnTimeMillis ?? int.MaxValue);
				int delay = AutoKkutuMain.Configuration.DelayInMillis;
				PathObject[] arr = qualifiedWordList.Where(po => po!.Content.Length * delay <= remain).ToArray();
				string? word = arr.Length - 1 >= wordIndex ? arr[wordIndex].Content : null;
				if (word == null)
					Log.Debug(I18n.TimeFilter_TimeOver, remain);
				else
					Log.Debug(I18n.TimeFilter_Success, remain, word.Length * delay);
				return word;
			}

			return qualifiedWordList.Count - 1 >= wordIndex ? qualifiedWordList[wordIndex].Content : null;
		}
	}

	public class EnterDelayingEventArgs : EventArgs
	{
		public int Delay
		{
			get;
		}

		public string? PathAttributes
		{
			get;
		}

		public EnterDelayingEventArgs(int delay, string? pathAttributes = null)
		{
			Delay = delay;
			PathAttributes = pathAttributes;
		}
	}

	public class AutoEnteredEventArgs : EventArgs
	{
		public string Content
		{
			get;
		}

		public AutoEnteredEventArgs(string content)
		{
			Content = content;
		}
	}
}
