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
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static ChromiumWebBrowser browser;

		public const string Version = "5.6.8500";

		private const string _mainthreadName = "MainThread";

		private const string _textinputPlaceholder = "여기에 텍스트를 입력해주세요";

		public static string LastUsedPath = "";

		private static bool _pathSelected;

		public KkutuHandler GameHandler;

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
				Address = "https://kkutu.co.kr",
				UseLayoutRounding = true
			};
			InitializeComponent();
			ConsoleManager.Show();
			Title = "KKutuHelper - Executive Beta";
			VersionLabel.Content = "KKutuHelper V - 5.6.8500";
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Starting Load Page...", "MainThread");
			LoadOverlay.Visibility = Visibility.Visible;
			TextInput.Text = "여기에 텍스트 입력";
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
			ConsoleManager.Log(ConsoleManager.LogType.Info, "First browser frame-load end.", "MainThread");
			RemoveAd();
			Dispatcher.Invoke(delegate ()
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Hide LoadOverlay.", "MainThread");
				DBStatus.Content = DatabaseManager.GetDBInfo();
				LoadOverlay.Visibility = Visibility.Hidden;
			});
			browser.FrameLoadEnd -= Browser_FrameLoadEnd;
			GameHandler = new KkutuHandler(browser);
			KkutuHandler gameHandler = GameHandler;
			gameHandler.GameStartedEvent = (EventHandler)Delegate.Combine(gameHandler.GameStartedEvent, new EventHandler(KkutuHandler_GameStart));
			KkutuHandler gameHandler2 = GameHandler;
			gameHandler2.GameEndedEvent = (EventHandler)Delegate.Combine(gameHandler2.GameEndedEvent, new EventHandler(KkutuHandler_GameEnd));
			KkutuHandler gameHandler3 = GameHandler;
			gameHandler3.MyTurnEvent = (EventHandler)Delegate.Combine(gameHandler3.MyTurnEvent, new EventHandler(KkutuHandler_MyTurnEvent));
			KkutuHandler gameHandler4 = GameHandler;
			gameHandler4.MyTurnEndedEvent = (EventHandler)Delegate.Combine(gameHandler4.MyTurnEndedEvent, new EventHandler(KkutuHandler_MyTurnEndEvent));
			GameHandler.StartWatchdog();
			PathFinder.UpdatedPath = (EventHandler)Delegate.Combine(PathFinder.UpdatedPath, new EventHandler(PathFinder_UpdatedPath));
		}

		private void SetSearchState(PathFinder.UpdatedPathEventArgs arg, bool IsEnd = false)
		{
			bool flag = arg == null;
			string Result;
			if (flag)
			{
				if (IsEnd)
					Result = "이 턴에 가능한 페스 없음.";
				else
					Result = "페스 검색 대기중.";
			}
			else
			{
				if (arg.Result == PathFinder.FindResult.Normal)
				{
					Result = string.Format("총 {0}개의 단어 중, {1}개의 페스 고려{2}{3}ms 소요. ", new object[]
					{
						arg.TotalWordCount,
						arg.CalcWordCount,
						Environment.NewLine,
						arg.Time
					});

					if (arg.IsUseEndWord)
						Result += "(한 방 단어 사용)";
				}
				else
				{
					if (arg.Result == PathFinder.FindResult.None)
					{
						Result = string.Format("총 {0}개의 단어 중, 가능한 페스 없음.{1}{2}ms 소요. ", arg.TotalWordCount, Environment.NewLine, arg.Time);
						bool isUseEndWord2 = arg.IsUseEndWord;
						if (isUseEndWord2)
							Result += "(한 방 단어 사용)";
					}
					else
						Result = "오류가 발생하여 페스 검색 실패.";
				}
			}
			Dispatcher.Invoke(delegate ()
			{
				SearchResult.Text = Result;
			});
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
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Path update reciviced. ( PathFinder_UpdatedPath() )", "MainThread");
			var i = (PathFinder.UpdatedPathEventArgs)e;
			Task.Run(() => UpdateUI(i));
			Dispatcher.Invoke(() =>
			{
				PathList.ItemsSource = PathFinder.FinalList;
				AutomodeChecked = Automode.IsChecked.Value;
			});
			_pathSelected = false;
			bool automodeChecked = AutomodeChecked;
			if (automodeChecked)
			{
				bool flag = i.Result == PathFinder.FindResult.None;
				if (flag)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Auto mode enabled. but can't find any path.", "MainThread");
					ChangeStatusBar(CurrentStatus.Warning);
				}
				else
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Auto mode enabled. automatically use first path.", "MainThread");
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Execute Path : " + PathFinder.FinalList.First().Content, "MainThread");
					LastUsedPath = PathFinder.FinalList.First().Content;
					_pathSelected = true;
					SendMessage(PathFinder.FinalList.First().Content);
				}
			}
		}

		private void ResetPathList()
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Reset Path list... ", "MainThread");
			PathFinder.FinalList = new List<PathFinder.PathObject>();
			Dispatcher.Invoke(delegate ()
			{
				PathList.ItemsSource = PathFinder.FinalList;
			});
		}

		private void KkutuHandler_MyTurnEndEvent(object sender, EventArgs e)
		{
		}

		private void KkutuHandler_MyTurnEvent(object sender, EventArgs e)
		{
			var i = (KkutuHandler.MyTurnEventArgs)e;
			bool EndwordChecked = false;
			Dispatcher.Invoke(delegate ()
			{
				EndwordChecked = EndWord.IsChecked.Value;
			});
			try
			{
				bool flag = PathFinder.EndWordList.Contains(i.Word.Content);
				if (flag)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't Find any path : Presented word is End word.", "MainThread");
					ResetPathList();
					SetSearchState(null, true);
					ChangeStatusBar(CurrentStatus.NoPath);
				}
				else
				{
					PathFinder.FindPath(i.Word, EndwordChecked);
				}
			}
			catch (Exception ex)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Can't Find Path : " + ex.ToString(), "MainThread");
			}
		}

		private void KkutuHandler_GameStart(object sender, EventArgs e) => ChangeStatusBar(CurrentStatus.Normal);

		private void KkutuHandler_GameEnd(object sender, EventArgs e)
		{
			bool EnableDBAutoUpdate = false;
			SetSearchState(null, false);
			ResetPathList();
			Dispatcher.Invoke(delegate ()
			{
				EnableDBAutoUpdate = AutoDBUpdate.IsEnabled;
			});
			PathFinder.AutoDBUpdate(EnableDBAutoUpdate);
			ChangeStatusBar(CurrentStatus.Wait);
		}

		private void SendMessage(string input)
		{
			browser.ExecuteScriptAsync("document.querySelectorAll('[id*=\"UserMessage\"]')[0].value='" + input.Trim() + "'");
			browser.ExecuteScriptAsync("document.getElementById('ChatBtn').click()");
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

			ConsoleManager.Log(ConsoleManager.LogType.Info, "Statusbar status change to " + status.ToString() + ".", "MainThread");
			Dispatcher.Invoke(delegate ()
			{
				StatusGrid.Background = new SolidColorBrush(StatusColor);
				StatusLabel.Content = StatusContent;
			});
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			bool flag = string.IsNullOrWhiteSpace(TextInput.Text) || TextInput.Text == "여기에 텍스트 입력";
			if (!flag)
			{
				SendMessage(TextInput.Text);
				TextInput.Text = "";
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

		private void TextInput_GotFocus(object sender, RoutedEventArgs e)
		{
			if (TextInput.Text == "여기에 텍스트 입력해주세요")
				TextInput.Text = "";
		}

		private void TextInput_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TextInput.Text))
				TextInput.Text = "여기에 텍스트 입력해주세요";
		}

		private void PathList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var i = (PathFinder.PathObject)PathList.SelectedItem;
			if (i != null)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Selected Path : " + i.Content, "MainThread");
				if (_pathSelected)
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Can't execute path! : _pathSelected = true.", "MainThread");
				else
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Execute Path : " + i.Content, "MainThread");
					_pathSelected = true;
					LastUsedPath = i.Content;
					SendMessage(i.Content);
				}
			}
		}

		private void DBManager_Click(object sender, RoutedEventArgs e) => new DatabaseManagement().Show();
	}
}
