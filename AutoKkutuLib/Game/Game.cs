using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketHandlers;

namespace AutoKkutuLib.Game;

public partial class Game : IGame
{
	public BrowserBase Browser => domHandler.Browser;

	public GameSessionState Session { get; private set; }

	#region Internal handle holder fields
	private readonly IDomHandler domHandler;
	private readonly IWebSocketHandler? webSocketHandler;
	#endregion

	#region Internal states
	private const int idleInterval = 3000;
	private const int looseInterval = 100;
	private const int intenseInterval = 10;
	private bool active;
	#endregion

	#region Game events
	// Game events
	public event EventHandler? GameStarted;
	public event EventHandler? GameEnded;
	public event EventHandler? RoundChanged;
	public event EventHandler<GameModeChangeEventArgs>? GameModeChanged;

	// Turn events
	public event EventHandler<TurnStartEventArgs>? TurnStarted;
	public event EventHandler<WordConditionPresentEventArgs>? PathRescanRequested;
	public event EventHandler<TurnEndEventArgs>? TurnEnded;
	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	public event EventHandler<WordPresentEventArgs>? HintWordPresented;
	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	public event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	#endregion

	public Game(IDomHandler domHandler, IWebSocketHandler? webSocketHandler)
	{
		this.domHandler = domHandler;
		this.webSocketHandler = webSocketHandler;

		Session = new GameSessionState(""); // default game session
	}

	public bool HasSameDomHandler(IDomHandler otherHandler) => domHandler.HandlerName.Equals(otherHandler.HandlerName, StringComparison.OrdinalIgnoreCase);
	public bool HasSameWebSocketHandler(IWebSocketHandler otherHandler) => webSocketHandler?.HandlerName.Equals(otherHandler.HandlerName, StringComparison.OrdinalIgnoreCase) ?? false;

	public void Start()
	{
		if (!active)
		{
			active = true;
			Task.Run(async () =>
			{
				await RegisterInGameFunctions(new HashSet<int>());
				StartPollers();
			});
			BeginWebSocketSniffing();
		}
	}

	public async Task RegisterInGameFunctions(ISet<int> registeredFunctions)
	{
		await domHandler.RegisterInGameFunctions(registeredFunctions);
		if (webSocketHandler != null)
			await webSocketHandler.RegisterInGameFunctions(registeredFunctions);
	}

	public void Stop()
	{
		if (active)
		{
			EndWebSocketSniffing();
			StopPollers();
			active = false;
		}
	}

	#region Game interaction
	public async Task<int> GetTurnTimeMillisAsync() => (int)(Math.Min(await domHandler.GetTurnTime(), await domHandler.GetRoundTime()) * 1000);

	public int GetTurnTimeMillis() => GetTurnTimeMillisAsync().Result;

	public bool IsPathExpired(PathDetails path)
		=> !path.HasFlag(PathFlags.DoNotCheckExpired)
			&& !path.HasFlag(PathFlags.PreSearch) // Pre-search 시 다음 턴에 제시될 단어 조건을 미리 예측하고 치는 것이기에, 당연히 Path-expired 검사에서 걸린다. 이를 우회하기 위해서 Presearch flag를 검사한다.
			&& !path.Condition.IsSimilar(Session.WordCondition);

	public bool RequestRescanIfPathExpired(PathDetails path)
	{
		if (IsPathExpired(path) && !Session.WordCondition.IsEmpty())
		{
			LibLogger.Warn<Game>("Path is expired. Requesting re-scan. old={opath} new={npath}", path.Condition, Session.WordCondition);
			PathRescanRequested?.Invoke(this, new WordConditionPresentEventArgs(Session.WordCondition)); // Request re-scan
			return true;
		}
		return false;
	}

	public void UpdateChat(string input) => domHandler.UpdateChat(input);

	public void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay)
		=> domHandler.AppendChat(textUpdate, sendEvents, key, shift, hangul, upDelay);

	public void ClickSubmitButton() => domHandler.ClickSubmit();

	public void FocusChat() => domHandler.FocusChat();
	#endregion

	public override string ToString() => $"Game{{DOM-Poller: {domHandler.HandlerName}, WS-Sniffer: {webSocketHandler?.HandlerName}, MainPoller: {mainPoller?.Id}}}";

	#region Disposal
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			Stop();
			pollerCancel?.Dispose();
			mainPoller?.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
