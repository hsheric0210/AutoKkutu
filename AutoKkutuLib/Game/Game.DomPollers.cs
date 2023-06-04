using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private Task? mainPoller;
	private CancellationTokenSource? pollerCancel;

	private void StartPollers()
	{
		pollerCancel = new CancellationTokenSource();
		CancellationToken token = pollerCancel.Token;

		mainPoller = new Task(async () => await ConditionlessDomPoller(PollGameProgress, token, "Game-Progress poller", primaryInterval), token);
		mainPoller.Start();

		Task.Run(async () => await BaseDomPoller(
			PollWordHistory,
			null,
			(ex) => Log.Error(ex, "'Word Histories' poller exception"),
			() => IsGameInProgress && !CurrentGameMode.IsFreeMode(),
			intenseInterval,
			idleInterval,
			token));
		Task.Run(async () => await BaseDomPoller(
			PollTypingWord,
			null,
			(ex) => Log.Error(ex, "'Typing-word' poller exception"),
			() => IsGameInProgress && CurrentGameMode == GameMode.TypingBattle,
			intenseInterval,
			idleInterval,
			token));

		Task.Run(async () => await GameDomPoller(PollRound, token, "Round index poller"));
		Task.Run(async () => await GameDomPoller(PollWordError, token, "Unsupported word poller"));
		Task.Run(async () => await GameDomPoller(PollWordHint, token, "Word-Hint poller"));
		Task.Run(async () => await SlowDomPoller(PollGameMode, token, "Gamemode poller"));
		Task.Run(async () => await GameDomPoller(PollTurn, token, "My turn poller"));
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
		await RegisterInGameFunctions(new HashSet<int>());
		NotifyGameProgress(await domHandler.GetIsGameInProgress());
	}

	private async Task PollTurn() => NotifyMyTurn(await domHandler.GetIsMyTurn(), await PollWordCondition());

	private async Task PollWordHistory()
	{
		var histories = await domHandler.GetWordInHistories();
		if (histories != null)
			NotifyWordHistories(histories);
	}

	private async Task PollRound() => NotifyRound(await domHandler.GetRoundIndex());

	private async Task PollWordError()
	{
		var word = await domHandler.GetUnsupportedWord();
		if (string.IsNullOrWhiteSpace(word))
			return;

		TurnErrorCode errorCode = TurnErrorCode.NotFound;
		if (word.Contains(':'))
		{
			var details = word[..word.IndexOf(':')];
			if (details.Contains("한방"))
				errorCode = details.Contains("첫 턴") ? TurnErrorCode.NoEndWordOnBegin : TurnErrorCode.EndWord; // '첫 턴 한방 금지' / '한방 단어'
			else if (details.Contains("외래")) // '외래어'
				errorCode = TurnErrorCode.Loanword;
			else if (details.Contains('깐')) // '깐깐!'
				errorCode = TurnErrorCode.Strict;
			else if (details.Contains("주제")) // '다른 주제'
				errorCode = TurnErrorCode.WrongSubject;
			else if (details.Contains("이미")) // '이미 사용된 단어'
				errorCode = TurnErrorCode.AlreadyUsed;
		}
		NotifyTurnError(word, errorCode);
	}

	/// <summary>
	/// 라운드가 끝났을 때, 회색으로 옅게 제시되는 예시 단어를 읽어들입니다.
	/// </summary>
	private async Task PollWordHint()
	{
		var word = await domHandler.GetExampleWord();
		if (!string.IsNullOrWhiteSpace(word) && !word.StartsWith("게임 끝", StringComparison.Ordinal))
			NotifyWordHint(word);
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
		if (gameMode != GameMode.None)
			NotifyGameMode(gameMode);
	}

	// TODO: Return nothing when the current gamemode is 'Free'
	private async Task<WordCondition?> PollWordCondition()
	{
		var condition = await domHandler.GetPresentedWord();
		var missionChar = await domHandler.GetMissionChar();
		missionChar = string.IsNullOrWhiteSpace(missionChar) ? null : missionChar;

		if (string.IsNullOrEmpty(condition))
			return null;

		string cChar;
		var cSubChar = string.Empty;
		if (condition.Contains('(') && condition.Last() == ')')
		{
			var parentheseStartIndex = condition.IndexOf('(');
			var parentheseEndIndex = condition.IndexOf(')');
			cChar = condition[..parentheseStartIndex];
			cSubChar = condition.Substring(parentheseStartIndex + 1, parentheseEndIndex - parentheseStartIndex - 1);
		}
		else if (condition.Length <= 1)
		{
			cChar = condition;
		}
		else
		{
			// 가끔가다가 서버 렉때문에 '내가 입력해야할 단어의 조건' 대신 '이전 라운드에 입력되었었던 단어'가
			// 나한테 그대로 넘어오는 경우가 왕왕 있음. 특히 양쪽이 오토 켜놓고 대결할 때.
			return CurrentGameMode.ConvertWordToCondition(condition, missionChar);
		}

		return new WordCondition(cChar, cSubChar, missionChar);
	}
}
