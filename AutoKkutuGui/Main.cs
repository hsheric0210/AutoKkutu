//#define SELENIUM
using AutoKkutuGui.Properties;
using AutoKkutuLib;
using AutoKkutuLib.Browser;
using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Jobs;
using AutoKkutuLib.Database.Path;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.Enterer;
using AutoKkutuLib.Game.WebSocketHandlers;
using AutoKkutuLib.MySql.Properties;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Windows;
using System.Xml.Serialization;

namespace AutoKkutuGui;

public partial class Main
{
	private const string ServerConfigFile = "Servers.xml";

	private static Main? instance;

	public Preference Prefs { get; set; } = null!;
	public ColorPreference ColorPreference { get; set; } = null!;
	public ServerConfig ServerConfig { get; }

	public BrowserBase Browser { get; private set; } = null!;
	public AutoKkutu? AutoKkutu { get; private set; }

	public event EventHandler? BrowserInitialized;
	public event EventHandler? AutoKkutuInitialized;
	public event EventHandler<PathListUpdateEventArgs>? PathListUpdated;
	public event EventHandler<SearchStateChangedEventArgs>? SearchStateChanged;
	public event EventHandler<StatusMessageChangedEventArgs>? StatusMessageChanged;
	public event EventHandler? ChatUpdated;

