using AutoKkutu.Constants;
using AutoKkutu.Modules.PathFinder;
using AutoKkutu.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.AutoEnter
{
	public class AutoEnterCore
	{
		public event EventHandler<InputDelayEventArgs>? InputDelayApply;
		public event EventHandler? NoPathAvailable;
		public event EventHandler<AutoEnterEventArgs>? AutoEnter;

		/* PUBLIC API  */

		public int WordIndex
		{
			get; private set;
		}

		public void ResetWordIndex() => WordIndex = 0;

		public void PerformAutoEnter(string content, PathUpdateEventArgs? args, string? pathAttribute = null)
		{
			if (content is null)
				throw new ArgumentNullException(nameof(content));

			if (pathAttribute is null)
				pathAttribute = I18n.Main_Optimal;

			if (AutoKkutuMain.Configuration.DelayEnabled && args?.Flags.HasFlag(PathFinderOptions.AutoFixed) != true)
			{
				int delay = GetDelay(content);
				InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, pathAttribute));
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
				PerformAutoEnterInternal(content, args);
		}

		public void PerformAutoFix(IList<PathObject> paths, bool delayPerCharEnabled, int delayPerChar, int? remainingTurnTime = null)
		{
			try
			{
				string? content = ApplyTimeFilter(delayPerCharEnabled, delayPerChar, paths, ++WordIndex, remainingTurnTime);
				if (content is null)
				{
					Log.Warning(I18n.Main_NoMorePathAvailable);
					NoPathAvailable?.Invoke(this, EventArgs.Empty);
					return;
				}

				if (AutoKkutuMain.Configuration.FixDelayEnabled)
				{
					int delay = GetFixDelay(content);
					InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, I18n.Main_Next));
					Log.Debug(I18n.Main_WaitingSubmitNext, delay);
					Task.Run(async () => await AutoEnterTask(content, delay, null, I18n.Main_Next));
				}
				else
					PerformAutoEnterInternal(content, null, I18n.Main_Next);
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.Main_PathSubmitException);
			}
		}

		// extmodules: Handler
		public bool CanPerformAutoEnterNow(PathUpdateEventArgs? args) => AutoKkutuMain.Handler.RequireNotNull().IsGameStarted && AutoKkutuMain.Handler.IsMyTurn && (args == null || AutoKkutuMain.CheckPathIsValid(args.Word, args.MissionChar, PathFinderOptions.AutoFixed));

		private void PerformAutoEnterInternal(string content, PathUpdateEventArgs? args, string? pathAttribute = null)
		{
			if (pathAttribute == null)
				pathAttribute = I18n.Main_Optimal;

			if (!CanPerformAutoEnterNow(args))
				return;

			Log.Information(I18n.Main_AutoEnter, pathAttribute, content);
			// FIXME: This module call should be handled with event 'AutoEnter' instead.
			// AutoKkutuMain.SendMessage(content, true);
			AutoEnter?.Invoke(this, new AutoEnterEventArgs(content));
		}

		private async Task AutoEnterTask(string content, int delay, PathUpdateEventArgs? args, string? pathAttributes = null)
		{
			await Task.Delay(delay);

			if (InputSimulation.CanSimulateInput())
				await InputSimulation.PerformAutoEnterInputSimulation(content, args, delay / content.Length, pathAttributes);
			else
				PerformAutoEnterInternal(content, args, pathAttributes);
		}

		// ExtModules: InputSimulation
		private async Task AutoEnterInputTimerTask(string content, int delay, PathUpdateEventArgs? args, string? pathAttribute = null)
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
				PerformAutoEnterInternal(content, args, pathAttribute);
		}

		private int GetDelay(string content)
		{
			int delay = AutoKkutuMain.Configuration.DelayInMillis;
			if (AutoKkutuMain.Configuration.DelayPerCharEnabled)
				delay *= content.Length;
			return delay;
		}

		private int GetFixDelay(string content)
		{
			int delay = AutoKkutuMain.Configuration.FixDelayInMillis;
			if (AutoKkutuMain.Configuration.FixDelayPerCharEnabled)
				delay *= content.Length;
			return delay;
		}

		// ExtModules: Handler
		private string? ApplyTimeFilter(bool enabled, int delay, IList<PathObject> qualifiedWordList, int wordIndex = 0, int? remainingTurnTime = null)
		{
			if (qualifiedWordList is null)
				throw new ArgumentNullException(nameof(qualifiedWordList));

			if (enabled)
			{
				int remain = Math.Max(300, remainingTurnTime ?? int.MaxValue);
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
}
