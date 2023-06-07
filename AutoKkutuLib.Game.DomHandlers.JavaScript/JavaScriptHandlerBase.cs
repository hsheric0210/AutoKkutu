using AutoKkutuLib.Browser;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game.DomHandlers;
using Serilog;
using System.Runtime.InteropServices.ObjectiveC;

namespace AutoKkutuLib.Handlers.JavaScript;

public abstract class JavaScriptHandlerBase : DomHandlerBase
{
	public override BrowserBase Browser { get; }

	protected JavaScriptHandlerBase(BrowserBase browser) => Browser = browser;

	#region Handler implementation
	public override async ValueTask<bool> GetIsGameInProgress() => await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.GameInProgress), errorMessage: nameof(GetIsGameInProgress));

	public override async ValueTask<bool> GetIsMyTurn() => await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.IsMyTurn), errorMessage: nameof(GetIsMyTurn));

	public override async ValueTask<string?> GetPresentedWord() => await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.PresentedWord), errorPrefix: nameof(GetPresentedWord));

	public override async ValueTask<int> GetRoundIndex() => await Browser.EvaluateJavaScriptIntAsync(Browser.GetScriptTypeName(CommonNameRegistry.RoundIndex), errorPrefix: nameof(GetRoundIndex));

	public override async ValueTask<string?> GetUnsupportedWord() => await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.TurnError), errorPrefix: nameof(GetUnsupportedWord));

	public override async ValueTask<GameMode> GetGameMode()
	{
		var gameMode = await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.GameMode), errorPrefix: nameof(GetGameMode));
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

	public override async ValueTask<float> GetTurnTime() => float.TryParse(await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.TurnTime), errorPrefix: nameof(GetTurnTime)), out var time) && time > 0 ? time : 150;

	public override async ValueTask<float> GetRoundTime() => float.TryParse(await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.RoundTime), errorPrefix: nameof(GetRoundTime)), out var time) && time > 0 ? time : 150;

	public override async ValueTask<string?> GetExampleWord() => (await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.TurnHint), errorPrefix: nameof(GetExampleWord)))?.Trim();

	public override async ValueTask<string?> GetMissionChar() => (await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.MissionChar), errorPrefix: nameof(GetMissionChar)))?.Trim();

	public override async ValueTask<IList<string>?> GetWordInHistories() => await Browser.EvaluateJavaScriptArrayAsync($"{Browser.GetScriptTypeName(CommonNameRegistry.WordHistories)}", "", errorPrefix: nameof(GetWordInHistories));

	public override void CallKeyEvent(char key, bool shift, bool hangul, int upDelay, int shiftUpDelay) => Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.CallKeyEvent, false)}('{key}',{(shift ? "1" : "0")},{(hangul ? "1" : "0")},{upDelay},{shiftUpDelay})", errorMessage: nameof(CallKeyEvent));

	public override void UpdateChat(string input) => Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.UpdateChat, false)}('{input}')", errorMessage: nameof(UpdateChat));

	public override void ClickSubmit() => Browser.ExecuteJavaScript(Browser.GetScriptTypeName(CommonNameRegistry.ClickSubmit), errorMessage: nameof(ClickSubmit));
	#endregion

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.GameInProgress, "", "var s=document.getElementsByClassName('GameBox Product')[0]?.style;return s!=undefined&&(s?.display ? s.display!='none' : s.height!='');");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.GameMode, "", "let s=document.getElementsByClassName('room-head-mode')[0]?.textContent?.split('/')[0]?.trim();return s?.substring(s.indexOf(' ')+1)||''");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.PresentedWord, "", "return document.getElementsByClassName('jjo-display ellipse')[0]?.textContent||''");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.IsMyTurn, "", "let s=document.getElementsByClassName('game-input')[0];return s!=undefined&&s.style.display!='none'");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.TurnError, "", "return document.getElementsByClassName('game-fail-text')[0]?.textContent||''");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.TurnTime, "", "let s=document.querySelector(\"[class='graph jjo-turn-time']>[class='graph-bar']\")?.textContent;return s?.substring(0,s.length-1)||''");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.RoundTime, "", "let s=document.querySelector(\"[class='graph jjo-round-time']>[class='graph-bar']\")?.textContent;return s?.substring(0,s.length-1)||''");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.RoundIndex, "", "return Array.from(document.querySelectorAll('#Middle>div.GameBox.Product>div>div.game-head>div.rounds>label')).indexOf(document.querySelector('.rounds-current'))");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.TurnHint, "", "let s=document.getElementsByClassName('jjo-display ellipse')[0];return (s&&s.innerHTML.includes('label')&&s.innerHTML.includes('color')&&s.innerHTML.includes('170,')) ? s.textContent : ''");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.MissionChar, "", "let s=document.getElementsByClassName('items')[0];return s&&s.style.opacity>=1 ? s.textContent : ''");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.WordHistories, "", "return Array.prototype.map.call(document.getElementsByClassName('ellipse history-item expl-mother'),v=>v.childNodes[0].textContent)");
		//영어->down(orig) -> up(orig)
		//한글->down(229) -> up(229) -> up(orig)
		//한글(쌍자음)->down(shift,16) -> down(229) -> up(229) -> up(orig) -> up(229) -> up(shift,16) 
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.CallKeyEvent, "key,shift,hangul,upDelay,shiftUpDelay", "function evt(type, param){document.dispatchEvent(new KeyboardEvent('key'+type,param));}let kc=key.toUpperCase().charCodeAt(0);if(shift){evt('down',{'key':'Shift','shiftKey':true,'keyCode':16});}if(hangul){evt('down',{'key':'Process','shiftKey':shift,'keyCode':229});}else{evt('down',{'key':key,'shiftKey':shift,'keyCode':kc});}window.setTimeout(function(){if(hangul){evt('up',{'key':'Process','shiftKey':shift,'keyCode':229});}evt('up',{'key':key,'shiftKey':shift,'keyCode':kc});},upDelay);if(shift){window.setTimeout(function(){if(hangul) evt('up',{'key':'Process','keyCode':229});evt('up',{'key':'Shift','shiftKey':true,'keyCode':16});},shiftUpDelay);}");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.UpdateChat, "input", "document.querySelector('[id=\"Talk\"]').value=input");
		await Browser.RegisterScriptFunction(alreadyRegistered, CommonNameRegistry.ClickSubmit, "", "document.getElementById('ChatBtn').click()");
	}
}