	private Main(Preference prefs, ColorPreference colorPrefs, ServerConfig serverConfig, BrowserBase browser)
	{
		Prefs = prefs;
		ColorPreference = colorPrefs;
		ServerConfig = serverConfig;
		Browser = browser;

		browser.LoadFrontPage();
		BrowserInitialized?.Invoke(this, EventArgs.Empty);
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
	private AbstractDatabaseConnection? InitializeDatabase(Uri uri)
	{
		try
		{
			ServerInfo config = ServerConfig.Servers.FirstOrDefault(server => server.ServerUri == uri, ServerConfig.Default);
			return DatabaseInit.Connect(config.DatabaseType, config.DatabaseConnectionString);
		}
		catch (Exception ex)
		{
			Log.Error(ex, I18n.Main_DBConfigException);
			return null;
		}
	}

	public void FrameReloaded() => Browser.PageLoaded += OnPageLoaded;

	private void OnPageLoaded(object? sender, PageLoadedEventArgs args)
	{
		var uri = new Uri(new Uri(args.Url).Host); // What a worst solution

		var prevInstance = AutoKkutu;
		if (prevInstance != null)
		{
			// 동일한 서버에 대하여 이미 AutoKkutu 파사드가 존재할 경우, 재 초기화 방지
			if (prevInstance.IsForServer(uri) == true)
				return;

			// 이전 AutoKkutu 파사드 제거
			prevInstance.Dispose();

			Log.Information("Disposed previous facade.");
		}

		// 서버에 대해 적절한 DOM Handler 탐색
		if (!domHandlerMapping.TryGetValue(uri, out var domHandlerName))
		{
			Log.Warning(I18n.Main_UnsupportedURL, uri);
			return;
		}
		if (!domHandlerMapping.TryGetValue(domHandlerName, out var domHandler))
		{
			Log.Error("DOM Handler doesn't exists: {name}", domHandlerName);
			return;
		}

		// 서버에 대해 적절한 WebSocket Handler 탐색
		if (!webSocketHandlerMapping.TryGetValue(uri, out var webSocketHandlerName))
		{
			Log.Warning("WebSocket sniffing is not supported on: {url}", uri);
		}
		if (!webSocketHandlerMapping.TryGetValue(webSocketHandlerName, out var webSocketHandler))
		{
			Log.Error("WebSocket Handler doesn't exists: {name}", webSocketHandlerName);
		}

		// 서버에 대해 적절한 Database 연결 수립
		var db = InitializeDatabase(uri);
		if (db is null)
		{
			// TODO: DataBaseError 이벤트 트리거
			Log.Error("Failed to initialize database!");
			MessageBox.Show("Failed to initialize database!", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
			return;
		}

		var game = new Game(domHandler, webSocketHandler);

		AutoKkutu = new AutoKkutu(uri, db, game);
		AutoKkutu.PathFinder.PathUpdated += OnPathUpdated;
		AutoKkutu.GameEnded += OnGameEnded;
		AutoKkutu.GameModeChanged += OnGameModeChange;
		AutoKkutu.GameStarted += OnGameStarted;
		AutoKkutu.MyTurnEnded += OnMyTurnEnded;
		AutoKkutu.MyTurnStarted += OnMyTurnStarted;
		AutoKkutu.UnsupportedWordEntered += OnUnsupportedWordEntered;
		AutoKkutu.TypingWordPresented += OnTypingWordPresented;
		AutoKkutu.PreviousUserTurnEnded += OnPreviousUserTurnEnded;
		AutoKkutu.RoundChanged += OnRoundChanged;
		AutoKkutuInitialized?.Invoke(null, EventArgs.Empty);

		// 정상적으로 파사드가 초기화되었다면, 페이지 로드 이벤트 등록 해제
		Browser.PageLoaded -= OnPageLoaded;
	}

	private void Browser_PageError(object? sender, PageErrorEventArgs args)
	{
		Log.Error("Browser load error!");
		Log.Error("Error: {error}", args.ErrorText);
	}

	public void UpdateSearchState(/* TODO: Don't pass EventArgs directly as parameter. Destruct and reconstruct it first. */ PathUpdateEventArgs? arguments, bool isEndWord = false) => SearchStateChanged?.Invoke(null, new SearchStateChangedEventArgs(arguments, isEndWord));

	public void UpdateStatusMessage(StatusMessage status, params object?[] formatterArgs) => StatusMessageChanged?.Invoke(null, new StatusMessageChangedEventArgs(status, formatterArgs));

	public PathFlags SetupPathFinderFlags(PathFlags flags = PathFlags.None)
	{
		if (Prefs.EndWordEnabled && (flags.HasFlag(PathFlags.ManualSearch) || AutoKkutu.PathFilter.PreviousPaths.Count > 0))  // 첫 턴 한방 방지
			flags |= PathFlags.UseEndWord;
		else
			flags &= ~PathFlags.UseEndWord;
		if (Prefs.AttackWordEnabled)
			flags |= PathFlags.UseAttackWord;
		else
			flags &= ~PathFlags.UseAttackWord;
		return flags;
	}

	// FIXME: DOM Handler, WebSocket Handler 처럼 List 클래스 만들고 거기에 쑤셔넣기
	// 지금 이 상태의 구현대로라면 사용할 때마다 객체가 생성됨
	public static EntererBase CreateEnterer(EntererMode mode, IGame game)
	{
		return mode switch
		{
			EntererMode.EnterImmediately => new DelayedInstantEnterer(game),
			EntererMode.SimulateInputJavaScript => new JavaScriptInputSimulator(game),
			EntererMode.SimulateInputWin32 => new Win32InputSimulator(game),
			EntererMode.SimulateInputArduino => throw new NotImplementedException("Arduino input simulator is not implemented yet"),
			_ => throw new ArgumentException("Unsupported enterer: " + mode, nameof(mode))
		};
	}

	// TODO: Move to Lib
	public void SendMessage(string message)
	{
		if (AutoKkutu == null)
			return;

		var enterer = CreateEnterer(Prefs.AutoEnterMode, AutoKkutu.Game);
		var opt = new EnterOptions(
			Prefs.AutoEnterDelayEnabled,
			Prefs.AutoEnterDelayStartAfterWordEnterEnabled,
			Prefs.AutoEnterInputSimulateJavaScriptSendKeys,
			0 /* Prefs.StartDelay */,
			0 /*Prefs.StartDelayRandom*/,
			Prefs.AutoEnterDelayPerChar,
			Prefs.AutoEnterDelayPerCharRandom);
		var inf = new EnterInfo(opt, PathDetails.Empty.WithFlags(PathFlags.ManualMessage), message);
		enterer.RequestSend(inf);
	}

	public static void ToggleFeature(Func<Preference, bool> toggleFunc, StatusMessage displayStatus)
	{
		if (toggleFunc is null)
			throw new ArgumentNullException(nameof(toggleFunc));
		UpdateStatusMessage(displayStatus, toggleFunc(Prefs) ? I18n.Enabled : I18n.Disabled);
	}

	/* EVENTS: PathFinder */

	private static PathUpdateEventArgs? preSearch;
	private static PathUpdateEventArgs? autoPathFindCache;

	// TODO: 내가 이번 턴에 패배했고, 라운드가 끝났을 경우
	// 다음 라운드 시작 단어에 대해 미리 Pre-search 및 입력 수행.
	private static void OnRoundChanged(object? sender, EventArgs e) => preSearch = null; // Invalidate pre-search result on round changed

	private static void OnPathUpdated(object? sender, PathUpdateEventArgs args)
	{
		Log.Verbose(I18n.Main_PathUpdateReceived);
		if (!args.HasFlag(PathFlags.ManualSearch))
			autoPathFindCache = args;
		if (args.HasFlag(PathFlags.PreSearch))
			preSearch = args;

		var autoEnter = Prefs.AutoEnterEnabled && !args.HasFlag(PathFlags.ManualSearch) /*&& !args.HasFlag(PathFinderFlags.PreSearch)*/;

		if (args.Result == PathFindResultType.NotFound && !args.HasFlag(PathFlags.ManualSearch))
			UpdateStatusMessage(StatusMessage.NotFound); // Not found
		else if (args.Result == PathFindResultType.Error)
			UpdateStatusMessage(StatusMessage.Error); // Error occurred
		else if (!autoEnter)
			UpdateStatusMessage(StatusMessage.Normal);

		if (AutoKkutu.Game.RescanIfPathExpired(args.Details.WithoutFlags(PathFlags.PreSearch)))
		{
			Log.Warning("Expired word condition {path} rejected. Rescanning...", args.Details.Condition);
			return;
		}

		UpdateSearchState(args);
		PathListUpdated?.Invoke(null, new PathListUpdateEventArgs(args.FoundWordList.Select(po => new GuiPathObject(po)).ToImmutableList()));

		if (autoEnter)
		{
			Log.Verbose("Auto-entering on path update...");
			TryAutoEnter(args);
		}
	}

	private static void TryAutoEnter(PathUpdateEventArgs args, bool usedPresearchResult = false)
	{
		if (args.Result == PathFindResultType.NotFound)
		{
			Log.Warning(I18n.Auto_NoMorePathAvailable);
			UpdateStatusMessage(StatusMessage.NotFound);
		}
		else
		{
			var opt = new EnterOptions(Prefs.AutoEnterDelayEnabled, Prefs.AutoEnterDelayStartAfterWordEnterEnabled, Prefs.AutoEnterInputSimulateJavaScriptSendKeys, Prefs.AutoEnterStartDelay, Prefs.AutoEnterStartDelayRandom, Prefs.AutoEnterDelayPerChar, Prefs.AutoEnterDelayPerCharRandom);
			var time = AutoKkutu.Game.GetTurnTimeMillis();
			(var wordToEnter, var timeover) = args.FilteredWordList.ChooseBestWord(opt, time);
			if (string.IsNullOrEmpty(wordToEnter))
			{
				if (timeover)
				{
					Log.Warning(I18n.Auto_TimeOver);
					UpdateStatusMessage(StatusMessage.AllWordTimeOver, time);
				}
				else
				{
					Log.Warning(I18n.Auto_NoMorePathAvailable);
					UpdateStatusMessage(StatusMessage.NotFound);
				}
			}
			else
			{
				var param = args.Details;
				if (usedPresearchResult)
					param = param.WithoutFlags(PathFlags.PreSearch); // Fixme: 이런 번거로운 방법 대신 더 나은 방법 생각해보기
				CreateEnterer(Prefs.AutoEnterMode, AutoKkutu.Game).RequestSend(new EnterInfo(opt, param, wordToEnter));
			}
		}
	}

	/* EVENTS: Handler */

	private static void OnGameEnded(object? sender, EventArgs e)
	{
		UpdateSearchState(null, false);
		AutoKkutu.PathFilter.UnsupportedPaths.Clear();
		if (Prefs.AutoDBUpdateEnabled)
		{
			UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheck, I18n.Status_AutoUpdate);
			var updateTask = new DbUpdateTask(AutoKkutu.NodeManager, AutoKkutu.PathFilter);
			var opts = DbUpdateTask.DbUpdateCategories.None;
			if (Prefs.AutoDBWordAddEnabled)
				opts |= DbUpdateTask.DbUpdateCategories.Add;
			if (Prefs.AutoDBWordRemoveEnabled)
				opts |= DbUpdateTask.DbUpdateCategories.Remove;
			if (Prefs.AutoDBAddEndEnabled)
				opts |= DbUpdateTask.DbUpdateCategories.AddEnd;
			var result = updateTask.Execute(opts);
			UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheckDone, I18n.Status_AutoUpdate, result);
		}
		else
		{
			UpdateStatusMessage(StatusMessage.Wait);
		}
	}

