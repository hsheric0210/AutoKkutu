using AutoKkutuLib.Browser;

namespace AutoKkutuLib;
public sealed class BrowserRandomNameMapping : NameMapping
{
	private readonly BrowserBase browser;

	public BrowserRandomNameMapping(BrowserBase browser) => this.browser = browser;

	public void Generate(string key, CommonNameRegistry id) => Generate(key, (int)id);

	public void Generate(string key, int id) => Add(key, browser.GenerateScriptTypeName(id, true));

	public static BrowserRandomNameMapping CreateForWsHook(BrowserBase browser)
	{
		var instance = new BrowserRandomNameMapping(browser);
		instance.Generate("___wsHook___", CommonNameRegistry.WsHook);
		instance.Generate("___originalWS___", CommonNameRegistry.WsOriginal);
		instance.Generate("___wsFilter___", CommonNameRegistry.WsFilter);
		instance.Generate("___nativeSend___", CommonNameRegistry.WsNativeSend);
		instance.Generate("___nativeAddEventListener___", CommonNameRegistry.WsNativeAddEventListener);
		instance.Generate("___passthru___", CommonNameRegistry.WsPassThru);
		instance.Generate("___WSProtoBackup___", 1923); // TODO: Assign registry
		instance.Generate("___injectedOnMessageListeners___", 1924); // TODO: Assign registry
		return instance;
	}
}
