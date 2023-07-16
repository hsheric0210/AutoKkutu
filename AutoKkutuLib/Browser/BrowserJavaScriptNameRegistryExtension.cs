namespace AutoKkutuLib.Browser;
public static class BrowserJavaScriptNameRegistryExtension
{
	public static string GenerateRandomString(this BrowserBase browser, CommonNameRegistry funcId) => browser.GenerateRandomString((int)funcId);

	public static string GenerateScriptTypeName(this BrowserBase browser, CommonNameRegistry funcId) => browser.GenerateScriptTypeName((int)funcId);

	public static string GetScriptTypeName(this BrowserBase browser, CommonNameRegistry funcId) => browser.GetScriptTypeName((int)funcId);

	public static string GetRandomString(this BrowserBase browser, CommonNameRegistry funcId) => browser.GetRandomString((int)funcId);
}
