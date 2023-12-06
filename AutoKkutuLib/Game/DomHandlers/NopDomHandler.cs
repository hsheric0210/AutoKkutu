using AutoKkutuLib.Browser;
using System.Collections.Immutable;

namespace AutoKkutuLib.Game.DomHandlers;

public sealed class NopDomHandler : IDomHandler
{
	public string HandlerName => "NopHandler";
	public string HandlerDetails => "This DOM Handler does literally nothing";

	public BrowserBase Browser { get; }

	public NopDomHandler(BrowserBase browser) => Browser = browser;

	#region Handler implementation
	public ValueTask<bool> GetIsMyTurn() => ValueTask.FromResult(false);

	public ValueTask<string> GetPresentedWord() => ValueTask.FromResult("");

	public ValueTask<int> GetWordLength() => ValueTask.FromResult(3);

	public ValueTask<int> GetRoundIndex() => ValueTask.FromResult(0);

	public ValueTask<string> GetUnsupportedWord() => ValueTask.FromResult("");

	public ValueTask<GameMode> GetGameMode() => ValueTask.FromResult(GameMode.LastAndFirst);

	public ValueTask<float> GetTurnTime() => ValueTask.FromResult(300f);

	public ValueTask<float> GetRoundTime() => ValueTask.FromResult(300f);

	public ValueTask<string> GetExampleWord() => ValueTask.FromResult("");

	public ValueTask<string> GetMissionChar() => ValueTask.FromResult("");

	public ValueTask<IImmutableList<string>> GetWordInHistories() => ValueTask.FromResult<IImmutableList<string>>(ImmutableList<string>.Empty);
	public ValueTask<int> GetTurnIndex() => ValueTask.FromResult(-1);
	public ValueTask<string> GetUserId() => ValueTask.FromResult("");
	public ValueTask<IImmutableList<string>> GetGameSeq() => ValueTask.FromResult<IImmutableList<string>>(ImmutableList<string>.Empty);

	public void UpdateChat(string input)
	{
		// nop
	}

	public void ClickSubmit()
	{
		// nop
	}

	public void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay)
	{
		// nop
	}
	public void FocusChat()
	{
		// nop
	}
	#endregion

	public ValueTask RegisterInGameFunctions(ISet<int> alreadyRegistered) => ValueTask.CompletedTask;
}
