using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketListener;

namespace AutoKkutuLib.Game;
public interface IGame : IDisposable
{
	AutoEnter AutoEnter { get; }
	GameMode CurrentGameMode { get; }
	string CurrentMissionChar { get; }
	WordCondition? CurrentPresentedWord { get; }
	bool IsGameStarted { get; }
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
	event EventHandler? MyTurnEnded;
	event EventHandler<WordConditionPresentEventArgs>? MyWordPresented;
	event EventHandler? RoundChanged;
	event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;

	void AppendChat(Func<string, string> appender);
	void ClickSubmitButton();
	bool HasSameDomHandler(DomHandlerBase otherHandler);
	bool HasSameWsSniffingHandler(WsHandlerBase otherHandler);
	bool IsValidPath(PathFinderParameter path);
	void Start();
	void Stop();
	void UpdateChat(string input);
}