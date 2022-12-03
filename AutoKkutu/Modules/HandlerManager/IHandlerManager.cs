using AutoKkutu.Constants;
using System;

namespace AutoKkutu.Modules.HandlerManager
{
	public interface IHandlerManager : IDisposable
	{
		string? CurrentMissionChar
		{
			get;
		}
		PresentedWord? CurrentPresentedWord
		{
			get;
		}
		bool IsGameStarted
		{
			get;
		}
		bool IsMyTurn
		{
			get;
		}
		int TurnTimeMillis
		{
			get;
		}

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
		string GetID();
		bool IsValidPath(PathFound path);
		void Start();
		void Stop();
		void UpdateChat(string input);
	}
}