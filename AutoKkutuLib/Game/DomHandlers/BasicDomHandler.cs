using AutoKkutuLib.Browser;
using AutoKkutuLib.Properties;
using System.Collections.Immutable;

namespace AutoKkutuLib.Game.DomHandlers;

public class BasicDomHandler : IDomHandler
{
	public virtual string HandlerName => "BasicDomHandler";
	public virtual string HandlerDetails => "JJoriping-compatible basic DOM Handler";

	public BrowserBase Browser { get; }

	private readonly BrowserRandomNameMapping mapping;

	public BasicDomHandler(BrowserBase browser)
	{
		Browser = browser;

		// functions
		var names = BrowserRandomNameMapping.BaseJs(browser);
		names.GenerateScriptType("___getGameMode___", CommonNameRegistry.GameMode);
		names.GenerateScriptType("___getPresentWord___", CommonNameRegistry.PresentedWord);
		names.GenerateScriptType("___getWordLength___", CommonNameRegistry.GetWordLength);
		names.GenerateScriptType("___isMyTurn___", CommonNameRegistry.IsMyTurn);
		names.GenerateScriptType("___getTurnError___", CommonNameRegistry.TurnError);
		names.GenerateScriptType("___getTurnTime___", CommonNameRegistry.TurnTime);
		names.GenerateScriptType("___getRoundTime___", CommonNameRegistry.RoundTime);
		names.GenerateScriptType("___getRoundIndex___", CommonNameRegistry.RoundIndex);
		names.GenerateScriptType("___getTurnHint___", CommonNameRegistry.TurnHint);
		names.GenerateScriptType("___getMissionChar___", CommonNameRegistry.MissionChar);
		names.GenerateScriptType("___getWordHistory___", CommonNameRegistry.WordHistory);
		names.GenerateScriptType("___getChatBox___", CommonNameRegistry.GetChatBox);
		names.GenerateScriptType("___getTurnIndex___", CommonNameRegistry.GetTurnIndex);
		names.GenerateScriptType("___getUserId___", CommonNameRegistry.GetUserId);
		names.GenerateScriptType("___getGameSeq___", CommonNameRegistry.GetGameSeq);
		names.GenerateScriptType("___sendKeyEvents___", CommonNameRegistry.SendKeyEvents);
		names.GenerateScriptType("___updateChat___", CommonNameRegistry.UpdateChat);
		names.GenerateScriptType("___clickSubmit___", CommonNameRegistry.ClickSubmit);
		names.GenerateScriptType("___appendChat___", CommonNameRegistry.AppendChat);
		names.GenerateScriptType("___focusChat___", CommonNameRegistry.FocusChat);
		names.GenerateScriptType("___funcRegistered___", CommonNameRegistry.FunctionsRegistered);

		// cache properties
		names.GenerateScriptType("___roomHeadMode___", CommonNameRegistry.RoomHeadModeCache);
		names.GenerateScriptType("___gameDisplay___", CommonNameRegistry.GameDisplayCache);
		names.GenerateScriptType("___wordLengthDisplay___", CommonNameRegistry.WordLengthDisplayCache);
		names.GenerateScriptType("___myInputDisplay___", CommonNameRegistry.MyInputDisplayCache);
		names.GenerateScriptType("___turnTimeDisplay___", CommonNameRegistry.TurnTimeDisplayCache);
		names.GenerateScriptType("___roundTimeDisplay___", CommonNameRegistry.RoundTimeDisplayCache);
		names.GenerateScriptType("___chatBox___", CommonNameRegistry.ChatBoxCache);
		names.GenerateScriptType("___chatBtn___", CommonNameRegistry.ChatBtnCache);
		names.GenerateScriptType("___shiftState___", CommonNameRegistry.ShiftState);

		LibLogger.Debug<BasicDomHandler>("baseHandler name mapping: {nameRandom}", names);

		mapping = names;
	}

	#region Handler implementation
	public virtual async ValueTask<bool> GetIsMyTurn() => await Browser.EvaluateJavaScriptBoolAsync(GetScriptNoArgFunctionName(CommonNameRegistry.IsMyTurn), errorMessage: nameof(GetIsMyTurn));

