using AutoKkutuLib.Game.Enterer;
using Serilog;
using System.Collections.Immutable;

namespace AutoKkutuLib.Extension;

public static class PathListExtension
{
	/// <summary>
	/// 현재 턴 시간을 고려하여 사용 가능한 최적의 단어를 선정합니다.
	/// 만약 현재 남은 턴 시간 안에 입력할 수 있는 단어가 없다면, <c>(null, true)</c>를 대신 반환합니다.
	/// </summary>
	/// <param name="availableWordList">사용 가능한 모든 단어가 정렬되어 담겨 있는 목록</param>
	/// <param name="delay">입력 딜레이 정보</param>
	/// <param name="remainingTurnTime">남은 턴 시간</param>
	/// <param name="wordIndex">만약 주어진다면, N번째 최적의 단어를 선택합니다.</param>
	/// <returns>최적의 단어를 찾은 경우 <c>([최적의 단어], false)</c>, 그렇지 못한 경우 <c>(null, true)</c></returns>
	public static (string?, bool) ChooseBestWord(this IImmutableList<PathObject> availableWordList, EnterOptions delay, int remainingTurnTime, int wordIndex = 0)
	{
		if (availableWordList is null)
			throw new ArgumentNullException(nameof(availableWordList));

		if (!delay.DelayEnabled) // Skip filter
			return (availableWordList.Count <= wordIndex ? null : availableWordList[wordIndex].Content, false);

		// FIXME: Presearch 시 Time-Filter 적용이 안되는 버그
		// -> Pre-search 시 ChooseBestWord 호출할 때 remainingTurnTime를 min(<현재 게임 한 사람당 턴 시간>, <남은 라운드 시간>)으로 설정하여 호출하도록 하기
		// 아니면, 턴 시간 계산 공식을 긁어와서 써도 됨

		var remain = Math.Max(300, remainingTurnTime); // clamp to min. 300ms
		Log.Verbose("(TimeFilter) turnTime={time}, clamped={cTime}", remainingTurnTime, remain);

		PathObject[] arr = availableWordList.Where(po => delay.GetMaxDelay(po?.Content) <= remain).ToArray(); // 딜레이가 항상 최악으로 적용된다고 가정하고 탐색
		var word = arr.Length <= wordIndex ? null : arr[wordIndex].Content;
		if (word != null)
		{
			Log.Debug(I18n.TimeFilter_Success, remain, delay.GetMaxDelay(word));
			return (word, false);
		}

		// 만약 글자 당 딜레이 랜덤화가 활성화되었을 경우 운 좋게 턴 끝나기 전에 입력에 성공할 수도 있음
		if (delay.IsDelayPerCharRandomized)
		{
			Log.Warning("There is no optimal words to enter in turn time {turnTime}ms in list. Finding the possible one.", remain);

			arr = availableWordList.Where(po => delay.GetMinDelay(po?.Content) <= remain).ToArray();
			word = arr.Length <= wordIndex ? null : arr[wordIndex].Content;

			if (word != null)
			{
				Log.Debug(I18n.TimeFilter_Success, remain, delay.GetMaxDelay(word)); // TODO: create 'I18n.TimeFilter_Potential_Success' which accepts 'maxDelay' and 'minDelay'
				return (word, false);
			}
		}

		// 단어는 찾았으나 남은 턴 시간 안에 입력할 수 있는 단어가 없음...

		Log.Warning(I18n.TimeFilter_TimeOver, remain);

		PathObject? closest = availableWordList.MinBy(w => delay.GetMaxDelay(w.Content));
		if (closest != null)
			Log.Verbose("(TimeFilter) Closest word to the delay: {word} (minTime: {minTime}, maxTime: {maxTime})", closest.Content, delay.GetMinDelay(closest.Content), delay.GetMaxDelay(closest.Content));

		return (null, true);
	}
}
