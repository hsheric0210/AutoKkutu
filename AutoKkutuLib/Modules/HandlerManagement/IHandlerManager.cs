using AutoKkutuLib.Constants;

namespace AutoKkutuLib.Modules.HandlerManagement;
public interface IHandlerManager
{
	string? CurrentMissionChar { get; }
	PresentedWord? CurrentPresentedWord { get; }
	bool IsGameStarted { get; }
	bool IsMyTurn { get; }
	int TurnTimeMillis { get; }

	event EventHandler? ChatUpdated;
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
	void Dispose();
	string GetID();
	bool IsValidPath(PathFinderParameter path);
	void Start();
	void Stop();
	void UpdateChat(string input);
}