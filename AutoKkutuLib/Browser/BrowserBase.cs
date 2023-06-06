using AutoKkutuLib.Extension;
using System.Collections.Concurrent;

namespace AutoKkutuLib.Browser;

public abstract class BrowserBase
{
	private static string functionPrefix = Random.Shared.GenerateRandomString(Random.Shared.Next(5, 16), true);

	private readonly ConcurrentDictionary<int, string> RegisteredFunctions = new();

	/// <summary>
	/// type is 'object' to prevent WPF to dependency. Please cast to <see cref="Control"/> when using.
	/// May be null if the WPF frame is not available
	/// </summary>
	public abstract object? BrowserControl { get; }
	public EventHandler<PageLoadedEventArgs>? PageLoaded;
	public EventHandler<PageErrorEventArgs>? PageError;
	public EventHandler<WebSocketMessageEventArgs>? WebSocketMessage;

	public abstract void LoadFrontPage();
	public abstract void Load(string url);
	public abstract void ShowDevTools();
	public abstract void ExecuteJavaScript(string script, string? errorMessage = null);
	public abstract Task<JavaScriptCallback> EvaluateJavaScriptRawAsync(string script);

	public string GenerateScriptTypeName(int id, bool noNamespace = false)
	{
		if (!RegisteredFunctions.TryGetValue(id, out var randomString))
		{
			randomString = $"{functionPrefix}{Math.Abs(id)}{Random.Shared.NextInt64()}";
			RegisteredFunctions[id] = randomString;
		}
		return (noNamespace ? "" : $"{GenerateScriptTypeName(CommonNameRegistry.Namespace, true)}.") + randomString;
	}

	public string GenerateScriptTypeName(CommonNameRegistry id, bool noNamespace = false) => GenerateScriptTypeName((int)id, noNamespace);

	public string GetScriptTypeName(int funcId, bool appendParentheses = true, bool noNamespace = false) => (noNamespace ? "" : (RegisteredFunctions[(int)CommonNameRegistry.Namespace] + '.')) + RegisteredFunctions[funcId] + (appendParentheses ? "()" : "");
}
