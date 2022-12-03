using AutoKkutu.Constants;
using AutoKkutu.Modules.HandlerManager;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.AutoEnter
{
	[ModuleDependency(typeof(IHandlerManager))]
	public class AutoEnterCore : IAutoEnter
	{
		public event EventHandler<InputDelayEventArgs>? InputDelayApply;
		public event EventHandler? NoPathAvailable;
		public event EventHandler<AutoEnterEventArgs>? AutoEntered;

		public static Stopwatch InputStopwatch
		{
			get;
		} = new();

		private readonly IHandlerManager HandlerManager;
		private readonly InputSimulationCore InputSimulation;

		public int WordIndex
		{
			get; private set;
		}

		public AutoEnterCore(IHandlerManager handlerManager)
		{
			HandlerManager = handlerManager;
			InputSimulation = new InputSimulationCore(this, handlerManager);
		}

		public void ResetWordIndex() => WordIndex = 0;

		public void PerformAutoEnter(string content, PathFinderParameters path, string? pathAttribute = null)
		{
			if (content is null)
				throw new ArgumentNullException(nameof(content));

			if (pathAttribute is null)
				pathAttribute = I18n.Main_Optimal;

			if (AutoKkutuMain.Configuration.DelayEnabled && path.Options.HasFlag(PathFinderOptions.AutoFixed) != true)
			{
				int delay = GetDelay(content);
				InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, pathAttribute));
				Log.Debug(I18n.Main_WaitingSubmit, delay);

				Task.Run(async () =>
				{
					if (AutoKkutuMain.Configuration.DelayStartAfterCharEnterEnabled)
						await AutoEnterInputTimerTask(content, delay, path, pathAttribute);
					else
						await AutoEnterTask(content, delay, path, pathAttribute);
				});
			}
			else
				// Enter immediately
				PerformAutoEnterNow(content, path);
		}

		public void PerformAutoFix(IList<PathObject> paths, bool delayPerCharEnabled, int delayPerChar, int remainingTurnTime)
		{
			try
			{
				string? content = GetWordByIndex(paths, delayPerCharEnabled, delayPerChar, remainingTurnTime, ++WordIndex);
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
					PerformAutoEnterNow(content, null, I18n.Main_Next);
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.Main_PathSubmitException);
			}
		}

		public bool CanPerformAutoEnterNow(PathFinderParameters? path) => HandlerManager.IsGameStarted && HandlerManager.IsMyTurn && (path == null || HandlerManager.IsValidPath(path with { Options = path.Options | PathFinderOptions.AutoFixed }));

		private void PerformAutoEnterNow(string content, PathFinderParameters? path, string? pathAttribute = null)
		{
			if (pathAttribute == null)
				pathAttribute = I18n.Main_Optimal;

			if (!CanPerformAutoEnterNow(path))
				return;

			Log.Information(I18n.Main_AutoEnter, pathAttribute, content);

			HandlerManager.UpdateChat(content);
			HandlerManager.ClickSubmitButton();
			InputStopwatch.Restart();
			AutoEntered?.Invoke(this, new AutoEnterEventArgs(content));
		}

		private async Task AutoEnterTask(string content, int delay, PathFinderParameters path, string? pathAttributes = null)
		{
			await Task.Delay(delay);

			if (InputSimulation.CanSimulateInput())
			{
				await InputSimulation.PerformAutoEnterInputSimulation(content, path, delay / content.Length, pathAttributes);
				AutoEntered?.Invoke(this, new AutoEnterEventArgs(content));
			}
			else
			{
				PerformAutoEnterNow(content, path, pathAttributes);
			}
		}

		// ExtModules: InputSimulation
		private async Task AutoEnterInputTimerTask(string content, int delay, PathFinderParameters path, string? pathAttribute = null)
		{
			int _delay = 0;
			if (InputStopwatch.ElapsedMilliseconds <= delay)
			{
				_delay = (int)(delay - InputStopwatch.ElapsedMilliseconds);
				await Task.Delay(_delay);
			}

			if (InputSimulation.CanSimulateInput())
			{
				await InputSimulation.PerformAutoEnterInputSimulation(content, path, _delay / content.Length, pathAttribute);
				AutoEntered?.Invoke(this, new AutoEnterEventArgs(content));
			}
			else
			{
				PerformAutoEnterNow(content, path, pathAttribute);
			}
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

		public string? GetWordByIndex(IList<PathObject> qualifiedWordList, bool delayPerChar, int delay, int remainingTurnTime, int wordIndex = 0)
		{
			if (qualifiedWordList is null)
				throw new ArgumentNullException(nameof(qualifiedWordList));

			if (delayPerChar)
			{
				int remain = Math.Max(300, remainingTurnTime);
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
