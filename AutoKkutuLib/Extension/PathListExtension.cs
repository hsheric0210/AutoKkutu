﻿using Serilog;

namespace AutoKkutuLib.Extension;

public static class PathListExtension
{
	public static (string?, bool) GetWordByIndex(this IList<PathObject> qualifiedWordList, bool delayPerChar, int delay, int remainingTurnTime, int wordIndex = 0)
	{
		if (qualifiedWordList is null)
			throw new ArgumentNullException(nameof(qualifiedWordList));

		if (delayPerChar)
		{
			var remain = Math.Max(300, remainingTurnTime);
			PathObject[] arr = qualifiedWordList.Where(po => po!.Content.Length * delay <= remain).ToArray();
			var word = arr.Length <= wordIndex ? null : arr[wordIndex].Content;
			if (word == null)
			{
				Log.Warning(I18n.TimeFilter_TimeOver, remain);
				return (null, true);
			}

			Log.Debug(I18n.TimeFilter_Success, remain, word.Length * delay);
			return (word, false);
		}

		return (qualifiedWordList.Count <= wordIndex ? null : qualifiedWordList[wordIndex].Content, false);
	}
}
