using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private Task? mainPoller;
	private CancellationTokenSource? pollerCancel;

	private readonly string[] wordHistoryCache = new string[6];
	private int roundIndexCache;
	private string unsupportedWordCache = "";
	private string exampleWordCache = "";
	private string currentPresentedWordCache = "";
	private long currentPresentedWordCacheTime = -1;

	private void StartPollers()
	{
		pollerCancel = new CancellationTokenSource();
		CancellationToken token = pollerCancel.Token;

		mainPoller = new Task(async () => await ConditionlessDomPoller(PollGameProgress, token, "Game-Progress poller", primaryInterval), token);
		mainPoller.Start();

		Task.Run(async () => await GameDomPoller(PollWordHistory, token, "Word history poller"));
		Task.Run(async () => await GameDomPoller(PollRound, token, "Round index poller"));
		Task.Run(async () => await GameDomPoller(PollMissionWord, token, "Mission word poller"));
		Task.Run(async () => await GameDomPoller(PollWordError, token, "Unsupported word poller"));
		Task.Run(async () => await GameDomPoller(PollWordHint, token, "Word-Hint poller"));
		Task.Run(async () => await SlowDomPoller(PollGameMode, token, "Gamemode poller"));
		Task.Run(async () => await GameDomPoller(PollTurn, token, "My turn poller"));
		Task.Run(async () => await TypingBattleWatchdog(PollTypingWord, token, "Typing-battle word poller"));
		Log.Debug("DOM pollers are now active.");
	}

	private void StopPollers()
	{
		Log.Debug("Shutting down DOM pollers...");
		pollerCancel?.Cancel();
		mainPoller?.Wait(); // await to be terminated
	}

	private async Task PollGameProgress()
	{
		domHandler.RegisterInGameFunctions(new HashSet<int>()); //FIXME: move to elsewhere
		if (await domHandler.GetIsGameInProgress())
		{
			if (!IsGameStarted)
			{
				Log.Debug("New game started; Used word history flushed.");
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

	private async Task PollTurn()
	{
		if (await domHandler.GetIsMyTurn())
		{
			if (!IsMyTurn)
			{
				// When my turn comes...
				IsMyTurn = true;

				if (CurrentGameMode == GameMode.Free)
				{
					MyWordPresented?.Invoke(this, new WordConditionPresentEventArgs(new WordCondition("", false), CurrentMissionChar));
					return;
				}

				WordCondition? presentedWord = await PollPresentedWord();

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

	private async Task PollWordHistory()
	{
		if (CurrentGameMode.IsFreeMode())
			return;

		var tmpWordCache = new string[6];

		for (var index = 0; index < 6; index++)
		{
			var history = await domHandler.GetWordInHistory(index); // TODO: Fix this. Do not parse by myself, instead use Json HTML DOM.
			if (!string.IsNullOrWhiteSpace(history) && history.Contains('<', StringComparison.Ordinal))
				tmpWordCache[index] = history[..history.IndexOf('<', StringComparison.Ordinal)].Trim();
		}

		for (var index = 0; index < 6; index++)
		{
			var word = tmpWordCache[index];
			if (!string.IsNullOrWhiteSpace(word) && !wordHistoryCache.Contains(word))
			{
				Log.Information("Found new used word in history : {word}", word);
				DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(word));
			}
		}

		Array.Copy(tmpWordCache, wordHistoryCache, 6);
	}

	private async Task PollMissionWord()
	{
		var missionWord = await domHandler.GetMissionChar();
		if (string.IsNullOrWhiteSpace(missionWord) || string.Equals(missionWord, CurrentMissionChar, StringComparison.Ordinal))
			return;
		Log.Information("Mission word change detected : {word}", missionWord);
		CurrentMissionChar = missionWord;
	}

	private async Task PollRound()
	{
		// Wait simultaneously
		Task<int> roundIndexTask = domHandler.GetRoundIndex();
		Task<string> roundTextTask = domHandler.GetRoundText();
		await Task.WhenAll(roundIndexTask, roundTextTask);

		var roundIndex = await roundIndexTask;
		if (roundIndex == roundIndexCache) // Detect round change by round index
			return;

		var roundText = await roundTextTask;
		if (string.IsNullOrWhiteSpace(roundText))
			return;

		roundIndexCache = roundIndex;

		if (roundIndex <= 0)
			return;
		Log.Information("Round Changed : Index {0} Word {1}", roundIndex, roundText);
		RoundChanged?.Invoke(this, new RoundChangeEventArgs(roundIndex, roundText));
	}

	private async Task PollWordError()
	{
		var unsupportedWord = await domHandler.GetUnsupportedWord();
		if (string.IsNullOrWhiteSpace(unsupportedWord) || string.Equals(unsupportedWord, unsupportedWordCache, StringComparison.OrdinalIgnoreCase) || unsupportedWord.Contains("T.T", StringComparison.OrdinalIgnoreCase))
			return;

		var isExistingWord = unsupportedWord.Contains(':', StringComparison.Ordinal); // '첫 턴 한방 금지: ', '한방 단어: ' 등등...
		var isEndWord = false;
		if (isExistingWord)
			isEndWord = unsupportedWord[..unsupportedWord.IndexOf(':')].Contains("한방"); // '한방 단어: ', '첫 턴 한방 금지: '

		unsupportedWordCache = unsupportedWord;

		UnsupportedWordEntered?.Invoke(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord, isEndWord));
		if (IsMyTurn)
			MyPathIsUnsupported?.Invoke(this, new UnsupportedWordEventArgs(unsupportedWord, isExistingWord, isEndWord));
	}

	/// <summary>
	/// 라운드가 끝났을 때, 회색으로 옅게 제시되는 예시 단어를 읽어들입니다.
	/// </summary>
	private async Task PollWordHint()
	{
		var example = await domHandler.GetExampleWord();
		if (string.IsNullOrWhiteSpace(example) || example.StartsWith("게임 끝", StringComparison.Ordinal))
			return;
		if (string.Equals(example, exampleWordCache, StringComparison.OrdinalIgnoreCase))
			return;
		exampleWordCache = example;
		Log.Information("Path example detected : {word}", example);
		ExampleWordPresented?.Invoke(this, new WordPresentEventArgs(example));
	}

	private async Task PollTypingWord() // TODO: Remove it and merge with UpdateWord
	{
		var word = await domHandler.GetPresentedWord();
		if (string.IsNullOrWhiteSpace(word) || word.StartsWith("게임 끝", StringComparison.InvariantCultureIgnoreCase))
			return;
		if (word.Contains(' ', StringComparison.Ordinal))
			word = word[..word.IndexOf(' ', StringComparison.Ordinal)];
		var tDelta = Environment.TickCount64 - currentPresentedWordCacheTime;
		if (string.Equals(word, currentPresentedWordCache, StringComparison.OrdinalIgnoreCase) && tDelta <= 1000)
			return;
		currentPresentedWordCache = word;
		currentPresentedWordCacheTime = Environment.TickCount64;
		Log.Information("Word detected : {word} (delay: {delta})", word, tDelta);
		TypingWordPresented?.Invoke(this, new WordPresentEventArgs(word));
	}

	private async Task PollGameMode()
	{
		GameMode gameMode = await domHandler.GetGameMode();
		if (gameMode == GameMode.None || gameMode == CurrentGameMode)
			return;
		CurrentGameMode = gameMode;
		Log.Information("Game mode change detected : {gameMode}", gameMode.GameModeName());
		GameModeChanged?.Invoke(this, new GameModeChangeEventArgs(gameMode));
	}

	private async Task<WordCondition?> PollPresentedWord()
	{
		var content = await domHandler.GetPresentedWord();

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

		return new WordCondition(primary, hasSecondary, secondary);
	}
}
