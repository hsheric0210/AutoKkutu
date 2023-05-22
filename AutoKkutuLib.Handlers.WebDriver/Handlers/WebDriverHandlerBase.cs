﻿using AutoKkutuLib.Extension;
using AutoKkutuLib.Selenium;
using OpenQA.Selenium;
using Serilog;

namespace AutoKkutuLib.Handlers.WebDriver.Handlers;

public abstract class WebDriverHandlerBase : HandlerBase
{
	public override SeleniumBrowserBase Browser { get; }

	protected WebDriverHandlerBase(SeleniumBrowserBase browser) => Browser = browser;

	#region Handler implementation
	public override bool IsGameInProgress
	{
		get
		{
			var display = Browser.FindElementClassName("GameBox Product", false)?.Displayed == true;
			var height = Browser.FindElementQuery("GameBox Product", false)?.GetCssValue("height");
			return display || !string.IsNullOrWhiteSpace(height);
		}
	}

	public override bool IsMyTurn => Browser.FindElementClassName("game-input", false)?.Displayed == true;

	public override string PresentedWord => Browser.FindElementClassName("jjo-display ellipse")!.Text.Trim();

	public override string RoundText => Browser.FindElementClassName("rounds-current")!.Text.Trim();

	public override int RoundIndex
	{
		get
		{
			var list = Browser.FindElementsQuery("#Middle > div.GameBox.Product > div > div.game-head > div.rounds label").ToList();
			return list.IndexOf(Browser.FindElementQuery(".rounds-current")!);
		}
	}

	public override string UnsupportedWord => Browser.FindElementClassName("game-fail-text", false)?.Text.Trim() ?? "";

	public override GameMode GameMode
	{
		get
		{
			var roomMode = Browser.FindElementClassName("room-head-mode")!.Text.Trim();
			if (!string.IsNullOrWhiteSpace(roomMode))
			{
				var trimmed = roomMode.Split('/')[0].Trim();
				switch (trimmed[(trimmed.IndexOf(' ', StringComparison.Ordinal) + 1)..])
				{
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
			}
			return GameMode.LastAndFirst;
		}
	}

	public override float TurnTime => float.TryParse(Browser.FindElementQuery("[class='graph jjo-turn-time'] > [class='graph-bar']")!.Text.TrimEnd('초'), out var time) ? time : 150;

	public override float RoundTime => float.TryParse(Browser.FindElementQuery("[class='graph jjo-round-time'] > [class='graph-bar round-extreme']")!.Text.TrimEnd('초'), out var time) ? time : 150;

	public override string ExampleWord
	{
		get
		{
			IWebElement elem = Browser.FindElementClassName("jjo-display ellipse")!;
			var content = elem.Text;
			return elem.GetAttribute("type").Equals("label", StringComparison.OrdinalIgnoreCase)
				&& elem.GetCssValue("color").Contains("170,", StringComparison.Ordinal)
				&& content.Length > 1 ? content : "";
		}
	}

	public override string MissionChar
	{
		get
		{
			var elem = Browser.FindElementClassName("items")!;
			return elem.GetCssValue("opacity") == "1" ? elem.Text.Trim() : "";
		}
	}

	public override string GetWordInHistory(int index)
	{
		if (index is < 0 or >= 6)
			throw new ArgumentOutOfRangeException($"index: {index}");
		return Browser.FindElementsClassName("ellipse history-item expl-mother").ToList()[index]?.GetAttribute("innerHTML") ?? "";
	}

	public override void UpdateChat(string input) => Browser.FindElementId("Talk", false)?.SendKeys(input.Trim());

	public override void ClickSubmit() => Browser.FindElementId("ChatBtn", false)?.Click();
	#endregion
}