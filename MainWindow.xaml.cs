using AutoKkutu.Handlers;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
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

		public CommonHandler GameHandler;

		private enum CurrentStatus
		{
			Normal,
			Warning,
			Error,
			NoPath,
			Wait
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
			browser = new ChromiumWebBrowser
			{
				Address = "https://kkutu.org",
				UseLayoutRounding = true
			};
			InitializeComponent();
			ConsoleManager.Show();
			Title = TITLE;
			VersionLabel.Content = "v1.0";
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Starting Load Page...", MAINTHREAD_NAME);
			LoadOverlay.Visibility = Visibility.Visible;
			TextInput.Text = INPUT_TEXT_PLACEHOLDER;
			ChangeStatusBar(CurrentStatus.Wait);
			SetSearchState(null, false);
			browser.FrameLoadEnd += Browser_FrameLoadEnd;
			browserContainer.Content = browser;
			DatabaseManager.DBError = (EventHandler)Delegate.Combine(DatabaseManager.DBError, new EventHandler(DatabaseManager_DBError));
			DatabaseManager.Init();
			PathFinder.Init();
		}

		private void DatabaseManager_DBError(object sender, EventArgs e) => ChangeStatusBar(CurrentStatus.Error);

		private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "First browser frame-load end.", MAINTHREAD_NAME);
			RemoveAd();
			Dispatcher.Invoke(() =>
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Hide LoadOverlay.", MAINTHREAD_NAME);
				DBStatus.Content = DatabaseManager.GetDBInfo();
				LoadOverlay.Visibility = Visibility.Hidden;
			});
			browser.FrameLoadEnd -= Browser_FrameLoadEnd;
			GameHandler = new KkutuOrgHandler(browser);
			GameHandler.GameStartedEvent += new EventHandler(KkutuHandler_GameStart);
			GameHandler.GameEndedEvent += new EventHandler(KkutuHandler_GameEnd);
			GameHandler.MyTurnEvent += new EventHandler(KkutuHandler_MyTurnEvent);
			GameHandler.MyTurnEndedEvent += new EventHandler(KkutuHandler_MyTurnEndEvent);
			GameHandler.MyPathIsWrongEvent += new EventHandler(KkutuHandler_MyPathIsWrong);
			GameHandler.StartWatchdog();
			PathFinder.UpdatedPath = (EventHandler)Delegate.Combine(PathFinder.UpdatedPath, new EventHandler(PathFinder_UpdatedPath));
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

		private async void UpdateUI(PathFinder.UpdatedPathEventArgs arg)
		{
			if (arg.Result == PathFinder.FindResult.Normal)
				ChangeStatusBar(CurrentStatus.Normal);
			else if (arg.Result == PathFinder.FindResult.None)
				ChangeStatusBar(CurrentStatus.Warning);
			else if (arg.Result == PathFinder.FindResult.Error)
				ChangeStatusBar(CurrentStatus.Error);
			SetSearchState(arg);
		}

		private void PathFinder_UpdatedPath(object sender, EventArgs e)
		{
			bool AutomodeChecked = false;
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Path update received. ( PathFinder_UpdatedPath() )", MAINTHREAD_NAME);

			var i = (PathFinder.UpdatedPathEventArgs)e;
			Task.Run(() => UpdateUI(i));
			Dispatcher.Invoke(() =>
			{
				PathList.ItemsSource = PathFinder.FinalList;
				AutomodeChecked = Automode.IsChecked.Value;
			});

			_pathSelected = false;
			if (AutomodeChecked)
			{
				if (i.Result == PathFinder.FindResult.None)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Auto mode enabled. but can't find any path.", MAINTHREAD_NAME);
					ChangeStatusBar(CurrentStatus.Warning);
				}
				else
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Auto mode enabled. automatically use first path.", MAINTHREAD_NAME);
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Execute Path : " + PathFinder.FinalList.First().Content, MAINTHREAD_NAME);
					LastUsedPath = PathFinder.FinalList.First().Content;
					_pathSelected = true;
					GameHandler.SendMessage(PathFinder.FinalList.First().Content);
				}
			}
		}

		private void ResetPathList()
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Reset Path list... ", MAINTHREAD_NAME);
			PathFinder.FinalList = new List<PathFinder.PathObject>();
			Dispatcher.Invoke(() => PathList.ItemsSource = PathFinder.FinalList);
		}

		private void KkutuHandler_MyTurnEndEvent(object sender, EventArgs e)
		{
		}

		private void KkutuHandler_MyTurnEvent(object sender, EventArgs e)
		{
			var word = ((CommonHandler.MyTurnEventArgs)e).Word;
			bool EndwordChecked = false;
			bool DontUseEndWord = false;
			Dispatcher.Invoke(() =>
			{
				EndwordChecked = PreferEndWord.IsChecked ?? false;
				DontUseEndWord = MannerMode.IsChecked ?? false;
			});
			try
			{
				if (PathFinder.EndWordList.Contains(word.Content) && (!word.CanSubstitution || PathFinder.EndWordList.Contains(word.Substitution)))
				{
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't Find any path : Presented word is End word.", MAINTHREAD_NAME);
					ResetPathList();
					SetSearchState(null, true);
					ChangeStatusBar(CurrentStatus.NoPath);
				}
				else
					PathFinder.FindPath(word, DontUseEndWord, EndwordChecked);
			}
			catch (Exception ex)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Can't Find Path : " + ex.ToString(), MAINTHREAD_NAME);
			}
		}

		private void KkutuHandler_MyPathIsWrong(object sender, EventArgs e)
		{
			bool AutomodeChecked = false;
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Entered word is wrong. ( KkutuHandler_MyPathIsWrong() )", MAINTHREAD_NAME);

			var word = ((CommonHandler.WrongWordEventArgs)e).Word;

			// Remove from final-list
			PathFinder.FinalList.RemoveAll(p => p.Content.Equals(word, StringComparison.InvariantCultureIgnoreCase));

			if (PathFinder.FinalList.Count <= 0)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't Find any path.", MAINTHREAD_NAME);
				ChangeStatusBar(CurrentStatus.NoPath);
			}

			Dispatcher.Invoke(() =>
			{
				PathList.ItemsSource = PathFinder.FinalList;
				AutomodeChecked = Automode.IsChecked.Value;
			});

			_pathSelected = false;
			if (AutomodeChecked)
			{
				try
				{
					string path = PathFinder.FinalList.First().Content;
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Auto mode enabled. automatically use next path.", MAINTHREAD_NAME);
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

			PathFinder.AddExclusion(word);
		}

		private void KkutuHandler_GameStart(object sender, EventArgs e) => ChangeStatusBar(CurrentStatus.Normal);

		private void KkutuHandler_GameEnd(object sender, EventArgs e)
		{
			bool EnableDBAutoUpdate = false;
			SetSearchState(null, false);
			ResetPathList();
			Dispatcher.Invoke(() => EnableDBAutoUpdate = AutoDBUpdate.IsEnabled);
			PathFinder.AutoDBUpdate(EnableDBAutoUpdate);
			ChangeStatusBar(CurrentStatus.Wait);
		}

		private void RemoveAd()
		{
			browser.ExecuteScriptAsyncWhenPageLoaded("document.body.style.overflow ='hidden'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADBox').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADVERTISEMENT').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('ADVERTISEMENT_TITLE').style = 'display:none'", false);
			browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementsByClassName('kktukorea__1LZzX_0')[0].style = 'display:none'", false);
		}

		private void ChangeStatusBar(CurrentStatus status)
		{
			Color StatusColor;
			string StatusContent;
			switch (status)
			{
				case CurrentStatus.Normal:
					StatusColor = Resource.ColorSource.NormalColor;
					StatusContent = "준비";
					break;
				case CurrentStatus.Warning:
					StatusColor = Resource.ColorSource.WarningColor;
					StatusContent = "이 턴에 낼 수 있는 단어를 데이터 집합에서 찾을 수 없었습니다. 수동으로 입력하십시오.";
					break;
				case CurrentStatus.NoPath:
					StatusColor = Resource.ColorSource.ErrorColor;
					StatusContent = "더 이상 이 턴에 낼 수 있는 단어가 없습니다.";
					break;
				case CurrentStatus.Error:
					StatusColor = Resource.ColorSource.ErrorColor;
					StatusContent = "서버에 문제가 있습니다. 자세한 사항은 콘솔을 참조하십시오.";
					break;
				default:
					StatusColor = Resource.ColorSource.WaitColor;
					StatusContent = "게임 참가를 기다리는 중.";
					break;

			}

			ConsoleManager.Log(ConsoleManager.LogType.Info, "Statusbar status change to " + status.ToString() + ".", MAINTHREAD_NAME);
			Dispatcher.Invoke(delegate ()
			{
				StatusGrid.Background = new SolidColorBrush(StatusColor);
				StatusLabel.Content = StatusContent;
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
				TextInput.Text = "";
		}

		private void TextInput_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TextInput.Text))
				TextInput.Text = INPUT_TEXT_PLACEHOLDER;
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

		private void MannerMode_Click(object sender, RoutedEventArgs e)
		{
			PathFinder.Manner = MannerMode.IsChecked ?? false;
		}

		private void Return_Click(object sender, RoutedEventArgs e)
		{
			PathFinder.AllowDuplicate = Return.IsChecked ?? false;
		}
	}
}
