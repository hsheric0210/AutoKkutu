using AutoKkutuLib.Extension;
using Serilog;
using System.Collections.Immutable;

namespace AutoKkutuLib.Game;
public partial class Game
{
	// 모든 상태 필드들은 Thread-safe해야 함.
	private readonly object sessionLock = new();

	private readonly object roundIndexLock = new();
	private int roundIndexCache = -1;

	private long currentPresentedWordCacheTime = -1;

	private readonly object turnErrorWordLock = new();
	private string? turnErrorWordCache;

	private readonly object turnHintWordLock = new();
	private string? turnHintWordCache;

	//private readonly object typingWordLock = new object();
	private string? typingWordCache;

	private readonly object wordHistoryLock = new();
	private string? wordHistoryCache;
	private IImmutableList<string> wordHistoriesCache = ImmutableList<string>.Empty;

	public void NotifyGameSession(string myUserId)
	{
		lock (sessionLock)
		{
			if (string.IsNullOrEmpty(myUserId) || Session.MyUserId.Equals(myUserId, StringComparison.OrdinalIgnoreCase))
				return;
			Log.Debug("New game session detected with UserId: {uid}.", myUserId);
			Session = new GameSessionState(myUserId);
		}
	}

	/// <summary>
	/// 현재 게임의 참가 플레이어 목록 리스트 변경을 알리고 관련 이벤트들을 호출합니다.
	/// <paramref name="seq"/>을 캐싱하여, 연속된 동일 매개 변수에 대해 한 번만 반응합니다.
	/// </summary>
	/// <param name="seq">게임 참가 플레이어 목록</param>
	public void NotifyGameSequence(IImmutableList<string> seq)
	{
		var prevGamingState = Session.AmIGaming;
		if (!Session.UpdateGameSequence(seq))
			return;

		Log.Debug("Game-seq changed to: {seq}", string.Join(", ", seq));

		if (Session.AmIGaming == prevGamingState)
			return;

		Log.Debug("Gaming state changed to: {state}", Session.AmIGaming);

		if (Session.AmIGaming)
		{
			Log.Debug("New game started; Used word history flushed.");
			GameStarted?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			Log.Debug("Game ended.");

			// Clear game-specific caches
			roundIndexCache = -1;
			currentPresentedWordCacheTime = -1;
			turnErrorWordCache = null;
			turnHintWordCache = null;
			typingWordCache = null;
			wordHistoryCache = null;
			wordHistoriesCache = ImmutableList<string>.Empty;

			GameEnded?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// 게임 모드 변경을 알리고 관련 이벤트들을 호출합니다.
	/// <paramref name="gameMode"/>를 캐싱하여, 연속된 동일 매개 변수에 대해 한 번만 반응합니다.
	/// </summary>
	/// <param name="gameMode"></param>
	/// <param name="byDOM"></param>
	public void NotifyGameMode(GameMode gameMode, bool byDOM = false)
	{
		if (!Session.UpdateGameMode(gameMode))
			return;

		Log.Debug("Game mode change detected : {gameMode} (byDOM: {byDOM})", gameMode.GameModeName(), byDOM);
		GameModeChanged?.Invoke(this, new GameModeChangeEventArgs(gameMode));
	}

	/// <summary>
	/// 단어 힌트 제시를 알리고 관련 이벤트들을 호출합니다.
	/// <paramref name="hint"/>를 캐싱하여, 연속된 동일 매개 변수에 대해 한 번만 반응합니다.
	/// </summary>
	/// <param name="hint">단어 힌트</param>
	public void NotifyWordHint(string hint)
	{
		lock (turnHintWordLock)
		{
			if (string.Equals(hint, turnHintWordCache, StringComparison.OrdinalIgnoreCase))
				return;

			turnHintWordCache = hint;
			Log.Debug("Path example detected : {word}", hint);
			HintWordPresented?.Invoke(this, new WordPresentEventArgs(hint));
		}
	}

	/// <summary>
	/// 클래식 게임 모드(끝말잇기, 앞말잇기 등)에서 턴 시작을 알리고 관련 이벤트들을 호출합니다.
	/// <paramref name="turnIndex"/>를 캐싱하여, 연속된 동일 <paramref name="turnIndex"/>에 대하여 한 번만 반응합니다.
	/// </summary>
	/// <param name="isMyTurn">
	/// 시작된 턴이 확실히 내 턴인지의 여부를 나타냅니다.
	/// <paramref name="isMyTurn"/>가 <c>false</c>이나, <paramref name="turnIndex"/>를 보면 내 턴인 경우도 존재할 수 있으며 해당 경우 실제로도 내 턴이 맞습니다.
	/// </param>
	/// <param name="turnIndex">
	/// 시작된 턴의 인덱스를 나타냅니다.
	/// 만약 바로 이전에 같은 <paramref name="turnIndex"/>로 이 함수를 호출한 적이 있다면, 이번 새로운 요청은 무시될 수 있습니다.
	/// <paramref name="isMyTurn"/>가 <c>true</c>일 경우, <c>-1</c>일 수도 있으나, 해당 경우 실제로 내 턴이 맞습니다.
	/// </param>
	/// <param name="condition">턴의 단어 조건을 나타냅니다.</param>
	public void NotifyClassicTurnStart(bool isMyTurn, int turnIndex, WordCondition condition)
	{
		if (condition.IsEmpty())
			return;

		lock (sessionLock)
		{
			if (isMyTurn && turnIndex == -1) // DomHandler: turnIndex가 '-1'이나, isMyTurn은 'true' --- 내 턴 맞음
				turnIndex = Session.GetMyTurnIndex();

			if (!isMyTurn && turnIndex == Session.GetMyTurnIndex()) // WebSocketHandler: turnIndex는 내 턴을 나타내나, isMyTurn은 'false' --- 내 턴 맞음
				isMyTurn = true;

			if (!Session.AmIGaming || Session.TurnIndex == turnIndex && Session.IsTurnInProgress)
				return;

			Session.TurnIndex = turnIndex;
			Session.IsTurnInProgress = true;
			if (!isMyTurn && Session.GetRelativeTurn() == Session.GetMyPreviousUserTurn())
			{
				Log.Debug("Previous user mission character is {char}.", condition.MissionChar);
				Session.PreviousTurnMission = condition.MissionChar;
			}

			Log.Debug("Turn #{turnIndex} arrived (isMyTurn: {isMyTurn}), word condition is {word}.", turnIndex, isMyTurn, condition);

			if (Session.GameMode == GameMode.Free)
			{
				TurnStarted?.Invoke(this, new TurnStartEventArgs(new GameSessionState(Session), WordCondition.Empty));
				return;
			}

			Session.WordCondition = condition;

			TurnStarted?.Invoke(this, new TurnStartEventArgs(new GameSessionState(Session), condition));
		}
	}

	/// <summary>
	/// 게임의 턴 끝을 알리고 관련 이벤트들을 호출하며, 한 턴에 국한된 캐시들을 초기화합니다.
	/// 딱히 변수를 캐싱하거나 하지 않기 때문에, 한 번의 턴 변경 당 한 번씩만 호출되어야 합니다.
	/// </summary>
	/// <param name="value">끝난 턴에서 유저가 입력한 단어; 빈 문자열일 수 있습니다</param>
	public void NotifyClassicTurnEnd(string value)
	{
		lock (sessionLock)
		{
			if (!Session.AmIGaming || !Session.IsTurnInProgress)
				return;

			Log.Debug("Turn #{turnIndex} ended. Value is {value}.", Session.TurnIndex, value);
			Session.IsTurnInProgress = false;

			// Clear turn-specific caches
			turnErrorWordCache = null;

			TurnEnded?.Invoke(this, new TurnEndEventArgs(new GameSessionState(Session), value));
		}
	}

	/// <summary>
	/// 게임의 라운드 변경을 알리고 관련 이벤트들을 호출하며, 한 라운드에 국한된 캐시들을 초기화합니다.
	/// <paramref name="roundIndex"/>를 캐싱하여, 연속된 동일 매개 변수에 대해 한 번만 반응합니다.
	/// </summary>
	/// <param name="roundIndex">변경된(새로운) 라운드 인덱스; 만약 이가 <c>-1</c>이라면 값은 캐싱되나 관련 이벤트 호출이 이루어지지 않습니다</param>
	public void NotifyRound(int roundIndex)
	{
		lock (roundIndexLock)
		{
			if (roundIndex == roundIndexCache)
				return;

			roundIndexCache = roundIndex;
			if (roundIndex <= 0)
				return;

			// Clear round-specific caches
			turnErrorWordCache = null;
			turnHintWordCache = null;
			typingWordCache = null;
			wordHistoryCache = null;
			wordHistoriesCache = ImmutableList<string>.Empty;

			Log.Debug("Round changed to {round}.", roundIndex);
			RoundChanged?.Invoke(this, new RoundChangeEventArgs(roundIndex));
		}
	}

	public void NotifyTurnError(string word, TurnErrorCode errorCode, bool byDOM)
	{
		lock (turnErrorWordLock)
		{
			if (string.Equals(word, turnErrorWordCache, StringComparison.OrdinalIgnoreCase) || word.Contains("T.T", StringComparison.OrdinalIgnoreCase))
				return;
			Log.Debug("NotifyTurnError {word} byDOM={bydom}", word, byDOM);

			turnErrorWordCache = word;
			UnsupportedWordEntered?.Invoke(this, new UnsupportedWordEventArgs(
				Session,
				word,
				errorCode != TurnErrorCode.NotFound,
				errorCode is TurnErrorCode.NoEndWordOnBegin or TurnErrorCode.EndWord));
		}
	}

	public void NotifyWordHistories(IImmutableList<string> newHistories)
	{
		lock (wordHistoryLock)
		{
			if (Session.GameMode.IsFreeMode())
				return;

			foreach (var historyElement in newHistories)
			{
				if (!string.IsNullOrWhiteSpace(historyElement) && historyElement != wordHistoryCache /*WebSocket에 의한 단어 수신이 DOM 업데이트보다 더 먼저 일어나기에 이러한 코드가 작동 가능하다.*/ && !wordHistoriesCache.Contains(historyElement))
				{
					Log.Debug("DOM: Found new used word in history : {word}", historyElement);
					DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(historyElement));
				}
			}

			wordHistoriesCache = newHistories;
		}
	}

	// TODO: Optimize
	public void NotifyWordHistory(string newHistoryElement)
	{
		lock (wordHistoryLock)
		{
			if (Session.GameMode.IsFreeMode())
				return;

			if (wordHistoryCache?.Equals(newHistoryElement, StringComparison.OrdinalIgnoreCase) == true)
				return;

			Log.Debug("WS: Found new used word in history : {word}", newHistoryElement);
			DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(newHistoryElement));
		}
	}
}
