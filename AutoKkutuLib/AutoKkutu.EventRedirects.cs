using AutoKkutuLib.Database.Path;
using AutoKkutuLib.Game;

namespace AutoKkutuLib;

public partial class AutoKkutu
{
	// Game
	public event EventHandler? GameStarted;
	public event EventHandler? GameEnded;
	public event EventHandler<PreviousUserTurnEndedEventArgs>? PreviousUserTurnEnded;
	public event EventHandler<WordConditionPresentEventArgs>? MyTurnStarted;
	public event EventHandler? MyTurnEnded;
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

		game.PreviousUserTurnEnded += Game_PreviousUserTurnEnded;
		game.TurnStarted += Game_MyTurnStarted;
		game.TurnEnded += Game_MyTurnEnded;
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

		game.PreviousUserTurnEnded -= Game_PreviousUserTurnEnded;
		game.TurnStarted -= Game_MyTurnStarted;
		game.TurnEnded -= Game_MyTurnEnded;
		game.UnsupportedWordEntered -= Game_UnsupportedWordEntered;
		game.HintWordPresented -= Game_HintWordPresented;
		game.TypingWordPresented -= Game_TypingWordPresented;
		game.DiscoverWordHistory -= Game_DiscoverWordHistory;

		pathFinder.FindStateChanged -= PathFinder_FindStateChanged;
		pathFinder.PathUpdated -= PathFinder_PathUpdated;

		// Clear event listeners: https://stackoverflow.com/a/9513372
		GameStarted = null;
		GameEnded = null;
		PreviousUserTurnEnded = null;
		MyTurnStarted = null;
		MyTurnEnded = null;
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
	private void Game_PreviousUserTurnEnded(object? sender, PreviousUserTurnEndedEventArgs e) => PreviousUserTurnEnded?.Invoke(sender, e);
	private void Game_MyTurnStarted(object? sender, WordConditionPresentEventArgs e) => MyTurnStarted?.Invoke(sender, e);
	private void Game_MyTurnEnded(object? sender, EventArgs e) => MyTurnEnded?.Invoke(sender, e);
	private void Game_UnsupportedWordEntered(object? sender, UnsupportedWordEventArgs e) => UnsupportedWordEntered?.Invoke(sender, e);
	private void Game_RoundChanged(object? sender, EventArgs e) => RoundChanged?.Invoke(sender, e);
	private void Game_GameModeChanged(object? sender, GameModeChangeEventArgs e) => GameModeChanged?.Invoke(sender, e);
	private void Game_TypingWordPresented(object? sender, WordPresentEventArgs e) => TypingWordPresented?.Invoke(sender, e);
	private void Game_DiscoverWordHistory(object? sender, WordHistoryEventArgs e) => DiscoverWordHistory?.Invoke(sender, e);
	private void Game_HintWordPresented(object? sender, WordPresentEventArgs e) => HintWordPresented?.Invoke(sender, e);

	private void PathFinder_FindStateChanged(object? sender, PathFinderStateEventArgs e) => FindStateChanged?.Invoke(sender, e);
	private void PathFinder_PathUpdated(object? sender, PathUpdateEventArgs e) => PathUpdated?.Invoke(sender, e);
}
