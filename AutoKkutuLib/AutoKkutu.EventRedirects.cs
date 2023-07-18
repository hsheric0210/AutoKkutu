using AutoKkutuLib.Database.Path;
using AutoKkutuLib.Game;

namespace AutoKkutuLib;

public partial class AutoKkutu
{
	// Game
	public event EventHandler? GameStarted;
	public event EventHandler? GameEnded;
	public event EventHandler<TurnStartEventArgs>? TurnStarted;
	public event EventHandler<WordConditionPresentEventArgs>? PathRescanRequested;
	public event EventHandler<TurnEndEventArgs>? TurnEnded;
	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	public event EventHandler? RoundChanged;
	public event EventHandler<GameModeChangeEventArgs>? GameModeChanged;
	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	public event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	public event EventHandler<WordPresentEventArgs>? HintWordPresented;

	// PathFinder
	public event EventHandler<PathFinderStateEventArgs>? FindStateChanged;
	public event EventHandler<PathUpdateEventArgs>? PathUpdated;

	private void RegisterEventRedirects(IGame game, PathFinder pathFinder)
	{
		game.GameStarted += Game_GameStarted;
		game.GameEnded += Game_GameEnded;
		game.RoundChanged += Game_RoundChanged;
		game.GameModeChanged += Game_GameModeChanged;

		game.TurnStarted += Game_TurnStarted;
		game.PathRescanRequested += Game_PathRescanRequested;
		game.TurnEnded += Game_TurnEnded;
		game.UnsupportedWordEntered += Game_UnsupportedWordEntered;
		game.HintWordPresented += Game_HintWordPresented;
		game.TypingWordPresented += Game_TypingWordPresented;
		game.DiscoverWordHistory += Game_DiscoverWordHistory;

		pathFinder.FindStateChanged += PathFinder_FindStateChanged;
		pathFinder.PathUpdated += PathFinder_PathUpdated;
	}

	private void UnregisterEventRedirects(IGame game, PathFinder pathFinder)
	{
		game.GameStarted -= Game_GameStarted;
		game.GameEnded -= Game_GameEnded;
		game.RoundChanged -= Game_RoundChanged;
		game.GameModeChanged -= Game_GameModeChanged;
		game.PathRescanRequested -= Game_PathRescanRequested;
		game.TurnStarted -= Game_TurnStarted;
		game.TurnEnded -= Game_TurnEnded;
		game.UnsupportedWordEntered -= Game_UnsupportedWordEntered;
		game.HintWordPresented -= Game_HintWordPresented;
		game.TypingWordPresented -= Game_TypingWordPresented;
		game.DiscoverWordHistory -= Game_DiscoverWordHistory;

		pathFinder.FindStateChanged -= PathFinder_FindStateChanged;
		pathFinder.PathUpdated -= PathFinder_PathUpdated;

		// Clear event listeners: https://stackoverflow.com/a/9513372
		GameStarted = null;
		GameEnded = null;
		TurnStarted = null;
		PathRescanRequested = null;
		TurnEnded = null;
		UnsupportedWordEntered = null;
		RoundChanged = null;
		GameModeChanged = null;
		TypingWordPresented = null;
		DiscoverWordHistory = null;
		HintWordPresented = null;
		FindStateChanged = null;
		PathUpdated = null;
	}

	private void Game_GameStarted(object? sender, EventArgs e) => GameStarted?.Invoke(sender, e);
	private void Game_GameEnded(object? sender, EventArgs e) => GameEnded?.Invoke(sender, e);
	private void Game_TurnStarted(object? sender, TurnStartEventArgs e) => TurnStarted?.Invoke(sender, e);
	private void Game_PathRescanRequested(object? sender, WordConditionPresentEventArgs e) => PathRescanRequested?.Invoke(sender, e);
	private void Game_TurnEnded(object? sender, TurnEndEventArgs e) => TurnEnded?.Invoke(sender, e);
	private void Game_UnsupportedWordEntered(object? sender, UnsupportedWordEventArgs e) => UnsupportedWordEntered?.Invoke(sender, e);
	private void Game_RoundChanged(object? sender, EventArgs e) => RoundChanged?.Invoke(sender, e);
	private void Game_GameModeChanged(object? sender, GameModeChangeEventArgs e) => GameModeChanged?.Invoke(sender, e);
	private void Game_TypingWordPresented(object? sender, WordPresentEventArgs e) => TypingWordPresented?.Invoke(sender, e);
	private void Game_DiscoverWordHistory(object? sender, WordHistoryEventArgs e) => DiscoverWordHistory?.Invoke(sender, e);
	private void Game_HintWordPresented(object? sender, WordPresentEventArgs e) => HintWordPresented?.Invoke(sender, e);

	private void PathFinder_FindStateChanged(object? sender, PathFinderStateEventArgs e) => FindStateChanged?.Invoke(sender, e);
	private void PathFinder_PathUpdated(object? sender, PathUpdateEventArgs e) => PathUpdated?.Invoke(sender, e);
}
