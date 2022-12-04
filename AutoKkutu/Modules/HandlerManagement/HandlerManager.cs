using AutoKkutu.Constants;
using AutoKkutu.Modules.HandlerManager.Handler;
using AutoKkutu.Modules.PathManager;
using AutoKkutu.Utils.Extension;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.HandlerManager
{
	[ModuleDependency(typeof(IPathManager))]
	public class HandlerManager : IHandlerManager
	{
		#region Game status properties
		public PresentedWord? CurrentPresentedWord
		{
			get; private set;
		}

		public string? CurrentMissionChar
		{
			get; private set;
		}

		public GameMode CurrentGameMode
		{
			get; private set;
		} = GameMode.LastAndFirst;

		public bool IsGameStarted
		{
			get; private set;
		}

		public bool IsMyTurn
		{
			get; private set;
		}

		public int TurnTimeMillis => (int)(Math.Min(Handler.TurnTime, Handler.RoundTime) * 1000);

		public bool ReturnMode
		{
			get; set;
		}
		#endregion

		private AbstractHandler Handler = null!;

		private Task? _primaryWatchdogTask;

		private CancellationTokenSource? cancelTokenSrc;

		#region Game state cache fields
		private readonly int _checkgame_interval = 3000;
		private readonly int _ingame_interval = 1;
		private bool _isWatchdogWokeUp;
		private readonly string[] _wordCache = new string[6];
		private int _roundIndexCache;
		private string _unsupportedWordCache = "";
		private string _exampleWordCache = "";
		private string _currentPresentedWordCache = "";
		private string LastChat = "";
		#endregion

		#region Game events
		public event EventHandler? GameStarted;

		public event EventHandler? GameEnded;

		public event EventHandler<WordPresentEventArgs>? MyWordPresented;

		public event EventHandler? MyTurnEnded;

		public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;

		public event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;

		public event EventHandler? RoundChanged;

		public event EventHandler<GameModeChangeEventArgs>? GameModeChanged;

		public event EventHandler<WordPresentEventArgs>? TypingWordPresented;

		public event EventHandler? ChatUpdated;
		#endregion

		private readonly IPathManager PathManager;

		public HandlerManager(IPathManager pathManager, AbstractHandler handler)
		{
			PathManager = pathManager;
			Handler = handler;
		}

		// public static AbstractHandler? GetHandler(Uri site)
		// {
		// 	foreach (AbstractHandler handler in GetAvailableHandlers())
		// 	{
		// 		if (handler.UrlPattern.Any(url => url.IsBaseOf(site)))
		// 			return handler;
		// 	}
		// 
		// 	return null;
		// }

		public string GetID() => $"{Handler.HandlerName} - #{(_primaryWatchdogTask == null ? "Global" : _primaryWatchdogTask.Id.ToString(CultureInfo.InvariantCulture))}";

		#region Watchdog proc. helper methods

		private static async Task WatchdogProc(Func<Task> repeatTask, Action<Exception> onException, CancellationToken cancelToken)
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
		private async Task AssistantWatchdog(string watchdogName, Action action, CancellationToken cancelToken)
		{
			await WatchdogProc(async () =>
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
			}, ex => Log.Error(ex, "{0}-watchdog task interrupted.", watchdogName), cancelToken);
		}
		#endregion

		#region Watchdog proc.
		public void Start()
		{
			if (!_isWatchdogWokeUp)
			{
				_isWatchdogWokeUp = true;

				cancelTokenSrc = new CancellationTokenSource();
				CancellationToken token = cancelTokenSrc.Token;

				_primaryWatchdogTask = new Task(async () => await PrimaryWatchdog(token), token);
				_primaryWatchdogTask.Start();

				Task.Run(async () => await AssistantWatchdog("History", UpdateWordHistory, token));
				Task.Run(async () => await AssistantWatchdog("Round", UpdateRound, token));
				Task.Run(async () => await AssistantWatchdog("Mission word", UpdateMissionWord, token));
				Task.Run(async () => await AssistantWatchdog("Unsupported word", UpdateUnsupportedWord, token));
				Task.Run(async () => await AssistantWatchdog("Example word", UpdateExample, token));
				Task.Run(async () => await GameModeWatchdog(token));
				Task.Run(async () => await AssistantWatchdog("My turn", () => CheckTurn(), token));
				Task.Run(async () => await PresentedWordGameMode(token));

				Log.Information("Watchdog threads are started.");
			}
		}

		public void Stop()
		{
			if (_isWatchdogWokeUp)
			{
				Log.Information("Watchdog stop requested.");
				cancelTokenSrc?.Cancel();
				_isWatchdogWokeUp = false;
			}
		}

		private async Task PrimaryWatchdog(CancellationToken cancelToken)
		{
			await WatchdogProc(async () =>
			{
				CheckGameStarted();
				await Task.Delay(IsGameStarted ? _ingame_interval : _checkgame_interval, cancelToken);
			}, ex => Log.Error(ex, "Primary watchdog task interrupted."), cancelToken);
		}

		private async Task GameModeWatchdog(CancellationToken cancelToken)
		{
			await WatchdogProc(async () =>
				{
					UpdateGameMode();
					await Task.Delay(_checkgame_interval, cancelToken);
				}, ex => Log.Error(ex, "GameMode watchdog task interrupted."), cancelToken);
		}

		// 참고: 이 와치독은 '타자 대결' 모드에서만 사용됩니다
		private async Task PresentedWordGameMode(CancellationToken cancelToken)
		{
			await WatchdogProc(async () =>
				{
					if (AutoKkutuMain.Configuration.GameMode == GameMode.TypingBattle && IsGameStarted)
					{
						if (IsMyTurn)
							UpdateTypingWord();
						await Task.Delay(_ingame_interval, cancelToken);
					}
					else
					{
						await Task.Delay(_checkgame_interval, cancelToken);
					}
				}, ex => Log.Error(ex, "Presented-word watchdog task interrupted."), cancelToken);
		}
		#endregion

		#region Game status update
		private void CheckGameStarted()
		{
			if (Handler.IsGameInProgress)
			{
				if (!IsGameStarted)
				{
					Handler.RegisterRoundIndexFunction();
					Log.Debug("New game started; Previous word list flushed.");
					GameStarted?.Invoke(this, EventArgs.Empty);
					IsGameStarted = true;
				}
			}
			else
			{
				if (!IsGameStarted)
					return;

				Log.Debug("Game ended.");
				GameEnded?.Invoke(this, EventArgs.Empty);
				IsGameStarted = false;
			}
		}

		private void CheckTurn()
		{
			if (Handler.IsMyTurn)
			{
				if (!IsMyTurn)
				{
					// When my turn comes...
					IsMyTurn = true;

					if (Handler.GameMode == GameMode.Free)
					{
						MyWordPresented?.Invoke(this, new WordPresentEventArgs(new PresentedWord("", false), CurrentMissionChar));
						return;
					}

					PresentedWord? presentedWord = UpdatePresentedWord();

					if (presentedWord == null)
						return;

					if (presentedWord.CanSubstitution)
						Log.Information("My turn arrived, presented word is {word} (Subsitution: {subsituation})", presentedWord.Content, presentedWord.Substitution);
					else
						Log.Information("My turn arrived, presented word is {word}.", presentedWord.Content);
					CurrentPresentedWord = presentedWord;
					MyWordPresented?.Invoke(this, new WordPresentEventArgs(presentedWord, CurrentMissionChar));
				}

				return;
			}

			if (!IsMyTurn)
				return;
			IsMyTurn = false;
			// When my turn ends...
			Log.Debug("My turn ended.");
			MyTurnEnded?.Invoke(this, EventArgs.Empty);
		}
		#endregion

		public bool IsValidPath(PathFinderParameter path)
		{
			if (path.Options.HasFlag(PathFinderOptions.ManualSearch))
				return true;

			bool differentWord = CurrentPresentedWord != null && !path.Word.Equals(CurrentPresentedWord);
			bool differentMissionChar = path.Options.HasFlag(PathFinderOptions.MissionWordExists) && !string.IsNullOrWhiteSpace(CurrentMissionChar) && !string.Equals(path.MissionChar, CurrentMissionChar, StringComparison.OrdinalIgnoreCase);
			if (IsMyTurn && (differentWord || differentMissionChar))
			{
				Log.Warning(I18n.PathFinder_InvalidatedUpdate, differentWord, differentMissionChar);
				MyWordPresented?.Invoke(this, new WordPresentEventArgs(CurrentPresentedWord, CurrentMissionChar));
				return false;
			}
			return true;
		}

		#region Update game status
		private void UpdateWordHistory()
		{
			if (ConfigEnums.IsFreeMode(AutoKkutuMain.Configuration.GameMode))
				return;

			string[] tmpWordCache = new string[6];

			for (int index = 0; index < 6; index++)
			{
				string previousWord = Handler.GetWordInHistory(index);
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

					if (ReturnMode)
						PathManager.ResetPreviousPath();
					else
						PathManager.AddPreviousPath(word);
				}
			}

			Array.Copy(tmpWordCache, _wordCache, 6);
		}

		private void UpdateMissionWord()
		{
			string missionWord = Handler.MissionChar;
			if (string.IsNullOrWhiteSpace(missionWord) || string.Equals(missionWord, CurrentMissionChar, StringComparison.Ordinal))
				return;
			Log.Information("Mission word change detected : {word}", missionWord);
			CurrentMissionChar = missionWord;
		}

		private void UpdateRound()
		{
			int roundIndex = Handler.RoundIndex;
			if (roundIndex == _roundIndexCache) // Detect round change by round index
				return;

			string roundText = Handler.RoundText;
			if (string.IsNullOrWhiteSpace(roundText))
				return;

			_roundIndexCache = roundIndex;

			if (roundIndex <= 0)
				return;
			Log.Information("Round Changed : Index {0} Word {1}", roundIndex, roundText);
			RoundChanged?.Invoke(this, new RoundChangeEventArgs(roundIndex, roundText));
			PathManager.ResetPreviousPath();
		}

		private void UpdateUnsupportedWord()
		{
			string unsupportedWord = Handler.UnsupportedWord;
			if (string.IsNullOrWhiteSpace(unsupportedWord) || string.Equals(unsupportedWord, _unsupportedWordCache, StringComparison.OrdinalIgnoreCase) || unsupportedWord.Contains("T.T", StringComparison.OrdinalIgnoreCase))
				return;

			bool isExistingWord = unsupportedWord.Contains(':', StringComparison.Ordinal); // '첫 턴 한방 금지: ', '한방 단어: ' 등등...
			_unsupportedWordCache = unsupportedWord;

			UnsupportedWordEntered?.Invoke(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
			if (IsMyTurn)
				MyPathIsUnsupported?.Invoke(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
		}

		/// <summary>
		/// 라운드가 끝났을 때, 회색으로 옅게 제시되는 예시 단어를 읽어들입니다.
		/// </summary>
		private void UpdateExample()
		{
			string example = Handler.ExampleWord;
			if (string.IsNullOrWhiteSpace(example) || example.StartsWith("게임 끝", StringComparison.Ordinal))
				return;
			if (string.Equals(example, _exampleWordCache, StringComparison.OrdinalIgnoreCase))
				return;
			_exampleWordCache = example;
			Log.Information("Path example detected : {word}", example);
			PathManager.NewPathList.Add(example);
		}

		private void UpdateTypingWord()
		{
			string word = Handler.PresentedWord;
			if (string.IsNullOrWhiteSpace(word) || word.StartsWith("게임 끝", StringComparison.InvariantCultureIgnoreCase))
				return;
			if (word.Contains(' ', StringComparison.Ordinal))
				word = word[..word.IndexOf(' ', StringComparison.Ordinal)];
			if (string.Equals(word, _currentPresentedWordCache, StringComparison.OrdinalIgnoreCase))
				return;
			_currentPresentedWordCache = word;
			Log.Information("Word detected : {word}", word);
			TypingWordPresented?.Invoke(this, new WordPresentEventArgs(new PresentedWord(word, false), ""));
		}

		private void UpdateGameMode()
		{
			GameMode gameMode = Handler.GameMode;
			if (gameMode == CurrentGameMode)
				return;
			CurrentGameMode = gameMode;
			Log.Information("Game mode change detected : {gameMode}", ConfigEnums.GetGameModeName(gameMode));
			GameModeChanged?.Invoke(this, new GameModeChangeEventArgs(gameMode));
		}

		private PresentedWord? UpdatePresentedWord()
		{
			string content = Handler.PresentedWord;

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
			else  // 가끔가다가 서버 렉때문에 '내가 입력해야할 단어의 조건' 대신 '이전 라운드에 입력되었었던 단어'가 나한테 그대로 넘어오는 경우가 왕왕 있음. 특히 양쪽이 오토 켜놓고 대결할 때.
			{
				var converted = CurrentGameMode.ConvertToPresentedWord(content);
				if (converted == null)
					return null;
				primary = converted;
			}

			return new PresentedWord(primary, hasSecondary, secondary);
		}

		public void UpdateChat(string input)
		{
			Handler.UpdateChat(input);
			LastChat = input;
			ChatUpdated?.Invoke(this, EventArgs.Empty);
		}

		public void AppendChat(Func<string, string> appender)
		{
			if (appender is null)
				throw new ArgumentNullException(nameof(appender));

			UpdateChat(appender(LastChat));
		}

		public void ClickSubmitButton()
		{
			Handler.ClickSubmit();
			LastChat = "";
		}
		#endregion


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
				_primaryWatchdogTask?.Dispose();
			}
		}
	}
}
