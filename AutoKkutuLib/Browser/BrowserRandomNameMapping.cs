namespace AutoKkutuLib.Browser;
public sealed class BrowserRandomNameMapping : NameMapping
{
	private readonly BrowserBase browser;

	public BrowserRandomNameMapping(BrowserBase browser) => this.browser = browser;

	public void Generate(string key, CommonNameRegistry id) => Generate(key, (int)id);

	public void Generate(string key, int id) => Add(key, browser.GenerateRandomString(id));

	public void GenerateScriptType(string key, CommonNameRegistry id) => GenerateScriptType(key, (int)id);

	public void GenerateScriptType(string key, int id) => Add(key, browser.GenerateScriptTypeName(id));

	public static BrowserRandomNameMapping MainHelperJs(BrowserBase browser)
	{
		var instance = new BrowserRandomNameMapping(browser);
		instance.GenerateScriptType("___originalWS___", CommonNameRegistry.OriginalWebSocket);
		instance.GenerateScriptType("___wsFilter___", CommonNameRegistry.WebSocketFilter);
		instance.Generate("___nativeSend___", CommonNameRegistry.WebSocketNativeSend);
		instance.Generate("___nativeAddEventListener___", CommonNameRegistry.WebSocketNativeAddEventListener);
		instance.Generate("___passthru___", CommonNameRegistry.WebSocketPassThru);
		instance.GenerateScriptType("___originalWSPrototype___", CommonNameRegistry.OriginalWebSocketPrototype);
		instance.Generate("___injectedOnMessageListeners___", CommonNameRegistry.InjectedWebSocketMessageHandlerList);
		instance.Generate("___commSend___", CommonNameRegistry.CommunicateSend);
		instance.Generate("___commRecv___", CommonNameRegistry.CommunicateReceive);
		instance.GenerateScriptType("___getComputedStyle___", CommonNameRegistry.InjectedWebSocketMessageHandlerList);
		instance.Add("___baseNamespace___", browser.JavaScriptBaseNamespace + '.' + browser.GetRandomString(CommonNameRegistry.Namespace)); // Note that namespace random string is initialized on browser class initialization.
		return instance;
	}
}
