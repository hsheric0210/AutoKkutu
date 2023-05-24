using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Handlers.JavaScript.Handlers;

public abstract class JavaScriptHandlerBase : HandlerBase
{
	public override BrowserBase Browser { get; }

	protected JavaScriptHandlerBase(BrowserBase browser) => Browser = browser;

	#region Handler implementation
	public override bool IsGameInProgress => Browser.EvaluateJavaScriptBool(GetRegisteredJSFunctionName(CommonFunctionNames.GameInProgress), errorMessage: nameof(IsGameInProgress));

	public override bool IsMyTurn => Browser.EvaluateJavaScriptBool(GetRegisteredJSFunctionName(CommonFunctionNames.IsMyTurn), errorMessage: nameof(IsMyTurn));

	public override string PresentedWord => Browser.EvaluateJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.PresentedWord), errorMessage: nameof(PresentedWord));

	public override string RoundText => Browser.EvaluateJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.RoundText), errorMessage: nameof(RoundText));

	public override int RoundIndex => Browser.EvaluateJavaScriptInt(GetRegisteredJSFunctionName(CommonFunctionNames.RoundIndex), errorMessage: nameof(RoundIndex));

	public override string UnsupportedWord => Browser.EvaluateJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.UnsupportedWord), errorMessage: nameof(UnsupportedWord));

	public override GameMode GameMode
	{
		get
		{
			var gameMode = Browser.EvaluateJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.GameMode), errorMessage: nameof(GameMode));
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
	}

	public override float TurnTime => float.TryParse(Browser.EvaluateJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.TurnTime), errorMessage: nameof(TurnTime)), out var time) && time > 0 ? time : 150;

	public override float RoundTime => float.TryParse(Browser.EvaluateJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.RoundTime), errorMessage: nameof(RoundTime)), out var time) && time > 0 ? time : 150;

	public override string ExampleWord => Browser.EvaluateJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.ExampleWord), errorMessage: nameof(ExampleWord)).Trim();

	public override string MissionChar => Browser.EvaluateJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.MissionChar), errorMessage: nameof(MissionChar)).Trim();

	public override string GetWordInHistory(int index)
	{
		if (index is < 0 or >= 6)
			throw new ArgumentOutOfRangeException($"index: {index}");
		return Browser.EvaluateJavaScript($"{GetRegisteredJSFunctionName(CommonFunctionNames.WordHistory, false)}({index})", errorMessage: nameof(GetWordInHistory));
	}

	public override void UpdateChat(string input) => Browser.ExecuteJavaScript($"{GetRegisteredJSFunctionName(CommonFunctionNames.UpdateChat, false)}('{input}')", errorMessage: nameof(UpdateChat));

	public override void ClickSubmit() => Browser.ExecuteJavaScript(GetRegisteredJSFunctionName(CommonFunctionNames.ClickSubmit), errorMessage: nameof(ClickSubmit));
	#endregion

	public override void RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.GameInProgress, "", "var s=document.getElementsByClassName('GameBox Product')[0]?.style;return s!=undefined&&(s?.display ? s.display!='none' : s.height!='');");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.GameMode, "", "let s=document.getElementsByClassName('room-head-mode')[0]?.textContent?.split('/')[0]?.trim();return s?.substring(s.indexOf(' ')+1)||''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.PresentedWord, "", "return document.getElementsByClassName('jjo-display ellipse')[0]?.textContent||''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.RoundText, "", "return document.getElementsByClassName('rounds-current')[0]?.textContent||''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.IsMyTurn, "", "let s=document.getElementsByClassName('game-input')[0];return s!=undefined&&s.style.display!='none'");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.UnsupportedWord, "", "return document.getElementsByClassName('game-fail-text')[0]?.textContent||''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.TurnTime, "", "let s=document.querySelector(\"[class='graph jjo-turn-time']>[class='graph-bar']\")?.textContent;return s?.substring(0,s.length-1)||''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.RoundTime, "", "let s=document.querySelector(\"[class='graph jjo-round-time']>[class='graph-bar']\")?.textContent;return s?.substring(0,s.length-1)||''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.RoundIndex, "", "return Array.from(document.querySelectorAll('#Middle>div.GameBox.Product>div>div.game-head>div.rounds>label')).indexOf(document.querySelector('.rounds-current'))");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.ExampleWord, "", "let s=document.getElementsByClassName('jjo-display ellipse')[0];return (s&&s.innerHTML.includes('label')&&s.innerHTML.includes('color')&&s.innerHTML.includes('170,')) ? s.textContent : ''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.MissionChar, "", "let s=document.getElementsByClassName('items')[0];return s&&s.style.opacity>=1 ? s.textContent : ''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.WordHistory, "i", "return document.getElementsByClassName('ellipse history-item expl-mother')[i]?.innerHTML||''");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.UpdateChat, "input", "document.querySelector('[id=\"Talk\"]').value=input");
		RegisterJavaScriptFunction(alreadyRegistered, CommonFunctionNames.ClickSubmit, "", "document.getElementById('ChatBtn').click()");
	}
}
