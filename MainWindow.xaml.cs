using AutoKkutu.Handlers;
using CefSharp;
using CefSharp.Handler;
using CefSharp.Wpf;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static AutoKkutu.CommonHandler;

namespace AutoKkutu
{
	// 한 게임이 끝나면 하는 '자동 결과 저장(AutoDBUpdate)' 기능이 일어나는 시점을 조정 가능할 수 있게(게임 끝났을 때, 라운드 끝났을 때, 고정적인 일정 시간마다, 매 턴마다 등등...) 콤보박스 등으로
	public partial class MainWindow : Window
	{
		// Succeed KKutu-Helper Release v5.6.8500
		private const string TITLE = "AutoKkutu - Improved KKutu-Helper";
		public const string VERSION = "1.0.0000";
		private const string MAINTHREAD_NAME = "MainThread";
		private const string INPUT_TEXT_PLACEHOLDER = "여기에 텍스트를 입력해주세요";
		private const string PATHFINDER_WAITING = "단어 검색 대기중.";
		private const string PATHFINDER_ERROR = "오류가 발생하여 단어 검색 실패.";
		private const string PATHFINDER_UNAVAILABLE = "이 턴에 사용 가능한 단어 없음.";

		private static ILog Logger = LogManager.GetLogger("MainThread");

		public static ChromiumWebBrowser browser;

		public static string LastUsedPath = "";

		//private static bool _pathSelected;

		private int WordIndex = 0;

		public CommonHandler Handler;

		public static Config CurrentConfig;

		private Stopwatch inputStopwatch = new Stopwatch();

		private enum CurrentStatus
		{
			Normal,
			Searching,
			AutoEntered,
			NotFound,
			Error,
			EndWord,
			Wait,
			DB_Job,
			DB_Job_Done,
			Adding_Words,
			Adding_Words_Done,
			Delaying
		}

		public MainWindow()
		{
			// Initialize CEF
			Cef.Initialize(new CefSettings
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
			}, true, (IApp)null);

			// Load default config
			PathFinder.UpdateConfig(CurrentConfig = new Config());
			CommonHandler.UpdateConfig(CurrentConfig);

			// Initialize Browser
			browser = new ChromiumWebBrowser
			{
				Address = "https://kkutu.pink",
				UseLayoutRounding = true
			};

			// Visual components setup
			InitializeComponent();
			ConsoleManager.Show();
			Title = TITLE;
			VersionLabel.Content = "v1.0";

			CommonHandler.InitHandlers(browser);
			Logger.Info("Starting Load Page...");
			LoadOverlay.Visibility = Visibility.Visible;
			SubmitWord.Text = INPUT_TEXT_PLACEHOLDER;
			SubmitWord.FontStyle = FontStyles.Italic;
			ChangeStatusBar(CurrentStatus.Wait);
			SetSearchState(null, false);
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
			browser.FrameLoadEnd += Browser_FrameLoadEnd_RunOnce;
			browserContainer.Content = browser;
			DatabaseManager.DBError = (EventHandler)Delegate.Combine(DatabaseManager.DBError, new EventHandler(DatabaseManager_DBError));
			DatabaseManager.Init();
			PathFinder.Init();
		}

		public static void UpdateConfig(Config newConfig)
		{
			Logger.Info("Updated config.");
			CurrentConfig = newConfig;
			PathFinder.UpdateConfig(newConfig);
			CommonHandler.UpdateConfig(newConfig);
		}

		private void DatabaseManager_DBError(object sender, EventArgs e) => ChangeStatusBar(CurrentStatus.Error);

		private void Browser_FrameLoadEnd_RunOnce(object sender, FrameLoadEndEventArgs e)
		{
			browser.FrameLoadEnd -= Browser_FrameLoadEnd_RunOnce;

			PathFinder.UpdatedPath += PathFinder_UpdatedPath;
			DatabaseManager.DBJobStart += DatabaseManager_DBJobStart;
			DatabaseManager.DBJobDone += DatabaseManager_DBJobDone;
			DatabaseManagement.AddWordStart += DatabaseManagement_AddWordStart;
			DatabaseManagement.AddWordDone += DatabaseManagement_AddWordDone;
		}

