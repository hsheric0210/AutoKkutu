using AutoKkutu.Constants;
using AutoKkutu.Database;
using AutoKkutu.Modules.AutoEnter;
using AutoKkutu.Modules.HandlerManager;
using AutoKkutu.Modules.HandlerManager.Handler;
using AutoKkutu.Modules.PathFinder;
using AutoKkutu.Modules.PathManager;
using AutoKkutu.Utils;
using AutoKkutu.Utils.Extension;
using CefSharp;
using CefSharp.Wpf;
using Serilog;
using System;
using System.Configuration;
using System.Diagnostics;

namespace AutoKkutu
{
	public static class AutoKkutuMain
	{
		public static AutoKkutuConfiguration Configuration
		{
			get; set;
		} = null!;

		public static AutoKkutuColorPreference ColorPreference
		{
			get; set;
		} = null!;

		public static ChromiumWebBrowser Browser
		{
			get; private set;
		} = null!;

		public static AbstractDatabase Database
		{
			get; private set;
		} = null!;

		public static IHandlerManager? Handler
		{
			get; private set;
		}

		/* EVENTS */
		public static event EventHandler? InitializeUI;

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
				// Initialize CEF
				InitializeCEF();

				// Load default config
				InitializeConfiguration();

				// Initialize browser
				InitializeBrowser();

				// Initialize database
				InitializeDatabase();

				// Initialize UI
				InitializeUI?.Invoke(null, EventArgs.Empty);

