using AutoKkutuLib.Browser;
using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Path;
using AutoKkutuLib.Game;

namespace AutoKkutuLib;

public class AutoKkutu : IDisposable
{
	private bool disposedValue;

	private Uri serverUri;

	#region Facade implementation - Module exposure
	public AbstractDatabaseConnection Database { get; }
	public IGame Game { get; }

	public PathFilter PathFilter { get; }
	public NodeManager NodeManager { get; }
	public PathFinder PathFinder { get; }
	#endregion

	#region Module sub-element exposure wrapper (to enforce Law of Demeter)
	public BrowserBase Browser => Game.Browser;
	#endregion

	#region Redirect module events
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
	#endregion

	/// <summary>
	/// AutoKkutu 파사드 클래스를 생성합니다.
	/// <paramref name="dbConnection"/>에 해당하는 데이터베이스 연결은 초기화 이전에 이미 열려 있어야 하며,
	/// <paramref name="game"/>에 해당하는 게임 핸들러 인스턴스는 이미 시작된 상태(<c>Start</c> 함수가 호출된 상태)이어야 합니다.
	/// </summary>
	/// <param name="dbConnection">데이터베이스 연결 인스턴스</param>
	/// <param name="game">게임 핸들러 인스턴스</param>
	public AutoKkutu(Uri serverUri, AbstractDatabaseConnection dbConnection, IGame game)
	{
		this.serverUri = serverUri;

		Database = dbConnection;
		PathFilter = new PathFilter();
		NodeManager = new NodeManager(dbConnection);
		PathFinder = new PathFinder(NodeManager, PathFilter);

		Game = game;

		// Mediators
		game.DiscoverWordHistory += HandleDiscoverWordHistory;
		game.HintWordPresented += HandleExampleWordPresented;
		game.RoundChanged += HandleRoundChanged;

		// Module event wrappers
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

		PathFinder.FindStateChanged += PathFinder_FindStateChanged;
		PathFinder.PathUpdated += PathFinder_PathUpdated;
	}

	public bool IsForServer(Uri server) => serverUri.Host.Equals(server.Host, StringComparison.OrdinalIgnoreCase);

	#region Module interconnector (Mediator)
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
	#endregion

	#region Redirect module events
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
	#endregion

	#region Disposal
	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			// Unregister game events
			if (disposing && Game != null)
			{
				// Remove Mediators
				Game.DiscoverWordHistory -= HandleDiscoverWordHistory;
				Game.HintWordPresented -= HandleExampleWordPresented;
				Game.RoundChanged -= HandleRoundChanged;

				// Remove module event wrappers
				Game.GameStarted -= Game_GameStarted;
				Game.GameEnded -= Game_GameEnded;
				Game.RoundChanged -= Game_RoundChanged;
				Game.GameModeChanged -= Game_GameModeChanged;

				Game.PreviousUserTurnEnded -= Game_PreviousUserTurnEnded;
				Game.TurnStarted -= Game_MyTurnStarted;
				Game.TurnEnded -= Game_MyTurnEnded;
				Game.UnsupportedWordEntered -= Game_UnsupportedWordEntered;
				Game.HintWordPresented -= Game_HintWordPresented;
				Game.TypingWordPresented -= Game_TypingWordPresented;
				Game.DiscoverWordHistory -= Game_DiscoverWordHistory;

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

				PathFinder.FindStateChanged -= PathFinder_FindStateChanged;
				PathFinder.PathUpdated -= PathFinder_PathUpdated;

				Game.Dispose();
				Database.Dispose();
			}

			disposedValue = true;
		}
	}

	/// <summary>
	/// 현재 AutoKkutu 파사드가 소유한 모든 리소스를 Dispose합니다.
	/// 생성자 파라미터로 넘어온 <c>dbConnection</c>과 <c>game</c> 역시 Dispose된다는 사실에 주의하세요.
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
