using AutoKkutu.Handlers;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using log4net;

namespace AutoKkutu
{
	public abstract class CommonHandler
	{
		private ILog GetLogger(int watchdogID) => LogManager.GetLogger($"{GetHandlerName()} - #{watchdogID}");

		public ChromiumWebBrowser Browser;
		public bool IsWatchdogAlive;

		public ResponsePresentedWord CurrentPresentedWord = null;
		public string CurrentMissionChar => _current_mission_word;

		public bool IsGameStarted => _isgamestarted;

		public bool IsMyTurn => _isMyTurn;

		private Task _watchdogTask;

		private CancellationTokenSource cancelTokenSrc;
		private readonly int _checkgame_interval = 3000;
		private readonly int _ingame_interval = 1;
		private bool _isgamestarted = false;
		private bool _isMyTurn = false;
		private bool _isWatchdogStarted = false;
		private string _current_mission_word = "";
		private string[] _wordCache = new string[6];
		private string _roundCache = "";
		private string _unsupportedWordCache = "";
		private string _exampleWordCache = "";

		public event EventHandler onGameStarted;
		public event EventHandler onGameEnded;
		public event EventHandler<MyTurnEventArgs> onMyTurn;
		public event EventHandler onMyTurnEnded;
		//public event EventHandler onRoundEnded;
		//public event EventHandler onPastDictionary;
		public event EventHandler<UnsupportedWordEventArgs> onUnsupportedWordEntered;
		public event EventHandler<UnsupportedWordEventArgs> onMyPathIsUnsupported;
		public event EventHandler onRoundChange;

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
			if (!string.IsNullOrEmpty(url))
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
				_isWatchdogStarted = true;
				cancelTokenSrc = new CancellationTokenSource();
				_watchdogTask = new Task(async () => await Watchdog(cancelTokenSrc.Token), cancelTokenSrc.Token);
				_watchdogTask.Start();
				GetLogger(CurrentWatchdogID).Info("Watchdog thread started.");
			}
		}
		public void StopWatchdog()
		{
			if (_isWatchdogStarted)
			{
				GetLogger(CurrentWatchdogID).Info("Watchdog thread stop requested.");
				cancelTokenSrc?.Cancel();
				_isWatchdogStarted = false;
			}
		}

		// TODO: 와치독 스레드를 기능별로 여러 개를 돌리면 어떨까?
		private async Task Watchdog(CancellationToken cancelToken)
		{
			int watchdogID = CurrentWatchdogID;
			try
			{
				cancelToken.ThrowIfCancellationRequested();

				while (true)
				{
					if (cancelToken.IsCancellationRequested)
						cancelToken.ThrowIfCancellationRequested();

					// 만약 누군가 이 Task.Run의 향연보다 더 좋은 방법을 알고 계신다면
					// 좀 고쳐주세요...
					await Task.Run(() => CheckGameState(CheckType.GameStarted, watchdogID));
					if (_isgamestarted)
					{
						await Task.WhenAll(
							Task.Run(() => GetPreviousWord(watchdogID)),
							Task.Run(() => GetCurrentRound(watchdogID)),
							Task.Run(() => GetCurrentMissionWord(watchdogID)),
							Task.Run(() => CheckUnsupportedWord(watchdogID)),
							Task.Run(() => CheckExample(watchdogID))
						);
						await Task.Run(() => CheckGameState(CheckType.MyTurn, watchdogID));
						await Task.Delay(_ingame_interval, cancelToken);
					}
					else
						await Task.Delay(_checkgame_interval, cancelToken);
				}
			}
			catch (Exception ex)
			{
				if (!(ex is OperationCanceledException) && !(ex is TaskCanceledException))
					GetLogger(watchdogID).Error("Watchdog thread terminated", ex);
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
				GetLogger(CurrentWatchdogID).Error("Failed to run script on site.", ex);
				return " ";
			}
		}

		private void CheckGameState(CheckType type, int watchdogID)
		{
			if (type == CheckType.GameStarted ? IsGameNotInProgress() : IsGameNotInMyTurn())
			{
				if (type == CheckType.GameStarted)
				{
					if (!IsGameStarted)
						return;
					GetLogger(watchdogID).Debug("Game ended.");
					if (onGameEnded != null)
						onGameEnded(this, EventArgs.Empty);
					_isgamestarted = false;
				}
				else if (IsMyTurn)
				{
					GetLogger(watchdogID).Debug("My turn ended.");
					if (onMyTurnEnded != null)
						onMyTurnEnded(this, EventArgs.Empty);
					_isMyTurn = false;
				}
			}
			else if (type == CheckType.GameStarted)
			{
				if (_isgamestarted)
					return;
				GetLogger(watchdogID).Debug("New round started; Previous word list flushed.");
				if (onGameStarted != null)
					onGameStarted(this, EventArgs.Empty);
				_isgamestarted = true;
			}
			else if (!_isMyTurn)
			{
				ResponsePresentedWord presentedWord = GetPresentedWord();
				if (presentedWord == null)
					return;
				if (presentedWord.CanSubstitution)
					GetLogger(watchdogID).InfoFormat("My Turn. presented word is {0} (Subsitution: {1})", presentedWord.Content, presentedWord.Substitution);
				else
					GetLogger(watchdogID).InfoFormat("My Turn. presented word is {0}", presentedWord.Content);
				CurrentPresentedWord = presentedWord;
				if (onMyTurn != null)
					onMyTurn(this, new MyTurnEventArgs(presentedWord, CurrentMissionChar));
				_isMyTurn = true;
			}
		}

		private void GetPreviousWord(int watchdogID)
		{
			string[] tmpWordCache = new string[6];

			for (int index = 0; index < 6; index++)
			{
				string previousWord = GetGamePreviousWord(index);
				if (!string.IsNullOrWhiteSpace(previousWord) && previousWord.Contains('<'))
					tmpWordCache[index] = previousWord.Substring(0, previousWord.IndexOf('<'));
			}

			for (int index = 0; index < 6; index++)
			{
				string word = tmpWordCache[index];
				if (!string.IsNullOrWhiteSpace(word) && !_wordCache.Contains(word))
				{
					GetLogger(watchdogID).InfoFormat("Found Previous Word : {0}", word);

					if (word != MainWindow.LastUsedPath && !PathFinder.NewPathList.Contains(word))
						PathFinder.NewPathList.Add(word);
					PathFinder.AddPreviousPath(word);
				}
			}

			Array.Copy(tmpWordCache, _wordCache, 6);
		}

		private void GetCurrentMissionWord(int watchdogID)
		{
			string missionWord = GetMissionWord();
			if (string.IsNullOrWhiteSpace(missionWord) || string.Equals(missionWord, _current_mission_word, StringComparison.InvariantCulture))
				return;
			GetLogger(watchdogID).InfoFormat("Mission Word Changed : {0}", missionWord);
			_current_mission_word = missionWord;
		}

		private void GetCurrentRound(int watchdogID)
		{
			string round = GetGameRound();
			if (string.IsNullOrWhiteSpace(round) || string.Equals(round, _roundCache, StringComparison.InvariantCulture))
				return;
			GetLogger(watchdogID).InfoFormat("Round Changed : {0}", round);
			if (onRoundChange != null)
				onRoundChange(this, EventArgs.Empty);
			PathFinder.PreviousPath = new List<string>();
			_roundCache = round;
		}

		private void CheckUnsupportedWord(int watchdogID)
		{
			string unsupportedWord = GetUnsupportedWord();
			if (string.IsNullOrWhiteSpace(unsupportedWord) || string.Equals(unsupportedWord, _unsupportedWordCache, StringComparison.InvariantCultureIgnoreCase) || unsupportedWord.Contains("T.T"))
				return;

			bool isExistingWord = unsupportedWord.Contains(":"); // 첫 턴 한방 금지, 한방 단어(매너) 등등...
			_unsupportedWordCache = unsupportedWord;

			if (onUnsupportedWordEntered != null)
				onUnsupportedWordEntered(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
			if (IsMyTurn && onMyPathIsUnsupported != null)
				onMyPathIsUnsupported(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
		}

		private void CheckExample(int watchdogID)
		{
			string example = GetExampleWord();
			if (string.IsNullOrWhiteSpace(example))
				return;
			if (string.Equals(example, _exampleWordCache, StringComparison.InvariantCultureIgnoreCase))
				return;
			_exampleWordCache = example;
			GetLogger(watchdogID).InfoFormat("Path example detected : {0}", example);
			PathFinder.NewPathList.Add(example);
		}

		private int CurrentWatchdogID => _watchdogTask == null ? -1 : _watchdogTask.Id;

		private ResponsePresentedWord GetPresentedWord()
		{
			string content = GetGamePresentedWord().Trim();

			if (string.IsNullOrEmpty(content))
				return null;

			string primary;
			string secondary = null;
			bool hasSecondary = content.Length >= 4 && content[1] == '(' && content[3] == ')';
			if (hasSecondary)
			{
				primary = content[0].ToString();
				secondary = content[2].ToString();
			}
			else if (content.Length <= 1) // 가끔가다가 서버 랙때문에 '내가 입력해야할 단어의 조건' 대신 '이전 라운드에 입력되었었던 단어'가 나한테 넘어오는 경우가 있음
			{
				primary = content;
			}
			else
				return null;

			return new ResponsePresentedWord(primary, hasSecondary, secondary);
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

		public class UnsupportedWordEventArgs : EventArgs
		{
			public string Word;
			public Boolean IsExistingWord;

			public UnsupportedWordEventArgs(string word, bool isExistingWord)
			{
				Word = word;
				IsExistingWord = isExistingWord;
			}
		}

		public string GetID() => $"{GetHandlerName()} - #{(_watchdogTask == null ? "Global" : _watchdogTask.Id.ToString())}";

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

		public string GetGamePreviousWord(int index)
		{
			if (index < 0 || index >= 6)
				throw new ArgumentOutOfRangeException($"index: {index}");
			if (EvaluateJS($"document.getElementsByClassName('ellipse history-item expl-mother')[{index}]").Equals("undefined"))
				return "";
			return EvaluateJS($"document.getElementsByClassName('ellipse history-item expl-mother')[{index}].innerHTML");
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

		public string GetRoomInfo()
		{
			string roomMode = EvaluateJS("document.getElementsByClassName('room-head-mode')[0].textContent");
			string roomLimit = EvaluateJS("document.getElementsByClassName('room-head-limit')[0].textContent");
			string roomRounds = EvaluateJS("document.getElementsByClassName('room-head-round')[0].textContent");
			string roomTime = EvaluateJS("document.getElementsByClassName('room-head-time')[0].textContent");
			return $"{roomMode} | {roomLimit} | {roomRounds} | {roomTime}";
		}
	}
}
