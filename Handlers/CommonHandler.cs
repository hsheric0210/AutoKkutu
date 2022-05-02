using AutoKkutu.Constants;
using AutoKkutu.Handlers;
using AutoKkutu.Utils;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public abstract class CommonHandler : IDisposable
	{
		// Frequently-used function names
		protected const string WriteInputFunc = "WriteInputFunc";

		protected const string ClickSubmitFunc = "ClickSubmitFunc";

		private const string CurrentRoundIndexFunc = "CurrentRoundIndexFunc";

		protected enum CheckType
		{
			GameStarted,

			MyTurn
		}

		protected ILog GetLogger(int? watchdogID = null, string watchdogType = null) => LogManager.GetLogger($"{GetHandlerName()}{(watchdogType == null ? "" : $" - {watchdogType}")} - #{watchdogID ?? CurrentMainWatchdogID}");

		private Dictionary<string, string> RegisteredFunctionNames = new Dictionary<string, string>();

		public bool IsWatchdogAlive;

		public ResponsePresentedWord CurrentPresentedWord;

		public string CurrentMissionChar => _current_mission_word;

		public bool IsGameStarted => _isGameStarted;

		

		public bool IsMyTurn => _isMyTurn;

		private Task _mainWatchdogTask;

		private CancellationTokenSource cancelTokenSrc;

		private readonly int _checkgame_interval = 3000;

		private readonly int _ingame_interval = 1;

		private bool _isGameStarted;

		private bool _isMyTurn;

		private bool _isWatchdogStarted;

		private string _current_mission_word = "";

		private string[] _wordCache = new string[6];

		private int _roundIndexCache;

		private string _unsupportedWordCache = "";

		private string _exampleWordCache = "";

		private string _currentPresentedWordCache = "";

		private GameMode _gameModeCache = GameMode.LastAndFirst;

		public event EventHandler onGameStarted;

		public event EventHandler onGameEnded;

		public event EventHandler<WordPresentEventArgs> onMyTurn;

		public event EventHandler onMyTurnEnded;

		public event EventHandler<UnsupportedWordEventArgs> onUnsupportedWordEntered;

		public event EventHandler<UnsupportedWordEventArgs> onMyPathIsUnsupported;

		public event EventHandler onRoundChange;

		public event EventHandler<GameModeChangeEventArgs> onGameModeChange;

		// 참고: 이 이벤트는 '타자 대결' 모드에서만 사용됩니다
		public event EventHandler<WordPresentEventArgs> onWordPresented;

		private static AutoKkutuConfiguration CurrentConfig;

		private static CommonHandler[] HANDLERS;

		public static void InitializeHandlers()
		{
			HANDLERS = new CommonHandler[5];
			HANDLERS[0] = new KkutuOrgHandler();
			HANDLERS[1] = new KkutuPinkHandler();
			HANDLERS[2] = new BFKkutuHandler();
			HANDLERS[3] = new KkutuCoKrHandler();
			HANDLERS[4] = new MusicKkutuHandler();
		}

		public static CommonHandler getHandler(string url)
		{
			if (!string.IsNullOrEmpty(url))
				foreach (CommonHandler handler in HANDLERS)
					if (Regex.Match(url, handler.GetSiteURLPattern()).Success)
						return handler;
			return null;
		}

		public static void UpdateConfig(AutoKkutuConfiguration newConfig) => CurrentConfig = newConfig;

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
					GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID, watchdogName).Error($"{watchdogName} watchdog task terminated", ex);
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
					GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID, "GameMode").Error("GameMode watchdog task terminated", ex);
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

					if (CurrentConfig.GameMode == GameMode.TypingBattle && _isGameStarted)
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
					GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID, "Present word").Error("Present word watchdog task terminated", ex);
			}
		}

		private async Task AssistantWatchdog(string watchdogName, Action<int> task, int mainWatchdogID, CancellationToken cancelToken)
		{
			await AssistantWatchdog(watchdogName, () => task.Invoke(mainWatchdogID), cancelToken, mainWatchdogID);
		}

		/// <summary>
		/// 현재 게임의 진행 상태를 모니터링합니다.
		/// </summary>
		/// <param name="type">검사 타입</param>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
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
					GetLogger(watchdogID, "Turn").Debug("My turn ended.");
					if (onMyTurnEnded != null)
						onMyTurnEnded(this, EventArgs.Empty);
					_isMyTurn = false;
				}
			}
			else if (type == CheckType.GameStarted)
			{
				if (_isGameStarted)
					return;

				RegisterJSFunction(CurrentRoundIndexFunc, "", "return Array.from(document.querySelectorAll('#Middle > div.GameBox.Product > div > div.game-head > div.rounds label')).indexOf(document.querySelector('.rounds-current'));");
				GetLogger(watchdogID).Debug("New game started; Previous word list flushed.");
				if (onGameStarted != null)
					onGameStarted(this, EventArgs.Empty);
				_isGameStarted = true;
			}
			else if (!_isMyTurn)
			{
				ResponsePresentedWord presentedWord = GetPresentedWord();
				if (presentedWord.CanSubstitution)
					GetLogger(watchdogID, "Turn").InfoFormat("My Turn. presented word is {0} (Subsitution: {1})", presentedWord.Content, presentedWord.Substitution);
				else
					GetLogger(watchdogID, "Turn").InfoFormat("My Turn. presented word is {0}", presentedWord.Content);
				CurrentPresentedWord = presentedWord;
				if (onMyTurn != null)
					onMyTurn(this, new WordPresentEventArgs(presentedWord, CurrentMissionChar));
				_isMyTurn = true;
			}
		}

		/// <summary>
		/// 이전에 제시된 단어들의 목록을 읽어들입니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void GetPreviousWord(int watchdogID)
		{
			if (ConfigEnums.IsFreeMode(CurrentConfig.GameMode))
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
					GetLogger(watchdogID, "Previous word").InfoFormat("Found Previous Word : {0}", word);

					if (!PathFinder.NewPathList.Contains(word))
						PathFinder.NewPathList.Add(word);
					PathFinder.AddPreviousPath(word);
				}
			}

			Array.Copy(tmpWordCache, _wordCache, 6);
		}

		/// <summary>
		/// 현재 미션 단어를 읽어들입니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void GetCurrentMissionWord(int watchdogID)
		{
			string missionWord = GetMissionWord();
			if (string.IsNullOrWhiteSpace(missionWord) || string.Equals(missionWord, _current_mission_word, StringComparison.InvariantCulture))
				return;
			GetLogger(watchdogID, "Mission word").InfoFormat("Mission Word Changed : {0}", missionWord);
			_current_mission_word = missionWord;
		}

		/// <summary>
		/// 현재 게임의 라운드를 읽어들이고, 만약 바뀌었으면 이벤트를 호출합니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
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
			GetLogger(watchdogID, "Round").InfoFormat("Round Changed : Index {0} Word {1}", roundIndex, roundText);
			if (onRoundChange != null)
				onRoundChange(this, new RoundChangeEventArgs(roundIndex, roundText));
			PathFinder.ResetPreviousPath();
		}

		/// <summary>
		/// 현재 입력된 단어가 틀렸는지 검사하고, 이벤트를 호출합니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
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

		/// <summary>
		/// 라운드가 끝났을 때, 회색으로 옅게 제시되는 예시 단어를 읽어들입니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void CheckExample(int watchdogID)
		{
			string example = GetExampleWord();
			if (string.IsNullOrWhiteSpace(example) || example.StartsWith("게임 끝"))
				return;
			if (string.Equals(example, _exampleWordCache, StringComparison.InvariantCultureIgnoreCase))
				return;
			_exampleWordCache = example;
			GetLogger(watchdogID, "Example").InfoFormat("Path example detected : {0}", example);
			PathFinder.NewPathList.Add(example);
		}

		/// <summary>
		/// 참고: 이 메서드는 '타자 대결' 모드에서만 사용됩니다
		/// </summary>
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
			GetLogger(watchdogID, "Presented word").InfoFormat("Word detected : {0}", word);
			if (onWordPresented != null)
				onWordPresented(this, new WordPresentEventArgs(new ResponsePresentedWord(word, false), ""));
		}

		private void CheckGameMode(int watchdogID)
		{
			GameMode gameMode = GetCurrentGameMode();
			if (gameMode == _gameModeCache)
				return;
			_gameModeCache = gameMode;
			GetLogger(watchdogID, "GameMode").InfoFormat("GameMode Changed : {0}", ConfigEnums.GetGameModeName(gameMode));
			if (onGameModeChange != null)
				onGameModeChange(this, new GameModeChangeEventArgs(gameMode));
		}

		protected int CurrentMainWatchdogID => _mainWatchdogTask == null ? -1 : _mainWatchdogTask.Id;

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
				primary = PathFinder.ConvertToPresentedWord(content);

			return new ResponsePresentedWord(primary, hasSecondary, secondary);
		}

		protected bool EvaluateJSReturnError(string javaScript, out string error) => JSEvaluator.EvaluateJSReturnError(javaScript, out error);

		protected string EvaluateJS(string javaScript, string moduleName = null, string defaultResult = " ") => JSEvaluator.EvaluateJS(javaScript, defaultResult, GetLogger(CurrentMainWatchdogID, moduleName));

		protected int EvaluateJSInt(string javaScript, string moduleName = null, int defaultResult = -1) => JSEvaluator.EvaluateJSInt(javaScript, defaultResult, GetLogger(CurrentMainWatchdogID, moduleName));

		protected bool EvaluateJSBool(string javaScript, string moduleName = null, bool defaultResult = false) => JSEvaluator.EvaluateJSBool(javaScript, defaultResult, GetLogger(CurrentMainWatchdogID, moduleName));

		protected void RegisterJSFunction(string funcName, string funcArgs, string funcBody)
		{
			if (!RegisteredFunctionNames.ContainsKey(funcName))
				RegisteredFunctionNames[funcName] = $"__{RandomUtils.GenerateRandomString(64, true)}";

			var realFuncName = RegisteredFunctionNames[funcName];
			if (EvaluateJSBool($"typeof {realFuncName} != 'function'"))
			{
				if (EvaluateJSReturnError($@"function {realFuncName}({funcArgs}) {{{funcBody}}}", out string error))
					GetLogger().ErrorFormat("Failed to register {0}: {1}", funcName, error);
				else
					GetLogger().InfoFormat("Register {0}: {1}()", funcName, realFuncName);
			}
		}

		protected string RegisteredJSFunctionName(string funcName)
		{
			return RegisteredFunctionNames[funcName];
		}

		public string GetID() => $"{GetHandlerName()} - #{(_mainWatchdogTask == null ? "Global" : _mainWatchdogTask.Id.ToString(CultureInfo.InvariantCulture))}";

		public abstract string GetSiteURLPattern();

		public abstract string GetHandlerName();

		public virtual bool IsGameNotInProgress()
		{
			string display = EvaluateJS("document.getElementsByClassName('GameBox Product')[0].style.display", "IsGameNotInProgress");
			string height = EvaluateJS("document.getElementsByClassName('GameBox Product')[0].style.height", "IsGameNotInProgress");
			return (string.IsNullOrWhiteSpace(height) || !string.IsNullOrWhiteSpace(display)) && (string.IsNullOrWhiteSpace(display) || display.Equals("none", StringComparison.OrdinalIgnoreCase));
		}

		public virtual bool IsGameNotInMyTurn()
		{
			string element = EvaluateJS("document.getElementsByClassName('game-input')[0]", "IsGameNotInMyTurn");
			string displayOpt = EvaluateJS("document.getElementsByClassName('game-input')[0].style.display", "IsGameNotInMyTurn");
			return string.Equals(element, "undefined", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(displayOpt) || displayOpt.Equals("none", StringComparison.OrdinalIgnoreCase);
		}

		public virtual string GetGamePresentedWord()
		{
			return EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent", "GetGamePresentedWord");
		}

		public virtual string GetGamePreviousWord(int index)
		{
			if (index < 0 || index >= 6)
				throw new ArgumentOutOfRangeException($"index: {index}");
			if (EvaluateJS($"document.getElementsByClassName('ellipse history-item expl-mother')[{index}]", "GetGamePreviousWord").Equals("undefined", StringComparison.OrdinalIgnoreCase))
				return "";
			return EvaluateJS($"document.getElementsByClassName('ellipse history-item expl-mother')[{index}].innerHTML", "GetGamePreviousWord");
		}

		public virtual string GetGameRoundText()
		{
			return EvaluateJS("document.getElementsByClassName('rounds-current')[0].textContent", "GetGameRoundText");
		}

		public virtual int GetGameRoundIndex()
		{
			return EvaluateJSInt($"{RegisteredJSFunctionName(CurrentRoundIndexFunc)}()", "GetGameRoundIndex");
		}

		public virtual string GetUnsupportedWord()
		{
			return EvaluateJS("document.getElementsByClassName('game-fail-text')[0]", "GetUnsupportedWord") != "undefined" ? EvaluateJS("document.getElementsByClassName('game-fail-text')[0].textContent", "GetUnsupportedWord") : "";
		}

		public virtual string GetExampleWord()
		{
			string innerHTML = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].innerHTML", "GetExampleWord");
			string content = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent", "GetExampleWord");
			return innerHTML.Contains("label") && innerHTML.Contains("color") && innerHTML.Contains("170,") && content.Length > 1 ? content : "";
		}

		public virtual string GetMissionWord()
		{
			return EvaluateJS("document.getElementsByClassName('items')[0].style.opacity", "GetMissionWord") == "1" ? EvaluateJS("document.getElementsByClassName('items')[0].textContent", "GetMissionWord") : "";
		}

		public virtual void SendMessage(string input)
		{
			EvaluateJS($"document.querySelectorAll('[id=\"Talk\"]')[0].value='{input?.Trim()}'", "SendMessage"); // "UserMessage"
			EvaluateJS("document.getElementById('ChatBtn').click()", "SendMessage");
		}

		public virtual GameMode GetCurrentGameMode()
		{
			string roomMode = EvaluateJS("document.getElementsByClassName('room-head-mode')[0].textContent");
			if (!string.IsNullOrWhiteSpace(roomMode))
			{
				string trimmed = roomMode.Split('/')[0].Trim();
				switch (trimmed.Substring(trimmed.IndexOf(' ') + 1))
				{
					case "앞말잇기":
						return GameMode.FirstAndLast;

					case "가운뎃말잇기":
						return GameMode.MiddleAddFirst;

					case "쿵쿵따":
						return GameMode.KungKungTta;

					case "끄투":
						return GameMode.Kkutu;

					case "전체":
						return GameMode.All;

					case "자유":
						return GameMode.Free;

					case "자유 끝말잇기":
						return GameMode.LastAndFirstFree;

					case "타자 대결":
						return GameMode.TypingBattle;
				}
			}
			return GameMode.LastAndFirst;
		}

		public virtual string GetRoomInfo()
		{
			string roomMode = EvaluateJS("document.getElementsByClassName('room-head-mode')[0].textContent");
			string roomLimit = EvaluateJS("document.getElementsByClassName('room-head-limit')[0].textContent");
			string roomRounds = EvaluateJS("document.getElementsByClassName('room-head-round')[0].textContent");
			string roomTime = EvaluateJS("document.getElementsByClassName('room-head-time')[0].textContent");
			return $"{roomMode} | {roomLimit} | {roomRounds} | {roomTime}";
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				cancelTokenSrc.Dispose();
				_mainWatchdogTask.Dispose();
			}
		}
	}

	public class ResponsePresentedWord
	{
		public string Content
		{
			get;
		}

		public bool CanSubstitution
		{
			get;
		}

		public string Substitution
		{
			get;
		}

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
			return string.Equals(Content, other.Content, StringComparison.OrdinalIgnoreCase) && Substitution == other.Substitution && (!CanSubstitution || string.Equals(Substitution, other.Substitution, StringComparison.OrdinalIgnoreCase));
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Content, CanSubstitution, Substitution);
		}
	}

	public class WordPresentEventArgs : EventArgs
	{
		public ResponsePresentedWord Word
		{
			get;
		}

		public string MissionChar
		{
			get;
		}

		public WordPresentEventArgs(ResponsePresentedWord word, string missionChar)
		{
			Word = word;
			MissionChar = missionChar;
		}
	}

	public class UnsupportedWordEventArgs : EventArgs
	{
		public string Word
		{
			get;
		}

		public bool IsExistingWord
		{
			get;
		}

		public UnsupportedWordEventArgs(string word, bool isExistingWord)
		{
			Word = word;
			IsExistingWord = isExistingWord;
		}
	}

	public class RoundChangeEventArgs : EventArgs
	{
		public int RoundIndex
		{
			get;
		}

		public string RoundWord
		{
			get;
		}

		public RoundChangeEventArgs(int roundIndex, string roundWord)
		{
			RoundIndex = roundIndex;
			RoundWord = roundWord;
		}
	}

	public class GameModeChangeEventArgs : EventArgs
	{
		public GameMode GameMode
		{
			get;
		}

		public GameModeChangeEventArgs(GameMode gameMode) => GameMode = gameMode;
	}
}
