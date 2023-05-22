using AutoKkutuLib.Selenium;

namespace AutoKkutuLib.Handlers.WebDriver.Handlers;

internal class BasicHandler : WebDriverHandlerBase
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] {
		new Uri("https://kkutu.pink/"),
		new Uri("https://musickkutu.xyz/"),
		new Uri("https://kkutu.org/")
	};

	public override string HandlerName => "Basic Handler";

	public BasicHandler(SeleniumBrowser jsEvaluator) : base(jsEvaluator)
	{
	}
}