				PathFinder.OnPathUpdated += OnPathUpdated;
				InitializationFinished?.Invoke(null, EventArgs.Empty);
			}
			catch (Exception e)
			{
				Log.Error(e, "Initialization failure");
			}
		}

		private static void InitializeCEF()
		{
			Log.Information("Initializing CEF");

			// TODO: Configurable CEF settings
			using var settings = new CefSettings
			{
				LogFile = "CefSharp.log",
				LogSeverity = LogSeverity.Default,
				
				CefCommandLineArgs =
				{
					{
						"disable-direct-write",
						"1"
					},
					"disable-gpu",
					"enable-begin-frame-scheduling"
				},
				UserAgent = "Chrome",
				CachePath = Environment.CurrentDirectory + "\\CefSharp"
			};

			try
			{
				if (!Cef.Initialize(settings, true, (IApp?)null))
					Log.Warning("CEF initialization failed.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "CEF initialization exception.");
			}
		}

		private static void InitializeConfiguration()
		{
			Log.Information("Initializing configuration");

			try
			{
				Settings config = Settings.Default;
				config.Reload();
				Configuration = new AutoKkutuConfiguration
				{
					AutoEnterEnabled = config.AutoEnterEnabled,
					AutoDBUpdateEnabled = config.AutoDBUpdateEnabled,
					AutoDBUpdateMode = config.AutoDBUpdateMode,
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
					GameModeAutoDetectEnabled = config.GameModeAutoDetectionEnabled,
					MaxDisplayedWordCount = config.MaxDisplayedWordCount,
					FixDelayEnabled = config.FixDelayEnabled,
					FixDelayPerCharEnabled = config.FixDelayPerCharEnabled,
					FixDelayInMillis = config.FixDelayInMillis
				};

				ColorPreference = new AutoKkutuColorPreference
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

		private static void InitializeBrowser()
		{
			Log.Information("Initializing browser");

			// Initialize Browser
			Browser = new ChromiumWebBrowser
			{
				Address = "https://kkutu.pink",
				UseLayoutRounding = true
			};

			Browser.FrameLoadEnd += OnBrowserFrameLoadEnd;
		}

		private static void InitializeDatabase()
		{
			try
			{
				var watch = new Stopwatch();
				watch.Start();

				Configuration databaseConfig = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = "database.config" }, ConfigurationUserLevel.None);
				Database = DatabaseUtils.CreateDatabase(databaseConfig);
				Log.Information(I18n.Main_Initialization, "Database connection initialization", watch.ElapsedMilliseconds);

				watch.Restart();
				PathManager.Initialize();
				Log.Information(I18n.Main_Initialization, "PathFinder initialization", watch.ElapsedMilliseconds);

				watch.Stop();
			}
			catch (Exception ex)
			{
				Log.Error(ex, I18n.Main_DBConfigException);
				Environment.Exit(1);
			}
		}

		/* Browser-related */

		public static void FrameReloaded() => Browser.FrameLoadEnd += OnBrowserFrameLoadEnd;

		private static void RemoveAd()
		{
			// Kkutu.co.kr
			Browser.ExecuteScriptAsyncWhenPageLoaded("document.body.style.overflow ='hidden'", false);
			Browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADBox').style = 'display:none'", false);
			Browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADVERTISEMENT').style = 'display:none'", false);
			Browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADVERTISEMENT_TITLE').style = 'display:none'", false);
			Browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementsByClassName('kktukorea__1LZzX_0')[0].style = 'display:none'", false);

			// Kkutu.pink
			Browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('google-center-div')[0].style = 'display:none'", false);

			// Kkutu.org
			Browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementsByClassName('ADBox Product')[0].style = 'display:none'", false);
		}

		/* Handler-related */

		private static void LoadHandler(IHandlerManager handler)
		{
			Log.Information(I18n.Main_CEFFrameLoadEnd);

			handler.GameStarted += OnGameStarted;
			handler.GameEnded += OnGameEnded;
			handler.MyWordPresented += OnMyTurn;
			handler.MyTurnEnded += OnMyTurnEnded;
			handler.UnsupportedWordEntered += OnUnsupportedWordEntered;
			handler.MyPathIsUnsupported += OnMyPathIsUnsupported;
			handler.RoundChanged += OnRoundChange;
			handler.GameModeChanged += OnGameModeChange;
			handler.TypingWordPresented += OnTypingWordPresented;
			handler.ChatUpdated += OnChatUpdated;
			handler.Start();

			Log.Information(I18n.Main_UseHandler, handler.GetID());
			RemoveAd();

			HandlerRegistered?.Invoke(null, EventArgs.Empty);
		}

		private static void UnloadHandler(IHandlerManager handler)
		{
			Log.Information(I18n.HandlerRegistry_Unregistered, handler.GetID());

			// Unregister previous handler
			handler.GameStarted -= OnGameStarted;
			handler.GameEnded -= OnGameEnded;
			handler.MyWordPresented -= OnMyTurn;
			handler.MyTurnEnded -= OnMyTurnEnded;
			handler.UnsupportedWordEntered -= OnUnsupportedWordEntered;
			handler.MyPathIsUnsupported -= OnMyPathIsUnsupported;
			handler.RoundChanged -= OnRoundChange;
			handler.GameModeChanged -= OnGameModeChange;
			handler.TypingWordPresented -= OnTypingWordPresented;
			handler.ChatUpdated -= OnChatUpdated;
			handler.Stop();
		}

		// is this really required?
		private static void ResetPathList()
		{
			Log.Information(I18n.Main_ResetPathList);
			PathFinder.ResetFinalList();
			PathListUpdated?.Invoke(null, EventArgs.Empty);
		}

		/* EVENTS: Browser */

		private static void OnBrowserFrameLoadEnd(object? sender, FrameLoadEndEventArgs args)
		{
			if (Handler != null)
			{
				// Unload previous handler
				UnloadHandler(Handler);
				Handler = null;
			}

			string url = args.Url;

			// Find appropriate handler for current URL
			Handler = CommonHandler.GetHandler(url);
			if (Handler != null)
			{
				// Initialize and load the handler
				Browser.FrameLoadEnd -= OnBrowserFrameLoadEnd;
				LoadHandler(Handler);
			}
			else
			{
				Log.Warning(I18n.Main_UnsupportedURL, url);
			}
		}

		public static void UpdateSearchState(/* TODO: Don't pass EventArgs directly as parameter. Destruct and reconstruct it first. */ PathUpdateEventArgs? arguments, bool isEndWord = false) => SearchStateChanged?.Invoke(null, new SearchStateChangedEventArgs(arguments, isEndWord));

		public static void UpdateStatusMessage(StatusMessage status, params object?[] formatterArgs) => StatusMessageChanged?.Invoke(null, new StatusMessageChangedEventArgs(status, formatterArgs));

		public static void SendMessage(string message)
		{
			if (InputSimulation.CanSimulateInput())
			{
				Task.Run(async () => await InputSimulation.PerformInputSimulation(message));
			}
			else
			{
				Handler.UpdateChat(message);
				Handler.ClickSubmitButton();
			}
			InputStopwatch.Restart();
		}

		public static void ToggleFeature(Func<AutoKkutuConfiguration, bool> toggleFunc, StatusMessage displayStatus)
		{
			if (toggleFunc is null)
				throw new ArgumentNullException(nameof(toggleFunc));
			UpdateStatusMessage(displayStatus, toggleFunc(Configuration) ? I18n.Enabled : I18n.Disabled);
		}

		/* EVENTS: PathFinder */

		private static void OnPathUpdated(object? sender, PathUpdateEventArgs args)
		{
			Log.Information(I18n.Main_PathUpdateReceived);
			PathFinderParameter path = args.Result;

			bool autoEnter = Configuration.AutoEnterEnabled && !args.Result.Options.HasFlag(PathFinderOptions.ManualSearch);

			if (args.ResultType == PathFindResult.NotFound && !path.Options.HasFlag(PathFinderOptions.ManualSearch))
				UpdateStatusMessage(StatusMessage.NotFound); // Not found
			else if (args.ResultType == PathFindResult.Error)
				UpdateStatusMessage(StatusMessage.Error); // Error occurred
			else if (!autoEnter)
				UpdateStatusMessage(StatusMessage.Normal);

			if (!Handler.IsValidPath(path))
				return;

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
					string? wordToEnter = AutoEnter.GetWordByIndex(PathFinder.QualifiedList, Configuration.DelayEnabled && Configuration.DelayPerCharEnabled, Configuration.DelayInMillis, Handler.TurnTimeMillis);
					if (string.IsNullOrEmpty(wordToEnter))
					{
						Log.Warning(I18n.Auto_TimeOver);
						UpdateStatusMessage(StatusMessage.NotFound);
					}
					else
					{
						AutoEnter.PerformAutoEnter(wordToEnter, path);
					}
				}
			}
		}

		/* EVENTS: Handler */

		private static void OnGameEnded(object? sender, EventArgs e)
		{
			UpdateSearchState(null, false);
			// ResetPathList();
			PathManager.UnsupportedPathList.Clear();
			if (Configuration.AutoDBUpdateMode == DatabaseUpdateTiming.OnGameEnd)
			{
				UpdateStatusMessage(StatusMessage.DatabaseIntegrityCheck, I18n.Status_AutoUpdate);
				string? result = PathManager.UpdateDatabase();
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

		private static void OnGameModeChange(object? sender, GameModeChangeEventArgs args)
		{
			if (Configuration.GameModeAutoDetectEnabled)
			{
				GameMode newGameMode = args.GameMode;
				Configuration.GameMode = newGameMode;
				Log.Information(I18n.Main_GameModeUpdated, ConfigEnums.GetGameModeName(newGameMode));
			}
		}

		private static void OnGameStarted(object? sender, EventArgs e)
		{
			UpdateStatusMessage(StatusMessage.Normal);
			AutoEnter.ResetWordIndex();
		}

		private static void OnMyPathIsUnsupported(object? sender, UnsupportedWordEventArgs args)
		{
			string word = args.Word;
			Log.Warning(I18n.Main_MyPathIsUnsupported, word);

			if (Configuration.AutoEnterEnabled && Configuration.AutoFixEnabled)
				AutoEnter.PerformAutoFix();
		}

		private static void OnMyTurnEnded(object? sender, EventArgs e)
		{
			Log.Debug(I18n.Main_WordIndexReset);
			AutoEnter.ResetWordIndex();
		}

		private static void OnMyTurn(object? sender, WordPresentEventArgs args) => PathFinder.StartPathFinding(Configuration, args.Word, args.MissionChar, PathFinderOptions.None);

		private static void OnUnsupportedWordEntered(object? sender, UnsupportedWordEventArgs args)
		{
			bool isInexistent = !args.IsExistingWord;
			string word = args.Word;
			if (isInexistent)
				Log.Warning(I18n.Main_UnsupportedWord_Inexistent, word);
			else
				Log.Warning(I18n.Main_UnsupportedWord_Existent, word);
			PathManager.AddToUnsupportedWord(word, isInexistent);
		}

		private static void OnRoundChange(object? sender, EventArgs e)
		{
			if (Configuration.AutoDBUpdateMode == DatabaseUpdateTiming.OnRoundEnd)
				PathManager.UpdateDatabase();
		}

		private static void OnTypingWordPresented(object? sender, WordPresentEventArgs args)
		{
			string word = args.Word.Content;

			if (!Configuration.AutoEnterEnabled)
				return;

			AutoEnter.PerformAutoEnter(word, null, I18n.Main_Presented);
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
}
