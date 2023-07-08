using AutoKkutuLib.Browser;
using AutoKkutuLib.Game.DomHandlers.JavaScript.Properties;
using Serilog;

namespace AutoKkutuLib.Handlers.JavaScript;

internal sealed class SimpleBypassHandler : JavaScriptHandlerBase
{
	public override IReadOnlyCollection<Uri> UrlPattern => new Uri[] {
		new Uri("https://kkutu.co.kr/"),
		new Uri("https://bfkkutu.kr/"),
		new Uri("https://kkutu.io/")
	};
	private readonly BrowserRandomNameMapping mapping;

	public override string HandlerName => "Simple Fake-element Bypassing Handler";

	public SimpleBypassHandler(BrowserBase browser) : base(browser)
	{
		var mapping = new BrowserRandomNameMapping(browser);
		mapping.GenerateScriptType("___getComputedStyle___", CommonNameRegistry.GetComputedStyle); // 브라우저 초기화 시 랜덤 생성되고, mainInjected.js 에서 값이 초기화된다
		mapping.GenerateScriptType("___funcRegistered___", CommonNameRegistry.FunctionsRegistered); // 아래 RegisterInGameFunctions()에서 검사를 수행하기 위함
		mapping.GenerateScriptType("___updateChat___", CommonNameRegistry.UpdateChat);
		mapping.GenerateScriptType("___clickSubmit___", CommonNameRegistry.ClickSubmit);
		mapping.GenerateScriptType("___chatBox___", 13707);
		mapping.GenerateScriptType("___chatBtn___", 13708);

		Log.Debug("bypassHandler name mapping: {nameRandom}", mapping);

		this.mapping = mapping;
	}

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		if (!await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.FunctionsRegistered)))
		{
			await Browser.EvaluateJavaScriptAsync(mapping.ApplyTo(JsResources.bypassHandler));
			await base.RegisterInGameFunctions(alreadyRegistered);
		}
	}
}
