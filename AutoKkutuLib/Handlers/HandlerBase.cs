using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Handlers;

public abstract class HandlerBase
{
	#region Frequently-used function names
	protected const string WriteInputFunc = "WriteInputFunc";

	protected const string ClickSubmitFunc = "ClickSubmitFunc";

	protected const string CurrentRoundIndexFunc = "CurrentRoundIndexFunc";
	#endregion

	private readonly Dictionary<string, string> RegisteredFunctionNames = new();

	#region Javascript function registration
	protected void RegisterJSFunction(string funcName, string funcArgs, string funcBody)
	{
		if (!RegisteredFunctionNames.ContainsKey(funcName))
			RegisteredFunctionNames[funcName] = $"jQuery{Random.Shared.GenerateRandomString(37, true)}";

		var realFuncName = RegisteredFunctionNames[funcName];
		if (Browser.EvaluateJavaScriptBool($"typeof({realFuncName})!='function'")) // check if already registered
		{
			if (Browser.EvaluateJSAndGetError($"function {realFuncName}({funcArgs}){{{funcBody}}}", out var error))
				Log.Error("Failed to register JavaScript function {funcName} : {error}", funcName, error);
			else
				Log.Information("Registered JavaScript function {funcName} : {realFuncName}()", funcName, realFuncName);
		}
	}

	protected string GetRegisteredJSFunctionName(string funcName) => RegisteredFunctionNames[funcName];
	#endregion

	public abstract string HandlerName { get; }
	public abstract IReadOnlyCollection<Uri> UrlPattern { get; }
	public abstract BrowserBase Browser { get; }

	public abstract GameMode GameMode { get; }
	public abstract bool IsGameInProgress { get; }
	public abstract bool IsMyTurn { get; }
	public abstract string PresentedWord { get; }
	public abstract string MissionChar { get; }
	public abstract string ExampleWord { get; }
	public abstract int RoundIndex { get; }
	public abstract string RoundText { get; }
	public abstract float RoundTime { get; }
	public abstract float TurnTime { get; }
	public abstract string UnsupportedWord { get; }

	public abstract void ClickSubmit();
	public abstract string GetWordInHistory(int index);
	public virtual void RegisterRoundIndexFunction() { }
	public abstract void UpdateChat(string input);
}