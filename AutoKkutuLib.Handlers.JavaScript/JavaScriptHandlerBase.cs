using AutoKkutuLib.Browser;
using AutoKkutuLib.Browser.Events;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game.DomHandlers;
using Serilog;
using System.Runtime.InteropServices.ObjectiveC;

namespace AutoKkutuLib.Handlers.JavaScript;

public abstract class JavaScriptHandlerBase : DomHandlerBase
{
	public override BrowserBase Browser { get; }

	protected JavaScriptHandlerBase(BrowserBase browser) => Browser = browser;

	public void OnWebSocketMessage(object? sender, WebSocketMessageEventArgs args)
	{
		Log.Information("[{handlerName}] {type} websocket message:\r\n{msg}", HandlerName, args.IsReceived ? "Incoming" : "Outgoing", args.Json.ToString());
	}

	#region Handler implementation
	public override void Start() => Browser.WebSocketMessage += OnWebSocketMessage;

	public override void Stop() => Browser.WebSocketMessage -= OnWebSocketMessage;

	public override async Task<bool> GetIsGameInProgress() => await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.GameInProgress), errorMessage: nameof(GetIsGameInProgress));

	public override async Task<bool> GetIsMyTurn() => await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.IsMyTurn), errorMessage: nameof(GetIsMyTurn));

	public override async Task<string> GetPresentedWord() => await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.PresentedWord), errorPrefix: nameof(GetPresentedWord));

	public override async Task<string> GetRoundText() => await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.RoundText), errorPrefix: nameof(GetRoundText));

	public override async Task<int> GetRoundIndex() => await Browser.EvaluateJavaScriptIntAsync(Browser.GetScriptTypeName(CommonNameRegistry.RoundIndex), errorPrefix: nameof(GetRoundIndex));

	public override async Task<string> GetUnsupportedWord() => await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.UnsupportedWord), errorPrefix: nameof(GetUnsupportedWord));

	public override async Task<GameMode> GetGameMode()
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

	public override async Task<float> GetTurnTime() => float.TryParse(await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.TurnTime), errorPrefix: nameof(GetTurnTime)), out var time) && time > 0 ? time : 150;

	public override async Task<float> GetRoundTime() => float.TryParse(await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.RoundTime), errorPrefix: nameof(GetRoundTime)), out var time) && time > 0 ? time : 150;

	public override async Task<string> GetExampleWord() => (await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.ExampleWord), errorPrefix: nameof(GetExampleWord))).Trim();

	public override async Task<string> GetMissionChar() => (await Browser.EvaluateJavaScriptAsync(Browser.GetScriptTypeName(CommonNameRegistry.MissionChar), errorPrefix: nameof(GetMissionChar))).Trim();

	public override async Task<string> GetWordInHistory(int index)
	{
		if (index is < 0 or >= 6)
			throw new ArgumentOutOfRangeException($"index: {index}");
		return await Browser.EvaluateJavaScriptAsync($"{Browser.GetScriptTypeName(CommonNameRegistry.WordHistory, false)}({index})", errorPrefix: nameof(GetWordInHistory));
	}

	public override void UpdateChat(string input) => Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.UpdateChat, false)}('{input}')", errorMessage: nameof(UpdateChat));

	public override void ClickSubmit() => Browser.ExecuteJavaScript(Browser.GetScriptTypeName(CommonNameRegistry.ClickSubmit), errorMessage: nameof(ClickSubmit));
	#endregion

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.GameInProgress, "", "var s=document.getElementsByClassName('GameBox Product')[0]?.style;return s!=undefined&&(s?.display ? s.display!='none' : s.height!='');");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.GameMode, "", "let s=document.getElementsByClassName('room-head-mode')[0]?.textContent?.split('/')[0]?.trim();return s?.substring(s.indexOf(' ')+1)||''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.PresentedWord, "", "return document.getElementsByClassName('jjo-display ellipse')[0]?.textContent||''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.RoundText, "", "return document.getElementsByClassName('rounds-current')[0]?.textContent||''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.IsMyTurn, "", "let s=document.getElementsByClassName('game-input')[0];return s!=undefined&&s.style.display!='none'");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.UnsupportedWord, "", "return document.getElementsByClassName('game-fail-text')[0]?.textContent||''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.TurnTime, "", "let s=document.querySelector(\"[class='graph jjo-turn-time']>[class='graph-bar']\")?.textContent;return s?.substring(0,s.length-1)||''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.RoundTime, "", "let s=document.querySelector(\"[class='graph jjo-round-time']>[class='graph-bar']\")?.textContent;return s?.substring(0,s.length-1)||''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.RoundIndex, "", "return Array.from(document.querySelectorAll('#Middle>div.GameBox.Product>div>div.game-head>div.rounds>label')).indexOf(document.querySelector('.rounds-current'))");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.ExampleWord, "", "let s=document.getElementsByClassName('jjo-display ellipse')[0];return (s&&s.innerHTML.includes('label')&&s.innerHTML.includes('color')&&s.innerHTML.includes('170,')) ? s.textContent : ''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.MissionChar, "", "let s=document.getElementsByClassName('items')[0];return s&&s.style.opacity>=1 ? s.textContent : ''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.WordHistory, "i", "return document.getElementsByClassName('ellipse history-item expl-mother')[i]?.innerHTML||''");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.UpdateChat, "input", "document.querySelector('[id=\"Talk\"]').value=input");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.ClickSubmit, "", "document.getElementById('ChatBtn').click()");
	}
}
