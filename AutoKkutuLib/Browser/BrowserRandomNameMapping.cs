namespace AutoKkutuLib.Browser;
public sealed class BrowserRandomNameMapping : NameMapping
{
	private readonly BrowserBase browser;

	public BrowserRandomNameMapping(BrowserBase browser)
	{
		this.browser = browser;
		Generate("___injectionNamespace___", CommonNameRegistry.InjectionNamespace); // The object all injected things should be located in (e.g. 'window.s9TinU2gq05iep02R6q')
	}

	public void Generate(string key, CommonNameRegistry id) => Generate(key, (int)id);

	public void Generate(string key, int id) => Add(key, browser.GenerateRandomString(id));

	public void GenerateScriptType(string key, CommonNameRegistry id) => GenerateScriptType(key, (int)id);

	public void GenerateScriptType(string key, int id) => Add(key, browser.GenerateScriptTypeName(id));

	public static BrowserRandomNameMapping MainHelperJs(BrowserBase browser)
	{
		BrowserRandomNameMapping instance = BaseJs(browser);
		instance.GenerateScriptType("___originalWS___", CommonNameRegistry.OriginalWebSocket);
		instance.GenerateScriptType("___wsFilter___", CommonNameRegistry.WebSocketFilter);
		instance.Generate("___nativeSend___", CommonNameRegistry.WebSocketNativeSend);
		instance.Generate("___nativeAddEventListener___", CommonNameRegistry.WebSocketNativeAddEventListener);
		instance.Generate("___passthru___", CommonNameRegistry.WebSocketPassThru);
		instance.GenerateScriptType("___originalWSPrototype___", CommonNameRegistry.OriginalWebSocketPrototype);
		instance.Generate("___injectedOnMessageListeners___", CommonNameRegistry.InjectedWebSocketMessageHandlerList);
		instance.GenerateScriptType("___commSend___", CommonNameRegistry.CommunicateSend);
		instance.GenerateScriptType("___commRecv___", CommonNameRegistry.CommunicateReceive);
		return instance;
	}

	public static BrowserRandomNameMapping BaseJs(BrowserBase browser)
	{
		var instance = new BrowserRandomNameMapping(browser);

		// Global object backups
		instance.GenerateScriptType("___getComputedStyle___", CommonNameRegistry.GetComputedStyle);
		instance.GenerateScriptType("___consoleLog___", CommonNameRegistry.ConsoleLog);
		instance.GenerateScriptType("___setTimeout___", CommonNameRegistry.SetTimeout);
		instance.GenerateScriptType("___setInterval___", CommonNameRegistry.SetInterval);

		instance.GenerateScriptType("___dispatchEvent___", CommonNameRegistry.DispatchEvent);
		instance.GenerateScriptType("___getElementsByClassName___", CommonNameRegistry.GetElementsByClassName);
		instance.GenerateScriptType("___querySelector___", CommonNameRegistry.QuerySelector);
		instance.GenerateScriptType("___querySelectorAll___", CommonNameRegistry.QuerySelectorAll);
		instance.GenerateScriptType("___getElementById___", CommonNameRegistry.GetElementById);

		return instance;
	}

	public override string ApplyTo(string target) => base.ApplyTo(target.Replace("___baseNamespace___", browser.JavaScriptBaseNamespace));
}
