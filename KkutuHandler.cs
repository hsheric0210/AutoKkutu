using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public class KkutuHandler
	{
		public ChromiumWebBrowser Browser;
		public bool IsWatchdogAlive;

		public string CurrentPresentedWord => _currentPresentedWord;

		public bool IsGameStarted => _isGamestarted;

		public bool IsMyTurn => _isMyTurn;

		private Task _watchdogTask;

		private readonly int _checkgameinterval = 3000;

		private readonly int _ingameinterval = 1;

		private bool _isGamestarted = false;

		private bool _isMyTurn = false;

		private bool _isWatchdogStarted = false;

		private string _wordCache = "";

		private string _roundCache = "";

		private string _currentPresentedWord;

		public EventHandler GameStartedEvent;

		public EventHandler GameEndedEvent;

		public EventHandler MyTurnEvent;

		public EventHandler MyTurnEndedEvent;

		public EventHandler RoundEndedEvent;

		public EventHandler PastDictionaryEvent;

		public enum CheckType
		{
			GameStarted,
			MyTurn
		}

		public KkutuHandler(ChromiumWebBrowser browser)
		{
			Browser = browser;
		}

		public void StartWatchdog()
		{
			if (IsWatchdogAlive)
				return;

			IsWatchdogAlive = true;
			new Task(new Action(() => { })).Start();
			// log
		}

		private string EvaluateJS(string javaScript)
		{
			try
			{
				return Browser.EvaluateScriptAsync(javaScript)?.Result?.Result?.ToString() ?? " ";
			}
			catch (NullReferenceException)
			{
				return " ";
			}
			catch (Exception)
			{
				// log
				return " ";
			}
		}

		private void CheckGameState(CheckType type)
		{
			string result;
			if (type == CheckType.GameStarted)
				result = EvaluateJS("document.getElementById('GameBox').style.display");
			else
				result = EvaluateJS("document.getElementsByClassName('game-input')[0].style.display");

			if (string.IsNullOrWhiteSpace(result) || result == "none")
			{
				if (type == CheckType.GameStarted)
				{
					bool isGameStarted = IsGameStarted;
					if (isGameStarted)
					{
						Log(ConsoleManager.LogType.Info, "Game ended.");
						bool flag4 = GameEndedEvent != null;
						if (flag4)
						{
							GameEndedEvent(this, EventArgs.Empty);
						}
						_isGamestarted = false;
					}
				}
				else
				{
					bool isMyTurn = IsMyTurn;
					if (isMyTurn)
					{
						Log(ConsoleManager.LogType.Info, "My turn ended.");
						bool flag5 = MyTurnEndedEvent != null;
						if (flag5)
						{
							MyTurnEndedEvent(this, EventArgs.Empty);
						}
						_isMyTurn = false;
					}
				}
			}
			else
			{
				bool flag6 = type == CheckType.GameStarted;
				if (flag6)
				{
					bool flag7 = !_isGamestarted;
					if (flag7)
					{
						Log(ConsoleManager.LogType.Info, "Game started.");
						Log(ConsoleManager.LogType.Info, "Reset PreviousWordList.");
						bool flag8 = GameStartedEvent != null;
						if (flag8)
						{
							GameStartedEvent(this, EventArgs.Empty);
						}
						_isGamestarted = true;
					}
				}
				else
				{
					bool flag9 = !_isMyTurn;
					if (flag9)
					{
						ResponsePresentedWord word = GetPresentedWord();
						bool canSubstitution = word.CanSubstitution;
						if (canSubstitution)
						{
							// Log(VerboseConsole.Level.Info, string.Concat(new string[]
							//{
							//	"My Turn. presented word is ",
							//	word.Content,
							//	" (Subsitution: ",
							//	word.Substitution,
							//	")"
							//}));
						}
						else
						{
							//Log(VerboseConsole.Level.Info, "My Turn. presented word is " + word.Content);
						}
						_currentPresentedWord = word.Content;
						if (MyTurnEvent != null)
							MyTurnEvent(this, new MyTurnEventArgs(word));
						_isMyTurn = true;
					}
				}
			}
		}

		private void GetPreviousWord()
		{
			string rawresult = EvaluateJS("document.getElementsByClassName('ellipse history-item expl-mother')[0].innerHTML");
			bool flag = string.IsNullOrWhiteSpace(rawresult);
			if (!flag)
			{
				string result = rawresult.Split(new char[]
				{
					'<'
				})[0];
				if (result != _wordCache)
				{
					//Log(VerboseConsole.Level.Info, "Found PreviousWord : " + result);
					_wordCache = result;
					bool flag3 = result != MainWindow.LastUsedPath;
					if (flag3)
					{
						bool flag4 = !PathFinder.AutoDBUpdateList.Contains(result);
						if (flag4)
						{
							PathFinder.AutoDBUpdateList.Add(result);
						}
					}
					PathFinder.AddPreviousPath(result);
				}
			}
		}

		private void GetRound()
		{
			string result = EvaluateJS("document.getElementsByClassName('rounds-current')[0].textContent");
			if (!string.IsNullOrWhiteSpace(result))
			{
				if (result != _roundCache)
				{
					Log(ConsoleManager.LogType.Info, "Round Changed: " + result);
					PathFinder.PreviousPath = new List<string>();
					_roundCache = result;
				}
			}
		}

		private void Log(ConsoleManager.LogType Level, string Content) => ConsoleManager.Log(Level, Content, "GameRuiner - #" + _watchdogTask.Id.ToString());

		private ResponsePresentedWord GetPresentedWord()
		{
			string i = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent").Trim();
			bool flag = i.Length > 1;
			ResponsePresentedWord result;
			if (flag)
			{
				result = new ResponsePresentedWord(i[0].ToString(), true, i[2].ToString());
			}
			else
			{
				result = new ResponsePresentedWord(i[0].ToString(), false, "");
			}
			return result;
		}

		public class ResponsePresentedWord
		{
			public ResponsePresentedWord(string content, bool canSubsitution, string subsituration = "")
			{
				Content = content;
				CanSubstitution = canSubsitution;
				bool canSubstitution = CanSubstitution;
				if (canSubstitution)
				{
					Substitution = subsituration;
				}
			}

			public string Content;

			public bool CanSubstitution;

			public string Substitution;
		}

		public class MyTurnEventArgs : EventArgs
		{
			public MyTurnEventArgs(ResponsePresentedWord word) => Word = word;

			public ResponsePresentedWord Word;
		}
	}
}
