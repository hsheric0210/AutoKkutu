using AutoKkutuLib.Game.Events;
using AutoKkutuLib.Handlers;

namespace AutoKkutuLib.Game;
public interface IGame : IDisposable
{
	AutoEnter AutoEnter { get; }
	GameMode CurrentGameMode { get; }
	string CurrentMissionChar { get; }
	PresentedWord? CurrentPresentedWord { get; }
	bool IsGameStarted { get; }
	bool IsMyTurn { get; }
	BrowserBase Browser { get; }
	bool ReturnMode { get; set; }
	int TurnTimeMillis { get; }

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
	string GetID();
	bool HasSameHandler(HandlerBase otherHandler);
	bool IsValidPath(PathFinderParameter path);
	void Start();
	void Stop();
	void UpdateChat(string input);
}