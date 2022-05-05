using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Utils;
using CefSharp;
using CefSharp.Wpf;
using log4net;
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

namespace AutoKkutu
{
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

		private const string PATHFINDER_ERROR = "오류가 발생하여 단어 검색 실패.";

		private const string PATHFINDER_UNAVAILABLE = "이 턴에 사용 가능한 단어 없음.";

		private const string PATHFINDER_WAITING = "단어 검색 대기중.";

		// Succeed KKutu-Helper Release v5.6.8500
		private const string TITLE = "AutoKkutu - Improved KKutu-Helper";

		public static readonly ILog Logger = LogManager.GetLogger("MainThread");
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

			Logger.Info("Starting Load Page...");
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
				Logger.InfoFormat("{0} initialization took {1}ms.", nameof(PathFinder), watch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				Logger.Error("Error loading database.config", ex);
				Environment.Exit(1);
			}
		}

		public static void UpdateColorPreference(AutoKkutuColorPreference newColorPref)
		{
			Logger.Info("Updated color preference.");
			CurrentColorPreference = newColorPref;
			PathFinder.UpdateColorPreference(newColorPref);
		}

		public static void UpdateConfig(AutoKkutuConfiguration newConfig)
		{
			Logger.Info("Updated config.");
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
				Properties.Settings config = Properties.Settings.Default;
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
				Logger.Error(ex);
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
				Logger.WarnFormat("Invalidated Incorrect Path Update Result. (Word: {0}, MissionChar: {1})", differentWord, differentMissionChar);
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
				Logger.Warn("Can't submit clipboard text", ex);
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
			if (CurrentConfig.RequireNotNull().AutoDBUpdateMode == AutoDBUpdateMode.OnGameEnd)
			{
				this.ChangeStatusBar(CurrentStatus.DatabaseIntegrityCheck, "자동 업데이트");
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
							Logger.Warn("Failed to write game result", ex);
						}
					}

					this.ChangeStatusBar(CurrentStatus.DatabaseIntegrityCheckDone, "자동 업데이트", result);
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
				Logger.InfoFormat("Automatically updated game mode to '{0}'", ConfigEnums.GetGameModeName(newGameMode));
				UpdateConfig(CurrentConfig);
			}
		}

		private void CommonHandler_GameStart(object? sender, EventArgs e)
		{
			this.ChangeStatusBar(CurrentStatus.Normal);
			WordIndex = 0;
			inputStopwatch.Restart();
		}

		// TODO: 오답 수정 딜레이
		private void CommonHandler_MyPathIsUnsupported(object? sender, UnsupportedWordEventArgs args)
		{
			string word = args.Word;
			Logger.WarnFormat("My path '{0}' is wrong.", word);

			if (!CurrentConfig.RequireNotNull().AutoFixEnabled)
				return;

			IList<PathObject> localFinalList = PathFinder.FinalList!;
			if (localFinalList.Count - 1 <= WordIndex)
			{
				Logger.Warn("Can't Find any other path.");
				this.ChangeStatusBar(CurrentStatus.NotFound);
				return;
			}

			if (CurrentConfig.AutoEnterEnabled)
			{
				try
				{
					WordIndex++;
					string path = localFinalList[WordIndex].Content;
					if (CurrentConfig.FixDelayEnabled)
					{
						int delay = CurrentConfig.FixDelayInMillis;
						if (CurrentConfig.FixDelayPerCharEnabled)
							delay *= path.Length;
						this.ChangeStatusBar(CurrentStatus.Delaying, delay);
						Logger.DebugFormat("Waiting {0}ms before entering next path.", delay);
						Task.Run(async () =>
						{
							await Task.Delay(delay);
							PerformAutoEnter(path, "next");
						});
					}
					else
					{
						PerformAutoEnter(path, "next");
					}
				}
				catch (Exception ex)
				{
					Logger.Error("Can't execute path due exception", ex);
				}
			}
		}

		private void CommonHandler_MyTurnEndEvent(object? sender, EventArgs e)
		{
			Logger.Debug("Reset WordIndex to zero.");
			WordIndex = 0;
		}

		private void CommonHandler_MyTurnEvent(object? sender, WordPresentEventArgs args) => StartPathFinding(args.Word, args.MissionChar, PathFinderOptions.None);

		private void CommonHandler_onUnsupportedWordEntered(object? sender, UnsupportedWordEventArgs args)
		{
			bool isInexistent = !args.IsExistingWord;
			string theWord = args.Word;
			if (isInexistent)
				Logger.WarnFormat("Entered word '{0}' is inexistent; Added to removal list.", theWord);
			else
				Logger.WarnFormat("Entered word '{0}' is exists but not supported; Added to unsupported list.", theWord);
			PathFinder.AddToUnsupportedWord(theWord, isInexistent);
		}

		private void CommonHandler_RoundChangeEvent(object? sender, EventArgs e)
		{
			if (CurrentConfig.RequireNotNull().AutoDBUpdateMode == AutoDBUpdateMode.OnRoundEnd)
				PathFinder.AutoDBUpdate();
		}

		private void CommonHandler_WordPresentedEvent(object? sender, WordPresentEventArgs args)
		{
			string word = args.Word.Content;

			Handler.RequireNotNull();
			if (!CurrentConfig.RequireNotNull().AutoEnterEnabled)
				return;

			DelayedEnter(word, null);
		}

		private static ICollection<string>? GetEndWordList(GameMode mode) => mode switch
		{
			GameMode.FirstAndLast => PathFinder.ReverseEndWordList,
			GameMode.Kkutu => PathFinder.KkutuEndWordList,
			_ => PathFinder.EndWordList,
		};

		private void HideLoadOverlay()
		{
			Logger.Info("Hide LoadOverlay.");

			var img = new BitmapImage();
			img.BeginInit();
			img.UriSource = new Uri($@"Images\{Database.GetDBType()}.png", UriKind.Relative);
			img.EndInit();
			DBLogo.Source = img;

			LoadOverlay.Visibility = Visibility.Hidden;
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
				Logger.WarnFormat("Unsupported site: {0}", url);
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
			foreach (MenuItem item in contextMenu.Items)
			{
				bool available = false;
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
				}
				item.IsEnabled = available;
			}
		}

		private void OnPathListMakeAttackClick(object? sender, RoutedEventArgs e)
		{
			Logger.Info(nameof(OnPathListMakeAttackClick));
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeAttack(CurrentConfig.RequireNotNull().GameMode, Database.DefaultConnection);
		}

		private void OnPathListMakeEndClick(object? sender, RoutedEventArgs e)
		{
			Logger.Info(nameof(OnPathListMakeEndClick));
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeEnd(CurrentConfig.RequireNotNull().GameMode, Database.DefaultConnection);
		}

		private void OnPathListMakeNormalClick(object? sender, RoutedEventArgs e)
		{
			Logger.Info(nameof(OnPathListMakeNormalClick));
			object currentSelected = PathList.SelectedItem;
			if (currentSelected is not PathObject)
				return;
			((PathObject)currentSelected).MakeNormal(CurrentConfig.RequireNotNull().GameMode, Database.DefaultConnection);
		}

		private void OnPathListMouseDoubleClick(object? sender, MouseButtonEventArgs e)
		{
			object selected = PathList.SelectedItem;
			if (selected is not PathObject)
				return;

			var i = (PathObject)selected;
			if (i != null)
			{
				Logger.InfoFormat("Executed Path : '{0}'.", i.Content);
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
			Logger.Info("Closing database connection...");
			Database.Dispose();
		}

		private void OnPathUpdated(object? sender, UpdatedPathEventArgs args)
		{
			Logger.Info("Path update received.");

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

			Dispatcher.Invoke(() => PathList.ItemsSource = PathFinder.FinalList);

			if (autoEnter)
			{
				if (args.Result == PathFinderResult.None)
				{
					Logger.Warn("Auto mode enabled. but can't find any path.");
					this.ChangeStatusBar(CurrentStatus.NotFound);
				}
				else
				{
					string content = PathFinder.FinalList![0].Content;
					DelayedEnter(content, args);
				}
			}
		}

		//private void DelayedTypingBattleEnter(string word)
		//{
		//	Handler.RequireNotNull();
		//	if (CurrentConfig.RequireNotNull().DelayEnabled)
		//	{
		//		int delay = CurrentConfig.DelayInMillis;
		//		if (CurrentConfig.DelayPerCharEnabled)
		//			delay *= word.Length;
		//		this.ChangeStatusBar(CurrentStatus.Delaying, delay);
		//		Logger.DebugFormat("Waiting {0}ms before entering path.", delay);
		//		if (CurrentConfig.DelayStartAfterCharEnterEnabled)
		//		{
		//			Task.Run(async () =>
		//			{
		//				while (inputStopwatch.ElapsedMilliseconds <= delay)
		//					await Task.Delay(1);
		//				if (Handler.IsMyTurn)
		//				{
		//					SendMessage(word);
		//					Logger.InfoFormat("Entered word (typing battle): '{0}'", word);
		//				}
		//			});
		//		}
		//		else
		//		{
		//			Task.Run(async () =>
		//			{
		//				await Task.Delay(delay);
		//				if (Handler.IsMyTurn)
		//				{
		//					SendMessage(word);
		//					Logger.InfoFormat("Entered word (typing battle): '{0}'", word);
		//				}
		//			});
		//		}
		//	}
		//	else
		//	{
		//		SendMessage(word);
		//		Logger.InfoFormat("Entered word (typing battle): '{0}'", word);
		//	}
		//}

		private void DelayedEnter(string content, UpdatedPathEventArgs? args)
		{
			if (CurrentConfig.RequireNotNull().DelayEnabled && args?.Flags.HasFlag(PathFinderOptions.AutoFixed) != true)
			{
				int delay = CurrentConfig.DelayInMillis;
				if (CurrentConfig.DelayPerCharEnabled)
					delay *= content.Length;
				this.ChangeStatusBar(CurrentStatus.Delaying, delay);
				Logger.DebugFormat("Waiting {0}ms before entering path.", delay);
				if (CurrentConfig.DelayStartAfterCharEnterEnabled)
				{
					// Delay-per-Char
					Task.Run(async () =>
					{
						while (inputStopwatch.ElapsedMilliseconds <= delay)
							await Task.Delay(1);

						PerformAutoEnter(content, args);
					});
				}
				else
				{
					// Delay
					Task.Run(async () =>
					{
						await Task.Delay(delay);
						PerformAutoEnter(content, args);
					});
				}
			}
			else
			{
				// Enter immediately
				PerformAutoEnter(content, args);
			}
		}

		private void PerformAutoEnter(string content, UpdatedPathEventArgs? args)
		{
			if (!Handler.RequireNotNull().IsGameStarted || !Handler.IsMyTurn || args != null && !CheckPathIsValid(args.Word, args.MissionChar, PathFinderOptions.AutoFixed))
				return;
			PerformAutoEnter(content);
		}

		private void PerformAutoEnter(string content, string pathAttribute = "first")
		{
			Logger.InfoFormat("Auto mode enabled. automatically use path: '{0}'", pathAttribute);
			SendMessage(content);
			this.ChangeStatusBar(CurrentStatus.AutoEntered, content);
		}

		private void RegisterHandler(CommonHandler handler)
		{
			Browser.FrameLoadEnd -= OnBrowserFrameLoadEnd;
			Logger.Info("Browser frame-load end.");

			handler.OnGameStarted += CommonHandler_GameStart;
			handler.OnGameEnded += CommonHandler_GameEnd;
			handler.OnMyTurn += CommonHandler_MyTurnEvent;
			handler.OnMyTurnEnded += CommonHandler_MyTurnEndEvent;
			handler.OnUnsupportedWordEntered += CommonHandler_onUnsupportedWordEntered;
			handler.OnMyPathIsUnsupported += CommonHandler_MyPathIsUnsupported;
			handler.OnRoundChange += CommonHandler_RoundChangeEvent;
			handler.OnGameModeChange += CommonHandler_GameModeChangeEvent;
			handler.OnWordPresented += CommonHandler_WordPresentedEvent;
			handler.StartWatchdog();

			Logger.InfoFormat("Using handler: {0}", handler.GetID());
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
			Logger.Info("Reset Path list... ");
			PathFinder.ResetFinalList();
			Dispatcher.Invoke(() => PathList.ItemsSource = PathFinder.FinalList);
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
					Result = PATHFINDER_UNAVAILABLE;
				else
					Result = PATHFINDER_WAITING;
			}
			else
			{
				Result = CreatePathResultExplain(arg);
			}
			Dispatcher.Invoke(() => SearchResult.Text = Result);
		}

		private static string CreatePathResultExplain(UpdatedPathEventArgs arg)
		{
			string FilterText = $"'{arg.Word.Content}'{(arg.Word.CanSubstitution ? $" 또는 '{arg.Word.Substitution}'" : string.Empty)}{(string.IsNullOrWhiteSpace(arg.MissionChar) ? string.Empty : $", 미션 단어 '{arg.MissionChar}'")} 에 대한 검색 결과";
			string SpecialFilterText = "";
			string FindResult;
			string ElapsedTimeText = $"{arg.Time}ms 소요.";
			if (arg.Result == PathFinderResult.Normal)
			{
				FindResult = $"총 {arg.TotalWordCount}개의 단어 중, {arg.CalcWordCount}개의 단어 추천됨.";
			}
			else
			{
				if (arg.Result == PathFinderResult.None)
					FindResult = $"총 {arg.TotalWordCount}개의 단어 중, 가능한 것 없음.";
				else
					FindResult = PATHFINDER_ERROR;
			}
			if (arg.Flags.HasFlag(PathFinderOptions.UseEndWord))
				SpecialFilterText += ", 한방";
			if (arg.Flags.HasFlag(PathFinderOptions.UseAttackWord))
				SpecialFilterText += ", 공격";

			string newSpecialFilterText = string.IsNullOrWhiteSpace(SpecialFilterText) ? string.Empty : $"{SpecialFilterText[2..]} 단어 사용";
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
					Logger.Warn("Can't Find any path : Presented word is End word.");
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
				Logger.Error("Can't find Path due exception", ex);
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

		// TODO: 검색 결과 (추천 목록) 맨 윗쪽이나 아랫쪽(걸린 시간 표시되는 곳)에 '무엇에 대한 검색 결과인지'까지 나타내도록 개선
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
			Logger.InfoFormat("Unregistered previous handler: {0}", handler.GetID());

			// Unregister previous handler
			handler.OnGameStarted -= CommonHandler_GameStart;
			handler.OnGameEnded -= CommonHandler_GameEnd;
			handler.OnMyTurn -= CommonHandler_MyTurnEvent;
			handler.OnMyTurnEnded -= CommonHandler_MyTurnEndEvent;
			handler.OnUnsupportedWordEntered -= CommonHandler_onUnsupportedWordEntered;
			handler.OnMyPathIsUnsupported -= CommonHandler_MyPathIsUnsupported;
			handler.OnRoundChange -= CommonHandler_RoundChangeEvent;
			handler.OnGameModeChange -= CommonHandler_GameModeChangeEvent;
			handler.OnWordPresented -= CommonHandler_WordPresentedEvent;
			handler.StopWatchdog();
		}
	}
}
