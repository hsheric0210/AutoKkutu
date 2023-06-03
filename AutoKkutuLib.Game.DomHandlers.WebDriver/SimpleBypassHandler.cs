using AutoKkutuLib.Browser;
using AutoKkutuLib.Selenium;
using OpenQA.Selenium;

namespace AutoKkutuLib.Handlers.WebDriver;

internal class SimpleBypassHandler : WebDriverHandlerBase
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] {
		new Uri("https://bfkkutu.kr/"),
		new Uri("https://kkutu.co.kr/"),
		new Uri("https://kkutu.io/")
	};

	public override string HandlerName => "Simple Fake-element Bypassing Handler";

	public SimpleBypassHandler(SeleniumBrowser browser) : base(browser)
	{
	}

	public override void ClickSubmit()
	{
		Browser.ExecuteJavaScript($"{Browser.GetScriptTypeName(CommonNameRegistry.ClickSubmit)}");
	}

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.UpdateChat, "input", "Array.prototype.find.call(document.querySelectorAll('#Middle>div.ChatBox.Product>div.product-body>input'),e=>window.getComputedStyle(e).display!='none')?.value=input");
		Browser.GenerateScriptTypeName(alreadyRegistered, CommonNameRegistry.ClickSubmit, "", "Array.prototype.find.call(document.querySelectorAll('#Middle>div.ChatBox.Product>div.product-body>button'),e=>window.getComputedStyle(e).display!='none')?.click()");
		base.RegisterInGameFunctions(alreadyRegistered);
	}
}
