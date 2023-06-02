using AutoKkutuLib;
using AutoKkutuLib.CefSharp;
using AutoKkutuLib.Database;
using AutoKkutuLib.Extension;
using AutoKkutuLib.Game;
using AutoKkutuLib.Game.Events;
using AutoKkutuLib.Handlers;
using AutoKkutuLib.Handlers.JavaScript;
using AutoKkutuLib.Path;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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

	public static IHandlerList Handler { get; private set; } = null!;

	public static AutoKkutu AutoKkutu
	{
		get; private set;
	} = null!;

	/* EVENTS */
	public static event EventHandler? BrowserFrameLoad;

	public static event EventHandler? HandlerRegistered;

	public static event EventHandler? PathListUpdated;

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
			AutoKkutu.MyWordPresented += OnMyTurn;
			AutoKkutu.UnsupportedWordEntered += OnUnsupportedWordEntered;
			AutoKkutu.TypingWordPresented += OnTypingWordPresented;
			AutoKkutu.ChatUpdated += OnChatUpdated;
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

		Handler = new JavaScriptHandlerList();
		Handler.InitDefaultHandlers(Browser);
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
		HandlerBase? handler = Handler.GetByUri(new Uri(url));
		if (handler is null)
		{
			Log.Warning(I18n.Main_UnsupportedURL, url);
			return;
		}

		if (!AutoKkutu.HasGameSet || !AutoKkutu.Game.HasSameHandler(handler)) // TODO: Move to Lib
		{
			Log.Information("Browser frame loaded.");
			AutoKkutu.SetGame(new Game(handler));
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

		if (Prefs.DelayEnabled && Prefs.DelayPerCharEnabled && Prefs.InputSimulate)
		{
			Task.Run(async () => await AutoKkutu.Game.AutoEnter.PerformInputSimulation(message, Prefs.DelayInMillis));
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

	private static PathFinderParameter? lastPathFinderParameter = null;

	private static void OnPathUpdated(object? sender, PathUpdateEventArgs args)
	{
		Log.Information(I18n.Main_PathUpdateReceived);
		PathFinderParameter path = args.Result;
		lastPathFinderParameter = path;

		var autoEnter = Prefs.AutoEnterEnabled && !args.Result.Options.HasFlag(PathFinderFlags.ManualSearch);

		if (args.ResultType == PathFindResult.NotFound && !path.Options.HasFlag(PathFinderFlags.ManualSearch))
			UpdateStatusMessage(StatusMessage.NotFound); // Not found
		else if (args.ResultType == PathFindResult.Error)
			UpdateStatusMessage(StatusMessage.Error); // Error occurred
		else if (!autoEnter)
			UpdateStatusMessage(StatusMessage.Normal);

		if (!AutoKkutu.Game.IsValidPath(path))
		{
			Log.Warning("Invalid path {path} (missionChar: {missionChar}) rejected.", path.Word, path.MissionChar);
			return;
		}

		UpdateSearchState(args);

		PathListUpdated?.Invoke(null, EventArgs.Empty);

		if (autoEnter)
		{
			if (args.ResultType == PathFindResult.NotFound)
			{

				Log.Warning(I18n.Auto_NoMorePathAvailable);
				UpdateStatusMessage(StatusMessage.NotFound);
			}
			else
			{
				var time = AutoKkutu.Game.TurnTimeMillis;
				(var wordToEnter, var timeover) = AutoKkutu.PathFinder.AvailableWordList.GetWordByIndex(Prefs.DelayEnabled && Prefs.DelayPerCharEnabled, Prefs.DelayInMillis, time);
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
					AutoKkutu.Game.AutoEnter.PerformAutoEnter(new AutoEnterParameter(
						Prefs.DelayEnabled,
						Prefs.DelayStartAfterCharEnterEnabled,
						Prefs.DelayInMillis,
						Prefs.DelayPerCharEnabled,
						Prefs.InputSimulate,
						path,
						wordToEnter));
				}
			}
		}
	}

	/* EVENTS: Handler */

	private static void OnGameEnded(object? sender, EventArgs e)
	{
		UpdateSearchState(null, false);
		// ResetPathList();
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
		if (lastPathFinderParameter is null)
			return;

		var word = args.Word;
		Log.Warning(I18n.Main_MyPathIsUnsupported, word);

		// TODO: check lastPathFinderParameter out-of-sync

		if (Prefs.AutoEnterEnabled && Prefs.AutoFixEnabled)
			AutoKkutu.Game.AutoEnter.PerformAutoFix(AutoKkutu.PathFinder.AvailableWordList, new AutoEnterParameter(
						Prefs.DelayEnabled,
						Prefs.DelayStartAfterCharEnterEnabled,
						Prefs.DelayInMillis,
						Prefs.DelayPerCharEnabled,
						Prefs.InputSimulate,
						lastPathFinderParameter, WordIndex: ++wordIndex), AutoKkutu.Game.TurnTimeMillis); // FIXME: according to current implementation, if user searches anything between AutoEnter and AutoFix, AutoFix uses the user search result, instead of previous AutoEnter search result.
	}

	private static void OnMyTurnEnded(object? sender, EventArgs e)
	{
		Log.Debug(I18n.Main_WordIndexReset);
		// AutoEnter.ResetWordIndex();
		wordIndex = 0;
	}

	// TODO: Move to Lib
	private static void OnMyTurn(object? sender, WordConditionPresentEventArgs args)
	{
		AutoKkutu.PathFinder.FindPath(AutoKkutu.Game.CurrentGameMode, new PathFinderParameter(args.Word, args.MissionChar, SetupPathFinderFlags(), Prefs.ReturnModeEnabled, Prefs.MaxDisplayedWordCount), Prefs.ActiveWordPreference);
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

		AutoKkutu.Game.AutoEnter.PerformAutoEnter(new AutoEnterParameter(
			Prefs.DelayEnabled,
			Prefs.DelayStartAfterCharEnterEnabled,
			Prefs.DelayInMillis,
			Prefs.DelayPerCharEnabled,
			Prefs.InputSimulate,
			null,
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
