using Microsoft.Win32.SafeHandles;

namespace AutoKkutuLib.Handlers.JavaScript.Handlers;

internal class SimpleBypassHandler : JavaScriptHandlerBase
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] {
		new Uri("https://kkutu.co.kr/"),
		new Uri("https://bfkkutu.kr/"),

	};

	public override string HandlerName => "Simple Fake-element Bypassing Handler";

	public SimpleBypassHandler(BrowserBase jsEvaluator) : base(jsEvaluator)
	{
	}

	public override void RegisterInGameFunctions()
	{
		RegisterJavaScriptFunction(CommonFunctionNames.UpdateChat, "input", "Array.prototype.find.call(document.querySelectorAll('#Middle>div.ChatBox.Product>div.product-body>input'),e=>window.getComputedStyle(e).display!='none')?.value=input");
		RegisterJavaScriptFunction(CommonFunctionNames.ClickSubmit, "", "Array.prototype.find.call(document.querySelectorAll('#Middle>div.ChatBox.Product>div.product-body>button'),e=>window.getComputedStyle(e).display!='none')?.click()");
		base.RegisterInGameFunctions();
	}
}
