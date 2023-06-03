using Serilog;

namespace AutoKkutuLib.Browser;
public static class BrowserJavaScriptNameRegistryExtension
{

	public static async Task GenerateScriptTypeName(this BrowserBase browser, ISet<int> alreadyRegistered, CommonNameRegistry id, string funcArgs, string funcBody) => await browser.GenerateScriptTypeName(alreadyRegistered, (int)id, funcArgs, funcBody);

	public static async Task GenerateScriptTypeName(this BrowserBase browser, CommonNameRegistry id, string funcArgs, string funcBody) => await browser.GenerateScriptTypeName((int)id, funcArgs, funcBody);

	public static async Task GenerateScriptTypeName(this BrowserBase browser, ISet<int> alreadyRegistered, int id, string funcArgs, string funcBody)
	{
		if (!alreadyRegistered.Contains(id))
		{
			await browser.GenerateScriptTypeName(id, funcArgs, funcBody);
			alreadyRegistered.Add(id);
		}
		else
		{
			Log.Information("Function {func} is already registered.", (CommonNameRegistry)id);
		}
	}

	public static async Task GenerateScriptTypeName(this BrowserBase browser, int id, string funcArgs, string funcBody)
	{
		var nsName = browser.GenerateScriptTypeName((int)CommonNameRegistry.Namespace, true);
		var realFuncName = browser.GenerateScriptTypeName(id);

		try
		{
			// Define namespace. Workaround for WebDriver IIFEs'. https://stackoverflow.com/a/14245853
			if (await browser.EvaluateJavaScriptBoolAsync($"typeof(window.{nsName})!='function'")) // check if already registered
			{
				await browser.EvaluateJavaScriptAsync($"window.{nsName}=function(){{}}");
			}

			if (await browser.EvaluateJavaScriptBoolAsync($"typeof({realFuncName})!='function'")) // check if already registered
			{
				(var err, var errMessage) = await browser.EvaluateScriptAndGetErrorAsync($"{realFuncName}=function({funcArgs}){{{funcBody}}}");
				if (err)
					Log.Error("Failed to register JavaScript function {funcName} : {error}", (CommonNameRegistry)id, errMessage);
				else
					Log.Information("Registered JavaScript function {funcName} : {realFuncName}()", (CommonNameRegistry)id, realFuncName);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to register JavaScript function {funcName}", (CommonNameRegistry)id);
		}
	}

	public static string GetScriptTypeName(this BrowserBase browser, CommonNameRegistry funcId, bool appendParentheses = true) => browser.GetScriptTypeName((int)funcId, appendParentheses);
}
