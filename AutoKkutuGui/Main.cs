//#define SELENIUM
using AutoKkutuGui.Enterer;
using AutoKkutuLib;
using AutoKkutuLib.Browser;
using AutoKkutuLib.Database;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.Enterer;
using AutoKkutuLib.Path;
using Serilog;
using System;
using System.Linq;

namespace AutoKkutuGui;

public partial class Main
{
	private const string ServerConfigFile = "Servers.xml";
	private const string PluginFolder = "plugins";

	private static Main? instance;

	public Preference Preference { get; set; }
	public ServerConfig ServerConfig { get; }
	public BrowserBase Browser { get; }
	public EntererManager EntererManager { get; }
	public DomHandlerManager DomHandlerManager { get; }
	public WebSocketHandlerManager WebSocketHandlerManager { get; }

	public AutoKkutu AutoKkutu { get; private set; }

	public event EventHandler? BrowserInitialized;
	public event EventHandler<AutoKkutuInitializedEventArgs>? AutoKkutuInitialized;
	public event EventHandler<PathListUpdateEventArgs>? PathListUpdated;
	public event EventHandler<PathFindResultUpdateEventArgs>? SearchStateChanged;
	public event EventHandler<StatusMessageChangedEventArgs>? StatusMessageChanged;
	public event EventHandler? NoPathAvailable;
	public event EventHandler<AllPathTimeOverEventArgs>? AllPathTimeOver;
	public event EventHandler? ChatUpdated;

	private Main(
		Preference prefs,
		ServerConfig serverConfig,
		BrowserBase browser,
		EntererManager entererMan,
		DomHandlerManager domHandlerMan,
		WebSocketHandlerManager webSocketHandlerMan)
	{
		Preference = prefs;
		ServerConfig = serverConfig;
		Browser = browser;
		EntererManager = entererMan;
		DomHandlerManager = domHandlerMan;
		WebSocketHandlerManager = webSocketHandlerMan;

		const string blank = "blank";
		AutoKkutu = new AutoKkutu(blank, InitializeDatabase(blank) ?? throw new AggregateException("Failed to load default database"), new Game(new NopDomHandler(browser), null));
	}

	public static Main GetInstance()
	{
		if (instance == null)
			instance = NewInstance();
		return instance;
	}

	/// <summary>
	/// 데이터베이스 설정을 로드하고, 데이터베이스 연결을 초기화한 후 서비스 제공자 인터페이스를 리턴합니다.
	/// </summary>
	private DbConnectionBase? InitializeDatabase(string serverHost)
	{
		try
		{
			var config = ServerConfig.Servers.FirstOrDefault(server => server.ServerHost.Equals(serverHost, StringComparison.OrdinalIgnoreCase), ServerConfig.Default);
			return DatabaseInit.Connect(config.DatabaseType, config.DatabaseConnectionString);
		}
		catch (Exception ex)
		{
			Log.Error(ex, I18n.Main_DBConfigException);
			return null;
		}
	}

	private void UpdateSearchState(/* TODO: Don't pass EventArgs directly as parameter. Destruct and reconstruct it first. */ PathFindResult arguments) => SearchStateChanged?.Invoke(this, new PathFindResultUpdateEventArgs(arguments));

	private void UpdateStatusMessage(StatusMessage status, params object?[] formatterArgs) => StatusMessageChanged?.Invoke(this, new StatusMessageChangedEventArgs(status, formatterArgs));

	public PathFlags SetupPathFinderFlags(PathFlags flags = PathFlags.None)
	{
		if (Preference.EndWordEnabled && (flags.HasFlag(PathFlags.DoNotAutoEnter) || AutoKkutu.PathFilter.PreviousPaths.Count > 0))  // 첫 턴 한방 방지
			flags |= PathFlags.UseEndWord;
		else
			flags &= ~PathFlags.UseEndWord;
		if (Preference.AttackWordEnabled)
			flags |= PathFlags.UseAttackWord;
		else
			flags &= ~PathFlags.UseAttackWord;
		return flags;
	}

	private void StartPathScan(GameMode gameMode, WordCondition condition, PathFlags additionalFlags = PathFlags.None)
	{
		var flags = SetupPathFinderFlags(additionalFlags);
		AutoKkutu.CreatePathFinder()
			.SetGameMode(gameMode)
			.SetPathDetails(new PathDetails(condition, flags, Preference.ReturnModeEnabled, Preference.MaxDisplayedWordCount))
			.SetWordPreference(Preference.ActiveWordPreference)
			.BeginFind(OnPathUpdated);
	}

	public void SendMessage(string message)
	{
		if (!EntererManager.TryGetEnterer(AutoKkutu.Game, Preference.AutoEnterMode, out var enterer))
		{
			Log.Error("SendMessage interrupted because the enterer {name} is not available.", Preference.AutoEnterMode);
			return;
		}

		var opt = new EnterOptions(
			Preference.AutoEnterDelayEnabled,
			0 /* Prefs.StartDelay */,
			0 /*Prefs.StartDelayRandom*/,
			Preference.AutoEnterDelayPerChar,
			Preference.AutoEnterDelayPerCharRandom,
			1,
			0,
			GetEnterCustomParameter());
		var inf = new EnterInfo(opt, PathDetails.Empty.WithFlags(PathFlags.DoNotCheckExpired), message);
		enterer.RequestSend(inf);
	}

	private object? GetEnterCustomParameter()
	{
		switch (Preference.AutoEnterMode)
		{
			case DelayedInstantEnterer.Name:
				return new DelayedInstantEnterer.Parameter(Preference.AutoEnterDelayStartAfterWordEnterEnabled);
			case JavaScriptInputSimulator.Name:
				return new JavaScriptInputSimulator.Parameter(Preference.AutoEnterInputSimulateJavaScriptSendKeys);
		}
		return null;
	}

	public void ToggleFeature(Func<Preference, bool> toggleFunc, StatusMessage displayStatus)
	{
		if (toggleFunc is null)
			throw new ArgumentNullException(nameof(toggleFunc));
		UpdateStatusMessage(displayStatus, toggleFunc(Preference) ? I18n.Enabled : I18n.Disabled);
	}
}
