using AutoKkutuLib.Extension;
using System.Collections.Immutable;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private const string gameStateNotify = "Game.StateNotify";

	// 모든 상태 필드들은 Thread-safe해야 함.
	private readonly object sessionLock = new();

	private readonly object roundIndexLock = new();
	private int roundIndexCache = -1;

	private long currentPresentedWordCacheTime = -1;

	private readonly object turnErrorWordLock = new();
	private string? turnErrorWordCache;

	private readonly object turnHintWordLock = new();
	private string? turnHintWordCache;

	private readonly object typingWordLock = new();
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
			LibLogger.Debug(gameStateNotify, "New game session detected with UserId: {uid}.", myUserId);
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

		LibLogger.Debug(gameStateNotify, "Game-seq changed to: {seq}", string.Join(", ", seq));

		if (Session.AmIGaming == prevGamingState)
			return;

		LibLogger.Debug(gameStateNotify, "Gaming state changed to: {state}", Session.AmIGaming);

		if (Session.AmIGaming)
		{
			LibLogger.Debug(gameStateNotify, "New game started; Used word history flushed.");
			GameStarted?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			LibLogger.Debug(gameStateNotify, "Game ended.");

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

		LibLogger.Debug(gameStateNotify, "Game mode change detected : {gameMode} (byDOM: {byDOM})", gameMode.GameModeName(), byDOM);
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
			LibLogger.Debug(gameStateNotify, "Path example detected : {word}", hint);
			HintWordPresented?.Invoke(this, new WordPresentEventArgs(hint));
		}
	}

	/// <summary>
	/// 게임의 라운드 변경을 알리고 관련 이벤트들을 호출하며, 한 라운드에 국한된 캐시들을 초기화합니다.
	/// <paramref name="roundIndex"/>를 캐싱하여, 연속된 동일 매개 변수에 대해 한 번만 반응합니다.
	/// </summary>
	/// <param name="roundIndex">변경된(새로운) 라운드 인덱스; 만약 이가 <c>-1</c>이라면 값은 캐싱되나 관련 이벤트 호출이 이루어지지 않습니다</param>
	public void NotifyRoundChange(int roundIndex)
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

			lock (sessionLock)
			{
				// Reset turn if round is changed
				Session.TurnIndex = -1;
				Session.IsTurnInProgress = false;
			}

			LibLogger.Debug(gameStateNotify, "Round changed to {round}.", roundIndex);
			RoundChanged?.Invoke(this, new RoundChangeEventArgs(roundIndex));
		}
	}

	public void NotifyTurnError(string word, TurnErrorCode errorCode, bool byDOM)
	{
		lock (turnErrorWordLock)
		{
			if (string.Equals(word, turnErrorWordCache, StringComparison.OrdinalIgnoreCase) || word.Contains("T.T", StringComparison.OrdinalIgnoreCase))
				return;
			LibLogger.Debug(gameStateNotify, "NotifyTurnError {word} byDOM={bydom}", word, byDOM);

			turnErrorWordCache = word;
			UnsupportedWordEntered?.Invoke(this, new UnsupportedWordEventArgs(
				Session,
				word,
				errorCode != TurnErrorCode.NotFound,
				errorCode is TurnErrorCode.NoEndWordOnBegin or TurnErrorCode.EndWord));
		}
	}

	/// <summary>
	/// FOR DOM ONLY todo: remove or move to Game.DomPoller (or other compatibility layer)
	/// </summary>
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
					LibLogger.Debug(gameStateNotify, "DOM: Found new used word in history : {word}", historyElement);
					DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(historyElement));
				}
			}

			wordHistoriesCache = newHistories;
		}
	}

	public void NotifyWordHistory(string newHistoryElement)
	{
		lock (wordHistoryLock)
		{
			if (Session.GameMode.IsFreeMode())
				return;

			if (wordHistoryCache?.Equals(newHistoryElement, StringComparison.OrdinalIgnoreCase) == true)
				return;

			LibLogger.Debug(gameStateNotify, "WS: Found new used word in history : {word}", newHistoryElement);
			DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(newHistoryElement));
		}
	}
}
