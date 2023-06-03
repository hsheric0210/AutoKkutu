using AutoKkutuLib.Browser;

namespace AutoKkutuLib.Game.DomHandlers;

public abstract class DomHandlerBase
{
	public abstract string HandlerName { get; }
	public abstract IReadOnlyCollection<Uri> UrlPattern { get; }
	public abstract BrowserBase Browser { get; }

	public abstract Task<GameMode> GetGameMode();
	public abstract Task<bool> GetIsGameInProgress();
	public abstract Task<bool> GetIsMyTurn();
	public abstract Task<string> GetPresentedWord();
	public abstract Task<string> GetMissionChar();
	public abstract Task<string> GetExampleWord();
	public abstract Task<int> GetRoundIndex();
	public abstract Task<string> GetRoundText();
	public abstract Task<float> GetRoundTime();
	public abstract Task<float> GetTurnTime();
	public abstract Task<string> GetUnsupportedWord();
	public abstract Task<string> GetWordInHistory(int index);

	public abstract void UpdateChat(string input);
	public abstract void ClickSubmit();

	public async virtual Task RegisterInGameFunctions(ISet<int> alreadyRegistered) { }
}