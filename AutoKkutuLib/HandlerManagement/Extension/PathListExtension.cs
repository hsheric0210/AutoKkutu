using Serilog;

namespace AutoKkutuLib.HandlerManagement.Extension;

public static class PathListExtension
{
	public static string? GetWordByIndex(this IList<PathObject> qualifiedWordList, bool delayPerChar, int delay, int remainingTurnTime, int wordIndex = 0)
	{
		if (qualifiedWordList is null)
			throw new ArgumentNullException(nameof(qualifiedWordList));

		if (delayPerChar)
		{
			var remain = Math.Max(300, remainingTurnTime);
			PathObject[] arr = qualifiedWordList.Where(po => po!.Content.Length * delay <= remain).ToArray();
			var word = arr.Length <= wordIndex ? null : arr[wordIndex].Content;
			if (word == null)
				Log.Debug(I18n.TimeFilter_TimeOver, remain);
			else
				Log.Debug(I18n.TimeFilter_Success, remain, word.Length * delay);
			return word;
		}

		return qualifiedWordList.Count <= wordIndex ? null : qualifiedWordList[wordIndex].Content;
	}
}