	public virtual async ValueTask<string> GetPresentedWord() => await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.PresentedWord), errorPrefix: nameof(GetPresentedWord));

	public virtual async ValueTask<int> GetWordLength() => int.TryParse(await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.GetWordLength), errorPrefix: nameof(GetWordLength)), out var length) ? length : 3;

	public virtual async ValueTask<int> GetRoundIndex() => await Browser.EvaluateJavaScriptIntAsync(GetScriptNoArgFunctionName(CommonNameRegistry.RoundIndex), errorPrefix: nameof(GetRoundIndex));

	public virtual async ValueTask<string> GetUnsupportedWord() => await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.TurnError), errorPrefix: nameof(GetUnsupportedWord));

	public virtual async ValueTask<GameMode> GetGameMode()
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

				case "한국어 전체":
					return GameMode.AllKorean;

				case "영어 전체":
					return GameMode.AllEnglish;

				case "자유":
					return GameMode.Free;

				case "자유 끝말잇기":
					return GameMode.LastAndFirstFree;

				case "타자 대결":
					return GameMode.TypingBattle;

				case "훈민정음":
					return GameMode.Hunmin;

				default:
					LibLogger.Warn<BasicDomHandler>("Unsupported game mode: {gameMode}", gameMode);
					break;
			}
		}
		return GameMode.None;
	}

	public virtual async ValueTask<float> GetTurnTime() => float.TryParse(await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.TurnTime), errorPrefix: nameof(GetTurnTime)), out var time) && time > 0 ? time : 150;

	public virtual async ValueTask<float> GetRoundTime() => float.TryParse(await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.RoundTime), errorPrefix: nameof(GetRoundTime)), out var time) && time > 0 ? time : 150;

	public virtual async ValueTask<string> GetExampleWord() => (await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.TurnHint), errorPrefix: nameof(GetExampleWord))).Trim();

	public virtual async ValueTask<string> GetMissionChar() => (await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.MissionChar), errorPrefix: nameof(GetMissionChar))).Trim();

	public virtual async ValueTask<IImmutableList<string>> GetWordInHistories() => await Browser.EvaluateJavaScriptArrayAsync(GetScriptNoArgFunctionName(CommonNameRegistry.WordHistory), "", errorPrefix: nameof(GetWordInHistories));

	public virtual async ValueTask<int> GetTurnIndex() => await Browser.EvaluateJavaScriptIntAsync(GetScriptNoArgFunctionName(CommonNameRegistry.GetTurnIndex), errorPrefix: nameof(GetTurnIndex));

	public virtual async ValueTask<string> GetUserId() => await Browser.EvaluateJavaScriptAsync(GetScriptNoArgFunctionName(CommonNameRegistry.GetUserId), errorPrefix: nameof(GetUserId));

	public virtual async ValueTask<IImmutableList<string>> GetGameSeq() => await Browser.EvaluateJavaScriptArrayAsync(GetScriptNoArgFunctionName(CommonNameRegistry.GetGameSeq), "", nameof(GetGameSeq));

	public virtual void UpdateChat(string input) => Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.UpdateChat)}('{input}')", errorMessage: nameof(UpdateChat));

	public virtual void ClickSubmit() => Browser.ExecuteJavaScript(GetScriptNoArgFunctionName(CommonNameRegistry.ClickSubmit), errorMessage: nameof(ClickSubmit));

	public virtual void AppendChat(string textUpdate, bool sendEvents, char key, bool shift, bool hangul, int upDelay)
		=> Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.AppendChat)}('{textUpdate}',{(sendEvents ? "1" : "0")},'{key}',{(shift ? "1" : "0")},{(hangul ? "1" : "0")},{upDelay})", errorMessage: nameof(AppendChat));

	public virtual void FocusChat() => Browser.ExecuteJavaScript(GetScriptNoArgFunctionName(CommonNameRegistry.FocusChat), errorMessage: nameof(FocusChat));
	#endregion

	protected string GetScriptNoArgFunctionName(CommonNameRegistry id) => Browser.GetScriptTypeName(id) + "()";

	public virtual async ValueTask RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		if (!await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.FunctionsRegistered)))
		{
			var script = mapping.ApplyTo(LibResources.baseDomHandlerJs);
			LibLogger.Debug<BasicDomHandler>("baseHandler injection result: {result}", await Browser.EvaluateJavaScriptRawAsync(script));
		}
	}
}
