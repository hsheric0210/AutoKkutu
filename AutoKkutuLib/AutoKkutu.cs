using AutoKkutuLib.Database;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.Events;
using AutoKkutuLib.Node;
using AutoKkutuLib.Path;

namespace AutoKkutuLib;

public class AutoKkutu : IDisposable
{
	#region Internal fields
	private bool disposedValue;
	private IGame? game;
	#endregion

	#region Module exposures
	public NodeManager NodeManager { get; }
	public PathFilter PathFilter { get; }
	public PathFinder PathFinder { get; }
	public AbstractDatabase Database { get; }
	public AbstractDatabaseConnection DbConnection => Database.Connection;

	public IGame Game => game ?? throw new InvalidOperationException("Game is not registered yet!");
	public BrowserBase GameJsEvaluator => Game.Browser;
	public bool HasGameSet => game is not null;
	#endregion

	#region Game event redirects
	public event EventHandler? ChatUpdated;
	public event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	public event EventHandler? GameEnded;
	public event EventHandler<GameModeChangeEventArgs>? GameModeChanged;
	public event EventHandler? GameStarted;
	public event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;
	public event EventHandler? MyTurnEnded;
	public event EventHandler<WordConditionPresentEventArgs>? MyWordPresented;
	public event EventHandler? RoundChanged;
	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	public event EventHandler<WordPresentEventArgs>? ExampleWordPresented;
	#endregion

	#region AutoEnter event redirects
	public event EventHandler<InputDelayEventArgs>? InputDelayApply;
	public event EventHandler<AutoEnterEventArgs>? AutoEntered;
	public event EventHandler<NoPathAvailableEventArgs>? NoPathAvailable;
	#endregion

	public AutoKkutu(AbstractDatabase db)
	{
		Database = db;
		PathFilter = new PathFilter();
		NodeManager = new NodeManager(db.Connection);
		PathFinder = new PathFinder(NodeManager, PathFilter);

		InterconnectModules();
	}

	private void InterconnectModules()
	{
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
			UnregisterGameEventRedirects(Game);
			Game.Stop();
			Game.Dispose();
		}
		this.game = game;
		RegisterGameEventRedirects(game);
		game.Start();
	}

	private void RegisterGameEventRedirects(IGame game)
	{
		game.ChatUpdated += ChatUpdated;
		game.DiscoverWordHistory += DiscoverWordHistory;
		game.GameEnded += GameEnded;
		game.GameModeChanged += GameModeChanged;
		game.GameStarted += GameStarted;
		game.MyPathIsUnsupported += MyPathIsUnsupported;
		game.MyTurnEnded += MyTurnEnded;
		game.MyWordPresented += MyWordPresented;
		game.RoundChanged += RoundChanged;
		game.TypingWordPresented += TypingWordPresented;
		game.UnsupportedWordEntered += UnsupportedWordEntered;
		game.ExampleWordPresented += ExampleWordPresented;

		game.AutoEnter.InputDelayApply += InputDelayApply;
		game.AutoEnter.AutoEntered += AutoEntered;
		game.AutoEnter.NoPathAvailable += NoPathAvailable;
	}

	private void UnregisterGameEventRedirects(IGame game)
	{
		game.ChatUpdated -= ChatUpdated;
		game.DiscoverWordHistory -= DiscoverWordHistory;
		game.GameEnded -= GameEnded;
		game.GameModeChanged -= GameModeChanged;
		game.GameStarted -= GameStarted;
		game.MyPathIsUnsupported -= MyPathIsUnsupported;
		game.MyTurnEnded -= MyTurnEnded;
		game.MyWordPresented -= MyWordPresented;
		game.RoundChanged -= RoundChanged;
		game.TypingWordPresented -= TypingWordPresented;
		game.UnsupportedWordEntered -= UnsupportedWordEntered;
		game.ExampleWordPresented -= ExampleWordPresented;

		game.AutoEnter.InputDelayApply -= InputDelayApply;
		game.AutoEnter.AutoEntered -= AutoEntered;
		game.AutoEnter.NoPathAvailable -= NoPathAvailable;
	}

	#region Disposal
	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				game?.Dispose();
			}
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
