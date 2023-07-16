namespace AutoKkutuLib.Browser;

/// <summary>
/// 브라우저 스크립트 내 타입 이름 및 문자열들의 랜덤화를 담당합니다.
/// </summary>
public sealed class BrowserRandomNameMapping : NameMapping
{
	private readonly BrowserBase browser;

	public BrowserRandomNameMapping(BrowserBase browser)
	{
		this.browser = browser;
		Generate("___injectionNamespace___", CommonNameRegistry.InjectionNamespace); // The object all injected things should be located in (e.g. 'window.s9TinU2gq05iep02R6q')
	}

	/// <summary>
	/// 주어진 자리 표시자에 대하여 치환될 새로운 무작위 문자열을 배정하고, 이를 <paramref name="id"/>와 연결시킵니다.
	/// 하나의 <paramref name="placeHolder"/>당 한 번만 호출되어야 합니다. 그러지 않으면 예외가 발생합니다.
	/// </summary>
	/// <remarks>
	/// 예시로, <c>___abcdef___</c>가 <c>o0SjGSlxy2tbTFLLiLIYWiJcTmK</c>로 치환될 수 있습니다.
	/// </remarks>
	/// <param name="placeHolder">무작위 문자열로 치환될 자리 표시자</param>
	/// <param name="id">생성된 무작위 문자열을 연결시킬 ID</param>
	public void Generate(string placeHolder, CommonNameRegistry id) => Generate(placeHolder, (int)id);

	/// <summary>
	/// 주어진 자리 표시자에 대하여 치환될 새로운 무작위 문자열을 배정하고, 이를 <paramref name="id"/>와 연결시킵니다.
	/// 하나의 <paramref name="placeHolder"/>당 한 번만 호출되어야 합니다. 그러지 않으면 예외가 발생합니다.
	/// </summary>
	/// <remarks>
	/// 예시로, <c>___abcdef___</c>가 <c>o0SjGSlxy2tbTFLLiLIYWiJcTmK</c>로 치환될 수 있습니다.
	/// </remarks>
	/// <param name="placeHolder">무작위 문자열로 치환될 자리 표시자</param>
	/// <param name="id">생성된 무작위 문자열을 연결시킬 ID</param>
	public void Generate(string placeHolder, int id) => Add(placeHolder, browser.GenerateRandomString(id));

	/// <summary>
	/// 주어진 자리 표시자에 대하여 치환될 새로운 무작위 타입 이름을 배정하고, 이를 <paramref name="id"/>와 연결시킵니다.
	/// 하나의 <paramref name="placeHolder"/>당 한 번만 호출되어야 합니다. 그러지 않으면 예외가 발생합니다.
	/// </summary>
	/// <remarks>
	/// 예시로, <c>___abcdef___</c>가 <c>window.xDVTT3JYhbt2cQdymmk.twiMdhIhwyU74VDySOhw</c>로 치환될 수 있습니다.
	/// </remarks>
	/// <param name="placeHolder">무작위 타입 이름으로 치환될 자리 표시자</param>
	/// <param name="id">생성된 무작위 문자열을 연결시킬 ID</param>
	public void GenerateScriptType(string placeHolder, CommonNameRegistry id) => GenerateScriptType(placeHolder, (int)id);

	/// <summary>
	/// 주어진 자리 표시자에 대하여 치환될 새로운 무작위 타입 이름을 배정하고, 이를 <paramref name="id"/>와 연결시킵니다.
	/// 하나의 <paramref name="placeHolder"/>당 한 번만 호출되어야 합니다. 그러지 않으면 예외가 발생합니다.
	/// </summary>
	/// <remarks>
	/// 예시로, <c>___abcdef___</c>가 <c>window.xDVTT3JYhbt2cQdymmk.twiMdhIhwyU74VDySOhw</c>로 치환될 수 있습니다.
	/// </remarks>
	/// <param name="placeHolder">무작위 타입 이름으로 치환될 자리 표시자</param>
	/// <param name="id">생성된 무작위 문자열을 연결시킬 ID</param>
	public void GenerateScriptType(string placeHolder, int id) => Add(placeHolder, browser.GenerateScriptTypeName(id));

	/// <summary>
	/// mainHelperJs에 대하여 미리 정의된 자리 표시자들을 포함한 <c>BrowserRandomNameMapping</c> 개체를 반환합니다.
	/// </summary>
	/// <param name="browser">대상 브라우저</param>
	public static BrowserRandomNameMapping MainHelperJs(BrowserBase browser)
	{
		var instance = BaseJs(browser);
		instance.GenerateScriptType("___originalWS___", CommonNameRegistry.OriginalWebSocket);
		instance.GenerateScriptType("___wsFilter___", CommonNameRegistry.WebSocketFilter);
		instance.Generate("___nativeSend___", CommonNameRegistry.WebSocketNativeSend);
		instance.Generate("___nativeAddEventListener___", CommonNameRegistry.WebSocketNativeAddEventListener);
		instance.Generate("___passthru___", CommonNameRegistry.WebSocketPassThru);
		instance.GenerateScriptType("___originalWSPrototype___", CommonNameRegistry.OriginalWebSocketPrototype);
		instance.Generate("___injectedOnMessageListeners___", CommonNameRegistry.InjectedWebSocketMessageHandlerList);
		instance.GenerateScriptType("___commSend___", CommonNameRegistry.CommunicateSend);
		instance.GenerateScriptType("___commRecv___", CommonNameRegistry.CommunicateReceive);
		return instance;
	}

	public static BrowserRandomNameMapping WebSocketFilterJs(BrowserBase browser)
	{
		var instance = BaseJs(browser);
		instance.GenerateScriptType("___wsFilter___", CommonNameRegistry.WebSocketFilter);
		return instance;
	}

	/// <summary>
	/// 모든 주입될 브라우저 스크립트에 공통으로 적용되는 기본 타입 이름 자리 표시자들을 모두 포함한 <c>BrowserRandomNameMapping</c>를 반환합니다.
	/// </summary>
	/// <param name="browser">대상 브라우저</param>
	public static BrowserRandomNameMapping BaseJs(BrowserBase browser)
	{
		var instance = new BrowserRandomNameMapping(browser);

		// Global object backups
		instance.GenerateScriptType("___getComputedStyle___", CommonNameRegistry.GetComputedStyle);
		instance.GenerateScriptType("___consoleLog___", CommonNameRegistry.ConsoleLog);
		instance.GenerateScriptType("___setTimeout___", CommonNameRegistry.SetTimeout);
		instance.GenerateScriptType("___setInterval___", CommonNameRegistry.SetInterval);

		instance.GenerateScriptType("___dispatchEvent___", CommonNameRegistry.DispatchEvent);
		instance.GenerateScriptType("___getElementsByClassName___", CommonNameRegistry.GetElementsByClassName);
		instance.GenerateScriptType("___querySelector___", CommonNameRegistry.QuerySelector);
		instance.GenerateScriptType("___querySelectorAll___", CommonNameRegistry.QuerySelectorAll);
		instance.GenerateScriptType("___getElementById___", CommonNameRegistry.GetElementById);

		return instance;
	}

	/// <summary>
	/// 주어진 <paramref name="target"/> 내에 존재하는 모든 자리 표시자들에 대하여 등록된 무작위 문자열 및 타입 이름을 적용합니다.
	/// </summary>
	/// <param name="target">대상 문자열</param>
	public override string ApplyTo(string target) => base.ApplyTo(target.Replace("___baseNamespace___", browser.JavaScriptBaseNamespace));
}
