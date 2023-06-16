using AutoKkutuLib.Browser;
using AutoKkutuLib.Database;
using AutoKkutuLib.Game;
using AutoKkutuLib.Node;
using AutoKkutuLib.Path;
using Serilog;

namespace AutoKkutuLib;

public class AutoKkutu : IDisposable
{
	#region Internal fields
	private bool disposedValue;
	private IGame? game;
	#endregion

	#region Module exposures
	public NodeManager NodeManager { get; private set; }
	public PathFilter PathFilter { get; private set; }
	public PathFinder PathFinder { get; private set; }
	public AbstractDatabaseConnection Database { get; }

	public IGame Game => game ?? throw new InvalidOperationException("Game is not registered yet!");
	public BrowserBase? Browser => game?.Browser;
	public bool HasGameSet => game is not null;
	#endregion

	#region Event redirects
	public event EventHandler? ChatUpdated;
	public event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	public event EventHandler? GameEnded;
	public event EventHandler<GameModeChangeEventArgs>? GameModeChanged;
	public event EventHandler? GameStarted;
	public event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;
	public event EventHandler<PreviousUserTurnEndedEventArgs>? PreviousUserTurnEnded;
	public event EventHandler<WordConditionPresentEventArgs>? MyTurnStarted;
	public event EventHandler? MyTurnEnded;
	public event EventHandler? RoundChanged;
	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	public event EventHandler<WordPresentEventArgs>? ExampleWordPresented;

	public event EventHandler<InputDelayEventArgs>? InputDelayApply;
	public event EventHandler<AutoEnterEventArgs>? AutoEntered;
	public event EventHandler<NoPathAvailableEventArgs>? NoPathAvailable;
	#endregion

	public AutoKkutu(AbstractDatabaseConnection dbConnection)
	{
		Database = dbConnection;
		PathFilter = new PathFilter();
		NodeManager = new NodeManager(dbConnection);
		PathFinder = new PathFinder(NodeManager, PathFilter);

		InterconnectModules();
	}

	private void InterconnectModules()
	{
		// TODO: Default game implementation in AutoKkutuGui should moved to here
		DiscoverWordHistory += HandleDiscoverWordHistory;
		ExampleWordPresented += HandleExampleWordPresented;
		RoundChanged += HandleRoundChanged;
	}

	private void HandleDiscoverWordHistory(object? sender, WordHistoryEventArgs args)
	{
		var word = args.Word;
		PathFilter.NewPaths.Add(word);
		PathFilter.PreviousPaths.Add(word);
	}

	private void HandleExampleWordPresented(object? sender, WordPresentEventArgs args)
	{
		var word = args.Word;
		PathFilter.NewPaths.Add(word);
	}

	private void HandleRoundChanged(object? sender, EventArgs args) => PathFilter.PreviousPaths.Clear();

	public void SetGame(IGame game)
	{
		if (HasGameSet)
		{
			UnregisterEventRedirects(Game);
			Game.Stop();
			Game.Dispose();
		}
		this.game = game;
		RegisterEventRedirects(game);
		game.Start();
	}

	#region Event redirects
	private void RegisterEventRedirects(IGame game)
	{
		game.ChatUpdated += RedirectChatUpdated;
		game.DiscoverWordHistory += RedirectDiscoverWordHistory;
		game.GameEnded += RedirectGameEnded;
		game.GameModeChanged += RedirectGameModeChanged;
		game.GameStarted += RedirectGameStarted;
		game.MyPathIsUnsupported += RedirectMyPathIsUnsupported;
		game.MyTurnEnded += RedirectMyTurnEnded;
		game.MyTurnStarted += RedirectMyTurnStarted;
		game.PreviousUserTurnEnded += RedirectPreviousUserTurnEnded;
		game.RoundChanged += RedirectRoundChanged;
		game.TypingWordPresented += RedirectTypingWordPresented;
		game.UnsupportedWordEntered += RedirectUnsupportedWordEntered;
		game.ExampleWordPresented += RedirectExampleWordPresented;

		game.AutoEnter.InputDelayApply += RedirectInputDelayApply;
		game.AutoEnter.AutoEntered += RedirectAutoEntered;
		game.AutoEnter.NoPathAvailable += RedirectNoPathAvailable;
	}

