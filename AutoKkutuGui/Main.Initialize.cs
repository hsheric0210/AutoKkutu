using AutoKkutuLib;
using AutoKkutuLib.Browser;
using AutoKkutuLib.Database;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketHandlers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;

namespace AutoKkutuGui;
public partial class Main
{
	private static Main? NewInstance()
	{
		try
		{
			Settings config = Settings.Default;
			config.Reload();
			var prefs = new Preference(config);

			var colorPrefs = new ColorPreference
			{
				EndWordColor = config.EndWordColor.ToMediaColor(),
				AttackWordColor = config.AttackWordColor.ToMediaColor(),
				MissionWordColor = config.MissionWordColor.ToMediaColor(),
				EndMissionWordColor = config.EndMissionWordColor.ToMediaColor(),
				AttackMissionWordColor = config.AttackMissionWordColor.ToMediaColor()
			};

			var serverConfig = new ServerConfig(ServerConfigFile);

			// Initialize browser
			var browser = InitializeBrowser();
			var (domHandlers, webSocketHandlers) = LoadHandlers(browser);

			return new Main(prefs, colorPrefs, serverConfig, browser);
		}
		catch (Exception e)
		{
			Log.Error(e, "Initialization failure");
		}
		return null;
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
		browser.PageLoaded += OnPageLoaded;
		browser.PageError += Browser_PageError;

		return browser;
	}

	private static (IImmutableDictionary<string, IDomHandler>, IImmutableDictionary<string, IWebSocketHandler>) LoadHandlers(BrowserBase browser)
	{
		var domHandlerMapping = ImmutableDictionary.CreateBuilder<string, IDomHandler>();
		var webSocketHandlerMapping = ImmutableDictionary.CreateBuilder<string, IWebSocketHandler>();

		void AddDomHandler(IDomHandler handler) => domHandlerMapping.Add(handler.HandlerName, handler);
		void AddWebSocketHandler(IWebSocketHandler handler) => webSocketHandlerMapping.Add(handler.HandlerName, handler);

		AddDomHandler(new BasicDomHandler(browser));
		AddDomHandler(new BasicBypassDomHandler(browser));

		AddWebSocketHandler(new BasicWebSocketHandler(browser));
		AddWebSocketHandler(new RioDecodeWebSocketHandler(browser));

		return (domHandlerMapping.ToImmutable(), webSocketHandlerMapping.ToImmutable());
	}
}
