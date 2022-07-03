using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Utils;
using CefSharp;
using CefSharp.Wpf;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Globalization;

namespace AutoKkutu
{
	// TODO: Hotkey 지원 추가 - 자동 입력 토글, 딜레이 토글

	public partial class MainWindow : Window
	{
		public const string VERSION = "1.0.0000";

		private ChromiumWebBrowser Browser
		{
			get; set;
		}

		private static AutoKkutuColorPreference? CurrentColorPreference
		{
			get; set;
		}

		private static AutoKkutuConfiguration? CurrentConfig
		{
			get; set;
		}

		public DatabaseWithDefaultConnection Database
		{
			get; private set;
		}

		public CommonHandler? Handler
		{
			get; private set;
		}

		// Succeed KKutu-Helper Release v5.6.8500
		private const string TITLE = "AutoKkutu - Improved KKutu-Helper";

		public static readonly Logger Logger = LogManager.GetLogger("MainThread");
		private readonly Stopwatch inputStopwatch = new();
		private int WordIndex;

		public MainWindow()
		{
			// Initialize CEF
			InitializeCEF();

			// Load default config
			InitializeConfiguration();

			// Initialize Browser
			Browser = new ChromiumWebBrowser
			{
				Address = "https://kkutu.pink",
				UseLayoutRounding = true
			};
			JSEvaluator.RegisterBrowser(Browser);

			// Visual components setup
			InitializeComponent();
			ConsoleManager.Show();
			Title = TITLE;
			VersionLabel.Content = "v1.0";

			Logger.Info(I18n.Main_StartLoad);
			LoadOverlay.Visibility = Visibility.Visible;

			this.ChangeStatusBar(CurrentStatus.Wait);
			SetSearchState(null, false);

			Browser.FrameLoadEnd += OnBrowserFrameLoadEnd;
			Browser.FrameLoadEnd += OnBrowserFrameLoadEnd_RunOnce;
			DatabaseEvents.DatabaseError += OnDataBaseError;
			BrowserContainer.Content = Browser;

			try
			{
				var watch = new Stopwatch();
				watch.Start();
				Configuration databaseConfig = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = "database.config" }, ConfigurationUserLevel.None);
				Database = DatabaseUtils.CreateDatabase(databaseConfig);
				PathFinder.Init(Database.DefaultConnection);
				watch.Stop();
				Logger.Info(CultureInfo.CurrentCulture, I18n.Main_Initialization, nameof(PathFinder), watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, I18n.Main_DBConfigException);
				Environment.Exit(1);
			}
		}

		public static void UpdateColorPreference(AutoKkutuColorPreference newColorPref)
		{
			Logger.Info(I18n.Main_ColorPrefsUpdated);
			CurrentColorPreference = newColorPref;
			PathFinder.UpdateColorPreference(newColorPref);
		}

		public static void UpdateConfig(AutoKkutuConfiguration newConfig)
		{
			Logger.Info(I18n.Main_ConfigUpdated);
			CurrentConfig = newConfig;
			PathFinder.UpdateConfig(newConfig);
			CommonHandler.UpdateConfig(newConfig);
		}

		private static void InitializeCEF()
		{
			// TODO: Configurable CEF settings
			using var settings = new CefSettings
			{
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
				CachePath = Environment.CurrentDirectory + "\\Cache"
			};
			Cef.Initialize(settings, true, (IApp?)null);
		}

		private static void InitializeConfiguration()
		{
			try
			{
				Settings config = AutoKkutu.Settings.Default;
				config.Reload();
				CurrentConfig = new AutoKkutuConfiguration
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
					GameModeAutoDetectEnabled = config.GameModeAutoDetectionEnabled,
					MaxDisplayedWordCount = config.MaxDisplayedWordCount,
					FixDelayEnabled = config.FixDelayEnabled,
					FixDelayPerCharEnabled = config.FixDelayPerCharEnabled,
					FixDelayInMillis = config.FixDelayInMillis
				};
				PathFinder.UpdateConfig(CurrentConfig);
				CommonHandler.UpdateConfig(CurrentConfig);

				CurrentColorPreference = new AutoKkutuColorPreference
				{
					EndWordColor = config.EndWordColor.ToMediaColor(),
					AttackWordColor = config.AttackWordColor.ToMediaColor(),
					MissionWordColor = config.MissionWordColor.ToMediaColor(),
					EndMissionWordColor = config.EndMissionWordColor.ToMediaColor(),
					AttackMissionWordColor = config.AttackMissionWordColor.ToMediaColor()
				};

				PathFinder.UpdateColorPreference(CurrentColorPreference);
			}
			catch (Exception ex)
			{
				// This log may only available in log file
				Logger.Error(ex, I18n.Main_ConfigLoadException);
			}
		}

		private void ChatField_KeyDown(object? sender, KeyEventArgs e)
		{
			if (e.Key is Key.Enter or Key.Return)
				SubmitChat_Click(sender, e);
		}

		private bool CheckPathIsValid(ResponsePresentedWord word, string missionChar, PathFinderOptions flags)
		{
			if (Handler == null || flags.HasFlag(PathFinderOptions.ManualSearch))
				return true;

			bool differentWord = Handler.CurrentPresentedWord != null && !word.Equals(Handler.CurrentPresentedWord);
			bool differentMissionChar = CurrentConfig?.MissionAutoDetectionEnabled != false && !string.IsNullOrWhiteSpace(Handler.CurrentMissionChar) && !string.Equals(missionChar, Handler.CurrentMissionChar, StringComparison.OrdinalIgnoreCase);
			if (Handler.IsMyTurn && (differentWord || differentMissionChar))
			{
				Logger.Warn(CultureInfo.CurrentCulture, I18n.PathFinder_InvalidatedUpdate, differentWord, differentMissionChar);
				StartPathFinding(Handler.CurrentPresentedWord, Handler.CurrentMissionChar, flags);
				return false;
			}
			return true;
		}

		private void ClipboardSubmit_Click(object? sender, RoutedEventArgs e)
		{
			try
			{
				string clipboard = Clipboard.GetText();
				if (!string.IsNullOrWhiteSpace(clipboard))
					SendMessage(clipboard);
			}
			catch (Exception ex)
			{
				Logger.Warn(I18n.Main_ClipboardSubmitException, ex);
			}
		}

		private void ColorManager_Click(object? sender, RoutedEventArgs e)
		{
			if (CurrentColorPreference != null)
				new ColorManagement(CurrentColorPreference).Show();
		}

		private void CommonHandler_GameEnd(object? sender, EventArgs e)
		{
			SetSearchState(null, false);
			ResetPathList();
			PathFinder.UnsupportedPathList.Clear();
			if (CurrentConfig.RequireNotNull().AutoDBUpdateMode == AutoDBUpdateMode.OnGameEnd)
			{
				this.ChangeStatusBar(CurrentStatus.DatabaseIntegrityCheck, I18n.Status_AutoUpdate);
				string? result = PathFinder.AutoDBUpdate();
				if (string.IsNullOrEmpty(result))
				{
					this.ChangeStatusBar(CurrentStatus.Wait);
				}
				else
				{
					if (new FileInfo("GameResult.txt").Exists)
					{
						try
						{
							File.AppendAllText("GameResult.txt", $"[{Handler.RequireNotNull().GetRoomInfo()}] {result}{Environment.NewLine}");
						}
						catch (Exception ex)
						{
							Logger.Warn(ex, I18n.Main_GameResultWriteException);
						}
					}

					this.ChangeStatusBar(CurrentStatus.DatabaseIntegrityCheckDone, I18n.Status_AutoUpdate, result);
				}
			}
			else
			{
				this.ChangeStatusBar(CurrentStatus.Wait);
			}
		}

		private void CommonHandler_GameModeChangeEvent(object? sender, GameModeChangeEventArgs args)
		{
			if (CurrentConfig.RequireNotNull().GameModeAutoDetectEnabled)
			{
				GameMode newGameMode = args.GameMode;
				CurrentConfig.GameMode = newGameMode;
				Logger.Info(CultureInfo.CurrentCulture, I18n.Main_GameModeUpdated, ConfigEnums.GetGameModeName(newGameMode));
				UpdateConfig(CurrentConfig);
			}
		}

		private void CommonHandler_GameStart(object? sender, EventArgs e)
		{
			this.ChangeStatusBar(CurrentStatus.Normal);
			WordIndex = 0;
			inputStopwatch.Restart();
		}

		private void CommonHandler_MyPathIsUnsupported(object? sender, UnsupportedWordEventArgs args)
		{
			string word = args.Word;
			Logger.Warn(CultureInfo.CurrentCulture, I18n.Main_MyPathIsUnsupported, word);

			if (!CurrentConfig.RequireNotNull().AutoFixEnabled)
				return;

			if (CurrentConfig.AutoEnterEnabled)
			{
				try
				{
					string? path = TimeFilterQualifiedWordListIndexed(PathFinder.QualifiedList, ++WordIndex);
					if (path == null)
					{
						Logger.Warn(I18n.Main_NoMorePathAvailable);
						this.ChangeStatusBar(CurrentStatus.NotFound);
						return;
					}

					if (CurrentConfig.FixDelayEnabled)
					{
						int delay = CurrentConfig.FixDelayInMillis;
						if (CurrentConfig.FixDelayPerCharEnabled)
							delay *= path.Length;
						this.ChangeStatusBar(CurrentStatus.Delaying, delay);
						Logger.Debug(CultureInfo.CurrentCulture, I18n.Main_WaitingSubmitNext, delay);
						Task.Run(async () =>
						{
							await Task.Delay(delay);
							PerformAutoEnter(path, null, I18n.Main_Next);
						});
					}
					else
					{
						PerformAutoEnter(path, null, I18n.Main_Next);
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex, I18n.Main_PathSubmitException);
				}
			}
		}

		private void CommonHandler_MyTurnEndEvent(object? sender, EventArgs e)
		{
			Logger.Debug(I18n.Main_WordIndexReset);
			WordIndex = 0;
		}

		private void CommonHandler_MyTurnEvent(object? sender, WordPresentEventArgs args) => StartPathFinding(args.Word, args.MissionChar, PathFinderOptions.None);

		private void CommonHandler_onUnsupportedWordEntered(object? sender, UnsupportedWordEventArgs args)
		{
			bool isInexistent = !args.IsExistingWord;
			string word = args.Word;
			if (isInexistent)
				Logger.Warn(CultureInfo.CurrentCulture, I18n.Main_UnsupportedWord_Inexistent, word);
			else
				Logger.Warn(CultureInfo.CurrentCulture, I18n.Main_UnsupportedWord_Existent, word);
			PathFinder.AddToUnsupportedWord(word, isInexistent);
		}

		private void CommonHandler_RoundChangeEvent(object? sender, EventArgs e)
		{
			if (CurrentConfig.RequireNotNull().AutoDBUpdateMode == AutoDBUpdateMode.OnRoundEnd)
				PathFinder.AutoDBUpdate();
		}

		private void CommonHandler_OnTypingWordPresentedEvent(object? sender, WordPresentEventArgs args)
		{
			string word = args.Word.Content;

			Handler.RequireNotNull();
			if (!CurrentConfig.RequireNotNull().AutoEnterEnabled)
				return;

			DelayedEnter(word, null, I18n.Main_Presented);
		}

		private static ICollection<string>? GetEndWordList(GameMode mode) => mode switch
		{
			GameMode.FirstAndLast => PathFinder.ReverseEndWordList,
			GameMode.Kkutu => PathFinder.KkutuEndWordList,
			_ => PathFinder.EndWordList,
		};

		private void HideLoadOverlay()
		{
			var img = new BitmapImage();
			img.BeginInit();
			img.UriSource = new Uri($@"Images\{Database.GetDBType()}.png", UriKind.Relative);
			img.EndInit();
			DBLogo.Source = img;

			LoadOverlay.Visibility = Visibility.Hidden;
		}

		private void OnToggleDelay(object? sender, RoutedEventArgs e) => ToggleFeature(config => config.DelayEnabled = !config.DelayEnabled, CurrentStatus.DelayToggled);

		private void OnToggleAllDelay(object? sender, RoutedEventArgs e) => ToggleFeature(config => config.DelayEnabled = config.FixDelayEnabled = !config.DelayEnabled, CurrentStatus.AllDelayToggled);

		private void OnToggleAutoEnter(object? sender, RoutedEventArgs e) => ToggleFeature(config => config.AutoEnterEnabled = !config.AutoEnterEnabled, CurrentStatus.AutoEnterToggled);

		private void ToggleFeature(Func<AutoKkutuConfiguration, bool> toggleFunc, CurrentStatus displayStatus)
		{
			if (CurrentConfig == null)
				return;
			bool newState = toggleFunc(CurrentConfig);
			UpdateConfig(CurrentConfig);
			this.ChangeStatusBar(displayStatus, newState ? I18n.Enabled : I18n.Disabled);
		}

		private void OnBrowserFrameLoadEnd(object? sender, FrameLoadEndEventArgs e)
		{
			if (Handler != null)
			{
				UnloadPreviousHandler(Handler);
				Handler = null;
			}

			// Initialize handler and Register event handlers
			string url = e.Url;
			Handler = CommonHandler.GetHandler(url);
			if (Handler != null)
				RegisterHandler(Handler);
			else
				Logger.Warn(CultureInfo.CurrentCulture, I18n.Main_UnsupportedURL, url);
		}

		private void OnBrowserFrameLoadEnd_RunOnce(object? sender, FrameLoadEndEventArgs e)
		{
			Browser.FrameLoadEnd -= OnBrowserFrameLoadEnd_RunOnce;

			PathFinder.OnPathUpdated += OnPathUpdated;
			DatabaseEvents.DatabaseIntegrityCheckStart += OnDatabaseCheckStart;
			DatabaseEvents.DatabaseIntegrityCheckDone += OnDatabaseCheckDone;
			DatabaseEvents.DatabaseImportStart += OnDatabaseImportStart;
			DatabaseEvents.DatabaseImportDone += OnDatabaseImportDone;
		}

		private void OnDatabaseCheckDone(object? sender, DataBaseIntegrityCheckDoneEventArgs args) => this.ChangeStatusBar(CurrentStatus.DatabaseIntegrityCheckDone, args.Result);

		private void OnDatabaseCheckStart(object? sender, EventArgs e) => this.ChangeStatusBar(CurrentStatus.DatabaseIntegrityCheck);

		private void OnDataBaseError(object? sender, EventArgs e) => this.ChangeStatusBar(CurrentStatus.Error);

		private void OnDatabaseImportDone(object? sender, DatabaseImportEventArgs args) => this.ChangeStatusBar(CurrentStatus.BatchJobDone, args.Name, args.Result);

		private void OnDatabaseImportStart(object? sender, DatabaseImportEventArgs args) => this.ChangeStatusBar(CurrentStatus.BatchJob, args.Name);

		private void OnDBManagementClick(object? sender, RoutedEventArgs e) => new DatabaseManagement(Database).Show();

		private void OnOpenDevConsoleClick(object? sender, RoutedEventArgs e) => Browser.ShowDevTools();

		private void OnPathListContextMenuOpen(object? sender, ContextMenuEventArgs e)
		{
			var source = (FrameworkElement)e.Source;
			ContextMenu contextMenu = source.ContextMenu;
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			var current = ((PathObject)currentSelected);
			foreach (Control item in contextMenu.Items)
			{
				if (item is not MenuItem)
					continue;

				bool available = true;
				switch (item.Name.ToUpperInvariant())
				{
					case "MAKEEND":
						available = current.MakeEndAvailable;
						break;

					case "MAKEATTACK":
						available = current.MakeAttackAvailable;
						break;

					case "MAKENORMAL":
						available = current.MakeNormalAvailable;
						break;

					case "INCLUDE":
						available = current.AlreadyUsed || current.Excluded;
						break;

					case "EXCLUDE":
						available = !current.AlreadyUsed;
						break;

					case "REMOVE":
						available = !current.Excluded;
						break;
				}
				item.IsEnabled = available;
			}
		}

		private void OnPathListMakeAttackClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeAttack(CurrentConfig.RequireNotNull().GameMode, Database.DefaultConnection);
		}

		private void OnPathListMakeEndClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeEnd(CurrentConfig.RequireNotNull().GameMode, Database.DefaultConnection);
		}

		private void OnPathListMakeNormalClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeNormal(CurrentConfig.RequireNotNull().GameMode, Database.DefaultConnection);
		}

		private void OnPathListQueueExcludedClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			var path = (PathObject)currentSelected;
			path.Excluded = true;
			path.RemoveQueued = false;
			try
			{
				PathFinder.PathListLock.EnterWriteLock();
				PathFinder.UnsupportedPathList.Add(path.Content);
				PathFinder.InexistentPathList.Remove(path.Content);
			}
			finally
			{
				PathFinder.PathListLock.ExitWriteLock();
			}
		}

		private void OnPathListIncludeClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			var path = (PathObject)currentSelected;
			path.Excluded = false;
			path.RemoveQueued = false;
			try
			{
				PathFinder.PathListLock.EnterWriteLock();
				PathFinder.UnsupportedPathList.Remove(path.Content);
				PathFinder.InexistentPathList.Remove(path.Content);
			}
			finally
			{
				PathFinder.PathListLock.ExitWriteLock();
			}
		}

		private void OnPathListQueueRemoveClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			var path = (PathObject)currentSelected;
			path.Excluded = false;
			path.RemoveQueued = true;
			try
			{
				PathFinder.PathListLock.EnterWriteLock();
				PathFinder.UnsupportedPathList.Add(path.Content);
				PathFinder.InexistentPathList.Add(path.Content);
			}
			finally
			{
				PathFinder.PathListLock.ExitWriteLock();
			}
		}

		private void OnPathListCopyClick(object? sender, RoutedEventArgs e)
		{
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			Clipboard.SetText(((PathObject)currentSelected).Content);
		}

		private void OnPathListMouseDoubleClick(object? sender, MouseButtonEventArgs e)
		{
			object selected = PathList.SelectedItem;
			if (selected is not PathObject)
				return;

			var i = (PathObject)selected;
			if (i != null)
			{
				Logger.Info(CultureInfo.CurrentCulture, I18n.Main_PathSubmitted, i.Content);
				SendMessage(i.Content);
			}
		}

		private void OnSettingsClick(object? sender, RoutedEventArgs e)
		{
			if (CurrentConfig != null)
				new ConfigWindow(CurrentConfig).Show();
		}

		private void OnSubmitURLClick(object? sender, RoutedEventArgs e)
		{
			Browser.Load(CurrentURL.Text);
			Browser.FrameLoadEnd += OnBrowserFrameLoadEnd;
		}

		private void OnWindowClose(object? sender, CancelEventArgs e)
		{
			Logger.Info(I18n.Main_ClosingDBConnection);
			Database.Dispose();
			LogManager.Shutdown();
		}

		private void OnPathUpdated(object? sender, UpdatedPathEventArgs args)
		{
			Logger.Info(I18n.Main_PathUpdateReceived);

			if (!CheckPathIsValid(args.Word, args.MissionChar, PathFinderOptions.None))
				return;

			bool autoEnter = CurrentConfig.RequireNotNull().AutoEnterEnabled && !args.Flags.HasFlag(PathFinderOptions.ManualSearch);

			if (args.Result == PathFinderResult.None && !args.Flags.HasFlag(PathFinderOptions.ManualSearch))
				this.ChangeStatusBar(CurrentStatus.NotFound);
			else if (args.Result == PathFinderResult.Error)
				this.ChangeStatusBar(CurrentStatus.Error);
			else if (!autoEnter)
				this.ChangeStatusBar(CurrentStatus.Normal);

			Task.Run(() => SetSearchState(args));

			Dispatcher.Invoke(() => PathList.ItemsSource = PathFinder.DisplayList);

			if (autoEnter)
			{
				if (args.Result == PathFinderResult.None)
				{
					Logger.Warn(I18n.Auto_NoMorePathAvailable);
					this.ChangeStatusBar(CurrentStatus.NotFound);
				}
				else
				{
					string? content = TimeFilterQualifiedWordList(PathFinder.QualifiedList);
					if (content == null)
					{
						Logger.Warn(I18n.Auto_TimeOver);
						this.ChangeStatusBar(CurrentStatus.NotFound);
					}
					else
					{
						DelayedEnter(content, args);
					}
				}
			}
		}

		private string? TimeFilterQualifiedWordList(IList<PathObject> qualifiedWordList)
		{
			if (CurrentConfig!.DelayPerCharEnabled)
			{
				int remain = Math.Max(300, Handler!.TurnTimeMillis);
				int delay = CurrentConfig.DelayInMillis;
				string? word = qualifiedWordList.FirstOrDefault(po => po!.Content.Length * delay <= remain, null)?.Content;
				if (word == null)
					Logger.Debug(CultureInfo.CurrentCulture, I18n.TimeFilter_TimeOver, remain);
				else
					Logger.Debug(CultureInfo.CurrentCulture, I18n.TimeFilter_Success, remain, word.Length * delay);
				return word;
			}

			return qualifiedWordList[0].Content;
		}

		private string? TimeFilterQualifiedWordListIndexed(IList<PathObject> qualifiedWordList, int wordIndex)
		{
			if (CurrentConfig!.DelayPerCharEnabled)
			{
				int remain = Math.Max(300, Handler!.TurnTimeMillis);
				int delay = CurrentConfig.DelayInMillis;
				PathObject[] arr = qualifiedWordList.Where(po => po!.Content.Length * delay <= remain).ToArray();
				string? word = (arr.Length - 1 >= wordIndex) ? arr[wordIndex].Content : null;
				if (word == null)
					Logger.Debug(CultureInfo.CurrentCulture, I18n.TimeFilter_TimeOver, remain);
				else
					Logger.Debug(CultureInfo.CurrentCulture, I18n.TimeFilter_Success, remain, word.Length * delay);
				return word;
			}

			return qualifiedWordList.Count - 1 >= WordIndex ? qualifiedWordList[wordIndex].Content : null;
		}

		private void DelayedEnter(string content, UpdatedPathEventArgs? args, string? pathAttribute = null)
		{
			if (pathAttribute == null)
				pathAttribute = I18n.Main_Optimal;

			if (CurrentConfig.RequireNotNull().DelayEnabled && args?.Flags.HasFlag(PathFinderOptions.AutoFixed) != true)
			{
				int delay = CurrentConfig.DelayInMillis;
				if (CurrentConfig.DelayPerCharEnabled)
					delay *= content.Length;
				this.ChangeStatusBar(CurrentStatus.Delaying, delay);
				Logger.Debug(CultureInfo.CurrentCulture, I18n.Main_WaitingSubmit, delay);
				if (CurrentConfig.DelayStartAfterCharEnterEnabled)
				{
					// Delay-per-Char
					Task.Run(async () =>
					{
						while (inputStopwatch.ElapsedMilliseconds <= delay)
							await Task.Delay(1);

						PerformAutoEnter(content, args, pathAttribute);
					});
				}
				else
				{
					// Delay
					Task.Run(async () =>
					{
						await Task.Delay(delay);
						PerformAutoEnter(content, args, pathAttribute);
					});
				}
			}
			else
			{
				// Enter immediately
				PerformAutoEnter(content, args);
			}
		}

		private void PerformAutoEnter(string content, UpdatedPathEventArgs? args, string? pathAttribute = null)
		{
			if (pathAttribute == null)
				pathAttribute = I18n.Main_Optimal;

			if (!Handler.RequireNotNull().IsGameStarted || !Handler.IsMyTurn || args != null && !CheckPathIsValid(args.Word, args.MissionChar, PathFinderOptions.AutoFixed))
				return;
			PerformAutoEnter(content, pathAttribute);
		}

		private void PerformAutoEnter(string content, string pathAttribute)
		{
			Logger.Info(CultureInfo.CurrentCulture, I18n.Main_AutoEnter, pathAttribute, content);
			SendMessage(content);
			this.ChangeStatusBar(CurrentStatus.AutoEntered, content);
		}

		private void RegisterHandler(CommonHandler handler)
		{
			Browser.FrameLoadEnd -= OnBrowserFrameLoadEnd;
			Logger.Info(I18n.Main_CEFFrameLoadEnd);

			handler.OnGameStarted += CommonHandler_GameStart;
			handler.OnGameEnded += CommonHandler_GameEnd;
			handler.OnMyTurn += CommonHandler_MyTurnEvent;
			handler.OnMyTurnEnded += CommonHandler_MyTurnEndEvent;
			handler.OnUnsupportedWordEntered += CommonHandler_onUnsupportedWordEntered;
			handler.OnMyPathIsUnsupported += CommonHandler_MyPathIsUnsupported;
			handler.OnRoundChange += CommonHandler_RoundChangeEvent;
			handler.OnGameModeChange += CommonHandler_GameModeChangeEvent;
			handler.OnTypingWordPresented += CommonHandler_OnTypingWordPresentedEvent;
			handler.StartWatchdog();

			Logger.Info(CultureInfo.CurrentCulture, I18n.Main_UseHandler, handler.GetID());
			RemoveAd();
			Dispatcher.Invoke(() => HideLoadOverlay());
		}

		private void RemoveAd()
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

		private void ResetPathList()
		{
			Logger.Info(I18n.Main_ResetPathList);
			PathFinder.ResetFinalList();
			Dispatcher.Invoke(() => PathList.ItemsSource = PathFinder.DisplayList);
		}

		private void SearchField_KeyDown(object? sender, KeyEventArgs e)
		{
			if (e.Key is Key.Enter or Key.Return)
				SubmitSearch_Click(sender, e);
		}

		private void SendMessage(string message)
		{
			Handler.RequireNotNull().SendMessage(message);
			inputStopwatch.Restart();
		}

		private void SetSearchState(UpdatedPathEventArgs? arg, bool IsEnd = false)
		{
			string Result;
			if (arg == null)
			{
				if (IsEnd)
					Result = I18n.PathFinderUnavailable;
				else
					Result = I18n.PathFinderWaiting;
			}
			else
			{
				Result = CreatePathResultExplain(arg);
			}
			Dispatcher.Invoke(() => SearchResult.Text = Result);
		}

		private static string CreatePathResultExplain(UpdatedPathEventArgs arg)
		{
			string filter = $"'{arg.Word.Content}'";
			if (arg.Word.CanSubstitution)
				filter = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview_Or, filter, $"'{arg.Word.Substitution}'");
			if (!string.IsNullOrWhiteSpace(arg.MissionChar))
				filter = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview_MissionChar, filter, arg.MissionChar);
			string FilterText = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderSearchOverview, filter);
			string SpecialFilterText = "";
			string FindResult;
			string ElapsedTimeText = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderTookTime, arg.Time);
			if (arg.Result == PathFinderResult.Normal)
			{
				FindResult = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderFound, arg.TotalWordCount, arg.CalcWordCount);
			}
			else
			{
				if (arg.Result == PathFinderResult.None)
					FindResult = string.Format(CultureInfo.CurrentCulture, I18n.PathFinderFoundButEmpty, arg.TotalWordCount);
				else
					FindResult = I18n.PathFinderError;
			}
			if (arg.Flags.HasFlag(PathFinderOptions.UseEndWord))
				SpecialFilterText += ", " + I18n.PathFinderEndWord;
			if (arg.Flags.HasFlag(PathFinderOptions.UseAttackWord))
				SpecialFilterText += ", " + I18n.PathFinderAttackWord;

			string newSpecialFilterText = string.IsNullOrWhiteSpace(SpecialFilterText) ? string.Empty : string.Format(CultureInfo.CurrentCulture, I18n.PathFinderIncludedWord, SpecialFilterText[2..]);
			return FilterText + Environment.NewLine + newSpecialFilterText + Environment.NewLine + FindResult + Environment.NewLine + ElapsedTimeText;
		}

		private void StartPathFinding(ResponsePresentedWord? word, string? missionChar, PathFinderOptions flags)
		{
			GameMode mode = CurrentConfig.RequireNotNull().GameMode;
			if (word == null || mode == GameMode.TypingBattle && !flags.HasFlag(PathFinderOptions.ManualSearch))
				return;

			try
			{
				if (!ConfigEnums.IsFreeMode(mode) && GetEndWordList(mode)?.Contains(word.Content) == true && (!word.CanSubstitution || GetEndWordList(mode)?.Contains(word.Substitution!) == true))
				{
					Logger.Warn(I18n.PathFinderFailed_Endword);
					ResetPathList();
					SetSearchState(null, true);
					this.ChangeStatusBar(CurrentStatus.EndWord);
				}
				else
				{
					this.ChangeStatusBar(CurrentStatus.Searching);
					SetupPathFinderInfo(ref flags);

					// Enqueue search
					PathFinder.FindPath(new FindWordInfo
					{
						Word = word,
						MissionChar = CurrentConfig.MissionAutoDetectionEnabled && missionChar != null ? missionChar : "",
						WordPreference = CurrentConfig.ActiveWordPreference,
						Mode = mode,
						PathFinderFlags = flags
					});
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, I18n.PathFinderFailed_Exception);
			}
		}

		private static void SetupPathFinderInfo(ref PathFinderOptions flags)
		{
			// Setup flag
			if (CurrentConfig.RequireNotNull().EndWordEnabled && (flags.HasFlag(PathFinderOptions.ManualSearch) || PathFinder.PreviousPath.Count > 0))  // 첫 턴 한방 방지
				flags |= PathFinderOptions.UseEndWord;
			else
				flags &= ~PathFinderOptions.UseEndWord;
			if (CurrentConfig.AttackWordAllowed)
				flags |= PathFinderOptions.UseAttackWord;
			else
				flags &= ~PathFinderOptions.UseAttackWord;
		}

		private void SubmitChat_Click(object? sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(ChatField.Text))
			{
				SendMessage(ChatField.Text);
				ChatField.Text = "";
			}
		}

		private void SubmitSearch_Click(object? sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(SearchField.Text))
			{
				StartPathFinding(new ResponsePresentedWord(SearchField.Text, false), Handler?.CurrentMissionChar ?? string.Empty, PathFinderOptions.ManualSearch);
				SearchField.Text = "";
			}
		}

		private void UnloadPreviousHandler(CommonHandler handler)
		{
			Logger.Info(CultureInfo.CurrentCulture, I18n.HandlerRegistry_Unregistered, handler.GetID());

			// Unregister previous handler
			handler.OnGameStarted -= CommonHandler_GameStart;
			handler.OnGameEnded -= CommonHandler_GameEnd;
			handler.OnMyTurn -= CommonHandler_MyTurnEvent;
			handler.OnMyTurnEnded -= CommonHandler_MyTurnEndEvent;
			handler.OnUnsupportedWordEntered -= CommonHandler_onUnsupportedWordEntered;
			handler.OnMyPathIsUnsupported -= CommonHandler_MyPathIsUnsupported;
			handler.OnRoundChange -= CommonHandler_RoundChangeEvent;
			handler.OnGameModeChange -= CommonHandler_GameModeChangeEvent;
			handler.OnTypingWordPresented -= CommonHandler_OnTypingWordPresentedEvent;
			handler.StopWatchdog();
		}
	}
}
