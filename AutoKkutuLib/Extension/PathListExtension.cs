using AutoKkutuLib.Game;
using Serilog;
using System.Collections.Immutable;

namespace AutoKkutuLib.Extension;

public static class PathListExtension
{
	/// <summary>
	/// 현재 턴 시간을 고려하여 사용 가능한 최적의 단어를 선정합니다.
	/// </summary>
	/// <param name="availableWordList">사용 가능한 모든 단어가 정렬되어 담겨 있는 목록</param>
	/// <param name="delay">입력 딜레이 정보</param>
	/// <param name="remainingTurnTime">남은 턴 시간</param>
	/// <param name="wordIndex">만약 주어진다면, N번째 최적의 단어를 선택합니다.</param>
	/// <returns>(<c>최적의 단어</c>, <c>턴 시간 초과 여부</c>)</returns>
	public static (string?, bool) ChooseBestWord(this IImmutableList<PathObject> availableWordList, AutoEnterOptions delay, int remainingTurnTime, int wordIndex = 0)
	{
		if (availableWordList is null)
			throw new ArgumentNullException(nameof(availableWordList));

		if (delay.DelayPerCharEnabled)
		{
			var remain = Math.Max(300, remainingTurnTime);
			Log.Verbose("(TimeFilter) turnTime={time}, clamped={cTime}", remainingTurnTime, remain);
			PathObject[] arr = availableWordList.Where(po => po!.Content.Length * delay.DelayInMillis <= remain).ToArray();
			var word = arr.Length <= wordIndex ? null : arr[wordIndex].Content;
			if (word == null)
			{
				Log.Warning(I18n.TimeFilter_TimeOver, remain);

				PathObject? closest = availableWordList.MinBy(w => w!.Content.Length * delay.DelayInMillis);
				if (closest != null)
					Log.Verbose("(TimeFilter) Closest word to the delay: {word} (time: {time})", closest.Content, closest.Content.Length * delay.DelayInMillis);

				return (null, true);
			}

			Log.Debug(I18n.TimeFilter_Success, remain, word.Length * delay.DelayInMillis);
			return (word, false);
		}

		return (availableWordList.Count <= wordIndex ? null : availableWordList[wordIndex].Content, false);
	}
}
