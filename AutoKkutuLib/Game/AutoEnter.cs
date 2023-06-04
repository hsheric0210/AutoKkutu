using AutoKkutuLib.Extension;
using AutoKkutuLib.Hangul;
using Serilog;
using System.Diagnostics;

namespace AutoKkutuLib.Game;

public class AutoEnter
{
	#region Events
	public event EventHandler<InputDelayEventArgs>? InputDelayApply;
	public event EventHandler<AutoEnterEventArgs>? AutoEntered;
	public event EventHandler<NoPathAvailableEventArgs>? NoPathAvailable;
	#endregion

	public static Stopwatch InputStopwatch
	{
		get;
	} = new();

	private readonly IGame game;

	public AutoEnter(IGame game) => this.game = game;

	public bool CanPerformAutoEnterNow(PathFinderParameter? path) => game.IsGameInProgress && game.IsMyTurn && (path == null || game.CheckPathExpired(path with { Options = path.Options | PathFinderFlags.DryRun }));

	#region AutoEnter starter
	// TODO: 'PerformAutoEnter' and 'PerformAutoFix' has multiple duplicate codes, these two could be possibly merged. (+ If then, remove 'content' property from AutoEnterParameter)
	public void PerformAutoEnter(AutoEnterParameter parameter)
	{
		if (parameter is null)
			throw new ArgumentNullException(nameof(parameter));
		if (string.IsNullOrEmpty(parameter.Content))
			throw new ArgumentException("parameter.Content should not be empty", nameof(parameter));

		if (parameter.DelayEnabled && parameter.PathFinderParams?.Options.HasFlag(PathFinderFlags.DryRun) != true)
		{
			var delay = parameter.RealDelay;
			InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, parameter.WordIndex));
			Log.Debug(I18n.Main_WaitingSubmit, delay);

			Task.Run(async () =>
			{
				if (parameter.DelayStartAfterCharEnterEnabled)
					await AutoEnterDynamicDelayTask(parameter);
				else
					await AutoEnterDelayTask(parameter);
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

		try
		{
			(var content, var timeover) = availablePaths.GetWordByIndex(parameter.DelayPerCharEnabled, parameter.DelayInMillis, remainingTurnTime, parameter.WordIndex);
			if (string.IsNullOrEmpty(content))
			{
				Log.Warning(I18n.Main_NoMorePathAvailable);
				NoPathAvailable?.Invoke(this, new NoPathAvailableEventArgs(timeover, remainingTurnTime));
				return;
			}

			AutoEnterParameter contentParameter = parameter with { Content = content };
			if (parameter.DelayEnabled)
			{
				var delay = contentParameter.RealDelay;
				InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, parameter.WordIndex));
				Log.Debug(I18n.Main_WaitingSubmitNext, delay);
				Task.Run(async () => await AutoEnterDelayTask(contentParameter));
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

	#region AutoEnter delay task proc.
	private async Task AutoEnterDelayTask(AutoEnterParameter parameter)
	{
		var delay = parameter.RealDelay;
		var delayBetweenInput = (int)(delay - InputStopwatch.ElapsedMilliseconds);
		delay = Math.Max(delay, delayBetweenInput); // Failsafe to prevent way-too-fast input
		Log.Information("Waiting: max(delay: {delay}, delayBetweenInput: {delayBetweenInput}) = {realDelay}ms", parameter.RealDelay, delayBetweenInput, delay);
		await Task.Delay(delay);

		if (parameter.CanSimulateInput)
		{
			await PerformInputSimulationAutoEnter(parameter);
			AutoEntered?.Invoke(this, new AutoEnterEventArgs(parameter.Content));
		}
		else
		{
			PerformAutoEnterNow(parameter.Content, parameter.PathFinderParams, parameter.WordIndex);
		}
	}

	private async Task AutoEnterDynamicDelayTask(AutoEnterParameter parameter)
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
			await PerformInputSimulationAutoEnter(parameter);
			AutoEntered?.Invoke(this, new AutoEnterEventArgs(parameter.Content));
		}
		else
		{
			PerformAutoEnterNow(parameter.Content, parameter.PathFinderParams, parameter.WordIndex);
		}
	}
	#endregion

	#region AutoEnter performer
	private void PerformAutoEnterNow(string content, PathFinderParameter? path, int pathIndex)
	{
		if (!CanPerformAutoEnterNow(path))
			return;

		Log.Information(I18n.Main_AutoEnter, pathIndex, content);

		game.UpdateChat(content);
		game.ClickSubmitButton();
		InputStopwatch.Restart();
		AutoEntered?.Invoke(this, new AutoEnterEventArgs(content));
	}
	#endregion

	#region Input simulation

	public async Task PerformInputSimulationAutoEnter(AutoEnterParameter parameter)
	{
		if (parameter is null)
			return;

		var content = parameter.Content;
		var wordIndex = parameter.WordIndex;
		var aborted = false;
		var list = new List<(JamoType, char)>();
		foreach (var ch in content)
			list.AddRange(ch.Split().Serialize());

		Log.Information(I18n.Main_InputSimulating, wordIndex, content);
		game.UpdateChat("");
		foreach ((JamoType type, var ch) in list)
		{
			if (!CanPerformAutoEnterNow(parameter.PathFinderParams))
			{
				aborted = true; // Abort
				break;
			}
			game.AppendChat(s => s.SimulateAppend(type, ch));
			await Task.Delay(parameter.DelayInMillis);
		}

		if (aborted)
			Log.Warning(I18n.Main_InputSimulationAborted, wordIndex, content);
		else
		{
			game.ClickSubmitButton();
			Log.Information(I18n.Main_InputSimulationFinished, wordIndex, content);
		}
		game.UpdateChat("");
	}

	public async Task PerformInputSimulation(string message, int delay)
	{
		if (message is null)
			return;

		var list = new List<(JamoType, char)>();
		foreach (var ch in message)
			list.AddRange(ch.Split().Serialize());

		Log.Information(I18n.Main_InputSimulating, "Input", message);
		game.UpdateChat("");
		foreach ((JamoType type, var ch) in list)
		{
			game.AppendChat(s => s.SimulateAppend(type, ch));
			await Task.Delay(delay);
		}
		game.ClickSubmitButton();
		game.UpdateChat("");
		Log.Information(I18n.Main_InputSimulationFinished, "Input ", message);
	}
	#endregion
}
