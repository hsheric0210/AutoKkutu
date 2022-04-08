using AutoKkutu.Handlers;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public abstract class CommonHandler
	{
		public ChromiumWebBrowser Browser;
		public bool IsWatchdogAlive;

		public ResponsePresentedWord CurrentPresentedWord = null;
		public string CurrentMissionChar => _current_mission_word;

		public bool IsGameStarted => _isgamestarted;

		public bool IsMyTurn => _isMyTurn;

		private Task _watchdogTask;

		private readonly int _checkgame_interval = 3000;
		private readonly int _ingame_interval = 1;
		private bool _isgamestarted = false;
		private bool _isMyTurn = false;
		private bool _isWatchdogStarted = false;
		private volatile bool _isWatchdogAlive = true;
		private string _current_mission_word = "";
		private string _wordCache = "";
		private string _roundCache = "";
		private string _unsupportedWordCache = "";
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

		private static CommonHandler[] HANDLERS;

		public static void InitHandlers(ChromiumWebBrowser browser)
		{
			HANDLERS = new CommonHandler[2];
			HANDLERS[0] = new KkutuOrgHandler(browser);
			HANDLERS[1] = new KkutuPinkHandler(browser);
		}

		public static CommonHandler getHandler(string url)
		{
			foreach (CommonHandler handler in HANDLERS)
				if (Regex.Match(url, handler.GetSiteURLPattern()).Success)
					return handler;
			return null;
		}

		public CommonHandler(ChromiumWebBrowser browser) => Browser = browser;

		public void StartWatchdog()
		{
			if (!_isWatchdogStarted)
			{
				_isWatchdogAlive = true;
				_isWatchdogStarted = true;
				_watchdogTask = new Task(Watchdog);
				_watchdogTask.Start();
				Log(ConsoleManager.LogType.Info, "Watchdog thread started.");
			}
		}
		public void StopWatchdog()
		{
			if (_isWatchdogStarted)
			{
				_isWatchdogStarted = false;
				_isWatchdogAlive = false;
				Log(ConsoleManager.LogType.Info, "Watchdog thread stop requested.");
			}
		}

		private async void Watchdog()
		{
			while (_isWatchdogAlive)
			{
				CheckGameState(CheckType.GameStarted);
				if (_isgamestarted)
				{
					CheckGameState(CheckType.MyTurn);
					GetPreviousWord();
					GetCurrentRound();
					GetCurrentMissionWord();
					CheckUnsupportedWord();
					CheckExample();
					await Task.Delay(_ingame_interval);
				}
				else
					await Task.Delay(_checkgame_interval);
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
					_isgamestarted = false;
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
				if (_isgamestarted)
					return;
				Log(ConsoleManager.LogType.Info, "New round started; Previous word list flushed.");
				if (GameStartedEvent != null)
					GameStartedEvent(this, EventArgs.Empty);
				_isgamestarted = true;
			}
			else if (!_isMyTurn)
			{
				ResponsePresentedWord presentedWord = GetPresentedWord();
				if (presentedWord.CanSubstitution)
					Log(ConsoleManager.LogType.Info, $"My Turn. presented word is {presentedWord.Content} (Subsitution: {presentedWord.Substitution})");
				else
					Log(ConsoleManager.LogType.Info, $"My Turn. presented word is {presentedWord.Content}");
				CurrentPresentedWord = presentedWord;
				if (MyTurnEvent != null)
					MyTurnEvent(this, new MyTurnEventArgs(presentedWord, CurrentMissionChar));
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

		private void GetCurrentMissionWord()
		{
			string missionWord = GetMissionWord();
			if (string.IsNullOrWhiteSpace(missionWord) || string.Equals(missionWord, _current_mission_word, StringComparison.InvariantCulture))
				return;
			Log(ConsoleManager.LogType.Info, "Mission Word Changed: " + missionWord);
			_current_mission_word = missionWord;
		}

		private void GetCurrentRound()
		{
			string round = GetGameRound();
			if (string.IsNullOrWhiteSpace(round) || string.Equals(round, _roundCache, StringComparison.InvariantCulture))
				return;
			Log(ConsoleManager.LogType.Info, "Round Changed: " + round);
			if (RoundChangeEvent != null)
				RoundChangeEvent(this, EventArgs.Empty);
			PathFinder.PreviousPath = new List<string>();
			_roundCache = round;
		}

		private void CheckUnsupportedWord()
		{
			string unsupportedWord = GetUnsupportedWord();
			if (string.IsNullOrWhiteSpace(unsupportedWord) || string.Equals(unsupportedWord, _unsupportedWordCache, StringComparison.InvariantCultureIgnoreCase) || unsupportedWord.Contains("T.T"))
				return;

			bool isExistingWord = unsupportedWord.Contains(":"); // 첫 턴 한방 금지, 한방 단어(매너) 등등...
			_unsupportedWordCache = unsupportedWord;

			if (WrongWordEvent != null)
				WrongWordEvent(this, new WrongWordEventArgs(unsupportedWord, isExistingWord));
			if (IsMyTurn && MyPathIsWrongEvent != null)
				MyPathIsWrongEvent(this, new WrongWordEventArgs(unsupportedWord, isExistingWord));
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

		void Log(ConsoleManager.LogType logtype, string Content) => ConsoleManager.Log(logtype, Content, $"{GetHandlerName()} - #{_watchdogTask.Id.ToString()}");

		private ResponsePresentedWord GetPresentedWord()
		{
			return new ResponsePresentedWord(GetGamePresentedWord().Trim());
		}

		public class ResponsePresentedWord
		{
			public string Content;
			public bool CanSubstitution;
			public string Substitution;

			public ResponsePresentedWord(string content, bool canSubsitution, string substituation = "")
			{
				Content = content;
				CanSubstitution = canSubsitution;
				if (!CanSubstitution)
					return;
				Substitution = substituation;
			}

			public ResponsePresentedWord(string fullContent)
			{
				CanSubstitution = fullContent.Length >= 4 && fullContent[1] == '(';
				if (CanSubstitution)
				{
					Content = fullContent[0].ToString();
					Substitution = fullContent[2].ToString();
				}
				else
				{
					Content = fullContent;
					CanSubstitution = false;
				}
			}

			public override bool Equals(object obj)
			{
				if (!(obj is ResponsePresentedWord))
					return false;
				ResponsePresentedWord other = (ResponsePresentedWord)obj;
				return string.Equals(Content, other.Content, StringComparison.InvariantCultureIgnoreCase) && Substitution == other.Substitution && (!CanSubstitution || CanSubstitution && string.Equals(Substitution, other.Substitution, StringComparison.InvariantCultureIgnoreCase));
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hash = 3049;
					hash = hash * 5039 + Content.GetHashCode();
					hash = hash * 883 + CanSubstitution.GetHashCode();
					if (CanSubstitution)
						hash = hash * 9719 + Substitution.GetHashCode();
					return hash;
				}
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
			public Boolean IsExistingWord;

			public WrongWordEventArgs(string word, bool isExistingWord)
			{
				Word = word;
				IsExistingWord = isExistingWord;
			}
		}

		// These methods should be overridded
		public abstract string GetSiteURLPattern();
		public abstract string GetHandlerName();

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

		public string GetUnsupportedWord()
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
