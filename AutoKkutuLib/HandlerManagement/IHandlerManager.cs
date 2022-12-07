using AutoKkutuLib.HandlerManagement.Events;

namespace AutoKkutuLib.HandlerManagement;
public interface IHandlerManager : IDisposable
{
	GameMode CurrentGameMode { get; }
	string? CurrentMissionChar { get; }
	PresentedWord? CurrentPresentedWord { get; }
	bool IsGameStarted { get; }
	bool IsMyTurn { get; }
	bool ReturnMode { get; set; }
	int TurnTimeMillis { get; }

	event EventHandler? ChatUpdated;
	event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	event EventHandler? GameEnded;
	event EventHandler<GameModeChangeEventArgs>? GameModeChanged;
	event EventHandler? GameStarted;
	event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;
	event EventHandler? MyTurnEnded;
	event EventHandler<WordPresentEventArgs>? MyWordPresented;
	event EventHandler? RoundChanged;
	event EventHandler<WordPresentEventArgs>? TypingWordPresented;
	event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;

	void AppendChat(Func<string, string> appender);
	void ClickSubmitButton();
	string GetID();
	bool IsValidPath(PathFinderParameter path);
	void Start();
	void Stop();
	void UpdateChat(string input);
}