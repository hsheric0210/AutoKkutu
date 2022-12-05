using AutoKkutuLib.Constants;
using AutoKkutuLib.Modules.HandlerManagement;
using Serilog;
using System.Diagnostics;

namespace AutoKkutuLib.Modules.AutoEntering;

[ModuleDependency(typeof(IHandlerManager))]
public class AutoEnter : IAutoEnter
{
	public event EventHandler<InputDelayEventArgs>? InputDelayApply;
	public event EventHandler? NoPathAvailable;
	public event EventHandler<AutoEnterEventArgs>? AutoEntered;

	public static Stopwatch InputStopwatch
	{
		get;
	} = new();

	private readonly IHandlerManager handlerManager;
	private readonly InputSimulation inputSimulation;

	public AutoEnter(IHandlerManager handlerManager)
	{
		this.handlerManager = handlerManager;
		inputSimulation = new InputSimulation(this, handlerManager);
	}

	public string? GetWordByIndex(IList<PathObject> qualifiedWordList, bool delayPerChar, int delay, int remainingTurnTime, int wordIndex = 0)
	{
		if (qualifiedWordList is null)
			throw new ArgumentNullException(nameof(qualifiedWordList));

		if (delayPerChar)
		{
			var remain = Math.Max(300, remainingTurnTime);
			PathObject[] arr = qualifiedWordList.Where(po => po!.Content.Length * delay <= remain).ToArray();
			var word = arr.Length <= wordIndex ? null : arr[wordIndex].Content;
			if (word == null)
				Log.Debug(I18n.TimeFilter_TimeOver, remain);
			else
				Log.Debug(I18n.TimeFilter_Success, remain, word.Length * delay);
			return word;
		}

		return qualifiedWordList.Count <= wordIndex ? null : qualifiedWordList[wordIndex].Content;
	}

	public bool CanPerformAutoEnterNow(PathFinderParameter? path) => handlerManager.IsGameStarted && handlerManager.IsMyTurn && (path == null || handlerManager.IsValidPath(path with { Options = path.Options | PathFinderOptions.AutoFixed }));

	#region AutoEnter initiator
	public void PerformAutoEnter(AutoEnterParameter parameter)
	{
		if (parameter is null)
			throw new ArgumentNullException(nameof(parameter));
		if (string.IsNullOrEmpty(parameter.Content))
			throw new ArgumentException("parameter.Content should not be empty", nameof(parameter));

		if (parameter.DelayEnabled && !parameter.PathFinderParams.Options.HasFlag(PathFinderOptions.AutoFixed))
		{
			var delay = parameter.RealDelay;
			InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, parameter.WordIndex));
			Log.Debug(I18n.Main_WaitingSubmit, delay);

			Task.Run(async () =>
			{
				if (parameter.DelayStartAfterCharEnterEnabled)
					await AutoEnterInputTimerTask(parameter);
				else
					await AutoEnterTask(parameter);
			});
		}
		else
		{
			// Enter immediately
			PerformAutoEnterNow(parameter.Content, parameter.PathFinderParams, parameter.WordIndex);
		}
	}

	public void PerformAutoFix(IList<PathObject> availablePaths, AutoEnterParameter parameter, int remainingTurnTime)
	{
		if (parameter is null)
			throw new ArgumentNullException(nameof(parameter));
		if (!string.IsNullOrEmpty(parameter.Content))
			throw new ArgumentException("parameter.Content should be empty", nameof(parameter));

		try
		{
			// TODO: move WordIndex incremental code out
			var content = GetWordByIndex(availablePaths, parameter.DelayPerCharEnabled, parameter.DelayInMillis, remainingTurnTime, parameter.WordIndex);
			if (content is null)
			{
				Log.Warning(I18n.Main_NoMorePathAvailable);
				NoPathAvailable?.Invoke(this, EventArgs.Empty);
				return;
			}

			AutoEnterParameter contentParameter = parameter with { Content = content };
			if (AutoKkutuMain.Configuration.FixDelayEnabled)
			{
				var delay = contentParameter.RealDelay;
				InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, parameter.WordIndex));
				Log.Debug(I18n.Main_WaitingSubmitNext, delay);
				Task.Run(async () => await AutoEnterTask(contentParameter));
			}
			else
			{
				PerformAutoEnterNow(content, null, parameter.WordIndex);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, I18n.Main_PathSubmitException);
		}
	}
	#endregion

	#region AutoEnter performer
	private void PerformAutoEnterNow(string content, PathFinderParameter? path, int pathIndex)
	{
		if (!CanPerformAutoEnterNow(path))
			return;

		Log.Information(I18n.Main_AutoEnter, pathIndex, content);

		handlerManager.UpdateChat(content);
		handlerManager.ClickSubmitButton();
		InputStopwatch.Restart();
		AutoEntered?.Invoke(this, new AutoEnterEventArgs(content));
	}
	#endregion

	#region AutoEnter task
	private async Task AutoEnterTask(AutoEnterParameter parameter)
	{
		await Task.Delay(parameter.RealDelay);

		if (parameter.CanSimulateInput)
		{
			await inputSimulation.PerformInputSimulationAutoEnter(parameter);
			AutoEntered?.Invoke(this, new AutoEnterEventArgs(parameter.Content));
		}
		else
		{
			PerformAutoEnterNow(parameter.Content, parameter.PathFinderParams, parameter.WordIndex);
		}
	}

	// ExtModules: InputSimulation
	private async Task AutoEnterInputTimerTask(AutoEnterParameter parameter)
	{
		var delay = parameter.RealDelay;
		var _delay = 0;
		if (InputStopwatch.ElapsedMilliseconds <= delay)
		{
			_delay = (int)(delay - InputStopwatch.ElapsedMilliseconds);
			await Task.Delay(_delay);
		}

		if (parameter.CanSimulateInput)
		{
			await inputSimulation.PerformInputSimulationAutoEnter(parameter);
			AutoEntered?.Invoke(this, new AutoEnterEventArgs(parameter.Content));
		}
		else
		{
			PerformAutoEnterNow(parameter.Content, parameter.PathFinderParams, parameter.WordIndex);
		}
	}
	#endregion
}
