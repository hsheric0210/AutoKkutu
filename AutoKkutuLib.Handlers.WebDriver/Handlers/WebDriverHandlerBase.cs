using AutoKkutuLib.Extension;
using AutoKkutuLib.Selenium;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools.V111.CSS;
using OpenQA.Selenium.Internal;
using Serilog;

namespace AutoKkutuLib.Handlers.WebDriver.Handlers;

public abstract class WebDriverHandlerBase : HandlerBase
{
	public override SeleniumBrowser Browser { get; }

	protected WebDriverHandlerBase(SeleniumBrowser browser) => Browser = browser;

	#region Handler implementation
	public override bool IsGameInProgress
	{
		get
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
	}

	public override bool IsMyTurn
	{
		get
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
	}

	public override string PresentedWord
	{
		get
		{
			try
			{
				return Browser.FindElementQuery("[class='jjo-display ellipse']")?.Text?.Trim() ?? "";
			}
			catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
		}
	}

	public override string RoundText
	{
		get
		{
			try
			{
				return Browser.FindElementClassName("rounds-current")?.Text?.Trim() ?? "";
			}
			catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
		}
	}

	public override int RoundIndex
	{
		get
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
	}

	public override string UnsupportedWord
	{
		get
		{
			try
			{
				return Browser.FindElementClassName("game-fail-text")?.Text?.Trim() ?? "";
			}
			catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
		}
	}

	public override GameMode GameMode
	{
		get
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
	}

	public override float TurnTime
	{
		get
		{
			try
			{
				return float.TryParse(Browser.FindElementQuery("[class='graph jjo-turn-time']>[class='graph-bar']")?.Text?.TrimEnd('초'), out var time) ? time : 150;
			}
			catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return 150; }
		}
	}

	public override float RoundTime
	{
		get
		{
			try
			{
				return float.TryParse(Browser.FindElementQuery("[class='graph jjo-round-time']>[class='graph-bar round-extreme']")?.Text?.TrimEnd('초'), out var time) ? time : 150;
			}
			catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return 150; }
		}
	}

	public override string ExampleWord
	{
		get
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
	}

	public override string MissionChar
	{
		get
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
	}

	public override string GetWordInHistory(int index)
	{
		try
		{
			if (index is < 0 or >= 6)
				throw new ArgumentOutOfRangeException($"index: {index}");
			var list = Browser.FindElementsQuery("[class='ellipse history-item expl-mother']");
			return list == null || list.Count <= index ? "" : list[index].GetAttribute("innerHTML") ?? "";
		}
		catch (Exception ex) when (ex is UnhandledAlertException or NullReferenceException or StaleElementReferenceException) { return ""; }
	}

	public override void UpdateChat(string input)
	{
		try
		{
			Browser.ExecuteJavaScript($"{GetRegisteredJSFunctionName(CommonFunctionNames.UpdateChat)}({input})");
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

	public override void RegisterInGameFunctions() => RegisterJavaScriptFunction(CommonFunctionNames.UpdateChat, "input", "document.getElementById('Talk').value=input");
}
