using AutoKkutu.Constants;
using AutoKkutu.Handlers;
using AutoKkutu.Utils;
using NLog;
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

		protected Logger GetLogger(int? watchdogID = null, string? watchdogType = null) => LogManager.GetLogger($"{GetHandlerName()}{(watchdogType == null ? "" : $" - {watchdogType}")} - #{watchdogID ?? CurrentMainWatchdogID}");

		private readonly Dictionary<string, string> RegisteredFunctionNames = new();

		public ResponsePresentedWord? CurrentPresentedWord
		{
			get; private set;
		}

		public string? CurrentMissionChar
		{
			get; private set;
		}

		public bool IsGameStarted
		{
			get; private set;
		}

		public int TurnTimeMillis
		{
			get
			{
				if (!float.TryParse(GetTurnTime().TrimEnd('초'), out float turnTime) || !float.TryParse(GetRoundTime().TrimEnd('초'), out float roundTime))
					return 150000; // 150초 (라운드 시간 최댓값)
				return (int)(Math.Min(turnTime, roundTime) * 1000);
			}
		}

		public bool IsMyTurn => _isMyTurn;

		private Task? _mainWatchdogTask;

		private CancellationTokenSource? cancelTokenSrc;

		private readonly int _checkgame_interval = 3000;

		private readonly int _ingame_interval = 1;

		private bool _isMyTurn;

		private bool _isWatchdogStarted;

		private readonly string[] _wordCache = new string[6];

		private int _roundIndexCache;

		private string _unsupportedWordCache = "";

		private string _exampleWordCache = "";

		private string _currentPresentedWordCache = "";

		private GameMode _gameModeCache = GameMode.LastAndFirst;

		public event EventHandler? OnGameStarted;

		public event EventHandler? OnGameEnded;

		public event EventHandler<WordPresentEventArgs>? OnMyTurn;

		public event EventHandler? OnMyTurnEnded;

		public event EventHandler<UnsupportedWordEventArgs>? OnUnsupportedWordEntered;

		public event EventHandler<UnsupportedWordEventArgs>? OnMyPathIsUnsupported;

		public event EventHandler? OnRoundChange;

		public event EventHandler<GameModeChangeEventArgs>? OnGameModeChange;

		public event EventHandler<WordPresentEventArgs>? OnTypingWordPresented;

		private static AutoKkutuConfiguration? CurrentConfig;

		public static CommonHandler[] GetAvailableHandlers()
		{
			var handlers = new CommonHandler[5];
			handlers[0] = new KkutuOrgHandler();
			handlers[1] = new KkutuPinkHandler();
			handlers[2] = new BFKkutuHandler();
			handlers[3] = new KkutuCoKrHandler();
			handlers[4] = new MusicKkutuHandler();
			return handlers;
		}

		public static CommonHandler? GetHandler(string? site)
		{
			if (!string.IsNullOrEmpty(site))
			{
				foreach (CommonHandler handler in GetAvailableHandlers())
				{
					if (Regex.Match(site, handler.GetSitePattern()).Success)
						return handler;
				}
			}

			return null;
		}

		public static void UpdateConfig(AutoKkutuConfiguration newConfig) => CurrentConfig = newConfig;

		public void StartWatchdog()
		{
			CurrentConfig.RequireNotNull();

			if (!_isWatchdogStarted)
			{
				_isWatchdogStarted = true;

				cancelTokenSrc = new CancellationTokenSource();
				CancellationToken token = cancelTokenSrc.Token;

				_mainWatchdogTask = new Task(async () => await WatchdogPrimary(token), token);
				_mainWatchdogTask.Start();

				int mainWatchdogID = CurrentMainWatchdogID;
				Task.Run(async () => await WatchdogAssistant("History", GetPreviousWord, mainWatchdogID, token));
				Task.Run(async () => await WatchdogAssistant("Round", GetCurrentRound, mainWatchdogID, token));
				Task.Run(async () => await WatchdogAssistant("Mission word", GetCurrentMissionWord, mainWatchdogID, token));
				Task.Run(async () => await WatchdogAssistant("Unsupported word", CheckUnsupportedWord, mainWatchdogID, token));
				Task.Run(async () => await WatchdogAssistant("Example word", CheckExample, mainWatchdogID, token));
				Task.Run(async () => await WatchdogGameMode(token, mainWatchdogID));
				Task.Run(async () => await AssistantWatchdog("My turn", () => CheckGameTurn(mainWatchdogID), token, mainWatchdogID));
				Task.Run(async () => await WatchdogPresentWord(token, mainWatchdogID));

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

		private static async Task Watchdog(Func<Task> repeatTask, Action<Exception> onException, CancellationToken cancelToken)
		{
			try
			{
				cancelToken.ThrowIfCancellationRequested();

				while (true)
				{
					if (cancelToken.IsCancellationRequested)
						cancelToken.ThrowIfCancellationRequested();

					await repeatTask();
				}
			}
			catch (Exception ex)
			{
				if (ex is not OperationCanceledException and not TaskCanceledException)
					onException(ex);
			}
		}

		private async Task WatchdogPrimary(CancellationToken cancelToken)
		{
			int mainWatchdogID = CurrentMainWatchdogID;
			await Watchdog(async () =>
			{
				CheckGameStarted(mainWatchdogID);
				await Task.Delay(IsGameStarted ? _ingame_interval : _checkgame_interval, cancelToken);
			}, ex => GetLogger(mainWatchdogID).Error(ex, "Main watchdog task interrupted."), cancelToken);
		}

		private async Task AssistantWatchdog(string watchdogName, Action action, CancellationToken cancelToken, int mainWatchdogID = -1) => await Watchdog(async () =>
																																			{
																																				if (IsGameStarted)
																																				{
																																					action();
																																					await Task.Delay(_ingame_interval, cancelToken);
																																				}
																																				else
																																				{
																																					await Task.Delay(_checkgame_interval, cancelToken);
																																				}
																																			}, ex => GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID, watchdogName).Error(ex, "{0} watchdog task interrupted.", watchdogName), cancelToken);

		private async Task WatchdogGameMode(CancellationToken cancelToken, int mainWatchdogID = -1) => await Watchdog(async () =>
																									   {
																										   CheckGameMode(mainWatchdogID);
																										   await Task.Delay(_checkgame_interval, cancelToken);
																									   }, ex => GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID, "GameMode").Error(ex, CultureInfo.CurrentCulture, "GameMode watchdog task interrupted."), cancelToken);

		// 참고: 이 와치독은 '타자 대결' 모드에서만 사용됩니다
		private async Task WatchdogPresentWord(CancellationToken cancelToken, int mainWatchdogID = -1) => await Watchdog(async () =>
																										  {
																											  if (CurrentConfig?.GameMode == GameMode.TypingBattle && IsGameStarted)
																											  {
																												  if (_isMyTurn)
																													  GetCurrentTypingWord(mainWatchdogID);
																												  await Task.Delay(_ingame_interval, cancelToken);
																											  }
																											  else
																											  {
																												  await Task.Delay(_checkgame_interval, cancelToken);
																											  }
																										  }, ex => GetLogger(mainWatchdogID > 0 ? mainWatchdogID : CurrentMainWatchdogID, "Present word").Error(ex, "Present word watchdog task interrupted."), cancelToken);

		private async Task WatchdogAssistant(string watchdogName, Action<int> task, int mainWatchdogID, CancellationToken cancelToken) => await AssistantWatchdog(watchdogName, () => task.Invoke(mainWatchdogID), cancelToken, mainWatchdogID);

		private void CheckGameStarted(int watchdogID)
		{
			Logger logger = GetLogger(watchdogID);
			if (IsGameNotInProgress())
			{
				if (!IsGameStarted)
					return;

				logger.Debug("Game ended.");
				OnGameEnded?.Invoke(this, EventArgs.Empty);
				IsGameStarted = false;
			}
			else if (!IsGameStarted)
			{
				RegisterJSFunction(CurrentRoundIndexFunc, "", "return Array.from(document.querySelectorAll('#Middle > div.GameBox.Product > div > div.game-head > div.rounds label')).indexOf(document.querySelector('.rounds-current'));");
				logger.Debug("New game started; Previous word list flushed.");
				OnGameStarted?.Invoke(this, EventArgs.Empty);
				IsGameStarted = true;
			}
		}

		private void CheckGameTurn(int watchdogID)
		{
			Logger logger = GetLogger(watchdogID, "Turn");
			if (IsGameNotInMyTurn())
			{
				if (!IsMyTurn)
					return;

				_isMyTurn = false;
				logger.Debug("My turn ended.");
				OnMyTurnEnded?.Invoke(this, EventArgs.Empty);
			}
			else if (!_isMyTurn)
			{
				_isMyTurn = true;
				ResponsePresentedWord? presentedWord = GetPresentedWord();
				if (presentedWord == null)
					return;

				if (presentedWord.CanSubstitution)
					logger.Info(CultureInfo.CurrentCulture, "My turn arrived, presented word is {word} (Subsitution: {subsituation})", presentedWord.Content, presentedWord.Substitution);
				else
					logger.Info(CultureInfo.CurrentCulture, "My turn arrived, presented word is {word}.", presentedWord.Content);
				CurrentPresentedWord = presentedWord;
				OnMyTurn?.Invoke(this, new WordPresentEventArgs(presentedWord, CurrentMissionChar));
			}
		}

		/// <summary>
		/// 이전에 제시된 단어들의 목록을 읽어들입니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void GetPreviousWord(int watchdogID)
		{
			if (CurrentConfig == null || ConfigEnums.IsFreeMode(CurrentConfig.GameMode))
				return;

			string[] tmpWordCache = new string[6];

			for (int index = 0; index < 6; index++)
			{
				string previousWord = GetGamePreviousWord(index);
				if (!string.IsNullOrWhiteSpace(previousWord) && previousWord.Contains('<', StringComparison.Ordinal))
					tmpWordCache[index] = previousWord[..previousWord.IndexOf('<', StringComparison.Ordinal)].Trim();
			}

			for (int index = 0; index < 6; index++)
			{
				string word = tmpWordCache[index];
				if (!string.IsNullOrWhiteSpace(word) && !_wordCache.Contains(word))
				{
					GetLogger(watchdogID, "Previous word").Info(CultureInfo.CurrentCulture, "Found previous word : {word}", word);

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
			if (string.IsNullOrWhiteSpace(missionWord) || string.Equals(missionWord, CurrentMissionChar, StringComparison.Ordinal))
				return;
			GetLogger(watchdogID, "Mission word").Info(CultureInfo.CurrentCulture, "Mission word change detected : {word}", missionWord);
			CurrentMissionChar = missionWord;
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
			GetLogger(watchdogID, "Round").Info(CultureInfo.CurrentCulture, "Round Changed : Index {0} Word {1}", roundIndex, roundText);
			OnRoundChange?.Invoke(this, new RoundChangeEventArgs(roundIndex, roundText));
			PathFinder.ResetPreviousPath();
		}

		/// <summary>
		/// 현재 입력된 단어가 틀렸는지 검사하고, 이벤트를 호출합니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void CheckUnsupportedWord(int watchdogID)
		{
			string unsupportedWord = GetUnsupportedWord();
			if (string.IsNullOrWhiteSpace(unsupportedWord) || string.Equals(unsupportedWord, _unsupportedWordCache, StringComparison.OrdinalIgnoreCase) || unsupportedWord.Contains("T.T", StringComparison.OrdinalIgnoreCase))
				return;

			bool isExistingWord = unsupportedWord.Contains(':', StringComparison.Ordinal); // 첫 턴 한방 금지, 한방 단어(매너) 등등...
			_unsupportedWordCache = unsupportedWord;

			OnUnsupportedWordEntered?.Invoke(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
			if (IsMyTurn && OnMyPathIsUnsupported != null)
				OnMyPathIsUnsupported(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
		}

		/// <summary>
		/// 라운드가 끝났을 때, 회색으로 옅게 제시되는 예시 단어를 읽어들입니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void CheckExample(int watchdogID)
		{
			string example = GetExampleWord();
			if (string.IsNullOrWhiteSpace(example) || example.StartsWith("게임 끝", StringComparison.Ordinal))
				return;
			if (string.Equals(example, _exampleWordCache, StringComparison.OrdinalIgnoreCase))
				return;
			_exampleWordCache = example;
			GetLogger(watchdogID, "Example").Info(CultureInfo.CurrentCulture, "Path example detected : {word}", example);
			PathFinder.NewPathList.Add(example);
		}

		private void GetCurrentTypingWord(int watchdogID)
		{
			string word = GetGamePresentedWord();
			if (string.IsNullOrWhiteSpace(word) || word.StartsWith("게임 끝", StringComparison.InvariantCultureIgnoreCase))
				return;
			if (word.Contains(' ', StringComparison.Ordinal))
				word = word[..word.IndexOf(' ', StringComparison.Ordinal)];
			if (string.Equals(word, _currentPresentedWordCache, StringComparison.OrdinalIgnoreCase))
				return;
			_currentPresentedWordCache = word;
			GetLogger(watchdogID, "Presented word").Info(CultureInfo.CurrentCulture, "Word detected : {word}", word);
			OnTypingWordPresented?.Invoke(this, new WordPresentEventArgs(new ResponsePresentedWord(word, false), ""));
		}

		private void CheckGameMode(int watchdogID)
		{
			GameMode gameMode = GetCurrentGameMode();
			if (gameMode == _gameModeCache)
				return;
			_gameModeCache = gameMode;
			GetLogger(watchdogID, "GameMode").Info(CultureInfo.CurrentCulture, "Game mode change detected : {gameMode}", ConfigEnums.GetGameModeName(gameMode));
			OnGameModeChange?.Invoke(this, new GameModeChangeEventArgs(gameMode));
		}

		protected int CurrentMainWatchdogID => _mainWatchdogTask == null ? -1 : _mainWatchdogTask.Id;

		private ResponsePresentedWord? GetPresentedWord()
		{
			string content = GetGamePresentedWord().Trim();

			if (string.IsNullOrEmpty(content))
				return null;

			string primary;
			string secondary = string.Empty;
			bool hasSecondary = content.Contains('(', StringComparison.Ordinal) && content.Last() == ')';
			if (hasSecondary)
			{
				int parentheseStartIndex = content.IndexOf('(', StringComparison.Ordinal);
				int parentheseEndIndex = content.IndexOf(')', StringComparison.Ordinal);
				primary = content[..parentheseStartIndex];
				secondary = content.Substring(parentheseStartIndex + 1, parentheseEndIndex - parentheseStartIndex - 1);
			}
			else if (content.Length <= 1)
			{
				primary = content;
			}
			else  // 가끔가다가 서버 랙때문에 '내가 입력해야할 단어의 조건' 대신 '이전 라운드에 입력되었었던 단어'가 나한테 넘어오는 경우가 있음
			{
				var converted = PathFinder.ConvertToPresentedWord(content);
				if (converted == null)
					return null;
				primary = converted;
			}

			return new ResponsePresentedWord(primary, hasSecondary, secondary);
		}

		protected static bool EvaluateJSReturnError(string javaScript, out string error) => JSEvaluator.EvaluateJSReturnError(javaScript, out error);

		protected string EvaluateJS(string javaScript, string? moduleName = null, string defaultResult = " ") => JSEvaluator.EvaluateJS(javaScript, defaultResult, GetLogger(CurrentMainWatchdogID, moduleName));

		protected int EvaluateJSInt(string javaScript, string? moduleName = null, int defaultResult = -1) => JSEvaluator.EvaluateJSInt(javaScript, defaultResult, GetLogger(CurrentMainWatchdogID, moduleName));

		protected bool EvaluateJSBool(string javaScript, string? moduleName = null, bool defaultResult = false) => JSEvaluator.EvaluateJSBool(javaScript, defaultResult, GetLogger(CurrentMainWatchdogID, moduleName));

		protected void RegisterJSFunction(string funcName, string funcArgs, string funcBody)
		{
			if (!RegisteredFunctionNames.ContainsKey(funcName))
				RegisteredFunctionNames[funcName] = $"__{RandomUtils.GenerateRandomString(64, true)}";

			string realFuncName = RegisteredFunctionNames[funcName];
			if (EvaluateJSBool($"typeof {realFuncName} != 'function'"))
			{
				if (EvaluateJSReturnError($"function {realFuncName}({funcArgs}) {{{funcBody}}}", out string error))
					GetLogger().Error(CultureInfo.CurrentCulture, "Failed to register JavaScript function {funcName} : {error:l}", funcName, error);
				else
					GetLogger().Info(CultureInfo.CurrentCulture, "Registered JavaScript function {funcName} : {realFuncName:l}()", funcName, realFuncName);
			}
		}

		protected string GetRegisteredJSFunctionName(string funcName) => RegisteredFunctionNames[funcName];

		public string GetID() => $"{GetHandlerName()} - #{(_mainWatchdogTask == null ? "Global" : _mainWatchdogTask.Id.ToString(CultureInfo.InvariantCulture))}";

		public abstract string GetSitePattern();

		public abstract string GetHandlerName();

		public virtual bool IsGameNotInProgress()
		{
			string display = EvaluateJS("document.getElementsByClassName('GameBox Product')[0].style.display", nameof(IsGameNotInProgress));
			string height = EvaluateJS("document.getElementsByClassName('GameBox Product')[0].style.height", nameof(IsGameNotInProgress));
			return (string.IsNullOrWhiteSpace(height) || !string.IsNullOrWhiteSpace(display)) && (string.IsNullOrWhiteSpace(display) || display.Equals("none", StringComparison.OrdinalIgnoreCase));
		}

		public virtual bool IsGameNotInMyTurn()
		{
			string element = EvaluateJS("document.getElementsByClassName('game-input')[0]", nameof(IsGameNotInMyTurn));
			if (string.Equals(element, "undefined", StringComparison.OrdinalIgnoreCase))
				return true;

			string displayOpt = EvaluateJS("document.getElementsByClassName('game-input')[0].style.display", nameof(IsGameNotInMyTurn));
			return string.IsNullOrWhiteSpace(displayOpt) || displayOpt.Equals("none", StringComparison.OrdinalIgnoreCase);
		}

		public virtual string GetGamePresentedWord() => EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent", nameof(GetGamePresentedWord));

		public virtual string GetGamePreviousWord(int index)
		{
			if (index is < 0 or >= 6)
				throw new ArgumentOutOfRangeException($"index: {index}");
			if (EvaluateJS($"document.getElementsByClassName('ellipse history-item expl-mother')[{index}]", nameof(GetGamePreviousWord)).Equals("undefined", StringComparison.OrdinalIgnoreCase))
				return "";
			return EvaluateJS($"document.getElementsByClassName('ellipse history-item expl-mother')[{index}].innerHTML", nameof(GetGamePreviousWord));
		}

		public virtual string GetGameRoundText() => EvaluateJS("document.getElementsByClassName('rounds-current')[0].textContent", "GetGameRoundText");

		public virtual int GetGameRoundIndex() => EvaluateJSInt($"{GetRegisteredJSFunctionName(CurrentRoundIndexFunc)}()", "GetGameRoundIndex");

		public virtual string GetUnsupportedWord() => EvaluateJS("document.getElementsByClassName('game-fail-text')[0]", "GetUnsupportedWord") != "undefined" ? EvaluateJS("document.getElementsByClassName('game-fail-text')[0].textContent", "GetUnsupportedWord") : "";

		public virtual string GetExampleWord()
		{
			string innerHTML = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].innerHTML", "GetExampleWord");
			string content = EvaluateJS("document.getElementsByClassName('jjo-display ellipse')[0].textContent", "GetExampleWord");
			return innerHTML.Contains("label", StringComparison.OrdinalIgnoreCase)
				&& innerHTML.Contains("color", StringComparison.OrdinalIgnoreCase)
				&& innerHTML.Contains("170,", StringComparison.Ordinal)
				&& content.Length > 1 ? content : "";
		}

		public virtual string GetMissionWord() => EvaluateJS("document.getElementsByClassName('items')[0].style.opacity", "GetMissionWord") == "1" ? EvaluateJS("document.getElementsByClassName('items')[0].textContent", "GetMissionWord") : "";

		public virtual void SendMessage(string input)
		{
			EvaluateJS($"document.querySelector('[id=\"Talk\"]').value='{input?.Trim()}'", "SendMessage");
			EvaluateJS("document.getElementById('ChatBtn').click()", "SendMessage");
		}

		public virtual GameMode GetCurrentGameMode()
		{
			string roomMode = EvaluateJS("document.getElementsByClassName('room-head-mode')[0].textContent");
			if (!string.IsNullOrWhiteSpace(roomMode))
			{
				string trimmed = roomMode.Split('/')[0].Trim();
				switch (trimmed[(trimmed.IndexOf(' ', StringComparison.Ordinal) + 1)..])
				{
					case "앞말잇기":
						return GameMode.FirstAndLast;

					case "가운뎃말잇기":
						return GameMode.MiddleAndFirst;

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

		public virtual string GetTurnTime()
		{
			return EvaluateJS("document.querySelector('[class=\"graph jjo-turn-time\"] > [class=\"graph-bar\"]').textContent", "GetTurnTime");
		}

		public virtual string GetRoundTime()
		{
			return EvaluateJS("document.querySelector('[class=\"graph jjo-round-time\"] > [class=\"graph-bar round-extreme\"]').textContent", "GetRoundTime");
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
				cancelTokenSrc?.Dispose();
				_mainWatchdogTask?.Dispose();
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

		public string? Substitution
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

		public override bool Equals(object? obj) => obj is ResponsePresentedWord other
			&& string.Equals(Content, other.Content, StringComparison.OrdinalIgnoreCase)
			&& Substitution == other.Substitution
			&& (!CanSubstitution || string.Equals(Substitution, other.Substitution, StringComparison.OrdinalIgnoreCase));

		public override int GetHashCode() => HashCode.Combine(Content, CanSubstitution, Substitution);
	}

	public class WordPresentEventArgs : EventArgs
	{
		public ResponsePresentedWord Word
		{
			get;
		}

		public string? MissionChar
		{
			get;
		}

		public WordPresentEventArgs(ResponsePresentedWord word, string? missionChar)
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
