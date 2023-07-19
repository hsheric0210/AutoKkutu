using AutoKkutuLib.Browser;
using AutoKkutuLib.Properties;

namespace AutoKkutuLib.Game.DomHandlers;

public sealed class BasicBypassDomHandler : BasicDomHandler
{
	private readonly BrowserRandomNameMapping mapping;

	public override string HandlerName => "BasicBypassDomHandler";
	public override string HandlerDetails => "Basic DOM handler with fake DOM element bypass";

	public BasicBypassDomHandler(BrowserBase browser) : base(browser)
	{
		var names = BrowserRandomNameMapping.BaseJs(browser);
		names.GenerateScriptType("___funcRegistered___", CommonNameRegistry.FunctionsRegistered); // 비록 스크립트 내에서 사용되지는 않으나, 아래 RegisterInGameFunctions()에서 검사를 수행하기 위함
		names.GenerateScriptType("___getChatBox___", CommonNameRegistry.GetChatBox);
		names.GenerateScriptType("___clickSubmit___", CommonNameRegistry.ClickSubmit);
		names.GenerateScriptType("___chatBox___", CommonNameRegistry.ChatBoxCache);
		names.GenerateScriptType("___chatBtn___", CommonNameRegistry.ChatBtnCache);

		LibLogger.Debug<BasicBypassDomHandler>("bypassHandler name mapping: {nameRandom}", names);

		mapping = names;
	}

	public override async ValueTask RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		if (!await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.FunctionsRegistered)))
		{
			LibLogger.Warn<BasicBypassDomHandler>("bypassHandler injection result: {result}", await Browser.EvaluateJavaScriptAsync(mapping.ApplyTo(LibResources.bypassDomHandlerJs)));
			await base.RegisterInGameFunctions(alreadyRegistered);
		}
	}
}
