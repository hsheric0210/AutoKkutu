using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Game;
public partial class Game
{
	private int roundIndexCache;
	private string turnErrorWordCache = "";
	private string exampleWordCache = "";
	private string currentPresentedWordCache = "";
	private long currentPresentedWordCacheTime = -1;

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
		if (string.Equals(hint, exampleWordCache, StringComparison.OrdinalIgnoreCase))
			return;
		exampleWordCache = hint;
		Log.Information("Path example detected : {word}", hint);
		ExampleWordPresented?.Invoke(this, new WordPresentEventArgs(hint));
	}

	public void NotifyMyTurn(bool isMyTurn, WordCondition? wordCondition)
	{
		if (isMyTurn)
		{
			if (IsMyTurn)
				return;

			IsMyTurn = true;

			if (CurrentGameMode == GameMode.Free)
			{
				MyTurnStarted?.Invoke(this, new WordConditionPresentEventArgs(new WordCondition("")));
				return;
			}

			if (wordCondition == null)
				return;

			Log.Information("My turn arrived, presented word is {word}.", wordCondition);
			CurrentPresentedWord = wordCondition;
			MyTurnStarted?.Invoke(this, new WordConditionPresentEventArgs((WordCondition)wordCondition));

			return;
		}

		if (!IsMyTurn)
			return;
		IsMyTurn = false;

		Log.Debug("My turn ended.");
		MyTurnEnded?.Invoke(this, EventArgs.Empty);
	}

	public void NotifyRound(int roundIndex)
	{
		if (roundIndex == roundIndexCache)
			return;

		roundIndexCache = roundIndex;
		if (roundIndex <= 0)
			return;

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

	public void NotifyWordHistories(string[] newHistories)
	{
		if (CurrentGameMode.IsFreeMode())
			return;

		for (var index = 0; index < 6; index++)
		{
			var word = newHistories[index];
			if (!string.IsNullOrWhiteSpace(word) && !wordHistoryCache.Contains(word))
			{
				Log.Information("Found new used word in history : {word}", word);
				DiscoverWordHistory?.Invoke(this, new WordHistoryEventArgs(word));
			}
		}

		Array.Copy(newHistories, wordHistoryCache, 6);
	}

	public void NotifyWordHistory(string newHistoryElement)
	{
		// Ugly solution :(
		var newHistories = new string[6];
		Array.Copy(wordHistoryCache, 0, newHistories, 1, 5);
		newHistories[0] = newHistoryElement;
		NotifyWordHistories(newHistories);
	}
}
