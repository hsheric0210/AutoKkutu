using AutoKkutuLib.Extension;
using Serilog;
using System;
using System.Collections.Concurrent;

namespace AutoKkutuLib.Handlers;

public abstract class HandlerBase
{
	#region Frequently-used function names
	public enum CommonFunctionNames
	{
		None = 0,
		GameInProgress,
		UpdateChat,
		ClickSubmit,
		RoundIndex,
		IsMyTurn,
		PresentedWord,
		RoundText,
		UnsupportedWord,
		GameMode,
		TurnTime,
		RoundTime,
		ExampleWord,
		MissionChar,
		WordHistory
	}
	#endregion

	private readonly ConcurrentDictionary<int, string> RegisteredFunctions = new();

	#region Javascript function registration
	protected void RegisterJavaScriptFunction(ISet<int> alreadyRegistered, CommonFunctionNames funcId, string funcArgs, string funcBody) => RegisterJavaScriptFunction(alreadyRegistered, (int)funcId, funcArgs, funcBody);

	protected void RegisterJavaScriptFunction(CommonFunctionNames funcId, string funcArgs, string funcBody) => RegisterJavaScriptFunction((int)funcId, funcArgs, funcBody);

	protected void RegisterJavaScriptFunction(ISet<int> alreadyRegistered, int funcId, string funcArgs, string funcBody)
	{
		if (!alreadyRegistered.Contains(funcId))
		{
			RegisterJavaScriptFunction(funcId, funcArgs, funcBody);
			alreadyRegistered.Add(funcId);
		}
	}

	protected void RegisterJavaScriptFunction(int funcId, string funcArgs, string funcBody)
	{
		var realFuncName = RegisterJavaScriptRandomName(funcId);

		Task.Run(() =>
		{
			if (Browser.EvaluateJavaScriptBool($"typeof({realFuncName})!='function'")) // check if already registered
			{
				if (Browser.EvaluateJSAndGetError($"function {realFuncName}({funcArgs}){{{funcBody}}}", out var error))
					Log.Error("Failed to register JavaScript function {funcName} : {error}", (CommonFunctionNames)funcId, error);
				else
					Log.Information("Registered JavaScript function {funcName} : {realFuncName}()", (CommonFunctionNames)funcId, realFuncName);
			}
		});
	}

	protected string RegisterJavaScriptRandomName(int id)
	{
		if (!RegisteredFunctions.TryGetValue(id, out var randomString))
		{
			randomString = $"jQuery{id}{Random.Shared.NextInt64()}";
			RegisteredFunctions[id] = randomString;
		}
		return randomString;
	}

	protected string GetRegisteredJSFunctionName(CommonFunctionNames funcId, bool appendParentheses = true) => GetRegisteredJSFunctionName((int)funcId, appendParentheses);
	protected string GetRegisteredJSFunctionName(int funcId, bool appendParentheses = true) => RegisteredFunctions[funcId] + (appendParentheses ? "()" : "");
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
	public virtual void RegisterInGameFunctions(ISet<int> alreadyRegistered) { }
	public abstract void UpdateChat(string input);
}