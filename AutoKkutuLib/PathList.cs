using AutoKkutuLib.Game.Enterer;
using System.Collections;
using System.Collections.Immutable;

namespace AutoKkutuLib;
public readonly struct PathList : IEnumerable<PathObject>
{
	private readonly List<PathObject> list;

	public IImmutableList<PathObject> List => list.ToImmutableList();
	public PathDetails Details { get; }
	public int Count => list.Count;

	public PathList(IEnumerable<PathObject> list, PathDetails details)
	{
		this.list = new List<PathObject>(list);
		Details = details;
	}

	private static T RandomElement<T>(IReadOnlyList<T> list, int range) => list[Random.Shared.Next(Math.Min(range, list.Count))];

	private static T RandomElement<T>(T[] list, int range) => list[Random.Shared.Next(Math.Min(range, list.Length))];

	/// <summary>
	/// 현재 턴 시간을 고려하여 사용 가능한 최적의 단어를 선정하고, 그 중 위에서부터 <paramref name="randomSelectionRange"/>개의 단어들 중 아무거나 반환합니다.
	/// </summary>
	/// <param name="delay">입력 딜레이 정보</param>
	/// <param name="remainingTurnTime">남은 턴 시간</param>
	/// <param name="randomSelectionRange">사용 가능한 단어들 중 </param>
	public BestPath ChooseBestWord(EnterOptions delay, int remainingTurnTime, int randomSelectionRange = 1)
	{
		if (list.Count == 0)
			return BestPath.Empty();

		if (!delay.DelayEnabled) // Skip filter
			return BestPath.Valid(RandomElement(list, randomSelectionRange));

		// FIXME: Presearch 시 Time-Filter 적용이 안되는 버그
		// -> Pre-search 시 ChooseBestWord 호출할 때 remainingTurnTime를 min(<현재 게임 한 사람당 턴 시간>, <남은 라운드 시간>)으로 설정하여 호출하도록 하기
		// 아니면, 턴 시간 계산 공식을 긁어와서 써도 됨

		var remain = Math.Max(300, remainingTurnTime); // clamp to min. 300ms
		LibLogger.Verbose<PathList>("(TimeFilter) turnTime={time}, clamped={cTime}", remainingTurnTime, remain);

		var arr = list.Where(po => delay.GetMaxDelay(po?.Content) <= remain).ToArray(); // 딜레이가 항상 최악으로 적용된다고 가정하고 탐색
		if (arr.Length > 0)
		{
			// 굳이 길지만 남은 턴 시간 안에 다 입력을 끝마칠 수 있을지 보장되지 않은 단어들을 위험부담을 감수하고 추천하기보다는
			// 남은 턴 시간 안에 최악의 경우에도 항상 입력을 끝마칠 수 있는 '안전한 단어'를 우선
			var path = RandomElement(arr, randomSelectionRange);
			LibLogger.Debug<PathList>(I18n.TimeFilter_Success, remain, delay.GetMaxDelay(path.Content));
			return BestPath.Valid(path);
		}

		// 비록 최악의 경우에는 입력을 끝마치치 못한 채로 턴이 끝날 수도 있으나 운 좋게 턴 끝나기 전에 입력에 성공할 수도 있는 단어들
		if (delay.StartDelayRandom > 0 || delay.DelayBeforeKeyUpRandom > 0 || delay.DelayBeforeKeyUpRandom > 0)
		{
			LibLogger.Warn<PathList>("There is no optimal words to enter in turn time {turnTime}ms in list. Finding the possible one.", remain);

			arr = list.Where(po => delay.GetMinDelay(po?.Content) <= remain).ToArray();
			if (arr.Length > 0)
			{
				var path = RandomElement(arr, randomSelectionRange);
				LibLogger.Debug<PathList>(I18n.TimeFilter_Success, remain, delay.GetMaxDelay(path.Content)); // TODO: create 'I18n.TimeFilter_Potential_Success' which accepts 'maxDelay' and 'minDelay'
				return BestPath.Valid(path);
			}
		}

		// 단어는 찾았으나 남은 턴 시간 안에 입력할 수 있는 단어가 없음...

		LibLogger.Warn<PathList>(I18n.TimeFilter_TimeOver, remain);

		var closest = list.MinBy(w => delay.GetMaxDelay(w.Content));
		if (closest != null)
			LibLogger.Verbose<PathList>("(TimeFilter) Closest word to the delay: {word} (minTime: {minTime}, maxTime: {maxTime})", closest.Content, delay.GetMinDelay(closest.Content), delay.GetMaxDelay(closest.Content));

		return BestPath.AllFilteredOut();
	}

	// 이미 사용된 단어는 자동적으로 제거됨 -> 랜덤 선택할 때 자동으로 제외되므로 고민할 필요 X
	public void PushUsed(PathObject obj) => list.Remove(obj);

	public void PushUsed(string content) => list.RemoveAll(o => o.Content.Equals(content, StringComparison.OrdinalIgnoreCase));

	public IEnumerator<PathObject> GetEnumerator() => List.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

	public readonly struct BestPath
	{
		public readonly bool Available { get; }
		public readonly bool AllTimeFilteredOut { get; }
		public readonly PathObject Object { get; }

		private BestPath(bool available, bool allTimeFiltered, PathObject obj)
		{
			Available = available;
			AllTimeFilteredOut = allTimeFiltered;
			Object = obj;
		}

		public static implicit operator PathObject(BestPath param) => param.Object;

		public static BestPath Valid(PathObject obj) => new(true, false, obj);

		public static BestPath AllFilteredOut() => new(false, true, PathObject.Empty);

		public static BestPath Empty() => new(false, false, PathObject.Empty);
	}
}
