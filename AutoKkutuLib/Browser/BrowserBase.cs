using AutoKkutuLib.Extension;
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
	public abstract string JavaScriptBaseNamespace { get; }
	public EventHandler<PageLoadedEventArgs>? PageLoaded;
	public EventHandler<PageErrorEventArgs>? PageError;
	public EventHandler<WebSocketMessageEventArgs>? WebSocketMessage;

	protected BrowserBase() => GenerateRandomString((int)CommonNameRegistry.InjectionNamespace); // Generate namespace string on initialize

	public abstract void LoadFrontPage();
	public abstract void Load(string url);
	public abstract void ShowDevTools();
	public abstract void ExecuteJavaScript(string script, string? errorMessage = null);
	public abstract Task<object?> EvaluateJavaScriptRawAsync(string script);
	public abstract IntPtr GetWindowHandle();
	public virtual void SetFocus() { }

	/// <summary>
	/// 문자열을 하나 랜덤하게 생성하고 주어진 <paramref name="id"/>에 대해 등록합니다.
	/// 만약 해당 <paramref name="id"/>에 대해 이미 생성된 문자열이 존재할 경우, 문자열을 새로 등록하는 대신 이미 등록된 문자열을 반환합니다.
	/// </summary>
	/// <param name="id">등록할 ID</param>
	public string GenerateRandomString(int id)
	{
		if (!RegisteredFunctions.TryGetValue(id, out var randomString))
		{
			randomString = $"{Random.Shared.NextTypeName(Random.Shared.Next(10, 32))}";
			RegisteredFunctions[id] = randomString;
		}
		return randomString;
	}

	/// <summary>
	/// <c>GenerateRandomString</c>과 완전히 똑같은 역할을 하나, 단순히 생성한 문자열을 반환하는 대신
	/// 해당 문자열의 앞쪽에 기본 Namespace를 붙혀 반환합니다.
	/// </summary>
	/// <seealso cref="GenerateRandomString(int)"/>
	/// <param name="id">등록할 ID</param>
	public string GenerateScriptTypeName(int funcId)
	{
		var str = GenerateRandomString(funcId);
		return $"{JavaScriptBaseNamespace}.{GetRandomString((int)CommonNameRegistry.InjectionNamespace)}.{str}";
	}

	/// <summary>
	/// 등록된 랜덤 생성 문자열을 반환합니다.
	/// </summary>
	/// <param name="funcId">문자열 등록 ID</param>
	public string GetRandomString(int funcId) => RegisteredFunctions[funcId];

	/// <summary>
	/// 등록된 JavaScript 타입 이름을 기본 Namespace를 붙여서 반환합니다.
	/// </summary>
	/// <param name="funcId">타입 이름 등록 ID</param>
	public string GetScriptTypeName(int funcId) => JavaScriptBaseNamespace + '.' + GetRandomString((int)CommonNameRegistry.InjectionNamespace) + '.' + RegisteredFunctions[funcId];
}
