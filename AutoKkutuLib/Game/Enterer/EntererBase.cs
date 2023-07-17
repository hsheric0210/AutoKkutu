using Serilog;
using System.Diagnostics;

namespace AutoKkutuLib.Game.Enterer;
public abstract class EntererBase : IEnterer
{
	public event EventHandler<InputDelayEventArgs>? InputDelayApply;
	public event EventHandler<EnterFinishedEventArgs>? EnterFinished;

	public string EntererName { get; }

	protected readonly IGame game;

	protected Stopwatch InputStopwatch { get; } = new();

	private volatile bool isInputInProgress;

	/// <summary>
	/// 현재 진행 중인 입력 시뮬레이션이 Pre-search 기능에 의한 것인지에 대한 여부를 나타냅니다.
	/// </summary>
	/// <remarks>
	/// Pre-search 기능에 의한 자동 입력에서 자동입력 완료 후 자동 전송을 막기 위해 존재합니다.
	/// (Pre-search 기능에 의한 자동 입력이 진행되는 시점에서는 아직 내 턴이 오지 않았을 확률이 있기 때문입니다)
	/// </remarks>
	protected volatile bool isPreinputSimInProg;

	/// <summary>
	/// 마지막으로 완료된 입력 시뮬레이션이 Pre-search 기능에 의한 것인지에 대한 여부를 나타냅니다.
	/// </summary>
	/// <remarks>
	/// 해당 검사는 '만약 현재 입력 시뮬레이션을 수행할 단어가 마지막으로 완료된 입력 시뮬레이션과 동일하다면
	/// 굳이 다시 입력 시뮬레이션을 수행는 대신 그냥 전송 버튼만 누르면 되는' 것과 같은 최적화가 가능하기에 필수적입니다.
	/// </remarks>
	protected bool IsPreinputFinished { get; set; }

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
	protected PathDetails lastPreinputPath = PathDetails.Empty;


	protected EntererBase(string name, IGame game)
	{
		EntererName = name;
		this.game = game;
	}

	// TODO: info.Content empty & null checks
	protected abstract ValueTask SendAsync(EnterInfo info);

	/// <summary>
	/// 자동 입력 수행을 요청합니다
	/// </summary>
	/// <param name="param">자동 입력 옵션; <c>param.Content</c>는 반드시 설정되어 있어야 합니다</param>
	/// <exception cref="ArgumentException"><c>param.Content</c>가 설정되어 있지 않을 때 발생</exception>
	// TODO: Rename to 'Send'
	public void RequestSend(EnterInfo param)
	{
		if (string.IsNullOrWhiteSpace(param.Content)) // TODO: Make AutoEnterInfo.content always non-null-able
			throw new ArgumentException("Content to auto-enter is not provided", nameof(param));

		Log.Verbose("AutoEnter request received: {request}", param);

		var pre = param.HasFlag(PathFlags.PreSearch);
		if (param.Options.DelayEnabled && !param.HasFlag(PathFlags.DryRun))
		{
			if (pre)
				lastPreinputPath = param;
			else
			{
				// Pre-search 입력 시뮬레이션 관련 최적화들 (자세한 내용은 위 필드들 설명 참조)
				if (lastPreinputPath.IsSimilar(param))
				{
					if (IsPreinputFinished) // Pre-search 입력 시뮬레이션 완료 시
					{
						TrySubmitInput(CanPerformAutoEnterNow(param)); // 자동으로 전송
						isPreinputSimInProg = IsPreinputFinished = false;
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
				await SendAsync(param);
				isInputInProgress = false;
			});
		}
		else if (!pre)
		{
			// Enter immediately
			EnterInstantly(param.Content, param.PathInfo);
		}
	}

	protected virtual void SubmitInput()
	{
		game.ClickSubmitButton();
		game.UpdateChat("");
	}

	protected virtual void ClearInput() => game.UpdateChat("");


	/// <summary>
	/// <paramref name="valid"/>가 <c>true</c>일 경우, 지금까지 입력된 내용을 전송 시도합니다.
	/// <paramref name="valid"/>가 <c>false</c>일 경우 자동 입력이 중단되었다는 로그를 남깁니다.
	/// </summary>
	/// <param name="valid">입력 내용을 제시할지의 여부입니다.</param>
	protected void TrySubmitInput(bool valid)
	{
		if (valid)
		{
			SubmitInput();
			Log.Information(I18n.Main_InputSimulationFinished); // TODO: Rename those
		}
		else
		{
			ClearInput();
			Log.Warning(I18n.Main_InputSimulationAborted); // TODO: Rename those
		}
	}

	/// <summary>
	/// 현재 자동 입력을 계속 이어나갈 수 있는지의 여부를 반환합니다.
	/// 만약 단어 조건이 바뀌는 등의 변화가 일어났다면 <c>false</c>를 반환하고 단어 재검색을 수행합니다.
	/// </summary>
	/// <param name="detail">검색 조건 상세 내용</param>
	/// <param name="checkTurn">현재 턴이 내 턴인지 검사 수행 여부</param>
	/// <returns>자동 입력을 계속 이어나갈 수 있는지의 여부</returns>
	protected bool CanPerformAutoEnterNow(PathDetails? detail, bool checkTurn = true)
	{
		if (detail is PathDetails detail1 && detail1.HasFlag(PathFlags.DoNotCheckExpired))
			return true;

		// Game is already ended
		if (!game.IsGameInProgress)
			return false;

		// Not my turn
		if (checkTurn && !game.IsMyTurn)
			return false;

		// Path is expired
		if (detail is PathDetails detail2 && game.RescanIfPathExpired(detail2.WithFlags(PathFlags.DryRun)))
			return false;

		return true;
	}

	/// <summary>
	/// 주어진 내용을 즉시, 한 번에, <i>그 어떤 우회도 적용하지 않고</i> 모두 전송합니다.
	/// </summary>
	/// <param name="content">전송할 입력</param>
	/// <param name="path"></param>
	protected void EnterInstantly(string content, PathDetails? path)
	{
		if (!CanPerformAutoEnterNow(path))
			return;

		Log.Information(I18n.Main_AutoEnter, content);
		game.UpdateChat(content);
		game.ClickSubmitButton();
		InputStopwatch.Restart();
		EnterFinished?.Invoke(this, new EnterFinishedEventArgs(content));
	}
}
