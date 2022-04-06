using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public abstract class CommonHandler
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

		private string _wrongWordCache = "";

		private string _exampleWordCache = "";

		public EventHandler GameStartedEvent;

		public EventHandler GameEndedEvent;

		public EventHandler MyTurnEvent;

		public EventHandler MyTurnEndedEvent;

		public EventHandler RoundEndedEvent;

		public EventHandler PastDictionaryEvent;

		public EventHandler WrongWordEvent;

		public EventHandler MyPathIsWrongEvent;

		public EventHandler RoundChangeEvent;

		public enum CheckType
		{
			GameStarted,
			MyTurn
		}

		public CommonHandler(ChromiumWebBrowser browser) => Browser = browser;

		public void StartWatchdog()
		{
			if (!_isWatchdogStarted)
			{
				_isWatchdogStarted = true;
				_watchdogTask = new Task(Watchdog);
				_watchdogTask.Start();
				Log(ConsoleManager.LogType.Info, "Watchdog thread started.");
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
					GetCurrentRound();
					CheckWrongWord();
					CheckExample();
					await Task.Delay(_ingameinterval);
				}
				else
					await Task.Delay(_checkgameinterval);
			}
		}

		protected string EvaluateJS(string javaScript)
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
			if (type == CheckType.GameStarted ? IsGameNotInProgress() : IsGameNotInMyTurn())
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
				Log(ConsoleManager.LogType.Info, "New round started; Previous word list flushed.");
				if (GameStartedEvent != null)
					GameStartedEvent(this, EventArgs.Empty);
				_isGamestarted = true;
			}
			else if (!_isMyTurn)
			{
				ResponsePresentedWord presentedWord = GetPresentedWord();
				if (presentedWord.CanSubstitution)
					Log(ConsoleManager.LogType.Info, $"My Turn. presented word is {presentedWord.Content} (Subsitution: {presentedWord.Substitution})");
				else
					Log(ConsoleManager.LogType.Info, $"My Turn. presented word is {presentedWord.Content}");
				_currentPresentedWord = presentedWord.Content;
				if (MyTurnEvent != null)
					MyTurnEvent(this, new MyTurnEventArgs(presentedWord, GetMissionWord()));
				_isMyTurn = true;
			}
		}

		private void GetPreviousWord()
		{
			string previousWord = GetGamePreviousWord();
			if (string.IsNullOrWhiteSpace(previousWord))
				return;
			string word = previousWord.Split('<')[0];
			if (word == _wordCache)
				return;
			Log(ConsoleManager.LogType.Info, "Found Previous Word : " + word);
			_wordCache = word;
			if (word != MainWindow.LastUsedPath && !PathFinder.NewPathList.Contains(word))
				PathFinder.NewPathList.Add(word);
			PathFinder.AddPreviousPath(word);
		}

		private void GetCurrentRound()
		{
			string round = GetGameRound();
			if (string.IsNullOrWhiteSpace(round))
				return;
			if (string.Equals(round, _roundCache, StringComparison.InvariantCulture))
				return;
			Log(ConsoleManager.LogType.Info, "Round Changed: " + round);
			if (RoundChangeEvent != null)
				RoundChangeEvent(this, EventArgs.Empty);
			PathFinder.PreviousPath = new List<string>();
			_roundCache = round;
		}

		private void CheckWrongWord()
		{
			string wrongWord = GetWrongWord();
			if (string.IsNullOrWhiteSpace(wrongWord))
				return;
			if (string.Equals(wrongWord, _wrongWordCache, StringComparison.InvariantCultureIgnoreCase))
				return;
			if (wrongWord.Contains(":") || wrongWord.Contains("T.T")) // 첫 턴 한방 금지, 한방 단어(매너) 등등...
				return;
			_wrongWordCache = wrongWord;

			if (WrongWordEvent != null)
				WrongWordEvent(this, new WrongWordEventArgs(wrongWord));
			if (IsMyTurn && MyPathIsWrongEvent != null)
				MyPathIsWrongEvent(this, new WrongWordEventArgs(wrongWord));
		}

		private void CheckExample()
		{
			string example = GetExampleWord();
			if (string.IsNullOrWhiteSpace(example))
				return;
			if (string.Equals(example, _exampleWordCache, StringComparison.InvariantCultureIgnoreCase))
				return;
			_exampleWordCache = example;
			Log(ConsoleManager.LogType.Info, "Example submitted: " + example);
			PathFinder.NewPathList.Add(example);
		}

		void Log(ConsoleManager.LogType logtype, string Content) => ConsoleManager.Log(logtype, Content, "KkutuHandler - #" + _watchdogTask.Id.ToString());

		private ResponsePresentedWord GetPresentedWord()
		{
			string presentWord = GetGamePresentedWord().Trim();
			if (presentWord.Length <= 1)
				return new ResponsePresentedWord(presentWord[0].ToString(), false);
			char firstChar = presentWord[0]; // TODO: 앞말잇기, 중간말잇기 feature 추가
			string content = firstChar.ToString();
			firstChar = presentWord[2];
			string subsituration = firstChar.ToString();
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
			public string MissionChar;

			public MyTurnEventArgs(ResponsePresentedWord word, string missionChar)
			{
				Word = word;
				MissionChar = missionChar;
			}
		}

		public class WrongWordEventArgs : EventArgs
		{
			public string Word;

			public WrongWordEventArgs(string word) => Word = word;
		}

		// These methods should be overridded
		public abstract string GetSiteURL();
		public bool IsGameNotInProgress()
		{
			string display = EvaluateJS("document.getElementsByClassName('GameBox Product')[0].style.display");
			string height = EvaluateJS("document.getElementsByClassName('GameBox Product')[0].style.height");
			return (string.IsNullOrWhiteSpace(height) || !string.IsNullOrWhiteSpace(display)) && (string.IsNullOrWhiteSpace(display) || display.Equals("none", StringComparison.InvariantCultureIgnoreCase));
		}

		public bool IsGameNotInMyTurn()
		{
			string displayOpt = EvaluateJS("document.getElementsByClassName('game-input')[0].style.display");
			return string.IsNullOrWhiteSpace(displayOpt) || displayOpt.Equals("none", StringComparison.InvariantCultureIgnoreCase);
		}

		public string GetGamePresentedWord()
		{
			return EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent");
		}

		public string GetGamePreviousWord()
		{
			return EvaluateJS("document.getElementsByClassName('ellipse history-item expl-mother')[0].innerHTML");
		}

		public string GetGameRound()
		{
			return EvaluateJS("document.getElementsByClassName('rounds-current')[0].textContent");
		}

		public string GetWrongWord()
		{
			return EvaluateJS("document.getElementsByClassName('game-fail-text')[0]") != "undefined" ? EvaluateJS("document.getElementsByClassName('game-fail-text')[0].textContent") : "";
		}

		public string GetExampleWord()
		{
			string innerHTML = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].innerHTML");
			string content = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent");
			return innerHTML.Contains("label") && innerHTML.Contains("color") && innerHTML.Contains("170,") && content.Length > 1 ? content : "";
		}

		public string GetMissionWord()
		{
			// TODO: 미션 단어 들어간 글자 선호해서 입력하는 기능 추가
			return EvaluateJS("document.getElementsByClassName('items')[0].style.opacity") == "1" ? EvaluateJS("document.getElementsByClassName('items')[0].textContent") : "";
		}

		public void SendMessage(string input)
		{
			EvaluateJS($"document.querySelectorAll('[id=\"Talk\"]')[0].value='{input.Trim()}'"); // "UserMessage"
			EvaluateJS("document.getElementById('ChatBtn').click()");
		}
	}
}
