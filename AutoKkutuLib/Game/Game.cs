using AutoKkutuLib.Extension;
using AutoKkutuLib.Game.Events;
using AutoKkutuLib.Handlers;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib.Game;

public class Game : IGame
{
	public AutoEnter AutoEnter { get; }
	public JsEvaluator JsEvaluator => handler.JsEvaluator;

	#region Game status properties

	public PresentedWord? CurrentPresentedWord
	{
		get; private set;
	}

	public string CurrentMissionChar
	{
		get; private set;
	} = "";

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

	public int TurnTimeMillis => (int)(Math.Min(handler.TurnTime, handler.RoundTime) * 1000);

	public bool ReturnMode
	{
		get; set;
	}
	#endregion

	#region Internal handle holder fields
	private readonly AbstractHandler handler;

	private Task? primaryWatchdogTask;

	private CancellationTokenSource? cancelTokenSrc;
	#endregion

	#region Game state cache fields
	private readonly int checkgameInterval = 3000;
	private readonly int ingameInterval = 1;
	private bool isWatchdogWokeUp;
	private readonly string[] wordHistoryCache = new string[6];
	private int roundIndexCache;
	private string unsupportedWordCache = "";
	private string exampleWordCache = "";
	private string currentPresentedWordCache = "";
	private string lastChat = "";
	#endregion

	#region Game events
	public event EventHandler? GameStarted;

	public event EventHandler? GameEnded;

	public event EventHandler<WordConditionPresentEventArgs>? MyWordPresented;

	public event EventHandler? MyTurnEnded;

	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;

	public event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;

	public event EventHandler? RoundChanged;

	public event EventHandler<GameModeChangeEventArgs>? GameModeChanged;

	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;

	public event EventHandler? ChatUpdated;

	public event EventHandler<WordHistoryEventArgs>? DiscoverWordHistory;
	public event EventHandler<WordPresentEventArgs>? ExampleWordPresented;
	#endregion

	public Game(AbstractHandler handler)
	{
		this.handler = handler;
		AutoEnter = new AutoEnter(this);
	}

	public string GetID() => $"{handler.HandlerName} - #{(primaryWatchdogTask == null ? "Global" : primaryWatchdogTask.Id.ToString(CultureInfo.InvariantCulture))}";

