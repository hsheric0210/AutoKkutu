﻿using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketListener;
using AutoKkutuLib.Hangul;
using Serilog;

namespace AutoKkutuLib.Game;

public partial class Game : IGame
{
	public AutoEnter AutoEnter { get; }
	public BrowserBase Browser => domHandler.Browser;

	#region Game status properties

	public WordCondition? CurrentWordCondition { get; private set; }

	public GameMode CurrentGameMode { get; private set; } = GameMode.LastAndFirst;

	public bool IsGameInProgress { get; private set; }

	public bool IsMyTurn { get => isMyTurn == 1; private set => isMyTurn = value ? 1 : 0; }

	public bool ReturnMode { get; set; }
	#endregion

	#region Internal handle holder fields
	private readonly DomHandlerBase domHandler;
	private readonly WsHandlerBase? wsSniffHandler;
	#endregion

	#region Internal states
	private const int idleInterval = 3000;
	private const int looseInterval = 100;
	private const int intenseInterval = 10;
	private bool active;
	private string lastChat = "";

	/// <summary>
	/// Workaround for Interlocked.CompareAndExchange
	/// </summary>
	private int isMyTurn;
	#endregion

	#region Game events
	public event EventHandler? GameStarted;
	public event EventHandler? GameEnded;
	public event EventHandler<PreviousUserTurnEndedEventArgs>? PreviousUserTurnEnded;
	public event EventHandler<WordConditionPresentEventArgs>? MyTurnStarted;
	public event EventHandler? MyTurnEnded;
	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	public event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;
	public event EventHandler? RoundChanged;
	public event EventHandler<GameModeChangeEventArgs>? GameModeChanged;
	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	public event EventHandler? ChatUpdated;
	public event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	public event EventHandler<WordPresentEventArgs>? ExampleWordPresented;
	#endregion

	public Game(DomHandlerBase domHandler, WsHandlerBase? wsSniffHandler)
	{
		this.domHandler = domHandler;
		this.wsSniffHandler = wsSniffHandler;
		AutoEnter = new AutoEnter(this);
	}

	public bool HasSameDomHandler(DomHandlerBase otherHandler) => domHandler.HandlerName.Equals(otherHandler.HandlerName, StringComparison.OrdinalIgnoreCase);
	public bool HasSameWsSniffingHandler(WsHandlerBase otherHandler) => wsSniffHandler?.HandlerName.Equals(otherHandler.HandlerName, StringComparison.OrdinalIgnoreCase) ?? false;

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
		if (wsSniffHandler != null)
			await wsSniffHandler.RegisterInGameFunctions(registeredFunctions);
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
			MyTurnStarted?.Invoke(this, new WordConditionPresentEventArgs((WordCondition)CurrentWordCondition!)); // Re-trigger search
			return true;
		}
		return false;
	}

	public void UpdateChat(string input)
	{
		domHandler.UpdateChat(input);
		lastChat = input;
		ChatUpdated?.Invoke(this, EventArgs.Empty);
	}


	public void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay)
		=> domHandler.AppendChat(textUpdate, sendEvents, key, shift, hangul, upDelay);

	public void ClickSubmitButton()
	{
		domHandler.ClickSubmit();
		if (!string.IsNullOrEmpty(lastChat))
			AutoEnter.InputStopwatch.Restart();
		lastChat = "";
	}
	#endregion

	public override string ToString() => $"Game{{DOM-Poller: {domHandler.HandlerName}, WS-Sniffer: {wsSniffHandler?.HandlerName}, MainPoller: {mainPoller?.Id}}}";

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
