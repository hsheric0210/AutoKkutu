using AutoKkutuLib.Extension;
using AutoKkutuLib.Hangul;
using Serilog;
using System.Collections.Immutable;
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

	public bool CanPerformAutoEnterNow(PathFinderParameter? param) => game.IsGameInProgress && game.IsMyTurn && (param is not PathFinderParameter _param || game.CheckPathExpired(_param.WithFlags(PathFinderFlags.DryRun | PathFinderFlags.NoRescan)));

	#region AutoEnter starter
	/// <summary>
	/// 자동 입력 수행을 요청합니다
	/// </summary>
	/// <param name="param">자동 입력 옵션; <c>param.Content</c>는 반드시 설정되어 있어야 합니다</param>
	/// <exception cref="ArgumentException"><c>param.Content</c>가 설정되어 있지 않을 때 발생</exception>
	/// TODO: 'PerformAutoEnter' and 'PerformAutoFix' has multiple duplicate codes, these two could be possibly merged. (+ If then, remove 'content' property from AutoEnterParameter)
	public void PerformAutoEnter(AutoEnterParameter param)
	{
		if (string.IsNullOrWhiteSpace(param.Content))
			throw new ArgumentException("Content to auto-enter is not provided", nameof(param));

		if (param.DelayParameter.DelayEnabled && !param.HasFlag(PathFinderFlags.DryRun))
		{
			var delay = param.RealDelay;
			InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, param.WordIndex));
			Log.Debug(I18n.Main_WaitingSubmit, delay);

			Task.Run(async () =>
			{
				if (param.DelayParameter.DelayStartAfterCharEnterEnabled)
					await AutoEnterDynamicDelayTask(param);
				else
					await AutoEnterDelayTask(param);
			});
		}
		else
		{
			// Enter immediately
			PerformAutoEnterNow(param.Content, param.PathInfo, param.WordIndex);
		}
	}

	/// <summary>
	/// 틀린 단어 자동 수정을 요청합니다
	/// </summary>
	/// <param name="availablePaths">사용 가능한 단어들의 목록</param>
	/// <param name="parameter">자동 입력 옵션; <c>parameter.Content</c>는 설정되어 있지 않아도 됩니다.</param>
	/// <param name="remainingTurnTime">남은 턴 시간</param>
	public void PerformAutoFix(IImmutableList<PathObject> availablePaths, AutoEnterParameter parameter, int remainingTurnTime)
	{
		try
		{
			(var content, var timeover) = availablePaths.ChooseBestWord(parameter.DelayParameter, remainingTurnTime, parameter.WordIndex);
			if (string.IsNullOrEmpty(content))
			{
				Log.Warning(I18n.Main_NoMorePathAvailable);
				NoPathAvailable?.Invoke(this, new NoPathAvailableEventArgs(timeover, remainingTurnTime));
				return;
			}

			AutoEnterParameter contentParameter = parameter with { Content = content };
			if (parameter.DelayParameter.DelayEnabled)
			{
				// Run asynchronously
				var delay = contentParameter.RealDelay;
				InputDelayApply?.Invoke(this, new InputDelayEventArgs(delay, parameter.WordIndex));
				Log.Debug(I18n.Main_WaitingSubmitNext, delay);
				Task.Run(async () => await AutoEnterDelayTask(contentParameter));
			}
			else
			{
				// Run synchronously
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
			PerformAutoEnterNow(parameter.Content, parameter.PathInfo, parameter.WordIndex);
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
			PerformAutoEnterNow(parameter.Content, parameter.PathInfo, parameter.WordIndex);
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
			if (!CanPerformAutoEnterNow(parameter.PathInfo))
			{
				aborted = true; // Abort
				break;
			}
			game.AppendChat(s => s.SimulateAppend(type, ch));
			await Task.Delay(parameter.DelayParameter.DelayInMillis);
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
