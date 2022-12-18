using AutoKkutuLib.Database;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.Events;
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
	public SpecialPathList SpecialPathList { get; }
	public PathFinder PathFinder { get; }
	public AbstractDatabase Database { get; }

	public IGame Game => game ?? throw new InvalidOperationException("Game is not registered yet!");
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
	public event EventHandler<WordPresentEventArgs>? MyWordPresented;
	public event EventHandler? RoundChanged;
	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	#endregion

	public AutoKkutu(AbstractDatabase db)
	{
		Database = db;
		SpecialPathList = new SpecialPathList();
		NodeManager = new NodeManager(db.Connection);
		PathFinder = new PathFinder(NodeManager, SpecialPathList);
	}

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
