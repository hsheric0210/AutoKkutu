using AutoKkutuLib.Browser;

namespace AutoKkutuLib.Game.DomHandlers;

public abstract class DomHandlerBase
{
	public abstract string HandlerName { get; }
	public abstract IReadOnlyCollection<Uri> UrlPattern { get; }
	public abstract BrowserBase Browser { get; }

	public abstract ValueTask<GameMode> GetGameMode();
	public abstract ValueTask<bool> GetIsGameInProgress();
	public abstract ValueTask<bool> GetIsMyTurn();
	public abstract ValueTask<string?> GetPresentedWord();
	public abstract ValueTask<string?> GetMissionChar();
	public abstract ValueTask<string?> GetExampleWord();
	public abstract ValueTask<int> GetRoundIndex();
	public abstract ValueTask<float> GetRoundTime();
	public abstract ValueTask<float> GetTurnTime();
	public abstract ValueTask<string?> GetUnsupportedWord();
	public abstract ValueTask<IList<string>?> GetWordInHistories();

	public abstract void CallKeyEvent(char key, bool shift, bool hangul, int upDelay, int shiftUpDelay);
	public abstract void UpdateChat(string input);
	public abstract void ClickSubmit();

	public async virtual Task RegisterInGameFunctions(ISet<int> alreadyRegistered) { }
}