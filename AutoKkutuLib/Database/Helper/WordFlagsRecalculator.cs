using System.Text.RegularExpressions;

namespace AutoKkutuLib.Database.Helper;
internal sealed class WordFlagsRecalculator
{
	private readonly NodeManager nodeManager;
	private readonly ThemeManager themeManager;
	private static readonly Regex KoreanMatcher = new Regex("[ㄱ-ㅎㅏ-ㅣ가-힣]+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
	private static readonly Regex EnglishMatcher = new Regex("[a-zA-Z]+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

	public WordFlagsRecalculator(NodeManager nodeManager, ThemeManager themeManager)
	{
		this.nodeManager = nodeManager;
		this.themeManager = themeManager;
	}

	public WordFlags GetWordFlags(string word)
	{
		var flags = WordFlags.None;

		var wordLength = word.Length;
		if (wordLength == 2)
			flags |= WordFlags.KKT2;
		else if (wordLength == 3)
			flags |= WordFlags.KKT3;

		try
		{
			if (KoreanMatcher.IsMatch(word))
				flags |= WordFlags.Korean;
			if (EnglishMatcher.IsMatch(word))
				flags |= WordFlags.English;
		}
		catch (Exception ex)
		{
			LibLogger.Warn<NodeManager>(ex, "GetWordFlags: Korean/English matcher timed out.");
		}

		// TODO: 어인정 단어
		//
		//if (themeManager.IsWordInjeong(word))
		//	flags |= WordFlags.Injeong;
		//

		return nodeManager.GetWordNodeFlags(word, flags);
	}
}