		private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
		{
			if (Handler != null)
			{
				Logger.InfoFormat("Unregistered previous handler: {0}", Handler.GetID());

				// Unregister previous handler
				Handler.onGameStarted -= CommonHandler_GameStart;
				Handler.onGameEnded -= CommonHandler_GameEnd;
				Handler.onMyTurn -= CommonHandler_MyTurnEvent;
				Handler.onMyTurnEnded -= CommonHandler_MyTurnEndEvent;
				Handler.onUnsupportedWordEntered -= CommonHandler_onUnsupportedWordEntered;
				Handler.onMyPathIsUnsupported -= CommonHandler_MyPathIsUnsupported;
				Handler.onRoundChange -= CommonHandler_RoundChangeEvent;
				Handler.onGameModeChange -= CommonHandler_GameModeChangeEvent;
				Handler.onWordPresented -= CommonHandler_WordPresentedEvent;
				Handler.StopWatchdog();
				Handler = null;
			}

			// Initialize handler and Register event handlers
			string url = e.Url;
			Handler = CommonHandler.getHandler(url);
			if (Handler != null)
			{
				browser.FrameLoadEnd -= Browser_FrameLoadEnd;
				Logger.Info("Browser frame-load end.");

				Handler.onGameStarted += CommonHandler_GameStart;
				Handler.onGameEnded += CommonHandler_GameEnd;
				Handler.onMyTurn += CommonHandler_MyTurnEvent;
				Handler.onMyTurnEnded += CommonHandler_MyTurnEndEvent;
				Handler.onUnsupportedWordEntered += CommonHandler_onUnsupportedWordEntered;
				Handler.onMyPathIsUnsupported += CommonHandler_MyPathIsUnsupported;
				Handler.onRoundChange += CommonHandler_RoundChangeEvent;
				Handler.onGameModeChange += CommonHandler_GameModeChangeEvent;
				Handler.onWordPresented += CommonHandler_WordPresentedEvent;
				Handler.StartWatchdog();

				Logger.InfoFormat("Using handler: {0}", Handler.GetID());
				RemoveAd();
				Dispatcher.Invoke(() =>
				{
					Logger.Info("Hide LoadOverlay.");
					DBStatus.Content = DatabaseManager.GetDBInfo();
					LoadOverlay.Visibility = Visibility.Hidden;
				});
			}
			else
				Logger.WarnFormat($"Unsupported site: {0}", url);
		}

		private void DatabaseManager_DBJobStart(object sender, EventArgs e)
		{
			ChangeStatusBar(CurrentStatus.DB_Job, ((DatabaseManager.DBJobArgs)e).JobName);
		}

		private void DatabaseManager_DBJobDone(object sender, EventArgs e)
		{
			var args = ((DatabaseManager.DBJobArgs)e);
			ChangeStatusBar(CurrentStatus.DB_Job_Done, args.JobName, args.Result);
		}

		private void DatabaseManagement_AddWordStart(object sender, EventArgs e)
		{
			ChangeStatusBar(CurrentStatus.Adding_Words);
		}

		private void DatabaseManagement_AddWordDone(object sender, EventArgs e)
		{
			ChangeStatusBar(CurrentStatus.Adding_Words_Done);
		}