	private static int wordIndex;

	private static void OnGameModeChange(object? sender, GameModeChangeEventArgs args) => Log.Information(I18n.Main_GameModeUpdated, ConfigEnums.GetGameModeName(args.GameMode));

	private static void OnGameStarted(object? sender, EventArgs e)
	{
		UpdateStatusMessage(StatusMessage.Normal);
		// AutoEnter.ResetWordIndex();
		wordIndex = 0;
	}

	private static void OnMyPathIsUnsupported(object? sender, UnsupportedWordEventArgs args)
	{
		if (autoPathFindCache is null)
		{
			Log.Warning("이전에 수행한 단어 검색 결과를 찾을 수 없습니다!");
			return;
		}

		var word = args.Word;
		Log.Warning(I18n.Main_MyPathIsUnsupported, word);

		if (Prefs.AutoEnterEnabled && Prefs.AutoFixEnabled)
		{
			var parameter = new EnterInfo(
							new EnterOptions(Prefs.AutoEnterDelayEnabled, Prefs.AutoEnterDelayStartAfterWordEnterEnabled, Prefs.AutoEnterInputSimulateJavaScriptSendKeys, Prefs.AutoEnterStartDelay, Prefs.AutoEnterStartDelayRandom, Prefs.AutoEnterDelayPerChar, Prefs.AutoEnterDelayPerCharRandom),
							autoPathFindCache.Details.WithoutFlags(PathFlags.PreSearch));

			(var content, var timeover) = autoPathFindCache.FilteredWordList.ChooseBestWord(parameter.Options, AutoKkutu.Game.GetTurnTimeMillis(), ++wordIndex);
			if (string.IsNullOrEmpty(content))
			{
				Log.Warning(I18n.Main_NoMorePathAvailable);
				//TODO: NoPathAvailable?.Invoke(this, new NoPathAvailableEventArgs(timeover, AutoKkutu.Game.GetTurnTimeMillis()));
				return;
			}

			AutoKkutu.Game.AutoEnter.PerformAutoEnter(parameter with { Content = content });
		}
	}

