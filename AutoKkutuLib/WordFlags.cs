namespace AutoKkutuLib;

/// <summary>
/// 데이터베이스의 'flags' 행에 해당하는 비스마스크 플래그를 나타냅니다.
/// </summary>
[Flags]
public enum WordFlags
{
	None = 0,

	/// <summary>
	/// 한방 단어
	/// </summary>
	EndWord = 1 << 0,

	/// <summary>
	/// 공격 단어
	/// </summary>
	AttackWord = 1 << 1,

	/// <summary>
	/// 앞말잇기 한방 단어
	/// </summary>
	ReverseEndWord = 1 << 2,

	/// <summary>
	/// 앞말잇기 공격 단어
	/// </summary>
	ReverseAttackWord = 1 << 3,

	/// <summary>
	/// 가운뎃말잇기 한방 단어
	/// </summary>
	MiddleEndWord = 1 << 4,

	/// <summary>
	/// 가운뎃말잇기 공격 단어
	/// </summary>
	MiddleAttackWord = 1 << 5,

	/// <summary>
	/// 끄투 한방 단어
	/// </summary>
	KkutuEndWord = 1 << 6,

	/// <summary>
	/// 끄투 공격 단어
	/// </summary>
	KkutuAttackWord = 1 << 7,

	/// <summary>
	/// 쿵쿵따에서 사용 가능한 단어인지 여부
	/// </summary>
	KKT3 = 1 << 8,

	/// <summary>
	/// 쿵쿵따-2323에서 사용 가능한 단어인지 여부
	/// </summary>
	KKT2 = 1 << 9,

	/// <summary>
	/// 쿵쿵따 한방 단어
	/// </summary>
	KKTEndWord = 1 << 10,

	/// <summary>
	/// 쿵쿵따 공격 단어
	/// </summary>
	KKTAttackWord = 1 << 11
}
