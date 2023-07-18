using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketHandlers;

namespace AutoKkutuLib.Game;
public interface IGame : IDisposable
{
	GameSessionState Session { get; }
	BrowserBase Browser { get; }

	int GetTurnTimeMillis();

	// Game events
	event EventHandler? GameEnded;
	event EventHandler? GameStarted;
	event EventHandler? RoundChanged;
	event EventHandler<GameModeChangeEventArgs>? GameModeChanged;

	// Turn events
	event EventHandler<TurnStartEventArgs>? TurnStarted;
	event EventHandler<WordConditionPresentEventArgs>? PathRescanRequested;
	event EventHandler<TurnEndEventArgs>? TurnEnded;
	event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;
	event EventHandler<WordPresentEventArgs>? HintWordPresented;
	event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;

	void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay);
	void ClickSubmitButton();
	void FocusChat();
	bool HasSameDomHandler(IDomHandler otherHandler);
	bool HasSameWebSocketHandler(IWebSocketHandler otherHandler);
	bool IsPathExpired(PathDetails path);
	bool RequestRescanIfPathExpired(PathDetails path);
	void Start();
	void Stop();
	void UpdateChat(string input);
}