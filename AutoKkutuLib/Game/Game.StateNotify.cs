using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private int roundIndexCache = -1;
	private long currentPresentedWordCacheTime = -1;
	private string? turnErrorWordCache;
	private string? turnHintWordCache;
	private string? typingWordCache;
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
			Interlocked.Exchange(ref isMyTurn, 0);

			GameEnded?.Invoke(this, EventArgs.Empty);
			IsGameInProgress = false;
		}
	}

	public void NotifyGameMode(GameMode gameMode)
	{
		if (gameMode == CurrentGameMode)
			return;
		CurrentGameMode = gameMode;
		Log.Information("Game mode change detected : {gameMode}", gameMode.GameModeName());
		GameModeChanged?.Invoke(this, new GameModeChangeEventArgs(gameMode));
	}

	public void NotifyWordHint(string hint)
	{
		if (string.Equals(hint, turnHintWordCache, StringComparison.OrdinalIgnoreCase))
			return;

		turnHintWordCache = hint;
		Log.Information("Path example detected : {word}", hint);
		ExampleWordPresented?.Invoke(this, new WordPresentEventArgs(hint));
	}

	public void NotifyMyTurn(bool myTurn, WordCondition? wordCondition = null, bool byDOM = false, bool bypassCache = false)
	{
		if (myTurn)
		{
			if (Interlocked.Exchange(ref isMyTurn, 1) == 1 && !bypassCache)
				return;

			if (CurrentGameMode == GameMode.Free)
			{
				MyTurnStarted?.Invoke(this, new WordConditionPresentEventArgs(WordCondition.Empty));
				return;
			}

			if (wordCondition == null)
				return;

			Log.Information("My turn arrived (byDOM:{dom}), presented word is {word}.", byDOM, wordCondition);
			CurrentPresentedWord = wordCondition;
			MyTurnStarted?.Invoke(this, new WordConditionPresentEventArgs((WordCondition)wordCondition));

			return;
		}

		if (Interlocked.Exchange(ref isMyTurn, 0) == 0 && !bypassCache)
			return;

		Log.Debug("My turn ended. (byDOM:{dom})", byDOM);

		// Clear turn-specific caches
		turnErrorWordCache = null;

		MyTurnEnded?.Invoke(this, EventArgs.Empty);
	}

	public void NotifyRound(int roundIndex)
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

		Log.Information("Round Changed : {0}", roundIndex);
		RoundChanged?.Invoke(this, new RoundChangeEventArgs(roundIndex));
	}

	public void NotifyTurnError(string word, TurnErrorCode errorCode)
	{
		if (string.Equals(word, turnErrorWordCache, StringComparison.OrdinalIgnoreCase) || word.Contains("T.T", StringComparison.OrdinalIgnoreCase))
			return;

		turnErrorWordCache = word;
		UnsupportedWordEntered?.Invoke(this, new UnsupportedWordEventArgs(word, errorCode != TurnErrorCode.NotFound, errorCode is TurnErrorCode.NoEndWordOnBegin or TurnErrorCode.EndWord));

		// TODO: Remove
		if (IsMyTurn)
			MyPathIsUnsupported?.Invoke(this, new UnsupportedWordEventArgs(word, errorCode != TurnErrorCode.NotFound, errorCode is TurnErrorCode.NoEndWordOnBegin or TurnErrorCode.EndWord));
	}

	public void NotifyWordHistories(IList<string> newHistories)
	{
		if (CurrentGameMode.IsFreeMode())
			return;

		foreach (var historyElement in newHistories)
		{
			if (!string.IsNullOrWhiteSpace(historyElement) && historyElement != wordHistoryCache /*WebSocket에 의한 단어 수신이 DOM 업데이트보다 더 먼저 일어나기에 이러한 코드가 작동 가능하다.*/ && (wordHistoriesCache == null || !wordHistoriesCache.Contains(historyElement)))
			{
				Log.Information("DOM: Found new used word in history : {word}", historyElement);
				DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(historyElement));
			}
		}

		wordHistoriesCache = newHistories;
	}

	// TODO: Optimize
	public void NotifyWordHistory(string newHistoryElement)
	{
		if (wordHistoryCache?.Equals(newHistoryElement, StringComparison.OrdinalIgnoreCase) == true)
			return;

		Log.Information("WS: Found new used word in history : {word}", newHistoryElement);
		DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(newHistoryElement));
	}
}