	private void UnregisterEventRedirects(IGame game)
	{
		game.ChatUpdated -= RedirectChatUpdated;
		game.DiscoverWordHistory -= RedirectDiscoverWordHistory;
		game.GameEnded -= RedirectGameEnded;
		game.GameModeChanged -= RedirectGameModeChanged;
		game.GameStarted -= RedirectGameStarted;
		game.MyPathIsUnsupported -= RedirectMyPathIsUnsupported;
		game.MyTurnEnded -= RedirectMyTurnEnded;
		game.MyTurnStarted -= RedirectMyTurnStarted;
		game.PreviousUserTurnEnded -= RedirectPreviousUserTurnEnded;
		game.RoundChanged -= RedirectRoundChanged;
		game.TypingWordPresented -= RedirectTypingWordPresented;
		game.UnsupportedWordEntered -= RedirectUnsupportedWordEntered;
		game.ExampleWordPresented -= RedirectExampleWordPresented;

		game.AutoEnter.InputDelayApply -= RedirectInputDelayApply;
		game.AutoEnter.AutoEntered -= RedirectAutoEntered;
		game.AutoEnter.NoPathAvailable -= RedirectNoPathAvailable;
	}

	private void RedirectChatUpdated(object? sender, EventArgs e) => ChatUpdated?.Invoke(sender, e);
	private void RedirectDiscoverWordHistory(object? sender, WordHistoryEventArgs e) => DiscoverWordHistory?.Invoke(sender, e);
	private void RedirectGameStarted(object? sender, EventArgs e) => GameStarted?.Invoke(sender, e);
	private void RedirectGameEnded(object? sender, EventArgs e) => GameEnded?.Invoke(sender, e);
	private void RedirectGameModeChanged(object? sender, GameModeChangeEventArgs e) => GameModeChanged?.Invoke(sender, e);
	private void RedirectMyTurnStarted(object? sender, WordConditionPresentEventArgs e) => MyTurnStarted?.Invoke(sender, e);
	private void RedirectMyTurnEnded(object? sender, EventArgs e) => MyTurnEnded?.Invoke(sender, e);
	private void RedirectMyPathIsUnsupported(object? sender, UnsupportedWordEventArgs e) => MyPathIsUnsupported?.Invoke(sender, e);
	private void RedirectUnsupportedWordEntered(object? sender, UnsupportedWordEventArgs e) => UnsupportedWordEntered?.Invoke(sender, e);
	private void RedirectPreviousUserTurnEnded(object? sender, PreviousUserTurnEndedEventArgs e) => PreviousUserTurnEnded?.Invoke(sender, e);
	private void RedirectRoundChanged(object? sender, EventArgs e) => RoundChanged?.Invoke(sender, e);
	private void RedirectTypingWordPresented(object? sender, WordPresentEventArgs e) => TypingWordPresented?.Invoke(sender, e);
	private void RedirectExampleWordPresented(object? sender, WordPresentEventArgs e) => ExampleWordPresented?.Invoke(sender, e);
	private void RedirectInputDelayApply(object? sender, InputDelayEventArgs e) => InputDelayApply?.Invoke(sender, e);
	private void RedirectAutoEntered(object? sender, AutoEnterEventArgs e) => AutoEntered?.Invoke(sender, e);
	private void RedirectNoPathAvailable(object? sender, NoPathAvailableEventArgs e) => NoPathAvailable?.Invoke(sender, e);
	#endregion

	#region Disposal
	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			// Dispose sub-components
			if (disposing)
			{
				game?.Dispose();
			}

			// Set fields to null to encourage GC
			game = null;
			NodeManager = null!;
			PathFilter = null!;
			PathFinder = null!;

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
