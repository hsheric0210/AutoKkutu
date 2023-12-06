using AutoKkutuLib.Browser;
using System.Collections.Immutable;

namespace AutoKkutuLib.Game.DomHandlers;
public interface IDomHandler
{
	BrowserBase Browser { get; }

	string HandlerName { get; }
	string HandlerDetails { get; }

	void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay);
	void ClickSubmit();
	ValueTask<string> GetExampleWord();
	ValueTask<GameMode> GetGameMode();
	ValueTask<bool> GetIsMyTurn();
	ValueTask<string> GetMissionChar();
	ValueTask<string> GetPresentedWord();
	ValueTask<int> GetWordLength();
	ValueTask<int> GetRoundIndex();
	ValueTask<float> GetRoundTime();
	ValueTask<float> GetTurnTime();
	ValueTask<string> GetUnsupportedWord();
	ValueTask<IImmutableList<string>> GetWordInHistories();
	ValueTask<int> GetTurnIndex();
	ValueTask<string> GetUserId();
	ValueTask<IImmutableList<string>> GetGameSeq();
	ValueTask RegisterInGameFunctions(ISet<int> alreadyRegistered);
	void UpdateChat(string input);
	void FocusChat();
}