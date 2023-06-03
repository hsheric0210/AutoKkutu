﻿using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketListener;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib.Game;

public partial class Game : IGame
{
	public AutoEnter AutoEnter { get; }
	public BrowserBase Browser => domHandler.Browser;

	#region Game status properties

	public WordCondition? CurrentPresentedWord { get; private set; }

	public string CurrentMissionChar { get; private set; } = "";

	public GameMode CurrentGameMode { get; private set; } = GameMode.LastAndFirst;

	public bool IsGameStarted { get; private set; }

	public bool IsMyTurn { get; private set; }

	public bool ReturnMode { get; set; }
	#endregion

	#region Internal handle holder fields
	private readonly DomHandlerBase domHandler;
	private readonly WsSniffingHandlerBase? wsSniffHandler;
	#endregion

	#region Internal states
	private const int idleInterval = 3000;
	private const int intenseInterval = 10;
	private const int primaryInterval = 100;
	private bool active;
	private string lastChat = "";
	#endregion

	#region Game events
	public event EventHandler? GameStarted;
	public event EventHandler? GameEnded;
	public event EventHandler<WordConditionPresentEventArgs>? PreviousUserTurnEnd; // TODO
	public event EventHandler<WordConditionPresentEventArgs>? MyWordPresented;
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

	public Game(DomHandlerBase domHandler, WsSniffingHandlerBase? wsSniffHandler)
	{
		this.domHandler = domHandler;
		this.wsSniffHandler = wsSniffHandler;
		AutoEnter = new AutoEnter(this);
	}

	public bool HasSameDomHandler(DomHandlerBase otherHandler) => domHandler.HandlerName.Equals(otherHandler.HandlerName, StringComparison.OrdinalIgnoreCase);
	public bool HasSameWsSniffingHandler(WsSniffingHandlerBase otherHandler) => wsSniffHandler?.HandlerName.Equals(otherHandler.HandlerName, StringComparison.OrdinalIgnoreCase) ?? false;

	public void Start()
	{
		if (!active)
		{
			active = true;
			var registeredFunctions = new HashSet<int>();
			Task.Run(async () =>
			{
				await domHandler.RegisterInGameFunctions(registeredFunctions); // TODO: Await for this to prevent initial js errors when startup
				if (wsSniffHandler != null)
					await wsSniffHandler.RegisterInGameFunctions(registeredFunctions);
				StartPollers();
			});
			BeginWebSocketSniffing();
		}
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

	public bool IsValidPath(PathFinderParameter path)
	{
		if (path.Options.HasFlag(PathFinderFlags.ManualSearch))
			return true;

		var differentWord = CurrentPresentedWord != null && !path.Word.Equals(CurrentPresentedWord);
		var differentMissionChar = path.Options.HasFlag(PathFinderFlags.MissionWordExists) && !string.IsNullOrWhiteSpace(CurrentMissionChar) && !string.Equals(path.MissionChar, CurrentMissionChar, StringComparison.OrdinalIgnoreCase);
		if (IsMyTurn && (differentWord || differentMissionChar))
		{
			Log.Warning(I18n.PathFinder_InvalidatedUpdate, differentWord, differentMissionChar);
			MyWordPresented?.Invoke(this, new WordConditionPresentEventArgs(CurrentPresentedWord!, CurrentMissionChar!)); // Re-trigger search
			return false;
		}
		return true;
	}

	public void UpdateChat(string input)
	{
		domHandler.UpdateChat(input);
		lastChat = input;
		ChatUpdated?.Invoke(this, EventArgs.Empty);
	}

	public void AppendChat(Func<string, string> appender)
	{
		if (appender is null)
			throw new ArgumentNullException(nameof(appender));

		UpdateChat(appender(lastChat));
	}

	public void ClickSubmitButton()
	{
		domHandler.ClickSubmit();
		if (!string.IsNullOrEmpty(lastChat))
			AutoEnter.InputStopwatch.Restart();
		lastChat = "";
	}
	#endregion

	public override string ToString() => $"Game[DOM-Poller={domHandler.HandlerName}, WS-Sniffer={wsSniffHandler.HandlerName}, MainPoller={mainPoller?.Id}]";

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
