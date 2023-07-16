using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketHandlers;
using Serilog;

namespace AutoKkutuLib.Game;

public partial class Game : IGame
{
	public BrowserBase Browser => domHandler.Browser;

	#region Game status properties

	public WordCondition? CurrentWordCondition { get; private set; }

	public GameMode CurrentGameMode { get; private set; } = GameMode.LastAndFirst;

	public bool IsGameInProgress { get; private set; }

	public bool IsMyTurn { get; private set; }

	public bool ReturnMode { get; set; }
	#endregion

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
	public event EventHandler<PreviousUserTurnEndedEventArgs>? PreviousUserTurnEnded;
	public event EventHandler<WordConditionPresentEventArgs>? TurnStarted;
	public event EventHandler? TurnEnded;
	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	public event EventHandler<WordPresentEventArgs>? HintWordPresented;
	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	public event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	#endregion

	public Game(IDomHandler domHandler, IWebSocketHandler? webSocketHandler)
	{
		this.domHandler = domHandler;
		this.webSocketHandler = webSocketHandler;
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

	public bool IsPathExpired(PathDetails path) => !path.HasFlag(PathFlags.ManualSearch) && CurrentWordCondition != null && !path.Condition.IsSimilar(CurrentWordCondition);

	public bool RescanIfPathExpired(PathDetails path)
	{
		if (IsPathExpired(path))
		{
			Log.Warning(I18n.PathFinder_InvalidatedUpdate);
			TurnStarted?.Invoke(this, new WordConditionPresentEventArgs((WordCondition)CurrentWordCondition!, IsMyTurn)); // Re-trigger search
			return true;
		}
		return false;
	}

	public void UpdateChat(string input) => domHandler.UpdateChat(input);

	public void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay)
		=> domHandler.AppendChat(textUpdate, sendEvents, key, shift, hangul, upDelay);

	public void ClickSubmitButton() => domHandler.ClickSubmit();
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