	private static void OnMyTurnEnded(object? sender, EventArgs e)
	{
		Log.Debug(I18n.Main_WordIndexReset);
		wordIndex = 0;
	}

	// TODO: Move to Lib
	private static void OnMyTurnStarted(object? sender, WordConditionPresentEventArgs args)
	{
		if (Prefs.AutoEnterEnabled)
		{
			if (preSearch?.Details.Condition.IsSimilar(args.Condition) == true)
			{
				Log.Debug("Using the pre-search result for: {condition}", preSearch.Details.Condition);
				TryAutoEnter(preSearch, usedPresearchResult: true);
				return;
			}

			if (preSearch == null)
				Log.Debug("Pre-search data not available. Starting the search.");
			else
				Log.Warning("Pre-search path is expired! Presearch: {pre}, Search: {now}", preSearch.Details.Condition, args.Condition);
		}

		AutoKkutu.PathFinder.FindPath(
			AutoKkutu.Game.CurrentGameMode,
			new PathDetails(args.Condition, SetupPathFinderFlags(), Prefs.ReturnModeEnabled, Prefs.MaxDisplayedWordCount),
			Prefs.ActiveWordPreference);
	}

	private static void OnPreviousUserTurnEnded(object? sender, PreviousUserTurnEndedEventArgs args)
	{
		if (args.Presearch != PreviousUserTurnEndedEventArgs.PresearchAvailability.Available || args.Condition is null)
		{
			Log.Verbose("Pre-search result flushed. Reason: {availability}", args.Presearch);
			preSearch = null;
			return;
		}

		Log.Verbose("Performing pre-search on: {condition}", args.Condition);
		AutoKkutu.PathFinder.FindPath(
			AutoKkutu.Game.CurrentGameMode,
			new PathDetails((WordCondition)args.Condition, SetupPathFinderFlags() | PathFlags.PreSearch, Prefs.ReturnModeEnabled, Prefs.MaxDisplayedWordCount),
			Prefs.ActiveWordPreference);
	}

