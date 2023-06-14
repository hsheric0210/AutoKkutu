using System.Text;

namespace AutoKkutuLib.Browser;
public sealed class NameRandomizer
{
	private readonly BrowserBase browser;
	private readonly IDictionary<string, string> mapping = new Dictionary<string, string>();

	private NameRandomizer(BrowserBase browser) => this.browser = browser;

	public void Add(string key, CommonNameRegistry id) => Add(key, (int)id);

	public void Add(string key, int id) => mapping.Add(key, browser.GenerateScriptTypeName(id, true));

	public void Add(string key, string value) => mapping.Add(key, value);

	public string ApplyTo(string target)
	{
		foreach ((var from, var to) in mapping)
			target = target.Replace(from, to);
		return target;
	}

	public override string ToString() => new StringBuilder().Append('{').AppendJoin(", ", mapping.Select(kv => $"\"{kv.Key}\": \"{kv.Value}\"")).Append('}').ToString(); // Json type :)

	public static NameRandomizer CreateForWsHook(BrowserBase browser)
	{
		var instance = new NameRandomizer(browser);
		instance.Add("___wsHook___", CommonNameRegistry.WsHook);
		instance.Add("___originalWS___", CommonNameRegistry.WsOriginal);
		instance.Add("___wsFilter___", CommonNameRegistry.WsFilter);
		instance.Add("___nativeSend___", CommonNameRegistry.WsNativeSend);
		instance.Add("___nativeAddEventListener___", CommonNameRegistry.WsNativeAddEventListener);
		instance.Add("___passthru___", CommonNameRegistry.WsPassThru);
		instance.Add("___WSProtoBackup___", 1923);
		return instance;
	}
}
