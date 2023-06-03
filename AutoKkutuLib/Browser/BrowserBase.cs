using System.Collections.Concurrent;

namespace AutoKkutuLib.Browser;

public abstract class BrowserBase
{
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
			randomString = $"v{Math.Abs(id)}{Random.Shared.NextInt64()}";
			RegisteredFunctions[id] = randomString;
		}
		return (noNamespace ? "" : $"{GenerateScriptTypeName((int)CommonNameRegistry.Namespace, true)}.") + randomString;
	}

	public string GetScriptTypeName(int funcId, bool appendParentheses = true) => RegisteredFunctions[(int)CommonNameRegistry.Namespace] + '.' + RegisteredFunctions[funcId] + (appendParentheses ? "()" : "");
}