	public bool HasSameHandler(AbstractHandler otherHandler) => handler.HandlerName.Equals(otherHandler.HandlerName, StringComparison.OrdinalIgnoreCase);

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
				await Task.Delay(ingameInterval, cancelToken);
			}
			else
			{
				await Task.Delay(checkgameInterval, cancelToken);
			}
		}, ex => Log.Error(ex, "{0}-watchdog task interrupted.", watchdogName), cancelToken);
	}
	#endregion

	#region Watchdog proc.
	public void Start()
	{
		if (!isWatchdogWokeUp)
		{
			isWatchdogWokeUp = true;

			cancelTokenSrc = new CancellationTokenSource();
			CancellationToken token = cancelTokenSrc.Token;

			primaryWatchdogTask = new Task(async () => await PrimaryWatchdog(token), token);
			primaryWatchdogTask.Start();

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
		if (isWatchdogWokeUp)
		{
			Log.Information("Watchdog stop requested.");
			cancelTokenSrc?.Cancel();
			primaryWatchdogTask?.Wait(); // await to be terminated
			isWatchdogWokeUp = false;
		}
	}

	private async Task PrimaryWatchdog(CancellationToken cancelToken)
	{
		await WatchdogProc(async () =>
		{
			CheckGameStarted();
			await Task.Delay(IsGameStarted ? ingameInterval : checkgameInterval, cancelToken);
		}, ex => Log.Error(ex, "Primary watchdog task interrupted."), cancelToken);
	}

	private async Task GameModeWatchdog(CancellationToken cancelToken)
	{
		await WatchdogProc(async () =>
			{
				UpdateGameMode();
				await Task.Delay(checkgameInterval, cancelToken);
			}, ex => Log.Error(ex, "GameMode watchdog task interrupted."), cancelToken);
	}

	// 참고: 이 와치독은 '타자 대결' 모드에서만 사용됩니다
	private async Task PresentedWordGameMode(CancellationToken cancelToken)
	{
		await WatchdogProc(async () =>
			{
				if (CurrentGameMode == GameMode.TypingBattle && IsGameStarted)
				{
					if (IsMyTurn)
						UpdateTypingWord();
					await Task.Delay(ingameInterval, cancelToken);
				}
				else
				{
					await Task.Delay(checkgameInterval, cancelToken);
				}
			}, ex => Log.Error(ex, "Presented-word watchdog task interrupted."), cancelToken);
	}
	#endregion

	#region Game status update
	private void CheckGameStarted()
	{
		if (handler.IsGameInProgress)
		{
			if (!IsGameStarted)
			{
				handler.RegisterRoundIndexFunction();
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
		if (handler.IsMyTurn)
		{
			if (!IsMyTurn)
			{
				// When my turn comes...
				IsMyTurn = true;

				if (handler.GameMode == GameMode.Free)
				{
					MyWordPresented?.Invoke(this, new WordConditionPresentEventArgs(new PresentedWord("", false), CurrentMissionChar));
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
				MyWordPresented?.Invoke(this, new WordConditionPresentEventArgs(presentedWord, CurrentMissionChar));
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
		if (path.Options.HasFlag(PathFinderFlags.ManualSearch))
			return true;

		var differentWord = CurrentPresentedWord != null && !path.Word.Equals(CurrentPresentedWord);
		var differentMissionChar = path.Options.HasFlag(PathFinderFlags.MissionWordExists) && !string.IsNullOrWhiteSpace(CurrentMissionChar) && !string.Equals(path.MissionChar, CurrentMissionChar, StringComparison.OrdinalIgnoreCase);
		if (IsMyTurn && (differentWord || differentMissionChar))
		{
			Log.Warning(I18n.PathFinder_InvalidatedUpdate, differentWord, differentMissionChar);
			MyWordPresented?.Invoke(this, new WordConditionPresentEventArgs(CurrentPresentedWord!, CurrentMissionChar!)); // Re-trigger search
			return false;
		}
		return true;
	}

	#region Update game status
	private void UpdateWordHistory()
	{
		if (CurrentGameMode.IsFreeMode())
			return;

		var tmpWordCache = new string[6];

		for (var index = 0; index < 6; index++)
		{
			var previousWord = handler.GetWordInHistory(index);
			if (!string.IsNullOrWhiteSpace(previousWord) && previousWord.Contains('<', StringComparison.Ordinal))
				tmpWordCache[index] = previousWord[..previousWord.IndexOf('<', StringComparison.Ordinal)].Trim();
		}

		for (var index = 0; index < 6; index++)
		{
			var word = tmpWordCache[index];
			if (!string.IsNullOrWhiteSpace(word) && !wordHistoryCache.Contains(word))
			{
				Log.Information("Found previous word : {word}", word);
				DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(word));
			}
		}

		Array.Copy(tmpWordCache, wordHistoryCache, 6);
	}

	private void UpdateMissionWord()
	{
		var missionWord = handler.MissionChar;
		if (string.IsNullOrWhiteSpace(missionWord) || string.Equals(missionWord, CurrentMissionChar, StringComparison.Ordinal))
			return;
		Log.Information("Mission word change detected : {word}", missionWord);
		CurrentMissionChar = missionWord;
	}

	private void UpdateRound()
	{
		var roundIndex = handler.RoundIndex;
		if (roundIndex == roundIndexCache) // Detect round change by round index
			return;

		var roundText = handler.RoundText;
		if (string.IsNullOrWhiteSpace(roundText))
			return;

		roundIndexCache = roundIndex;

		if (roundIndex <= 0)
			return;
		Log.Information("Round Changed : Index {0} Word {1}", roundIndex, roundText);
		RoundChanged?.Invoke(this, new RoundChangeEventArgs(roundIndex, roundText));
	}

	private void UpdateUnsupportedWord()
	{
		var unsupportedWord = handler.UnsupportedWord;
		if (string.IsNullOrWhiteSpace(unsupportedWord) || string.Equals(unsupportedWord, unsupportedWordCache, StringComparison.OrdinalIgnoreCase) || unsupportedWord.Contains("T.T", StringComparison.OrdinalIgnoreCase))
			return;

		var isExistingWord = unsupportedWord.Contains(':', StringComparison.Ordinal); // '첫 턴 한방 금지: ', '한방 단어: ' 등등...
		unsupportedWordCache = unsupportedWord;

		UnsupportedWordEntered?.Invoke(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
		if (IsMyTurn)
			MyPathIsUnsupported?.Invoke(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord));
	}

	/// <summary>
	/// 라운드가 끝났을 때, 회색으로 옅게 제시되는 예시 단어를 읽어들입니다.
	/// </summary>
	private void UpdateExample()
	{
		var example = handler.ExampleWord;
		if (string.IsNullOrWhiteSpace(example) || example.StartsWith("게임 끝", StringComparison.Ordinal))
			return;
		if (string.Equals(example, exampleWordCache, StringComparison.OrdinalIgnoreCase))
			return;
		exampleWordCache = example;
		Log.Information("Path example detected : {word}", example);
		ExampleWordPresented?.Invoke(this, new WordPresentEventArgs(example));
	}

	private void UpdateTypingWord()
	{
		var word = handler.PresentedWord;
		if (string.IsNullOrWhiteSpace(word) || word.StartsWith("게임 끝", StringComparison.InvariantCultureIgnoreCase))
			return;
		if (word.Contains(' ', StringComparison.Ordinal))
			word = word[..word.IndexOf(' ', StringComparison.Ordinal)];
		if (string.Equals(word, currentPresentedWordCache, StringComparison.OrdinalIgnoreCase))
			return;
		currentPresentedWordCache = word;
		Log.Information("Word detected : {word}", word);
		TypingWordPresented?.Invoke(this, new WordPresentEventArgs(word));
	}

	private void UpdateGameMode()
	{
		GameMode gameMode = handler.GameMode;
		if (gameMode == CurrentGameMode)
			return;
		CurrentGameMode = gameMode;
		Log.Information("Game mode change detected : {gameMode}", gameMode.GameModeName());
		GameModeChanged?.Invoke(this, new GameModeChangeEventArgs(gameMode));
	}

	private PresentedWord? UpdatePresentedWord()
	{
		var content = handler.PresentedWord;

		if (string.IsNullOrEmpty(content))
			return null;

		string primary;
		var secondary = string.Empty;
		var hasSecondary = content.Contains('(', StringComparison.Ordinal) && content.Last() == ')';
		if (hasSecondary)
		{
			var parentheseStartIndex = content.IndexOf('(', StringComparison.Ordinal);
			var parentheseEndIndex = content.IndexOf(')', StringComparison.Ordinal);
			primary = content[..parentheseStartIndex];
			secondary = content.Substring(parentheseStartIndex + 1, parentheseEndIndex - parentheseStartIndex - 1);
		}
		else if (content.Length <= 1)
		{
			primary = content;
		}
		else  // 가끔가다가 서버 렉때문에 '내가 입력해야할 단어의 조건' 대신 '이전 라운드에 입력되었었던 단어'가 나한테 그대로 넘어오는 경우가 왕왕 있음. 특히 양쪽이 오토 켜놓고 대결할 때.
		{
			var tailNode = CurrentGameMode.ConvertWordToTailNode(content);
			if (tailNode == null)
				return null;
			primary = tailNode;
		}

		return new PresentedWord(primary, hasSecondary, secondary);
	}

	public void UpdateChat(string input)
	{
		handler.UpdateChat(input);
		lastChat = input;
		ChatUpdated?.Invoke(this, EventArgs.Empty);
	}

	public void AppendChat(Func<string, string> appender)
	{
		if (appender is null)
			throw new ArgumentNullException(nameof(appender));

		UpdateChat(appender(lastChat));
	}

	public void ClickSubmitButton()
	{
		handler.ClickSubmit();
		if (!string.IsNullOrEmpty(lastChat))
			AutoEnter.InputStopwatch.Restart();
		lastChat = "";
	}
	#endregion

	#region Disposal
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			cancelTokenSrc?.Dispose();
			primaryWatchdogTask?.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
