using AutoKkutuLib.Extension;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private const string gameDomPoller = "Game.DomPoller";

	private Task? mainPoller;
	private CancellationTokenSource? pollerCancel;

	// Be careful, these cache variables are not thread-safe.
	// They should be called and modified only within a single thread.
	private bool domIsMyTurn;
	private int domTurnIndex = -1;
	private string domUserId = "";

	private void StartPollers()
	{
		pollerCancel = new CancellationTokenSource();
		var token = pollerCancel.Token;

		mainPoller = new Task(async () => await ConditionlessDomPoller(PollGameProgress, token, "Game-Progress poller", looseInterval), token);
		mainPoller.Start();

		Task.Run(async () => await BaseDomPoller(
			PollWordHistory,
			null,
			(ex) => LibLogger.Error(gameDomPoller, ex, "'Word Histories' poller exception"),
			() => Session.AmIGaming && !Session.GameMode.IsFreeMode(),
			intenseInterval,
			idleInterval,
			token));
		Task.Run(async () => await BaseDomPoller(
			PollTypingWord,
			null,
			(ex) => LibLogger.Error(gameDomPoller, ex, "Typing-battle word poller exception"),
			() => Session.AmIGaming && Session.GameMode == GameMode.TypingBattle,
			intenseInterval,
			idleInterval,
			token));

		Task.Run(async () => await GameDomPoller(PollClassicTurn, token, "Classic turn poller", looseInterval));
		Task.Run(async () => await GameDomPoller(PollRound, token, "Round index poller", looseInterval));
		Task.Run(async () => await GameDomPoller(PollWordError, token, "Word error poller"));
		Task.Run(async () => await GameDomPoller(PollWordHint, token, "Word hint poller", looseInterval));
		Task.Run(async () => await SlowDomPoller(PollGameMode, token, "Gamemode poller"));
		Task.Run(async () => await ConditionlessDomPoller(PollUserId, token, "User-ID poller", looseInterval));
		LibLogger.Debug(gameDomPoller, "DOM pollers are now active.");
	}

	private void StopPollers()
	{
		LibLogger.Debug(gameDomPoller, "Shutting down DOM pollers...");
		pollerCancel?.Cancel();
		mainPoller?.Wait(); // await to be terminated
	}

	private async ValueTask PollGameProgress()
	{
		await RegisterInGameFunctions(new HashSet<int>());
		await RegisterWebSocketFilters();
		NotifyGameSession(await domHandler.GetUserId());
		NotifyGameSequence(await domHandler.GetGameSeq());
	}

	private async ValueTask PollUserId()
	{
		var userId = await domHandler.GetUserId();
		if (string.IsNullOrWhiteSpace(userId) || domUserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
			return;
		domUserId = userId;
	}

	private async ValueTask PollClassicTurn()
	{
		// Use fast method to determine my turn *START*
		var myTurn = await domHandler.GetIsMyTurn();
		if (myTurn)
		{
			// Interlock check
			if (domIsMyTurn)
				return;
			domIsMyTurn = true;

			LibLogger.Verbose(gameDomPoller, "DOM Handler detected my turn start.");
			NotifyClassicTurnStart(true, -1, await PollWordCondition());
			return;
		}

		// DOM turn out-of-sync 방지 (특히 WebSocketHandler와 병행하여 사용하면서, 게임 템포가 매우 빠를 때 자주 발생함)
		if (domTurnIndex != -1 && domTurnIndex != Session.GetRelativeTurn())
		{
			LibLogger.Debug(gameDomPoller, "DomPoller PollTurn turn out-of-sync detected. domTurn={domTurn} sessionTurn={sessTurn}", domTurnIndex, Session.GetRelativeTurn());
			domTurnIndex = Session.GetRelativeTurn();
			return;
		}

		// Use slow but accurate method to determine my turn *END* and other player turn changes
		var turnIndexNow = await domHandler.GetTurnIndex();
		if (turnIndexNow != domTurnIndex) // Turn-index changed
		{
			if (domTurnIndex != -1) // turnEnd: prev != -1
			{
				LibLogger.Verbose(gameDomPoller, "DOM Handler detected turn end (prevTurnIndex: {prev}, nowTurnIndex: {now}, isMyTurn: {myTurn})", domTurnIndex, turnIndexNow, domIsMyTurn);
				NotifyClassicTurnEndOk("");
				domIsMyTurn = false;
			}

			if (turnIndexNow != -1) // turnStart: now != -1
			{
				LibLogger.Verbose(gameDomPoller, "DOM Handler detected other user turn start (prevTurnIndex: {prev}, nowTurnIndex: {now}, isMyTurn: {myTurn})", domTurnIndex, turnIndexNow, domIsMyTurn);
				NotifyClassicTurnStart(false, turnIndexNow, await PollWordCondition());
			}

			domTurnIndex = turnIndexNow;
		}
	}

	private async ValueTask PollWordHistory()
	{
		var histories = await domHandler.GetWordInHistories();
		if (histories != null)
			NotifyWordHistories(histories);
	}

	private async ValueTask PollRound() => NotifyRoundChange(await domHandler.GetRoundIndex());

	private async ValueTask PollWordError()
	{
		var word = await domHandler.GetUnsupportedWord();
		if (string.IsNullOrWhiteSpace(word))
			return;

		var errorCode = TurnErrorCode.NotFound;
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
		NotifyTurnError(word, errorCode, true);
	}

	/// <summary>
	/// 라운드가 끝났을 때, 회색으로 옅게 제시되는 예시 단어를 읽어들입니다.
	/// </summary>
	private async ValueTask PollWordHint()
	{
		var word = await domHandler.GetExampleWord();
		if (!string.IsNullOrWhiteSpace(word) && !word.StartsWith("게임 끝", StringComparison.Ordinal))
			NotifyWordHint(word);
	}

	private async ValueTask PollTypingWord() // TODO: Remove it and merge with UpdateWord
	{
		var word = await domHandler.GetPresentedWord();
		if (string.IsNullOrWhiteSpace(word) || word.StartsWith("게임 끝", StringComparison.InvariantCultureIgnoreCase) || !await domHandler.GetIsMyTurn())
			return;
		if (word.Contains(' ', StringComparison.Ordinal))
			word = word[..word.IndexOf(' ', StringComparison.Ordinal)];
		NotifyTypingBattleWord(word);
	}

	private async ValueTask PollGameMode()
	{
		var gameMode = await domHandler.GetGameMode();
		if (gameMode != GameMode.None)
			NotifyGameMode(gameMode, true);
	}

	private async ValueTask<WordCondition> PollWordCondition()
	{
		var condition = await domHandler.GetPresentedWord();
		var missionChar = await domHandler.GetMissionChar();

		if (string.IsNullOrEmpty(condition) || Session.GameMode.IsFreeMode())
			return WordCondition.Empty;

		condition = condition.TrimStart('<').TrimEnd('>'); // 훈민정음 등 모드 호환

		string cChar;
		var cSubChar = string.Empty;
		var wordLength = 3;
		if (condition.Contains('(') && condition.Last() == ')')
		{
			var parentheseStartIndex = condition.IndexOf('(');
			var parentheseEndIndex = condition.IndexOf(')');
			cChar = condition[..parentheseStartIndex];
			cSubChar = condition.Substring(parentheseStartIndex + 1, parentheseEndIndex - parentheseStartIndex - 1);

			if (Session.GameMode == GameMode.KungKungTta)
				wordLength = await domHandler.GetWordLength();
		}
		else if (condition.Length <= 1)
		{
			cChar = condition;
		}
		else if (Session.GameMode != GameMode.None)
		{
			// 가끔가다가 서버 렉때문에 '내가 입력해야할 단어의 조건' 대신 '이전 라운드에 입력되었었던 단어'가
			// 나한테 그대로 넘어오는 경우가 왕왕 있음. 특히 양쪽이 오토 켜놓고 대결할 때.
			var converted = Session.GameMode.ConvertWordToCondition(condition, missionChar);
			if (converted == null)
			{
				LibLogger.Error(gameDomPoller, "Failed to convert word {word} to condition. (missionChar: {missionChar})", condition, missionChar);
				return WordCondition.Empty;
			}

			var converted2 = (WordCondition)converted;

			if (Session.GameMode == GameMode.KungKungTta)
				wordLength = await domHandler.GetWordLength();
			wordLength = wordLength == 3 ? 2 : 3; // 3232 모드에서는 상대가 3이면 그 다음 사람은 2, 그 다음 사람이 2이면 그 다음다음 사람은 3 이 반복됨

			return new WordCondition(converted2.Char, converted2.SubChar, converted2.MissionChar, wordLength);
		}
		else
		{
			return WordCondition.Empty;
		}

		return new WordCondition(cChar, cSubChar, missionChar, wordLength);
	}
}
