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

		public bool IsGameStarted => _isGameStarted;

		public bool IsMyTurn => _isMyTurn;

		private Task _mainWatchdogTask;

		private CancellationTokenSource cancelTokenSrc;
		private readonly int _checkgame_interval = 3000;
		private readonly int _ingame_interval = 1;
		private bool _isGameStarted = false;
		private bool _isMyTurn = false;
		private bool _isWatchdogStarted = false;
		private string _current_mission_word = "";
		private string[] _wordCache = new string[6];
		private int _roundIndexCache = 0;
		private string _unsupportedWordCache = "";
		private string _exampleWordCache = "";
		private string _currentPresentedWordCache = "";
		private GameMode _gameModeCache = GameMode.Last_and_First;

		public event EventHandler onGameStarted;
		public event EventHandler onGameEnded;
		public event EventHandler<WordPresentEventArgs> onMyTurn;
		public event EventHandler onMyTurnEnded;
		//public event EventHandler onRoundEnded;
		//public event EventHandler onPastDictionary;
		public event EventHandler<UnsupportedWordEventArgs> onUnsupportedWordEntered;
		public event EventHandler<UnsupportedWordEventArgs> onMyPathIsUnsupported;
		public event EventHandler onRoundChange;
		public event EventHandler<GameModeChangeEventArgs> onGameModeChange;

		// 참고: 이 메서드는 '타자 대결' 모드에서만 사용됩니다
		public event EventHandler<WordPresentEventArgs> onWordPresented;

		private static Config CurrentConfig;

		private string _currentRoundIndexFuncName;

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

		public static void UpdateConfig(Config newConfig) => CurrentConfig = newConfig;

		public CommonHandler(ChromiumWebBrowser browser) => Browser = browser;

		public void StartWatchdog()
		{
			if (!_isWatchdogStarted)
			{
				_isWatchdogStarted = true;

				cancelTokenSrc = new CancellationTokenSource();
				CancellationToken token = cancelTokenSrc.Token;

				_mainWatchdogTask = new Task(async () => await MainWatchdog(token), token);
				_mainWatchdogTask.Start();

				int mainWatchdogID = CurrentMainWatchdogID;
				Task.Run(async () => await AssistantWatchdog("History", GetPreviousWord, mainWatchdogID, token));
				Task.Run(async () => await AssistantWatchdog("Round", GetCurrentRound, mainWatchdogID, token));
				Task.Run(async () => await AssistantWatchdog("Mission word", GetCurrentMissionWord, mainWatchdogID, token));
				Task.Run(async () => await AssistantWatchdog("Unsupported word", CheckUnsupportedWord, mainWatchdogID, token));
				Task.Run(async () => await AssistantWatchdog("Example word", CheckExample, mainWatchdogID, token));
				Task.Run(async () => await GameModeWatchdog(token, mainWatchdogID));
				Task.Run(async () => await AssistantWatchdog("My turn", () => CheckGameState(CheckType.MyTurn, mainWatchdogID), token, mainWatchdogID));
				Task.Run(async () => await PresentWordWatchdog(token, mainWatchdogID));

				GetLogger(mainWatchdogID).Info("Watchdog threads are started.");
			}
		}

		public void StopWatchdog()
		{
			if (_isWatchdogStarted)
			{
				GetLogger(CurrentMainWatchdogID).Info("Watchdog stop requested.");
				cancelTokenSrc?.Cancel();
				_isWatchdogStarted = false;
			}
		}

		private async Task MainWatchdog(CancellationToken cancelToken)
		{
			int mainWatchdogID = CurrentMainWatchdogID;
			try
			{
				cancelToken.ThrowIfCancellationRequested();

				while (true)
				{
					if (cancelToken.IsCancellationRequested)
						cancelToken.ThrowIfCancellationRequested();

					CheckGameState(CheckType.GameStarted, mainWatchdogID);
					if (_isGameStarted)
						await Task.Delay(_ingame_interval, cancelToken);
					else
						await Task.Delay(_checkgame_interval, cancelToken);
				}
			}
			catch (Exception ex)
			{
				if (!(ex is OperationCanceledException) && !(ex is TaskCanceledException))
					GetLogger(mainWatchdogID).Error("Main watchdog task terminated", ex);
			}
		}

		private async Task AssistantWatchdog(string watchdogName, Action action, CancellationToken cancelToken, int mainWatchdogID = -1)
		{
			try
			{
				cancelToken.ThrowIfCancellationRequested();

				while (true)
				{
					if (cancelToken.IsCancellationRequested)
						cancelToken.ThrowIfCancellationRequested();

					if (_isGameStarted)
					{
						action.Invoke();
						await Task.Delay(_ingame_interval, cancelToken);
					}
					else
						await Task.Delay(_checkgame_interval, cancelToken);
				}
			}
			catch (Exception ex)
			{
				if (!(ex is OperationCanceledException) && !(ex is TaskCanceledException))
					GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID).Error($"{watchdogName} watchdog task terminated", ex);
			}
		}

		private async Task GameModeWatchdog(CancellationToken cancelToken, int mainWatchdogID = -1)
		{
			try
			{
				cancelToken.ThrowIfCancellationRequested();

				while (true)
				{
					if (cancelToken.IsCancellationRequested)
						cancelToken.ThrowIfCancellationRequested();

					CheckGameMode(mainWatchdogID);
					await Task.Delay(_checkgame_interval, cancelToken);
				}
			}
			catch (Exception ex)
			{
				if (!(ex is OperationCanceledException) && !(ex is TaskCanceledException))
					GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID).Error($"GameMode watchdog task terminated", ex);
			}
		}

		// 참고: 이 와치독은 '타자 대결' 모드에서만 사용됩니다
		private async Task PresentWordWatchdog(CancellationToken cancelToken, int mainWatchdogID = -1)
		{
			try
			{
				cancelToken.ThrowIfCancellationRequested();

				while (true)
				{
					if (cancelToken.IsCancellationRequested)
						cancelToken.ThrowIfCancellationRequested();

					if (CurrentConfig.Mode == GameMode.Typing_Battle && _isGameStarted)
					{
						if (_isMyTurn)
							GetCurrentPresentedWord(mainWatchdogID);
						await Task.Delay(_ingame_interval, cancelToken);
					}
					else
						await Task.Delay(_checkgame_interval, cancelToken);
				}
			}
			catch (Exception ex)
			{
				if (!(ex is OperationCanceledException) && !(ex is TaskCanceledException))
					GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID).Error($"Present word watchdog task terminated", ex);
			}
		}

		private async Task AssistantWatchdog(string watchdogName, Action<int> task, int mainWatchdogID, CancellationToken cancelToken)
		{
			await AssistantWatchdog(watchdogName, () => task.Invoke(mainWatchdogID), cancelToken, mainWatchdogID);
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
				GetLogger(CurrentMainWatchdogID).Error("Failed to run script on site.", ex);
				return " ";
			}
		}

		protected int EvaluateJSInt(string javaScript)
		{
			try
			{
				var result = Browser.EvaluateScriptAsync(javaScript)?.Result;
				if (!string.IsNullOrWhiteSpace(result?.Message ?? ""))
					GetLogger(CurrentMainWatchdogID).Warn(result.Message);
				return Convert.ToInt32(result?.Result);
			}
			catch (NullReferenceException)
			{
				return -1;
			}
			catch (Exception ex)
			{
				GetLogger(CurrentMainWatchdogID).Error("Failed to run script on site.", ex);
				return -2;
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
					_isGameStarted = false;
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
				if (_isGameStarted)
					return;
				if (_currentRoundIndexFuncName != null)
				{
					_currentRoundIndexFuncName = $"__{Utils.GenerateRandomString(new Random(), 64, true)}()";
					Task.Run(() =>
					{
						var result = Browser.EvaluateScriptAsync($"function {_currentRoundIndexFuncName} {{ var maxIndex = document.querySelectorAll('#Middle > div.GameBox.Product > div > div.game-head > div.rounds > label').length, index = 1; while (index <= maxIndex) {{ if (document.querySelector('#Middle > div.GameBox.Product > div > div.game-head > div.rounds :nth-child(' + index.toString() + ')').className == 'rounds-current') {{ return index; }} else {{ index++; }} }} return -1; }}").Result;
						if (!string.IsNullOrWhiteSpace(result?.Message ?? ""))
							GetLogger(watchdogID).Error("Failed to register currentRoundIndexFunc: " + result.Message);
						else
							GetLogger(watchdogID).Info($"Register currentRoundIndexFunc: {_currentRoundIndexFuncName}");

					});
				}
				GetLogger(watchdogID).Debug("New game started; Previous word list flushed.");
				if (onGameStarted != null)
					onGameStarted(this, EventArgs.Empty);
				_isGameStarted = true;
			}
			else if (!_isMyTurn)
			{
				ResponsePresentedWord presentedWord = GetPresentedWord();
				if (presentedWord.CanSubstitution)
					GetLogger(watchdogID).InfoFormat("My Turn. presented word is {0} (Subsitution: {1})", presentedWord.Content, presentedWord.Substitution);
				else
					GetLogger(watchdogID).InfoFormat("My Turn. presented word is {0}", presentedWord.Content);
				CurrentPresentedWord = presentedWord;
				if (onMyTurn != null)
					onMyTurn(this, new WordPresentEventArgs(presentedWord, CurrentMissionChar));
				_isMyTurn = true;
			}
		}

		private void GetPreviousWord(int watchdogID)
		{
			if (ConfigEnums.IsFreeMode(CurrentConfig.Mode))
				return;

			string[] tmpWordCache = new string[6];

			for (int index = 0; index < 6; index++)
			{
				string previousWord = GetGamePreviousWord(index);
				if (!string.IsNullOrWhiteSpace(previousWord) && previousWord.Contains('<'))
					tmpWordCache[index] = previousWord.Substring(0, previousWord.IndexOf('<')).Trim();
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
			int roundIndex = GetGameRoundIndex();
			if (roundIndex == _roundIndexCache)
				return;

			string roundText = GetGameRoundText();
			if (string.IsNullOrWhiteSpace(roundText))
				return;

			_roundIndexCache = roundIndex;

			if (roundIndex <= 0)
				return;
			GetLogger(watchdogID).InfoFormat("Round Changed : Index {0} Word {1}", roundIndex, roundText);
			if (onRoundChange != null)
				onRoundChange(this, new RoundChangeEventArgs(roundIndex, roundText));
			PathFinder.PreviousPath = new List<string>();
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
			if (string.IsNullOrWhiteSpace(example) || example.StartsWith("게임 끝"))
				return;
			if (string.Equals(example, _exampleWordCache, StringComparison.InvariantCultureIgnoreCase))
				return;
			_exampleWordCache = example;
			GetLogger(watchdogID).InfoFormat("Path example detected : {0}", example);
			PathFinder.NewPathList.Add(example);
		}

		// 참고: 이 메서드는 '타자 대결' 모드에서만 사용됩니다
		private void GetCurrentPresentedWord(int watchdogID)
		{
			string word = GetGamePresentedWord();
			if (string.IsNullOrWhiteSpace(word) || word.StartsWith("게임 끝"))
				return;
			if (word.Contains(' '))
				word = word.Substring(0, word.IndexOf(' '));
			if (string.Equals(word, _currentPresentedWordCache, StringComparison.InvariantCultureIgnoreCase))
				return;
			_currentPresentedWordCache = word;
			GetLogger(watchdogID).InfoFormat("Word detected : {0}", word);
			if (onWordPresented != null)
				onWordPresented(this, new WordPresentEventArgs(new ResponsePresentedWord(word, false), ""));
		}

		private void CheckGameMode(int watchdogID)
		{
			GameMode gameMode = GetCurrentGameMode();
			if (gameMode == _gameModeCache)
				return;
			_gameModeCache = gameMode;
			GetLogger(watchdogID).InfoFormat("GameMode Changed : {0}", ConfigEnums.GetGameModeName(gameMode));
			if (onGameModeChange != null)
				onGameModeChange(this, new GameModeChangeEventArgs(gameMode));
		}


		private int CurrentMainWatchdogID => _mainWatchdogTask == null ? -1 : _mainWatchdogTask.Id;

		private ResponsePresentedWord GetPresentedWord()
		{
			string content = GetGamePresentedWord().Trim();

			if (string.IsNullOrEmpty(content))
				return null;

			string primary;
			string secondary = null;
			bool hasSecondary = content.Contains('(') && content.Last() == ')';
			if (hasSecondary)
			{
				int parentheseStartIndex = content.IndexOf('(');
				int parentheseEndIndex = content.IndexOf(')');
				primary = content.Substring(0, parentheseStartIndex);
				secondary = content.Substring(parentheseStartIndex + 1, parentheseEndIndex - parentheseStartIndex - 1);
			}
			else if (content.Length <= 1)
				primary = content;
			else  // 가끔가다가 서버 랙때문에 '내가 입력해야할 단어의 조건' 대신 '이전 라운드에 입력되었었던 단어'가 나한테 넘어오는 경우가 있음
				primary = PathFinder.ConvertToWord(content);

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

		public class WordPresentEventArgs : EventArgs
		{
			public ResponsePresentedWord Word;
			public string MissionChar;

			public WordPresentEventArgs(ResponsePresentedWord word, string missionChar)
			{
				Word = word;
				MissionChar = missionChar;
			}
		}

		public class UnsupportedWordEventArgs : EventArgs
		{
			public string Word;
			public bool IsExistingWord;

			public UnsupportedWordEventArgs(string word, bool isExistingWord)
			{
				Word = word;
				IsExistingWord = isExistingWord;
			}
		}

		public class RoundChangeEventArgs : EventArgs
		{
			public int RoundIndex;
			public string RoundWord;

			public RoundChangeEventArgs(int roundIndex, string roundWord)
			{
				RoundIndex = roundIndex;
				RoundWord = roundWord;
			}
		}

		public class GameModeChangeEventArgs : EventArgs
		{
			public GameMode GameMode;

			public GameModeChangeEventArgs(GameMode gameMode) => GameMode = gameMode;
		}

		public string GetID() => $"{GetHandlerName()} - #{(_mainWatchdogTask == null ? "Global" : _mainWatchdogTask.Id.ToString())}";

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
			string element = EvaluateJS("document.getElementsByClassName('game-input')[0]");
			string displayOpt = EvaluateJS("document.getElementsByClassName('game-input')[0].style.display");
			return string.Equals(element, "undefined", StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrWhiteSpace(displayOpt) || displayOpt.Equals("none", StringComparison.InvariantCultureIgnoreCase);
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

		public string GetGameRoundText()
		{
			return EvaluateJS("document.getElementsByClassName('rounds-current')[0].textContent");
		}

		public int GetGameRoundIndex()
		{
			return EvaluateJSInt(_currentRoundIndexFuncName);
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
			return EvaluateJS("document.getElementsByClassName('items')[0].style.opacity") == "1" ? EvaluateJS("document.getElementsByClassName('items')[0].textContent") : "";
		}

		public void SendMessage(string input)
		{
			EvaluateJS($"document.querySelectorAll('[id=\"Talk\"]')[0].value='{input.Trim()}'"); // "UserMessage"
			EvaluateJS("document.getElementById('ChatBtn').click()");
		}

		public GameMode GetCurrentGameMode()
		{
			string roomMode = EvaluateJS("document.getElementsByClassName('room-head-mode')[0].textContent");
			if (!string.IsNullOrWhiteSpace(roomMode))
			{
				string trimmed = roomMode.Split('/')[0].Trim();
				switch (trimmed.Substring(trimmed.IndexOf(' ') + 1))
				{
					case "앞말잇기":
						return GameMode.First_and_Last;
					case "가운뎃말잇기":
						return GameMode.Middle_and_First;
					case "쿵쿵따":
						return GameMode.Kung_Kung_Tta;
					case "끄투":
						return GameMode.Kkutu;
					case "전체":
						return GameMode.All;
					case "자유":
						return GameMode.Free;
					case "자유 끝말잇기":
						return GameMode.Free_Last_and_First;
					case "타자 대결":
						return GameMode.Typing_Battle;
				}
			}
			return GameMode.Last_and_First;
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
