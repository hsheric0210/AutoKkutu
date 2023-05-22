using AutoKkutuLib.Selenium;

namespace AutoKkutuLib.Handlers.WebDriver.Handlers;

internal class SimpleBypassHandler : WebDriverHandlerBase
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] {
		new Uri("https://bfkkutu.kr/"),
		new Uri("https://kkutu.co.kr/"),
		new Uri("https://kkutu.io/")
	};

	public override string HandlerName => "Simple Fake-element Bypassing Handler";

	public SimpleBypassHandler(SeleniumBrowserBase browser) : base(browser)
	{
	}

	public override void UpdateChat(string input) => Browser.FindElementsQuery("#Middle > div.ChatBox.Product > div.product-body > input").First(elem => elem.Displayed).SendKeys(input.Trim());

	public override void ClickSubmit() => Browser.FindElementsQuery("#Middle > div.ChatBox.Product > div.product-body > button").First(elem => elem.Displayed).Click();
}
