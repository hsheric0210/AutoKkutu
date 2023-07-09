using System.Collections.Immutable;

namespace AutoKkutuLib.Game;
public sealed class WsSessionState
{
	public string MyUserId { get; }

	public bool IsGaming { get; private set; }

	public IImmutableList<string> GameSeq { get; private set; } = ImmutableList<string>.Empty;

	private int myTurnOrdinalCache = -1;
	public int MyTurnOrdinal
	{
		get
		{
			if (GameSeq != null && myTurnOrdinalCache < 0)
				myTurnOrdinalCache = GameSeq.IndexOf(MyUserId);
			return myTurnOrdinalCache;
		}
	}

	/// MUTABLE PROPERTIES ///

	/// <summary>
	/// 내 바로 이전 유저의 턴에서 제시된 미션 단어를 저장하기 위한 변수.
	/// </summary>
	public string? MyGamePreviousUserMission { get; set; } = null;

	public int Turn { get; set; } = -1;

	public WsSessionState(string myUserId)
	{
		MyUserId = myUserId;
		MyGamePreviousUserMission = null;
	}

	public WsSessionState(string myUserId, IList<string> seq)
	{
		MyUserId = myUserId;
		UpdateGameSeq(seq.ToImmutableList());
	}

	public void UpdateGameSeq(IImmutableList<string> seq)
	{
		GameSeq = seq;
		IsGaming = seq.Contains(MyUserId);
		myTurnOrdinalCache = -1; // invalidate cache
	}

	/// <summary>
	/// 실제로 지금 '몇 번째 플레이어의 턴인지'를 반환합니다.
	/// 예시로, 플레이어가 3명이고 지금이 63번째 턴이라면 지금은 0번째 사람(첫 번째 사람)의 턴입니다.
	/// 만약 게임이 진행 중이지 않다면, <c>-1</c>을 반환합니다.
	/// </summary>
	public int GetRelativeTurn() => GameSeq == null ? -1 : ((Turn + GameSeq.Count) % GameSeq.Count);

	/// <summary>
	/// 실제로 지금 '몇 번째 플레이어의 턴인지'를 반환합니다.
	/// 예시로, 플레이어가 3명이고 지금이 63번째 턴이라면 지금은 0번째 사람(첫 번째 사람)의 턴입니다.
	/// 만약 게임이 진행 중이지 않다면, <c>-1</c>을 반환합니다.
	/// </summary>
	public int GetTurnOf(string userId) => GameSeq == null ? -1 : GameSeq.IndexOf(userId);

	public bool IsMyTurn() => GameSeq != null && GetRelativeTurn() == MyTurnOrdinal;

	/// <summary>
	/// 내 바로 이전 사람의 턴 번째수를 반환합니다.
	/// 만약 '랜덤턴' 모드가 활성화되었다면 내 바로 이전 사람이 단어 입력을 마치더라도, 다음 턴이 나에게 오지 않을 수 있다는 것에 주의합니다.
	/// </summary>
	public int GetMyPreviousUserTurn() => GameSeq == null ? -1 : ((MyTurnOrdinal - 1 + GameSeq.Count) % GameSeq.Count);
}
