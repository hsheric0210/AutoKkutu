using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketListener;

namespace AutoKkutuLib.Game;
public interface IGame : IDisposable
{
	AutoEnter AutoEnter { get; }
	GameMode CurrentGameMode { get; }
	WordCondition? CurrentWordCondition { get; }
	bool IsGameInProgress { get; }
	bool IsMyTurn { get; }
	BrowserBase Browser { get; }
	bool ReturnMode { get; set; }

	int GetTurnTimeMillis();

	event EventHandler? ChatUpdated;
	event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	event EventHandler<WordPresentEventArgs>? ExampleWordPresented;
	event EventHandler? GameEnded;
	event EventHandler<GameModeChangeEventArgs>? GameModeChanged;
	event EventHandler? GameStarted;
	event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;
	event EventHandler<PreviousUserTurnEndedEventArgs>? PreviousUserTurnEnded;
	event EventHandler<WordConditionPresentEventArgs>? MyTurnStarted;
	event EventHandler? MyTurnEnded;
	event EventHandler? RoundChanged;
	event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;

	void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay);
	void ClickSubmitButton();
	bool HasSameDomHandler(DomHandlerBase otherHandler);
	bool HasSameWsSniffingHandler(WsHandlerBase otherHandler);
	bool IsPathExpired(PathDetails path);
	bool RescanIfPathExpired(PathDetails path);
	void Start();
	void Stop();
	void UpdateChat(string input);
}