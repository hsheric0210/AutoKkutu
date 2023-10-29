using AutoKkutuLib.Browser;
using AutoKkutuLib.Properties;

namespace AutoKkutuLib.Game.WebSocketHandlers;

/// <summary>
/// '클래식' 모드(끝말잇기, 가운뎃말잇기, 앞말잇기 등)에 대한 쪼리핑의 원 통신 프로토콜에 따른 메세지를 파싱하는 클래스입니다.
/// 만약 특정 사이트가 이와는 다른 프로토콜을 사용한다면, 이 클래스를하여 속성이나 함수를 수정해 주세요.
/// 쪼리핑의 '클래식' 모드 구현체 프로토콜 구현체: https://github.com/JJoriping/KKuTu/blob/a2c240bc31fe2dea31d26fb1cf7625b4645556a6/Server/lib/Web/lib/kkutu/rule_classic.js
/// </summary>
public class RioDecodeWebSocketHandler : BasicWebSocketHandler
{
	public override string HandlerName => "RioDecodeWebSocketHandler";
	public override string HandlerDetails => "Basic WebSocket handler with KkutuIO turnEnd packet decoder";

	public RioDecodeWebSocketHandler(BrowserBase browser) : base(browser)
	{
	}

	public override async ValueTask RegisterWebSocketFilter()
	{
		var wsFilter = Browser.GetScriptTypeName(CommonNameRegistry.WebSocketFilter);
		if (await Browser.EvaluateJavaScriptBoolAsync($"{wsFilter}.registered"))
			return;

		// Only executed if the filter is not registered

		await base.RegisterWebSocketFilter();

		var mapping = BrowserRandomNameMapping.BaseJs(Browser);
		mapping.GenerateScriptType("___wsFilter___", CommonNameRegistry.WebSocketFilter);
		Browser.ExecuteJavaScript(mapping.ApplyTo(LibResources.teDecoderWebSocketFilterJs), "WsFilter register");
	}
}