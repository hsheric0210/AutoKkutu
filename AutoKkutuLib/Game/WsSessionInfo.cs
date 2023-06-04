using System.Collections.Immutable;

namespace AutoKkutuLib.Game;
public struct WsSessionInfo
{
	public string MyUserId { get; }

	public bool IsGaming => MyGameTurns != null;
	public IImmutableList<string>? MyGameTurns { get; set; }

	private int myTurnOrdinalCache = -1;
	public int MyTurnOrdinal
	{
		get
		{
			if (MyGameTurns != null && myTurnOrdinalCache < 0)
				myTurnOrdinalCache = MyGameTurns.IndexOf(MyUserId);
			return myTurnOrdinalCache;
		}
	}

	/// <summary>
	/// 내 바로 이전 유저의 턴에서 제시된 미션 단어를 저장하기 위한 변수.
	/// </summary>
	public string? MyGamePreviousUserMission { get; set; } = null;

	public WsSessionInfo(string myUserId)
	{
		MyUserId = myUserId;
		MyGameTurns = null;
		MyGamePreviousUserMission = null;
	}

	public WsSessionInfo(string myUserId, IList<string> myGameTurns)
	{
		MyUserId = myUserId;
		MyGameTurns = myGameTurns.IndexOf(myUserId) >= 0 ? myGameTurns.ToImmutableList() : (IImmutableList<string>?)null;
	}

	/// <summary>
	/// 실제로 지금 '몇 번째 플레이어의 턴인지'를 반환합니다.
	/// 예시로, 플레이어가 3명이고 지금이 63번째 턴이라면 지금은 0번째 사람(첫 번째 사람)의 턴입니다.
	/// 만약 게임이 진행 중이지 않다면, <c>-1</c>을 반환합니다.
	/// </summary>
	public int GetRelativeTurn(int absoluteTurn) => MyGameTurns == null ? -1 : ((absoluteTurn + MyGameTurns.Count) % MyGameTurns.Count);

	/// <summary>
	/// 실제로 지금 '몇 번째 플레이어의 턴인지'를 반환합니다.
	/// 예시로, 플레이어가 3명이고 지금이 63번째 턴이라면 지금은 0번째 사람(첫 번째 사람)의 턴입니다.
	/// 만약 게임이 진행 중이지 않다면, <c>-1</c>을 반환합니다.
	/// </summary>
	public int GetTurnOf(string userId) => MyGameTurns == null ? -1 : MyGameTurns.IndexOf(userId);

	/// <summary>
	/// 실제로 지금 '몇 번째 플레이어의 턴인지'를 반환합니다.
	/// 예시로, 플레이어가 3명이고 지금이 63번째 턴이라면 지금은 0번째 사람(첫 번째 사람)의 턴입니다.
	/// 만약 게임이 진행 중이지 않다면, <c>-1</c>을 반환합니다.
	/// </summary>
	public bool IsMyTurn(int absoluteTurn) => MyGameTurns != null && GetRelativeTurn(absoluteTurn) == MyTurnOrdinal;

	/// <summary>
	/// 내 바로 이전 사람의 턴 번째수를 반환합니다.
	/// 만약 '랜덤턴' 모드가 활성화되었다면 내 바로 이전 사람이 단어 입력을 마치더라도, 다음 턴이 나에게 오지 않을 수 있다는 것에 주의합니다.
	/// </summary>
	public int GetMyPreviousUserTurn() => MyGameTurns == null ? -1 : ((MyTurnOrdinal - 1 + MyGameTurns.Count) % MyGameTurns.Count);
}
