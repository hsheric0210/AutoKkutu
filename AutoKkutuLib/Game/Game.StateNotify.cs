using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Game;
public partial class Game
{
	// 모든 캐시 필드들은 Thread-safe해야 함.
	private readonly object gameModeLock = new object();
	private readonly object turnLock = new object();

	private readonly object roundIndexLock = new object();
	private int roundIndexCache = -1;

	private long currentPresentedWordCacheTime = -1;

	private readonly object turnErrorWordLock = new object();
	private string? turnErrorWordCache;

	private readonly object turnHintWordLock = new object();
	private string? turnHintWordCache;

	//private readonly object typingWordLock = new object();
	private string? typingWordCache;

	private readonly object wordHistoryLock = new object();
	private string? wordHistoryCache;
	private IList<string>? wordHistoriesCache;

	public void NotifyGameProgress(bool isGameInProgress)
	{
		if (isGameInProgress)
		{
			if (!IsGameInProgress)
			{
				Log.Debug("New game started; Used word history flushed.");
				GameStarted?.Invoke(this, EventArgs.Empty);
				IsGameInProgress = true;
			}
		}
		else
		{
			if (!IsGameInProgress)
				return;

			Log.Debug("Game ended.");

			// Clear game-specific caches
			roundIndexCache = -1;
			currentPresentedWordCacheTime = -1;
			turnErrorWordCache = null;
			turnHintWordCache = null;
			typingWordCache = null;
			wordHistoryCache = null;
			wordHistoriesCache = null;
			IsMyTurn = false;

			GameEnded?.Invoke(this, EventArgs.Empty);
			IsGameInProgress = false;
		}
	}

	public void NotifyGameMode(GameMode gameMode, bool byDOM = false)
	{
		lock (gameModeLock)
		{
			if (gameMode == CurrentGameMode)
				return;
			CurrentGameMode = gameMode;
			Log.Debug("Game mode change detected : {gameMode} (byDOM: {byDOM})", gameMode.GameModeName(), byDOM);
			GameModeChanged?.Invoke(this, new GameModeChangeEventArgs(gameMode));
		}
	}

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

	// TODO: 다른 사람 턴 때도 이벤트 발생시키도록 수정
	public void NotifyMyTurn(bool isMyTurn, WordCondition? wordCondition = null, bool byDOM = false, bool bypassCache = false)
	{
		lock (turnLock)
		{
			if (isMyTurn)
			{
				if (IsMyTurn && !bypassCache)
					return;

				IsMyTurn = true;

				if (CurrentGameMode == GameMode.Free)
				{
					TurnStarted?.Invoke(this, new WordConditionPresentEventArgs(WordCondition.Empty, isMyTurn));
					return;
				}

				if (wordCondition == null)
					return;

				Log.Debug("My turn arrived (byDOM:{dom}), presented word is {word}.", byDOM, wordCondition);
				CurrentWordCondition = wordCondition;
				TurnStarted?.Invoke(this, new WordConditionPresentEventArgs((WordCondition)wordCondition, isMyTurn));

				return;
			}

			if (IsMyTurn && !bypassCache)
				return;

			IsMyTurn = false;

			Log.Debug("My turn ended. (byDOM:{dom})", byDOM);

			// Clear turn-specific caches
			turnErrorWordCache = null;

			TurnEnded?.Invoke(this, EventArgs.Empty);
		}
	}

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
			wordHistoriesCache = null;

			Log.Debug("Round Changed : {0}", roundIndex);
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
				word,
				errorCode != TurnErrorCode.NotFound,
				errorCode is TurnErrorCode.NoEndWordOnBegin or TurnErrorCode.EndWord,
				IsMyTurn));
		}
	}

	public void NotifyWordHistories(IList<string> newHistories)
	{
		lock (wordHistoryLock)
		{
			if (CurrentGameMode.IsFreeMode())
				return;

			foreach (var historyElement in newHistories)
			{
				if (!string.IsNullOrWhiteSpace(historyElement) && historyElement != wordHistoryCache /*WebSocket에 의한 단어 수신이 DOM 업데이트보다 더 먼저 일어나기에 이러한 코드가 작동 가능하다.*/ && (wordHistoriesCache == null || !wordHistoriesCache.Contains(historyElement)))
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
			if (wordHistoryCache?.Equals(newHistoryElement, StringComparison.OrdinalIgnoreCase) == true)
				return;

			Log.Debug("WS: Found new used word in history : {word}", newHistoryElement);
			DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(newHistoryElement));
		}
	}
}
