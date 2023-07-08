using AutoKkutuLib.Browser;

namespace AutoKkutuLib.Handlers.JavaScript;

internal class BasicHandler : JavaScriptHandlerBase
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] {
		new Uri("https://kkutu.pink/"),
		new Uri("https://kkutu.org/"),
		new Uri("https://musickkutu.xyz/")
	};

	public override string HandlerName => "Basic Handler";

	public BasicHandler(BrowserBase browser) : base(browser)
	{
	}
}
