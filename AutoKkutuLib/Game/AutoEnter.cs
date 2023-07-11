using AutoKkutuLib.Extension;
using AutoKkutuLib.Hangul;
using Serilog;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace AutoKkutuLib.Game;

// TODO: As it contains all of the game 'input' features, rename this class to 'GameInput' instead of 'AutoEnter'
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

	/// <summary>
	/// 현재 자동 입력이 진행 중인지(딜레이를 기다리는 중인지, 또는 키보드 입력 시뮬레이션 중인지) 상태를 나타냅니다.
	/// </summary>
	private volatile bool isInputInProgress;

	/// <summary>
	/// 현재 진행 중인 입력 시뮬레이션이 Pre-search 기능에 의한 것인지에 대한 여부를 나타냅니다.
	/// </summary>
	/// <remarks>
	/// Pre-search 기능에 의한 자동 입력에서 자동입력 완료 후 자동 전송을 막기 위해 존재합니다.
	/// (Pre-search 기능에 의한 자동 입력이 진행되는 시점에서는 아직 내 턴이 오지 않았을 확률이 있기 때문입니다)
	/// </remarks>
	private volatile bool isPreinputSimInProg;

	/// <summary>
	/// 마지막으로 완료된 입력 시뮬레이션이 Pre-search 기능에 의한 것인지에 대한 여부를 나타냅니다.
	/// </summary>
	/// <remarks>
	/// 해당 검사는 '만약 현재 입력 시뮬레이션을 수행할 단어가 마지막으로 완료된 입력 시뮬레이션과 동일하다면
	/// 굳이 다시 입력 시뮬레이션을 수행는 대신 그냥 전송 버튼만 누르면 되는' 것과 같은 최적화가 가능하기에 필수적입니다.
	/// </remarks>
	private bool isPreinputFinished;

	/// <summary>
	/// SHIFT 키가 눌린 (것과 같은 상태로 취급하는) 상태라면 <c>true</c>, 아니라면 <c>false</c>.
	/// </summary>
	private bool shiftState;

	/// <summary>
	/// 마지막으로 완료된(또는 현재 진행 중인) 입력 시뮬레이션에 패스파인더 매개 변수입니다.
	/// </summary>
	/// <remarks>
	/// <para>
	/// 만약 새로 진행하려는 입력 시뮬레이션과 마지막으로 진행된(또는 진행 중인) 입력 시뮬레이션의 매개 변수가 같다면,
	/// 굳이 현재 진행 중인 입력 시뮬레이션을 취소하고 처음부터 다시 진행할 필요 없는 것처럼 최적화가 가능합니다.
	/// </para>
	/// <para>
	/// 또한, 위 경우와 함께 만약 마지막으로 진행된(또는 현재 진행 중인) 입력 시뮬레이션이 Pre-search 기능에 의한 것이라면,
	/// 그냥 <c>isPreinputSimInProg</c>를 <c>false</c>로 설정해 줌으로써 그냥 끝났을 때 전송 버튼만 눌러주는 식으로 더욱 최적화할 수 있습니다.
	/// </para>
	/// </remarks>
	private PathDetails lastPreinputPath = PathDetails.Empty;

	public AutoEnter(IGame game) => this.game = game;

	/// <summary>
	/// 현재 자동 입력을 계속 이어나갈 수 있는지의 여부를 반환합니다.
	/// 만약 단어 조건이 바뀌는 등의 변화가 일어났다면 <c>false</c>를 반환하고 단어 재검색을 수행합니다.
	/// </summary>
	/// <param name="detail">검색 조건 상세 내용</param>
	/// <param name="checkTurn">현재 턴이 내 턴인지 검사 수행 여부</param>
	/// <returns>자동 입력을 계속 이어나갈 수 있는지의 여부</returns>
	private bool CanPerformAutoEnterNow(PathDetails? detail, bool checkTurn = true)
	{
		if (detail is PathDetails detail2 && detail2.HasFlag(PathFlags.ManualMessage))
			return true;

		// Game is already ended
		if (!game.IsGameInProgress)
			return false;

		// Not my turn
		if (checkTurn && !game.IsMyTurn)
			return false;

		// Path is expired
		if (detail is PathDetails detail3 && game.RescanIfPathExpired(detail3.WithFlags(PathFlags.DryRun)))
			return false;

		return true;
	}

	/// <summary>
	/// 자동 입력 수행을 요청합니다
	/// </summary>
	/// <param name="param">자동 입력 옵션; <c>param.Content</c>는 반드시 설정되어 있어야 합니다</param>
	/// <exception cref="ArgumentException"><c>param.Content</c>가 설정되어 있지 않을 때 발생</exception>
	// TODO: Rename to 'Send'
	public void PerformAutoEnter(AutoEnterInfo param)
	{
		if (string.IsNullOrWhiteSpace(param.Content))
			throw new ArgumentException("Content to auto-enter is not provided", nameof(param));
		Log.Verbose("AutoEnter request received: {request}", param);

		var pre = param.HasFlag(PathFlags.PreSearch);
		if (param.Options.DelayEnabled && !param.HasFlag(PathFlags.DryRun))
		{
			if (pre)
			{
				lastPreinputPath = param;
			}
			else
			{
				// Pre-search 입력 시뮬레이션 관련 최적화들 (자세한 내용은 위 필드들 설명 참조)
				if (lastPreinputPath.IsSimilar(param))
				{
					if (isPreinputFinished) // Pre-search 입력 시뮬레이션 완료 시
					{
						CheckAndSubmit(CanPerformAutoEnterNow(param)); // 자동으로 전송
						isPreinputSimInProg = isPreinputFinished = false;
						return;
					}
					if (isInputInProgress && isPreinputSimInProg) // Pre-search 입력 시뮬레이션 진행 중일 시
					{
						isPreinputSimInProg = false; // 현재 진행 중인 이른 자동 입력을 정규 자동 입력으로 승격. 이를 통해 해당 이른 자동 입력 완료 시 자동 전송 기능 활성화됨.
						return; // 현재 새로 요청된 입력 시뮬레이션은 취소시킴
					}
				}
			}

			isInputInProgress = true;
			Task.Run(async () =>
			{
				await AutoEnterDelayTask(param);
				isInputInProgress = false;
			});
		}
		else if (!pre)
		{
			// Enter immediately
			EnterInstantly(param.Content, param.PathInfo);
		}
	}

	private async Task AutoEnterDelayTask(AutoEnterInfo param)
	{
		if (string.IsNullOrWhiteSpace(param.Content))
			return;

		if (param.Options.Mode.ShouldSimulateInput())
		{
			await SimulateInput(param);
			AutoEntered?.Invoke(this, new AutoEnterEventArgs(param.Content));
		}
		else if (!param.HasFlag(PathFlags.PreSearch)) // Pre-search result should not be auto-entered immediately (mostly because my turn hadn't come yet)
		{
			var totalDelay = param.GetTotalDelay();
			if (param.Options.DelayStartAfterCharEnterEnabled)
			{
				if (InputStopwatch.ElapsedMilliseconds < totalDelay)
				{
					await Task.Delay((int)(totalDelay - InputStopwatch.ElapsedMilliseconds));
					Log.Debug("Waiting: (delay: {delay}) - (elaspsed:{elapsed}) = {realDelay}ms", totalDelay, InputStopwatch.ElapsedMilliseconds, totalDelay);
				}
			}
			else
			{
				var delayBetweenInput = (int)(totalDelay - InputStopwatch.ElapsedMilliseconds);
				var delay = Math.Max(totalDelay, delayBetweenInput); // Failsafe to prevent way-too-fast input
				Log.Debug("Waiting: max(delay: {delay}, delayBetweenInput: {delayBetweenInput}) = {realDelay}ms", totalDelay, delayBetweenInput, delay);
				await Task.Delay(delay);
			}

			EnterInstantly(param.Content, param.PathInfo);
		}
	}

	private void EnterInstantly(string content, PathDetails? path)
	{
		if (!CanPerformAutoEnterNow(path))
			return;

		Log.Information(I18n.Main_AutoEnter, content);

		game.UpdateChat(content);
		game.ClickSubmitButton();
		InputStopwatch.Restart();
		AutoEntered?.Invoke(this, new AutoEnterEventArgs(content));
	}

	private async Task SimulateInput(AutoEnterInfo param)
	{
		if (string.IsNullOrWhiteSpace(param.Content))
			return;

		isPreinputSimInProg = param.HasFlag(PathFlags.PreSearch);
		isPreinputFinished = false;

		var content = param.Content;
		var valid = true;

		var list = new List<HangulSplit>();
		foreach (var ch in content)
			list.Add(HangulSplit.Parse(ch));

		var recomp = new HangulRecomposer(KeyboardLayout.QWERTY, list.ToImmutableList());
		var inputList = recomp.Recompose();

		var startDelay = param.Options.GetStartDelay();
		await Task.Delay(startDelay);

		Log.Information(I18n.Main_InputSimulating, content);
		game.UpdateChat("");
		foreach (var input in inputList)
		{
			Log.Debug("Input requested: {ipt}", input);
			if (!CanPerformAutoEnterNow(param.PathInfo, !isPreinputSimInProg))
			{
				valid = false; // Abort
				break;
			}

			var delay = param.Options.GetDelayPerChar();

			if (shiftState && input.ShiftState == ShiftState.Release)
				shiftState = false;
			else if (!shiftState && input.ShiftState == ShiftState.Press)
				shiftState = true;

			game.AppendChat(input.TextUpdate, param.Options.Mode == AutoEnterMode.SimulateInputJSKeyEvents, input.Key, shiftState, input.ImeState == ImeState.Korean, param.Options.DelayBeforeKeyUp);
			await Task.Delay(delay);
		}

		if (isPreinputSimInProg) // As this function runs asynchronously, this value could have been changed.
		{
			isPreinputSimInProg = false;
			isPreinputFinished = true;
			return; // Don't submit yet
		}

		CheckAndSubmit(valid);
	}

	private void CheckAndSubmit(bool valid)
	{
		if (valid)
		{
			game.ClickSubmitButton();
			Log.Information(I18n.Main_InputSimulationFinished);
		}
		else
		{
			Log.Warning(I18n.Main_InputSimulationAborted);
		}
		game.UpdateChat("");
	}
}
