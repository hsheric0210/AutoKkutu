using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
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

		public KkutuHandler(ChromiumWebBrowser browser) => Browser = browser;

		public void StartWatchdog()
		{
			if (!_isWatchdogStarted)
			{
				_isWatchdogStarted = true;
				_watchdogTask = new Task(Watchdog);
				_watchdogTask.Start();
				Log(ConsoleManager.LogType.Info, "Task created and start.");
			}
		}

		private async void Watchdog()
		{
			while (true)
			{
				CheckGameState(CheckType.GameStarted);
				if (_isGamestarted)
				{
					CheckGameState(CheckType.MyTurn);
					GetPreviousWord();
					GetRound();
					await Task.Delay(_ingameinterval);
				}
				else
					await Task.Delay(_checkgameinterval);
			}
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
			catch (Exception ex)
			{
				Log(ConsoleManager.LogType.Error, "Failed to run script on site. Expection : \n" + ex.ToString());
				return " ";
			}
		}

		private void CheckGameState(CheckType type)
		{
			string str = type != CheckType.GameStarted ? EvaluateJS("document.getElementsByClassName('game-input')[0].style.display") : EvaluateJS("document.getElementById('GameBox').style.display");
			if (string.IsNullOrWhiteSpace(str) || str == "none")
			{
				if (type == CheckType.GameStarted)
				{
					if (!IsGameStarted)
						return;
					Log(ConsoleManager.LogType.Info, "Game ended.");
					if (GameEndedEvent != null)
						GameEndedEvent(this, EventArgs.Empty);
					_isGamestarted = false;
				}
				else if (IsMyTurn)
				{
					Log(ConsoleManager.LogType.Info, "My turn ended.");
					if (MyTurnEndedEvent != null)
						MyTurnEndedEvent(this, EventArgs.Empty);
					_isMyTurn = false;
				}
			}
			else if (type == CheckType.GameStarted)
			{
				if (_isGamestarted)
					return;
				Log(ConsoleManager.LogType.Info, "Game started.");
				Log(ConsoleManager.LogType.Info, "Reset PreviousWordList.");
				if (GameStartedEvent != null)
					GameStartedEvent(this, EventArgs.Empty);
				_isGamestarted = true;
			}
			else if (!_isMyTurn)
			{
				ResponsePresentedWord presentedWord = GetPresentedWord();
				if (presentedWord.CanSubstitution)
					Log(ConsoleManager.LogType.Info, "My Turn. presented word is " + presentedWord.Content + " (Subsitution: " + presentedWord.Substitution + ")");
				else
					Log(ConsoleManager.LogType.Info, "My Turn. presented word is " + presentedWord.Content);
				_currentPresentedWord = presentedWord.Content;
				if (MyTurnEvent != null)
					MyTurnEvent(this, new MyTurnEventArgs(presentedWord));
				_isMyTurn = true;
			}
		}

		private void GetPreviousWord()
		{
			string str = EvaluateJS("document.getElementsByClassName('ellipse history-item expl-mother')[0].innerHTML");
			if (string.IsNullOrWhiteSpace(str))
				return;
			string word = str.Split('<')[0];
			if (word == _wordCache)
				return;
			Log(ConsoleManager.LogType.Info, "Found PreviousWord : " + word);
			_wordCache = word;
			if (word != MainWindow.LastUsedPath && !PathFinder.AutoDBUpdateList.Contains(word))
				PathFinder.AutoDBUpdateList.Add(word);
			PathFinder.AddPreviousPath(word);
		}

		private void GetRound()
		{
			string str = EvaluateJS("document.getElementsByClassName('rounds-current')[0].textContent");
			if (string.IsNullOrWhiteSpace(str) || !(str != _roundCache))
				return;
			Log(ConsoleManager.LogType.Info, "Round Changed: " + str);
			PathFinder.PreviousPath = new List<string>();
			_roundCache = str;
		}

		private void Log(ConsoleManager.LogType logtype, string Content) => ConsoleManager.Log(logtype, Content, "KkutuHandler - #" + _watchdogTask.Id.ToString());

		private ResponsePresentedWord GetPresentedWord()
		{
			string str = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent").Trim();
			if (str.Length <= 1)
				return new ResponsePresentedWord(str[0].ToString(), false);
			char ch = str[0];
			string content = ch.ToString();
			ch = str[2];
			string subsituration = ch.ToString();
			return new ResponsePresentedWord(content, true, subsituration);
		}

		public class ResponsePresentedWord
		{
			public string Content;
			public bool CanSubstitution;
			public string Substitution;

			public ResponsePresentedWord(string content, bool canSubsitution, string subsituration = "")
			{
				Content = content;
				CanSubstitution = canSubsitution;
				if (!CanSubstitution)
					return;
				Substitution = subsituration;
			}
		}

		public class MyTurnEventArgs : EventArgs
		{
			public ResponsePresentedWord Word;

			public MyTurnEventArgs(ResponsePresentedWord word) => Word = word;
		}
	}
}
