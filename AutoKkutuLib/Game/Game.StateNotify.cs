using AutoKkutuLib.Extension;
using Serilog;

namespace AutoKkutuLib.Game;
public partial class Game
{
	public void NotifyGameProgress(bool isGameInProgress)
	{
		if (isGameInProgress)
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
			if (!IsMyTurn)
			{
				IsMyTurn = true;

				if (CurrentGameMode == GameMode.Free)
				{
					MyWordPresented?.Invoke(this, new WordConditionPresentEventArgs(new WordCondition("", false), CurrentMissionChar));
					return;
				}

				if (wordCondition == null)
					return;

				if (wordCondition.CanSubstitution)
					Log.Information("My turn arrived, presented word is {word} (Subsitution: {subsituation})", wordCondition.Content, wordCondition.Substitution);
				else
					Log.Information("My turn arrived, presented word is {word}.", wordCondition.Content);
				CurrentPresentedWord = (WordCondition?)wordCondition;
				MyWordPresented?.Invoke(this, new WordConditionPresentEventArgs((WordCondition?)wordCondition, CurrentMissionChar));
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

	public void NotifyMissionChar(string missionChar)
	{
		if (string.Equals(missionChar, CurrentMissionChar, StringComparison.Ordinal))
			return;
		Log.Information("Mission char change detected : {word}", missionChar);
		CurrentMissionChar = missionChar;
	}
}
