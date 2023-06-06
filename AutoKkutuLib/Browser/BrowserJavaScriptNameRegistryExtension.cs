using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Browser;
public static class BrowserJavaScriptNameRegistryExtension
{
	public static async Task RegisterScriptFunction(this BrowserBase browser, ISet<int> alreadyRegistered, CommonNameRegistry id, string funcArgs, string funcBody) => await browser.RegisterScriptFunction(alreadyRegistered, (int)id, funcArgs, funcBody);

	public static async Task RegisterScriptFunction(this BrowserBase browser, CommonNameRegistry id, string funcArgs, string funcBody) => await browser.RegisterScriptFunction((int)id, funcArgs, funcBody);

	public static async Task RegisterScriptFunction(this BrowserBase browser, ISet<int> alreadyRegistered, int id, string funcArgs, string funcBody)
	{
		if (!alreadyRegistered.Contains(id))
		{
			await browser.RegisterScriptFunction(id, funcArgs, funcBody);
			alreadyRegistered.Add(id);
		}
		else
		{
			Log.Information("Function {func} is already registered.", (CommonNameRegistry)id);
		}
	}

	public static async Task RegisterScriptFunction(this BrowserBase browser, int id, string funcArgs, string funcBody)
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
				var scr = $"{realFuncName}=function({funcArgs}){{{funcBody}}}";
				(var err, var errMessage) = await browser.EvaluateScriptAndGetErrorAsync(scr);
				if (err)
					Log.Error("Failed to register JavaScript function {funcName} : {error} {scr}", (CommonNameRegistry)id, errMessage, scr);
				else
					Log.Debug("Generated JavaScript type name - {funcName} : {realFuncName}", (CommonNameRegistry)id, realFuncName);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to register JavaScript function {funcName}", (CommonNameRegistry)id);
		}
	}

	public static string GetScriptTypeName(this BrowserBase browser, CommonNameRegistry funcId, bool appendParentheses = true, bool noNamespace = false) => browser.GetScriptTypeName((int)funcId, appendParentheses, noNamespace);
}
