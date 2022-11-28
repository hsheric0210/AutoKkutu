using AutoKkutu.Constants;
using AutoKkutu.Handlers;
using AutoKkutu.Modules;
using AutoKkutu.Utils;
using Serilog;
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

		private string LastChat = "";

		public event EventHandler? GameStarted;

		public event EventHandler? GameEnded;

		public event EventHandler<WordPresentEventArgs>? MyTurn;

		public event EventHandler? MyTurnEnded;

		public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;

		public event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;

		public event EventHandler? RoundChange;

		public event EventHandler<GameModeChangeEventArgs>? GameModeChange;

		public event EventHandler<WordPresentEventArgs>? TypingWordPresented;

		public event EventHandler? ChatUpdated;

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

		public void StartWatchdog()
		{
			if (!_isWatchdogStarted)
			{
				_isWatchdogStarted = true;

				cancelTokenSrc = new CancellationTokenSource();
				CancellationToken token = cancelTokenSrc.Token;

				_mainWatchdogTask = new Task(async () => await WatchdogPrimary(token), token);
				_mainWatchdogTask.Start();

				Task.Run(async () => await WatchdogAssistant("History", GetPreviousWord, token));
				Task.Run(async () => await WatchdogAssistant("Round", GetCurrentRound,  token));
				Task.Run(async () => await WatchdogAssistant("Mission word", GetCurrentMissionWord,  token));
				Task.Run(async () => await WatchdogAssistant("Unsupported word", CheckUnsupportedWord,  token));
				Task.Run(async () => await WatchdogAssistant("Example word", CheckExample,  token));
				Task.Run(async () => await WatchdogGameMode(token));
				Task.Run(async () => await AssistantWatchdog("My turn", () => CheckGameTurn(), token));
				Task.Run(async () => await WatchdogPresentWord(token));

				Log.Information("Watchdog threads are started.");
			}
		}

		public void StopWatchdog()
		{
			if (_isWatchdogStarted)
			{
				Log.Information("Watchdog stop requested.");
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
			await Watchdog(async () =>
			{
				CheckGameStarted();
				await Task.Delay(IsGameStarted ? _ingame_interval : _checkgame_interval, cancelToken);
			}, ex => Log.Error(ex, "Main watchdog task interrupted."), cancelToken);
		}

		private async Task AssistantWatchdog(string watchdogName, Action action, CancellationToken cancelToken)
		{
			await Watchdog(async () =>
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
				}, ex => Log.Error(ex, "{0} watchdog task interrupted.", watchdogName), cancelToken);
		}

		private async Task WatchdogGameMode(CancellationToken cancelToken)
		{
			await Watchdog(async () =>
				{
					CheckGameMode();
					await Task.Delay(_checkgame_interval, cancelToken);
				}, ex => Log.Error(ex, "GameMode watchdog task interrupted."), cancelToken);
		}

		// 참고: 이 와치독은 '타자 대결' 모드에서만 사용됩니다
		private async Task WatchdogPresentWord(CancellationToken cancelToken)
		{
			await Watchdog(async () =>
				{
					if (AutoKkutuMain.Configuration.GameMode == GameMode.TypingBattle && IsGameStarted)
					{
						if (_isMyTurn)
							GetCurrentTypingWord();
						await Task.Delay(_ingame_interval, cancelToken);
					}
					else
					{
						await Task.Delay(_checkgame_interval, cancelToken);
					}
				}, ex => Log.Error(ex, "Present word watchdog task interrupted."), cancelToken);
		}

		private async Task WatchdogAssistant(string watchdogName, Action task, CancellationToken cancelToken) => await AssistantWatchdog(watchdogName, () => task.Invoke(), cancelToken);

		private void CheckGameStarted()
		{
			if (IsGameNotInProgress())
			{
				if (!IsGameStarted)
					return;

				Log.Debug("Game ended.");
				GameEnded?.Invoke(this, EventArgs.Empty);
				IsGameStarted = false;
			}
			else if (!IsGameStarted)
			{
				RegisterJSFunction(CurrentRoundIndexFunc, "", "return Array.from(document.querySelectorAll('#Middle > div.GameBox.Product > div > div.game-head > div.rounds label')).indexOf(document.querySelector('.rounds-current'));");
				Log.Debug("New game started; Previous word list flushed.");
				GameStarted?.Invoke(this, EventArgs.Empty);
				IsGameStarted = true;
			}
		}

		private void CheckGameTurn()
		{
			if (IsGameNotInMyTurn())
			{
				if (!IsMyTurn)
					return;

				_isMyTurn = false;
				Log.Debug("My turn ended.");
				MyTurnEnded?.Invoke(this, EventArgs.Empty);
			}
			else if (!_isMyTurn)
			{
				_isMyTurn = true;
				ResponsePresentedWord? presentedWord = GetPresentedWord();
				if (presentedWord == null)
					return;

				if (presentedWord.CanSubstitution)
					Log.Information("My turn arrived, presented word is {word} (Subsitution: {subsituation})", presentedWord.Content, presentedWord.Substitution);
				else
					Log.Information("My turn arrived, presented word is {word}.", presentedWord.Content);
				CurrentPresentedWord = presentedWord;
				MyTurn?.Invoke(this, new WordPresentEventArgs(presentedWord, CurrentMissionChar));
			}
		}

		/// <summary>
		/// 이전에 제시된 단어들의 목록을 읽어들입니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void GetPreviousWord()
		{
			if (ConfigEnums.IsFreeMode(AutoKkutuMain.Configuration.GameMode))
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
					Log.Information("Found previous word : {word}", word);

					if (!PathManager.NewPathList.Contains(word))
						PathManager.NewPathList.Add(word);
					PathManager.AddPreviousPath(word);
				}
			}

			Array.Copy(tmpWordCache, _wordCache, 6);
		}

		/// <summary>
		/// 현재 미션 단어를 읽어들입니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void GetCurrentMissionWord()
		{
			string missionWord = GetMissionWord();
			if (string.IsNullOrWhiteSpace(missionWord) || string.Equals(missionWord, CurrentMissionChar, StringComparison.Ordinal))
				return;
			Log.Information("Mission word change detected : {word}", missionWord);
			CurrentMissionChar = missionWord;
		}

		/// <summary>
		/// 현재 게임의 라운드를 읽어들이고, 만약 바뀌었으면 이벤트를 호출합니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void GetCurrentRound()
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
			Log.Information("Round Changed : Index {0} Word {1}", roundIndex, roundText);
			RoundChange?.Invoke(this, new RoundChangeEventArgs(roundIndex, roundText));
			PathManager.ResetPreviousPath();
		}

		/// <summary>
		/// 현재 입력된 단어가 틀렸는지 검사하고, 이벤트를 호출합니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void CheckUnsupportedWord()
		{
			string unsupportedWord = GetUnsupportedWord();
			if (string.IsNullOrWhiteSpace(unsupportedWord) || string.Equals(unsupportedWord, _unsupportedWordCache, StringComparison.OrdinalIgnoreCase) || unsupportedWord.Contains("T.T", StringComparison.OrdinalIgnoreCase))
				return;

			bool isExistingWord = unsupportedWord.Contains(':', StringComparison.Ordinal); // 첫 턴 한방 금지, 한방 단어(매너) 등등...
			_unsupportedWordCache = unsupportedWord;

			UnsupportedWordEntered?.Invoke(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
			if (IsMyTurn && MyPathIsUnsupported != null)
				MyPathIsUnsupported(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
		}

		/// <summary>
		/// 라운드가 끝났을 때, 회색으로 옅게 제시되는 예시 단어를 읽어들입니다.
		/// </summary>
		/// <param name="watchdogID">현재 와치독 스레드의 ID</param>
		private void CheckExample()
		{
			string example = GetExampleWord();
			if (string.IsNullOrWhiteSpace(example) || example.StartsWith("게임 끝", StringComparison.Ordinal))
				return;
			if (string.Equals(example, _exampleWordCache, StringComparison.OrdinalIgnoreCase))
				return;
			_exampleWordCache = example;
			Log.Information("Path example detected : {word}", example);
			PathManager.NewPathList.Add(example);
		}

		private void GetCurrentTypingWord()
		{
			string word = GetGamePresentedWord();
			if (string.IsNullOrWhiteSpace(word) || word.StartsWith("게임 끝", StringComparison.InvariantCultureIgnoreCase))
				return;
			if (word.Contains(' ', StringComparison.Ordinal))
				word = word[..word.IndexOf(' ', StringComparison.Ordinal)];
			if (string.Equals(word, _currentPresentedWordCache, StringComparison.OrdinalIgnoreCase))
				return;
			_currentPresentedWordCache = word;
			Log.Information("Word detected : {word}", word);
			TypingWordPresented?.Invoke(this, new WordPresentEventArgs(new ResponsePresentedWord(word, false), ""));
		}

		private void CheckGameMode()
		{
			GameMode gameMode = GetCurrentGameMode();
			if (gameMode == _gameModeCache)
				return;
			_gameModeCache = gameMode;
			Log.Information("Game mode change detected : {gameMode}", ConfigEnums.GetGameModeName(gameMode));
			GameModeChange?.Invoke(this, new GameModeChangeEventArgs(gameMode));
		}

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
				var converted = PathManager.ConvertToPresentedWord(content);
				if (converted == null)
					return null;
				primary = converted;
			}

			return new ResponsePresentedWord(primary, hasSecondary, secondary);
		}

		protected static bool EvaluateJSReturnError(string javaScript, out string error) => JSEvaluator.EvaluateJSReturnError(javaScript, out error);

		protected string EvaluateJS(string javaScript, string? moduleName = null, string defaultResult = " ") => JSEvaluator.EvaluateJS(javaScript, defaultResult);

		protected int EvaluateJSInt(string javaScript, string? moduleName = null, int defaultResult = -1) => JSEvaluator.EvaluateJSInt(javaScript, defaultResult);

		protected bool EvaluateJSBool(string javaScript, string? moduleName = null, bool defaultResult = false) => JSEvaluator.EvaluateJSBool(javaScript, defaultResult);

		protected void RegisterJSFunction(string funcName, string funcArgs, string funcBody)
		{
			if (!RegisteredFunctionNames.ContainsKey(funcName))
				RegisteredFunctionNames[funcName] = $"__{RandomUtils.GenerateRandomString(64, true)}";

			string realFuncName = RegisteredFunctionNames[funcName];
			if (EvaluateJSBool($"typeof {realFuncName} != 'function'"))
			{
				if (EvaluateJSReturnError($"function {realFuncName}({funcArgs}) {{{funcBody}}}", out string error))
					Log.Error("Failed to register JavaScript function {funcName} : {error:l}", funcName, error);
				else
					Log.Information("Registered JavaScript function {funcName} : {realFuncName:l}()", funcName, realFuncName);
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

		public void UpdateChat(string input)
		{
			UpdateChatInternal(input);
			LastChat = input;
			ChatUpdated?.Invoke(this, EventArgs.Empty);
		}

		public void AppendChat(JamoType type, char ch)
		{
			UpdateChat(LastChat.AppendChar(type, ch));
		}

		public void ClickSubmitButton()
		{
			ClickSubmitButtonInternal();
			LastChat = "";
		}

		protected virtual void UpdateChatInternal(string input)
		{
			EvaluateJS($"document.querySelector('[id=\"Talk\"]').value='{input?.Trim()}'", "UpdateChat");
		}

		protected virtual void ClickSubmitButtonInternal()
		{
			EvaluateJS("document.getElementById('ChatBtn').click()", "PressEnterButton");
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
