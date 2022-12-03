using AutoKkutu.Constants;
using System;

namespace AutoKkutu.Modules.HandlerManager
{
	public interface IHandlerManager
	{
		string? CurrentMissionChar
		{
			get;
		}
		ResponsePresentedWord? CurrentPresentedWord
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
		event EventHandler<WordPresentEventArgs>? OnMyBegan;
		event EventHandler? RoundChanged;
		event EventHandler<WordPresentEventArgs>? TypingWordPresented;
		event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;

		void AppendChat(Func<string, string> appender);
		void ClickSubmitButton();
		void Dispose();
		string GetID();
		void StartHandlerManager();
		void StopHandlerManager();
		void UpdateChat(string input);
	}
}