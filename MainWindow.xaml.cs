using AutoKkutu.Handlers;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


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

		public static ChromiumWebBrowser browser;

		public static string LastUsedPath = "";

		private static bool _pathSelected;

		private int WordIndex = 0;

		public CommonHandler GameHandler;

		public static Config CurrentConfig;

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
			Adding_Words_Done
		}

		public MainWindow()
		{
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
				CachePath = Environment.CurrentDirectory + "\\Cache"
			}, true, (IApp)null);

			// Load default config
			PathFinder.UpdateConfig(CurrentConfig = new Config());

			// TODO: Improve this
			string url = "https://kkutu.org";
			if (new FileInfo("targetserver.txt").Exists)
				url = File.ReadAllText("targetserver.txt");
			browser = new ChromiumWebBrowser
			{
				Address = url,
				UseLayoutRounding = true
			};
			InitializeComponent();
			ConsoleManager.Show();
			Title = TITLE;
			VersionLabel.Content = "v1.0";
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Starting Load Page...", MAINTHREAD_NAME);
			LoadOverlay.Visibility = Visibility.Visible;
			TextInput.Text = INPUT_TEXT_PLACEHOLDER;
			TextInput.FontStyle = FontStyles.Italic;
			ChangeStatusBar(CurrentStatus.Wait);
			SetSearchState(null, false);
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
			browserContainer.Content = browser;
			DatabaseManager.DBError = (EventHandler)Delegate.Combine(DatabaseManager.DBError, new EventHandler(DatabaseManager_DBError));
			DatabaseManager.Init();
			PathFinder.Init();
		}

		public static void UpdateConfig(Config newConfig)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Updated config.", MAINTHREAD_NAME);
			CurrentConfig = newConfig;
			PathFinder.UpdateConfig(newConfig);
		}

		private void DatabaseManager_DBError(object sender, EventArgs e) => ChangeStatusBar(CurrentStatus.Error);

		private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Browser frame-load end.", MAINTHREAD_NAME);
			RemoveAd();
			Dispatcher.Invoke(() =>
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Hide LoadOverlay.", MAINTHREAD_NAME);
				DBStatus.Content = DatabaseManager.GetDBInfo();
				LoadOverlay.Visibility = Visibility.Hidden;
			});
			browser.FrameLoadEnd -= Browser_FrameLoadEnd;
			GameHandler = new KkutuOrgHandler(browser);
			GameHandler.GameStartedEvent += new EventHandler(CommonHandler_GameStart);
			GameHandler.GameEndedEvent += new EventHandler(CommonHandler_GameEnd);
			GameHandler.MyTurnEvent += new EventHandler(CommonHandler_MyTurnEvent);
			GameHandler.MyTurnEndedEvent += new EventHandler(CommonHandler_MyTurnEndEvent);
			GameHandler.WrongWordEvent += new EventHandler(CommonHandler_WrongPathEvent);
			GameHandler.MyPathIsWrongEvent += new EventHandler(CommonHandler_MyPathIsWrong);
			GameHandler.RoundChangeEvent += new EventHandler(CommonHandler_RoundChangeEvent);
			GameHandler.StartWatchdog();
			PathFinder.UpdatedPath += new EventHandler(PathFinder_UpdatedPath);
			DatabaseManager.DBJobStart += new EventHandler(DatabaseManager_DBJobStart);
			DatabaseManager.DBJobDone += new EventHandler(DatabaseManager_DBJobDone);
			DatabaseManagement.AddWordStart += new EventHandler(DatabaseManagement_AddWordStart);
			DatabaseManagement.AddWordDone += new EventHandler(DatabaseManagement_AddWordDone);
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
					if (arg.IsUseEndWord)
						Result += " (한방 단어 사용)";
				}
				else
				{
					if (arg.Result == PathFinder.FindResult.None)
					{
						Result = $"총 {arg.TotalWordCount}개의 단어 중, 가능한 것 없음.{Environment.NewLine}{arg.Time}ms 소요.";
						if (arg.IsUseEndWord)
							Result += " (한방 단어 사용)";
					}
					else
						Result = PATHFINDER_ERROR;
				}
			}
			Dispatcher.Invoke(() => SearchResult.Text = Result);
		}

		private void PathFinder_UpdatedPath(object sender, EventArgs e)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Path update received. ( PathFinder_UpdatedPath() )", MAINTHREAD_NAME);

			var i = (PathFinder.UpdatedPathEventArgs)e;

			if (i.Result == PathFinder.FindResult.None)
				ChangeStatusBar(CurrentStatus.NotFound);
			else if (i.Result == PathFinder.FindResult.Error)
				ChangeStatusBar(CurrentStatus.Error);
			
			Task.Run(() => SetSearchState(i));

			Dispatcher.Invoke(() =>
			{
				PathList.ItemsSource = PathFinder.FinalList;
			});

			_pathSelected = false;
			if (CurrentConfig.AutoEnter)
			{
				if (i.Result == PathFinder.FindResult.None)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Auto mode enabled. but can't find any path.", MAINTHREAD_NAME);
					ChangeStatusBar(CurrentStatus.NotFound);
				}
				else
				{
					string content = PathFinder.FinalList.First().Content;
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Auto mode enabled. automatically use first path.", MAINTHREAD_NAME);
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Execute Path : " + content, MAINTHREAD_NAME);
					LastUsedPath = content;
					_pathSelected = true;
					GameHandler.SendMessage(content);
					ChangeStatusBar(CurrentStatus.AutoEntered, content);
				}
			}
			else
				ChangeStatusBar(CurrentStatus.Normal);
		}

		private void ResetPathList()
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Reset Path list... ", MAINTHREAD_NAME);
			PathFinder.FinalList = new List<PathFinder.PathObject>();
			Dispatcher.Invoke(() => PathList.ItemsSource = PathFinder.FinalList);
		}

		private void CommonHandler_MyTurnEndEvent(object sender, EventArgs e)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Warning, "Reset WordIndex to zero", MAINTHREAD_NAME);
			WordIndex = 0;
		}

		private void CommonHandler_MyTurnEvent(object sender, EventArgs e)
		{
			var args = ((CommonHandler.MyTurnEventArgs)e);
			var word = args.Word;
			try
			{
				if (PathFinder.EndWordList.Contains(word.Content) && (!word.CanSubstitution || PathFinder.EndWordList.Contains(word.Substitution)))
				{
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't Find any path : Presented word is End word.", MAINTHREAD_NAME);
					ResetPathList();
					SetSearchState(null, true);
					ChangeStatusBar(CurrentStatus.EndWord);
				}
				else
				{
					ChangeStatusBar(CurrentStatus.Searching);
					PathFinder.FindPath(word, CurrentConfig.MissionDetection ? args.MissionChar : "", CurrentConfig.WordPreference, CurrentConfig.UseEndWord && PathFinder.PreviousPath.Count > 0, CurrentConfig.ReverseMode); // 첫 턴 한방 방지
				}
			}
			catch (Exception ex)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Can't Find Path : " + ex.ToString(), MAINTHREAD_NAME);
			}
		}

		private void CommonHandler_WrongPathEvent(object sender, EventArgs e)
		{
			string theWord = ((CommonHandler.WrongWordEventArgs)e).Word;
			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Entered word '{theWord}' is wrong; Added to removal list", MAINTHREAD_NAME);
			PathFinder.AddToWrongWord(theWord);
		}

		private void CommonHandler_MyPathIsWrong(object sender, EventArgs e)
		{
			var word = ((CommonHandler.WrongWordEventArgs)e).Word;
			ConsoleManager.Log(ConsoleManager.LogType.Info, $"My path '{word}' is wrong.", MAINTHREAD_NAME);

			if (!CurrentConfig.AutoFix)
				return;

			List<PathFinder.PathObject> localFinalList = PathFinder.FinalList;
			if (localFinalList.Count < WordIndex - 1)
			{
				if (localFinalList.Count <= 0)
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't Find any path.", MAINTHREAD_NAME);
				else
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Found path, but there's no path to use.", MAINTHREAD_NAME);
				ChangeStatusBar(CurrentStatus.EndWord);
			}

			_pathSelected = false;
			if (CurrentConfig.AutoEnter)
			{
				try
				{
					WordIndex++;
					string path = localFinalList[WordIndex].Content;
					ConsoleManager.Log(ConsoleManager.LogType.Info, $"Auto mode enabled. automatically use next path (index {WordIndex}).", MAINTHREAD_NAME);
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Execute Path : " + path, MAINTHREAD_NAME);
					LastUsedPath = path;
					_pathSelected = true;
					GameHandler.SendMessage(path);
				}
				catch (Exception ex)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Error, $"Can't execute path : {ex}", MAINTHREAD_NAME);
				}
			}
		}

		private void CommonHandler_GameStart(object sender, EventArgs e)
		{
			ChangeStatusBar(CurrentStatus.Normal);
			WordIndex = 0;
		}

		private void CommonHandler_GameEnd(object sender, EventArgs e)
		{
			SetSearchState(null, false);
			ResetPathList();
			if (CurrentConfig.AutoDBUpdateMode == Config.DBAUTOUPDATE_GAME_END_INDEX)
				PathFinder.AutoDBUpdate();
			ChangeStatusBar(CurrentStatus.Wait);
		}

		private void CommonHandler_RoundChangeEvent(object sender, EventArgs e)
		{
			if (CurrentConfig.AutoDBUpdateMode == Config.DBAUTOUPDATE_GAME_ROUND_INDEX)
				PathFinder.AutoDBUpdate();
		}

		private void RemoveAd()
		{
			browser.ExecuteScriptAsyncWhenPageLoaded("document.body.style.overflow ='hidden'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADBox').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADVERTISEMENT').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADVERTISEMENT_TITLE').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementsByClassName('kktukorea__1LZzX_0')[0].style = 'display:none'", false);
		}

		private void ChangeStatusBar(CurrentStatus status, params object[] formatterArgs)
		{
			Color StatusColor;
			string StatusContent;
			switch (status)
			{
				case CurrentStatus.Normal:
					StatusColor = Resource.ColorSource.NormalColor;
					StatusContent = "준비";
					break;
				case CurrentStatus.NotFound:
					StatusColor = Resource.ColorSource.WarningColor;
					StatusContent = "이 턴에 낼 수 있는 단어를 데이터 집합에서 찾을 수 없었습니다. 수동으로 입력하십시오.";
					break;
				case CurrentStatus.EndWord:
					StatusColor = Resource.ColorSource.ErrorColor;
					StatusContent = "더 이상 이 턴에 낼 수 있는 단어가 없습니다.";
					break;
				case CurrentStatus.Error:
					StatusColor = Resource.ColorSource.ErrorColor;
					StatusContent = "서버에 문제가 있습니다. 자세한 사항은 콘솔을 참조하십시오.";
					break;
				case CurrentStatus.Searching:
					StatusColor = Resource.ColorSource.WarningColor;
					StatusContent = "단어 찾는 중...";
					break;
				case CurrentStatus.AutoEntered:
					StatusColor = Resource.ColorSource.NormalColor;
					StatusContent = "단어 자동 입력됨: {0}";
					break;
				case CurrentStatus.DB_Job:
					StatusColor = Resource.ColorSource.WarningColor;
					StatusContent = "데이터베이스 작업 '{0}' 진행 중...";
					break;
				case CurrentStatus.DB_Job_Done:
					StatusColor = Resource.ColorSource.NormalColor;
					StatusContent = "데이터베이스 작업 '{0}' 완료: {1}";
					break;
				case CurrentStatus.Adding_Words:
					StatusColor = Resource.ColorSource.WarningColor;
					StatusContent = "단어 일괄 추가 작업 중...";
					break;
				case CurrentStatus.Adding_Words_Done:
					StatusColor = Resource.ColorSource.NormalColor;
					StatusContent = "단어 일괄 추가 작업 완료";
					break;
				default:
					StatusColor = Resource.ColorSource.WaitColor;
					StatusContent = "게임 참가를 기다리는 중.";
					break;

			}

			ConsoleManager.Log(ConsoleManager.LogType.Info, "Statusbar status change to " + status.ToString() + ".", MAINTHREAD_NAME);
			Dispatcher.Invoke(() =>
			{
				StatusGrid.Background = new SolidColorBrush(StatusColor);
				StatusLabel.Content = string.Format(StatusContent, formatterArgs);
			});
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			if (!(string.IsNullOrWhiteSpace(TextInput.Text) || TextInput.Text == INPUT_TEXT_PLACEHOLDER))
			{
				GameHandler.SendMessage(TextInput.Text);
				TextInput.Text = "";
			}
		}

		private void ClipboardSubmit_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string clipboard = Clipboard.GetText();
				if (!string.IsNullOrWhiteSpace(clipboard))
					GameHandler.SendMessage(clipboard);
			}
			catch
			{
			}
		}

		private void TextInput_GotFocus(object sender, RoutedEventArgs e)
		{
			if (TextInput.Text == INPUT_TEXT_PLACEHOLDER)
			{
				TextInput.Text = "";
				TextInput.FontStyle = FontStyles.Normal;
			}
		}

		private void TextInput_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TextInput.Text))
			{
				TextInput.Text = INPUT_TEXT_PLACEHOLDER;
				TextInput.FontStyle = FontStyles.Normal;
			}
		}

		private void PathList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var i = (PathFinder.PathObject)PathList.SelectedItem;
			if (i != null)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Selected Path : " + i.Content, MAINTHREAD_NAME);

				// In sometimes, we are smarter than machines
				// if (_pathSelected)
				// 	ConsoleManager.Log(ConsoleManager.LogType.Info, "Can't execute path! : _pathSelected = true.", MAINTHREAD_NAME);
				// else
				// {
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Execute Path : " + i.Content, MAINTHREAD_NAME);
				_pathSelected = true;
				LastUsedPath = i.Content;
				GameHandler.SendMessage(i.Content);
				// }
			}
		}

		private void DBManager_Click(object sender, RoutedEventArgs e) => new DatabaseManagement().Show();

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			new ConfigWindow(CurrentConfig).Show();
		}
	}
}
