using AutoKkutuLib.Browser;
using AutoKkutuLib.Properties;
using Serilog;

namespace AutoKkutuLib.Game.DomHandlers;

public sealed class BasicBypassDomHandler : BasicDomHandler
{
	private readonly BrowserRandomNameMapping mapping;

	public override string HandlerName => "BasicBypassHandler";
	public override string HandlerDetails => "Basic DOM handler with fake DOM element bypass";

	public BasicBypassDomHandler(BrowserBase browser) : base(browser)
	{
		var names = BrowserRandomNameMapping.BaseJs(browser);
		names.GenerateScriptType("___funcRegistered___", CommonNameRegistry.FunctionsRegistered); // 비록 스크립트 내에서 사용되지는 않으나, 아래 RegisterInGameFunctions()에서 검사를 수행하기 위함
		names.GenerateScriptType("___updateChat___", CommonNameRegistry.UpdateChat);
		names.GenerateScriptType("___clickSubmit___", CommonNameRegistry.ClickSubmit);
		names.GenerateScriptType("___chatBox___", CommonNameRegistry.ChatBoxCache);
		names.GenerateScriptType("___chatBtn___", CommonNameRegistry.ChatBtnCache);

		Log.Debug("bypassHandler name mapping: {nameRandom}", names);

		mapping = names;
	}

	public override async Task RegisterInGameFunctions(ISet<int> alreadyRegistered)
	{
		if (!await Browser.EvaluateJavaScriptBoolAsync(Browser.GetScriptTypeName(CommonNameRegistry.FunctionsRegistered)))
		{
			Log.Warning("bypassHandler injection result: {result}", await Browser.EvaluateJavaScriptAsync(mapping.ApplyTo(LibResources.bypassDomHandlerJs)));
			await base.RegisterInGameFunctions(alreadyRegistered);
		}
	}
}
