using System.Collections.Immutable;

namespace AutoKkutuLib.Game;
public partial class Game
{
	public void NotifyTypingBattleWord(string word)
	{
		lock (typingWordLock)
		{
			var tDelta = Environment.TickCount64 - currentPresentedWordCacheTime;
			if (string.Equals(word, typingWordCache, StringComparison.OrdinalIgnoreCase) && tDelta <= 1000) // 1초 이후에도 같은 단어가 여전히 나타나 있는 경우, 이벤트를 한번 더 발생시킴
				return;
			typingWordCache = word;
			currentPresentedWordCacheTime = Environment.TickCount64;
			LibLogger.Verbose(gameDomPoller, "Word detected : {word} (delay: {delta})", word, tDelta);
			TypingWordPresented?.Invoke(this, new WordPresentEventArgs(word));
		}
	}

	public void NotifyTypingBattleRoundChange(int roundIndex, IImmutableList<string> wordList)
	{
		NotifyRoundChange(roundIndex);
		lock (sessionLock)
		{
			Session.TypingWordList = wordList;
			Session.TypingWordIndex = 0;
		}
	}

	public void NotifyTypingBattleTurnStart()
	{
		lock (sessionLock)
		{
			Session.TypingWordIndex = 0;
		}

		TypingWordPresented?.Invoke(this, new WordPresentEventArgs(Session.TypingWordList[0]));
	}

	public void NotifyTypingBattleUpdate()
	{
		TypingWordPresented?.Invoke(this, new WordPresentEventArgs(Session.TypingWordList[Session.TypingWordIndex]));
	}

	public void NotifyTypingBattleTurnEndOk()
	{
		NotifyTypingBattleUpdate();
		lock (sessionLock)
		{
			Session.TypingWordIndex = (Session.TypingWordIndex + 1) % Session.TypingWordList.Count;
		}
	}
}