		private void SetSearchState(PathFinder.UpdatedPathEventArgs arg, bool IsEnd = false)
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
				if (arg.Result == PathFinder.FindResult.Normal)
				{
					Result = $"총 {arg.TotalWordCount}개의 단어 중, {arg.CalcWordCount}개의 단어 추천됨.{Environment.NewLine}{arg.Time}ms 소요.";
					if (arg.Flags.HasFlag(PathFinderFlags.USING_END_WORD))
						Result += " (한방 단어 사용)";
				}
				else
				{
					if (arg.Result == PathFinder.FindResult.None)
					{
						Result = $"총 {arg.TotalWordCount}개의 단어 중, 가능한 것 없음.{Environment.NewLine}{arg.Time}ms 소요.";
						if (arg.Flags.HasFlag(PathFinderFlags.USING_END_WORD))
							Result += " (한방 단어 사용)";
					}
					else
						Result = PATHFINDER_ERROR;
				}
			}
			Dispatcher.Invoke(() => SearchResult.Text = Result);
		}

		private void PathFinder_UpdatedPath(object sender, PathFinder.UpdatedPathEventArgs args)
		{
			Logger.Info("Path update received.");

			if (!CheckPathIsValid(args, PathFinderFlags.NONE))
				return;

			if (args.Result == PathFinder.FindResult.None)
				ChangeStatusBar(CurrentStatus.NotFound);
			else if (args.Result == PathFinder.FindResult.Error)
				ChangeStatusBar(CurrentStatus.Error);

			Task.Run(() => SetSearchState(args));

			Dispatcher.Invoke(() =>
			{
				PathList.ItemsSource = PathFinder.FinalList;
			});

			//_pathSelected = false;
			if (CurrentConfig.AutoEnter)
			{
				if (args.Result == PathFinder.FindResult.None)
				{
					Logger.Warn("Auto mode enabled. but can't find any path.");
					ChangeStatusBar(CurrentStatus.NotFound);
				}
				else
				{
					string content = PathFinder.FinalList.First().Content;
					if (CurrentConfig.Delay && !args.Flags.HasFlag(PathFinderFlags.RETRIAL))
					{
						int delay = CurrentConfig.nDelay;
						if (CurrentConfig.DelayPerWord)
							delay *= content.Length;
						ChangeStatusBar(CurrentStatus.Delaying, delay);
						Logger.DebugFormat("Waiting {0}ms before entering path.", delay);
						if (CurrentConfig.DelayStartAfterWordEnter)
							Task.Run(async () =>
							{
								while (inputStopwatch.ElapsedMilliseconds <= delay)
									await Task.Delay(1);

								performAutoEnter(args, content);
							});
						else
							Task.Run(async () =>
							{
								await Task.Delay(delay);
								performAutoEnter(args, content);
							});
					}
					else
						performAutoEnter(args, content);
				}
			}
			else
				ChangeStatusBar(CurrentStatus.Normal);
		}

		private bool CheckPathIsValid(PathFinder.UpdatedPathEventArgs args, PathFinderFlags flags)
		{
			bool differentWord = Handler.CurrentPresentedWord != null && !args.Word.Equals(Handler.CurrentPresentedWord);
			bool differentMissionChar = !string.IsNullOrWhiteSpace(Handler.CurrentMissionChar) && !string.Equals(args.MissionChar, Handler.CurrentMissionChar, StringComparison.InvariantCultureIgnoreCase);
			if (Handler.IsMyTurn && (differentWord || differentMissionChar))
			{
				Logger.WarnFormat("Invalidated Incorrect Path Update Result. (Word: {0}, MissionChar: {1})", differentWord, differentMissionChar);
				StartPathFinding(Handler.CurrentPresentedWord, Handler.CurrentMissionChar, flags);
				return false;
			}
			return true;
		}

		private void performAutoEnter(PathFinder.UpdatedPathEventArgs args, string content)
		{
			if (!Handler.IsGameStarted || !Handler.IsMyTurn || !CheckPathIsValid(args, PathFinderFlags.RETRIAL))
				return;
			Logger.Info("Auto mode enabled. automatically use first path.");
			Logger.InfoFormat("Execute Path : {0}", content);
			LastUsedPath = content;
			//_pathSelected = true;
			SendMessage(content);
			ChangeStatusBar(CurrentStatus.AutoEntered, content);
		}

		private void ResetPathList()
		{
			Logger.Info("Reset Path list... ");
			PathFinder.FinalList = new List<PathFinder.PathObject>();
			Dispatcher.Invoke(() => PathList.ItemsSource = PathFinder.FinalList);
		}

		private void CommonHandler_MyTurnEndEvent(object sender, EventArgs e)
		{
			Logger.Debug("Reset WordIndex to zero.");
			WordIndex = 0;
		}

		private void CommonHandler_MyTurnEvent(object sender, CommonHandler.WordPresentEventArgs args)
		{
			StartPathFinding(args.Word, args.MissionChar, PathFinderFlags.NONE);
		}

		private void StartPathFinding(CommonHandler.ResponsePresentedWord word, string missionChar, PathFinderFlags flags)
		{
			if (CurrentConfig.Mode == GameMode.Typing_Battle)
				return;

			try
			{
				if (!ConfigEnums.IsFreeMode(CurrentConfig.Mode) && PathFinder.EndWordList.Contains(word.Content) && (!word.CanSubstitution || PathFinder.EndWordList.Contains(word.Substitution)))
				{
					Logger.Warn("Can't Find any path : Presented word is End word.");
					ResetPathList();
					SetSearchState(null, true);
					ChangeStatusBar(CurrentStatus.EndWord);
				}
				else
				{
					ChangeStatusBar(CurrentStatus.Searching);
					if (CurrentConfig.UseEndWord && PathFinder.PreviousPath.Count > 0)  // 첫 턴 한방 방지
						flags |= PathFinderFlags.USING_END_WORD;
					else
						flags &= ~PathFinderFlags.USING_END_WORD;
					PathFinder.FindPath(word, CurrentConfig.MissionDetection ? missionChar : "", CurrentConfig.WordPreference, CurrentConfig.Mode, flags);
				}
			}
			catch (Exception ex)
			{
				Logger.Error("Can't find Path due exception", ex);
			}
		}

		private void CommonHandler_onUnsupportedWordEntered(object sender, CommonHandler.UnsupportedWordEventArgs args)
		{
			bool isInexistent = !args.IsExistingWord;
			string theWord = args.Word;
			if (isInexistent)
				Logger.WarnFormat("Entered word '{0}' is inexistent; Added to removal list.", theWord);
			else
				Logger.WarnFormat("Entered word '{0}' is exists but not supported; Added to unsupported list.", theWord);
			PathFinder.AddToUnsupportedWord(theWord, isInexistent);
		}

		private void CommonHandler_MyPathIsUnsupported(object sender, CommonHandler.UnsupportedWordEventArgs args)
		{
			var word = args.Word;
			Logger.WarnFormat("My path '{0}' is wrong.", word);

			if (!CurrentConfig.AutoFix)
				return;

			List<PathFinder.PathObject> localFinalList = PathFinder.FinalList;
			if (localFinalList.Count - 1 <= WordIndex)
			{
				Logger.Warn("Can't Find any other path.");
				ChangeStatusBar(CurrentStatus.NotFound);
				return;
			}

			//_pathSelected = false;
			if (CurrentConfig.AutoEnter)
			{
				try
				{
					WordIndex++;
					string path = localFinalList[WordIndex].Content;
					Logger.InfoFormat("Auto mode enabled. automatically use next path (index {0}).", WordIndex);
					Logger.InfoFormat("Execute Path: {0}", path);
					LastUsedPath = path;
					//_pathSelected = true;
					SendMessage(path);
				}
				catch (Exception ex)
				{
					Logger.Error("Can't execute path due exception", ex);
				}
			}
		}

		private void CommonHandler_GameStart(object sender, EventArgs e)
		{
			ChangeStatusBar(CurrentStatus.Normal);
			WordIndex = 0;
			inputStopwatch.Restart();
		}

		private void CommonHandler_GameEnd(object sender, EventArgs e)
		{
			SetSearchState(null, false);
			ResetPathList();
			if (CurrentConfig.AutoDBUpdateMode == DBAutoUpdateMode.GAME_END)
			{
				ChangeStatusBar(CurrentStatus.DB_Job, "자동 업데이트");
				string result = PathFinder.AutoDBUpdate();
				if (string.IsNullOrEmpty(result))
					ChangeStatusBar(CurrentStatus.Wait);
				else
				{
					if (new FileInfo("GameResult.txt").Exists)
						try
						{
							File.AppendAllText("GameResult.txt", $"[{Handler.GetRoomInfo()}] {result}{Environment.NewLine}");
						}
						catch (Exception ex)
						{
							Logger.Warn("Failed to write game result", ex);
						}

					ChangeStatusBar(CurrentStatus.DB_Job_Done, "자동 업데이트", result);
				}
			}
			else
				ChangeStatusBar(CurrentStatus.Wait);
		}

		private void CommonHandler_RoundChangeEvent(object sender, EventArgs e)
		{
			if (CurrentConfig.AutoDBUpdateMode == DBAutoUpdateMode.ROUND_END)
				PathFinder.AutoDBUpdate();
		}

		private void CommonHandler_GameModeChangeEvent(object sender, GameModeChangeEventArgs args)
		{
			if (CurrentConfig.GameModeAutoDetect)
			{
				var newGameMode = args.GameMode;
				CurrentConfig.Mode = newGameMode;
				Logger.InfoFormat("Automatically updated game mode to '{0}'", ConfigEnums.GetGameModeName(newGameMode));
				UpdateConfig(CurrentConfig);
			}
		}
		private void CommonHandler_WordPresentedEvent(object sender, WordPresentEventArgs args)
		{
			string word = args.Word.Content;
			Logger.InfoFormat("Entered word (typing battle): '{0}'", word);
			if (CurrentConfig.Delay)
			{
				int delay = CurrentConfig.nDelay;
				if (CurrentConfig.DelayPerWord)
					delay *= word.Length;
				ChangeStatusBar(CurrentStatus.Delaying, delay);
				Logger.DebugFormat("Waiting {0}ms before entering path.", delay);
				if (CurrentConfig.DelayStartAfterWordEnter)
					Task.Run(async () =>
					{
						while (inputStopwatch.ElapsedMilliseconds <= delay)
							await Task.Delay(1);
						SendMessage(word);
					});
				else
					Task.Run(async () =>
					{
						await Task.Delay(delay);
						SendMessage(word);
					});
			}
			else
				SendMessage(word);
		}

		private void RemoveAd()
		{
			// Kkutu.co.kr
			browser.ExecuteScriptAsyncWhenPageLoaded("document.body.style.overflow ='hidden'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADBox').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADVERTISEMENT').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADVERTISEMENT_TITLE').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementsByClassName('kktukorea__1LZzX_0')[0].style = 'display:none'", false);

			// Kkutu.pink
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('google-center-div')[0].style = 'display:none'", false);

			// Kkutu.org
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementsByClassName('ADBox Product')[0].style = 'display:none'", false);
		}

		private void ChangeStatusBar(CurrentStatus status, params object[] formatterArgs)
		{
			Color StatusColor;
			string StatusContent;
			switch (status)
			{
				case CurrentStatus.Normal:
					StatusColor = Utils.ColorConstants.NormalColor;
					StatusContent = "준비";
					break;
				case CurrentStatus.NotFound:
					StatusColor = Utils.ColorConstants.WarningColor;
					StatusContent = "이 턴에 낼 수 있는 단어를 데이터 집합에서 찾을 수 없었습니다. 수동으로 입력하십시오.";
					break;
				case CurrentStatus.EndWord:
					StatusColor = Utils.ColorConstants.ErrorColor;
					StatusContent = "더 이상 이 턴에 낼 수 있는 단어가 없습니다.";
					break;
				case CurrentStatus.Error:
					StatusColor = Utils.ColorConstants.ErrorColor;
					StatusContent = "프로그램에 오류가 발생하였습니다. 자세한 사항은 콘솔을 참조하십시오.";
					break;
				case CurrentStatus.Searching:
					StatusColor = Utils.ColorConstants.WarningColor;
					StatusContent = "단어 찾는 중...";
					break;
				case CurrentStatus.AutoEntered:
					StatusColor = Utils.ColorConstants.NormalColor;
					StatusContent = "단어 자동 입력됨: {0}";
					break;
				case CurrentStatus.DB_Job:
					StatusColor = Utils.ColorConstants.WarningColor;
					StatusContent = "데이터베이스 작업 '{0}' 진행 중...";
					break;
				case CurrentStatus.DB_Job_Done:
					StatusColor = Utils.ColorConstants.NormalColor;
					StatusContent = "데이터베이스 작업 '{0}' 완료: {1}";
					break;
				case CurrentStatus.Adding_Words:
					StatusColor = Utils.ColorConstants.WarningColor;
					StatusContent = "단어 일괄 추가 작업 중...";
					break;
				case CurrentStatus.Adding_Words_Done:
					StatusColor = Utils.ColorConstants.NormalColor;
					StatusContent = "단어 일괄 추가 작업 완료";
					break;
				case CurrentStatus.Delaying:
					StatusColor = Utils.ColorConstants.NormalColor;
					StatusContent = "단어 찾음! 딜레이 대기 중: {0}ms";
					break;
				default:
					StatusColor = Utils.ColorConstants.WaitColor;
					StatusContent = "게임 참가를 기다리는 중.";
					break;

			}

			Logger.DebugFormat("Statusbar status change to {0}.", status);
			Dispatcher.Invoke(() =>
			{
				StatusGrid.Background = new SolidColorBrush(StatusColor);
				StatusLabel.Content = string.Format(StatusContent, formatterArgs);
			});
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			if (!(string.IsNullOrWhiteSpace(SubmitWord.Text) || SubmitWord.Text == INPUT_TEXT_PLACEHOLDER))
			{
				SendMessage(SubmitWord.Text);
				SubmitWord.Text = "";
			}
		}

		private void ClipboardSubmit_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string clipboard = Clipboard.GetText();
				if (!string.IsNullOrWhiteSpace(clipboard))
					SendMessage(clipboard);
			}
			catch
			{
			}
		}

		private void SendMessage(string message)
		{
			Handler.SendMessage(message);
			inputStopwatch.Restart();
		}

		private void TextInput_GotFocus(object sender, RoutedEventArgs e)
		{
			if (SubmitWord.Text == INPUT_TEXT_PLACEHOLDER)
			{
				SubmitWord.Text = "";
				SubmitWord.FontStyle = FontStyles.Normal;
			}
		}

		private void TextInput_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(SubmitWord.Text))
			{
				SubmitWord.Text = INPUT_TEXT_PLACEHOLDER;
				SubmitWord.FontStyle = FontStyles.Normal;
			}
		}

		private void PathList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var i = (PathFinder.PathObject)PathList.SelectedItem;
			if (i != null)
			{
				Logger.InfoFormat("Selected Path : '{0}'.", i.Content);

				// In sometimes, we are smarter than machines
				// if (_pathSelected)
				// 	ConsoleManager.Log(ConsoleManager.LogType.Info, "Can't execute path! : _pathSelected = true.", MAINTHREAD_NAME);
				// else
				// {
				Logger.InfoFormat("Executed Path : '{0}'.", i.Content);
				//_pathSelected = true;
				LastUsedPath = i.Content;
				SendMessage(i.Content);
				// }
			}
		}

		private void DBManager_Click(object sender, RoutedEventArgs e) => new DatabaseManagement().Show();

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			new ConfigWindow(CurrentConfig).Show();
		}

		private void Submit_URL(object sender, RoutedEventArgs e)
		{
			browser.Load(CurrentURL.Text);
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
		}

		private void Open_DevConsole(object sender, RoutedEventArgs e)
		{
			browser.ShowDevTools();
		}
	}
}
