using AutoKkutu.Constants;
using AutoKkutu.Modules.Handlers;
using AutoKkutu.Modules.Path;
using AutoKkutu.Utils.Extension;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoKkutu.Modules.HandlerManagement;

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

	public int TurnTimeMillis => (int)(Math.Min(handler.TurnTime, handler.RoundTime) * 1000);

	public bool ReturnMode
	{
		get; set;
	}
	#endregion

	private readonly AbstractHandler handler = null!;

	private Task? primaryWatchdogTask;

	private CancellationTokenSource? cancelTokenSrc;

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

	public event EventHandler<WordPresentEventArgs>? MyWordPresented;

	public event EventHandler? MyTurnEnded;

	public event EventHandler<UnsupportedWordEventArgs>? UnsupportedWordEntered;

	public event EventHandler<UnsupportedWordEventArgs>? MyPathIsUnsupported;

	public event EventHandler? RoundChanged;

	public event EventHandler<GameModeChangeEventArgs>? GameModeChanged;

	public event EventHandler<WordPresentEventArgs>? TypingWordPresented;

	public event EventHandler? ChatUpdated;
	#endregion

	private readonly IPathManager pathManager;

	public HandlerManager(IPathManager pathManager, AbstractHandler handler)
	{
		this.pathManager = pathManager;
		this.handler = handler;
	}

	public string GetID() => $"{handler.HandlerName} - #{(primaryWatchdogTask == null ? "Global" : primaryWatchdogTask.Id.ToString(CultureInfo.InvariantCulture))}";

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
				if (AutoKkutuMain.Configuration.GameMode == GameMode.TypingBattle && IsGameStarted)
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

		var differentWord = CurrentPresentedWord != null && !path.Word.Equals(CurrentPresentedWord);
		var differentMissionChar = path.Options.HasFlag(PathFinderOptions.MissionWordExists) && !string.IsNullOrWhiteSpace(CurrentMissionChar) && !string.Equals(path.MissionChar, CurrentMissionChar, StringComparison.OrdinalIgnoreCase);
		if (IsMyTurn && (differentWord || differentMissionChar))
		{
			Log.Warning(I18n.PathFinder_InvalidatedUpdate, differentWord, differentMissionChar);
			MyWordPresented?.Invoke(this, new WordPresentEventArgs(CurrentPresentedWord!, CurrentMissionChar!));
			return false;
		}
		return true;
	}

	#region Update game status
	private void UpdateWordHistory()
	{
		if (ConfigEnums.IsFreeMode(AutoKkutuMain.Configuration.GameMode))
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

				if (!pathManager.NewPathList.Contains(word))
					pathManager.NewPathList.Add(word);

				if (ReturnMode)
					pathManager.ResetPreviousPath();
				else
					pathManager.AddPreviousPath(word);
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
		pathManager.ResetPreviousPath();
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
		pathManager.NewPathList.Add(example);
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
		TypingWordPresented?.Invoke(this, new WordPresentEventArgs(new PresentedWord(word, false), ""));
	}

	private void UpdateGameMode()
	{
		GameMode gameMode = handler.GameMode;
		if (gameMode == CurrentGameMode)
			return;
		CurrentGameMode = gameMode;
		Log.Information("Game mode change detected : {gameMode}", ConfigEnums.GetGameModeName(gameMode));
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
			var converted = CurrentGameMode.ConvertToPresentedWord(content);
			if (converted == null)
				return null;
			primary = converted;
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
		lastChat = "";
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
			primaryWatchdogTask?.Dispose();
		}
	}
}
