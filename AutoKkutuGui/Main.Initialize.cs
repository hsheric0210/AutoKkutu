using AutoKkutuGui.Enterer;
using AutoKkutuLib.Browser;
using Serilog;
using System;

namespace AutoKkutuGui;
public partial class Main
{
	/// <summary>
	/// 새로운 <c>AutoKkutu</c> 인스턴스를 생성합니다.
	/// 만약 생성에 실패할 경우, <c>AggregateException</c>을 발생시킵니다.
	/// 이 때 생성된 InvalidOperationException는 복구 불가능한 오류를 나태니기에 catch해서는 안 됩니다.
	/// </summary>
	/// <exception cref="AggregateException"><c>AutoKkutu</c> 개체 생성 중 오류 발생했을 때 발생합니다.</exception>
	private static Main NewInstance()
	{
		try
		{
			var config = Settings.Default;
			config.Reload();
			var prefs = new Preference(config);

			var serverConfig = new ServerConfig(ServerConfigFile);

			var browser = InitializeBrowser();

			var plugin = new PluginLoader(PluginFolder, browser);
			var entererMan = new EntererManager(plugin.EntererProviders.Add(new DefaultEntererProvider()));
			var domHandlerMan = new DomHandlerManager(plugin.DomHandlerProviders.Add(new DefaultDomHandlerProvider(browser)));
			var webSocketMan = new WebSocketHandlerManager(plugin.WebSocketHandlerProviders.Add(new DefaultWebSocketHandlerProvider(browser)));

			var inst = new Main(prefs, serverConfig, browser, entererMan, domHandlerMan, webSocketMan);
			browser.PageLoaded += inst.Browser_PageLoaded;
			browser.PageError += inst.Browser_PageError;
			return inst;
		}
		catch (Exception e)
		{
			Log.Error(e, "Initialization failure");
			throw new AggregateException("AutoKkutu instance initialization failure", e);
		}
	}

	/// <summary>
	/// 브라우저와 DOM, 핸들러 목록을 초기화하고 <c>Browser</c>, <c>DomHandlerList</c>, <c>WsSniffingHandlerList</c>에 각각 저장합니다.
	/// </summary>
	private static BrowserBase InitializeBrowser()
	{
		Log.Verbose("Initializing browser");

		// Initialize Browser
#if RELEASESELENIUM || DEBUGSELENIUM
		var browser = new AutoKkutuLib.Selenium.SeleniumBrowser();
#else
		var browser = new AutoKkutuLib.CefSharp.CefSharpBrowser();
#endif
		return browser;
	}
}
