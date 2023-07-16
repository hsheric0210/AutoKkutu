using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketHandlers;

namespace AutoKkutuLib.Game;
public interface IGame : IDisposable
{
	GameMode CurrentGameMode { get; }
	WordCondition? CurrentWordCondition { get; }
	bool IsGameInProgress { get; }
	bool IsMyTurn { get; }
	BrowserBase Browser { get; }
	bool ReturnMode { get; set; }

	int GetTurnTimeMillis();

	// Game events
	event EventHandler? GameEnded;
	event EventHandler? GameStarted;
	event EventHandler? RoundChanged;
	event EventHandler<GameModeChangeEventArgs>? GameModeChanged;

	// Turn events
	event EventHandler<PreviousUserTurnEndedEventArgs>? PreviousUserTurnEnded;
	event EventHandler<WordConditionPresentEventArgs>? TurnStarted;
	event EventHandler? TurnEnded;
	event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	event EventHandler<WordPresentEventArgs>? HintWordPresented;
	event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;

	void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay);
	void ClickSubmitButton();
	bool HasSameDomHandler(IDomHandler otherHandler);
	bool HasSameWebSocketHandler(IWebSocketHandler otherHandler);
	bool IsPathExpired(PathDetails path);
	bool RescanIfPathExpired(PathDetails path);
	void Start();
	void Stop();
	void UpdateChat(string input);
}