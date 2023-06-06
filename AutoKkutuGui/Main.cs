using AutoKkutuLib;
using AutoKkutuLib.Browser;
using AutoKkutuLib.CefSharp;
using AutoKkutuLib.Database;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.DomHandlers;
using AutoKkutuLib.Game.WebSocketListener;
using AutoKkutuLib.Game.WsHandlers;
using AutoKkutuLib.Handlers.JavaScript;
using AutoKkutuLib.Path;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AutoKkutuGui;

public static class Main
{
	public static Preference Prefs
	{
		get; set;
	} = null!;

	public static ColorPreference ColorPreference
	{
		get; set;
	} = null!;

	public static BrowserBase Browser { get; private set; } = null!;

	public static IDomHandlerList DomHandlerList { get; private set; } = null!;
	public static IWsHandlerList WsSniffingHandlerList { get; private set; } = null!;

	public static AutoKkutu AutoKkutu
	{
		get; private set;
	} = null!;

	/* EVENTS */
	public static event EventHandler? BrowserFrameLoad;

	public static event EventHandler? HandlerRegistered;

	public static event EventHandler<PathListUpdateEventArgs>? PathListUpdated;

	public static event EventHandler? InitializationFinished;

	public static event EventHandler<SearchStateChangedEventArgs>? SearchStateChanged;

	public static event EventHandler<StatusMessageChangedEventArgs>? StatusMessageChanged;

	public static event EventHandler? ChatUpdated;

	/* Misc. variables */

	/* Initialization-related */

	public static void Initialize()
	{
		try
		{
			// Load default config
			InitializeConfiguration();

			// Initialize browser
			SetupBrowser();

			// Initialize database
			AbstractDatabase? database = InitializeDatabase();

			if (database is null) // Not triggered; because InitializeDatabase calls Application.Exit()
				return;

			AutoKkutu = new AutoKkutu(database);

			AutoKkutu.PathFinder.OnPathUpdated += OnPathUpdated;
			AutoKkutu.GameEnded += OnGameEnded;
			AutoKkutu.GameModeChanged += OnGameModeChange;
			AutoKkutu.GameStarted += OnGameStarted;
			AutoKkutu.MyPathIsUnsupported += OnMyPathIsUnsupported;
			AutoKkutu.MyTurnEnded += OnMyTurnEnded;
			AutoKkutu.MyTurnStarted += OnMyTurnStarted;
			AutoKkutu.UnsupportedWordEntered += OnUnsupportedWordEntered;
			AutoKkutu.TypingWordPresented += OnTypingWordPresented;
			AutoKkutu.ChatUpdated += OnChatUpdated;
			AutoKkutu.PreviousUserTurnEnded += OnPreviousUserTurnEnded;
			AutoKkutu.RoundChanged += OnRoundChanged;
			InitializationFinished?.Invoke(null, EventArgs.Empty);

			Browser.LoadFrontPage();
			BrowserFrameLoad?.Invoke(null, EventArgs.Empty);
		}
		catch (Exception e)
		{
			Log.Error(e, "Initialization failure");
		}
	}

	public static PathFinderFlags SetupPathFinderFlags(PathFinderFlags flags = PathFinderFlags.None)
	{
		if (Prefs.EndWordEnabled && (flags.HasFlag(PathFinderFlags.ManualSearch) || AutoKkutu.PathFilter.PreviousPaths.Count > 0))  // 첫 턴 한방 방지
			flags |= PathFinderFlags.UseEndWord;
		else
			flags &= ~PathFinderFlags.UseEndWord;
		if (Prefs.AttackWordAllowed)
			flags |= PathFinderFlags.UseAttackWord;
		else
			flags &= ~PathFinderFlags.UseAttackWord;
		return flags;
	}

	private static void InitializeConfiguration()
	{
		Log.Information("Initializing configuration");

		try
		{
			Settings config = Settings.Default;
			config.Reload();
			Prefs = new Preference
			{
				AutoEnterEnabled = config.AutoEnterEnabled,
				AutoDBUpdateEnabled = config.AutoDBUpdateEnabled,
				ActiveWordPreference = config.ActiveWordPreference,
				InactiveWordPreference = config.InactiveWordPreference,
				AttackWordAllowed = config.AttackWordEnabled,
				EndWordEnabled = config.EndWordEnabled,
				ReturnModeEnabled = config.ReturnModeEnabled,
				AutoFixEnabled = config.AutoFixEnabled,
				MissionAutoDetectionEnabled = config.MissionAutoDetectionEnabled,
				DelayEnabled = config.DelayEnabled,
				DelayPerCharEnabled = config.DelayPerCharEnabled,
				DelayInMillis = config.DelayInMillis,
				DelayStartAfterCharEnterEnabled = config.DelayStartAfterWordEnterEnabled,
				InputSimulate = config.InputSimulate,
				MaxDisplayedWordCount = config.MaxDisplayedWordCount,
				FixDelayEnabled = config.FixDelayEnabled,
				FixDelayPerCharEnabled = config.FixDelayPerCharEnabled,
				FixDelayInMillis = config.FixDelayInMillis
			};

			ColorPreference = new ColorPreference
			{
				EndWordColor = config.EndWordColor.ToMediaColor(),
				AttackWordColor = config.AttackWordColor.ToMediaColor(),
				MissionWordColor = config.MissionWordColor.ToMediaColor(),
				EndMissionWordColor = config.EndMissionWordColor.ToMediaColor(),
				AttackMissionWordColor = config.AttackMissionWordColor.ToMediaColor()
			};
		}
		catch (Exception ex)
		{
			// This exception log may only available in the log file.
			Log.Error(ex, I18n.Main_ConfigLoadException);
		}
	}