	// TODO: Move to Lib
	private static void OnUnsupportedWordEntered(object? sender, UnsupportedWordEventArgs args)
	{
		var isInexistent = !args.IsExistingWord;
		var word = args.Word;
		ICollection<string> list;
		if (isInexistent)
		{
			list = AutoKkutu.PathFilter.InexistentPaths;
			Log.Warning(I18n.Main_UnsupportedWord_Inexistent, word);
		}
		else
		{
			list = AutoKkutu.PathFilter.UnsupportedPaths;
			if (args.IsEndWord)
			{
				GameMode gm = AutoKkutu.Game.CurrentGameMode;
				var node = gm.ConvertWordToTailNode(word);
				if (!string.IsNullOrWhiteSpace(node))
				{
					Log.Information("New end node: {node}", node);
					AutoKkutu.PathFilter.NewEndPaths.Add((gm, node));
				}
			}
			Log.Warning(I18n.Main_UnsupportedWord_Existent, word);
		}
		list.Add(word);
	}

	private void OnTypingWordPresented(object? sender, WordPresentEventArgs args)
	{
		var word = args.Word;

		if (!Prefs.AutoEnterEnabled)
			return;

		AutoEnter.PerformAutoEnter(new EnterInfo(
			new EnterOptions(Prefs.AutoEnterDelayEnabled, Prefs.AutoEnterDelayStartAfterWordEnterEnabled, Prefs.AutoEnterInputSimulateJavaScriptSendKeys, Prefs.AutoEnterStartDelay, Prefs.AutoEnterStartDelayRandom, Prefs.AutoEnterDelayPerChar, Prefs.AutoEnterDelayPerCharRandom),
			PathDetails.Empty,
			word));
	}

	private static void OnChatUpdated(object? sender, EventArgs args) => ChatUpdated?.Invoke(null, args);
}

public class SearchStateChangedEventArgs : EventArgs
{
	public PathUpdateEventArgs? Arguments
	{
		get;
	}

	public bool IsEndWord
	{
		get;
	}

	public SearchStateChangedEventArgs(PathUpdateEventArgs? arguments, bool isEndWord = false)
	{
		Arguments = arguments;
		IsEndWord = isEndWord;
	}
}

public class StatusMessageChangedEventArgs : EventArgs
{
	private readonly object?[] formatterArguments;

	public StatusMessage Status
	{
		get;
	}

	public object?[] GetFormatterArguments() => formatterArguments;

	public StatusMessageChangedEventArgs(StatusMessage status, params object?[] formatterArgs)
	{
		Status = status;
		formatterArguments = formatterArgs;
	}
}
