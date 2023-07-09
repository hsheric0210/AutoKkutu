using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.DomHandlers.JavaScript.Properties;
using Serilog;

namespace AutoKkutuLib.Handlers.JavaScript;

public abstract class JavaScriptHandlerBase : DomHandlerBase
{
	public override BrowserBase Browser { get; }
	private readonly BrowserRandomNameMapping mapping;

	protected JavaScriptHandlerBase(BrowserBase browser)
	{
		Browser = browser;

		// functions
		var names = BrowserRandomNameMapping.BaseJs(browser);
		names.GenerateScriptType("___gameInProgress___", CommonNameRegistry.GameInProgress);
		names.GenerateScriptType("___getGameMode___", CommonNameRegistry.GameMode);
		names.GenerateScriptType("___getPresentWord___", CommonNameRegistry.PresentedWord);
		names.GenerateScriptType("___isMyTurn___", CommonNameRegistry.IsMyTurn);
		names.GenerateScriptType("___getTurnError___", CommonNameRegistry.TurnError);
		names.GenerateScriptType("___getTurnTime___", CommonNameRegistry.TurnTime);
		names.GenerateScriptType("___getRoundTime___", CommonNameRegistry.RoundTime);
		names.GenerateScriptType("___getRoundIndex___", CommonNameRegistry.RoundIndex);
		names.GenerateScriptType("___getTurnHint___", CommonNameRegistry.TurnHint);
		names.GenerateScriptType("___getMissionChar___", CommonNameRegistry.MissionChar);
		names.GenerateScriptType("___getWordHistory___", CommonNameRegistry.WordHistory);
		names.GenerateScriptType("___sendKeyEvents___", CommonNameRegistry.SendKeyEvents);
		names.GenerateScriptType("___updateChat___", CommonNameRegistry.UpdateChat);
		names.GenerateScriptType("___clickSubmit___", CommonNameRegistry.ClickSubmit);
		names.GenerateScriptType("___funcRegistered___", CommonNameRegistry.FunctionsRegistered);

		// cache properties TODO: add registry id
		int i = 16999;
		names.GenerateScriptType("___roomHeadMode___", i++);
		names.GenerateScriptType("___gameDisplay___", i++);
		names.GenerateScriptType("___myInputDisplay___", i++);
		names.GenerateScriptType("___turnTimeDisplay___", i++);
		names.GenerateScriptType("___roundTimeDisplay___", i++);
		names.GenerateScriptType("___chatBox___", i++);
		names.GenerateScriptType("___chatBtn___", i);

		Log.Debug("baseHandler name mapping: {nameRandom}", names);

		mapping = names;
	}

	#region Handler implementation
	public override async ValueTask<bool> GetIsGameInProgress() => await Browser.EvaluateJavaScriptBoolAsync(GetScriptNoArgFunctionName(CommonNameRegistry.GameInProgress), errorMessage: nameof(GetIsGameInProgress));

	public override async ValueTask<bool> GetIsMyTurn() => await Browser.EvaluateJavaScriptBoolAsync(GetScriptNoArgFunctionName(CommonNameRegistry.IsMyTurn), errorMessage: nameof(GetIsMyTurn));

	public override async ValueTask<string?> GetPresentedWord() => await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.PresentedWord), errorPrefix: nameof(GetPresentedWord));

	public override async ValueTask<int> GetRoundIndex() => await Browser.EvaluateJavaScriptIntAsync(GetScriptNoArgFunctionName(CommonNameRegistry.RoundIndex), errorPrefix: nameof(GetRoundIndex));

	public override async ValueTask<string?> GetUnsupportedWord() => await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.TurnError), errorPrefix: nameof(GetUnsupportedWord));

	public override async ValueTask<GameMode> GetGameMode()
	{
		var gameMode = await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.GameMode), errorPrefix: nameof(GetGameMode));
		if (!string.IsNullOrWhiteSpace(gameMode))
		{
			switch (gameMode)
			{
				case "끝말잇기":
					return GameMode.LastAndFirst;

				case "앞말잇기":
					return GameMode.FirstAndLast;

				case "가운뎃말잇기":
					return GameMode.MiddleAndFirst;

				case "쿵쿵따":
					return GameMode.KungKungTta;

				case "끄투":
					return GameMode.Kkutu;

				case "전체":
					return GameMode.All;

				case "자유":
					return GameMode.Free;

				case "자유 끝말잇기":
					return GameMode.LastAndFirstFree;

				case "타자 대결":
					return GameMode.TypingBattle;

				default:
					Log.Warning("Unsupported game mode: {gameMode}", gameMode);
					break;
			}
		}
		return GameMode.None;
	}

	public override async ValueTask<float> GetTurnTime() => float.TryParse(await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.TurnTime), errorPrefix: nameof(GetTurnTime)), out var time) && time > 0 ? time : 150;

	public override async ValueTask<float> GetRoundTime() => float.TryParse(await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.RoundTime), errorPrefix: nameof(GetRoundTime)), out var time) && time > 0 ? time : 150;

	public override async ValueTask<string?> GetExampleWord() => (await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.TurnHint), errorPrefix: nameof(GetExampleWord)))?.Trim();

	public override async ValueTask<string?> GetMissionChar() => (await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.MissionChar), errorPrefix: nameof(GetMissionChar)))?.Trim();

	public override async ValueTask<IList<string>?> GetWordInHistories() => await Browser.EvaluateJavaScriptArrayAsync($"{GetScriptNoArgFunctionName(CommonNameRegistry.WordHistory)}", "", errorPrefix: nameof(GetWordInHistories));

	public override void CallKeyEvent(char key, bool shift, bool hangul, int upDelay, int shiftUpDelay) => Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.SendKeyEvents)}('{key}',{(shift ? "1" : "0")},{(hangul ? "1" : "0")},{upDelay},{shiftUpDelay})", errorMessage: nameof(CallKeyEvent));

	public override void UpdateChat(string input) => Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.UpdateChat)}('{input}')", errorMessage: nameof(UpdateChat));

	public override void ClickSubmit() => Browser.ExecuteJavaScript(GetScriptNoArgFunctionName(CommonNameRegistry.ClickSubmit), errorMessage: nameof(ClickSubmit));
	#endregion

	protected string GetScriptNoArgFunctionName(CommonNameRegistry id) => Browser.GetScriptTypeName(id) + "()";

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		if (!await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.FunctionsRegistered)))
		{
			var script = mapping.ApplyTo(JsResources.baseHandler);
			Log.Warning("baseHandler injection result: {result}", await Browser.EvaluateJavaScriptRawAsync(script));
		}
	}
}