	private static void SetupBrowser()
	{
		Log.Information("Initializing browser");

		// Initialize Browser
		Browser = new CefSharpBrowser();
		Browser.PageLoaded += OnPageLoaded;
		Browser.PageError += OnPageError;

		DomHandlerList = new JavaScriptHandlerList();
		DomHandlerList.InitDefaultHandlers(Browser);

		WsSniffingHandlerList = new WsHandlerList();
		WsSniffingHandlerList.InitDefaultHandlers(Browser);

	}

	private static AbstractDatabase? InitializeDatabase()
	{
		AbstractDatabase database;
		try
		{
			var watch = new Stopwatch();
			watch.Start();

			System.Configuration.Configuration databaseConfig = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = "database.config" }, ConfigurationUserLevel.None);
			database = DatabaseInit.CreateDatabase(databaseConfig);
			Log.Information(I18n.Main_Initialization, "Database connection initialization", watch.ElapsedMilliseconds);

			watch.Restart();
			Log.Information(I18n.Main_Initialization, "PathFinder initialization", watch.ElapsedMilliseconds);

			watch.Stop();
			return database;
		}
		catch (Exception ex)
		{
			Log.Error(ex, I18n.Main_DBConfigException);
			Environment.Exit(1);
			return null;
		}
	}

	/* Browser-related */

	public static void FrameReloaded() => Browser.PageLoaded += OnPageLoaded;

	/* EVENTS: Browser */

	private static void OnPageLoaded(object? sender, PageLoadedEventArgs args)
	{
		var url = args.Url;

		// Find appropriate handler for current URL
		DomHandlerBase? domHandler = DomHandlerList.GetByUri(new Uri(url));
		if (domHandler is null)
		{
			Log.Warning(I18n.Main_UnsupportedURL, url);
			return;
		}

		WsHandlerBase? wsHandler = WsSniffingHandlerList.GetByUri(new Uri(url));
		if (wsHandler is null)
		{
			Log.Warning("WebSocket sniffing is not supported on: {url}", url);
		}

		if (!AutoKkutu.HasGameSet || !AutoKkutu.Game.HasSameDomHandler(domHandler) || wsHandler != null && AutoKkutu.Game.HasSameWsSniffingHandler(wsHandler)) // TODO: Move to Lib
		{
			Log.Information("Browser frame loaded.");
			AutoKkutu.SetGame(new Game(domHandler, wsHandler));
			Browser.PageLoaded -= OnPageLoaded;
		}
	}

	private static void OnPageError(object? sender, PageErrorEventArgs args)
	{
		Log.Error("Browser load error!");
		Log.Error("Error: {error}", args.ErrorText);
	}

	public static void UpdateSearchState(/* TODO: Don't pass EventArgs directly as parameter. Destruct and reconstruct it first. */ PathUpdateEventArgs? arguments, bool isEndWord = false) => SearchStateChanged?.Invoke(null, new SearchStateChangedEventArgs(arguments, isEndWord));

	public static void UpdateStatusMessage(StatusMessage status, params object?[] formatterArgs) => StatusMessageChanged?.Invoke(null, new StatusMessageChangedEventArgs(status, formatterArgs));

	// TODO: Move to Lib
	public static void SendMessage(string message)
	{
		if (!AutoKkutu.HasGameSet)
			return;

		var opt = new AutoEnterOptions(Prefs.DelayEnabled, Prefs.DelayStartAfterCharEnterEnabled, Prefs.DelayInMillis, Prefs.DelayPerCharEnabled, Prefs.InputSimulate);
		if (opt.SimulateInput)
		{
			Task.Run(async () => await AutoKkutu.Game.AutoEnter.PerformInputSimulation(message, opt));
		}
		else
		{
			AutoKkutu.Game.UpdateChat(message);
			AutoKkutu.Game.ClickSubmitButton();
		}
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

	private static void OnRoundChanged(object? sender, EventArgs e) => preSearch = null; // Invalidate pre-search result on round changed

	private static void OnPathUpdated(object? sender, PathUpdateEventArgs args)
	{
		Log.Information(I18n.Main_PathUpdateReceived);
		if (!args.HasFlag(PathFinderFlags.ManualSearch))
			autoPathFindCache = args;
		if (args.HasFlag(PathFinderFlags.PreSearch))
			preSearch = args;

		var autoEnter = Prefs.AutoEnterEnabled && !args.HasFlag(PathFinderFlags.ManualSearch) && !args.HasFlag(PathFinderFlags.PreSearch);

		if (args.Result == PathFindResultType.NotFound && !args.HasFlag(PathFinderFlags.ManualSearch))
			UpdateStatusMessage(StatusMessage.NotFound); // Not found
		else if (args.Result == PathFindResultType.Error)
			UpdateStatusMessage(StatusMessage.Error); // Error occurred
		else if (!autoEnter)
			UpdateStatusMessage(StatusMessage.Normal);

		if (!AutoKkutu.Game.CheckPathExpired(args.Info.WithoutFlags(PathFinderFlags.PreSearch)))
		{
			Log.Warning("Expired word condition {path} rejected. Rescanning...", args.Info.Condition);
			return;
		}

		UpdateSearchState(args);
		PathListUpdated?.Invoke(null, new PathListUpdateEventArgs(args.FoundWordList.Select(po => new GuiPathObject(po)).ToImmutableList()));

		if (autoEnter)
			TryAutoEnter(args);
	}

	private static void TryAutoEnter(PathUpdateEventArgs args)
	{
		if (args.Result == PathFindResultType.NotFound)
		{
			Log.Warning(I18n.Auto_NoMorePathAvailable);
			UpdateStatusMessage(StatusMessage.NotFound);
		}
		else
		{
			var opt = new AutoEnterOptions(Prefs.DelayEnabled, Prefs.DelayStartAfterCharEnterEnabled, Prefs.DelayInMillis, Prefs.DelayPerCharEnabled, Prefs.InputSimulate);
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
				AutoKkutu.Game.AutoEnter.PerformAutoEnter(new AutoEnterInfo(opt, args.Info, wordToEnter));
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
			var result = updateTask.Execute();
			if (string.IsNullOrEmpty(result))
			{
				UpdateStatusMessage(StatusMessage.Wait);
			}
			else
			{
				UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheckDone, I18n.Status_AutoUpdate, result);
			}
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
			AutoKkutu.Game.AutoEnter.PerformAutoFix(autoPathFindCache.FilteredWordList, new AutoEnterInfo(
						new AutoEnterOptions(Prefs.DelayEnabled,
							Prefs.DelayStartAfterCharEnterEnabled,
							Prefs.DelayInMillis,
							Prefs.DelayPerCharEnabled,
							Prefs.InputSimulate),
						autoPathFindCache.Info, wordIndex: ++wordIndex), AutoKkutu.Game.GetTurnTimeMillis()); // FIXME: according to current implementation, if user searches anything between AutoEnter and AutoFix, AutoFix uses the user search result, instead of previous AutoEnter search result.
	}

	private static void OnMyTurnEnded(object? sender, EventArgs e)
	{
		Log.Debug(I18n.Main_WordIndexReset);
		wordIndex = 0;
	}

	// TODO: Move to Lib
	private static void OnMyTurnStarted(object? sender, WordConditionPresentEventArgs args)
	{
		if (preSearch != null && AutoKkutu.Game.CheckPathExpired(preSearch.Info.WithFlags(PathFinderFlags.NoRescan)))
		{
			Log.Information("Using the pre-search result for: {condition}", preSearch.Info.Condition);
			TryAutoEnter(preSearch);
			return;
		}

		if (preSearch == null)
			Log.Warning("Pre-search data not available. Starting the search.");
		else
			Log.Warning("Pre-search path is expired! Presearch: {pre}, Search: {now}", preSearch.Info.Condition, args.Condition);

		AutoKkutu.PathFinder.FindPath(
			AutoKkutu.Game.CurrentGameMode,
			new PathFinderParameter(args.Condition, SetupPathFinderFlags(), Prefs.ReturnModeEnabled, Prefs.MaxDisplayedWordCount),
			Prefs.ActiveWordPreference);
	}

	private static void OnPreviousUserTurnEnded(object? sender, PreviousUserTurnEndedEventArgs args)
	{
		if (!args.CanPresearch || args.Condition is null)
		{
			preSearch = null;
			return;
		}

		Log.Debug("Performing pre-search on: {condition}", args.Condition);
		AutoKkutu.PathFinder.FindPath(
			AutoKkutu.Game.CurrentGameMode,
			new PathFinderParameter((WordCondition)args.Condition, SetupPathFinderFlags() | PathFinderFlags.PreSearch, Prefs.ReturnModeEnabled, Prefs.MaxDisplayedWordCount),
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

	private static void OnTypingWordPresented(object? sender, WordPresentEventArgs args)
	{
		var word = args.Word;

		if (!Prefs.AutoEnterEnabled)
			return;

		AutoKkutu.Game.AutoEnter.PerformAutoEnter(new AutoEnterInfo(
			new AutoEnterOptions(Prefs.DelayEnabled,
				Prefs.DelayStartAfterCharEnterEnabled,
				Prefs.DelayInMillis,
				Prefs.DelayPerCharEnabled,
				Prefs.InputSimulate),
			PathFinderParameter.Empty,
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
