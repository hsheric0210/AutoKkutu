using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Selenium;
using OpenQA.Selenium;

namespace AutoKkutuLib.Handlers.WebDriver;

public abstract class WebDriverHandlerBase : DomHandlerBase
{
	public override SeleniumBrowser Browser { get; }

	protected WebDriverHandlerBase(SeleniumBrowser browser) => Browser = browser;

	#region Handler implementation
	public override async ValueTask<bool> GetIsGameInProgress()
	{
		try
		{
			var elem = Browser.FindElementQuery("[class='GameBox Product']");
			if (elem == null)
				return false;

			return elem.Displayed || !string.IsNullOrWhiteSpace(elem.GetCssValue("height"));
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return false; }
	}

	public override async ValueTask<bool> GetIsMyTurn()
	{
		try
		{
			var elem = Browser.FindElementClassName("game-input");
			if (elem == null)
				return false;

			return elem.Displayed;
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return false; }
	}

	public override async ValueTask<string?> GetPresentedWord()
	{
		try
		{
			return Browser.FindElementQuery("[class='jjo-display ellipse']")?.Text?.Trim() ?? "";
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
	}

	public override async ValueTask<string?> GetRoundText()
	{
		try
		{
			return Browser.FindElementClassName("rounds-current")?.Text?.Trim() ?? "";
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
	}

	public override async ValueTask<int> GetRoundIndex()
	{
		try
		{
			var list = Browser.FindElementsQuery("#Middle>div.GameBox.Product>div>div.game-head>div.rounds>label");
			var point = Browser.FindElementQuery(".rounds-current");
			if (point == null)
				return -1;

			return list?.IndexOf(point) ?? -1;
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException or StaleElementReferenceException) { return -1; }
	}

	public override async ValueTask<string?> GetUnsupportedWord()
	{
		try
		{
			return Browser.FindElementClassName("game-fail-text")?.Text?.Trim() ?? "";
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
	}

	public override async ValueTask<GameMode> GetGameMode()
	{
		try
		{
			var roomMode = Browser.FindElementClassName("room-head-mode")?.Text?.Trim();
			if (string.IsNullOrWhiteSpace(roomMode))
				return GameMode.None;

			var trimmed = roomMode.Split('/')[0].Trim();
			switch (trimmed[(trimmed.IndexOf(' ', StringComparison.Ordinal) + 1)..])
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
			}

			return GameMode.None;
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return GameMode.None; }
	}

	public override async ValueTask<float> GetTurnTime()
	{
		try
		{
			return float.TryParse(Browser.FindElementQuery("[class='graph jjo-turn-time']>[class='graph-bar']")?.Text?.TrimEnd('초'), out var time) && time > 0 ? time : 150;
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return 150; }
	}

	public override async ValueTask<float> GetRoundTime()
	{
		try
		{
			return float.TryParse(Browser.FindElementQuery("[class='graph jjo-round-time']>[class='graph-bar round-extreme']")?.Text?.TrimEnd('초'), out var time) && time > 0 ? time : 150;
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return 150; }
	}

	public override async ValueTask<string?> GetExampleWord()
	{
		try
		{
			IWebElement? elem = Browser.FindElementQuery("[class='jjo-display ellipse']");
			if (elem == null)
				return "";

			var content = elem.Text;
			return elem.GetAttribute("type")?.Equals("label", StringComparison.OrdinalIgnoreCase) == true
				&& (elem.GetCssValue("color")?.Contains("170,", StringComparison.Ordinal) ?? true)
				&& content.Length > 1 ? content : "";
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
	}

	public override async ValueTask<string?> GetMissionChar()
	{
		try
		{
			IWebElement? elem = Browser.FindElementClassName("items");
			if (elem == null)
				return "";

			return elem.GetCssValue("opacity") == "1" ? elem.Text.Trim() : "";
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
	}

	public override async ValueTask<IList<string>?> GetWordInHistories()
	{
		try
		{
			return await Browser.EvaluateJavaScriptArrayAsync($"{Browser.GetScriptTypeName(CommonNameRegistry.WordHistories)}", "", errorPrefix: nameof(GetWordInHistories));
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return null; }
	}

	public override void CallKeyEvent(char key, bool shift, bool hangul, int upDelay, int shiftUpDelay) => Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.CallKeyEvent, false)}('{key}',{shift},{hangul},{upDelay},{shiftUpDelay})", errorMessage: nameof(CallKeyEvent));

	public override void UpdateChat(string input)
	{
		try
		{
			Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.UpdateChat)}({input})");
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { }
	}

	public override void ClickSubmit()
	{
		try
		{
			Browser.FindElementId("ChatBtn")?.Click();
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { }
	}
	#endregion

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.UpdateChat, "input", "document.getElementById('Talk').value=input");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.WordHistories, "", "return Array.prototype.map.call(document.getElementsByClassName('ellipse history-item expl-mother'),v=>v.childNodes[0].textContent)");
		await Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.CallKeyEvent, "key,shift,hangul,upDelay,shiftUpDelay", "function evt(type, param){document.dispatchEvent(new KeyboardEvent('key'+type,param));}let kc=key.toUpperCase().charCodeAt(0);if(shift){evt('down',{'key':'Shift','code':'ShiftRight','shiftKey':true,'keyCode':16});}if(hangul){evt('down',{'key':'Process','shiftKey':shift,'keyCode':229});}else{evt('down',{'key':key,'shiftKey':shift,'keyCode':kc});}window.setTimeout(function(){if(hangul){evt('up',{'key':'Process','shiftKey':shift,'keyCode':229});}evt('up',{'key':key,'shiftKey':shift,'keyCode':kc});},upDelay);if(shift){window.setTimeout(function(){if(hangul) evt('up',{'key':'Process','keyCode':229});evt('up',{'key':'Shift','code':'ShiftRight','shiftKey':true,'keyCode':16});},shiftUpDelay);}");
	}
}
